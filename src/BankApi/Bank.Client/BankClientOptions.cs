namespace Bank.Client
{
    public class BankClientOptions
    {
        private string authApiHost;
        public string ApiProtocol { get; set; } = "https";
        public string ApiHost { get; set; }

        public string AuthApiHost
        {
            get => authApiHost ?? ApiHost;
            set => authApiHost = value;
        }

        public string ApiClientId { get; set; }
        public string ApiGrantType { get; set; } = "client_credentials";
        public string ApiClientSecret { get; set; }
        public string ApiSubscriptionKey { get; set; }
        public bool? IsManagedIdentity { get; set; } = false;
        public string TokenEarlyExpirySeconds => "10";
    }
}