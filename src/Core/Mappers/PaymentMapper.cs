using Core.Commands;
using Core.Entities;
using SharedKernel.Interfaces;

namespace Core.Mappers
{
    public class PaymentMapper : IMapper<PaymentProcessCommand, Payment>
    {
        public Payment Map(PaymentProcessCommand requestData)
        {
            return new Payment
            {
                CardNumber = requestData.CardNumber,
                ExpiryMonth = requestData.ExpiryMonth,
                ExpiryYear = requestData.ExpiryYear,
                Amount = requestData.Amount,
                Currency = requestData.Currency,
                Cvv = requestData.Cvv
            };
        }
    }
}