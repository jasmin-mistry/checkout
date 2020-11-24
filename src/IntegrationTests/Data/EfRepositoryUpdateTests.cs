using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PaymentGateway.UnitTests.Helper;
using Shouldly;

namespace IntegrationTests.Data
{
    [TestFixture]
    public class EfRepositoryUpdateTests : EfRepositoryTestBase
    {
        [Test]
        public async Task UpdatesItemAfterAddingIt()
        {
            var repository = GetRepository();
            const string transactionStatus = "Running";
            var item = PaymentTestDataHelper.GetPayment(1, 123, 1000);
            item.TransactionStatus = transactionStatus;

            await repository.AddAsync(item);

            DbContext.Entry(item).State = EntityState.Detached;

            var newItem = (await repository.ListAsync<Payment>())
                .FirstOrDefault(x => x.TransactionStatus == transactionStatus);

            newItem.ShouldNotBeNull();
            newItem.ShouldNotBeSameAs(item);

            const string newTransactionStatus = "Completed";
            newItem.TransactionStatus = newTransactionStatus;

            await repository.UpdateAsync(newItem);

            var updatedItem = (await repository.ListAsync<Payment>())
                .FirstOrDefault(i => i.TransactionStatus == newTransactionStatus);

            updatedItem.ShouldNotBeNull();
            updatedItem.TransactionStatus.ShouldNotBeSameAs(item.TransactionStatus);
            updatedItem.Id.ShouldBe(newItem.Id);
        }
    }
}