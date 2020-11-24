using Newtonsoft.Json;

namespace Bank.Client.Auth
{
    public class Token
    {
        [JsonProperty("token_type")] public string TokenType { get; set; }
        [JsonProperty("expires_in")] public string ExpiresIn { get; set; }
        [JsonProperty("access_token")] public string AccessToken { get; set; }
    }
}