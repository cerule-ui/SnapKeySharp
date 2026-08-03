using Microsoft.Win32;
using SnapKeyLauncher.Services;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SnapKeyLauncher
{
    public partial class MainWindow : Window
    {
        private bool _isInstalled;
        private static readonly string InstallPath =
            @"C:\Program Files\SnapKeySharp\SnapKeySharp.exe";
        private static readonly string RegPath =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SnapKeySharp";

        public MainWindow()
        {
            InitializeComponent();
            CheckInstallState();
            ApplyState();
        }

        private void CheckInstallState()
        {
            bool exeExists = File.Exists(InstallPath);
            bool regExists = Registry.LocalMachine.OpenSubKey(RegPath) != null;
            _isInstalled = exeExists && regExists;
        }

        private void ApplyState()
        {
            if (_isInstalled)
            {
                StatusText.Text = "SnapKey is installed and ready.";

                // Install — неактивна
                InstallText.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
                InstallBorder.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                InstallBorder.Cursor = Cursors.No;

                // Update — активна, зелёная
                UpdateBorder.Background = new SolidColorBrush(Color.FromRgb(20, 50, 30));
                UpdateLabel.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                UpdateBorder.Cursor = Cursors.Hand;

                // Repair — активна
                RepairBorder.Background = new SolidColorBrush(Color.FromRgb(40, 35, 20));
                RepairLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                RepairBorder.Cursor = Cursors.Hand;

                // Uninstall — активна, красная
                UninstallBorder.Background = new SolidColorBrush(Color.FromRgb(50, 20, 20));
                UninstallLabel.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                UninstallBorder.Cursor = Cursors.Hand;
            }
            else
            {
                StatusText.Text = "SnapKey is not installed.";

                // Install — активна, синяя
                InstallBorder.Background = new SolidColorBrush(Color.FromRgb(26, 58, 92));
                InstallBorder.Cursor = Cursors.Hand;

                // Update, Repair, Uninstall — неактивны
                UpdateBorder.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                UpdateBorder.Cursor = Cursors.No;
                UpdateLabel.Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80));

                RepairBorder.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                RepairBorder.Cursor = Cursors.No;
                RepairLabel.Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80));

                UninstallBorder.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                UninstallBorder.Cursor = Cursors.No;
                UninstallLabel.Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            }
        }

        private async void InstallClick(object sender, MouseButtonEventArgs e)
        {
            if (_isInstalled) return;
            this.Hide();
            var window = new UpdateProgressWindow("Installing SnapKey");
            window.Show();
            await InstallerService.Install(window, window.Token);
            Application.Current.Shutdown();
        }

        private async void UpdateClick(object sender, MouseButtonEventArgs e)
        {
            if (!_isInstalled) return;
            this.Hide();
            var window = new UpdateProgressWindow("Updating SnapKey");
            window.Show();
            bool success = await InstallerService.Update(window, window.Token);
            if (!success)
            {
                this.Show();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private async void RepairClick(object sender, MouseButtonEventArgs e)
        {
            if (!_isInstalled) return;
            this.Hide();
            var window = new UpdateProgressWindow("Repairing SnapKey");
            window.Show();
            await InstallerService.Repair(window, window.Token);
            Application.Current.Shutdown();
            return;
        }

        private async void UninstallClick(object sender, MouseButtonEventArgs e)
        {
            if (!_isInstalled) return;
            this.Hide();
            var window = new UpdateProgressWindow("Uninstalling SnapKey");
            window.Show();
            await InstallerService.Uninstall(window);
            Application.Current.Shutdown();
            return;
        }
    }
}