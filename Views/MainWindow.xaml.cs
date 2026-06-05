using AutoTool.ViewModels;
using Microsoft.Win32;
using System.Linq;
using System.Windows;

namespace AutoTool.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnAddApkUI_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "App Files (*.apk;*.xapk)|*.apk;*.xapk|All files (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true && DataContext is MainViewModel vm)
            {
                foreach (var file in ofd.FileNames)
                {
                    if (!vm.ApkFiles.Contains(file))
                        vm.ApkFiles.Add(file);
                }
            }
        }

        private void BtnBrowseAdb_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "ADB Executable (adb.exe)|adb.exe|All files (*.*)|*.*",
                Title = "Chọn file adb.exe"
            };

            if (ofd.ShowDialog() == true && DataContext is MainViewModel vm)
            {
                vm.AdbPath = ofd.FileName;
            }
        }

        private void BtnBrowseRestore_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Chọn file danh sách Restore"
            };

            if (ofd.ShowDialog() == true && DataContext is MainViewModel vm)
            {
                vm.RestoreFilePath = ofd.FileName;
            }
        }

        private void BtnBrowseImageFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.Description = "Chọn thư mục chứa hình ảnh Shopee / Google Auth";

                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (DataContext is MainViewModel vm)
                    {
                        vm.ImageFolderPath = fbd.SelectedPath;
                    }
                }
            }
        }

        private void BtnBrowseTestMailFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Chọn file mail test dạng email|pass hoặc email|pass|recovery"
            };

            if (ofd.ShowDialog() == true && DataContext is MainViewModel vm)
            {
                vm.TestMailFilePath = ofd.FileName;
                vm.UseTestMailFile = true;
                vm.AddLog($"🧪 Đã chọn file mail test: {ofd.FileName}", "#F57C00");
            }
        }
    }
}