using SnapKeyLauncher.Services;
using System.Configuration;
using System.Data;
using System.Windows;

namespace SnapKeyLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Contains("--update"))
            {
                string downloadUrl = e.Args.Length > 1 ? e.Args[1] : "";
                var window = new UpdateProgressWindow("Updating SnapKey");
                window.Show();
                _ = Task.Run(async () =>
                {
                    await InstallerService.UpdateFromUrl(window, downloadUrl,
                        new CancellationTokenSource().Token);
                    Application.Current.Dispatcher.Invoke(() =>
                        Application.Current.Shutdown());
                });
                return;
            }

            // обычный запуск - главное меню
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }


    
    }

}