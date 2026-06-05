using System;
using System.Threading;
using System.Threading.Tasks;
using AutoTool.Models;
using AutoTool.Services;

namespace AutoTool.Services.GoogleAuth
{
    public class GoogleAuthStateMachine
    {
        public bool UseTestMailFile { get; set; } = false;
        public string TestMailFilePath { get; set; } = "";

        private readonly GoogleAuthNavigator _navigator;
        private readonly GoogleAuthVerifier _verifier;
        private readonly MailApiService _mailApi;
        private readonly TestMailFileService _testMailFileService;
        private readonly GoogleAuthOptions _options;

        private class GoogleAuthContext
        {
            public string DeviceId { get; set; } = "";
            public string MailApiKey { get; set; } = "";
            public string MailProdId { get; set; } = "";

            public bool NeedTabBeforeEmailInput { get; set; }
            public bool IsRetryEmail { get; set; }
            public int EmailRetryCount { get; set; }

            public MailAccount CurrentMail { get; set; }
        }

        public GoogleAuthStateMachine(
            GoogleAuthNavigator navigator,
            GoogleAuthVerifier verifier,
            MailApiService mailApi,
            TestMailFileService testMailFileService,
            GoogleAuthOptions options)
        {
            _navigator = navigator;
            _verifier = verifier;
            _mailApi = mailApi;
            _testMailFileService = testMailFileService;
            _options = options;
        }

        public async Task<bool> RunAsync(
            string deviceId,
            string mailApiKey,
            string mailProdId,
            Func<bool> shouldStop,
            Action<string> log,
            CancellationToken token)
        {
            var context = new GoogleAuthContext
            {
                DeviceId = deviceId,
                MailApiKey = mailApiKey,
                MailProdId = mailProdId
            };

            GoogleAuthState state = GoogleAuthState.FindEmailInput;

            while (state != GoogleAuthState.Success && state != GoogleAuthState.Failed)
            {
                if (shouldStop?.Invoke() == true || token.IsCancellationRequested)
                    return false;

                StepResult result = await ExecuteStateAsync(state, context, log, token);

                if (!string.IsNullOrWhiteSpace(result.Message))
                    log?.Invoke(result.Message);

                if (result.ShouldAbort)
                    return false;

                state = result.NextState;
            }

            return state == GoogleAuthState.Success;
        }

        private async Task<StepResult> ExecuteStateAsync(
            GoogleAuthState state,
            GoogleAuthContext context,
            Action<string> log,
            CancellationToken token)
        {
            switch (state)
            {
                case GoogleAuthState.FindEmailInput:
                    return await FindEmailInputAsync(context, log, token);

                case GoogleAuthState.BuyAndInputEmail:
                    return await BuyAndInputEmailAsync(context, log, token);

                case GoogleAuthState.VerifyEmailAlive:
                    return await VerifyEmailAliveAsync(context, log, token);

                case GoogleAuthState.InputPassword:
                    return await InputPasswordAsync(context, log, token);

                case GoogleAuthState.HandleRecoveryEmail:
                    return await HandleRecoveryEmailAsync(context, log, token);

                case GoogleAuthState.AcceptTerms:
                    return await AcceptTermsAsync(context, log, token);

                case GoogleAuthState.HandleCancelAndSkip:
                    return await HandleCancelAndSkipAsync(context, log, token);

                case GoogleAuthState.HandleServiceConsent:
                    return await HandleServiceConsentAsync(context, log, token);

                case GoogleAuthState.FinalAgree:
                    return await FinalAgreeAsync(context, log, token);

                default:
                    return StepResult.Fail($"❌ State không hợp lệ: {state}");
            }
        }

        private async Task<StepResult> FindEmailInputAsync(
            GoogleAuthContext context,
            Action<string> log,
            CancellationToken token)
        {
            var result = await _navigator.WaitForEmailInputAsync(
                context.DeviceId,
                log,
                token);

            if (!result.Ready)
                return StepResult.Fail("❌ Không vào được màn hình nhập email.");

            context.NeedTabBeforeEmailInput = result.NeedTab;

            return StepResult.GoTo(
                GoogleAuthState.BuyAndInputEmail,
                "✅ Đã sẵn sàng nhập email.");
        }

