using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Bank.Client;
using Bank.Client.Payment;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace PaymentGateway
{
    public class BankApiHealthCheck : IHealthCheck
    {
        private readonly IBankClient client;
        private readonly ILogger<BankApiHealthCheck> logger;

        public BankApiHealthCheck(IBankClient client, ILogger<BankApiHealthCheck> logger)
        {
            this.client = client;
            this.logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                await client.Payment.Process(new Payment {CardNumber = "9999-9999-9999-9999"});
            }
            catch (Exception ex) when (ex is BankApiHttpException exception &&
                                       exception.Response.StatusCode == HttpStatusCode.BadRequest)
            {
                const string msg = "Bank Api is up and running";
                logger.LogInformation(msg);
                return HealthCheckResult.Healthy(msg);
            }
            catch (Exception ex)
            {
                const string errorMsg = "Unable to connect to Bank Api";
                logger.LogError(ex, errorMsg);
                return HealthCheckResult.Unhealthy(errorMsg, ex);
            }

            return default;
        }
    }
}