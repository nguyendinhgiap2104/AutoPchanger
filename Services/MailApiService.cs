using System;
using System.Collections.Generic;
using System.Linq; // Cần thiết để sắp xếp và lọc danh sách
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoTool.Models;

namespace AutoTool.Models
{
    // Bổ sung Class để hứng data từ API GetListGmailProduct
    public class GmailProductResponse
    {
        public bool success { get; set; }
        public List<GmailProductInfo> listproduct { get; set; }
    }

    public class GmailProductInfo
    {
        public int id { get; set; }
        public string name { get; set; }
        public int price { get; set; }
        public int quantity { get; set; }
    }
}

namespace AutoTool.Services
{
    public class MailApiService
    {
        private readonly HttpClient _http;

        public MailApiService()
        {
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(60);
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        // ==============================================================================
        // HÀM GỌI API THUÊ MAIL CHÍNH (VÒNG LẶP SĂN MỒI VÔ TẬN)
        // ==============================================================================
        public async Task<(bool success, string email, string password, string recoveryEmail, string errorMessage)> BuyMailAsync(string apiKey, string defaultProductId, CancellationToken token, Action<string> log = null)
        {
            int maxRetries = 5;

            // Vòng lặp vô tận: Sẽ kẹt ở đây cày liên tục cho đến khi mua được Acc mới nhả ra
            while (true)
            {
                if (token.IsCancellationRequested) return (false, "", "", "", "Đã hủy");

                // 1. THỬ DỊCH VỤ MẶC ĐỊNH TRƯỚC
                log?.Invoke($"🛒 Đang thử thuê dịch vụ mặc định (ID: {defaultProductId})...");
                bool isDefaultSuccess = false;
                for (int i = 1; i <= maxRetries; i++)
                {
                    if (token.IsCancellationRequested) return (false, "", "", "", "Đã hủy");

                    var result = await TryBuySingleMailAsync(apiKey, defaultProductId, token);
                    if (result.success)
                    {
                        log?.Invoke($"✅ Đã thuê thành công ID mặc định: {defaultProductId}");
                        return result; // Mua thành công -> Thoát hoàn toàn
                    }

                    log?.Invoke($"⏳ Thuê ID {defaultProductId} thất bại lần {i}/{maxRetries}. Đang chờ 3s thử lại...");
                    await Task.Delay(3000, token);
                }

                // 2. NẾU MẶC ĐỊNH TỊT -> LẤY DANH SÁCH DỊCH VỤ DỰ PHÒNG
                log?.Invoke("⚠️ Dịch vụ mặc định không khả dụng. Đang quét danh sách dịch vụ thay thế...");
                var fallbackProducts = await GetAvailableProductsAsync(apiKey, token);

                if (fallbackProducts == null || fallbackProducts.Count == 0)
                {
                    log?.Invoke("🔴 Mạng lỗi hoặc API không trả về danh sách. Nghỉ 1 phút rồi quét lại toàn bộ...");
                    await Task.Delay(60000, token);
                    continue; // Hết 1 phút -> Quay lại vòng lặp while(true)
                }

                // 3. LỌC VÀ SẮP XẾP: Bỏ cái mặc định -> Chỉ lấy cái còn hàng (quantity > 0) -> Xếp giá từ thấp tới cao
                var sortedProducts = fallbackProducts
                    .Where(p => p.id.ToString() != defaultProductId && p.quantity > 0)
                    .OrderBy(p => p.price)
                    .ToList();

                // NẾU TẤT CẢ ĐỀU HẾT HÀNG (QUANTITY = 0)
                if (sortedProducts.Count == 0)
                {
                    log?.Invoke("🔴 Hiện tại toàn bộ hệ thống đều HẾT HÀNG. Tiến hành rình rập: Đợi 1 phút rồi check lại...");
                    await Task.Delay(60000, token); // Ngủ 1 phút
                    continue; // Đánh thức dậy -> Quay lại đầu vòng lặp while(true) cày tiếp
                }

                // 4. VÉT MÁNG TỪNG DỊCH VỤ (TỪ RẺ TỚI ĐẮT)
                bool isBoughtFallback = false;
                foreach (var product in sortedProducts)
                {
                    log?.Invoke($"🔄 Chuyển sang dịch vụ: {product.name} (ID: {product.id} - Giá: {product.price}đ - Tồn: {product.quantity})");

                    for (int i = 1; i <= maxRetries; i++)
                    {
                        if (token.IsCancellationRequested) return (false, "", "", "", "Đã hủy");

                        var result = await TryBuySingleMailAsync(apiKey, product.id.ToString(), token);
                        if (result.success)
                        {
                            log?.Invoke($"✅ Đã thuê thành công ID thay thế: {product.id}");
                            return result; // Mua thành công -> Thoát hoàn toàn
                        }

                        log?.Invoke($"⏳ Thuê ID {product.id} thất bại lần {i}/{maxRetries}. Chờ 3s thử lại...");
                        await Task.Delay(3000, token);
                    }
                }

                // 5. RÌNH RẬP DỰ PHÒNG CHÓT
                // Nếu cày nát list có số lượng > 0 mà vẫn xịt (do api ảo hoặc người khác hớt tay trên)
                log?.Invoke("🔴 Cày nát list nhưng vẫn không mua được acc. Nghỉ 1 phút rồi quét lại từ đầu...");
                await Task.Delay(60000, token); // Ngủ 1 phút rồi lại lao vào while(true)
            }
        }

        // ==============================================================================
        // HÀM LÕI LÀM NHIỆM VỤ MUA 1 ACCOUNT
        // ==============================================================================
        private async Task<(bool success, string email, string password, string recoveryEmail, string errorMessage)> TryBuySingleMailAsync(string apiKey, string productId, CancellationToken token)
        {
            try
            {
                string baseUrl = "https://api.shopgmail9999.com";
                string url = $"{baseUrl}/api/BuyGmail/BuyProduct?apikey={apiKey}&quantity=1&product_id={productId}";

                var response = await _http.GetAsync(url, token);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResult = JsonSerializer.Deserialize<MailApiResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (apiResult != null && apiResult.Success && apiResult.Data?.Accounts?.Count > 0)
                    {
                        string accountInfo = apiResult.Data.Accounts[0];
                        string[] parts = accountInfo.Split('|');

                        if (parts.Length >= 2)
                        {
                            string email = parts[0];
                            string password = parts[1];
                            string recoveryEmail = parts.Length >= 3 ? parts[2] : "";

                            return (true, email, password, recoveryEmail, "");
                        }
                    }
                    return (false, "", "", "", apiResult?.Message ?? "Không lấy được Acc");
                }
                return (false, "", "", "", $"Lỗi Server: {response.StatusCode}");
            }
            catch (TaskCanceledException) { return (false, "", "", "", "Timeout"); }
            catch (Exception ex) { return (false, "", "", "", ex.Message); }
        }

        // ==============================================================================
        // HÀM LẤY DANH SÁCH DỊCH VỤ DỰ PHÒNG
        // ==============================================================================
        private async Task<List<GmailProductInfo>> GetAvailableProductsAsync(string apiKey, CancellationToken token)
        {
            try
            {
                string url = $"https://api.shopgmail9999.com/api/BuyGmail/GetListGmailProduct?apikey={apiKey}";
                var response = await _http.GetAsync(url, token);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<GmailProductResponse>(content, options);

                    if (result != null && result.success && result.listproduct != null)
                    {
                        return result.listproduct;
                    }
                }
            }
            catch { }
            return new List<GmailProductInfo>(); // Lỗi mạng thì trả về mảng rỗng
        }
    }
}