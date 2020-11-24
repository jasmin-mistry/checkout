using System;
using Bank.Client.Auth;
using Bank.Client.Payment;

namespace Bank.Client
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class BankClient : IBankClient
    {
        public BankClient(IAuthClient auth, IPaymentClient payment)
        {
            Auth = auth ?? throw new ArgumentNullException(nameof(auth));
            Payment = payment ?? throw new ArgumentNullException(nameof(payment));
        }

        public IAuthClient Auth { get; }
        public IPaymentClient Payment { get; }
    }
}