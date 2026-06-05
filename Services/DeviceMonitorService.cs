using AutoTool.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTool.Services
{
    public class DeviceMonitorService
    {
        private readonly AdbService _adb;
        public DeviceMonitorService(AdbService adb) { _adb = adb; }

        public async Task MonitorAsync(ObservableCollection<DeviceModel> devices, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var currentDevices = await _adb.GetConnectedDevicesAsync();
                    foreach (var d in devices.ToList())
                    {
                        if (string.IsNullOrEmpty(d.AdbId)) continue;
                        if (!currentDevices.Contains(d.AdbId))
                        {
                            if (d.Status == "RUNNING" || d.Status == "RESTING")
                            {
                                d.Status = "DISCONNECTED";
                                d.StepInfo = "⚠ Rớt cáp USB!";
                            }
                        }
                        else if (d.Status == "DISCONNECTED")
                        {
                            d.Status = "RECONNECTED";
                            d.StepInfo = "Đã cắm lại cáp";
                        }
                    }
                }
                catch { }
                await Task.Delay(3000, token);
            }
        }
    }
}