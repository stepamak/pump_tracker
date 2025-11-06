using System;
using System.IO;
using System.Text;
using System.Text.Json;
using SolanaPumpTracker.Models;

namespace SolanaPumpTracker.Services
{
    public static class SettingsService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static string AppDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SolanaPumpTracker");
        public static string FilePath => Path.Combine(AppDir, "settings.json");

        public static Settings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath, Encoding.UTF8);
                    return JsonSerializer.Deserialize<Settings>(json, JsonOpts) ?? new Settings();
                }
            }
            catch {  }
            return new Settings();
        }

        public static void Save(Settings s)
        {
            Directory.CreateDirectory(AppDir);
            var json = JsonSerializer.Serialize(s, JsonOpts);
            File.WriteAllText(FilePath, json, Encoding.UTF8);
        }
    }
}
