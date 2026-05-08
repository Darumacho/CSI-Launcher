using System;
using System.IO;

namespace GameLauncher
{
    static class AppSettings
    {
        private static readonly string SettingsFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.txt");

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
    }
}
