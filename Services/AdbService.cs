using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Services
{
    public class AdbService
    {
        public string AdbPath { get; set; } = @"D:\PM\data\adb\adb.exe";

        private string RunAdb(string args)
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = AdbPath;
                p.StartInfo.Arguments = args;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                p.WaitForExit();
                return output + error;
            }
        }

        public async Task<bool> IsDeviceReadyAsync(string adbId, int retries = 10, int delayMs = 3000)
        {
            for (int i = 0; i < retries; i++)
            {
                string output = await Task.Run(() => RunAdb("devices"));
                var devices = ParseDevices(output);
                foreach (var d in devices)
                {
                    if (d.id == adbId && d.status.Equals("device", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                await Task.Delay(delayMs);
            }
            return false;
        }

        private List<(string id, string status)> ParseDevices(string raw)
        {
            var list = new List<(string, string)>();
            var lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("List of devices")) continue;
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    list.Add((parts[0].Trim(), parts[1].Trim()));
            }
            return list;
        }

        public async Task<List<string>> GetConnectedDevicesAsync()
        {
            var result = new List<string>();
            try
            {
                string output = await Task.Run(() => RunAdb("devices"));
                var devices = ParseDevices(output);
                result = devices.Where(d => d.status == "device").Select(d => d.id).ToList();
            }
            catch { }
            return result;
        }

        public async Task<(bool success, string output)> InstallAsync(string adbId, string filePath, CancellationToken token)
        {
            try
            {
                if (string.IsNullOrEmpty(adbId) || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                    return (false, "Invalid file or adbId");

                if (filePath.EndsWith(".xapk", StringComparison.OrdinalIgnoreCase))
                    return await InstallXapkAsync(adbId, filePath, token);

                string args = $"-s {adbId} install -r \"{filePath}\"";
                string output = await Task.Run(() => RunAdb(args));
                bool success = output.Contains("Success");
                return (success, output);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private async Task<(bool success, string output)> InstallXapkAsync(string adbId, string xapkPath, CancellationToken token)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(tempDir);
                ZipFile.ExtractToDirectory(xapkPath, tempDir); // Cần using System.IO.Compression
                var apkFiles = Directory.GetFiles(tempDir, "*.apk", SearchOption.AllDirectories);
                if (apkFiles.Length == 0)
                    return (false, "Không có APK trong XAPK");

                string args = $"-s {adbId} install-multiple -r ";
                foreach (var apk in apkFiles)
                    args += $"\"{apk}\" ";

                string output = await Task.Run(() => RunAdb(args));
                bool success = output.Contains("Success");
                return (success, output);
            }
            catch (Exception ex)
            {
                return (false, $"XAPK ERROR: {ex.Message}");
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}