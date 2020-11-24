using Core.Entities;
using Core.Responses;
using SharedKernel.Interfaces;

namespace Core.Mappers
{
    public class PaymentProcessResponseMapper : IMapper<Payment, PaymentProcessResponse>
    {
        public PaymentProcessResponse Map(Payment payment)
        {
            if (payment != null)
                return new PaymentProcessResponse
                {
                    PaymentId = payment.Id,
                    TransactionId = payment.TransactionId,
                    Status = payment.TransactionStatus
                };

            return null;
        }
    }
}