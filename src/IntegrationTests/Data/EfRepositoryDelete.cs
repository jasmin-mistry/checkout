using System.Threading.Tasks;
using Core.Entities;
using NUnit.Framework;
using PaymentGateway.UnitTests.Helper;
using Shouldly;

namespace IntegrationTests.Data
{
    [TestFixture]
    public class EfRepositoryDelete : EfRepositoryTestBase
    {
        [Test]
        public async Task DeletesItemAfterAddingIt()
        {
            var repository = GetRepository();
            var item = PaymentTestDataHelper.GetPayment(1, 123, 1000);
            await repository.AddAsync(item);
            var paymentId = item.Id;

            await repository.DeleteAsync(item);

            var payments = await repository.ListAsync<Payment>();
            payments.ShouldNotContain(x => x.Id == paymentId);
        }
    }
}