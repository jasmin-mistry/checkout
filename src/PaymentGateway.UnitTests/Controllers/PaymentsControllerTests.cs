using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using Core.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using PaymentGateway.Controllers;
using Shouldly;

namespace PaymentGateway.UnitTests.Controllers
{
    [TestFixture]
    public class PaymentsControllerTests
    {
        private MockRepository mockRepository;
        private Mock<IMediator> mockMediator;

        private PaymentsController CreatePaymentsController()
        {
            return new PaymentsController(mockMediator.Object);
        }

        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);

            mockMediator = mockRepository.Create<IMediator>();
        }

        [TearDown]
        public void TearDown()
        {
            mockRepository.VerifyAll();
        }

        [Test]
        public async Task Post_ShouldReturnSuccessfulPaymentResponse_WhenPaymentIsProcessed()
        {
            var expectedResponse = new PaymentProcessResponse
            {
                TransactionId = new Guid().ToString(),
                Status = "Success"
            };
            var paymentsController = CreatePaymentsController();

            mockMediator.Setup(x => x.Send(It.IsAny<PaymentProcessCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var response = await paymentsController.Post(new PaymentProcessCommand());

            response.ShouldNotBeNull();
            var result = response.ShouldBeOfType<OkObjectResult>();
            var paymentResponse = result.Value.ShouldBeOfType<PaymentProcessResponse>();
            paymentResponse.ShouldBeSameAs(expectedResponse);
        }
    }
}