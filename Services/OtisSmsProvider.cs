using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Services.SmsProviders
{
    public class OtisSmsProvider : ISmsProvider
    {
        private readonly HttpClient _http;
        private const string OtisBaseUrl = "https://otistx.com/api";

        public OtisSmsProvider()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }

        public async Task<SmsRentResult> RentPhoneAsync(string apiKey, string serviceId, string network, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return new SmsRentResult { Success = false, Message = "Chưa nhập API Key Otis!" };

            // Xử lý giá trị mặc định cho Otis
            if (string.IsNullOrWhiteSpace(serviceId)) serviceId = "otissim_v1";
            if (string.IsNullOrWhiteSpace(network)) network = "main_3";

            try
            {
                var payload = new { service = serviceId, carrier = network };
                string jsonPayload = JsonSerializer.Serialize(payload);

                using (var request = new HttpRequestMessage(HttpMethod.Post, $"{OtisBaseUrl}/phone-rental/start"))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey.Replace("Bearer ", "").Trim());
                    request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
                    request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await _http.SendAsync(request, token);
                    string result = await response.Content.ReadAsStringAsync();

                    if (result.Trim().StartsWith("<")) return new SmsRentResult { Success = false, Message = "Lỗi HTML (Sai URL hoặc Token hết hạn)" };

                    JsonNode root = JsonNode.Parse(result);
                    if (root == null) return new SmsRentResult { Success = false, Message = "Lỗi đọc JSON" };

                    JsonNode data = root["data"];
                    string sessId = data?["sessionId"]?.GetValue<string>();
                    string phone = data?["number"]?.GetValue<string>();

                    if (!string.IsNullOrEmpty(sessId))
                    {
                        return new SmsRentResult { Success = true, SessionId = sessId, PhoneNumber = phone };
                    }

                    bool isSuccess = root["success"] != null && (bool)root["success"];
                    if (!isSuccess)
                    {
                        string msg = root["message"]?.GetValue<string>() ?? "Lỗi từ server Otis";
                        return new SmsRentResult { Success = false, Message = msg };
                    }
                }
            }
            catch (TaskCanceledException) { return new SmsRentResult { Success = false, Message = "Timeout request." }; }
            catch (Exception ex) { return new SmsRentResult { Success = false, Message = "Exception: " + ex.Message }; }

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
                    using (var request = new HttpRequestMessage(HttpMethod.Get, $"{OtisBaseUrl}/phone-rental/get-otp?sessionId={sessionId}"))
                    {
                        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey.Replace("Bearer ", "").Trim());
                        var response = await _http.SendAsync(request, token);
                        string result = await response.Content.ReadAsStringAsync();

                        if (!result.Trim().StartsWith("<"))
                        {
                            JsonNode root = JsonNode.Parse(result);
                            if (root != null)
                            {
                                JsonNode data = root["data"];
                                string otp = data?["otp"]?.GetValue<string>() ?? data?["code"]?.GetValue<string>() ?? root["otp"]?.GetValue<string>();
                                if (!string.IsNullOrEmpty(otp)) return otp;

                                int? status = (int?)root["status"];
                                if (status == 2) return null; // Hủy hoặc Hết hạn
                            }
                        }
                    }
                }
                catch { }
                await Task.Delay(delayMs, token);
            }
            return null;
        }
    }
}