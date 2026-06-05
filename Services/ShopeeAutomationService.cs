using KAutoHelper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using AutoTool.Services.GoogleAuth;

namespace AutoTool.Services
{
    public class ShopeeAutomationService
    {
        public bool IsStop { get; set; } = false;

        #region Dữ liệu ảnh của Shopee
        private Image<Gray, byte> KO_CHO_PHEP_MAT;
        private Image<Gray, byte> TIEP_TUC_GG_MAT;
        private Image<Gray, byte> TOI_MAT;
        private Image<Gray, byte> TOI1_MAT;
        private Image<Gray, byte> TOI2_MAT;
        private Image<Gray, byte> DANG_KY_MAT;
        private Image<Gray, byte> BAT_DAU_MAT;

        // Ảnh hệ thống Google Popup (hiện lên sau khi bấm Tiếp tục với GG)
        private Image<Gray, byte> THEM_TAI_KHOAN_MAT;

        private string dataPath = "";
        #endregion

        private readonly AdbService _adb;
        private readonly MailApiService _mailApi;

        // BIẾN CHỨA MODULE GOOGLE LOGIN ĐƯỢC TÁCH RIÊNG
        private readonly GoogleAuthService _googleAuth;

        public enum ScreenRegion { Full, TopHalf, BottomHalf, BottomRight, RightHalf }

        private Rectangle GetRegion(Image<Gray, byte> screen, ScreenRegion regionType)
        {
            int w = screen.Width; int h = screen.Height;
            switch (regionType)
            {
                case ScreenRegion.TopHalf: return new Rectangle(0, 0, w, h / 2);
                case ScreenRegion.BottomHalf: return new Rectangle(0, h / 2, w, h / 2);
                case ScreenRegion.BottomRight: return new Rectangle(w / 2, h / 2, w / 2, h / 2);
                case ScreenRegion.RightHalf: return new Rectangle(w / 2, 0, w / 2, h);
                default: return new Rectangle(0, 0, w, h);
            }
        }

        public ShopeeAutomationService(AdbService adb, MailApiService mailApi)
        {
            _adb = adb;
            _mailApi = mailApi;

            // Khởi tạo Module Google Auth
            _googleAuth = new GoogleAuthService(_adb, _mailApi);
        }

        private Image<Gray, byte> LoadImageMat(string fileName, Action<string> log)
        {
            string fullPath = Path.Combine(dataPath, fileName);
            if (!File.Exists(fullPath)) return null;
            try { using (Bitmap bmp = (Bitmap)Image.FromFile(fullPath)) return new Image<Gray, byte>(bmp); }
            catch { return null; }
        }

