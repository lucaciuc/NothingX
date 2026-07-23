using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace NothingX.Models
{
    public class AppSettings
    {
        public double WindowTop { get; set; } = double.NaN;
        public double WindowLeft { get; set; } = double.NaN;
        public double WindowWidth { get; set; } = 900;
        public double WindowHeight { get; set; } = 600;
        public WindowState WindowState { get; set; } = WindowState.Normal;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NothingX", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
