using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using NUnit.Framework;
using PaymentGateway.UnitTests.Helper;
using Shouldly;

namespace IntegrationTests.Data
{
    [TestFixture]
    public class EfRepositoryAddTests : EfRepositoryTestBase
    {
        [Test]
        public async Task AddsItemAndSetsId()
        {
            var repository = GetRepository();
            var item = PaymentTestDataHelper.GetPayment(1, 123, 1000);

            await repository.AddAsync(item);

            var newItem = (await repository.ListAsync<Payment>()).FirstOrDefault();

            newItem.ShouldNotBeNull();
            newItem.ShouldBeSameAs(item);
            newItem.Id.ToString().ShouldNotBeNull();
        }
    }
}