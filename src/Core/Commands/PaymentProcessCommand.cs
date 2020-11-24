using Core.Responses;
using MediatR;

namespace Core.Commands
{
    public class PaymentProcessCommand : IRequest<PaymentProcessResponse>
    {
        public string CardNumber { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Cvv { get; set; }
    }
}