using System;
using System.IO;
using System.Text.Json;

namespace AutoTool.Services
{
    public class AppSettings
    {
        public string BaseUrl { get; set; } = "http://127.0.0.1:8080";
        public string AdbPath { get; set; } = "adb";
        public string ImageFolderPath { get; set; } = "";
        public string RestoreFilePath { get; set; } = "";
        public string Brand { get; set; } = "google";
        public string BackupPrefix { get; set; } = "GIAPDAX";
        public int NghiSauVong { get; set; } = 10;
        public int PhutNghi { get; set; } = 5;
        public string LatLon { get; set; } = "";
        public int DelaySeconds { get; set; } = 3;

        public bool IsShopee { get; set; } = false;
        public bool IsAddPhone { get; set; } = true;
        public bool IsTikTok { get; set; } = false;
        public bool IsNuoiShopee { get; set; } = false;
        public bool IsSerialAuto { get; set; } = false;

        // API MAIL (Tách biệt)
        public string MailApiKey { get; set; } = "";
        public string MailProductId { get; set; } = "";

        // API SMS (Tách biệt hoàn toàn)
        public string SmsApiKey { get; set; } = "";
        public string SmsServiceId { get; set; } = "";
        public string SmsNetwork { get; set; } = "viettel";

        // API OPEN AI
        public string OpenAIApiKey { get; set; } = "";

        // NETWORK & PROXY
        public bool UseTMProxy { get; set; } = true;
        public bool UseProxyNo1 { get; set; } = false;
        public string ProxyApiKey { get; set; } = "";
        public bool Use4GHotspot { get; set; } = false;
        public int WifiDuration { get; set; } = 30;
        public string HotspotName { get; set; } = "Pchanger_Wifi";
        public bool UseSocksDroid { get; set; } = false;
        public bool UseOpenVPN { get; set; } = false;
        public string OpenVPNPath { get; set; } = "";
    }

    public static class AppConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            if (!File.Exists(ConfigPath)) return new AppSettings();
            try
            {
                string json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch { return new AppSettings(); }
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }
    }
}