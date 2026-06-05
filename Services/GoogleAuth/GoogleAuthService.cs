using System;
using System.Threading;
using System.Threading.Tasks;
using AutoTool.Services.Device;
using AutoTool.Services.Vision;

namespace AutoTool.Services.GoogleAuth
{
    public class GoogleAuthService : IDisposable
    {
        public bool IsStop { get; set; } = false;

        public bool UseTestMailFile { get; set; } = false;
        public string TestMailFilePath { get; set; } = "";

        private readonly AdbService _adb;
        private readonly MailApiService _mailApi;

        private readonly GoogleAuthOptions _options;
        private readonly GoogleAuthTemplateStore _templates;

        private readonly ScreenRegionService _regionService;
        private readonly ScreenCaptureService _screenCapture;
        private readonly ImageMatcher _imageMatcher;
        private readonly OcrTextFinder _ocrTextFinder;

        private readonly DeviceCommandService _deviceCommand;
        private readonly DeviceInputService _deviceInput;
        private readonly TestMailFileService _testMailFileService;

        private GoogleAuthVerifier _verifier;
        private GoogleAuthNavigator _navigator;
        private GoogleAuthStateMachine _stateMachine;

        public GoogleAuthService(AdbService adb, MailApiService mailApi)
            : this(adb, mailApi, new GoogleAuthOptions())
        {
        }

        public GoogleAuthService(
            AdbService adb,
            MailApiService mailApi,
            GoogleAuthOptions options)
        {
            _adb = adb;
            _mailApi = mailApi;
            _options = options ?? new GoogleAuthOptions();

            _templates = new GoogleAuthTemplateStore();

            _regionService = new ScreenRegionService();
            _screenCapture = new ScreenCaptureService();
            _imageMatcher = new ImageMatcher(_regionService);
            _ocrTextFinder = new OcrTextFinder();

            _deviceCommand = new DeviceCommandService();
            _deviceInput = new DeviceInputService(_screenCapture);
            _testMailFileService = new TestMailFileService();
        }

        public bool LoadData(string path, Action<string> log)
        {
            bool loaded = _templates.Load(path, log);

            if (!loaded)
            {
                log?.Invoke("❌ GoogleAuthService LoadData thất bại.");
                return false;
            }

            _verifier = new GoogleAuthVerifier(
                _screenCapture,
                _imageMatcher,
                _ocrTextFinder,
                _templates,
                _options);

            _navigator = new GoogleAuthNavigator(
                _deviceInput,
                _deviceCommand,
                _verifier,
                _options);

            _stateMachine = new GoogleAuthStateMachine(
                _navigator,
                _verifier,
                _mailApi,
                _testMailFileService,
                _options);

            log?.Invoke("✅ GoogleAuthService LoadData thành công.");
            return true;
        }

        public async Task<bool> ExecuteLoginAsync(
            string deviceID,
            string mailApiKey,
            string mailProdId,
            Action<string> log,
            CancellationToken token)
        {
            try
            {
                if (_stateMachine == null)
                {
                    log?.Invoke("❌ GoogleAuthService chưa LoadData.");
                    return false;
                }

                _stateMachine.UseTestMailFile = UseTestMailFile;
                _stateMachine.TestMailFilePath = TestMailFilePath;

                if (UseTestMailFile)
                {
                    log?.Invoke("🧪 [GoogleAuth] Đang bật chế độ TEST MAIL từ file TXT.");

                    if (string.IsNullOrWhiteSpace(TestMailFilePath))
                    {
                        log?.Invoke("❌ Bạn đã bật test mail nhưng chưa chọn file TXT.");
                        return false;
                    }
                }
                else
                {
                    log?.Invoke("📧 [GoogleAuth] Đang dùng Mail API như bình thường.");
                }

                log?.Invoke("🚀 [GoogleAuth] Bắt đầu login Google bằng State Machine...");

                bool success = await _stateMachine.RunAsync(
                    deviceID,
                    mailApiKey,
                    mailProdId,
                    () => IsStop,
                    log,
                    token);

                if (success)
                    log?.Invoke("🎉 [GoogleAuth] Login Google thành công.");
                else
                    log?.Invoke("❌ [GoogleAuth] Login Google thất bại.");

                return success;
            }
            catch (OperationCanceledException)
            {
                log?.Invoke("⛔ [GoogleAuth] Đã hủy bởi CancellationToken.");
                return false;
            }
            catch (Exception ex)
            {
                log?.Invoke($"❌ [GoogleAuth] Lỗi: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _templates?.Dispose();
        }
    }
}