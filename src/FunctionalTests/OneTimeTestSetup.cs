using System.Linq;
using System.Net.Http;
using Bank.ApiStub;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PaymentGateway;
using WireMock.Server;
using WireMock.Settings;

namespace FunctionalTests
{
    [SetUpFixture]
    public static class OneTimeTestSetup
    {
        public static HttpClient Client;
        public static ApiWebApplicationFactory<Startup> Factory;
        private static IWireMockService wireMockService;
        private static ServiceProvider serviceProvider;
        public static WireMockServer ApiStubClient;

        public static string Url => serviceProvider.GetRequiredService<IWireMockServerSettings>().Urls.Single();

        [OneTimeSetUp]
        public static void GivenARequestToTheController()
        {
            Factory = new ApiWebApplicationFactory<Startup>();
            Client = Factory.CreateClient();

            serviceProvider = new ServiceCollection()
                .AddApiStub(Startup.Configuration)
                .BuildServiceProvider();

            wireMockService = serviceProvider.GetService<IWireMockService>();
            ApiStubClient = wireMockService.Start();
        }

        [OneTimeTearDown]
        public static void TearDown()
        {
            Client?.Dispose();
            Factory?.Dispose();
            serviceProvider?.Dispose();
        }
    }
}