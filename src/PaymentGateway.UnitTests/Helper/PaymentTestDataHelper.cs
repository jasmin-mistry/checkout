using System;
using System.Collections.Generic;
using System.Linq;
using Core.Entities;

namespace PaymentGateway.UnitTests.Helper
{
    public static class PaymentTestDataHelper
    {
        public static Payment GetPayment(int cardNumber, int cvv, decimal amount, string currency = "GBP",
            string expiryMonth = "Dec", int expiryYear = 2020, bool invalidCard = false)
        {
            var cardPrefix = !invalidCard ? "1234-1234-1234-" : "9999-9999-9999-";
            return new Payment
            {
                CardNumber = $"{cardPrefix}{cardNumber.ToString().PadLeft(4, '0')}",
                Amount = amount,
                Currency = currency,
                Cvv = cvv.ToString(),
                ExpiryMonth = expiryMonth,
                ExpiryYear = expiryYear.ToString()
            };
        }

        public static IEnumerable<Payment> GetPayments()
        {
            return Enumerable.Range(0, 30)
                .Select(x => GetPayment(x, new Random().Next(100, 999), new Random().Next(1000, 9999)));
        }
    }
}