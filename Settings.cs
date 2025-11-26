using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

namespace ThinkMine
{
    public class AppSettings
    {
        public string FontFamily { get; set; } = "Segoe UI";
        public double FontSize { get; set; } = 24;
        public string TextColor { get; set; } = "#000000"; // Hex color
        public double WindowWidth { get; set; } = 800;
        public string LastVersion { get; set; } = "0.0.0";

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
                string dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                
                string json = JsonSerializer.Serialize(this);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
