using Bank.ApiStub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WireMock.Settings;

// note: this is recommended practise to place registrations in this MS namespace. 
// See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection#registering-your-own-services
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiStub(this IServiceCollection serviceCollection,
            IConfiguration configuration)
        {
            var factory = new LoggerFactory();

            serviceCollection.AddLogging(configure => configure
                .AddConsole()
                .AddDebug()
                .AddAzureWebAppDiagnostics());

            serviceCollection.AddSingleton(factory.CreateLogger("WireMock.Net Logger"));

            var settings = configuration.GetSection("WireMockServerSettings").Get<WireMockServerSettings>();
            serviceCollection.AddSingleton<IWireMockServerSettings>(settings);

            var proxyAndRecordSettings =
                configuration.GetSection("ProxyAndRecordSettings").Get<ProxyAndRecordSettings>();
            if (proxyAndRecordSettings != null)
                serviceCollection.AddSingleton<IProxyAndRecordSettings>(proxyAndRecordSettings);

            serviceCollection.AddTransient<IWireMockService, WireMockService>();
            serviceCollection.AddTransient<App>();
            return serviceCollection;
        }
    }
}