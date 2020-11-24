using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Bank.Client.Auth;
using LazyCache;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;

namespace Bank.Client.Handlers
{
    public class BankAuthHandler : DelegatingHandler
    {
        private readonly IAuthClient auth;
        private readonly IAppCache cache;
        private readonly ILogger<BankAuthHandler> logger;
        private readonly BankClientOptions options;
        private readonly AzureServiceTokenProvider tokenProvider;

        public BankAuthHandler(ILogger<BankAuthHandler> logger,
            IOptions<BankClientOptions> options, AzureServiceTokenProvider tokenProvider, IAuthClient auth,
            IAppCache cache)
        {
            this.logger = logger;
            this.tokenProvider = tokenProvider;
            this.options = options.Value;
            this.auth = auth;
            this.cache = cache;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request = await SetRequestHeaders(request);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private async Task<HttpRequestMessage> SetRequestHeaders(HttpRequestMessage request)
        {
            if (request.Content != null && request.Method != HttpMethod.Get)
                await request.Content.ReadAsStringAsync();

            if (request.Headers?.Authorization?.Scheme == InternalConstants.HttpHeaderAuthType)
            {
                logger?.LogDebug("Adding authorization headers to request {0}", request.RequestUri);
                var credentials = await GetToken();
                AddSecurityTokenHeader(request, credentials, options);
            }

            return request;
        }

        private static void AddSecurityTokenHeader(HttpRequestMessage request, ApiResponse<Token> credentials,
            BankClientOptions options)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.Content.AccessToken);

            request.Headers.Add(InternalConstants.HttpHeaderSubscriptionKey, options.ApiSubscriptionKey);
        }

        private async Task<ApiResponse<Token>> GetToken()
        {
            ApiResponse<Token> tokenResponse;

            var credentials = await cache.GetOrAddAsync(typeof(BankAuthHandler).FullName, async entry =>
            {
                if (options.IsManagedIdentity.HasValue && options.IsManagedIdentity == true)
                {
                    var authResult =
                        await tokenProvider.GetAuthenticationResultAsync(options.ApiClientId);
                    var token = new Token
                    {
                        TokenType = authResult.TokenType,
                        ExpiresIn = authResult.ExpiresOn.Offset.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                        AccessToken = authResult.AccessToken
                    };
                    tokenResponse = new ApiResponse<Token>(new HttpResponseMessage(HttpStatusCode.OK), token);
                }
                else
                {
                    tokenResponse = await auth.GetToken(new Dictionary<string, object>
                    {
                        {InternalConstants.HttpHeaderClientId, options.ApiClientId},
                        {InternalConstants.HttpHeaderClientSecret, options.ApiClientSecret},
                        {InternalConstants.HttpHeaderGrantType, options.ApiGrantType}
                    });
                }

                var expiryDate = DateTime.UtcNow.AddSeconds(Convert.ToInt16(tokenResponse.Content.ExpiresIn));
                //set up early expiration 
                expiryDate = expiryDate.AddSeconds(Convert.ToInt16(options.TokenEarlyExpirySeconds) * -1);
                entry.SetAbsoluteExpiration(expiryDate);
                await tokenResponse.EnsureSuccessStatusCodeAsync();
                return tokenResponse;
            });

            return credentials;
        }
    }
}