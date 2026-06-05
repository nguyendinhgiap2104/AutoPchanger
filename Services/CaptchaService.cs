using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AutoTool.Services
{
    public class CaptchaService
    {
        private readonly HttpClient _http;

        public CaptchaService()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        private string BitmapToBase64(Bitmap bitmap)
        {
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        // ==========================================
        // 1. CHỨC NĂNG ANTI CAPTCHA TOP 
        // ==========================================
        public async Task<int?> SolveShopeeCaptcha_AntiCaptchaAsync(Bitmap bgBitmap, string apiKey, CancellationToken token)
        {
            try
            {
                string base64Img = BitmapToBase64(bgBitmap);
                var requestBody = new
                {
                    key = apiKey,
                    method = "base64",
                    click = "shopee",
                    body = $"{base64Img}|{base64Img}", // Gửi 2 ảnh giống nhau theo API docs
                    json = 1
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                using (var content = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                {
                    // Lệnh PostAsync CÓ nhận token
                    var response = await _http.PostAsync("https://anticaptcha.top/in.php", content, token);

                    // Lệnh ReadAsStringAsync KHÔNG nhận token
                    string responseString = await response.Content.ReadAsStringAsync();

                    dynamic jsonRes = JsonConvert.DeserializeObject(responseString);

                    if (jsonRes.status != 1)
                    {
                        Console.WriteLine($"❌ Tạo Task thất bại: {jsonRes.request}");
                        return null;
                    }

                    string taskId = jsonRes.request;
                    Console.WriteLine($"✅ Task ID: {taskId}. Đang đợi giải...");

                    // Polling kết quả
                    for (int i = 0; i < 15; i++)
                    {
                        await Task.Delay(2000, token);
                        string urlResult = $"https://anticaptcha.top/res.php?key={apiKey}&id={taskId}&json=1";

                        // Tách GetStringAsync thành GetAsync + ReadAsStringAsync để không lỗi token
                        var resResponse = await _http.GetAsync(urlResult, token);
                        string resResult = await resResponse.Content.ReadAsStringAsync();

                        dynamic jsonResult = JsonConvert.DeserializeObject(resResult);

                        if (jsonResult.status == 1)
                        {
                            string resultStr = jsonResult.request;
                            if (int.TryParse(resultStr, out int resultX)) return resultX;
                            return null;
                        }
                        if (jsonResult.request == "CAPCHA_NOT_READY")
                        {
                            continue;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            catch (TaskCanceledException) { /* Bỏ qua khi người dùng ấn Stop */ }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception API: {ex.Message}");
            }
            return null;
        }

        // ==========================================
        // 2. CHỨC NĂNG CHAT GPT SOLVER 
        // ==========================================
        public async Task<(int? ResultX, string ErrorMsg)> SolveShopeeCaptcha_ChatGPTAsync(Bitmap image, string apiKey, CancellationToken token)
        {
            try
            {
                string base64Image = BitmapToBase64(image);
                int imgWidth = image.Width;

                var messagesPayload = new object[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = new object[]
                        {
                            new { type = "text", text = $"Find the missing puzzle piece hole (dark socket) on the right side of this image (Width: {imgWidth}px). Return the X coordinate of the CENTER of that hole. Output valid JSON: {{\"x\": 123}}." },
                            new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                        }
                    }
                };

                var requestBody = new
                {
                    model = "gpt-4o",
                    messages = messagesPayload,
                    max_tokens = 50,
                    temperature = 0.0,
                    response_format = new { type = "json_object" }
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);

                using (var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions"))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // Lệnh SendAsync CÓ nhận token
                    var response = await _http.SendAsync(request, token);

                    // Lệnh ReadAsStringAsync KHÔNG nhận token
                    string resString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return (null, $"HTTP {(int)response.StatusCode}: {resString}");
                    }

                    JObject jsonRes = JObject.Parse(resString);
                    string contentStr = jsonRes["choices"]?[0]?["message"]?["content"]?.ToString();

                    if (!string.IsNullOrEmpty(contentStr))
                    {
                        try
                        {
                            JObject coord = JObject.Parse(contentStr);
                            if (coord.ContainsKey("x"))
                            {
                                int x = (int)coord["x"];
                                return (x, null);
                            }
                        }
                        catch
                        {
                            return (null, $"GPT trả về sai định dạng: {contentStr}");
                        }
                    }

                    return (null, "GPT trả về nội dung rỗng.");
                }
            }
            catch (TaskCanceledException)
            {
                return (null, "Tiến trình bị hủy ngang.");
            }
            catch (Exception ex)
            {
                return (null, $"Exception: {ex.Message}");
            }
        }
    }
}