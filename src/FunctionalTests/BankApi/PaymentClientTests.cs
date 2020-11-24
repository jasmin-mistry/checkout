using System.Linq;
using System.Threading.Tasks;
using Bank.Client;
using Bank.Client.Payment;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace FunctionalTests.BankApi
{
    [TestFixture]
    public class PaymentClientTests : TestBase
    {
        private IPaymentClient paymentClient;

        [SetUp]
        public void BeforeEveryTest()
        {
            var serviceProvider = GetServiceProvider();
            paymentClient = serviceProvider.GetService<IPaymentClient>();
        }

        [Test]
        public async Task Process_ShouldReturn200StatusCode_WhenPaymentIsSuccessfullyProcessed()
        {
            await StubAdminClient.ResetRequestsAsync();

            var response = await paymentClient.Process(new Payment
            {
                CardNumber = "1234-1234-1234-1234",
                Amount = 1000m,
                Currency = "GBP",
                Cvv = "123",
                ExpiryMonth = "Dec",
                ExpiryYear = "2022"
            });

            var result = (await response.EnsureSuccessStatusCodeAsync()).Content;
            var requestsMade = await StubAdminClient.GetRequestsAsync();

            result.ShouldNotBeNull();
            result.TransactionId.ShouldNotBeNull();
            result.Status.ShouldNotBeNull();
            result.Reason.ShouldBeNull();

            requestsMade
                .Count(x => x.Request.AbsolutePath == "/payment" && x.Response.StatusCode.Equals(200L))
                .ShouldBe(1);
        }

        [Test]
        public async Task Process_ShouldReturn400StatusCode_WhenPaymentProcessHasFailed()
        {
            await StubAdminClient.ResetRequestsAsync();

            var response = await paymentClient.Process(new Payment
            {
                CardNumber = "9999-9999-9999-9999",
                Amount = 1000m,
                Currency = "GBP",
                Cvv = "123",
                ExpiryMonth = "Dec",
                ExpiryYear = "2022"
            }).ShouldThrowAsync<BankApiHttpException>();
        }
    }
}