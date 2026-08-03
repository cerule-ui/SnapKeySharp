using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
namespace SnapKeyLauncher.Services
{
    public static class InstallerService
    {
        private static readonly string InstallDir =
            @"C:\Program Files\SnapKeySharp";
        private static readonly string ExePath =
            Path.Combine(InstallDir, "SnapKeySharp.exe");
        private static readonly string RegPath =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SnapKeySharp";
        private static readonly string GithubUrl =
            "https://github.com/cerule-ui/SnapKeySharp/releases/latest/download/SnapKeySharp.zip";
        private static readonly string GithubApiUrl =
            "https://api.github.com/repos/cerule-ui/SnapKeySharp/releases/latest";
        private static readonly string PastebinUrl =
            "https://pastebin.com/raw/sxCnyNLT";

        public class ReleaseInfo
        {
            public string version { get; set; } = "";
            public string changelog { get; set; } = "";
            public string download_url { get; set; } = "";
        }


        public static async Task Install(UpdateProgressWindow progress, CancellationToken ct)
        {
            // шаг 0 - связь с браузером
            var release = await GetLatestRelease(progress);
            if (release == null)
            {
                MessageBox.Show("Could not check for updates. Check your internet connection.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                progress.Dispatcher.Invoke(() => progress.Close());
                return;
            }

            // шаг 1 - создаем папку
            progress.SetStep("Preparing...");
            progress.SetProgress(0);
            if (Directory.Exists(InstallDir))
                Directory.Delete(InstallDir, true);
            Directory.CreateDirectory(InstallDir);

            // шаг 2 - скачиваем zip во временную папку
            progress.SetStep("Downloading...");
            string tempZip = Path.Combine(Path.GetTempPath(), "SnapKeySharp.zip");
            await DownloadFile(release.download_url, tempZip, progress, ct);

            // шаг 3 - распаковываем
            progress.SetStep("Extracting...");
            progress.SetProgress(0);
            ZipFile.ExtractToDirectory(tempZip, InstallDir);

            // шаг 4 - копируем лаунчер рядом
            progress.SetStep("Copying launcher...");
            string launcherDest = Path.Combine(InstallDir, "SnapKeyLauncher.exe");
            File.Copy(Environment.ProcessPath!, launcherDest, overwrite: true);

            // шаг 5 - ярлык в меню Пуск
            progress.SetStep("Creating shortcuts...");
            string startMenu = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                "Programs", "SnapKey.lnk"
            );
            CreateShortcut(startMenu, ExePath);

            // шаг 6 - реестр (Programs and Features)
            progress.SetStep("Registering...");
            var key = Registry.LocalMachine.CreateSubKey(RegPath);
            key.SetValue("DisplayName", "SnapKey");
            key.SetValue("DisplayVersion", release.version);
            key.SetValue("Publisher", "cerule");
            key.SetValue("InstallLocation", InstallDir);
            key.SetValue("UninstallString", $"\"{launcherDest}\"");
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.Close();

            // шаг 7 - запускаем
            progress.SetStep("Done!");
            progress.SetProgress(100);
            progress.HideCancel();
            await Task.Delay(800, ct);
            Process.Start(ExePath);
            progress.Dispatcher.Invoke(() => progress.Close());
        }

