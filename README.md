# PortfolioHelper
Written by Matthew Calligaro, January 2019

## Summary
PortfolioHelper tracks a stock portfolio by looking up the current price and dividend history of a stock and using this to calculate statistics such as the capital gain, total dividends to date, annual rate of return, etc.

## Input File Format
The input file must be a .csv with the following four columns (each with the exact column header as listed):
* Stock Symbol: The symbol of the stock, such as MSFT for Microsoft
* Purchase Date: The date on which the stock was purchased, such as 1/1/2018
* Purchase Share Price: The price per share at which the stock was purchased, such as $100.00.
* Shares: The number of shares of the stock purchased, such as 10

These columns can appear in any order, and the input file can contain any number of additional columns.

## Command Format
Commands should be of the form `<inputFilePath> <outputFilePath>`\
In place of `<outputFilePath>`, you can use `o` to overwrite the input file or `d` to use the default filename.\
If `<inputFilePath>` and `<outputFilePath>` are equal, the input file will be overwritten.\
Example Command: `stocks.csv stocks_edited.csv`

## Additional Commands
`quit`: Closes the application
`?`: Prints this help text
`template`: Creates a template input file named template.csv in the current directory
