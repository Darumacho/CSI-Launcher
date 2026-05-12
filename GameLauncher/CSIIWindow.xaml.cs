using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace GameLauncher
{
    public partial class CSIIWindow : Window
    {
        private static readonly HttpClient _http = new HttpClient();

        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;

        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Jouer à CSII Forever";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Échec de l'installation";
                        break;
                    case LauncherStatus.downloadingGame:
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Téléchargement en cours";
                        break;
                    default:
                        break;
                }
            }
        }

        public CSIIWindow()
        {
            InitializeComponent();

            rootPath = Path.Combine(AppSettings.InstallPath, "CSII Forever");
            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "Build.zip");
            gameExe = Path.Combine(rootPath, "Game.exe");
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();
                try
                {
                    Version onlineVersion = new Version(_http.GetStringAsync(
                        "https://www.dropbox.com/s/udosqsch0c03lew/Version.txt?dl=1")
                        .GetAwaiter().GetResult());
                    if (onlineVersion.IsDifferentThan(localVersion))
                        InstallGameFiles(true, onlineVersion);
                    else
                        Status = LauncherStatus.ready;
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Erreur lors de la mise à jour: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        private async void InstallGameFiles(bool isUpdate, Version onlineVersion)
        {
            try
            {
                if (isUpdate)
                    Status = LauncherStatus.downloadingUpdate;
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    onlineVersion = new Version(_http.GetStringAsync(
                        "https://www.dropbox.com/s/udosqsch0c03lew/Version.txt?dl=1")
                        .GetAwaiter().GetResult());
                }

                await DownloadWithProgressAsync(
                    "https://www.dropbox.com/s/sdw7vddvdwkvlx0/Build.zip?dl=1",
                    gameZip,
                    (recv, total) => Dispatcher.Invoke(() => PlayButton.Content = total > 0
                        ? $"Téléchargement - {recv / (1024 * 1024)} sur {total / (1024 * 1024)}Mo"
                        : $"Téléchargement - {recv / (1024 * 1024)}Mo"));

                string ver = onlineVersion.ToString();
                PlayButton.Content = "Installation...";
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                    File.Delete(gameZip);
                    File.WriteAllText(versionFile, ver);
                });
                VersionText.Text = ver;
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors de l'installation: {ex}");
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (!File.Exists(gameExe) || AppSettings.AutoUpdate)
                CheckForUpdates();
            else
                Status = LauncherStatus.ready;
            LoadPatchNotes();
        }

        private void LoadPatchNotes()
        {
            try
            {
                PatchNotesText.Text = _http.GetStringAsync(
                    "https://github.com/Darumacho/CSII-Forever/releases/download/release/PatchNotes.txt")
                    .GetAwaiter().GetResult();
            }
            catch
            {
                PatchNotesText.Text = "Notes de mise à jour indisponibles.";
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                Process.Start(new ProcessStartInfo(gameExe) { WorkingDirectory = rootPath });
                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
        }

        private void Return_Click(object sender, RoutedEventArgs e)
        {
            Main main = new Main();
            main.Show();
            this.Close();
        }

        private static async Task DownloadWithProgressAsync(string url, string destPath,
            Action<long, long> onProgress)
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            long total = response.Content.Headers.ContentLength ?? -1;
            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            await using var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            var buffer = new byte[81920];
            long downloaded = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory()).ConfigureAwait(false)) > 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
                downloaded += read;
                onProgress(downloaded, total);
            }
        }
    }

    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }

        internal Version(string _version)
        {
            string[] versionStrings = _version.Trim().Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0; minor = 0; subMinor = 0;
                return;
            }
            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(Version other)
            => major != other.major || minor != other.minor || subMinor != other.subMinor;

        public override string ToString() => $"{major}.{minor}.{subMinor}";
    }
}
