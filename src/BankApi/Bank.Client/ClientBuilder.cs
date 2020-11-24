using System;
using System.Net.Http;
using Bank.Client.Auth;
using Bank.Client.Handlers;
using LazyCache;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Options;
using Refit;

namespace Bank.Client
{
    public static class ClientBuilder
    {
        public static RefitSettings GetJsonRefitSettings()
        {
            return new RefitSettings {ContentSerializer = new JsonContentSerializer()};
        }

        public static TClient BuildClient<TClient>(HttpClient httpClient, Uri baseAddress, RefitSettings refitSettings)
        {
            httpClient.BaseAddress = baseAddress;
            var requestBuilder = RequestBuilder.ForType<TClient>(refitSettings);
            var client = RestService.For(httpClient, requestBuilder);
            return client;
        }

        public static DelegatingHandler[] GetApiHandlers(IAuthClient auth)
        {
            var options = Options.Create(new BankClientOptions());

            return new DelegatingHandler[]
            {
                new BankAuthHandler(null, options, new AzureServiceTokenProvider(), auth, new CachingService())
            };
        }
    }
}