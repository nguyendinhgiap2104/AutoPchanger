using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using AutoTool.Models;
using AutoTool.Services.Device;

namespace AutoTool.Services.GoogleAuth
{
    public class GoogleAuthNavigator
    {
        private readonly DeviceInputService _input;
        private readonly DeviceCommandService _command;
        private readonly GoogleAuthVerifier _verifier;
        private readonly GoogleAuthOptions _options;

        public GoogleAuthNavigator(
            DeviceInputService input,
            DeviceCommandService command,
            GoogleAuthVerifier verifier,
            GoogleAuthOptions options)
        {
            _input = input;
            _command = command;
            _verifier = verifier;
            _options = options;
        }

        public async Task<(bool Ready, bool NeedTab)> WaitForEmailInputAsync(
            string deviceId,
            Action<string> log,
            CancellationToken token)
        {
            log?.Invoke("⏳ [GoogleAuth] Đang tìm màn hình nhập email...");

            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds < _options.FindEmailInputTimeoutSeconds)
            {
                token.ThrowIfCancellationRequested();

                byte[] screenBytes = _verifier.CaptureBytes(deviceId, log);

                if (screenBytes != null && screenBytes.Length > 0)
                {
                    using (var screen = _verifier.CaptureMat(deviceId, log))
                    {
                        if (screen != null)
                        {
                            Point? skipPoint = _verifier.FindSkip(screen);

                            if (skipPoint != null)
                            {
                                log?.Invoke("🎯 Thấy nút [Bỏ qua] -> Nhấn.");
                                _input.Tap(deviceId, skipPoint.Value);
                                await Task.Delay(_options.DelayAfterSkipMs, token);

                                return (true, false);
                            }
                        }
                    }

                    if (_verifier.IsCreateTextVisible(screenBytes))
                    {
                        log?.Invoke("🎯 Thấy chữ [tạo] -> Cần Tab 3 lần để focus ô email.");
                        return (true, true);
                    }
                }

                await Task.Delay(_options.PollingDelayMs, token);
            }

            log?.Invoke("⚠️ Không detect được màn hình email, fallback Tab 1 lần.");
            _command.Tab(deviceId);
            await Task.Delay(_options.PollingDelayMs, token);

            return (true, false);
        }

        public async Task InputEmailAsync(
            string deviceId,
            string email,
            bool needTab,
            bool isRetry,
            Action<string> log,
            CancellationToken token)
        {
            if (needTab)
            {
                log?.Invoke("👉 Tab 3 lần để focus ô email.");
                for (int i = 0; i < 3; i++)
                {
                    _command.Tab(deviceId);
                    await Task.Delay(400, token);
                }
            }

            if (isRetry)
            {
                log?.Invoke("🗑️ Xóa email cũ bằng Backspace.");
                for (int i = 0; i < 40; i++)
                    _command.Backspace(deviceId);

                await Task.Delay(500, token);
            }

            log?.Invoke($"⌨️ Nhập email: {email}");
            _input.InputText(deviceId, email);
            await Task.Delay(1000, token);

            _command.Enter(deviceId);
        }

        public async Task InputPasswordAsync(
            string deviceId,
            string password,
            Action<string> log,
            CancellationToken token)
        {
            log?.Invoke("🔑 Nhập password.");
            _input.InputText(deviceId, password);

            await Task.Delay(1500, token);
            _command.Enter(deviceId);
        }

