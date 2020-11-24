using System;

namespace Core.Responses
{
    public class PaymentProcessResponse
    {
        public Guid PaymentId { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
    }

    public class PaymentProcessErrorResponse : PaymentProcessResponse
    {
        public string Reason { get; set; }
    }
}