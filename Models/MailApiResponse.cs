using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AutoTool.Models
{
    public class MailApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public MailApiData Data { get; set; }
    }

    public class MailApiData
    {
        [JsonPropertyName("accounts")]
        public List<string> Accounts { get; set; }
    }
}