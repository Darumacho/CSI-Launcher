using System;
using System.IO;

namespace GameLauncher
{
    static class AppSettings
    {
        private static readonly string SettingsFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.txt");

        private static readonly string AutoUpdateFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "autoupdate.txt");

        private static readonly string BackgroundFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "background.txt");

        public static string InstallPath
        {
            get
            {
                if (File.Exists(SettingsFile))
                {
                    string path = File.ReadAllText(SettingsFile).Trim();
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                        return path;
                }
                return AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            }
            set => File.WriteAllText(SettingsFile, value.TrimEnd('\\', '/'));
        }

        public static string Background
        {
            get
            {
                if (File.Exists(BackgroundFile))
                {
                    string val = File.ReadAllText(BackgroundFile).Trim();
                    if (!string.IsNullOrEmpty(val))
                        return val;
                }
                return "BackgroundMain.png";
            }
            set => File.WriteAllText(BackgroundFile, value);
        }

        public static bool AutoUpdate
        {
            get
            {
                if (File.Exists(AutoUpdateFile))
                    return File.ReadAllText(AutoUpdateFile).Trim() != "false";
                return true;
            }
            set => File.WriteAllText(AutoUpdateFile, value ? "true" : "false");
        }
    }
}