        public static async Task<bool> Update(UpdateProgressWindow progress, CancellationToken ct) 
        {
            // шаг 0 - проверка обновлений
            progress.SetStep("Checking for updates...");
            progress.HideCancel();
            var latestJson = await GetLatestRelease(progress);
            string? latest = (latestJson != null) ? latestJson.version : null;

            if (latest==null)
            {
                MessageBox.Show("Could not check for updates. Check your internet connection.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                progress.Dispatcher.Invoke(() => progress.Close());
                return false;
            }
            // читаем текущую версию из реестра
            var regKey = Registry.LocalMachine.OpenSubKey(RegPath);
            string current = regKey?.GetValue("DisplayVersion")?.ToString() ?? "0.0.0";

            if (new Version(latest) <= new Version(current))
            {
                MessageBox.Show($"You already have the latest version ({current}).",
                    "No updates", MessageBoxButton.OK, MessageBoxImage.Information);
                progress.Dispatcher.Invoke(() => progress.Close());
                return false;
            }

            // шаг 1 - закрываем снапкей

            progress.SetStep($"Updating to {latest}...");
            progress.SetProgress(10);
            var processes = Process.GetProcessesByName("SnapKeySharp");
            foreach (var p in processes)
            {
                p.Kill();
                p.WaitForExit();
            }

            // удаляем все файлы кроме лаунчера
            foreach (var file in Directory.GetFiles(InstallDir))
            {
                if (Path.GetFileName(file) != "SnapKeyLauncher.exe")
                    File.Delete(file);
            }

            // шаг 2 - скачиваем
            progress.SetStep("Downloading...");
            progress.SetProgress(0);
            string tempZip = Path.Combine(Path.GetTempPath(), "SnapKeySharp.zip");
            await DownloadFile(latestJson!.download_url, tempZip, progress, ct);

            // шаг 3 - распаковываем с перезаписью файлов
            progress.SetStep("Extracting...");
            progress.SetProgress(0);
            // распаковываем, пропуская занятые файлы
            using var zip = ZipFile.OpenRead(tempZip);
            foreach (var entry in zip.Entries)
            {
                string destPath = Path.Combine(InstallDir, entry.FullName);
                try
                {
                    entry.ExtractToFile(destPath, overwrite: true);
                }
                catch (IOException)
                {
                    // файл занят лаунчером - пропускаем
                }
            }

            // шаг 4 - запускаем
            progress.SetStep("Done!");
            progress.SetProgress(100);
            progress.HideCancel();
            await Task.Delay(800, ct);
            Process.Start(ExePath);
            progress.Dispatcher.Invoke(() => progress.Close());
            return true;
        }
        public static async Task Repair(UpdateProgressWindow progress, CancellationToken ct) 
        {
            // шаг 0 - связь с браузером
            var release = await GetLatestRelease(progress);
            if (release == null)
            {
                MessageBox.Show("Could not check for updates. Check your internet connection.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                progress.Dispatcher.Invoke(() => progress.Close());
                return;
            }
            // шаг 1 - закрываем снапкей
            progress.SetStep("Closing SnapKey...");
            progress.SetProgress(10);
            var processes = Process.GetProcessesByName("SnapKeySharp");
            foreach (var p in processes)
            {
                p.Kill();
                p.WaitForExit();
            }
            // шаг 2 - скачиваем
            progress.SetStep("Downloading...");
            progress.SetProgress(0);
            string tempZip = Path.Combine(Path.GetTempPath(), "SnapKeySharp.zip");
            await DownloadFile(release.download_url, tempZip, progress, ct);

            // шаг 3 - распаковываем с перезаписью файлов
            progress.SetStep("Extracting...");
            progress.SetProgress(0);
            // распаковываем, пропуская занятые файлы
            using var zip = ZipFile.OpenRead(tempZip);
            foreach (var entry in zip.Entries)
            {
                string destPath = Path.Combine(InstallDir, entry.FullName);
                try
                {
                    entry.ExtractToFile(destPath, overwrite: true);
                }
                catch (IOException)
                {
                    // файл занят лаунчером - пропускаем
                }
            }

            // шаг 4 - запускаем
            progress.SetStep("Done!");
            progress.SetProgress(100);
            progress.HideCancel();
            await Task.Delay(800, ct);
            Process.Start(ExePath);
            progress.Dispatcher.Invoke(() => progress.Close());
        }
        public static async Task Uninstall(UpdateProgressWindow progress)
        {
            progress.HideCancel();

            progress.SetStep("Closing SnapKey...");
            progress.SetProgress(10);
            var processes = Process.GetProcessesByName("SnapKeySharp");
            foreach (var p in processes)
            {
                p.Kill();
                p.WaitForExit();
            }

            progress.SetStep("Removing files...");
            progress.SetProgress(40);
            // удаляем все файлы кроме лаунчера и его dll
            foreach (var file in Directory.GetFiles(InstallDir))
            {
                try
                {
                    if (Path.GetFileName(file) != "SnapKeyLauncher.exe" &&
                        Path.GetFileName(file) != "SnapKeyLauncher.pdb" &&
                        Path.GetExtension(file) != ".dll")
                        File.Delete(file);
                }
                catch { }
            }

            progress.SetStep("Removing shortcuts...");
            progress.SetProgress(70);
            string startMenu = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                "Programs", "SnapKey.lnk"
            );
            if (File.Exists(startMenu))
                File.Delete(startMenu);

            progress.SetStep("Cleaning registry...");
            progress.SetProgress(90);
            Registry.LocalMachine.DeleteSubKey(RegPath, throwOnMissingSubKey: false);

            progress.SetStep("Done!");
            progress.SetProgress(100);

            await Task.Delay(800);
            SelfDestruct();

            progress.Dispatcher.Invoke(() =>
            {
                progress.Close();
                System.Windows.MessageBox.Show(
                    "SnapKey has been successfully uninstalled.",
                    "Uninstall complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information
                );
                System.Windows.Application.Current.Shutdown();
            });
        }

        private static async Task DownloadFile(string url, string dest, UpdateProgressWindow progress, CancellationToken ct)
        {
            using var client = new HttpClient();

            // получаем размер файла
            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            long totalBytes = response.Content.Headers.ContentLength ?? 0;

            // открываем поток
            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var fileStream = File.Create(dest);

            byte[] buffer = new byte[8192]; // читаем по 8KB
            long downloadedBytes = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                downloadedBytes += bytesRead;

                int percent = (int)(downloadedBytes * 100 / totalBytes);
                progress.SetProgress(percent); // обновляем UI
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath)
        {
            // используем встроенный Windows Script Host
            Type t = Type.GetTypeFromProgID("WScript.Shell")!;
            dynamic shell = Activator.CreateInstance(t)!;
            var shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = InstallDir;
            shortcut.Save();
        }

        public static async Task<ReleaseInfo?> GetLatestRelease(UpdateProgressWindow progress)
        {
            // сначала пробуем GitHub
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "SnapKeyLauncher");
                string json = await client.GetStringAsync(GithubApiUrl);
                using var doc = System.Text.Json.JsonDocument.Parse(json);

                string version = doc.RootElement.GetProperty("tag_name")
                    .GetString()?.TrimStart('v') ?? "";
                string changelog = doc.RootElement.GetProperty("body")
                    .GetString() ?? "";
                string downloadUrl = GithubUrl;

                return new ReleaseInfo { version = version, changelog = changelog, download_url = downloadUrl };
            }
            catch
            {
            }

