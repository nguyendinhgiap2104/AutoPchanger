using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AutoTool.Services
{
    public class PChangerApiService
    {
        private readonly HttpClient _client;

        // BaseUrl sẽ được gán tự động khi gọi các hàm
        private string _baseUrl = "http://127.0.0.1:8080";

        public PChangerApiService()
        {
            _client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        }

        // ==========================================
        // CÁC HÀM CŨ BẠN ĐÃ CÓ
        // ==========================================
        public async Task<string> GetAdbIdAsync(string baseUrl, string deviceKey, CancellationToken token)
        {
            _baseUrl = baseUrl;
            try
            {
                string url = $"{baseUrl}/dev/{deviceKey}/device";
                var response = await _client.GetAsync(url, token);
                string result = await response.Content.ReadAsStringAsync();
                dynamic json = JsonConvert.DeserializeObject(result);
                if (json != null && json.status == true)
                {
                    return json.adb;
                }
            }
            catch { }
            return null;
        }

        public async Task<bool> SendCommandAsync(string baseUrl, string endpoint, CancellationToken token)
        {
            _baseUrl = baseUrl;
            try
            {
                string url = $"{baseUrl}{endpoint}";
                var response = await _client.GetAsync(url, token);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<PchangerResponse> GetDeviceDetailAsync(string deviceKey)
        {
            try
            {
                string url = $"{_baseUrl}/dev/{deviceKey}/device";
                var response = await _client.GetStringAsync(url);
                return JsonConvert.DeserializeObject<PchangerResponse>(response);
            }
            catch { return null; }
        }

        // ==========================================
        // CÁC HÀM BỔ SUNG ĐỂ CHẠY AUTO
        // ==========================================
        public async Task<bool> ChangeDeviceAsync(string key, double? lat, double? lon)
        {
            string query = (lat.HasValue && lon.HasValue) ? $"?lat={lat}&lon={lon}" : "";
            return await SendCommandWithRetryAsync($"/dev/{key}/change{query}");
        }

        public async Task<bool> BackupAsync(string key, string name)
        {
            return await SendCommandWithRetryAsync($"/dev/{key}/backup?name={Uri.EscapeDataString(name)}");
        }

        public async Task<bool> RestoreAsync(string key, string name)
        {
            return await SendCommandWithRetryAsync($"/dev/{key}/restore?name={Uri.EscapeDataString(name)}");
        }

        private async Task<bool> SendCommandWithRetryAsync(string endpoint)
        {
            while (true)
            {
                try
                {
                    var response = await _client.GetAsync($"{_baseUrl}{endpoint}");
                    string result = await response.Content.ReadAsStringAsync();
                    dynamic json = JsonConvert.DeserializeObject(result);

                    // Nếu Pchanger đang busy -> Đợi 2s rồi gọi lại
                    if (json != null && json.status == false && json.note != null && json.note.ToString().ToLower().Contains("busy"))
                    {
                        await Task.Delay(2000);
                        continue;
                    }
                    return json?.status == true;
                }
                catch { return false; }
            }
        }
    }

    // ==========================================
    // CLASS MODEL CHỨA DATA TRẢ VỀ
    // ==========================================
    public class PchangerResponse
    {
        [JsonProperty("status")] public bool Status { get; set; }
        [JsonProperty("note")] public string Note { get; set; }
        [JsonProperty("adb")] public string AdbId { get; set; }
        [JsonProperty("key")] public string DeviceKey { get; set; }
    }
}