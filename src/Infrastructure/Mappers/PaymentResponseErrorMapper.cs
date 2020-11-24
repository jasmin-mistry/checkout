using SharedKernel.Interfaces;
using BankApi = Bank.Client.Payment;
using CoreResponses = Core.Responses;

namespace Infrastructure.Mappers
{
    public class
        PaymentErrorResponseMapper : IMapper<BankApi.PaymentResponse, CoreResponses.PaymentProcessErrorResponse>
    {
        public CoreResponses.PaymentProcessErrorResponse Map(BankApi.PaymentResponse source)
        {
            if (source != null)
                return new CoreResponses.PaymentProcessErrorResponse
                {
                    TransactionId = source.TransactionId,
                    Status = source.Status,
                    Reason = source.Reason
                };
            return new CoreResponses.PaymentProcessErrorResponse
            {
                Reason = "Unexpected error in the payment process"
            };
        }
    }
}