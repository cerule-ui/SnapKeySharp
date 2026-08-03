using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.IO;

namespace SnapKeySharp.Services
{
    internal class ConfigService
    {
        private static string _configPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SnapKeySharp", "config.json"
            );

        public static AppConfig Load()
        {
            if (!File.Exists(_configPath))
            {
                return new AppConfig();
            }
            string json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }

        public static void Save(AppConfig config)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_configPath)!);
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
    }

}
