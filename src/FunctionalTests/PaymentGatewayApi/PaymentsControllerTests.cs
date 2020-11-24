using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bank.Client.Payment;
using Core.Responses;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;

namespace FunctionalTests.PaymentGatewayApi
{
    [TestFixture]
    public class PaymentsControllerTests : TestBase
    {
        private const string ApiPayments = "/api/payments/";

        private Task<HttpResponseMessage> ProcessPayment(dynamic payment)
        {
            return PostJson(ApiPayments, payment);
        }

        private Task<HttpResponseMessage> GetPayment(Guid id)
        {
            return Get($"{ApiPayments}{id}");
        }

        public static Payment BuildPayment()
        {
            return new Payment
            {
                CardNumber = "1234-1234-1234-1234",
                Amount = 1000m,
                Currency = "GBP",
                Cvv = "123",
                ExpiryMonth = "Dec",
                ExpiryYear = "2022"
            };
        }

        [Test]
        public async Task Get_ShouldReturnNotFound_WhenPaymentDetailsDoesNotExists()
        {
            var response = await GetPayment(Guid.NewGuid());

            response.IsSuccessStatusCode.ShouldBeFalse();
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Test, Order(2)]
        public async Task Get_ShouldReturnOkWithPayment_WhenPaymentDetailsExists()
        {
            var addPaymentResponse = await ProcessPayment(BuildJsonApiPayload(BuildPayment()));

            var content = await addPaymentResponse.Content.ReadAsStringAsync();

            var payment = JsonConvert.DeserializeObject<PaymentProcessResponse>(content);

            var response = await GetPayment(payment.PaymentId);

            response.IsSuccessStatusCode.ShouldBeTrue();
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Test, Order(1)]
        public async Task Put_ShouldReturnNoContent_AndUpdateAmounts()
        {
            var response = await ProcessPayment(BuildJsonApiPayload(BuildPayment()));

            response.IsSuccessStatusCode.ShouldBeTrue();
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}