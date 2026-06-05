/*using KAutoHelper;
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

namespace AutoTool.Services
{
    public class GoogleAuthService
    {
        public bool IsStop { get; set; } = false;
        private readonly AdbService _adb;
        private readonly MailApiService _mailApi;
        private readonly OcrService _ocr;

        #region Dữ liệu ảnh mẫu
        private string dataPath = "";
        private Image<Gray, byte> KO_CHO_PHEP_MAT;
        private Image<Gray, byte> BO_QUA_MAT;
        private Image<Gray, byte> BO_QUA1_MAT;
        private Image<Gray, byte> BO_QUA2_MAT;
        private Image<Gray, byte> DONG_Y_MAT; // Dùng chung cho "Tôi đồng ý" và "Đồng ý"
        private Image<Gray, byte> HUY_MAT;
        private Image<Gray, byte> EMAIL_KP_MAT;

        // Ảnh mới cho Giai đoạn 6
        private Image<Gray, byte> XEM_THEM_MAT;
        private Image<Gray, byte> CHAP_NHAN_MAT;
        #endregion

        // Cấu trúc phân vùng màn hình
        public enum ScreenRegion { Full, LeftHalf, RightHalf, BottomHalf, BottomRight, BottomLeft, MiddleThird }

        private Rectangle GetRegion(Image<Gray, byte> screen, ScreenRegion regionType)
        {
            int w = screen.Width; int h = screen.Height;
            switch (regionType)
            {
                case ScreenRegion.LeftHalf: return new Rectangle(0, 0, w / 2, h);
                case ScreenRegion.RightHalf: return new Rectangle(w / 2, 0, w / 2, h);
                case ScreenRegion.BottomHalf: return new Rectangle(0, h / 2, w, h / 2);
                case ScreenRegion.BottomRight: return new Rectangle(w / 2, h / 2, w / 2, h / 2);
                case ScreenRegion.BottomLeft: return new Rectangle(0, h / 2, w / 2, h / 2);
                case ScreenRegion.MiddleThird: return new Rectangle(0, h / 3, w, h / 3);
                default: return new Rectangle(0, 0, w, h);
            }
        }

        public GoogleAuthService(AdbService adb, MailApiService mailApi)
        {
            _adb = adb;
            _mailApi = mailApi;
            _ocr = new OcrService();
        }

        public bool LoadData(string path, Action<string> log)
        {
            dataPath = path;
            try
            {
                KO_CHO_PHEP_MAT = LoadImageMat("ko_cho_phep.png", log);
                BO_QUA_MAT = LoadImageMat("bo_qua.png", log);
                BO_QUA1_MAT = LoadImageMat("bo_qua1.png", log);
                BO_QUA2_MAT = LoadImageMat("bo_qua2.png", log);
                DONG_Y_MAT = LoadImageMat("dong_y.png", log);
                HUY_MAT = LoadImageMat("huy.png", log);
                EMAIL_KP_MAT = LoadImageMat("email_kp.png", log);

                XEM_THEM_MAT = LoadImageMat("xem_them.png", log);
                CHAP_NHAN_MAT = LoadImageMat("chap_nhan.png", log);
                return true;
            }
            catch { return false; }
        }

        private Image<Gray, byte> LoadImageMat(string fileName, Action<string> log)
        {
            string fullPath = Path.Combine(dataPath, fileName);
            if (!File.Exists(fullPath)) return null;
            try { using (Bitmap bmp = (Bitmap)Image.FromFile(fullPath)) return new Image<Gray, byte>(bmp); }
            catch { return null; }
        }

        private byte[] GetScreenByte(string deviceID)
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
                        if (ms.Length > 0) return ms.ToArray();
                    }
                }
            }
            catch { }
            return null;
        }

        private Point? FastFindImageInRegion(Image<Gray, byte> screen, Image<Gray, byte> template, Rectangle region, double threshold = 0.8)
        {
            if (screen == null || template == null) return null;
            try
            {
                screen.ROI = region;
                using (Image<Gray, float> result = screen.MatchTemplate(template, TemplateMatchingType.CcoeffNormed))
                {
                    double[] minValues, maxValues; Point[] minLocations, maxLocations;
                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);
                    if (maxValues[0] >= threshold)
                    {
                        screen.ROI = Rectangle.Empty;
                        return new Point(maxLocations[0].X + (template.Width / 2) + region.X, maxLocations[0].Y + (template.Height / 2) + region.Y);
                    }
                }
                screen.ROI = Rectangle.Empty;
            }
            catch { screen.ROI = Rectangle.Empty; }
            return null;
        }

        private async Task HideKeyboardIfOpenAsync(string deviceID, Action<string> log, CancellationToken token)
        {
            await Task.CompletedTask;
        }

        // ==============================================================================
        // LUỒNG CHÍNH LOGIN GOOGLE
        // ==============================================================================
        public async Task<bool> ExecuteLoginAsync(string deviceID, string mailApiKey, string mailProdId, Action<string> log, CancellationToken token)
        {
            try
            {
                bool isEmailAccepted = false;
                (bool success, string email, string password, string recoveryEmail, string error) mail = (false, "", "", "", "");
                bool isRetry = false;

                while (!isEmailAccepted)
                {
                    if (IsStop || token.IsCancellationRequested) return false;

                    // ==============================================================================
                    // 🚀 GIAI ĐOẠN 1: ĐI TÌM Ô NHẬP MAIL
                    // ==============================================================================
                    log("⏳ [GIAI ĐOẠN 1] Đang quét tìm HÌNH 'Bỏ qua' hoặc CHỮ 'tạo'...");
                    DateTime waitStartTime = DateTime.Now;
                    bool readyToInput = false;
                    bool needTab = false;

                    while ((DateTime.Now - waitStartTime).TotalSeconds < 45)
                    {
                        if (IsStop || token.IsCancellationRequested) return false;

                        byte[] screenBytes = GetScreenByte(deviceID);
                        if (screenBytes != null && screenBytes.Length > 0)
                        {
                            using (var ms = new MemoryStream(screenBytes))
                            using (var bmp = new Bitmap(ms))
                            using (var screenMat = new Image<Gray, byte>(bmp))
                            {
                                Point? ptBoQua = FastFindImageInRegion(screenMat, BO_QUA_MAT, GetRegion(screenMat, ScreenRegion.LeftHalf)) ??
                                                 FastFindImageInRegion(screenMat, BO_QUA1_MAT, GetRegion(screenMat, ScreenRegion.LeftHalf)) ??
                                                 FastFindImageInRegion(screenMat, BO_QUA2_MAT, GetRegion(screenMat, ScreenRegion.LeftHalf));

                                if (ptBoQua != null)
                                {
                                    log("🎯 Thấy nút BỎ QUA -> Nhấn và đợi 5s để app chuyển trang...");
                                    ADBHelper.Tap(deviceID, ptBoQua.Value.X, ptBoQua.Value.Y);
                                    await Task.Delay(5000, token);
                                    readyToInput = true;
                                    needTab = false;
                                    break;
                                }

                                Point? ptTao = _ocr.FindTextAndGetPoint(screenBytes, "tạo", 0.6f);
                                if (ptTao != null)
                                {
                                    log("🎯 Thấy chữ TẠO -> Xác định cần Tab 3 lần để vào ô nhập Mail...");
                                    readyToInput = true;
                                    needTab = true;
                                    break;
                                }
                            }
                        }
                        await Task.Delay(400, token);
                    }

                    if (!readyToInput)
                    {
                        log("⚠️ [Dự phòng] Hết 30s không thấy gì, tự động bấm Tab 1 lần vớt vát...");
                        ADBHelper.ExecuteCMD($"adb -s {deviceID} shell input keyevent 61");
                        await Task.Delay(400, token);
                        readyToInput = true;
                    }

                    // ==============================================================================
                    // 📧 GIAI ĐOẠN 2: THUÊ VÀ NHẬP MAIL
                    // ==============================================================================
                    log("📧 [GIAI ĐOẠN 2] Đang gọi API chờ thuê mail mới...");
                    var mailRes = await _mailApi.BuyMailAsync(mailApiKey, mailProdId, token, log);
                    if (!mailRes.success) return false;

                    mail = (mailRes.success, mailRes.email, mailRes.password, mailRes.recoveryEmail, "");

                    if (needTab)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            ADBHelper.ExecuteCMD($"adb -s {deviceID} shell input keyevent 61");
                            await Task.Delay(400, token);
                        }
                    }

                    if (isRetry)
                    {
                        log("🗑️ Vòng lặp lấy lại mail: Xóa sạch mail cũ bằng 40 lần Backspace...");
                        for (int i = 0; i < 40; i++) ADBHelper.ExecuteCMD($"adb -s {deviceID} shell input keyevent 67");
                        await Task.Delay(500, token);
                    }

                    log($"⌨️ Nhập mail: {mail.email}");
                    ADBHelper.InputText(deviceID, mail.email);
                    await Task.Delay(1000, token);
                    ADBHelper.ExecuteCMD($"adb -s {deviceID} shell input keyevent 66");

                    // ==============================================================================
                    // 🛡️ GIAI ĐOẠN 3: KIỂM TRA SỐNG CHẾT
                    // ==============================================================================
                    log("⏳ [GIAI ĐOẠN 3] Đang chờ xác định Mail sống hay chết...");
                    DateTime passWait = DateTime.Now;
                    bool atPass = false;
                    while ((DateTime.Now - passWait).TotalSeconds < 20)
                    {
                        byte[] s = GetScreenByte(deviceID);
                        if (_ocr.FindTextAndGetPoint(s, "thử cách khác", 0.65f) != null)
                        {
                            log("❌ Thấy chữ 'thử cách khác' -> Mail chết -> Back lại để thuê mail mới!");
                            ADBHelper.ExecuteCMD($"adb -s {deviceID} shell input keyevent 4");
                            await Task.Delay(2000, token); isRetry = true; break;
                        }
                        if (_ocr.FindTextAndGetPoint(s, "mật khẩu", 0.6f) != null)
                        {
                            log("✅ Thấy ô 'mật khẩu' -> Mail sống!");
                            atPass = true; break;
                        }
                        await Task.Delay(400, token);
                    }

                    if (isRetry) continue;
                    isEmailAccepted = atPass || true;
                }

                // ==============================================================================
                // 🔑 GIAI ĐOẠN 4: NHẬP PASSWORD
                // ==============================================================================
                log($"🔑 [GIAI ĐOẠN 4] Nhập password...");
                ADBHelper.InputText(deviceID, mail.password);
                await Task.Delay(1500, token);
                ADBHelper.ExecuteCMD($"adb -s {deviceID} shell input keyevent 66");

                // ==============================================================================
                // 🛡️ GIAI ĐOẠN 4.5: CHECK EMAIL KHÔI PHỤC BẢO MẬT (DUAL-SCAN)
                // ==============================================================================
                log("⏳ [GIAI ĐOẠN 4.5] Kiểm tra xem có bắt xác nhận Email Khôi Phục không (Max 12s)...");
                DateTime checkKpStart = DateTime.Now;

                while ((DateTime.Now - checkKpStart).TotalSeconds < 12)
                {
                    if (IsStop || token.IsCancellationRequested) return false;
                    byte[] screenBytes = GetScreenByte(deviceID);

                    if (screenBytes != null && screenBytes.Length > 0)
                    {
                        using (var ms = new MemoryStream(screenBytes))
                        using (var bmp = new Bitmap(ms))
                        using (var screenMat = new Image<Gray, byte>(bmp))
                        {
                            Point? ptEmailKp = FastFindImageInRegion(screenMat, EMAIL_KP_MAT, GetRegion(screenMat, ScreenRegion.Full));
                            if (ptEmailKp != null)
                            {
                                log("🎯 Thấy hình 'Xác nhận email khôi phục' -> Tiến hành vả và nhập...");
                                ADBHelper.Tap(deviceID, ptEmailKp.Value.X, ptEmailKp.Value.Y);
                                await Task.Delay(1500, token);

                                log($"⌨️ Nhập Email KP: {mail.recoveryEmail}");
                                ADBHelper.InputText(deviceID, mail.recoveryEmail);
                                await Task.Delay(1000, token);

                                log("👉 Gửi phím Enter xác nhận...");
                                ADBHelper.ExecuteCMD($"adb -s {deviceID} shell input keyevent 66");
                                await Task.Delay(3000, token);
                                break;
                            }

                            Point? ptDongYSom = FastFindImageInRegion(screenMat, DONG_Y_MAT, GetRegion(screenMat, ScreenRegion.BottomRight), 0.75);
                            if (ptDongYSom != null)
                            {
                                log("⚡ Không bị check bảo mật, đã thấy 'Tôi đồng ý' xuất hiện sớm -> Đi tiếp ngay!");
                                break;
                            }
                        }
                    }
                    await Task.Delay(400, token);
                }

                // ==============================================================================
                // 🔽 GIAI ĐOẠN 5: TÌM "TÔI ĐỒNG Ý" VÀ VẢ
                // ==============================================================================
                log("🔍 [GIAI ĐOẠN 5] Check hình 'Tôi đồng ý' sớm...");
                bool isDongYClicked = false;

                Func<Task<bool>> CheckAndTapDongY = async () =>
                {
                    byte[] screenBytes = GetScreenByte(deviceID);
                    if (screenBytes != null && screenBytes.Length > 0)
                    {
                        using (var ms = new MemoryStream(screenBytes))
                        using (var bmp = new Bitmap(ms))
                        using (var screenMat = new Image<Gray, byte>(bmp))
                        {
                            Point? ptDongY = FastFindImageInRegion(screenMat, DONG_Y_MAT, GetRegion(screenMat, ScreenRegion.BottomRight), 0.75);
                            if (ptDongY != null)
                            {
                                log("🎯 Thấy HÌNH 'Tôi đồng ý' -> Nhấn luôn!");
                                ADBHelper.Tap(deviceID, ptDongY.Value.X, ptDongY.Value.Y);
                                await Task.Delay(2000, token);
                                return true;
                            }
                        }
                        Point? ptTextDongY = _ocr.FindTextAndGetPoint(screenBytes, "đồng ý", 0.6f);
                        if (ptTextDongY != null)
                        {
                            log("🎯 Thấy CHỮ 'đồng ý' -> Nhấn luôn!");
                            ADBHelper.Tap(deviceID, ptTextDongY.Value.X, ptTextDongY.Value.Y);
                            await Task.Delay(2000, token);
                            return true;
                        }
                    }
                    return false;
                };

                // Lần 1: Check sớm
                isDongYClicked = await CheckAndTapDongY();

                // Lần 2: Nếu chưa thấy, vuốt và vả mù
                if (!isDongYClicked)
                {
                    log("🔽 Không thấy 'Tôi đồng ý' sớm -> Vuốt xuống...");
                    ADBHelper.Swipe(deviceID, 746, 2324, 649, 987);
                    await Task.Delay(1500, token);

                    log("👉 Vả tọa độ mù (Đồng ý 1)...");
                    ADBHelper.Tap(deviceID, 1168, 2742);
                    await Task.Delay(1500, token);

                    log("🔍 Chờ tối đa 7s để check lại...");
                    DateTime dyStartTime = DateTime.Now;
                    while ((DateTime.Now - dyStartTime).TotalSeconds < 7)
                    {
                        if (IsStop || token.IsCancellationRequested) return false;
                        isDongYClicked = await CheckAndTapDongY();
                        if (isDongYClicked) break;
                        await Task.Delay(500, token);
                    }

                    // Lần 3: Vẫn không thấy, vuốt phát nữa
                    if (!isDongYClicked)
                    {
                        log("🔽 Vẫn chưa thấy -> Vuốt tiếp...");
                        ADBHelper.Swipe(deviceID, 746, 2324, 649, 987);
                        await Task.Delay(1500, token);

                        log("👉 Vả tọa độ mù (Đồng ý 2)...");
                        ADBHelper.Tap(deviceID, 1168, 2742);
                        await Task.Delay(2000, token);
                    }
                }

                // ==============================================================================
                // ⏭️ GIAI ĐOẠN 5.5: THEO DÕI "HỦY" VÀ "BỎ QUA" 
                // ==============================================================================
                log("🔍 [GIAI ĐOẠN 5.5] Kiểm tra nút Hủy -> Bỏ qua...");
                DateTime checkHuyStart = DateTime.Now;
                bool isHuyClicked = false;

                while ((DateTime.Now - checkHuyStart).TotalSeconds < 10)
                {
                    if (IsStop || token.IsCancellationRequested) return false;

                    byte[] screenBytes = GetScreenByte(deviceID);
                    if (screenBytes != null && screenBytes.Length > 0)
                    {
                        using (var ms = new MemoryStream(screenBytes))
                        using (var bmp = new Bitmap(ms))
                        using (var screenMat = new Image<Gray, byte>(bmp))
                        {
                            Point? ptHuy = FastFindImageInRegion(screenMat, HUY_MAT, GetRegion(screenMat, ScreenRegion.BottomLeft));
                            if (ptHuy != null)
                            {
                                log("🎯 Thấy HÌNH 'Hủy' -> Nhấn Hủy!");
                                ADBHelper.Tap(deviceID, ptHuy.Value.X, ptHuy.Value.Y);
                                await Task.Delay(2000, token);
                                isHuyClicked = true;
                                break;
                            }
                        }
                    }
                    await Task.Delay(400, token);
                }

                if (isHuyClicked)
                {
                    log("🔍 Vừa nhấn Hủy xong -> Tiếp tục tìm nút 'Bỏ qua' (Nửa trái màn hình)...");
                    DateTime checkBoQuaStart = DateTime.Now;
                    while ((DateTime.Now - checkBoQuaStart).TotalSeconds < 10)
                    {
                        if (IsStop || token.IsCancellationRequested) return false;

                        byte[] screenBytes = GetScreenByte(deviceID);
                        if (screenBytes != null && screenBytes.Length > 0)
                        {
                            using (var ms = new MemoryStream(screenBytes))
                            using (var bmp = new Bitmap(ms))
                            using (var screenMat = new Image<Gray, byte>(bmp))
                            {
                                Point? ptBoQua = FastFindImageInRegion(screenMat, BO_QUA_MAT, GetRegion(screenMat, ScreenRegion.LeftHalf)) ??
                                                 FastFindImageInRegion(screenMat, BO_QUA1_MAT, GetRegion(screenMat, ScreenRegion.LeftHalf)) ??
                                                 FastFindImageInRegion(screenMat, BO_QUA2_MAT, GetRegion(screenMat, ScreenRegion.LeftHalf));

                                if (ptBoQua != null)
                                {
                                    log("🎯 Thấy HÌNH 'Bỏ qua' -> Nhấn Bỏ qua!");
                                    ADBHelper.Tap(deviceID, ptBoQua.Value.X, ptBoQua.Value.Y);
                                    await Task.Delay(7000, token);
                                    break;
                                }
                            }
                        }
                        await Task.Delay(400, token);
                    }
                }

                // ==============================================================================
                // ⚔️ GIAI ĐOẠN 6: KIỂM TRA 'XEM THÊM' -> 'CHẤP NHẬN' -> 'TÔI ĐỒNG Ý'
                // ==============================================================================
                log("🔍 [GIAI ĐOẠN 6] Kiểm tra nút 'Xem thêm' trên màn hình Dịch vụ (Max 10s)...");
                DateTime checkXemThemStart = DateTime.Now;
                bool hasXemThem = false;

                while ((DateTime.Now - checkXemThemStart).TotalSeconds < 10)
                {
                    if (IsStop || token.IsCancellationRequested) return false;

                    byte[] screenBytes = GetScreenByte(deviceID);
                    if (screenBytes != null && screenBytes.Length > 0)
                    {
                        using (var ms = new MemoryStream(screenBytes))
                        using (var bmp = new Bitmap(ms))
                        using (var screenMat = new Image<Gray, byte>(bmp))
                        {
                            // Tìm chữ 'Xem thêm' ở nửa dưới màn hình
                            Point? ptXemThem = FastFindImageInRegion(screenMat, XEM_THEM_MAT, GetRegion(screenMat, ScreenRegion.BottomHalf));
                            if (ptXemThem != null)
                            {
                                log("🎯 Thấy nút 'Xem thêm' -> Nhấn!");
                                ADBHelper.Tap(deviceID, ptXemThem.Value.X, ptXemThem.Value.Y);
                                await Task.Delay(1500, token);
                                hasXemThem = true;
                                break; // Nhấn xong thì thoát vòng chờ Xem thêm
                            }
                        }
                    }
                    await Task.Delay(400, token);
                }

                if (hasXemThem)
                {
                    log("🔍 Đã nhấn Xem thêm, tiếp tục tìm nút 'Chấp nhận' (Max 10s)...");
                    DateTime checkChapNhanStart = DateTime.Now;
                    while ((DateTime.Now - checkChapNhanStart).TotalSeconds < 10)
                    {
                        if (IsStop || token.IsCancellationRequested) return false;

                        byte[] screenBytes = GetScreenByte(deviceID);
                        if (screenBytes != null && screenBytes.Length > 0)
                        {
                            using (var ms = new MemoryStream(screenBytes))
                            using (var bmp = new Bitmap(ms))
                            using (var screenMat = new Image<Gray, byte>(bmp))
                            {
                                Point? ptChapNhan = FastFindImageInRegion(screenMat, CHAP_NHAN_MAT, GetRegion(screenMat, ScreenRegion.BottomHalf));
                                if (ptChapNhan != null)
                                {
                                    log("🎯 Thấy nút 'Chấp nhận' -> Nhấn!");
                                    ADBHelper.Tap(deviceID, ptChapNhan.Value.X, ptChapNhan.Value.Y);
                                    await Task.Delay(2000, token);
                                    break; // Xong chấp nhận
                                }
                            }
                        }
                        await Task.Delay(400, token);
                    }
                }
                else
                {
                    log("⚡ Không thấy nút 'Xem thêm' -> Bỏ qua bước kiểm tra 'Chấp nhận'.");
                }

                // Chốt hạ: Tìm và vả "Tôi đồng ý" cuối cùng
                log("🔍 Chờ màn hình 'Tôi đồng ý' cuối cùng nếu có (Max 15s)...");
                DateTime checkDongYCuoiStart = DateTime.Now;
                while ((DateTime.Now - checkDongYCuoiStart).TotalSeconds < 15)
                {
                    if (IsStop || token.IsCancellationRequested) return false;

                    byte[] screenBytes = GetScreenByte(deviceID);
                    if (screenBytes != null && screenBytes.Length > 0)
                    {
                        using (var ms = new MemoryStream(screenBytes))
                        using (var bmp = new Bitmap(ms))
                        using (var screenMat = new Image<Gray, byte>(bmp))
                        {
                            // Vẫn xài chung DONG_Y_MAT theo yêu cầu
                            Point? ptDongYCuoi = FastFindImageInRegion(screenMat, DONG_Y_MAT, GetRegion(screenMat, ScreenRegion.BottomRight), 0.75);
                            if (ptDongYCuoi != null)
                            {
                                log("🎯 Thấy 'Tôi đồng ý' chốt hạ -> Nhấn!");
                                ADBHelper.Tap(deviceID, ptDongYCuoi.Value.X, ptDongYCuoi.Value.Y);
                                await Task.Delay(2000, token);
                                break;
                            }
                        }

                        // Dự phòng OCR
                        Point? ptTextDongYCuoi = _ocr.FindTextAndGetPoint(screenBytes, "đồng ý", 0.6f);
                        if (ptTextDongYCuoi != null)
                        {
                            log("🎯 Thấy CHỮ 'đồng ý' -> Nhấn!");
                            ADBHelper.Tap(deviceID, ptTextDongYCuoi.Value.X, ptTextDongYCuoi.Value.Y);
                            await Task.Delay(2000, token);
                            break;
                        }
                    }
                    await Task.Delay(400, token);
                }

                log("🎉 [Google AI] HOÀN THÀNH LOGIN TRÊN ĐIỆN THOẠI THẬT!");
                return true;
            }
            catch (Exception ex) { log($"❌ Lỗi: {ex.Message}"); return false; }
        }
    }
}*/