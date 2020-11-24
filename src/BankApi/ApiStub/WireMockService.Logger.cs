using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WireMock.Admin.Requests;
using WireMock.Logging;

namespace Bank.ApiStub
{
    public partial class WireMockService
    {
        private class Logger : IWireMockLogger
        {
            private readonly ILogger logger;

            public Logger(ILogger logger)
            {
                this.logger = logger;
            }

            public void Debug(string formatString, params object[] args)
            {
                logger.LogDebug(formatString, args);
            }

            public void Info(string formatString, params object[] args)
            {
                logger.LogInformation(formatString, args);
            }

            public void Warn(string formatString, params object[] args)
            {
                logger.LogWarning(formatString, args);
            }

            public void Error(string formatString, params object[] args)
            {
                logger.LogError(formatString, args);
            }

            public void Error(string formatString, Exception exception)
            {
                logger.LogError(formatString, exception);
            }

            public void DebugRequestResponse(LogEntryModel logEntryModel, bool isAdminRequest)
            {
                var message = JsonConvert.SerializeObject(logEntryModel, Formatting.Indented);
                logger.LogDebug("Admin[{0}] {1}", isAdminRequest, message);
            }
        }
    }
}