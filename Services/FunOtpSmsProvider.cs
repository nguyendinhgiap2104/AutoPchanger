using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Services.SmsProviders
{
    public class FunOtpSmsProvider : ISmsProvider
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "https://funotp.com/api";

        public FunOtpSmsProvider()
        {
            // ==========================================
            // FIX TẬN GỐC LỖI KẾT NỐI MẠNG (AN ERROR OCCURRED...)
            // ==========================================
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }

        public async Task<SmsRentResult> RentPhoneAsync(string apiKey, string serviceId, string network, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return new SmsRentResult { Success = false, Message = "Chưa nhập API Key FunOTP" };
            if (string.IsNullOrWhiteSpace(serviceId)) return new SmsRentResult { Success = false, Message = "Chưa nhập Service ID" };

            try
            {
                string url = $"{BaseUrl}?action=number&service={serviceId}&apikey={apiKey}";
                if (!string.IsNullOrWhiteSpace(network)) url += $"&operator={network.ToLower()}";

                var response = await _http.GetAsync(url, token);
                string result = await response.Content.ReadAsStringAsync();

                JObject root = JObject.Parse(result);
                int responseCode = root["ResponseCode"] != null ? root["ResponseCode"].ToObject<int>() : -1;

                if (responseCode == 0)
                {
                    var resultObj = root["Result"];
                    string phone = resultObj?["numberno84"]?.ToString();
                    string rentId = resultObj?["id"]?.ToString();

                    if (!string.IsNullOrEmpty(rentId))
                        return new SmsRentResult { Success = true, SessionId = rentId, PhoneNumber = phone };
                }
                else
                {
                    // ==========================================
                    // ĐÃ ĐƯA VỀ CÚ PHÁP C# 7.3 ĐỂ KHÔNG BỊ LỖI
                    // ==========================================
                    string errorMsg;
                    switch (responseCode)
                    {
                        case 1:
                            errorMsg = "Hết số trên hệ thống";
                            break;
                        case 2:
                            errorMsg = "Dịch vụ không hợp lệ (Kiểm tra Prod ID)";
                            break;
                        case 3:
                            errorMsg = "Số dư không đủ";
                            break;
                        case 4:
                            errorMsg = "Mua số quá nhiều (Bị block tạm thời)";
                            break;
                        default:
                            errorMsg = $"Lỗi từ server ({responseCode})";
                            break;
                    }
                    return new SmsRentResult { Success = false, Message = errorMsg };
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
                    string url = $"{BaseUrl}?action=code&id={sessionId}&apikey={apiKey}";
                    var response = await _http.GetAsync(url, token);
                    string result = await response.Content.ReadAsStringAsync();

                    JObject root = JObject.Parse(result);
                    int responseCode = root["ResponseCode"] != null ? root["ResponseCode"].ToObject<int>() : -1;

                    if (responseCode == 0)
                    {
                        string otp = root["Result"]?["otp"]?.ToString();
                        if (!string.IsNullOrEmpty(otp)) return otp;
                    }
                    else if (responseCode == 2)
                    {
                        return null;
                    }
                }
                catch { }

                await Task.Delay(delayMs, token);
            }

            return null;
        }
    }
}