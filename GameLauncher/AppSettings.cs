using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GameLauncher
{
    static class AppSettings
    {
        private static readonly string BaseDir =
            Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        internal static readonly string ConfigDir =
            Path.Combine(BaseDir, "Config");

        private static readonly string SettingsFile =
            Path.Combine(ConfigDir, "settings.txt");

        private static readonly string AutoUpdateFile =
            Path.Combine(ConfigDir, "autoupdate.txt");

        private static readonly string BackgroundFile =
            Path.Combine(ConfigDir, "background.txt");

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
            set { Directory.CreateDirectory(ConfigDir); File.WriteAllText(SettingsFile, value.TrimEnd('\\', '/')); }
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
            set { Directory.CreateDirectory(ConfigDir); File.WriteAllText(BackgroundFile, value); }
        }

        public static string SmtpEmail => ReadSmtpConfig("email");
        public static string SmtpPassword => ReadSmtpConfig("password");

        private static string ReadSmtpConfig(string key)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resource = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("smtp.cfg", StringComparison.OrdinalIgnoreCase));
            if (resource == null) return null;

            using var stream = assembly.GetManifestResourceStream(resource);
            using var reader = new StreamReader(stream);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split('=', 2);
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
            set { Directory.CreateDirectory(ConfigDir); File.WriteAllText(AutoUpdateFile, value ? "true" : "false"); }
        }
    }
}
