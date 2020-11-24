namespace Core.Responses
{
    public class PaymentResponse
    {
        public string CardNumber { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }

        public string Status { get; set; }
    }
}