            // GitHub не работает - переключаемся на зеркало
            progress.SetMirrorMode(); // желтый прогрессбар
            try
            {
                using var client = new HttpClient();
                string json = await client.GetStringAsync(PastebinUrl);
                return System.Text.Json.JsonSerializer.Deserialize<ReleaseInfo>(json);
            }
            catch
            {
                return null;
            }
        }

        public static async Task UpdateFromUrl(UpdateProgressWindow progress,
            string url, CancellationToken ct)
        {
            progress.HideCancel();

            progress.SetStep("Closing SnapKey...");
            progress.SetProgress(10);

            var processes = Process.GetProcessesByName("SnapKeySharp");
            foreach (var p in processes)
            {
                p.Kill();
                p.WaitForExit(3000);
            }

            progress.SetStep("Deleting old files...");


            progress.SetStep("Downloading...");
            progress.SetProgress(0);
            string tempZip = Path.Combine(Path.GetTempPath(), "SnapKeySharp.zip");
            await DownloadFile(url, tempZip, progress, ct);

            progress.SetStep("Extracting...");
            progress.SetProgress(0);
            // распаковываем, пропуская занятые файлы
            using var zip = ZipFile.OpenRead(tempZip);
            foreach (var entry in zip.Entries)
            {
                string destPath = Path.Combine(InstallDir, entry.FullName);
                try
                {
                    entry.ExtractToFile(destPath, overwrite: true);
                }
                catch (IOException)
                {
                    // файл занят лаунчером - пропускаем
                }
            }

            progress.SetStep("Done!");
            progress.SetProgress(100);
            await Task.Delay(800, ct);

            Process.Start(ExePath);
            progress.Dispatcher.BeginInvoke(() => progress.Close());

        }
        private static void SelfDestruct()
        {
            string batPath = Path.Combine(Path.GetTempPath(), "snapkey_uninstall.bat");
            string launcherPath = Environment.ProcessPath!;
            string launcherDir = Path.GetDirectoryName(launcherPath)!;

            File.WriteAllText(batPath, $@"
@echo off
timeout /t 2 /nobreak >nul
rd /s /q ""{launcherDir}""
del ""%~f0""
");

            Process.Start(new ProcessStartInfo
            {
                FileName = batPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
    }
}
