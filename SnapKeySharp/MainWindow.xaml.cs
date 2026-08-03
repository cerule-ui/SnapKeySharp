using SnapKeySharp.Core;
using SnapKeySharp.Services;
using SnapKeySharp.Windows;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static SnapKeySharp.Native.NativeMethods;
namespace SnapKeySharp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SnapKeyService _service;
        bool _isActive;
        private AppConfig _config;

        public MainWindow()
        {
            InitializeComponent();

            // инициализация
            _service = new SnapKeyService();
            _config = ConfigService.Load();
            _isActive = _config.IsActive;

            // загружаем пары из конфига
            foreach (var pair in _config.Pairs)
            {
                var parts = pair.Split(',');
                uint key1 = uint.Parse(parts[0]);
                uint key2 = uint.Parse(parts[1]);
                _service.AddPair(key1, key2);
                AddPairToUI(key1, key2); // метод который добавит строку в PairsPanel
            }

            // загружаем исключения
            foreach (var exclusion in _config.Exclusions)
            {
                _service.AddExcludedProcess(exclusion);
                AddExclusionToUI(exclusion); // метод который добавит строку в ExclusionsPanel
            }

            var regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            bool realAutoStart = regKey?.GetValue("SnapKeySharp") != null;

            _config.AutoStart = realAutoStart; // синхронизируем конфиг с реальностью
            ConfigService.Save(_config);

            AutoStartMenuItem.IsChecked = realAutoStart; // галочка в трее на автозагрузку
            WorkingMenuItem.IsChecked = _config.IsActive;

            if (_isActive)
            {
                _service.Start();
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                StatusText.Text = (string)FindResource("StatusActive");
                ToggleButton.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.Pause);
                ToggleButton.Label = (string)FindResource("BtnPause");
            }
            else
            {
                _service.Stop();
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                StatusText.Text = (string)FindResource("StatusInactive");
                ToggleButton.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.Play);
                ToggleButton.Label = (string)FindResource("BtnPlay");
            }


        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // отменяем закрытие
            this.Hide();     // прячем окно
        }

        private void TrayIcon_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void TrayExit_Click(object sender, RoutedEventArgs e)
        {
            TrayIcon.Dispose();
            Application.Current.Shutdown();
        }

        private void TrayAutoStart_Click(object sender, RoutedEventArgs e)
        {
            _config.AutoStart = !_config.AutoStart;
            ConfigService.Save(_config);

            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            if (key == null) return;

            string exePath = Environment.ProcessPath!;
            if (_config.AutoStart)
                key.SetValue("SnapKeySharp", $"\"exePath\" --tray");
            else
                key.DeleteValue("SnapKeySharp", false);
        }

        private void TrayWorking_Click(object sender, RoutedEventArgs e)
        {
            ToggleButtonClick(sender, e);
        }


        private void ToggleButtonClick(object sender, RoutedEventArgs e)
        {
            _isActive = !_isActive;
            _config.IsActive = _isActive;
            ConfigService.Save(_config);

            if (_isActive)
            {
                _service.Start();
                WorkingMenuItem.IsChecked = true;
                StatusText.Text = (string)FindResource("StatusActive");
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                ToggleButton.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.Pause);
                ToggleButton.Label = (string)FindResource("BtnPause");
            }
            else
            {
                _service.Stop();
                WorkingMenuItem.IsChecked = false;
                StatusText.Text = (string)FindResource("StatusInactive");
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                ToggleButton.Icon = new ModernWpf.Controls.SymbolIcon(ModernWpf.Controls.Symbol.Play);
                ToggleButton.Label = (string)FindResource("BtnPlay");
            }
        }

        private void AddPairClick(object sender, RoutedEventArgs e)
        {
            var dialog = new AddPairWindow();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                if (!_config.Pairs.Contains($"{dialog.Key1},{dialog.Key2}") && dialog.Key1 != dialog.Key2)
                {
                    _service.AddPair((uint)dialog.Key1, (uint)dialog.Key2);
                    _config.Pairs.Add($"{dialog.Key1},{dialog.Key2}");
                    ConfigService.Save(_config);
                    AddPairToUI((uint)dialog.Key1, (uint)dialog.Key2);
                }
            }


        }

        private void AddExclusionClick(object sender, RoutedEventArgs e)
        {
            var dialog = new AddExclusionWindow();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                string name = dialog.ProcessName;
                if (_config.Exclusions.Contains(name)) return;
                _service.AddExcludedProcess(name);
                _config.Exclusions.Add(name);
                ConfigService.Save(_config);
                AddExclusionToUI(name);
            }
        }
        private void AddPairToUI(uint key1, uint key2)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 4, 8, 4) };
            var text = new TextBlock { Text = " •  " + (char)MapVirtualKey(key1, 2)  + "   ⇔   " + (char)MapVirtualKey(key2, 2), 
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = new Thickness(0, 0, 8, 0)
            };
            var btn = new Button { Content = "✕" };
            btn.Click += (s, e) =>
            {
                _service.RemovePair(key1, key2);
                int fKey = (int)key1;
                int sKey = (int)key2;
                string Pair = fKey.ToString() + "," + sKey.ToString();
                _config.Pairs.Remove(Pair);
                ConfigService.Save(_config);
                PairsPanel.Children.Remove(panel);
            };
            panel.Children.Add(text);
            panel.Children.Add(btn);
            PairsPanel.Children.Add(panel);
        }

        private void AddExclusionToUI(string processName)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 4, 8, 4) };
            var text = new TextBlock { Text = " •  " + processName, 
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = new Thickness(0, 0, 8, 0),
                TextTrimming = TextTrimming.CharacterEllipsis, // автоматически добавляет "..."
                MaxWidth = 275, // максимальная ширина до обрезки
            };
            var btn = new Button { Content = "✕" };
            btn.Click += (s, e) =>
            {
                _service.RemoveExcludedProcess(processName);
                _config.Exclusions.Remove(processName);
                ConfigService.Save(_config);
                ExclusionsPanel.Children.Remove(panel);
            };
            panel.Children.Add(text);
            panel.Children.Add(btn);
            ExclusionsPanel.Children.Add(panel);
        }

    }
}