        public async Task<bool> WaitAndHandleRecoveryEmailAsync(
            string deviceId,
            string recoveryEmail,
            Action<string> log,
            CancellationToken token)
        {
            log?.Invoke("🛡️ Kiểm tra yêu cầu email khôi phục...");

            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds < _options.RecoveryEmailTimeoutSeconds)
            {
                token.ThrowIfCancellationRequested();

                byte[] screenBytes = _verifier.CaptureBytes(deviceId, log);

                using (var screen = _verifier.CaptureMat(deviceId, log))
                {
                    if (screen != null)
                    {
                        Point? recoveryPoint = _verifier.FindRecoveryEmail(screen);

                        if (recoveryPoint != null)
                        {
                            log?.Invoke("🎯 Thấy màn xác nhận email khôi phục.");
                            _input.Tap(deviceId, recoveryPoint.Value);

                            await Task.Delay(_options.DelayAfterTapMs, token);

                            log?.Invoke($"⌨️ Nhập recovery email: {recoveryEmail}");
                            _input.InputText(deviceId, recoveryEmail);

                            await Task.Delay(1000, token);

                            _command.Enter(deviceId);

                            await Task.Delay(_options.DelayAfterRecoverySubmitMs, token);

                            return true;
                        }

                        Point? agreePoint = _verifier.FindAgree(screen);
                        if (agreePoint != null)
                        {
                            log?.Invoke("⚡ Không bị check recovery, đã thấy [Tôi đồng ý] sớm.");
                            return false;
                        }
                    }
                }

                await Task.Delay(_options.PollingDelayMs, token);
            }

