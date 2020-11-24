using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WireMock.Net.StandAlone;
using WireMock.Server;
using WireMock.Settings;

namespace Bank.ApiStub
{
    public partial class WireMockService : IWireMockService
    {
        private const int SleepTime = 30000;

        private readonly ILogger logger;
        private readonly IWireMockServerSettings settings;

        public WireMockService(ILogger logger, IWireMockServerSettings settings,
            IProxyAndRecordSettings proxyAndRecordSettings)
        {
            this.logger = logger;
            this.settings = settings;
            this.settings.ProxyAndRecordSettings = proxyAndRecordSettings;
            this.settings.Logger = new Logger(logger);
        }

        public WireMockService(ILogger logger, IWireMockServerSettings settings)
        {
            this.logger = logger;
            this.settings = settings;
            this.settings.Logger = new Logger(logger);
        }

        public void Run()
        {
            logger.LogInformation("WireMock.Net server starting");

            Start();

            logger.LogInformation($"WireMock.Net server settings {JsonConvert.SerializeObject(settings)}");

            while (true)
            {
                logger.LogInformation("WireMock.Net server running");
                Thread.Sleep(SleepTime);
            }
        }

        public WireMockServer Start()
        {
            var stubServer = StandAloneApp.Start(settings);
            stubServer.ReadAllMappings();
            return stubServer;
        }

        public void Stop(WireMockServer wireMockServer)
        {
            wireMockServer.Stop();
        }
    }
}