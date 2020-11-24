using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bank.Client;
using Bank.Client.Payment;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Shouldly;

namespace PaymentGateway.UnitTests
{
    [TestFixture]
    public class BankApiHealthCheckTests
    {
        private Mock<ILogger<BankApiHealthCheck>> loggerMock;
        private Mock<IBankClient> clientMock;
        private BankApiHealthCheck sut;

        [SetUp]
        public void BeforeEachTest()
        {
            loggerMock = new Mock<ILogger<BankApiHealthCheck>>();
            clientMock = new Mock<IBankClient>();
            sut = new BankApiHealthCheck(clientMock.Object, loggerMock.Object);
        }

        [Test]
        public async Task CheckHealthAsync_ShouldReturnStatusAsHealthy_WhenBankApiEndPointIsReturnExpectedResponse()
        {
            var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new PaymentResponse
                    {TransactionId = "999999", Status = "Failed", Reason = "Something went wrong"}))
            };
            var exception =
                new BankApiHttpException(new HttpRequestMessage(HttpMethod.Post, new Uri("payment", UriKind.Relative)),
                    errorResponse);

            clientMock.Setup(m => m.Payment.Process(It.IsAny<Payment>())).ThrowsAsync(exception);

            var result = await sut.CheckHealthAsync(It.IsAny<HealthCheckContext>(), It.IsAny<CancellationToken>());

            clientMock.Verify(m => m.Payment.Process(It.IsAny<Payment>()), Times.Once);

            result.ShouldBeAssignableTo<HealthCheckResult>();
            result.Status.ShouldBe(HealthStatus.Healthy);
        }

        [Test]
        public async Task CheckHealthAsync_ShouldReturnStatusAsUnhealthy_WhenBankApiEndPointIsReturningError()
        {
            var errorResponse = new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
            var exception =
                new BankApiHttpException(new HttpRequestMessage(HttpMethod.Post, new Uri("payment", UriKind.Relative)),
                    errorResponse);
            clientMock.Setup(m => m.Payment.Process(It.IsAny<Payment>()))
                .ThrowsAsync(exception);

            var result = await sut.CheckHealthAsync(It.IsAny<HealthCheckContext>(), It.IsAny<CancellationToken>());

            clientMock.Verify(m => m.Payment.Process(It.IsAny<Payment>()), Times.Once());

            result.ShouldBeAssignableTo<HealthCheckResult>();
            result.Status.ShouldBe(HealthStatus.Unhealthy);
        }
    }
}