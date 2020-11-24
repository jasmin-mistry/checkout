using WireMock.Server;

namespace Bank.ApiStub
{
    public interface IWireMockService
    {
        void Run();
        WireMockServer Start();
        void Stop(WireMockServer wireMockServer);
    }
}