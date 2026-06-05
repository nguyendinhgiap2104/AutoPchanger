using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoTool.Models
{
    public class DeviceModel : INotifyPropertyChanged
    {
        private string _status = "OFFLINE";
        private string _stepInfo = "Sẵn sàng";
        private string _adbId;
        private int _loopCount = 0;
        private bool _isSelected = true;

        public string Key { get; set; }
        public string AdbId
        {
            get => _adbId;
            set { _adbId = value; OnPropertyChanged(); }
        }
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }
        public string StepInfo
        {
            get => _stepInfo;
            set { _stepInfo = value; OnPropertyChanged(); }
        }
        public int LoopCount
        {
            get => _loopCount;
            set { _loopCount = value; OnPropertyChanged(); }
        }
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}