        private Image<Gray, byte> GetScreenMat(string deviceID)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo { FileName = "adb", Arguments = $"-s {deviceID} exec-out screencap -p", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                using (Process process = Process.Start(psi))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        process.StandardOutput.BaseStream.CopyTo(ms);
                        process.WaitForExit(2000);
                        if (ms.Length > 0) { ms.Position = 0; using (Bitmap bmp = new Bitmap(ms)) return new Image<Gray, byte>(bmp); }
                    }
                }
            }
            catch { Bitmap backupBmp = ADBHelper.ScreenShoot(deviceID); if (backupBmp != null) { var mat = new Image<Gray, byte>(backupBmp); backupBmp.Dispose(); return mat; } }
            return null;
        }

        private Point? FastFindImage(Image<Gray, byte> screen, Image<Gray, byte> template, double threshold = 0.8)
        {
            if (screen == null || template == null) return null;
            try
            {
                using (Image<Gray, float> result = screen.MatchTemplate(template, TemplateMatchingType.CcoeffNormed))
                {
                    double[] minValues, maxValues; Point[] minLocations, maxLocations;
                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);
                    if (maxValues[0] >= threshold) return new Point(maxLocations[0].X + (template.Width / 2), maxLocations[0].Y + (template.Height / 2));
                }
            }
            catch { }
            return null;
        }

        private Point? FastFindImageInRegion(Image<Gray, byte> screen, Image<Gray, byte> template, Rectangle region, double threshold = 0.8)
        {
            if (screen == null || template == null) return null;
            try { screen.ROI = region; Point? ptCenter = FastFindImage(screen, template, threshold); screen.ROI = Rectangle.Empty; if (ptCenter != null) return new Point(ptCenter.Value.X + region.X, ptCenter.Value.Y + region.Y); }
            catch { screen.ROI = Rectangle.Empty; }
            return null;
        }

        private async Task<string> SmartWaitForAnyAsync(string deviceID, Dictionary<string, (Image<Gray, byte> Img, ScreenRegion Reg)> targets, int maxWaitSeconds, Action<string> log, CancellationToken token)
        {
            DateTime startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < maxWaitSeconds)
            {
                if (IsStop || token.IsCancellationRequested) return null;
                using (var screen = GetScreenMat(deviceID))
                {
                    if (screen != null)
                    {
                        if (KO_CHO_PHEP_MAT != null) { var pt = FastFindImage(screen, KO_CHO_PHEP_MAT); if (pt != null) ADBHelper.Tap(deviceID, pt.Value.X, pt.Value.Y); }
                        foreach (var target in targets)
                        {
                            if (target.Value.Img == null) continue;
                            Point? point = FastFindImageInRegion(screen, target.Value.Img, GetRegion(screen, target.Value.Reg));
                            if (point != null) return target.Key;
                        }
                    }
                }
                await Task.Delay(200, token);
            }
            return null;
        }

        private async Task<bool> SmartWaitAndTap(string deviceID, List<Image<Gray, byte>> templates, string note, int maxWaitSeconds, Action<string> log, CancellationToken token, ScreenRegion regionType = ScreenRegion.Full)
        {
            DateTime startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < maxWaitSeconds)
            {
                if (IsStop || token.IsCancellationRequested) return false;
                using (var screen = GetScreenMat(deviceID))
                {
                    if (screen != null)
                    {
                        if (KO_CHO_PHEP_MAT != null) { var pt = FastFindImage(screen, KO_CHO_PHEP_MAT); if (pt != null) ADBHelper.Tap(deviceID, pt.Value.X, pt.Value.Y); }
                        Rectangle region = GetRegion(screen, regionType);
                        foreach (var temp in templates)
                        {
                            if (temp == null) continue;
                            Point? point = FastFindImageInRegion(screen, temp, region);
                            if (point != null)
                            {
                                log($"🎯 Đã nhấn [{note}]");
                                ADBHelper.Tap(deviceID, point.Value.X, point.Value.Y);
                                await Task.Delay(1000, token);
                                return true;
                            }
                        }
                    }
                }
                await Task.Delay(200, token);
            }
            return false;
        }

        public bool LoadData(string path, Action<string> log)
        {
            dataPath = path;
            try
            {
                if (string.IsNullOrWhiteSpace(dataPath) || !Directory.Exists(dataPath)) return false;

                // Load dữ liệu ảnh của Shopee
                KO_CHO_PHEP_MAT = LoadImageMat("ko_cho_phep.png", log);
                TIEP_TUC_GG_MAT = LoadImageMat("tiep_tuc_voi_gg.png", log);
                TOI_MAT = LoadImageMat("toi.png", log);
                TOI1_MAT = LoadImageMat("toi1.png", log);
                TOI2_MAT = LoadImageMat("toi2.png", log);
                DANG_KY_MAT = LoadImageMat("dang_ky.png", log);
                BAT_DAU_MAT = LoadImageMat("bat_dau.png", log);
                THEM_TAI_KHOAN_MAT = LoadImageMat("them_tai_khoan_khac.png", log);

                // NẠP DATA CHO MODULE GOOGLE AUTH
                _googleAuth.LoadData(path, log);

                return true;
            }
            catch { return false; }
        }

        public async Task<bool> RunRegistrationScriptAsync(string deviceID, string mailApiKey, string mailProdId, Action<string> log, CancellationToken token)
        {
            int maxRetries = 3;
            bool needAppRestart = true; // Cờ kiểm soát việc có phải Force Stop và Mở lại app hay không

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (IsStop || token.IsCancellationRequested) return false;

                    log($"🚀 Bắt đầu kịch bản Shopee (Lần thử {attempt}/{maxRetries})...");
                    bool isQuickGoogle = false;

                    // =========================================================================
                    // 1. CHUẨN BỊ APP HOẶC TIẾP NỐI TRỰC TIẾP
                    // =========================================================================
                    if (needAppRestart)
                    {
                        ADBHelper.ExecuteCMD($"adb -s {deviceID} shell input keyevent 224");
                        await Task.Delay(1000, token);
                        ADBHelper.Swipe(deviceID, 500, 2000, 500, 500);
                        await Task.Delay(1000, token);

                        ADBHelper.ExecuteCMD($"adb -s {deviceID} shell am force-stop com.shopee.vn");
                        await Task.Delay(1000, token);
                        ADBHelper.ExecuteCMD($"adb -s {deviceID} shell am start -n com.shopee.vn/com.shopee.app.ui.home.HomeActivity_");

                        log("🔍 Giăng lưới bắt [Tiếp tục GG sớm] hoặc [Bắt Đầu] (Tối đa 30s)...");
                        var startupTargets = new Dictionary<string, (Image<Gray, byte> Img, ScreenRegion Reg)>
                        {
                            { "GG_Som", (TIEP_TUC_GG_MAT, ScreenRegion.BottomHalf) },
                            { "Bat_Dau", (BAT_DAU_MAT, ScreenRegion.BottomHalf) }
                        };

                        string startResult = await SmartWaitForAnyAsync(deviceID, startupTargets, 30, log, token);

                        if (startResult == "GG_Som")
                        {
                            log("👉 TH1: Xuất hiện Tiếp tục với Google -> Đi thẳng tới trang Login!");
                            await SmartWaitAndTap(deviceID, new List<Image<Gray, byte>> { TIEP_TUC_GG_MAT }, "Tiếp tục với Google", 5, log, token, ScreenRegion.BottomHalf);
                            isQuickGoogle = true;
                        }
                        else if (startResult == "Bat_Dau")
                        {
                            log("👉 TH2: Nhấn Bắt Đầu...");
                            await SmartWaitAndTap(deviceID, new List<Image<Gray, byte>> { BAT_DAU_MAT }, "Bắt đầu", 5, log, token, ScreenRegion.BottomHalf);
                        }
                    }
                    else
                    {
                        log($"🔄 [Lần thử {attempt}] Bỏ qua đóng mở app, sẽ quét và bấm nút Đăng Ký ngay tại đây để đi tiếp...");
                    }

                    // Reset cờ. Nếu đoạn dưới có bất kỳ lỗi nào giữa chừng, vòng sau sẽ Force Stop app cho sạch
                    needAppRestart = true;

                    // =========================================================================
                    // 2. TÌM NÚT ĐĂNG KÝ / TAB TÔI
                    // =========================================================================
                    if (!isQuickGoogle)
                    {
                        log("🔍 Quét NỬA PHẢI check nút Đăng Ký và Tab Tôi (Max 60s)...");
                        DateTime th2Start = DateTime.Now;
                        int countTapToi = 0;

                        while ((DateTime.Now - th2Start).TotalSeconds < 60)
                        {
                            if (IsStop || token.IsCancellationRequested) return false;

                            using (var screen = GetScreenMat(deviceID))
                            {
                                if (screen != null)
                                {
                                    if (KO_CHO_PHEP_MAT != null) { var pt = FastFindImage(screen, KO_CHO_PHEP_MAT); if (pt != null) ADBHelper.Tap(deviceID, pt.Value.X, pt.Value.Y); }

                                    Rectangle rightHalf = GetRegion(screen, ScreenRegion.RightHalf);
                                    Point? ptDangKy = FastFindImageInRegion(screen, DANG_KY_MAT, rightHalf);

                                    if (ptDangKy != null)
                                    {
                                        log("🎯 Đã thấy ĐĂNG KÝ -> Nhấn luôn.");
                                        await Task.Delay(1000, token);
                                        ADBHelper.Tap(deviceID, ptDangKy.Value.X, ptDangKy.Value.Y);
                                        await Task.Delay(2000, token); // Đợi 1 chút cho app phản hồi

                                        // ==========================================================
                                        // LOGIC MỚI: QUÉT ĐỒNG THỜI SAU KHI NHẤN ĐĂNG KÝ
                                        // ==========================================================
                                        bool isReadyToMoveOn = false;
                                        DateTime checkStartTime = DateTime.Now;

                                        // Quét kiểm tra kết quả trong khoảng 15 giây
                                        while ((DateTime.Now - checkStartTime).TotalSeconds < 15)
                                        {
                                            if (IsStop || token.IsCancellationRequested) return false;

                                            using (var checkScreen = GetScreenMat(deviceID))
                                            {
                                                if (checkScreen != null)
                                                {
                                                    var ptGg = FastFindImageInRegion(checkScreen, TIEP_TUC_GG_MAT, GetRegion(checkScreen, ScreenRegion.BottomHalf));
                                                    var ptDkCheck = FastFindImageInRegion(checkScreen, DANG_KY_MAT, GetRegion(checkScreen, ScreenRegion.RightHalf));

                                                    if (ptGg != null)
                                                    {
                                                        log("✅ Thấy nút 'Tiếp tục với Google'. Thực hiện tiếp các bước bình thường.");
                                                        isReadyToMoveOn = true;
                                                        break; // Thoát vòng lặp kiểm tra nhỏ
                                                    }
                                                    else if (ptDkCheck != null)
                                                    {
                                                        log("⚠️ Nút Đăng ký vẫn hiển thị (bị kẹt). Lùi lại 1 bước bằng ADB...");
                                                        ADBHelper.ExecuteCMD($"adb -s {deviceID} shell input keyevent 4");
                                                        await Task.Delay(2000, token); // Đợi animation lùi

                                                        using (var afterBackScreen = GetScreenMat(deviceID))
                                                        {
                                                            if (afterBackScreen != null)
                                                            {
                                                                var ptDkAfterBack = FastFindImageInRegion(afterBackScreen, DANG_KY_MAT, GetRegion(afterBackScreen, ScreenRegion.RightHalf));

                                                                if (ptDkAfterBack != null)
                                                                {
                                                                    log("🔄 Vẫn thấy nút Đăng ký -> Nhấn trực tiếp Đăng ký lần nữa.");
                                                                    ADBHelper.Tap(deviceID, ptDkAfterBack.Value.X, ptDkAfterBack.Value.Y);
                                                                }
                                                                else
                                                                {
                                                                    log("🔄 Không thấy nút Đăng ký -> Nhấn Tab Tôi rồi mới nhấn Đăng ký.");

                                                                    Rectangle botRightCheck = GetRegion(afterBackScreen, ScreenRegion.BottomRight);
                                                                    Point? ptToiCheck = FastFindImageInRegion(afterBackScreen, TOI_MAT, botRightCheck) ??
                                                                                        FastFindImageInRegion(afterBackScreen, TOI1_MAT, botRightCheck) ??
                                                                                        FastFindImageInRegion(afterBackScreen, TOI2_MAT, botRightCheck);

                                                                    if (ptToiCheck != null)
                                                                    {
                                                                        ADBHelper.Tap(deviceID, ptToiCheck.Value.X, ptToiCheck.Value.Y);
                                                                        await Task.Delay(1500, token);

                                                                        // Chụp lại màn hình để nhấn Đăng ký sau khi vào Tab Tôi
                                                                        using (var finalScreen = GetScreenMat(deviceID))
                                                                        {
                                                                            if (finalScreen != null)
                                                                            {
                                                                                var ptDkFinal = FastFindImageInRegion(finalScreen, DANG_KY_MAT, GetRegion(finalScreen, ScreenRegion.RightHalf));
                                                                                if (ptDkFinal != null)
                                                                                {
                                                                                    ADBHelper.Tap(deviceID, ptDkFinal.Value.X, ptDkFinal.Value.Y);
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        await Task.Delay(2000, token); // Chờ app chuyển trang sau khi xử lý xong
                                                    }
                                                }
                                            }
                                            await Task.Delay(500, token);
                                        }

                                        if (isReadyToMoveOn)
                                        {
                                            break; // Thoát hẳn vòng lặp quét 60s to nhất
                                        }
                                    }

                                    // Logic nhấn Tab Tôi nếu chưa thấy Đăng ký
                                    if (countTapToi < 3)
                                    {
                                        Rectangle botRight = GetRegion(screen, ScreenRegion.BottomRight);
                                        Point? ptToi = FastFindImageInRegion(screen, TOI_MAT, botRight) ??
                                                       FastFindImageInRegion(screen, TOI1_MAT, botRight) ??
                                                       FastFindImageInRegion(screen, TOI2_MAT, botRight);

                                        if (ptToi != null)
                                        {
                                            countTapToi++;
                                            log($"🎯 Nhấn Tab Tôi lần {countTapToi}/3");
                                            ADBHelper.Tap(deviceID, ptToi.Value.X, ptToi.Value.Y);
                                            await Task.Delay(1500, token);
                                        }
                                    }
                                }
                            }
                            await Task.Delay(200, token);
                        }

                        log("🔍 Quét NỬA DƯỚI nút Tiếp tục với Google...");
                        await SmartWaitAndTap(deviceID, new List<Image<Gray, byte>> { TIEP_TUC_GG_MAT }, "Tiếp tục với Google", 15, log, token, ScreenRegion.BottomHalf);
                    }

                    // =========================================================================================
                    // 3. BƯỚC ĐỆM: QUÉT POPUP "THÊM TÀI KHOẢN KHÁC"
                    // =========================================================================================
                    log("🔍 [BƯỚC ĐỆM] Kiểm tra popup 'Thêm tài khoản khác' (Max 10s)...");
                    bool clickedThemTk = await SmartWaitAndTap(deviceID, new List<Image<Gray, byte>> { THEM_TAI_KHOAN_MAT }, "Thêm tài khoản khác", 10, log, token, ScreenRegion.Full);
                    if (clickedThemTk)
                    {
                        log("👉 Đã nhấn Thêm tài khoản khác, đang load trang Google Auth...");
                        await Task.Delay(3000, token);
                    }

                    // =========================================================================================
                    // 4. CHUYỂN GIAO QUYỀN CHO MODULE GOOGLE AUTH
                    // =========================================================================================
                    log("🚀 CHUYỂN GIAO QUYỀN ĐIỀU KHIỂN CHO MODULE GOOGLE AUTH...");

                    _googleAuth.IsStop = this.IsStop; // Đồng bộ lệnh Stop
                    bool isGoogleSuccess = await _googleAuth.ExecuteLoginAsync(deviceID, mailApiKey, mailProdId, log, token);

                    if (!isGoogleSuccess)
                    {
                        log("❌ Kịch bản Google Login thất bại! Sẽ Force Stop và mở lại app để thử lại...");
                        continue; // Trở lại đầu vòng lặp. needAppRestart đang là true -> sẽ restart app.
                    }

                    // =========================================================================================
                    // 5. ĐÓNG VÀ MỞ LẠI SHOPEE ĐỂ CHECK KẾT QUẢ
                    // =========================================================================================
                    log("🔄 Đã Login Google xong. Đang đóng và mở lại Shopee để kiểm tra trạng thái...");
                    ADBHelper.ExecuteCMD($"adb -s {deviceID} shell am force-stop com.shopee.vn");
                    await Task.Delay(2000, token);
                    ADBHelper.ExecuteCMD($"adb -s {deviceID} shell am start -n com.shopee.vn/com.shopee.app.ui.home.HomeActivity_");

                    // Bước 5.1: Quét tìm "Bắt Đầu" trong 10s
                    log("🔍 Chờ tìm nút [Bắt Đầu] nếu có (Max 10s)...");
                    await SmartWaitAndTap(deviceID, new List<Image<Gray, byte>> { BAT_DAU_MAT }, "Bắt đầu (sau khi login)", 10, log, token, ScreenRegion.BottomHalf);

                    // Bước 5.2: Chuyển sang Tab Tôi (NHẤN 2 LẦN)
                    log("👉 Chuyển sang Tab [Tôi] để xác minh (Nhấn 2 lần)...");
                    await SmartWaitAndTap(deviceID, new List<Image<Gray, byte>> { TOI_MAT, TOI1_MAT, TOI2_MAT }, "Tab Tôi (Lần 1)", 15, log, token, ScreenRegion.BottomRight);
                    await Task.Delay(1000, token);
                    await SmartWaitAndTap(deviceID, new List<Image<Gray, byte>> { TOI_MAT, TOI1_MAT, TOI2_MAT }, "Tab Tôi (Lần 2)", 5, log, token, ScreenRegion.BottomRight);

                    log("⏳ Đợi 4s cho trang load đầy đủ...");
                    await Task.Delay(4000, token);

                    // Bước 5.3: Chụp màn hình để Check kết quả
                    bool isStillNotLoggedIn = false;
                    using (var screen = GetScreenMat(deviceID))
                    {
                        if (screen != null)
                        {
                            var ptDangKyCheck = FastFindImageInRegion(screen, DANG_KY_MAT, GetRegion(screen, ScreenRegion.RightHalf));
                            var ptGgCheck = FastFindImageInRegion(screen, TIEP_TUC_GG_MAT, GetRegion(screen, ScreenRegion.BottomHalf));

                            if (ptDangKyCheck != null || ptGgCheck != null)
                            {
                                isStillNotLoggedIn = true;
                            }
                        }
                    }

                    if (isStillNotLoggedIn)
                    {
                        log("⚠️ CẢNH BÁO: Vẫn còn thấy nút [Đăng ký] hoặc [Tiếp tục GG]. Tài khoản chưa nhận!");
                        log("🔄 Sẽ KHÔNG khởi động lại app. Tiến tục nhấn Đăng ký ngay để thử lại vòng nữa...");

                        // QUAN TRỌNG: Cờ này giúp vòng lặp kế tiếp không cần Force Stop
                        needAppRestart = false;
                        continue;
                    }
                    else
                    {
                        log("🎉 Xác nhận: KHÔNG còn nút Đăng ký. Tạo tài khoản Shopee thành công!");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    log($"❌ LỖI VÒNG LẶP {attempt}: {ex.Message}");
                    await Task.Delay(2000, token);
                }
            }

            log("❌ Đã hết số lần thử. Kịch bản Shopee thất bại hoàn toàn.");
            return false;
        }
    }
}