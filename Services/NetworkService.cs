using AutoTool.Services;
using Emgu.CV.Ocl;
using KAutoHelper;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Services
{
    public class NetworkService
    {
        private readonly AdbService _adbService;

        public NetworkService(AdbService adbService)
        {
            _adbService = adbService;
        }

        // --- Helper chạy lệnh ADB ngầm ---
        private async Task<string> AdbCmdAsync(string adbId, string cmd)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = _adbService.AdbPath;
                    p.StartInfo.Arguments = $"-s {adbId} {cmd}";
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.Start();
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    return output;
                }
                catch { return string.Empty; }
            });
        }

        public async Task ToggleAirplaneModeAsync(string adbId, Action<string> log, CancellationToken token)
        {
            log($"✈️ [{adbId}] Kích hoạt ngắt sóng mạng...");
            await AdbCmdAsync(adbId, "shell cmd connectivity airplane-mode enable");
            await AdbCmdAsync(adbId, "shell settings put global airplane_mode_on 1");
            await AdbCmdAsync(adbId, "shell am broadcast -a android.intent.action.AIRPLANE_MODE --ez state true");

            await Task.Delay(3000, token);

            log($"✈️ [{adbId}] Mở lại sóng, chờ nhận IP mới...");
            await AdbCmdAsync(adbId, "shell cmd connectivity airplane-mode disable");
            await AdbCmdAsync(adbId, "shell settings put global airplane_mode_on 0");
            await AdbCmdAsync(adbId, "shell am broadcast -a android.intent.action.AIRPLANE_MODE --ez state false");

            await Task.Delay(5000, token);
        }

        // ==============================================================
        // BẢN FIX: CẤU HÌNH SOCKSDROID CHUẨN XÁC, KHÔNG GÕ MÙ
        // ==============================================================
        public async Task<bool> ConfigSocksDroidAsync(string adbId, string proxyString, Action<string> log, CancellationToken token)
        {
            if (string.IsNullOrEmpty(proxyString)) return false;

            string ip = "", port = "", user = "", pass = "";
            string[] parts = proxyString.Split(':');
            if (parts.Length >= 2) { ip = parts[0]; port = parts[1]; }
            if (parts.Length >= 4) { user = parts[2]; pass = parts[3]; }

            log($"🧦 [{adbId}] Bơm IP vào SocksDroid: {ip}:{port}");

            try
            {
                log("Mở sáng màn hình và mở khóa (nếu đang tắt)...");
                ADBHelper.ExecuteCMD($"adb -s {adbId} shell input keyevent 224");
                await Task.Delay(1000, token);
                ADBHelper.Swipe(adbId, 500, 2000, 500, 500);
                await Task.Delay(1000, token);

                // 1. Tắt app để làm mới
                await AdbCmdAsync(adbId, "shell am force-stop net.typeblog.socks");
                await Task.Delay(1000, token);

                // 2. Mở app SocksDroid
                await AdbCmdAsync(adbId, "shell monkey -p net.typeblog.socks -c android.intent.category.LAUNCHER 1");
                await Task.Delay(3000, token); // Đợi app lên hẳn

                // 3. Nhập IP (Giả sử tọa độ ô IP là Y=200, bạn có thể sửa lại nếu màn hình máy bạn khác)
                log($"👉 Bấm ô IP và nhập...");
                await AdbCmdAsync(adbId, "shell input tap 380 819");
                await Task.Delay(500, token);
                await ClearEditTextAsync(adbId, 20); // Xóa IP cũ
                await AdbCmdAsync(adbId, $"shell input text \"{ip}\"");
                // (MỚI) Nhập xong -> Tab 2 lần -> Enter để bấm chữ OK
                await AdbCmdAsync(adbId, "shell input keyevent 61"); // Tab 1
                await AdbCmdAsync(adbId, "shell input keyevent 61"); // Tab 2
                await AdbCmdAsync(adbId, "shell input keyevent 66"); // Enter OK
                await Task.Delay(500, token);

                // 4. Nhập Port (Giả sử tọa độ ô Port là Y=350)
                log($"👉 Bấm ô Port và nhập...");
                await AdbCmdAsync(adbId, "shell input tap 257 1073");
                await Task.Delay(500, token);
                await ClearEditTextAsync(adbId, 10); // Xóa Port cũ
                await AdbCmdAsync(adbId, $"shell input text \"{port}\"");
                // (MỚI) Nhập xong -> Tab 2 lần -> Enter để bấm chữ OK
                await AdbCmdAsync(adbId, "shell input keyevent 61"); // Tab 1
                await AdbCmdAsync(adbId, "shell input keyevent 61"); // Tab 2
                await AdbCmdAsync(adbId, "shell input keyevent 66"); // Enter OK
                await Task.Delay(500, token);

                // (Nếu TMProxy cấp User/Pass thì bạn chèn lệnh Tap tọa độ nhập User/Pass tương tự ở đây)

                // 5. Gạt công tắc ON (Nút gạt thường nằm ở góc trên cùng bên phải)
                log($"✅ Đang gạt công tắc kích hoạt VPN...");
                await AdbCmdAsync(adbId, "shell input tap 1194 188"); // Đổi tọa độ X Y cho đúng cái nút gạt trên máy của bạn
                await Task.Delay(1000, token);

                // 6. Xử lý hộp thoại "Connection request" (Chỉ hiện ở lần chạy đầu tiên)
                await AdbCmdAsync(adbId, "shell input keyevent 61"); // Tab 1
                await AdbCmdAsync(adbId, "shell input keyevent 61"); // Tab 2
                await AdbCmdAsync(adbId, "shell input keyevent 61"); // Tab 3
                await AdbCmdAsync(adbId, "shell input keyevent 66"); // Enter OK

                return true;
            }
            catch (Exception ex)
            {
                log($"❌ Lỗi cấu hình SocksDroid: {ex.Message}");
                return false;
            }
        }

        private async Task ClearEditTextAsync(string adbId, int repeat)
        {
            // Di chuyển con trỏ xuống cuối dòng bằng End (keyevent 123) sau đó mới xóa
            await AdbCmdAsync(adbId, "shell input keyevent 123");
            string dels = string.Join(" ", Enumerable.Repeat("67", repeat));
            await AdbCmdAsync(adbId, $"shell input keyevent {dels}");
        }

        public async Task ChangeHotspotNameAsync(string adbId, string newName, Action<string> log, CancellationToken token)
        {
            log($"📡 [{adbId}] Đang cấu hình trạm 4G Hotspot: {newName}");
            await AdbCmdAsync(adbId, "shell am force-stop com.android.settings");
            await AdbCmdAsync(adbId, "shell am start -a android.settings.TETHER_SETTINGS");
            await Task.Delay(2000, token);

            await AdbCmdAsync(adbId, "shell input keyevent 66"); // Vào mục Wifi Hotspot
            await Task.Delay(1000, token);
            await AdbCmdAsync(adbId, "shell input keyevent 20"); // Xuống Hotspot Name
            await AdbCmdAsync(adbId, "shell input keyevent 66"); // Mở Edit
            await ClearEditTextAsync(adbId, 30);
            await AdbCmdAsync(adbId, $"shell input text \"{newName}\"");

            await AdbCmdAsync(adbId, "shell input keyevent 61");
            await AdbCmdAsync(adbId, "shell input keyevent 61");
            await AdbCmdAsync(adbId, "shell input keyevent 66");

            await AdbCmdAsync(adbId, "shell input keyevent 19"); // Trở lên
            await AdbCmdAsync(adbId, "shell input keyevent 66"); // ON/OFF
            await Task.Delay(3000, token);

            await AdbCmdAsync(adbId, "shell am force-stop com.android.settings");
            log($"✅ Phát Wifi 4G [{newName}] thành công!");
        }
    }
}