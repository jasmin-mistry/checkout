using SharedKernel;

namespace Core.Entities
{
    public class Payment : BaseEntity
    {
        public string CardNumber { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Cvv { get; set; }

        public string TransactionId { get; set; }
        public string TransactionStatus { get; set; }
        public string Reason { get; set; }
    }
}