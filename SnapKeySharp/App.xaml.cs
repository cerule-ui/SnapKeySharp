using SnapKeySharp.Services;
using SnapKeySharp.Windows;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Windows;

namespace SnapKeySharp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>


    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);


            if (!UpdateService.CheckLauncher())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/cerule-ui/SnapKeySharp/releases/latest",
                    UseShellExecute = true
                });

                MessageBox.Show(
                    "Please install SnapKey using SnapKeyLauncher.exe.",
                    "Launcher not found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                Shutdown();
                return;
            }

            string culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            string dict = culture == "ru" ? "Strings.ru.xaml" : "Strings.en.xaml";

            var uri = new Uri($"Localization/{dict}", UriKind.Relative);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });

            var window = new MainWindow();

            if (e.Args.Contains("--tray"))
            {
                window.ShowInTaskbar = false;
                window.WindowState = WindowState.Minimized;
                window.Show();
                window.Hide();
            }
            else
            {
                window.Show();

            }


            _ = Task.Run(async () =>
            {
                await UpdateService.CheckForUpdates(window);
            });
        }
    }


}
