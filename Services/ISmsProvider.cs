using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Services.SmsProviders
{
    public class SmsRentResult
    {
        public bool Success { get; set; }
        public string SessionId { get; set; }
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
    }

    public interface ISmsProvider
    {
        Task<SmsRentResult> RentPhoneAsync(string apiKey, string serviceId, string network, CancellationToken token);
        Task<string> PollOtpAsync(string apiKey, string sessionId, int timeoutSeconds, CancellationToken token);
    }
}