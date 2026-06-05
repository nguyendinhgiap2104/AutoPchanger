using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Services
{
    public class ProxyService
    {
        private readonly HttpClient _http;

        public ProxyService()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<(bool Success, string ProxyString, string ErrorMsg)> GetTMProxyAsync(string apiKey, Action<string> log, CancellationToken token)
        {
            log("🔄 [TMProxy] Đang yêu cầu cấp Proxy mới...");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var payload = new { api_key = apiKey, id_location = 0, id_isp = 0 };
                    string jsonPayload = JsonSerializer.Serialize(payload);
                    using (var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
                    {
                        var response = await _http.PostAsync("https://tmproxy.com/api/proxy/get-new-proxy", content, token);

                        string responseString = await response.Content.ReadAsStringAsync();

                        using (JsonDocument doc = JsonDocument.Parse(responseString))
                        {
                            var root = doc.RootElement;
                            int code = root.GetProperty("code").GetInt32();
                            string message = root.GetProperty("message").GetString();

                            if (code == 0)
                            {
                                var data = root.GetProperty("data");
                                string socks5 = data.GetProperty("socks5").GetString();
                                string user = data.TryGetProperty("username", out var u) ? u.GetString() : "";
                                string pass = data.TryGetProperty("password", out var p) ? p.GetString() : "";

                                string fullProxy = string.IsNullOrEmpty(user) ? socks5 : $"{socks5}:{user}:{pass}";
                                log($"✅ [TMProxy] Đã lấy IP: {fullProxy}");
                                return (true, fullProxy, "");
                            }

                            if (message.ToLower().Contains("wait") || message.ToLower().Contains("đợi") || message.ToLower().Contains("retry"))
                            {
                                int waitSeconds = 10;
                                var match = Regex.Match(message, @"\d+");
                                if (match.Success) waitSeconds = int.Parse(match.Value);

                                log($"⏳ [TMProxy] Server yêu cầu đợi {waitSeconds}s...");
                                await Task.Delay((waitSeconds + 2) * 1000, token);
                                continue;
                            }

                            log($"❌ [TMProxy] Lỗi: {message}");
                            return (false, "", message);
                        }
                    }
                }
                catch (TaskCanceledException) { return (false, "", "Hủy thao tác"); }
                catch (Exception ex)
                {
                    log($"❌ [TMProxy] Lỗi mạng: {ex.Message}. Thử lại sau 5s...");
                    await Task.Delay(5000, token);
                }
            }
            return (false, "", "Timeout");
        }

        // ================= HÀM PROXY NO1 ĐƯỢC BỔ SUNG =================
        public async Task<bool> ChangeProxyNo1Async(string apiKey, Action<string> log, CancellationToken token)
        {
            log("🔄 [ProxyNo1] Đang yêu cầu đổi IP...");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string urlChange = $"https://app.proxyno1.com/api/change-key-ip/{apiKey}";

                    var response = await _http.GetAsync(urlChange, token);
                    string responseString = await response.Content.ReadAsStringAsync(); // Không truyền token

                    using (JsonDocument doc = JsonDocument.Parse(responseString))
                    {
                        var root = doc.RootElement;
                        int status = root.GetProperty("status").GetInt32();
                        string message = root.GetProperty("message").GetString();

                        if (status == 0)
                        {
                            log($"✅ [ProxyNo1] Đổi IP thành công!");
                            return true;
                        }

                        var match = Regex.Match(message, @"\d+");
                        int waitSeconds = match.Success ? int.Parse(match.Value) : 10;
                        log($"⏳ [ProxyNo1] Chờ {waitSeconds}s theo yêu cầu server...");
                        await Task.Delay((waitSeconds + 2) * 1000, token);
                    }
                }
                catch (TaskCanceledException) { return false; }
                catch (Exception ex)
                {
                    log($"❌ [ProxyNo1] Lỗi mạng: {ex.Message}. Chờ 5s thử lại...");
                    await Task.Delay(5000, token);
                }
            }
            return false;
        }
    }
}