        private async Task<StepResult> BuyAndInputEmailAsync(
            GoogleAuthContext context,
            Action<string> log,
            CancellationToken token)
        {
            log?.Invoke("📧 Đang chuẩn bị mail đăng nhập Google...");

            if (UseTestMailFile)
            {
                log?.Invoke("🧪 Đang lấy mail test từ file TXT...");

                if (!_testMailFileService.TryGetNextMail(
                        TestMailFilePath,
                        log,
                        out TestMailAccount testMail))
                {
                    return StepResult.Fail("❌ Không lấy được mail test từ file TXT.");
                }

                context.CurrentMail = new MailAccount
                {
                    Email = testMail.Email,
                    Password = testMail.Password,
                    RecoveryEmail = testMail.RecoveryEmail
                };
            }
            else
            {
                if (context.EmailRetryCount >= _options.MaxEmailRetry)
                {
                    return StepResult.Fail($"❌ Vượt quá số lần retry mail: {_options.MaxEmailRetry}");
                }

                log?.Invoke("📧 Đang gọi API thuê mail mới...");

                var mailRes = await _mailApi.BuyMailAsync(
                    context.MailApiKey,
                    context.MailProdId,
                    token,
                    log);

                if (!mailRes.success)
                    return StepResult.Fail("❌ Thuê mail bằng API thất bại.");

                context.CurrentMail = new MailAccount
                {
                    Email = mailRes.email,
                    Password = mailRes.password,
                    RecoveryEmail = mailRes.recoveryEmail
                };
            }

            if (context.CurrentMail == null || !context.CurrentMail.IsValid())
                return StepResult.Fail("❌ Mail thiếu email hoặc password.");

            await _navigator.InputEmailAsync(
                context.DeviceId,
                context.CurrentMail.Email,
                context.NeedTabBeforeEmailInput,
                context.IsRetryEmail,
                log,
                token);

            return StepResult.GoTo(
                GoogleAuthState.VerifyEmailAlive,
                "✅ Đã nhập email, chuyển sang kiểm tra mail sống/chết.");
        }

        private async Task<StepResult> VerifyEmailAliveAsync(
            GoogleAuthContext context,
            Action<string> log,
            CancellationToken token)
        {
            log?.Invoke("🛡️ Đang kiểm tra mail sống/chết...");

            DateTime start = DateTime.Now;
            bool atPass = false;
            bool isRetry = false;

            while ((DateTime.Now - start).TotalSeconds < _options.VerifyEmailTimeoutSeconds)
            {
                token.ThrowIfCancellationRequested();

                byte[] screenBytes = _verifier.CaptureBytes(context.DeviceId, log);

                if (_verifier.IsDeadEmailScreen(screenBytes))
                {
                    context.EmailRetryCount++;
                    context.IsRetryEmail = true;
                    isRetry = true;

                    log?.Invoke("❌ Thấy [thử cách khác] -> Mail chết -> Back lại để lấy mail mới.");

                    KAutoHelper.ADBHelper.ExecuteCMD($"adb -s {context.DeviceId} shell input keyevent 4");

                    await Task.Delay(2000, token);
                    break;
                }

                if (_verifier.IsPasswordScreen(screenBytes))
                {
                    log?.Invoke("✅ Thấy ô [mật khẩu] -> Mail sống.");
                    atPass = true;
                    break;
                }

                await Task.Delay(_options.PollingDelayMs, token);
            }

            if (isRetry)
            {
                return StepResult.RetryEmail(
                    GoogleAuthState.FindEmailInput,
                    $"🔄 Retry mail lần {context.EmailRetryCount}/{_options.MaxEmailRetry}");
            }

            // Giữ nguyên behavior cũ: file gốc dùng isEmailAccepted = atPass || true,
            // nghĩa là dù chưa OCR thấy password thì vẫn đi tiếp nhập password.
            bool isEmailAccepted = atPass || true;

            if (isEmailAccepted)
            {
                return StepResult.GoTo(
                    GoogleAuthState.InputPassword,
                    "➡️ Giữ nguyên behavior cũ: tiếp tục sang bước nhập password.");
            }

            return StepResult.Fail("❌ Không xác định được trạng thái email.");
        }

