using System.Drawing;

namespace AutoTool.Services.Vision
{
    public class OcrTextFinder
    {
        private readonly OcrService _ocrService;

        public OcrTextFinder()
        {
            _ocrService = new OcrService();
        }

        public Point? FindText(byte[] screenBytes, string text, float confidence = 0.6f)
        {
            if (screenBytes == null || screenBytes.Length == 0)
                return null;

            return _ocrService.FindTextAndGetPoint(screenBytes, text, confidence);
        }
    }
}