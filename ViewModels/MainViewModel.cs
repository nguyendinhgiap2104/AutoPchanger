using AutoTool.Helpers;
using AutoTool.Models;
using AutoTool.Services;
using AutoTool.Services.GoogleAuth;
using AutoTool.Services.SmsProviders;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AutoTool.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<DeviceModel> Devices { get; set; } = new ObservableCollection<DeviceModel>();
        public ObservableCollection<LogItem> Logs { get; set; } = new ObservableCollection<LogItem>();
        public ObservableCollection<string> ApkFiles { get; set; } = new ObservableCollection<string>();

        private AppSettings _config;

        private void SaveConfig()
        {
            AppConfigManager.Save(_config);
        }

        public string BaseUrl
        {
            get => _config.BaseUrl;
            set
            {
                _config.BaseUrl = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public string AdbPath
        {
            get => _config.AdbPath;
            set
            {
                _config.AdbPath = value;
                _adbService.AdbPath = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public string ImageFolderPath
        {
            get => _config.ImageFolderPath;
            set
            {
                _config.ImageFolderPath = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public string RestoreFilePath
        {
            get => _config.RestoreFilePath;
            set
            {
                _config.RestoreFilePath = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public string OpenVPNPath
        {
            get => _config.OpenVPNPath;
            set
            {
                _config.OpenVPNPath = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public string Brand
        {
            get => _config.Brand;
            set
            {
                _config.Brand = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public string BackupPrefix
        {
            get => _config.BackupPrefix;
            set
            {
                _config.BackupPrefix = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public int NghiSauVong
        {
            get => _config.NghiSauVong;
            set
            {
                _config.NghiSauVong = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public int PhutNghi
        {
            get => _config.PhutNghi;
            set
            {
                _config.PhutNghi = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public string LatLon
        {
            get => _config.LatLon;
            set
            {
                _config.LatLon = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public int DelaySeconds
        {
            get => _config.DelaySeconds;
            set
            {
                _config.DelaySeconds = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        private int _dungHanSauVong = 0;
        public int DungHanSauVong
        {
            get => _dungHanSauVong;
            set
            {
                _dungHanSauVong = value;
                OnPropertyChanged();
            }
        }

        public bool IsShopee
        {
            get => _config.IsShopee;
            set
            {
                _config.IsShopee = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public bool IsAddPhone
        {
            get => _config.IsAddPhone;
            set
            {
                _config.IsAddPhone = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public bool IsTikTok
        {
            get => _config.IsTikTok;
            set
            {
                _config.IsTikTok = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public bool IsNuoiShopee
        {
            get => _config.IsNuoiShopee;
            set
            {
                _config.IsNuoiShopee = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public bool IsSerialAuto
        {
            get => _config.IsSerialAuto;
            set
            {
                _config.IsSerialAuto = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public string MailApiKey
        {
            get => _config.MailApiKey;
            set
            {
                _config.MailApiKey = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public string MailProductId
        {
            get => _config.MailProductId;
            set
            {
                _config.MailProductId = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public string OpenAIApiKey
        {
            get => _config.OpenAIApiKey;
            set
            {
                _config.OpenAIApiKey = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        private bool _useTestMailFile = false;
        public bool UseTestMailFile
        {
            get => _useTestMailFile;
            set
            {
                _useTestMailFile = value;
                OnPropertyChanged();
            }
        }

        private string _testMailFilePath = "";
        public string TestMailFilePath
        {
            get => _testMailFilePath;
            set
            {
                _testMailFilePath = value;
                OnPropertyChanged();
            }
        }

        public string SelectedSmsProvider { get; set; } = "Otis";

        public bool IsNetwork4G
        {
            get => _config.Use4GHotspot;
            set
            {
                _config.Use4GHotspot = value;
                SaveConfig();
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNetworkWifi));
            }
        }

        public bool IsNetworkWifi
        {
            get => !_config.Use4GHotspot;
            set
            {
                _config.Use4GHotspot = !value;
                SaveConfig();
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNetwork4G));
            }
        }

        private string _targetWifiSsid = "";
        public string TargetWifiSsid
        {
            get => _targetWifiSsid;
            set
            {
                _targetWifiSsid = value;
                OnPropertyChanged();
            }
        }

        private string _targetWifiPassword = "";
        public string TargetWifiPassword
        {
            get => _targetWifiPassword;
            set
            {
                _targetWifiPassword = value;
                OnPropertyChanged();
            }
        }

        private bool _useProxy = false;
        public bool UseProxy
        {
            get => _useProxy;
            set
            {
                _useProxy = value;
                OnPropertyChanged();
            }
        }

        public bool UseTMProxy
        {
            get => _config.UseTMProxy;
            set
            {
                _config.UseTMProxy = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public bool UseProxyNo1
        {
            get => _config.UseProxyNo1;
            set
            {
                _config.UseProxyNo1 = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public string ProxyApiKey
        {
            get => _config.ProxyApiKey;
            set
            {
                _config.ProxyApiKey = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public bool Use4GHotspot
        {
            get => _config.Use4GHotspot;
            set
            {
                _config.Use4GHotspot = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public int WifiDuration
        {
            get => _config.WifiDuration;
            set
            {
                _config.WifiDuration = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public string HotspotName
        {
            get => _config.HotspotName;
            set
            {
                _config.HotspotName = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public bool UseSocksDroid
        {
            get => _config.UseSocksDroid;
            set
            {
                _config.UseSocksDroid = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public bool UseOpenVPN
        {
            get => _config.UseOpenVPN;
            set
            {
                _config.UseOpenVPN = value;
                SaveConfig();
                OnPropertyChanged();
            }
        }

        public List<string> RunModes { get; set; } = new List<string> { "Backup", "Restore and Backup" };
        public string SelectedRunMode { get; set; } = "Backup";

        private Queue<string> _restoreQueue = new Queue<string>();

        private string _newKey;
        public string NewKey
        {
            get => _newKey;
            set
            {
                _newKey = value;
                OnPropertyChanged();
            }
        }

        public IAsyncCommand StartAllCommand { get; }
        public IAsyncCommand StopAllCommand { get; }
        public IAsyncCommand AddKeyCommand { get; }
        public IAsyncCommand TestShopeeCommand { get; }
        public IAsyncCommand TestAddPhoneCommand { get; }
        public IAsyncCommand TestGoogleCommand { get; }
        public IAsyncCommand ImportDataCommand { get; }
        public IAsyncCommand ExportDataCommand { get; }

        private readonly AdbService _adbService;
        private readonly PChangerApiService _apiService;
        private readonly DeviceMonitorService _monitorService;
        private readonly MailApiService _mailApi;
        private readonly SmsManager _smsManager;
        private readonly CaptchaService _captchaService;
        private readonly NetworkService _networkService;
        private readonly ProxyService _proxyService;
        private readonly ShopeeAutomationService _shopeeAuto;
        private readonly Services.GoogleAuth.GoogleAuthService _googleAuth;

        private CancellationTokenSource _cts;

        public MainViewModel()
        {
            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 |
                System.Net.SecurityProtocolType.Tls11 |
                System.Net.SecurityProtocolType.Tls;

            _config = AppConfigManager.Load();

            _adbService = new AdbService() { AdbPath = this.AdbPath };
            _apiService = new PChangerApiService();
            _mailApi = new MailApiService();
            _smsManager = new SmsManager();
            _captchaService = new CaptchaService();
            _proxyService = new ProxyService();
            _networkService = new NetworkService(_adbService);

            _shopeeAuto = new ShopeeAutomationService(_adbService, _mailApi);
            _googleAuth = new Services.GoogleAuth.GoogleAuthService(_adbService, _mailApi);

            Environment.SetEnvironmentVariable("ANDROID_ADB_SERVER_PORT", "5037");

            StartAllCommand = new AsyncRelayCommand(StartAllAsync);
            StopAllCommand = new AsyncRelayCommand(StopAllAsync);
            AddKeyCommand = new AsyncRelayCommand(AddKeyAsync);
            TestShopeeCommand = new AsyncRelayCommand(TestShopeeAsync);
            TestAddPhoneCommand = new AsyncRelayCommand(TestAddPhoneAsync);
            TestGoogleCommand = new AsyncRelayCommand(TestGoogleAsync);
            ImportDataCommand = new AsyncRelayCommand(ImportDataAsync);
            ExportDataCommand = new AsyncRelayCommand(ExportDataAsync);

            _monitorService = new DeviceMonitorService(_adbService);
            Task.Run(() => _monitorService.MonitorAsync(Devices, CancellationToken.None));

            AddLog("🚀 SYSTEM READY - Chế độ ĐƠN LUỒNG đã sẵn sàng");
        }

        private async Task ImportDataAsync()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Text/CSV Files|*.txt;*.csv|All Files|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                var lines = File.ReadAllLines(ofd.FileName);
                int count = 0;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.Contains("Name|Location"))
                        continue;

                    var parts = line.Split('|', ',');
                    string key = parts[0].Trim();

                    if (!Devices.Any(d => d.Key == key))
                    {
                        Devices.Add(new DeviceModel { Key = key, IsSelected = true });
                        count++;
                    }
                }

                AddLog($"📥 Đã import thành công {count} thiết bị.");
            }

            await Task.CompletedTask;
        }

        private async Task ExportDataAsync()
        {
            string targetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Export");

            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            string filePath = Path.Combine(targetPath, $"Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            var lines = new List<string> { "Key,AdbId,Status,Progress,LoopCount" };

            foreach (var d in Devices)
            {
                lines.Add($"{d.Key},{d.AdbId},{d.Status},{d.StepInfo},{d.LoopCount}");
            }

            File.WriteAllLines(filePath, lines, System.Text.Encoding.UTF8);
            AddLog($"📤 Đã xuất file thành công: {filePath}", "#00E676");

            await Task.CompletedTask;
        }

        private async Task AddKeyAsync()
        {
            if (!string.IsNullOrWhiteSpace(NewKey) && !Devices.Any(d => d.Key == NewKey))
            {
                Devices.Add(new DeviceModel
                {
                    Key = NewKey.Trim(),
                    IsSelected = true
                });

                AddLog($"➕ Đã thêm key: {NewKey}");
                NewKey = "";
            }

            await Task.CompletedTask;
        }

        private async Task StartAllAsync()
        {
            var selected = Devices.Where(d => d.IsSelected).ToList();

            if (!selected.Any())
            {
                AddLog("⚠ Vui lòng chọn ít nhất 1 thiết bị!", "#FF5252");
                return;
            }

            if (SelectedRunMode == "Restore and Backup")
            {
                if (string.IsNullOrWhiteSpace(RestoreFilePath) || !File.Exists(RestoreFilePath))
                {
                    AddLog("⚠ Vui lòng chọn File .txt chứa danh sách tên Restore!", "#FF5252");
                    return;
                }

                var lines = File.ReadAllLines(RestoreFilePath)
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Select(l => l.Trim())
                    .ToList();

                if (lines.Count == 0)
                {
                    AddLog("⚠ File Restore trống!", "#FF5252");
                    return;
                }

                _restoreQueue = new Queue<string>(lines);
            }

            _shopeeAuto.IsStop = false;

            if (!_shopeeAuto.LoadData(ImageFolderPath, msg => AddLog(msg, "#F57C00")))
                return;

            if (_cts != null)
                _cts.Cancel();

            _cts = new CancellationTokenSource();

            AddLog($"🚀 BẮT ĐẦU CHẠY TUẦN TỰ {selected.Count} THIẾT BỊ...");

            _ = Task.Run(() => MasterSequentialLoopAsync(selected, _cts.Token));

            await Task.CompletedTask;
        }

        private async Task StopAllAsync()
        {
            _shopeeAuto.IsStop = true;

            if (_cts != null)
                _cts.Cancel();

            AddLog("🛑 ĐÃ GỬI LỆNH DỪNG TOÀN BỘ", "#FF5252");

            foreach (var d in Devices)
            {
                UpdateDeviceStatus(d, "STOPPED", "Đã dừng");
            }

            await Task.CompletedTask;
        }

        private async Task TestShopeeAsync()
        {
            var device = Devices.FirstOrDefault(d => d.IsSelected);

            if (device == null)
            {
                AddLog("⚠ Hãy chọn 1 device để test!", "#FF5252");
                return;
            }

            _shopeeAuto.IsStop = false;

            if (!_shopeeAuto.LoadData(ImageFolderPath, msg => AddLog(msg, "#F57C00")))
                return;

            AddLog($"[{device.Key}] 🧪 TEST KỊCH BẢN SHOPEE AUTO (CHỈ MAIL)...", "#F57C00");

            if (!await WaitForAdb(device, CancellationToken.None))
                return;

            await Task.Run(async () =>
            {
                try
                {
                    if (!Use4GHotspot)
                    {
                        AddLog($"[{device.Key}] 🌐 [Test] Bật Wi-Fi...");
                        KAutoHelper.ADBHelper.ExecuteCMD($"adb -s {device.AdbId} shell svc wifi enable");

                        await Task.Delay(3000, CancellationToken.None);

                        if (!string.IsNullOrWhiteSpace(TargetWifiSsid))
                        {
                            AddLog($"[{device.Key}] 🔄 Đang kết nối vào Wi-Fi: {TargetWifiSsid}...");

                            string connectCmd =
                                $"adb -s {device.AdbId} shell cmd wifi connect-network \"{TargetWifiSsid}\" wpa2 \"{TargetWifiPassword}\"";

                            KAutoHelper.ADBHelper.ExecuteCMD(connectCmd);

                            await Task.Delay(5000, CancellationToken.None);
                        }
                    }

                    await _shopeeAuto.RunRegistrationScriptAsync(
                        device.AdbId,
                        MailApiKey,
                        MailProductId,
                        msg => AddLog($"[{device.Key}] {msg}"),
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    AddLog($"[{device.Key}] ❌ Lỗi Test Shopee: {ex.Message}", "#FF5252");
                }
            });
        }

        private async Task TestAddPhoneAsync()
        {
            var device = Devices.FirstOrDefault(d => d.IsSelected);

            if (device == null)
            {
                AddLog("⚠ Hãy chọn 1 device để test!", "#FF5252");
                return;
            }

            AddLog($"[{device.Key}] 📱 TEST KỊCH BẢN THÊM SĐT & CAPTCHA...", "#F57C00");

            if (!await WaitForAdb(device, CancellationToken.None))
                return;

            await Task.Run(async () =>
            {
                try
                {
                    ISmsProvider smsApi = _smsManager.GetProvider(SelectedSmsProvider);
                    ShopeeAddPhoneService testSvc = new ShopeeAddPhoneService(_adbService, smsApi, _captchaService);

                    await testSvc.ExecuteAddPhoneFlowAsync(
                        device.AdbId,
                        MailApiKey,
                        MailProductId,
                        "viettel",
                        OpenAIApiKey,
                        msg => AddLog($"[{device.Key}] {msg}"),
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    AddLog($"[{device.Key}] ❌ Lỗi Test SĐT: {ex.Message}", "#FF5252");
                }
            });
        }

        private async Task TestGoogleAsync()
        {
            var device = Devices.FirstOrDefault(d => d.IsSelected);

            if (device == null)
            {
                AddLog("⚠ Hãy chọn 1 device để test!", "#FF5252");
                return;
            }

            _googleAuth.IsStop = false;

            if (!_googleAuth.LoadData(ImageFolderPath, msg => AddLog(msg, "#F57C00")))
                return;

            AddLog($"[{device.Key}] 🧪 TEST KỊCH BẢN GOOGLE LOGIN ĐỘC LẬP...", "#F57C00");

            if (UseTestMailFile)
            {
                if (string.IsNullOrWhiteSpace(TestMailFilePath) || !File.Exists(TestMailFilePath))
                {
                    AddLog("❌ Bạn đã bật dùng mail test nhưng chưa chọn file TXT hợp lệ.", "#FF5252");
                    return;
                }

                AddLog($"[{device.Key}] 🧪 Test Google sẽ dùng mail từ file TXT: {TestMailFilePath}", "#F57C00");
            }
            else
            {
                AddLog($"[{device.Key}] 📧 Test Google sẽ dùng Mail API như bình thường.", "#4FC3F7");
            }

            if (!await WaitForAdb(device, CancellationToken.None))
                return;

            await Task.Run(async () =>
            {
                try
                {
                    if (!Use4GHotspot)
                    {
                        AddLog($"[{device.Key}] 🌐 [Test] Bật Wi-Fi...");
                        KAutoHelper.ADBHelper.ExecuteCMD($"adb -s {device.AdbId} shell svc wifi enable");

                        await Task.Delay(3000, CancellationToken.None);

                        if (!string.IsNullOrWhiteSpace(TargetWifiSsid))
                        {
                            AddLog($"[{device.Key}] 🔄 Đang kết nối vào Wi-Fi: {TargetWifiSsid}...");

                            string connectCmd =
                                $"adb -s {device.AdbId} shell cmd wifi connect-network \"{TargetWifiSsid}\" wpa2 \"{TargetWifiPassword}\"";

                            KAutoHelper.ADBHelper.ExecuteCMD(connectCmd);

                            await Task.Delay(5000, CancellationToken.None);
                        }
                    }

                    _googleAuth.UseTestMailFile = UseTestMailFile;
                    _googleAuth.TestMailFilePath = TestMailFilePath;

                    await _googleAuth.ExecuteLoginAsync(
                        device.AdbId,
                        MailApiKey,
                        MailProductId,
                        msg => AddLog($"[{device.Key}] {msg}"),
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    AddLog($"[{device.Key}] ❌ Lỗi Test Google: {ex.Message}", "#FF5252");
                }
            });
        }

        private async Task MasterSequentialLoopAsync(List<DeviceModel> activeDevices, CancellationToken token)
        {
            int globalLoop = 0;

            try
            {
                while (!token.IsCancellationRequested && !_shopeeAuto.IsStop)
                {
                    globalLoop++;

                    AddLog($"🔄 --- BẮT ĐẦU VÒNG LẶP TỔNG THỨ {globalLoop} ---", "#4FC3F7");

                    foreach (var device in activeDevices)
                    {
                        if (token.IsCancellationRequested || _shopeeAuto.IsStop)
                            break;

                        await ProcessSingleDeviceAsync(device, token);

                        if (!token.IsCancellationRequested)
                            UpdateDeviceStatus(device, "WAITING", "Chờ tới lượt...");
                    }

                    if (token.IsCancellationRequested || _shopeeAuto.IsStop)
                        break;

                    if (DungHanSauVong > 0 && globalLoop >= DungHanSauVong)
                    {
                        AddLog($"🛑 Đã đạt giới hạn {DungHanSauVong} vòng. TỰ ĐỘNG DỪNG HẲN KỊCH BẢN!", "#FF5252");

                        _shopeeAuto.IsStop = true;

                        if (_cts != null && !_cts.IsCancellationRequested)
                            _cts.Cancel();

                        foreach (var d in activeDevices)
                        {
                            UpdateDeviceStatus(d, "STOPPED", "Đã xong mục tiêu");
                        }

                        break;
                    }

                    if (NghiSauVong > 0 && PhutNghi > 0 && globalLoop % NghiSauVong == 0)
                    {
                        AddLog($"⏳ Dàn máy đã hoàn thành {globalLoop} vòng. Bắt đầu nghỉ {PhutNghi} phút...", "#F57C00");

                        foreach (var d in activeDevices)
                        {
                            UpdateDeviceStatus(d, "RESTING", $"Đang nghỉ {PhutNghi}p");
                        }

                        try
                        {
                            await Task.Delay(PhutNghi * 60000, token);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                    }
                }
            }
            catch
            {
                AddLog("🛑 Đã ngắt tiến trình chạy tuần tự.", "#FF5252");
            }
        }

        private async Task ProcessSingleDeviceAsync(DeviceModel device, CancellationToken token)
        {
            Random rnd = new Random();

            device.LoopCount++;

            UpdateDeviceStatus(device, "RUNNING", "Đang xử lý...");
            AddLog($"[{device.Key}] 👉 BẮT ĐẦU CHẠY (Vòng {device.LoopCount})", "#00E676");

            try
            {
                if (!await WaitForAdb(device, token))
                    return;

                UpdateDeviceStatus(device, "CHANGING", "B1: Random x2");

                await _apiService.SendCommandAsync(BaseUrl, $"/dev/{device.Key}/random?brand={Brand}", token);
                await Task.Delay(1000, token);

                await _apiService.SendCommandAsync(BaseUrl, $"/dev/{device.Key}/random?brand={Brand}", token);
                await Task.Delay(2000, token);

                UpdateDeviceStatus(device, "CHANGING", "B2: Change Info");

                double? lat = null;
                double? lon = null;

                if (!string.IsNullOrEmpty(LatLon) && LatLon.Contains(","))
                {
                    try
                    {
                        var parts = LatLon.Split(',');

                        lat = double.Parse(
                            parts[0].Trim(),
                            System.Globalization.CultureInfo.InvariantCulture);

                        lon = double.Parse(
                            parts[1].Trim(),
                            System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        AddLog($"[{device.Key}] ⚠️ Lỗi định dạng Lat/Lon.");
                    }
                }

                await _apiService.ChangeDeviceAsync(device.Key, lat, lon);

                UpdateDeviceStatus(device, "WAITING", "Chờ Boot...");

                if (!await WaitForAdb(device, token))
                    return;

                await Task.Delay(3000, token);

                await _apiService.SendCommandAsync(BaseUrl, $"/dev/{device.Key}/random?brand={Brand}", token);
                await Task.Delay(1000, token);

                if (Use4GHotspot)
                {
                    UpdateDeviceStatus(device, "NETWORK", "Đổi IP 4G...");

                    AddLog($"[{device.Key}] ✈ Cấu hình dùng 4G: Ra lệnh TẮT Wi-Fi để đổi IP mạng...");

                    KAutoHelper.ADBHelper.ExecuteCMD($"adb -s {device.AdbId} shell svc wifi disable");

                    await Task.Delay(2000, token);

                    if (!string.IsNullOrEmpty(HotspotName) && HotspotName != "Pchanger_Wifi")
                    {
                        await _networkService.ChangeHotspotNameAsync(
                            device.AdbId,
                            HotspotName,
                            msg => AddLog(msg),
                            token);
                    }
                    else
                    {
                        await _networkService.ToggleAirplaneModeAsync(
                            device.AdbId,
                            msg => AddLog(msg),
                            token);
                    }
                }

                if (SelectedRunMode == "Restore and Backup")
                {
                    string target = "";

                    lock (_restoreQueue)
                    {
                        if (_restoreQueue.Count > 0)
                            target = _restoreQueue.Dequeue();
                    }

                    if (!string.IsNullOrEmpty(target))
                    {
                        UpdateDeviceStatus(device, "RESTORING", $"Restore {target}");
                        await _apiService.RestoreAsync(device.Key, target);
                    }
                }
                else
                {
                    UpdateDeviceStatus(device, "CHANGING", "Change Lần 2");
                    await _apiService.ChangeDeviceAsync(device.Key, lat, lon);
                }

                if (!await WaitForAdb(device, token))
                    return;

                await Task.Delay(5000, token);

                if (ApkFiles.Any())
                {
                    foreach (var apk in ApkFiles)
                    {
                        if (token.IsCancellationRequested)
                            break;

                        UpdateDeviceStatus(device, "INSTALLING", $"Cài {Path.GetFileName(apk)}");

                        await _adbService.InstallAsync(device.AdbId, apk, token);
                    }
                }

                UpdateDeviceStatus(device, "NETWORK", "Bật Wifi/Proxy...");

                if (!Use4GHotspot)
                {
                    AddLog($"[{device.Key}] 🌐 Cấu hình dùng Wi-Fi: Đang BẬT Wi-Fi và đợi kết nối...");

                    KAutoHelper.ADBHelper.ExecuteCMD($"adb -s {device.AdbId} shell svc wifi enable");

                    await Task.Delay(3000, token);

                    if (!string.IsNullOrWhiteSpace(TargetWifiSsid))
                    {
                        AddLog($"[{device.Key}] 🔄 Đang kết nối vào Wi-Fi: {TargetWifiSsid}...");

                        string connectCmd =
                            $"adb -s {device.AdbId} shell cmd wifi connect-network \"{TargetWifiSsid}\" wpa2 \"{TargetWifiPassword}\"";

                        KAutoHelper.ADBHelper.ExecuteCMD(connectCmd);

                        await Task.Delay(5000, token);
                    }
                    else
                    {
                        await Task.Delay(5000, token);
                    }
                }

                if (UseProxy)
                {
                    if (UseTMProxy)
                    {
                        AddLog($"[{device.Key}] 🛡️ Đang thiết lập kết nối TMProxy...");

                        var proxyResult = await _proxyService.GetTMProxyAsync(
                            ProxyApiKey,
                            msg => AddLog(msg),
                            token);

                        if (proxyResult.Success && UseSocksDroid)
                        {
                            await _networkService.ConfigSocksDroidAsync(
                                device.AdbId,
                                proxyResult.ProxyString,
                                msg => AddLog(msg),
                                token);
                        }
                    }
                    else if (UseProxyNo1)
                    {
                        AddLog($"[{device.Key}] 🛡️ Đang thiết lập kết nối ProxyNo1...");

                        await _proxyService.ChangeProxyNo1Async(
                            ProxyApiKey,
                            msg => AddLog(msg),
                            token);
                    }
                }

                bool isScriptSuccess = true;

                if (IsShopee)
                {
                    UpdateDeviceStatus(device, "SHOPEE", "Đăng ký Mail...");

                    _shopeeAuto.LoadData(ImageFolderPath, msg => { });

                    bool isMailOk = await _shopeeAuto.RunRegistrationScriptAsync(
                        device.AdbId,
                        MailApiKey,
                        MailProductId,
                        msg => AddLog($"[{device.Key}] {msg}"),
                        token);

                    if (isMailOk && IsAddPhone)
                    {
                        UpdateDeviceStatus(device, "ADD PHONE", "Thêm SĐT & Captcha...");

                        ISmsProvider smsApi = _smsManager.GetProvider(SelectedSmsProvider);

                        ShopeeAddPhoneService addPhoneSvc = new ShopeeAddPhoneService(
                            _adbService,
                            smsApi,
                            _captchaService);

                        bool isPhoneAdded = await addPhoneSvc.ExecuteAddPhoneFlowAsync(
                            device.AdbId,
                            MailApiKey,
                            MailProductId,
                            "viettel",
                            OpenAIApiKey,
                            msg => AddLog($"[{device.Key}] {msg}"),
                            token);

                        if (!isPhoneAdded)
                            isScriptSuccess = false;
                    }
                    else if (!isMailOk)
                    {
                        isScriptSuccess = false;
                    }
                }

                if (IsNuoiShopee)
                {
                    UpdateDeviceStatus(device, "NUOI SPe", "Đang Nuôi Shopee...");
                    await Task.Delay(2000, token);
                }

                if (DelaySeconds > 0)
                {
                    AddLog($"[{device.Key}] ⏳ Đang nghỉ {DelaySeconds} giây theo cấu hình...");
                    await Task.Delay(DelaySeconds * 1000, token);
                }

                string bName = $"{BackupPrefix}_{DateTime.Now:ddMMyy}_{rnd.Next(1000, 9999)}";

                if (!isScriptSuccess)
                    bName += "_ERR";

                UpdateDeviceStatus(device, "BACKUP", "Đang lưu...");

                await _apiService.BackupAsync(device.Key, bName);

                if (!await WaitForAdb(device, token))
                    return;

                await Task.Delay(3000, token);

                AddLog($"[{device.Key}] 🎉 HOÀN TẤT VÒNG {device.LoopCount}!", "#00E676");
            }
            catch (OperationCanceledException)
            {
                UpdateDeviceStatus(device, "STOPPED", "Đã dừng");
            }
            catch (Exception ex)
            {
                UpdateDeviceStatus(device, "ERROR", "Lỗi: " + ex.Message);
                AddLog($"[{device.Key}] ❌ Lỗi: {ex.Message}", "#FF5252");
            }
        }

        private async Task<bool> WaitForAdb(DeviceModel device, CancellationToken token)
        {
            UpdateDeviceStatus(device, "WAIT_ADB", "Đợi PChanger kết nối...");
            AddLog($"[{device.Key}] ⏳ Đang hỏi PChanger để lấy mã kết nối ADB...");

            for (int i = 0; i < 200; i++)
            {
                if (token.IsCancellationRequested)
                    return false;

                var info = await _apiService.GetDeviceDetailAsync(device.Key);

                if (info != null && info.Status == true && !string.IsNullOrEmpty(info.AdbId))
                {
                    device.AdbId = info.AdbId.Trim();

                    if (await _adbService.IsDeviceReadyAsync(device.AdbId))
                    {
                        AddLog($"[{device.Key}] ✅ PChanger đã online! Bắt được ADB ID: {device.AdbId}");
                        return true;
                    }
                }

                await Task.Delay(3000, token);
            }

            AddLog($"[{device.Key}] ❌ Quá thời gian 600s mà PChanger vẫn chưa nhận diện được máy.", "#FF5252");
            return false;
        }

        private void UpdateDeviceStatus(DeviceModel device, string status, string step)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                device.Status = status;
                device.StepInfo = step;
            });
        }

        public void AddLog(string msg, string color = "#00FF41")
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Logs.Add(new LogItem
                {
                    Message = $"[{DateTime.Now:HH:mm:ss}] {msg}",
                    Color = color
                });

                if (Logs.Count > 150)
                    Logs.RemoveAt(0);
            }));
        }
    }
}