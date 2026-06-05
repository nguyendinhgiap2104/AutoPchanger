using AutoTool.Services.SmsProviders;
using System;

namespace AutoTool.Services
{
    public class SmsManager
    {
        private readonly ISmsProvider _otisProvider;
        private readonly ISmsProvider _bossOtpProvider;
        private readonly ISmsProvider _funOtpProvider;

        public SmsManager()
        {
            _otisProvider = new OtisSmsProvider();
            _bossOtpProvider = new BossOtpSmsProvider();
            _funOtpProvider = new FunOtpSmsProvider();
        }

        public ISmsProvider GetProvider(string providerName)
        {
            if (string.IsNullOrEmpty(providerName)) return _otisProvider;

            // ==========================================
            // FIX TẬN GỐC LỖI WPF COMBOBOX ITEM Ở ĐÂY
            // ==========================================
            if (providerName.Contains("ComboBoxItem:"))
            {
                providerName = providerName.Split(':')[1].Trim();
            }

            switch (providerName.ToLower())
            {
                case "otis":
                case "otisx":
                    return _otisProvider;
                case "bossotp":
                    return _bossOtpProvider;
                case "funotp":
                    return _funOtpProvider;
                default:
                    return _otisProvider; // Fallback an toàn, tránh crash tool
            }
        }
    }
}