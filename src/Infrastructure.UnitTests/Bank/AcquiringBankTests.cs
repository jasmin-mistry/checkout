using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bank.Client;
using Bank.Client.Payment;
using Core.Responses;
using Infrastructure.Bank;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Refit;
using Shouldly;
using CoreEntities = Core.Entities;
using PaymentResponse = Bank.Client.Payment.PaymentResponse;

namespace Infrastructure.UnitTests.Bank
{
    [TestFixture]
    public class AcquiringBankTests
    {
        private Mock<IBankClient> mockBankClient;

        private AcquiringBank CreateAcquiringBank()
        {
            return new AcquiringBank(mockBankClient.Object);
        }

        [SetUp]
        public void SetUp()
        {
            mockBankClient = new Mock<IBankClient>();
        }

        [Test]
        public async Task ProcessPayment_StateUnderTest_ExpectedBehavior()
        {
            var acquiringBank = CreateAcquiringBank();
            var expectedResult = new PaymentResponse {TransactionId = new Guid().ToString(), Status = "Success"};

            mockBankClient.Setup(x => x.Payment.Process(It.IsAny<Payment>()))
                .ReturnsAsync(new ApiResponse<PaymentResponse>(new HttpResponseMessage(HttpStatusCode.OK),
                    expectedResult));

            var payment = new CoreEntities.Payment
            {
                CardNumber = "1234-5678-9012-3456",
                Amount = 100.12m,
                Currency = "GBP",
                Cvv = "123",
                ExpiryMonth = "Dec",
                ExpiryYear = "2030"
            };

            var response = await acquiringBank.ProcessPayment(payment);

            response.ShouldNotBeNull();
            response.TransactionId.ShouldBeSameAs(expectedResult.TransactionId);
            response.Status.ShouldBeSameAs(expectedResult.Status);

            mockBankClient.Verify(x => x.Payment.Process(It.IsAny<Payment>()), Times.AtLeastOnce);
        }

        [Test]
        public async Task ProcessPayment_StateUnderTest_ExpectedBehavior1()
        {
            var acquiringBank = CreateAcquiringBank();
            const string reason = "Something went wrong";
            const string status = "Failed";
            const string id = "99999999999";

            var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new PaymentResponse
                    {TransactionId = id, Status = status, Reason = reason}))
            };
            var exception =
                new BankApiHttpException(new HttpRequestMessage(HttpMethod.Post, new Uri("payment", UriKind.Relative)),
                    errorResponse);

            mockBankClient.Setup(x => x.Payment.Process(It.IsAny<Payment>()))
                .ThrowsAsync(exception);

            var response = await acquiringBank.ProcessPayment(new CoreEntities.Payment());

            response.ShouldNotBeNull();
            var result = response.ShouldBeOfType<PaymentProcessErrorResponse>();
            result.TransactionId.ShouldBe(id);
            result.Status.ShouldBe(status);
            result.Reason.ShouldBe(reason);

            mockBankClient.Verify(x => x.Payment.Process(It.IsAny<Payment>()), Times.AtLeastOnce);
        }
    }
}