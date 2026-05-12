using System;
using System.Diagnostics;
using System.IO;

namespace GameLauncher
{
    static class AppSettings
    {
        private static readonly string BaseDir =
            Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        private static readonly string SettingsFile =
            Path.Combine(BaseDir, "settings.txt");

        private static readonly string AutoUpdateFile =
            Path.Combine(BaseDir, "autoupdate.txt");

        private static readonly string BackgroundFile =
            Path.Combine(BaseDir, "background.txt");

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

        private static readonly string SmtpFile =
            Path.Combine(BaseDir, "smtp.cfg");

        public static string SmtpEmail => ReadSmtpConfig("email");
        public static string SmtpPassword => ReadSmtpConfig("password");

        private static string ReadSmtpConfig(string key)
        {
            if (!File.Exists(SmtpFile)) return null;
            foreach (var line in File.ReadAllLines(SmtpFile))
            {
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2 && parts[0].Trim() == key)
                    return parts[1].Trim();
            }
            return null;
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
