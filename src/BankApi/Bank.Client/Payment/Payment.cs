using Newtonsoft.Json;

namespace Bank.Client.Payment
{
    public class Payment
    {
        [JsonProperty("cardNumber")] public string CardNumber { get; set; }
        [JsonProperty("expiryMonth")] public string ExpiryMonth { get; set; }
        [JsonProperty("expiryYear")] public string ExpiryYear { get; set; }
        [JsonProperty("amount")] public decimal Amount { get; set; }
        [JsonProperty("currency")] public string Currency { get; set; }
        [JsonProperty("cvv")] public string Cvv { get; set; }
    }
}