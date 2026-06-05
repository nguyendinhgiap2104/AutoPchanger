using Newtonsoft.Json.Linq; // Đã chuyển sang Newtonsoft cho đồng bộ
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Services.SmsProviders
{
    public class BossOtpSmsProvider : ISmsProvider
    {
        private readonly HttpClient _http;
        private const string BossBaseUrl = "https://bossotp.com";

        public BossOtpSmsProvider()
        {
            // ==========================================
            // FIX TẬN GỐC LỖI KẾT NỐI MẠNG (AN ERROR OCCURRED...)
            // ==========================================
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; }; // Bỏ qua lỗi SSL

            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }

        public async Task<SmsRentResult> RentPhoneAsync(string apiKey, string serviceId, string network, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return new SmsRentResult { Success = false, Message = "Chưa nhập API Key BossOTP" };
            if (string.IsNullOrWhiteSpace(serviceId)) return new SmsRentResult { Success = false, Message = "Chưa nhập Service ID" };

            try
            {
                string url = $"{BossBaseUrl}/api/v4/rents/create?api_token={apiKey}&service_id={serviceId}";
                if (!string.IsNullOrWhiteSpace(network)) url += $"&network={network.ToUpper()}";

                var response = await _http.GetAsync(url, token);
                string result = await response.Content.ReadAsStringAsync();

                JObject root = JObject.Parse(result);

                if (response.IsSuccessStatusCode)
                {
                    string phone = root["number"]?.ToString();
                    string rentId = root["rent_id"]?.ToString();

                    if (!string.IsNullOrEmpty(rentId))
                        return new SmsRentResult { Success = true, SessionId = rentId, PhoneNumber = phone };
                }
                else
                {
                    string errorMsg = root["error"]?.ToString();
                    string errorCode = root["code"]?.ToString();
                    return new SmsRentResult { Success = false, Message = $"[{errorCode}] {errorMsg}" };
                }
            }
            catch (TaskCanceledException) { return new SmsRentResult { Success = false, Message = "Hủy request." }; }
            catch (Exception ex) { return new SmsRentResult { Success = false, Message = $"Lỗi kết nối: {ex.Message}" }; }

            return new SmsRentResult { Success = false, Message = "Lỗi không xác định." };
        }

        public async Task<string> PollOtpAsync(string apiKey, string sessionId, int timeoutSeconds, CancellationToken token)
        {
            int delayMs = 3000;
            int maxAttempts = (timeoutSeconds * 1000) / delayMs;

            for (int i = 0; i < maxAttempts; i++)
            {
                if (token.IsCancellationRequested) return null;

                try
                {
                    string url = $"{BossBaseUrl}/api/v4/rents/check?api_token={apiKey}&_id={sessionId}";
                    var response = await _http.GetAsync(url, token);
                    string result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        JObject root = JObject.Parse(result);
                        string status = root["status"]?.ToString();

                        if (status == "SUCCESS")
                        {
                            string otp = root["otp"]?.ToString();
                            if (!string.IsNullOrEmpty(otp)) return otp;
                        }
                        else if (status == "FAILED") return null;
                    }
                }
                catch { }

                await Task.Delay(delayMs, token);
            }
            return null;
        }
    }
}