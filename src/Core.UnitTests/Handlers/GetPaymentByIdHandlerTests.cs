using System;
using System.Threading.Tasks;
using Core.Entities;
using Core.Handlers;
using Core.Queries;
using Moq;
using NUnit.Framework;
using SharedKernel.Interfaces;
using Shouldly;

namespace Core.UnitTests.Handlers
{
    [TestFixture]
    public class GetPaymentByIdHandlerTests
    {
        private Mock<IRepository> mockEfRepository;

        private GetPaymentByIdHandler CreateGetPaymentByIdHandler()
        {
            return new GetPaymentByIdHandler(mockEfRepository.Object);
        }

        [SetUp]
        public void SetUp()
        {
            mockEfRepository = new Mock<IRepository>();
        }


        [Test]
        public async Task Handle_ShouldReturnNull_WhenPaymentDoesNotExists()
        {
            var getPaymentByIdHandler = CreateGetPaymentByIdHandler();
            mockEfRepository.Setup(x => x.GetByIdAsync<Payment>(It.IsAny<Guid>()))
                .ReturnsAsync(default(Payment));

            var request = new GetPaymentByIdQuery(Guid.NewGuid());

            var response = await getPaymentByIdHandler.Handle(request, default);

            response.ShouldBeNull();

            mockEfRepository.Verify(x => x.GetByIdAsync<Payment>(It.IsAny<Guid>()), Times.AtLeastOnce);
        }

        [Test]
        public async Task Handle_ShouldReturnPayment_WhenIdExistsForAPayment()
        {
            var paymentId = Guid.NewGuid();
            var getPaymentByIdHandler = CreateGetPaymentByIdHandler();
            mockEfRepository.Setup(x => x.GetByIdAsync<Payment>(It.IsAny<Guid>()))
                .ReturnsAsync(new Payment
                {
                    Id = paymentId, CardNumber = "1234-2345-3456-4567", TransactionStatus = "Completed"
                });

            var request = new GetPaymentByIdQuery(paymentId);

            var response = await getPaymentByIdHandler.Handle(request, default);

            response.ShouldNotBeNull();

            mockEfRepository.Verify(x => x.GetByIdAsync<Payment>(It.IsAny<Guid>()), Times.AtLeastOnce);
        }
    }
}