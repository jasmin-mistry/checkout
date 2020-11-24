using System;
using Core.Bank;
using Infrastructure.Bank;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using SharedKernel.Interfaces;

// note: this is recommended practise to place registrations in this MS namespace. 
// See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection#registering-your-own-services
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddInfrastructureServices(this IServiceCollection serviceCollection,
            IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            serviceCollection.AddTransient<IRepository, EfRepository>();
            serviceCollection.AddTransient<IAcquiringBank, AcquiringBank>();
        }
    }
}