using System.Threading.Tasks;
using Refit;

namespace Bank.Client.Payment
{
    [Headers(
        InternalConstants.HttpHeaderAuthorization + ": " + InternalConstants.HttpHeaderAuthType,
        InternalConstants.HttpHeaderContentType + ": " + InternalConstants.HttpHeaderContentTypeValue)]
    public interface IPaymentClient
    {
        [Post("/payment")]
        Task<ApiResponse<PaymentResponse>> Process([Body] Payment payment);
    }
}