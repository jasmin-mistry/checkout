using SharedKernel.Interfaces;
using BankApi = Bank.Client.Payment;
using CoreResponses = Core.Responses;

namespace Infrastructure.Mappers
{
    public class PaymentResponseMapper : IMapper<BankApi.PaymentResponse, CoreResponses.PaymentProcessResponse>
    {
        public CoreResponses.PaymentProcessResponse Map(BankApi.PaymentResponse source)
        {
            return new CoreResponses.PaymentProcessResponse
            {
                TransactionId = source.TransactionId,
                Status = source.Status
            };
        }
    }
}