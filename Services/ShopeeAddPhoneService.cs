using KAutoHelper;
using AutoTool.Services.SmsProviders;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Services
{
    public class ShopeeAddPhoneService
    {
        private readonly AdbService _adb;
        private readonly ISmsProvider _smsApi;
        private readonly CaptchaService _captchaApi;

        public ShopeeAddPhoneService(AdbService adb, ISmsProvider smsApi, CaptchaService captchaApi)
        {
            _adb = adb;
            _smsApi = smsApi;
            _captchaApi = captchaApi;
        }

        public async Task<bool> ExecuteAddPhoneFlowAsync(
            string deviceId,
            string smsToken, string smsServiceId, string smsNetwork,
            string openAiKey,
            Action<string> log,
            CancellationToken token)
        {
            Bitmap screen = null;
            try
            {
                log("📱 [AddPhone] Bắt đầu luồng thêm Số điện thoại & Xử lý Captcha...");

                // 1. ĐIỀU HƯỚNG TỚI CHỖ NHẬP SĐT 
                // (Bạn đo tọa độ bấm chữ 'Thêm SĐT' hoặc nút điều hướng và điền vào đây)
                // ADBHelper.Tap(deviceId, x, y); 
                // await Task.Delay(3000, token);

                // 2. THUÊ SỐ ĐIỆN THOẠI TỪ API
                log("📡 [AddPhone] Đang gọi API thuê số điện thoại...");
                var phoneRes = await _smsApi.RentPhoneAsync(smsToken, smsServiceId, smsNetwork, token);

                if (!phoneRes.Success)
                {
                    log($"❌ [AddPhone] Lỗi thuê số: {phoneRes.Message}");
                    return false;
                }

                log($"✅ [AddPhone] Đã lấy SĐT: {phoneRes.PhoneNumber}. Tiến hành nhập...");

                // 3. NHẬP SĐT VÀ BẤM TIẾP TỤC
                ADBHelper.Tap(deviceId, 500, 500); // Thay bằng tọa độ ô nhập SĐT thực tế
                await Task.Delay(1000, token);

                string phoneToInput = phoneRes.PhoneNumber.Replace("+84", "");
                ADBHelper.InputText(deviceId, phoneToInput);
                await Task.Delay(1000, token);

                ADBHelper.ExecuteCMD($"adb -s {deviceId} shell input keyevent 66"); // Bấm Tiếp theo
                await Task.Delay(5000, token); // Đợi load Captcha

                // 4. GIẢI CAPTCHA NGAY SAU KHI NHẬP SĐT
                log("🔍 [AddPhone] Đang quét màn hình kiểm tra Captcha...");
                screen = ADBHelper.ScreenShoot(deviceId);

                if (screen != null && !string.IsNullOrEmpty(openAiKey))
                {
                    var captchaResult = await _captchaApi.SolveShopeeCaptcha_ChatGPTAsync(screen, openAiKey, token);

                    if (captchaResult.ResultX != null)
                    {
                        log($"💡 [AddPhone] GPT tìm thấy Captcha. Cần kéo: {captchaResult.ResultX}px");
                        int startDragX = 309, startDragY = 1793;
                        int pieceOffsetX = 40;
                        int distance = captchaResult.ResultX.Value - pieceOffsetX;
                        int endDragX = startDragX + distance;

                        log("👉 [AddPhone] Tiến hành kéo Captcha...");
                        ADBHelper.Swipe(deviceId, startDragX, startDragY, endDragX, startDragY, 2000);

                        log("⏳ [AddPhone] Đợi xác thực Captcha...");
                        await Task.Delay(5000, token);
                    }
                    else
                    {
                        log($"⚠️ [AddPhone] Không thấy Captcha hoặc GPT không giải được: {captchaResult.ErrorMsg}");
                    }
                }

                // 5. CHỜ LẤY OTP
                log($"⏳ [AddPhone] Đang chờ OTP cho số {phoneRes.PhoneNumber} (Timeout 60s)...");
                string otpCode = await _smsApi.PollOtpAsync(smsToken, phoneRes.SessionId, 60, token);

                if (string.IsNullOrEmpty(otpCode))
                {
                    log("❌ [AddPhone] Hết thời gian chờ OTP từ server.");
                    return false;
                }

                log($"🎉 [AddPhone] Đã nhận OTP: {otpCode}. Đang điền vào máy...");

                // 6. NHẬP OTP
                // Bạn có thể Tap vào ô OTP trước nếu cần
                ADBHelper.InputText(deviceId, otpCode);
                await Task.Delay(1000, token);
                ADBHelper.ExecuteCMD($"adb -s {deviceId} shell input keyevent 66"); // Xác nhận OTP

                await Task.Delay(3000, token);
                log("✅ [AddPhone] Hoàn tất thêm số điện thoại thành công!");

                return true;
            }
            catch (TaskCanceledException) { log("🛑 [AddPhone] Đã hủy luồng."); return false; }
            catch (Exception ex) { log($"❌ [AddPhone] Lỗi: {ex.Message}"); return false; }
            finally { if (screen != null) screen.Dispose(); }
        }
    }
}