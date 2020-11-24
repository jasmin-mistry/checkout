using Core.Entities;
using Core.Responses;
using SharedKernel;
using SharedKernel.Interfaces;

namespace Core.Mappers
{
    public class PaymentResponseMapper : IMapper<Payment, PaymentResponse>
    {
        public PaymentResponse Map(Payment payment)
        {
            if (payment != null)
                return new PaymentResponse
                {
                    CardNumber = payment.CardNumber.MaskCardNumber('x'),
                    ExpiryMonth = payment.ExpiryMonth,
                    ExpiryYear = payment.ExpiryYear,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = payment.TransactionStatus
                };

            return null;
        }
    }
}