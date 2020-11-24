using System;
using System.Threading.Tasks;
using Core.Bank;
using Core.Commands;
using Core.Entities;
using Core.Handlers;
using Core.Responses;
using Moq;
using NUnit.Framework;
using SharedKernel.Interfaces;
using Shouldly;

namespace Core.UnitTests.Handlers
{
    [TestFixture]
    public class PaymentProcessHandlerTests
    {
        private MockRepository mockRepository;

        private Mock<IAcquiringBank> mockBank;
        private Mock<IRepository> mockEfRepository;

        private PaymentProcessHandler CreatePaymentProcessHandler()
        {
            return new PaymentProcessHandler(mockBank.Object, mockEfRepository.Object);
        }

        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);

            mockBank = mockRepository.Create<IAcquiringBank>();
            mockEfRepository = mockRepository.Create<IRepository>();
        }

        [TearDown]
        public void TearDown()
        {
            mockRepository.VerifyAll();
        }

        [Test]
        public async Task Handle_ShouldSavePayment_WhenPaymentProcessCompleted()
        {
            var request = new PaymentProcessCommand();
            var paymentProcessHandler = CreatePaymentProcessHandler();

            var expectedResponse = new PaymentProcessResponse {TransactionId = "123456789", Status = "Completed"};
            mockBank.Setup(x => x.ProcessPayment(It.IsAny<Payment>()))
                .ReturnsAsync(expectedResponse);
            mockEfRepository.Setup(x => x.AddAsync(It.IsAny<Payment>())).ReturnsAsync(new Payment
            {
                Id = Guid.NewGuid(),
                CardNumber = "1234-1234-1234-4568",
                TransactionId = expectedResponse.TransactionId,
                TransactionStatus = expectedResponse.Status
            });

            var response = await paymentProcessHandler.Handle(request, default);

            response.ShouldNotBeNull();
            response.ShouldBeOfType<PaymentProcessResponse>();
            mockBank.Verify(x => x.ProcessPayment(It.IsAny<Payment>()), Times.AtLeastOnce());
            mockEfRepository.Verify(x => x.AddAsync(It.IsAny<Payment>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task Handle_ShouldSavePaymentWithAReason_WhenPaymentProcessFailed()
        {
            var request = new PaymentProcessCommand();
            var paymentProcessHandler = CreatePaymentProcessHandler();

            const string status = "Failed";
            var expectedResponse = new PaymentProcessErrorResponse
                {TransactionId = "123456789", Status = status, Reason = "Something went wrong"};

            mockBank.Setup(x => x.ProcessPayment(It.IsAny<Payment>()))
                .ReturnsAsync(expectedResponse);
            mockEfRepository.Setup(x => x.AddAsync(It.IsAny<Payment>())).ReturnsAsync(new Payment
            {
                Id = Guid.NewGuid(),
                CardNumber = "1234-1234-1234-4568",
                TransactionId = expectedResponse.TransactionId,
                TransactionStatus = expectedResponse.Status,
                Reason = expectedResponse.Reason
            });

            var response = await paymentProcessHandler.Handle(request, default);

            response.ShouldNotBeNull();
            var result = response.ShouldBeOfType<PaymentProcessResponse>();
            result.Status.ShouldBeSameAs(status);
            mockBank.Verify(x => x.ProcessPayment(It.IsAny<Payment>()), Times.AtLeastOnce());
            mockEfRepository.Verify(x => x.AddAsync(It.Is<Payment>(p => !string.IsNullOrWhiteSpace(p.Reason))),
                Times.AtLeastOnce);
        }
    }
}