using System.Threading.Tasks;
using Core.Entities;
using Core.Responses;

namespace Core.Bank
{
    public interface IAcquiringBank
    {
        Task<PaymentProcessResponse> ProcessPayment(Payment payment);
    }
}