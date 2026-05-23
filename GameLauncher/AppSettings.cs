using System;
using System.Collections.Generic;
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

        private static readonly string IniFile =
            Path.Combine(ConfigDir, "settings.ini");

        private static Dictionary<string, string> _values;

        private static Dictionary<string, string> Values
        {
            get
            {
                _values ??= Load();
                return _values;
            }
        }

        private static Dictionary<string, string> Load()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(IniFile))
            {
                foreach (var line in File.ReadAllLines(IniFile))
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                        dict[parts[0].Trim()] = parts[1].Trim();
                }
            }

            MigrateOldFiles(dict);

            return dict;
        }

        private static void MigrateOldFiles(Dictionary<string, string> dict)
        {
            bool changed = false;

            string settingsFile = Path.Combine(ConfigDir, "settings.txt");
            if (File.Exists(settingsFile))
            {
                string val = File.ReadAllText(settingsFile).Trim();
                if (!string.IsNullOrEmpty(val) && !dict.ContainsKey("install_path"))
                    dict["install_path"] = val;
                File.Delete(settingsFile);
                changed = true;
            }

            string autoUpdateFile = Path.Combine(ConfigDir, "autoupdate.txt");
            if (File.Exists(autoUpdateFile))
            {
                string val = File.ReadAllText(autoUpdateFile).Trim();
                if (!string.IsNullOrEmpty(val) && !dict.ContainsKey("auto_update"))
                    dict["auto_update"] = val;
                File.Delete(autoUpdateFile);
                changed = true;
            }

            string backgroundFile = Path.Combine(ConfigDir, "background.txt");
            if (File.Exists(backgroundFile))
            {
                string val = File.ReadAllText(backgroundFile).Trim();
                if (!string.IsNullOrEmpty(val) && !dict.ContainsKey("background"))
                    dict["background"] = val;
                File.Delete(backgroundFile);
                changed = true;
            }

            if (changed)
                Save(dict);
        }

        private static void Save(Dictionary<string, string> dict)
        {
            Directory.CreateDirectory(ConfigDir);
            File.WriteAllLines(IniFile, dict.Select(kv => $"{kv.Key}={kv.Value}"));
        }

        private static void Set(string key, string value)
        {
            Values[key] = value;
            Save(Values);
        }

        public static string InstallPath
        {
            get
            {
                if (Values.TryGetValue("install_path", out string path) &&
                    !string.IsNullOrEmpty(path) && Directory.Exists(path))
                    return path;
                return AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            }
            set => Set("install_path", value.TrimEnd('\\', '/'));
        }

        public static string Background
        {
            get
            {
                if (Values.TryGetValue("background", out string val) && !string.IsNullOrEmpty(val))
                    return val;
                return "BackgroundMain.png";
            }
            set => Set("background", value);
        }

        public static bool AutoUpdate
        {
            get
            {
                if (Values.TryGetValue("auto_update", out string val))
                    return val != "false";
                return true;
            }
            set => Set("auto_update", value ? "true" : "false");
        }

        public static string PlayerToken
        {
            get => Values.TryGetValue("player_token", out string v) ? v : null;
            set => Set("player_token", value ?? "");
        }

        public static string PlayerUsername
        {
            get => Values.TryGetValue("player_username", out string v) && !string.IsNullOrEmpty(v) ? v : null;
            set => Set("player_username", value ?? "");
        }

        public static string PlayerEmail
        {
            get => Values.TryGetValue("player_email", out string v) && !string.IsNullOrEmpty(v) ? v : null;
            set => Set("player_email", value ?? "");
        }

        public static string PlayerAvatarUrl
        {
            get => Values.TryGetValue("player_avatar_url", out string v) && !string.IsNullOrEmpty(v) ? v : null;
            set => Set("player_avatar_url", value ?? "");
        }

        public static string PlayerDescription
        {
            get => Values.TryGetValue("player_description", out string v) && !string.IsNullOrEmpty(v) ? v : null;
            set => Set("player_description", value ?? "");
        }

        public static int? PlayerMoney
        {
            get => Values.TryGetValue("player_money", out string v) && int.TryParse(v, out int n) ? n : null;
            set => Set("player_money", value?.ToString() ?? "");
        }

        public static int? PlayerPremiumMoney
        {
            get => Values.TryGetValue("player_premium_money", out string v) && int.TryParse(v, out int n) ? n : null;
            set => Set("player_premium_money", value?.ToString() ?? "");
        }

        public static int? PlayerAchievementCount
        {
            get => Values.TryGetValue("player_achievement_count", out string v) && int.TryParse(v, out int n) ? n : null;
            set => Set("player_achievement_count", value?.ToString() ?? "");
        }

        public static int? PlayerAchievementScore
        {
            get => Values.TryGetValue("player_achievement_score", out string v) && int.TryParse(v, out int n) ? n : null;
            set => Set("player_achievement_score", value?.ToString() ?? "");
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
    }
}
