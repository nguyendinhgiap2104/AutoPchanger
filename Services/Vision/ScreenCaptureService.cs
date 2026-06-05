using KAutoHelper;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;

namespace AutoTool.Services.Vision
{
    public class ScreenCaptureService
    {
        public byte[] GetScreenBytes(string deviceId, Action<string> log = null)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = $"-s {deviceId} exec-out screencap -p",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                using (var ms = new MemoryStream())
                {
                    process.StandardOutput.BaseStream.CopyTo(ms);
                    process.WaitForExit(2000);

                    if (ms.Length > 0)
                        return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"⚠️ GetScreenBytes lỗi: {ex.Message}");
            }

            return null;
        }

        public Bitmap GetScreenBitmap(string deviceId, Action<string> log = null)
        {
            try
            {
                byte[] bytes = GetScreenBytes(deviceId, log);

                if (bytes != null && bytes.Length > 0)
                {
                    using (var ms = new MemoryStream(bytes))
                    {
                        return new Bitmap(ms);
                    }
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"⚠️ GetScreenBitmap lỗi: {ex.Message}");
            }

            try
            {
                return ADBHelper.ScreenShoot(deviceId);
            }
            catch (Exception ex)
            {
                log?.Invoke($"⚠️ ADBHelper.ScreenShoot lỗi: {ex.Message}");
                return null;
            }
        }

        public Image<Gray, byte> GetScreenMat(string deviceId, Action<string> log = null)
        {
            Bitmap bitmap = null;

            try
            {
                bitmap = GetScreenBitmap(deviceId, log);
                if (bitmap == null) return null;

                return new Image<Gray, byte>(bitmap);
            }
            catch (Exception ex)
            {
                log?.Invoke($"⚠️ GetScreenMat lỗi: {ex.Message}");
                return null;
            }
            finally
            {
                bitmap?.Dispose();
            }
        }
    }
}