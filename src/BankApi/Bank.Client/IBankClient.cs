using Bank.Client.Auth;
using Bank.Client.Payment;

namespace Bank.Client
{
    public interface IBankClient
    {
        IAuthClient Auth { get; }
        IPaymentClient Payment { get; }
    }
}