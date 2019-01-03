using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PortfolioHelper
{
    /// <summary>
    /// The columns created in the output file
    /// </summary>
    public enum Headers
    {
        Symbol,
        PurchaseDate,
        PurchaseSharePrice,
        Shares,
        PurchaseValue,
        CurrentSharePrice,
        CurrentValue,
        CapitalGain,
        CapitalGainPercent,
        AnnualCapitalGainPercent,
        DividendsPerShare,
        TotalDividends,
        DividendPercent,
        AnnualDividendPercent,
        TotalGain,
        TotalGainPercent,
        AnnualTotalGainPercent,
        Error,
    }

    class Program
    {
        /// <summary>
        /// The TDAmeritrade API key used for requests
        /// </summary>
        const string ApiKey = "MCALLIGARODEV";

        /// <summary>
        /// The enum index of the last column required in the input file (ie PurchaseSharePrice)
        /// </summary>
        const int LastRequiredColumnIndex = 3;

        /// <summary>
        /// The text given to the column headers defined in the enum Headers
        /// </summary>
        static readonly string[] HeaderTexts =
        {
            "Stock Symbol",
            "Purchase Date",
            "Purchase Share Price",
            "Shares",
            "Total Purchase Cost",
            "Current Share Price",
            "Current Holdings",
            "Capital Gain",
            "Capital Gain %",
            "Annual Capital Gain %",
            "Dividends Per Share",
            "Total Dividends",
            "Dividend %",
            "Annual Dividend %",
            "Total Gain",
            "Total Gain %",
            "Annual Total Gain %",
            "Error Notes",
        };

        /// <summary>
        /// The help text printed when the user enters "?"
        /// </summary>
        static readonly string[] HelpText =
        {
            "PortfolioHelper",
            "Written by Matthew Calligaro, January 2019",
            "",
            ">> Summary",
            "PortfolioHelper tracks a stock portfolio by looking up the current price and dividend history of a stock and using this to calculate statistics such as the capital gain, total dividends to date, annual rate of return, etc.",
            "",
            ">> Input File Format",
            "The input file must be a .csv with the following four columns (each with the exact column header as listed):",
            "Stock Symbol: The symbol of the stock, such as MSFT for Microsoft",
            "Purchase Date: The date on which the stock was purchased, such as 1/1/2018",
            "Purchase Share Price: The price per share at which the stock was purchased, such as $100.00.",
            "Shares: The number of shares of the stock purchased, such as 10",
            "These columns can appear in any order, and the input file can contain any number of additional columns.",
            "",
            ">> Command Format",
            "Commands should be of the form \"<inputFilePath> <outputFilePath>\"",
            "In place of <outputFilePath>, you can use \"o\" to overwrite the input file or \"d\" to use the default filename.",
            "If <inputFilePath> and <outputFilePath> are equal, the input file will be overwritten.",
            "Example Command: stocks.csv stocks_edited.csv",
            "",
            ">> Additional Commands",
            "quit: Closes the application",
            "?: Prints this help text",
            "template: Creates a template input file named template.csv in the current directory",
            "",
        };

        /// <summary>
        /// The lines contained in template.csv
        /// </summary>
        static readonly string[] TemplateLines =
        {
            "Stock Symbol,Purchase Date,Purchase Share Price,Shares,Additional Notes",
            "MSFT,1/1/2018,$100.00,10,You can add as many other columns as you like.  These will be ignored (and left untouched) by PortfolioHelper.  Only the first 4 columns are necessary and can be ordered however you like."
        };

        /// <summary>
        /// A dictionary mapping stock symbols to their current prices
        /// </summary>
        static Dictionary<string, double> prices = new Dictionary<string, double>();

        /// <summary>
        /// A dictionary mapping stock symbols to their dividend histories
        /// </summary>
        static Dictionary<string, DividendPayment[]> dividends = new Dictionary<string, DividendPayment[]>();

        /// <summary>
        /// Entry point for the application, continuously reads user input
        /// </summary>
        /// <param name="args">arguments passed to the application (not used)</param>
        static void Main(string[] args)
        {
            bool writePrompt = true;
            while (true)
            {
                if (writePrompt)
                {
                    Console.WriteLine("Please enter an input of the form \"<inputFilePath> <outputFilePath>\" or enter \"?\" for help:");
                }

                writePrompt = ParseInput(Console.ReadLine());
            }
        }

        /// <summary>
        /// Interpret the user's input and take the corresponding action
        /// </summary>
        /// <param name="input">the user's input</param>
        /// <returns>true if the program should reprint the prompt</returns>
        static bool ParseInput(string input)
        {
            // Do nothing on a blank input
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            // Check for special commands
            switch (input.Replace(" ", "").ToLower())
            {
                case "q":
                case "quit":
                case "exit":
                    Environment.Exit(0);
                    return false;

                case "?":
                case "h":
                case "help":
                    Help();
                    return false;

                case "t":
                case "template":
                    Template();
                    return true;

                case "thanks":
                case "thanks!":
                case "thankyou":
                    Console.WriteLine("You're welcome!");
                    return true;
            }

            string[] inputSplit = input.Split(' ');

            // Error check input
            if (inputSplit.Length < 2)
            {
                Console.WriteLine("Invalid input.  Please try again or enter \"?\" for help:");
                return false;
            }
            if (Path.GetExtension(inputSplit[0]) != ".csv")
            {
                Console.WriteLine("The input must be a .csv file.  Please try again or enter \"?\" for help:");
                return false;
            }

            // Read the input file
            string[] inputFileLines;
            try
            {
                inputFileLines = File.ReadAllLines(inputSplit[0]);
            }
            catch (IOException)
            {
                Console.WriteLine("Could not read from input file.  Make sure that the input file exists and is not in use by another application.  Please try again:");
                return false;
            }

            // Determine outputFilePath
            string outputFilePath;
            switch (inputSplit[1].ToLower())
            {
                case "d":
                case "default":
                    outputFilePath = inputSplit[0].Substring(0, inputSplit[0].Length - 4) + "_updated.csv";
                    break;

                case "o":
                case "overwrite":
                case "update":
                    outputFilePath = inputSplit[0];
                    break;

                default:
                    outputFilePath = Path.GetExtension(inputSplit[1]) == ".csv" ? inputSplit[1] : inputSplit[1] + ".csv";
                    break;
            }

            UpdatePortfolio(inputFileLines, outputFilePath);
            return true;
        }
        
        /// <summary>
        /// Print the help text to the console
        /// </summary>
        static void Help()
        {
            foreach (string line in HelpText)
            {
                Console.WriteLine(line);
            }
        }

        /// <summary>
        /// Create a file named template.csv in the current directory which provides an example input file
        /// </summary>
        static void Template()
        {
            try
            {
                File.WriteAllLines("template.csv", TemplateLines);
                Console.WriteLine("An example input file has been saved in the current directory as template.csv.");
            }
            catch (IOException)
            {
                Console.WriteLine("template.csv is currently in use so could not be written to.  Please close the application using template.csv and try again.");
            }
        }

        /// <summary>
        /// For a given input, perform the desired calculations and save them to outputFile
        /// </summary>
        /// <param name="inputFileLines">the lines of the input file</param>
        /// <param name="outputFile">the path of the file to which the output is saved</param>
        static void UpdatePortfolio(string[] inputFileLines, string outputFile)
        {
            // Remove empty lines at the end of inputFileLines
            int newInputLength = inputFileLines.Length;
            while (newInputLength > 0 && string.IsNullOrWhiteSpace(inputFileLines[newInputLength - 1].Replace(",", "")))
            {
                newInputLength--;
            }
            Array.Resize(ref inputFileLines, newInputLength);

            string[] headers = ParseCSVLine(inputFileLines[0]);
            int[] columns = new int[HeaderTexts.Length];
            int nextCol = headers.Length;

            // Find the indicies of the columns described in the enum Headers
            for (int i = 0; i < HeaderTexts.Length; i++)
            {
                // Try to find the column header in the input file
                bool foundMatch = false;
                for (int j = 0; j < headers.Length; ++j)
                {
                    if (headers[j] == HeaderTexts[i])
                    {
                        columns[i] = j;
                        foundMatch = true;
                        break;
                    }
                }

                if (!foundMatch)
                {
                    // If the column was a necessary input, return an error
                    if (i <= LastRequiredColumnIndex)
                    {
                        Console.WriteLine("Input file was not formatted correctly.  You can enter 't' to generate an input template.");
                        return;
                    }

                    // If the column was an output column, add it to the end
                    inputFileLines[0] += $",{HeaderTexts[i]}";
                    columns[i] = nextCol;
                    nextCol++;
                }
            }

            // Parse each line into columns
            string[][] parsedLines = new string[inputFileLines.Length][];
            for (int i = 0; i < inputFileLines.Length; i++)
            {
                string[] parsedLine = ParseCSVLine(inputFileLines[i]);
                if (parsedLine.Length >= nextCol)
                {
                    parsedLines[i] = parsedLine;
                }
                else
                {
                    parsedLines[i] = new string[nextCol];
                    Array.Copy(parsedLine, parsedLines[i], parsedLine.Length);
                }
            }

            int totalShares = 0;
            double totalPurchaseCost = 0;
            double totalCurrentValue = 0;
            double totalDividend = 0;
            int totalLineExists = 0;

            // Perform calculations for each stock
            for (int i = 1; i < parsedLines.Length; i++)
            {
                // Clear the error message
                parsedLines[i][columns[Headers.Error.GetHashCode()]] = string.Empty;

                // Parse stock symbol
                string symbol = parsedLines[i][columns[Headers.Symbol.GetHashCode()]].Replace(" ", "").ToUpper();
                
                // Ignore blank lines
                if (symbol == string.Empty)
                {
                    continue;
                }

                // If this is the total line, remove it so that we overwrite it
                if (symbol == "TOTAL" && i == parsedLines.Length - 1)
                {
                    totalLineExists = 1;
                    continue;
                }

                // Parse purchase price
                double purchasePrice;
                if (!Double.TryParse(parsedLines[i][columns[Headers.PurchaseSharePrice.GetHashCode()]].Replace("$", "").Replace(",",""), out purchasePrice))
                {
                    parsedLines[i][columns[Headers.Error.GetHashCode()]] += $"Could not parse {HeaderTexts[Headers.PurchaseSharePrice.GetHashCode()]} as a double; ";
                    continue;
                }

                // Parse purchase date
                DateTime purchaseDate;
                if (!DateTime.TryParse(parsedLines[i][columns[Headers.PurchaseDate.GetHashCode()]], out purchaseDate))
                {
                    parsedLines[i][columns[Headers.Error.GetHashCode()]] += $"Could not parse {HeaderTexts[Headers.PurchaseDate.GetHashCode()]} as a date; ";
                    continue;
                }
                double yearsOwned = Math.Max((DateTime.Now - purchaseDate).TotalDays, 1) / 365;

                // Parse number of shares
                int shares;
                if (!int.TryParse(parsedLines[i][columns[Headers.Shares.GetHashCode()]], out shares))
                {
                    parsedLines[i][columns[Headers.Error.GetHashCode()]] += $"Could not parse {HeaderTexts[Headers.Shares.GetHashCode()]} as an integer; ";
                    continue;
                }

                // Look up price
                double curPrice = GetPrice(symbol);
                if (curPrice == 0)
                {
                    parsedLines[i][columns[Headers.Error.GetHashCode()]] += $"Could not find price information; ";
                    continue;
                }
                parsedLines[i][columns[Headers.CurrentSharePrice.GetHashCode()]] = string.Format("{0:C}", curPrice);

                // Look up dividend to date
                double dividends = GetDividend(symbol, purchaseDate);
                if (dividends == 0)
                {
                    parsedLines[i][columns[Headers.Error.GetHashCode()]] += $"Could not find dividend information; ";
                }
                parsedLines[i][columns[Headers.DividendsPerShare.GetHashCode()]] = string.Format("{0:C}", dividends);

                // Calculate remaining information
                double purchaseCost = Math.Max(purchasePrice * shares, 0.01);
                parsedLines[i][columns[Headers.PurchaseValue.GetHashCode()]] = string.Format("{0:C}", purchaseCost);
                parsedLines[i][columns[Headers.CurrentValue.GetHashCode()]] = string.Format("{0:C}", curPrice * shares);

                double capitalGain = (curPrice - purchasePrice) * shares;
                parsedLines[i][columns[Headers.CapitalGain.GetHashCode()]] = string.Format("{0:C}", capitalGain);
                parsedLines[i][columns[Headers.CapitalGainPercent.GetHashCode()]] = $"{Math.Round(capitalGain * 100 / purchaseCost, 3)}%";
                parsedLines[i][columns[Headers.AnnualCapitalGainPercent.GetHashCode()]] = $"{Math.Round(capitalGain * 100 / purchaseCost / yearsOwned, 3)}%";

                parsedLines[i][columns[Headers.TotalDividends.GetHashCode()]] = string.Format("{0:C}", dividends * shares);
                parsedLines[i][columns[Headers.DividendPercent.GetHashCode()]] = $"{Math.Round(dividends * shares * 100 / purchaseCost, 3)}%";
                parsedLines[i][columns[Headers.AnnualDividendPercent.GetHashCode()]] = $"{Math.Round(dividends * shares * 100 / purchaseCost / yearsOwned, 3)}%";

                double totalGain = capitalGain + (dividends * shares);
                parsedLines[i][columns[Headers.TotalGain.GetHashCode()]] = string.Format("{0:C}", totalGain);
                parsedLines[i][columns[Headers.TotalGainPercent.GetHashCode()]] = $"{Math.Round(totalGain * 100 / purchaseCost, 3)}%";
                parsedLines[i][columns[Headers.AnnualTotalGainPercent.GetHashCode()]] = $"{Math.Round(totalGain * 100 / purchaseCost / yearsOwned, 3)}%";

                // Update totals
                totalShares += shares;
                totalPurchaseCost += purchaseCost;
                totalCurrentValue += curPrice * shares;
                totalDividend += dividends * shares;
            }

            // Calculate aggregate information in final line
            string[] sumLine = new string[parsedLines[0].Length];
            for (int i = 0; i < sumLine.Length; i++)
            {
                sumLine[i] = string.Empty;
            }

            sumLine[columns[Headers.Symbol.GetHashCode()]] = "TOTAL";
            sumLine[columns[Headers.Shares.GetHashCode()]] = totalShares.ToString();
            sumLine[columns[Headers.PurchaseValue.GetHashCode()]] = string.Format("{0:C}", totalPurchaseCost);
            sumLine[columns[Headers.CurrentValue.GetHashCode()]] = string.Format("{0:C}", totalCurrentValue);
            sumLine[columns[Headers.CapitalGain.GetHashCode()]] = string.Format("{0:C}", totalCurrentValue - totalPurchaseCost);
            sumLine[columns[Headers.CapitalGainPercent.GetHashCode()]] = $"{Math.Round((totalCurrentValue - totalPurchaseCost) * 100 / totalPurchaseCost, 3)}%";
            sumLine[columns[Headers.TotalDividends.GetHashCode()]] = string.Format("{0:C}", totalDividend);
            sumLine[columns[Headers.DividendPercent.GetHashCode()]] = $"{Math.Round(totalDividend * 100 / totalPurchaseCost, 3)}%";
            sumLine[columns[Headers.TotalGain.GetHashCode()]] = string.Format("{0:C}", totalCurrentValue - totalPurchaseCost + totalDividend);
            sumLine[columns[Headers.TotalGainPercent.GetHashCode()]] = $"{Math.Round((totalCurrentValue - totalPurchaseCost + totalDividend) * 100 / totalPurchaseCost, 3)}%";

            // Encode parsedLines as comma seperated columns in outputFileLines
            string[] outputFileLines = new string[inputFileLines.Length + 1 - totalLineExists];
            for (int i = 0; i < outputFileLines.Length - 1; i++)
            {
                outputFileLines[i] = "";
                foreach (string col in parsedLines[i])
                {
                    if (col != null)
                    {
                        outputFileLines[i] += col.Contains(",") ? $"\"{col}\"," : $"{col},";
                    }
                    else
                    {
                        outputFileLines[i] += ",";
                    }
                }
                outputFileLines[i] = outputFileLines[i].Substring(0, outputFileLines[i].Length - 1); // remove trailing comma
            }

            // Encode sumLine
            int index = outputFileLines.Length - 1;
            outputFileLines[index] = "";
            foreach (string col in sumLine)
            {
                outputFileLines[index] += col.Contains(",") ? $"\"{col}\"," : $"{col},";
            }
            outputFileLines[index] = outputFileLines[index].Substring(0, outputFileLines[index].Length - 1); // remove trailing comma

            // Write lines to output file
            try
            {
                File.WriteAllLines(outputFile, outputFileLines);
                Console.WriteLine($"Output sucessfully saved to {outputFile}");
            }
            catch (IOException)
            {
                Console.WriteLine($"Could not write to {outputFile} beacuse it is in use by another application.  Please close the application using {outputFile} and try again.");
            }
        }

        /// <summary>
        /// Parse a line from a .csv file into an array of columns
        /// </summary>
        /// <param name="line">a line from a .csv file</param>
        /// <returns>an array in which entries correspond to columns in the .csv file</returns>
        static string[] ParseCSVLine(string line)
        {
            List<string> parsed = new List<string>();
            int index = 0;  // index of the first real character in a column
            int endIndex;   // index of the first closing character of a column (either a " or a ,)
            int increment;  // the amount index will be ahead of the previous endIndex
            while (index < line.Length)
            {
                // Quotation marks are used to enclose content which contains one or more commas
                if (line[index] == '"')
                {
                    index++;
                    endIndex = line.IndexOf('"', index);
                    increment = 2;
                }
                
                // Otherwise, a comma indicates the end of a column
                else
                {
                    endIndex = line.IndexOf(',', index);
                    increment = 1;
                }

                // Handle the last column case
                endIndex = endIndex < 0 ? line.Length : endIndex;

                parsed.Add(line.Substring(index, endIndex - index));
                index = endIndex + increment;
            }

            return parsed.ToArray();
        }

        /// <summary>
        /// Look up a stock's price using the TDAmeritrade API and add it to prices
        /// </summary>
        /// <param name="stockSymbol">symbol of the stock to look up</param>
        static void LookupPrice(string stockSymbol)
        {
            // Create a GET request
            long endDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string uri = $"https://api.tdameritrade.com/v1/marketdata/{stockSymbol}/pricehistory?apikey={ApiKey}%40AMER.OAUTHAP&periodType=day&period=5&frequencyType=minute&frequency=1&endDate={endDate}&needExtendedHoursData=false";
            HttpWebRequest request = WebRequest.CreateHttp(uri);

            // Recieve the json response
            string json;
            try
            {
                using (StreamReader sr = new StreamReader(request.GetResponse().GetResponseStream()))
                {
                    json = sr.ReadLine();
                }
            }
            catch
            {
                // If the API fails, use a price of 0
                prices.Add(stockSymbol, 0);
                return;
            }

            // Serialize the json into a PriceHistory object
            PriceHistory history;
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(new PriceHistory().GetType());
                history = ser.ReadObject(ms) as PriceHistory;
            }

            // Take the most recent close price or use 0 if no price history could be found
            double price = history.candles.Length != 0 ? history.candles[history.candles.Length - 1].close : 0;
            prices.Add(stockSymbol, price);
        }

        /// <summary>
        /// Get the current price of a stock
        /// </summary>
        /// <param name="stockSymbol">symbol of the stock</param>
        /// <returns>price of the stock in dollars</returns>
        static double GetPrice(string stockSymbol)
        {
            if (!prices.ContainsKey(stockSymbol))
            {
                LookupPrice(stockSymbol);
            }

            return prices[stockSymbol];
        }

        /// <summary>
        /// Look up a stock's dividend history by screen scraping from dividata.com and add it to dividends
        /// </summary>
        /// <param name="stockSymbol">symbol of the stock to look up</param>
        static void LookupDividend(string stockSymbol)
        {
            string url = $"https://dividata.com/stock/{stockSymbol}/dividend";
            string data = string.Empty;
            List<DividendPayment> payments = new List<DividendPayment>();

            // Download html from dividata.com
            try
            {
                using (WebClient client = new WebClient())
                {
                    data = client.DownloadString(url);
                }
            }
            catch (WebException)
            {
                // If no dividend information can be found, use a blank history
                dividends.Add(stockSymbol, payments.ToArray());
                return;
            }

            // Parse each dividend payments as a date and dollar amount
            // Dividned information is stored in rows formatted as follows: 
            // <tr><td class="date">Aug 15, 2018</td><td class="money">$0.420</td></tr>
            int curIndex = data.IndexOf("date", data.IndexOf("Dividend Amount")) + 6;
            int endIndex = data.IndexOf("</thead>", curIndex);
            while (curIndex < endIndex && curIndex > 5)
            {
                int length = data.IndexOf("<", curIndex) - curIndex;
                DateTime exDate = DateTime.Parse(data.Substring(curIndex, length));

                curIndex = data.IndexOf("money", curIndex) + 8;
                length = data.IndexOf("<", curIndex) - curIndex;
                double amount = Double.Parse(data.Substring(curIndex, length));
                payments.Add(new DividendPayment(amount, exDate));

                curIndex = data.IndexOf("date", curIndex) + 6;
            }

            dividends.Add(stockSymbol, payments.ToArray());
        }

        /// <summary>
        /// Calculate the total dividends per share for a stock purchased at a given date
        /// </summary>
        /// <param name="stockSymbol">symbol of the stock</param>
        /// <param name="purchaseDate">date at which the stock was purchased</param>
        /// <returns>total dividends per share since the purchase date in dollars</returns>
        static double GetDividend(string stockSymbol, DateTime purchaseDate)
        {
            // Ensure that we have dividend history for the stockSymbol
            if (!dividends.ContainsKey(stockSymbol))
            {
                LookupDividend(stockSymbol);
            }

            DividendPayment[] payments = dividends[stockSymbol];

            // If missing or insufficient data, return 0 
            // We require at least one payment entry before the purchase date to ensure that the payment history goes back far enough
            if (payments.Length == 0 || payments[payments.Length - 1].ExDate > purchaseDate)
            {
                return 0;
            }

            double dividend = 0;
            int i = 0;

            // Increment i to be the first payment in the past
            while (payments[i].ExDate > DateTime.Now)
            {
                i++;
            }
            
            // Count up all remaining payments between the present and the purchaseDate
            for (; payments[i].ExDate > purchaseDate; i++)
            {
                dividend += payments[i].Amount; 
            }

            return dividend;
        }
    }
}
