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
        public double FontSize { get; set; } = 36;
        public string TextColor { get; set; } = "#000000"; // Hex color
        public double WindowWidth { get; set; } = 800;
        public double WindowHeight { get; set; } = 600;
        public double WindowTop { get; set; } = 100;
        public double WindowLeft { get; set; } = 100;
        public bool IsFullScreen { get; set; } = false;
        public int BackgroundIndex { get; set; } = 0;
        public string CurrentFontFamily { get; set; } = "Segoe UI"; // Persist font name directly
        public string LastTextColor { get; set; } = "#000000";
        public int LastTimerSeconds { get; set; } = 0;
        public bool IsBold { get; set; } = false;
        public bool IsItalic { get; set; } = false;
        public bool IsFirstRun { get; set; } = true;
        public bool HasSeenTutorial { get; set; } = false;
        public List<string> RecentFiles { get; set; } = new List<string>();
        public string ClientId { get; set; } = Guid.NewGuid().ToString();

        private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ThinkMine", "settings.json");

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
