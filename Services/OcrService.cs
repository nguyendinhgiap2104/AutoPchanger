using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Globalization; // Cần thiết để xử lý font chữ và dấu
using Tesseract; // Thư viện OCR

namespace AutoTool.Services
{
    public class OcrService : IDisposable
    {
        // Khởi tạo engine OCR một lần duy nhất để dùng chung cho nhẹ máy
        private readonly TesseractEngine _engine;

        public OcrService()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string tessdataPath = Path.Combine(basePath, "tessdata");

                if (!Directory.Exists(tessdataPath))
                {
                    throw new Exception($"❌ CHƯA COPY TESSDATA! Hãy copy thư mục tessdata vào đường dẫn này:\n{tessdataPath}");
                }

                // Dù lột dấu, vẫn nên nạp "vie" để AI nhận diện hình dáng chữ tốt hơn
                _engine = new TesseractEngine(tessdataPath, "vie", EngineMode.Default);
            }
            catch (Exception ex)
            {
                throw new Exception("❌ Lỗi khởi tạo bộ não OCR: " + ex.Message);
            }
        }

        // ==============================================================================
        // HÀM LOẠI BỎ DẤU TIẾNG VIỆT VÀ CHỮ Đ/đ CỰC KỲ CHUẨN XÁC
        // ==============================================================================
        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            // Ép về Unicode chuẩn và xử lý riêng chữ Đ/đ (Vì chữ Đ không thuộc dạng có dấu chuẩn)
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC)
                                .Replace('đ', 'd').Replace('Đ', 'D');
        }

        // ==============================================================================
        // HÀM TRÁI TIM: TÌM CHỮ VÀ LẤY TỌA ĐỘ TRÊN ẢNH
        // ==============================================================================
        public Point? FindTextAndGetPoint(byte[] screenByte, string textToFind, float threshold = 0.7f)
        {
            if (screenByte == null || string.IsNullOrWhiteSpace(textToFind)) return null;

            try
            {
                // 1. CHUẨN HÓA CHỮ CẦN TÌM: Viết thường -> Lột sạch dấu -> Xóa khoảng trắng 2 đầu
                // Ví dụ: "Đồng ý" -> "dong y"
                string normalizedTextToFind = RemoveDiacritics(textToFind).ToLower().Trim();

                using (var ms = new MemoryStream(screenByte))
                {
                    using (var bmp = new Bitmap(ms))
                    {
                        using (var page = _engine.Process(bmp))
                        {
                            // Đọc theo Line (Dòng) để chữ "bỏ qua" đi liền với nhau, không bị chẻ đôi thành "bỏ" và "qua"
                            using (var iter = page.GetIterator())
                            {
                                iter.Begin();

                                do
                                {
                                    string detectedText = iter.GetText(PageIteratorLevel.TextLine);

                                    if (!string.IsNullOrWhiteSpace(detectedText))
                                    {
                                        float confidence = iter.GetConfidence(PageIteratorLevel.TextLine);

                                        // 2. CHUẨN HÓA CHỮ AI ĐỌC ĐƯỢC: Viết thường -> Lột sạch dấu -> Xóa khoảng trắng
                                        // Ví dụ AI đọc sai thành "Đống ỷ" -> Lột ra vẫn là "dong y"
                                        string normalizedDetectedText = RemoveDiacritics(detectedText).ToLower().Trim();

                                        // 3. SO SÁNH "KHÔNG DẤU" VÀ TÌM KIẾM
                                        if (confidence >= threshold && normalizedDetectedText.Contains(normalizedTextToFind))
                                        {
                                            // Nếu khớp, lấy khung tọa độ của cả dòng chữ đó
                                            if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var rect))
                                            {
                                                // Tính tâm để click
                                                int centerX = rect.X1 + (rect.Width / 2);
                                                int centerY = rect.Y1 + (rect.Height / 2);

                                                return new Point(centerX, centerY);
                                            }
                                        }
                                    }

                                } while (iter.Next(PageIteratorLevel.TextLine));
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public void Dispose()
        {
            if (_engine != null) _engine.Dispose();
        }
    }
}