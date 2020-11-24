using System;
using Bank.Client;
using Bank.Client.Auth;
using Bank.Client.Handlers;
using Bank.Client.Payment;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Refit;

// note: this is recommended practise to place registrations in this MS namespace. 
// See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection#registering-your-own-services
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBankClient(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            serviceCollection.AddOptions();
            serviceCollection.AddLogging();

            serviceCollection.Configure<BankClientOptions>(configuration.GetSection("bank-api-options"));


            serviceCollection.AddSingleton<IBankClient, BankClient>();
            serviceCollection.AddTransient<BankAuthHandler>();
            serviceCollection.AddTransient<BankExceptionHandler>();
            serviceCollection.AddTransient(
                provider => new AzureServiceTokenProvider());

            var settings = ClientBuilder.GetJsonRefitSettings();
            serviceCollection.AddAuthClient(settings);
            serviceCollection.AddApiClient<IPaymentClient>(settings);
            serviceCollection.AddLazyCache();
        }

        private static void AddAuthClient(this IServiceCollection services, RefitSettings settings = null)
        {
            services.AddSingleton(provider => RequestBuilder.ForType<IAuthClient>(settings));
            services.AddRefitClient<IAuthClient>(settings)
                .SetClientBaseAddress(true)
                .AddHttpMessageHandler<BankExceptionHandler>();
        }

        private static void AddApiClient<TClient>(this IServiceCollection services,
            RefitSettings settings = null) where TClient : class
        {
            services.AddRefitClient<TClient>(settings)
                .SetClientBaseAddress()
                .AddHttpMessageHandler<BankAuthHandler>()
                .AddHttpMessageHandler<BankExceptionHandler>();
        }

        private static IHttpClientBuilder SetClientBaseAddress(this IHttpClientBuilder builder, bool auth = false)
        {
            return builder.ConfigureHttpClient((serviceProvider, client) =>
            {
                BankClientOptions bankApiOptions;
                try
                {
                    bankApiOptions =
                        serviceProvider.GetRequiredService<IOptions<BankClientOptions>>().Value;
                }
                catch (Exception e)
                {
                    throw new ConfigurationException(
                        "Creation of Bank Client failed due to missing configuration.",
                        e);
                }

                try
                {
                    if (auth)
                    {
                        client.BaseAddress =
                            new Uri(
                                $"{bankApiOptions.ApiProtocol}://{bankApiOptions.ApiHost}");
                    }
                    else
                    {
                        client.BaseAddress =
                            new Uri(
                                $"{bankApiOptions.ApiProtocol}://{bankApiOptions.ApiHost}");
                    }
                }
                catch (Exception e)
                {
                    throw new ConfigurationException(
                        $"Creation of Bank Client failed - probably due to incorrect configuration. The provided options were: \n{JsonConvert.SerializeObject(bankApiOptions)}",
                        e);
                }
            });
        }
    }
}