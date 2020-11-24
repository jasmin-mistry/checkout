using SharedKernel.Interfaces;
using BankApi = Bank.Client.Payment;
using CoreEntities = Core.Entities;

namespace Infrastructure.Mappers
{
    public class PaymentMapper : IMapper<CoreEntities.Payment, BankApi.Payment>
    {
        public BankApi.Payment Map(CoreEntities.Payment source)
        {
            return new BankApi.Payment
            {
                CardNumber = source.CardNumber,
                ExpiryMonth = source.ExpiryMonth,
                ExpiryYear = source.ExpiryYear,
                Amount = source.Amount,
                Currency = source.Currency,
                Cvv = source.Cvv
            };
        }
    }
}