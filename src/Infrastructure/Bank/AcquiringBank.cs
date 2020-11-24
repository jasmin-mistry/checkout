using System.Threading.Tasks;
using Bank.Client;
using Core.Bank;
using Core.Entities;
using Core.Responses;
using Infrastructure.Mappers;
using Newtonsoft.Json;
using PaymentResponse = Bank.Client.Payment.PaymentResponse;

namespace Infrastructure.Bank
{
    public class AcquiringBank : IAcquiringBank
    {
        private readonly IBankClient client;

        public AcquiringBank(IBankClient client)
        {
            this.client = client;
        }

        public async Task<PaymentProcessResponse> ProcessPayment(Payment payment)
        {
            try
            {
                var inputData = new PaymentMapper().Map(payment);

                var response = await client.Payment.Process(inputData);

                return new PaymentResponseMapper().Map(response.Content);
            }
            catch (BankApiHttpException ex)
            {
                var responseContent = await ex.Response.Content.ReadAsStringAsync();
                return new PaymentErrorResponseMapper().Map(
                    JsonConvert.DeserializeObject<PaymentResponse>(responseContent));
            }
        }
    }
}