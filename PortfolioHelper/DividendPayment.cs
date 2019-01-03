using System;

namespace PortfolioHelper
{
    /// <summary>
    /// Encodes the date and amount for a dividend payment
    /// </summary>
    public struct DividendPayment
    {
        /// <summary>
        /// Payment amount dollars
        /// </summary>
        public double Amount { get; }

        /// <summary>
        /// Ex-dividend date for payment
        /// </summary>
        public DateTime ExDate { get; }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="amount">payment amount in cents per share</param>
        /// <param name="exDate">ex-dividend date for payment</param>
        public DividendPayment(double amount, DateTime exDate)
        {
            this.Amount = amount;
            this.ExDate = exDate;
        }
    }
}