        private async Task<StepResult> InputPasswordAsync(
    GoogleAuthContext context,
    Action<string> log,
    CancellationToken token)
        {
            if (context.CurrentMail == null || !context.CurrentMail.IsValid())
                return StepResult.Fail("❌ Không có thông tin mail để nhập password.");

            await _navigator.InputPasswordAsync(
                context.DeviceId,
                context.CurrentMail.Password,
                log,
                token);

            // Màn phát sinh sau khi nhập password:
            // "Không bao giờ bị mất danh bạ" -> chọn "Không bật".
            // Nếu không gặp màn này thì method tự timeout ngắn và flow chạy tiếp bình thường.
            await _navigator.TryHandleBackupContactsPromptAsync(
                context.DeviceId,
                log,
                token);

            return StepResult.GoTo(
                GoogleAuthState.HandleRecoveryEmail,
                "✅ Đã nhập password và đã kiểm tra màn sao lưu danh bạ.");
        }

        private async Task<StepResult> HandleRecoveryEmailAsync(
            GoogleAuthContext context,
            Action<string> log,
            CancellationToken token)
        {
            if (context.CurrentMail == null)
                return StepResult.Fail("❌ Không có thông tin recovery email.");

            await _navigator.WaitAndHandleRecoveryEmailAsync(
                context.DeviceId,
                context.CurrentMail.RecoveryEmail,
                log,
                token);

            return StepResult.GoTo(
                GoogleAuthState.AcceptTerms,
                "✅ Hoàn tất kiểm tra recovery email.");
        }

        private async Task<StepResult> AcceptTermsAsync(
    GoogleAuthContext context,
    Action<string> log,
    CancellationToken token)
        {
            await _navigator.AcceptTermsAsync(
                context.DeviceId,
                log,
                token);

            // Sau khi nhấn "Tôi đồng ý" sớm, có thể gặp màn:
            // 1. Không bao giờ bị mất danh bạ -> nhấn Không bật
            // 2. Bảo vệ quyền truy cập bằng video selfie -> nhấn Để sau
            await _navigator.TryHandleBackupFeaturePromptAsync(
                context.DeviceId,
                log,
                token);

            await _navigator.TryHandleVideoSelfiePromptAsync(
                context.DeviceId,
                log,
                token);

            return StepResult.GoTo(
                GoogleAuthState.HandleCancelAndSkip,
                "✅ Hoàn tất bước [Tôi đồng ý] và kiểm tra các popup sau đồng ý.");
        }

        private async Task<StepResult> HandleCancelAndSkipAsync(
            GoogleAuthContext context,
            Action<string> log,
            CancellationToken token)
        {
            await _navigator.TryHandleCancelAndSkipAsync(
                context.DeviceId,
                log,
                token);

            return StepResult.GoTo(
                GoogleAuthState.HandleServiceConsent,
                "✅ Hoàn tất kiểm tra [Hủy] / [Bỏ qua].");
        }

        private async Task<StepResult> HandleServiceConsentAsync(
            GoogleAuthContext context,
            Action<string> log,
            CancellationToken token)
        {
            await _navigator.HandleServiceConsentAsync(
                context.DeviceId,
                log,
                token);

            return StepResult.GoTo(
                GoogleAuthState.FinalAgree,
                "✅ Hoàn tất kiểm tra [Xem thêm] / [Chấp nhận].");
        }

        private async Task<StepResult> FinalAgreeAsync(
            GoogleAuthContext context,
            Action<string> log,
            CancellationToken token)
        {
            await _navigator.WaitFinalAgreeAsync(
                context.DeviceId,
                log,
                token);

            return StepResult.GoTo(
                GoogleAuthState.Success,
                "🎉 [GoogleAuth] Hoàn thành login Google.");
        }
    }
}