            log?.Invoke("ℹ️ Không thấy yêu cầu email khôi phục.");
            return false;
        }

        public async Task<bool> TryTapAgreeAsync(
            string deviceId,
            Action<string> log,
            CancellationToken token)
        {
            byte[] screenBytes = _verifier.CaptureBytes(deviceId, log);

            using (var screen = _verifier.CaptureMat(deviceId, log))
            {
                if (screen != null)
                {
                    Point? agreePoint = _verifier.FindAgree(screen);

                    if (agreePoint != null)
                    {
                        log?.Invoke("🎯 Thấy hình [Tôi đồng ý] -> Nhấn.");
                        _input.Tap(deviceId, agreePoint.Value);

                        await Task.Delay(_options.DelayAfterAgreeMs, token);
                        return true;
                    }
                }
            }

            Point? agreeTextPoint = _verifier.FindAgreeByText(screenBytes);

            if (agreeTextPoint != null)
            {
                log?.Invoke("🎯 Thấy chữ [đồng ý] -> Nhấn.");
                _input.Tap(deviceId, agreeTextPoint.Value);

                await Task.Delay(_options.DelayAfterAgreeMs, token);
                return true;
            }

            return false;
        }

        public async Task<bool> AcceptTermsAsync(
            string deviceId,
            Action<string> log,
            CancellationToken token)
        {
            log?.Invoke("🔍 Kiểm tra [Tôi đồng ý] sớm...");

            bool clicked = await TryTapAgreeAsync(deviceId, log, token);

            if (clicked)
                return true;

            log?.Invoke("🔽 Không thấy [Tôi đồng ý] sớm -> Swipe xuống.");
            _input.SwipeByRatio(
                deviceId,
                _options.SwipeStartXRatio,
                _options.SwipeStartYRatio,
                _options.SwipeEndXRatio,
                _options.SwipeEndYRatio);

            await Task.Delay(1500, token);

            log?.Invoke("👉 Fallback tap theo tỉ lệ màn hình.");
            _input.TapByRatio(
                deviceId,
                _options.FallbackAgreeTapXRatio,
                _options.FallbackAgreeTapYRatio);

            await Task.Delay(1500, token);

            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds < 7)
            {
                token.ThrowIfCancellationRequested();

                clicked = await TryTapAgreeAsync(deviceId, log, token);

                if (clicked)
                    return true;

                await Task.Delay(500, token);
            }

            log?.Invoke("🔽 Vẫn chưa thấy [Tôi đồng ý] -> Swipe và fallback tap lần 2.");

            _input.SwipeByRatio(
                deviceId,
                _options.SwipeStartXRatio,
                _options.SwipeStartYRatio,
                _options.SwipeEndXRatio,
                _options.SwipeEndYRatio);

            await Task.Delay(1500, token);

            _input.TapByRatio(
                deviceId,
                _options.FallbackAgreeTapXRatio,
                _options.FallbackAgreeTapYRatio);

            await Task.Delay(2000, token);

            return true;
        }
        public async Task<bool> TryHandleBackupFeaturePromptAsync(
    string deviceId,
    Action<string> log,
    CancellationToken token)
        {
            log?.Invoke("🔍 Kiểm tra màn [Tính năng sao lưu danh bạ]...");

            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds < 15)
            {
                token.ThrowIfCancellationRequested();

                // 1. Bắt bằng ảnh template tinh_nang_sao_luu.png
                using (var screen = _verifier.CaptureMat(deviceId, log))
                {
                    if (screen != null)
                    {
                        Point? backupFeaturePoint = _verifier.FindBackupFeatureScreen(screen);

                        if (backupFeaturePoint != null)
                        {
                            log?.Invoke("🎯 Thấy ảnh [tinh_nang_sao_luu] -> Nhấn [Không bật].");

                            // Màn bạn gửi: nút Không bật nằm góc trái dưới.
                            _input.TapByRatio(deviceId, 0.14, 0.93);

                            await Task.Delay(2500, token);
                            return true;
                        }
                    }
                }

                // 2. Bắt bằng OCR nút "Không bật"
                byte[] screenBytes = _verifier.CaptureBytes(deviceId, log);

                if (screenBytes != null && screenBytes.Length > 0)
                {
                    Point? dontEnablePoint = _verifier.FindText(
                        screenBytes,
                        "Không bật",
                        0.50f);

                    if (dontEnablePoint != null)
                    {
                        log?.Invoke("🎯 OCR thấy nút [Không bật] -> Nhấn.");

                        _input.Tap(deviceId, dontEnablePoint.Value);

                        await Task.Delay(2500, token);
                        return true;
                    }

                    // 3. Bắt bằng OCR tiêu đề/nội dung màn hình
                    Point? lostContactsPoint = _verifier.FindText(
                        screenBytes,
                        "mất danh bạ",
                        0.50f);

                    if (lostContactsPoint != null)
                    {
                        log?.Invoke("🎯 OCR thấy chữ [mất danh bạ] -> Tap fallback [Không bật].");

                        _input.TapByRatio(deviceId, 0.14, 0.93);

                        await Task.Delay(2500, token);
                        return true;
                    }

                    Point? backupPoint = _verifier.FindText(
                        screenBytes,
                        "sao lưu",
                        0.50f);

                    if (backupPoint != null)
                    {
                        log?.Invoke("🎯 OCR thấy chữ [sao lưu] -> Tap fallback [Không bật].");

                        _input.TapByRatio(deviceId, 0.14, 0.93);

                        await Task.Delay(2500, token);
                        return true;
                    }

                    Point? contactsPoint = _verifier.FindText(
                        screenBytes,
                        "danh bạ",
                        0.50f);

                    if (contactsPoint != null)
                    {
                        log?.Invoke("🎯 OCR thấy chữ [danh bạ] -> Tap fallback [Không bật].");

                        _input.TapByRatio(deviceId, 0.14, 0.93);

                        await Task.Delay(2500, token);
                        return true;
                    }
                }

                await Task.Delay(_options.PollingDelayMs, token);
            }

            log?.Invoke("ℹ️ Không gặp màn [Tính năng sao lưu danh bạ], tiếp tục flow bình thường.");
            return false;
        }
        public async Task<bool> TryHandleVideoSelfiePromptAsync(
    string deviceId,
    Action<string> log,
    CancellationToken token)
        {
            log?.Invoke("🔍 Kiểm tra màn [Bảo vệ quyền truy cập bằng video selfie]...");

            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds < 8)
            {
                token.ThrowIfCancellationRequested();

                using (var screen = _verifier.CaptureMat(deviceId, log))
                {
                    if (screen != null)
                    {
                        Point? laterPoint = _verifier.FindLaterButton(screen);

                        if (laterPoint != null)
                        {
                            log?.Invoke("🎯 Thấy ảnh [de_sau] -> Nhấn [Để sau].");

                            _input.Tap(deviceId, laterPoint.Value);

                            await Task.Delay(2000, token);
                            return true;
                        }
                    }
                }

                byte[] screenBytes = _verifier.CaptureBytes(deviceId, log);

                if (screenBytes != null && screenBytes.Length > 0)
                {
                    Point? laterTextPoint = _verifier.FindText(
                        screenBytes,
                        "Để sau",
                        0.55f);

                    if (laterTextPoint != null)
                    {
                        log?.Invoke("🎯 OCR thấy chữ [Để sau] -> Nhấn.");

                        _input.Tap(deviceId, laterTextPoint.Value);

                        await Task.Delay(2000, token);
                        return true;
                    }

                    Point? selfieTitlePoint = _verifier.FindText(
                        screenBytes,
                        "video selfie",
                        0.55f);

                    if (selfieTitlePoint != null)
                    {
                        log?.Invoke("🎯 OCR thấy màn video selfie -> Tap fallback [Để sau].");

                        // Theo ảnh bạn gửi, nút "Để sau" nằm góc trái phía dưới.
                        _input.TapByRatio(deviceId, 0.12, 0.91);

                        await Task.Delay(2000, token);
                        return true;
                    }
                }

                await Task.Delay(_options.PollingDelayMs, token);
            }

            log?.Invoke("ℹ️ Không gặp màn video selfie, tiếp tục flow bình thường.");
            return false;
        }

        public async Task<bool> TryHandleCancelAndSkipAsync(
            string deviceId,
            Action<string> log,
            CancellationToken token)
        {
            log?.Invoke("🔍 Kiểm tra nút [Hủy]...");

            DateTime start = DateTime.Now;
            bool cancelClicked = false;

            while ((DateTime.Now - start).TotalSeconds < _options.CancelTimeoutSeconds)
            {
                token.ThrowIfCancellationRequested();

                using (var screen = _verifier.CaptureMat(deviceId, log))
                {
                    if (screen != null)
                    {
                        Point? cancelPoint = _verifier.FindCancel(screen);

                        if (cancelPoint != null)
                        {
                            log?.Invoke("🎯 Thấy [Hủy] -> Nhấn.");
                            _input.Tap(deviceId, cancelPoint.Value);

                            await Task.Delay(2000, token);
                            cancelClicked = true;
                            break;
                        }
                    }
                }

                await Task.Delay(_options.PollingDelayMs, token);
            }

            if (!cancelClicked)
            {
                log?.Invoke("ℹ️ Không thấy [Hủy], bỏ qua bước này.");
                return false;
            }

            log?.Invoke("🔍 Sau khi nhấn [Hủy], tìm [Bỏ qua]...");

            DateTime skipStart = DateTime.Now;

            while ((DateTime.Now - skipStart).TotalSeconds < _options.SkipAfterCancelTimeoutSeconds)
            {
                token.ThrowIfCancellationRequested();

                using (var screen = _verifier.CaptureMat(deviceId, log))
                {
                    if (screen != null)
                    {
                        Point? skipPoint = _verifier.FindSkip(screen);

                        if (skipPoint != null)
                        {
                            log?.Invoke("🎯 Thấy [Bỏ qua] -> Nhấn.");
                            _input.Tap(deviceId, skipPoint.Value);

                            await Task.Delay(7000, token);
                            return true;
                        }
                    }
                }

                await Task.Delay(_options.PollingDelayMs, token);
            }

            return true;
        }

        public async Task<bool> HandleServiceConsentAsync(
            string deviceId,
            Action<string> log,
            CancellationToken token)
        {
            log?.Invoke("🔍 Kiểm tra [Xem thêm]...");

            DateTime start = DateTime.Now;
            bool hasSeeMore = false;

            while ((DateTime.Now - start).TotalSeconds < _options.SeeMoreTimeoutSeconds)
            {
                token.ThrowIfCancellationRequested();

                using (var screen = _verifier.CaptureMat(deviceId, log))
                {
                    if (screen != null)
                    {
                        Point? seeMorePoint = _verifier.FindSeeMore(screen);

                        if (seeMorePoint != null)
                        {
                            log?.Invoke("🎯 Thấy [Xem thêm] -> Nhấn.");
                            _input.Tap(deviceId, seeMorePoint.Value);

                            await Task.Delay(1500, token);
                            hasSeeMore = true;
                            break;
                        }
                    }
                }

                await Task.Delay(_options.PollingDelayMs, token);
            }

            if (!hasSeeMore)
            {
                log?.Invoke("ℹ️ Không thấy [Xem thêm], bỏ qua [Chấp nhận].");
                return false;
            }

            log?.Invoke("🔍 Tìm [Chấp nhận]...");

            DateTime acceptStart = DateTime.Now;

            while ((DateTime.Now - acceptStart).TotalSeconds < _options.AcceptTimeoutSeconds)
            {
                token.ThrowIfCancellationRequested();

                using (var screen = _verifier.CaptureMat(deviceId, log))
                {
                    if (screen != null)
                    {
                        Point? acceptPoint = _verifier.FindAccept(screen);

                        if (acceptPoint != null)
                        {
                            log?.Invoke("🎯 Thấy [Chấp nhận] -> Nhấn.");
                            _input.Tap(deviceId, acceptPoint.Value);

                            await Task.Delay(2000, token);
                            return true;
                        }
                    }
                }

                await Task.Delay(_options.PollingDelayMs, token);
            }

            log?.Invoke("⚠️ Có [Xem thêm] nhưng không thấy [Chấp nhận].");
            return false;
        }
        public async Task<bool> TryHandleBackupContactsPromptAsync(
    string deviceId,
    Action<string> log,
    CancellationToken token)
        {
            log?.Invoke("🔍 Kiểm tra màn [Không bao giờ bị mất danh bạ]...");

            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds < 8)
            {
                token.ThrowIfCancellationRequested();

                byte[] screenBytes = _verifier.CaptureBytes(deviceId, log);

                if (screenBytes != null && screenBytes.Length > 0)
                {
                    // Ưu tiên tìm đúng nút "Không bật"
                    Point? dontEnablePoint = _verifier.FindText(
                        screenBytes,
                        "Không bật",
                        _options.DefaultOcrConfidence);

                    if (dontEnablePoint != null)
                    {
                        log?.Invoke("🎯 Thấy nút [Không bật] -> Nhấn.");
                        _input.Tap(deviceId, dontEnablePoint.Value);

                        await Task.Delay(2000, token);
                        return true;
                    }

                    // Fallback: nếu thấy tiêu đề màn sao lưu danh bạ
                    Point? titlePoint = _verifier.FindText(
                        screenBytes,
                        "Không bao giờ bị mất danh bạ",
                        0.55f);

                    if (titlePoint != null)
                    {
                        log?.Invoke("🎯 Thấy màn sao lưu danh bạ nhưng OCR chưa thấy nút [Không bật] -> Tap fallback bên trái.");

                        // Theo layout ảnh bạn gửi: nút "Không bật" nằm góc trái phía dưới.
                        _input.TapByRatio(deviceId, 0.16, 0.95);

                        await Task.Delay(2000, token);
                        return true;
                    }

                    // Fallback mềm hơn, phòng OCR chỉ bắt được một phần tiêu đề.
                    Point? partialTitlePoint = _verifier.FindText(
                        screenBytes,
                        "mất danh bạ",
                        0.55f);

                    if (partialTitlePoint != null)
                    {
                        log?.Invoke("🎯 Thấy dấu hiệu màn sao lưu danh bạ -> Tap [Không bật] fallback.");

                        _input.TapByRatio(deviceId, 0.16, 0.95);

                        await Task.Delay(2000, token);
                        return true;
                    }
                }

                await Task.Delay(_options.PollingDelayMs, token);
            }

            log?.Invoke("ℹ️ Không gặp màn sao lưu danh bạ, tiếp tục flow bình thường.");
            return false;
        }

        public async Task<bool> WaitFinalAgreeAsync(
            string deviceId,
            Action<string> log,
            CancellationToken token)
        {
            log?.Invoke("🔍 Chờ [Tôi đồng ý] cuối cùng nếu có...");

            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).TotalSeconds < _options.AgreeTimeoutSeconds)
            {
                token.ThrowIfCancellationRequested();

                bool clicked = await TryTapAgreeAsync(deviceId, log, token);

                if (clicked)
                    return true;

                await Task.Delay(_options.PollingDelayMs, token);
            }

            log?.Invoke("ℹ️ Không thấy [Tôi đồng ý] cuối cùng, coi như đã qua màn.");
            return false;
        }
    }
}