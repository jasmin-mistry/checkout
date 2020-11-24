using Newtonsoft.Json;

namespace Bank.Client.Payment
{
    public class PaymentResponse
    {
        [JsonProperty("uniqueId")] public string TransactionId { get; set; }
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("reason")] public string Reason { get; set; }
    }
}