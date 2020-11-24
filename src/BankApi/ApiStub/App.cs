using Microsoft.Extensions.Logging;

namespace Bank.ApiStub
{
    public class App
    {
        private readonly ILogger logger;
        private readonly IWireMockService service;

        public App(IWireMockService service, ILogger logger)
        {
            this.service = service;
            this.logger = logger;
        }

        public void Run()
        {
            logger.LogInformation("WireMock.Net App running");
            service.Run();
        }
    }
}