using KAutoHelper;

namespace AutoTool.Services.Device
{
    public class DeviceCommandService
    {
        public void Execute(string command)
        {
            ADBHelper.ExecuteCMD(command);
        }

        public void KeyEvent(string deviceId, int keyCode)
        {
            Execute($"adb -s {deviceId} shell input keyevent {keyCode}");
        }

        public void Back(string deviceId)
        {
            KeyEvent(deviceId, 4);
        }

        public void Enter(string deviceId)
        {
            KeyEvent(deviceId, 66);
        }

        public void Tab(string deviceId)
        {
            KeyEvent(deviceId, 61);
        }

        public void Backspace(string deviceId)
        {
            KeyEvent(deviceId, 67);
        }
    }
}