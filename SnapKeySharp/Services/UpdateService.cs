using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;

namespace SnapKeySharp.Services
{
    internal class UpdateService
    {
        private static readonly string CurrentVersion =
            System.Reflection.Assembly.GetExecutingAssembly()
                .GetName().Version?.ToString(3) ?? "0.0.0";
        private static readonly string PastebinUrl = "https://pastebin.com/raw/sxCnyNLT";
        private static readonly string GithubApiUrl =
            "https://api.github.com/repos/cerule-ui/SnapKeySharp/releases/latest";
        private static readonly string GithubDownloadUrl =
            "https://github.com/cerule-ui/SnapKeySharp/releases/latest/download/SnapKeySharp.zip";

        public class ReleaseInfo
        {
            public string version { get; set; } = "";
            public string changelog { get; set; } = "";
            public string download_url { get; set; } = "";
        }

        public static bool CheckLauncher()
        {
            string launcherPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "SnapKeyLauncher.exe"
            );
            return File.Exists(launcherPath);
        }

        public static async Task<ReleaseInfo?> GetLatestRelease()
        {
            // сначала GitHub
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "SnapKeySharp");
                string json = await client.GetStringAsync(GithubApiUrl);
                using var doc = JsonDocument.Parse(json);

                string version = doc.RootElement.GetProperty("tag_name")
                    .GetString()?.TrimStart('v') ?? "";
                string changelog = doc.RootElement.GetProperty("body")
                    .GetString() ?? "";

                return new ReleaseInfo
                {
                    version = version,
                    changelog = changelog,
                    download_url = GithubDownloadUrl
                };
            }
            catch { }

            // fallback — Pastebin
            try
            {
                using var client = new HttpClient();
                string json = await client.GetStringAsync(PastebinUrl);
                return JsonSerializer.Deserialize<ReleaseInfo>(json);
            }
            catch
            {
                return null;
            }
        }

        public static async Task CheckForUpdates(Window owner)
        {
            try
            {
                var release = await GetLatestRelease();
                if (release == null) return;

                if (new Version(release.version) <= new Version(CurrentVersion)) return;

                // показываем окно обновления в UI потоке
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var w = new Windows.UpdateWindow(new UpdateInfo
                    {
                        Version = release.version,
                        Changelog = release.changelog,
                        DownloadUrl = release.download_url
                    });
                    w.Owner = owner;
                    w.ShowDialog();
                });
            }
            catch { }
        }

        public static void ApplyUpdate(string downloadUrl)
        {
            string launcherPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "SnapKeyLauncher.exe"
            );

            var psi = new ProcessStartInfo
            {
                FileName = launcherPath,
                Arguments = $"--update \"{downloadUrl}\"",
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(psi);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // пользователь отменил UAC
                return;
            }

            Application.Current.Shutdown();
        }
    }

    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string Changelog { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
    }
}