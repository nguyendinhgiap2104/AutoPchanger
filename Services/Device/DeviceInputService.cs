using KAutoHelper;
using System.Drawing;
using AutoTool.Services.Vision;

namespace AutoTool.Services.Device
{
    public class DeviceInputService
    {
        private readonly ScreenCaptureService _screenCapture;

        public DeviceInputService(ScreenCaptureService screenCapture)
        {
            _screenCapture = screenCapture;
        }

        public void Tap(string deviceId, int x, int y)
        {
            ADBHelper.Tap(deviceId, x, y);
        }

        public void Tap(string deviceId, Point point)
        {
            Tap(deviceId, point.X, point.Y);
        }

        public void Swipe(string deviceId, int x1, int y1, int x2, int y2)
        {
            ADBHelper.Swipe(deviceId, x1, y1, x2, y2);
        }

        public void InputText(string deviceId, string text)
        {
            ADBHelper.InputText(deviceId, text);
        }

        public bool TapByRatio(string deviceId, double xRatio, double yRatio)
        {
            using (var screen = _screenCapture.GetScreenMat(deviceId))
            {
                if (screen == null)
                    return false;

                int x = (int)(screen.Width * xRatio);
                int y = (int)(screen.Height * yRatio);

                Tap(deviceId, x, y);
                return true;
            }
        }

        public bool SwipeByRatio(
            string deviceId,
            double startXRatio,
            double startYRatio,
            double endXRatio,
            double endYRatio)
        {
            using (var screen = _screenCapture.GetScreenMat(deviceId))
            {
                if (screen == null)
                    return false;

                int x1 = (int)(screen.Width * startXRatio);
                int y1 = (int)(screen.Height * startYRatio);
                int x2 = (int)(screen.Width * endXRatio);
                int y2 = (int)(screen.Height * endYRatio);

                Swipe(deviceId, x1, y1, x2, y2);
                return true;
            }
        }
    }
}