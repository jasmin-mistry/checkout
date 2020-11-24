using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Bank.ApiStub;
using Bank.Client;
using Bank.Client.Auth;
using Bank.Client.Payment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Refit;
using RestEase;
using WireMock.Client;
using WireMock.Server;

namespace FunctionalTests
{
    public class TestBase
    {
        private const string ApplicationJson = "application/json";
        public static WireMockServer ApiStubClient;
        public static Uri BaseUri;
        public static IWireMockAdminApi StubAdminClient;

        static TestBase()
        {
            StubAdminClient = RestClient.For<IWireMockAdminApi>(OneTimeTestSetup.Url);
            BaseUri = new Uri("http://localhost");
            ApiStubClient = OneTimeTestSetup.ApiStubClient;
            Client = OneTimeTestSetup.Client;
        }

        public static HttpClient Client { get; set; }

        protected async Task<HttpResponseMessage> Get(string url)
        {
            return await OneTimeTestSetup.Client.GetAsync(url);
        }

        protected async Task<string> GetJson(string url)
        {
            var response = await Client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            return body;
        }

        protected async Task<T> GetJsonAs<T>(string url)
        {
            var body = await GetJson(url);
            var result = body.DeserializeFromJson<T>();
            return result;
        }

        protected async Task<HttpResponseMessage> PostJson(string url, dynamic jToken)
        {
            var message = new StringContent(jToken.ToString(), Encoding.UTF8, ApplicationJson);
            var response = await Client.PostAsync(url, message);
            return response;
        }

        protected async Task<HttpResponseMessage> PostJson(string url)
        {
            var response = await Client.PostAsync(url, new StringContent(string.Empty));
            return response;
        }

        protected async Task<HttpResponseMessage> PutJson(string url, dynamic jToken,
            Dictionary<string, string> headers)
        {
            string data = JsonConvert.SerializeObject(jToken);
            var message = new StringContent(data, Encoding.UTF8, ApplicationJson);

            if (headers == null || !headers.Any()) return await Client.PutAsync(url, message);

            foreach (var (key, value) in headers)
            {
                message.Headers.Add(key, value);
            }

            return await OneTimeTestSetup.Client.PutAsync(url, message);
        }

        protected static BankClient BuildBankClient()
        {
            // send auth requests to the Api stub
            var settings = ClientBuilder.GetJsonRefitSettings();
            var authHttpClient = new HttpClient();
            var authClient = GetRefitClient<IAuthClient>(authHttpClient, new Uri(OneTimeTestSetup.Url), settings);

            // send client requests to the integration test server but with the refit handlers in place
            var httpClient = OneTimeTestSetup.Factory.CreateDefaultClient(ClientBuilder.GetApiHandlers(authClient));
            var paymentClient = GetRefitClient<IPaymentClient>(httpClient, BaseUri, settings);

            var bankClient = new BankClient(authClient, paymentClient);
            return bankClient;
        }

        private static TClient GetRefitClient<TClient>(HttpClient client, Uri baseUri, RefitSettings settings)
        {
            var refitClient = ClientBuilder.BuildClient<TClient>(client, baseUri, settings);
            return refitClient;
        }

        public static string BuildJsonApiPayload<T>(T content)
        {
            var payload = JsonConvert.SerializeObject(content);
            return payload;
        }

        public static ServiceProvider GetServiceProvider()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.IntegrationTests.json", false, true);

            var configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBankClient(configuration);

            return serviceCollection.BuildServiceProvider();
        }
    }
}