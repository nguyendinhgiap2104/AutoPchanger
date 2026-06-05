using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;

namespace AutoTool.Services.GoogleAuth
{
    public class GoogleAuthTemplateStore : IDisposable
    {
        private string _dataPath = "";

        public Image<Gray, byte> BackupFeature { get; private set; }

        public Image<Gray, byte> Later { get; private set; }
        public Image<Gray, byte> DenyPermission { get; private set; }
        public Image<Gray, byte> Skip { get; private set; }
        public Image<Gray, byte> Skip1 { get; private set; }
        public Image<Gray, byte> Skip2 { get; private set; }
        public Image<Gray, byte> Agree { get; private set; }
        public Image<Gray, byte> Cancel { get; private set; }
        public Image<Gray, byte> RecoveryEmail { get; private set; }
        public Image<Gray, byte> SeeMore { get; private set; }
        public Image<Gray, byte> Accept { get; private set; }

        public bool Load(string dataPath, Action<string> log)
        {
            _dataPath = dataPath;

            if (string.IsNullOrWhiteSpace(_dataPath) || !Directory.Exists(_dataPath))
            {
                log?.Invoke($"❌ Thư mục data không tồn tại: {_dataPath}");
                return false;
            }
            BackupFeature = LoadImageMat("tinh_nang_sao_luu.png", log, required: false);

            DenyPermission = LoadImageMat("ko_cho_phep.png", log, required: false);
            Skip = LoadImageMat("bo_qua.png", log, required: false);
            Skip1 = LoadImageMat("bo_qua1.png", log, required: false);
            Skip2 = LoadImageMat("bo_qua2.png", log, required: false);
            Agree = LoadImageMat("dong_y.png", log, required: true);
            Cancel = LoadImageMat("huy.png", log, required: false);
            RecoveryEmail = LoadImageMat("email_kp.png", log, required: false);
            SeeMore = LoadImageMat("xem_them.png", log, required: false);
            Accept = LoadImageMat("chap_nhan.png", log, required: false);
            Later = LoadImageMat("de_sau.png", log, required: false);

            return Agree != null;
        }

        public List<Image<Gray, byte>> GetSkipTemplates()
        {
            return new List<Image<Gray, byte>>
            {
                Skip,
                Skip1,
                Skip2
            };
        }

        private Image<Gray, byte> LoadImageMat(string fileName, Action<string> log, bool required)
        {
            string fullPath = Path.Combine(_dataPath, fileName);

            if (!File.Exists(fullPath))
            {
                if (required)
                    log?.Invoke($"❌ Thiếu ảnh bắt buộc: {fileName}");
                else
                    log?.Invoke($"⚠️ Không thấy ảnh optional: {fileName}");

                return null;
            }

            try
            {
                using (Bitmap bmp = (Bitmap)Image.FromFile(fullPath))
                {
                    return new Image<Gray, byte>(bmp);
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"❌ Load ảnh lỗi [{fileName}]: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            DenyPermission?.Dispose();
            BackupFeature?.Dispose();
            Skip?.Dispose();
            Skip1?.Dispose();
            Skip2?.Dispose();
            Agree?.Dispose();
            Cancel?.Dispose();
            RecoveryEmail?.Dispose();
            SeeMore?.Dispose();
            Later?.Dispose();
            Accept?.Dispose();
        }
    }
}