using System;
using System.Collections.Generic;
using System.Linq;
using Core.Entities;
using Infrastructure.Data;

namespace FunctionalTests
{
    public static class TestDataHelper
    {
        public const string CardNumberPrefix = "1234-1234-1234-";

        public static Payment GetPayment(int cardNumber, int cvv, decimal amount, string currency = "GBP",
            string expiryMonth = "Dec", int expiryYear = 2020)
        {
            return new Payment
            {
                CardNumber = $"{CardNumberPrefix}{cardNumber.ToString().PadLeft(4, '0')}",
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

        public static void PopulateTestData(AppDbContext dbContext)
        {
            foreach (var item in dbContext.Payments)
            {
                dbContext.Remove(item);
            }

            dbContext.SaveChanges();

            dbContext.Payments.AddRange(GetPayments());

            dbContext.SaveChanges();
        }
    }
}