using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace GameLauncher
{
    public partial class NarvalWindow : Window
    {
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
                        PlayButton.Content = "Jouer à Narval Souls";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Échec de l'installation";
                        break;
                    case LauncherStatus.downloadingGame:
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Téléchargement en cours";
                        break;
                }
            }
        }

        public NarvalWindow()
        {
            InitializeComponent();
            rootPath = Path.Combine(AppSettings.InstallPath, "Narval Souls");
            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "Build.zip");
            gameExe = Path.Combine(rootPath, "Narval Souls/Game.exe");
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                NarvalVersion localVersion = new NarvalVersion(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();
                try
                {
                    WebClient webClient = new WebClient();
                    NarvalVersion onlineVersion = new NarvalVersion(webClient.DownloadString("https://github.com/Darumacho/Narval-Souls/releases/download/release/Version.txt"));
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
                InstallGameFiles(false, NarvalVersion.zero);
            }
        }

        private void InstallGameFiles(bool _isUpdate, NarvalVersion _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new NarvalVersion(webClient.DownloadString("https://github.com/Darumacho/Narval-Souls/releases/download/release/Version.txt"));
                }
                webClient.DownloadProgressChanged += (s, pe) =>
                {
                    long receivedMB = pe.BytesReceived / (1024 * 1024);
                    long totalMB = pe.TotalBytesToReceive / (1024 * 1024);
                    Dispatcher.Invoke(() => PlayButton.Content = totalMB > 0
                        ? $"Téléchargement - {receivedMB} sur {totalMB}Mo"
                        : $"Téléchargement - {receivedMB}Mo");
                };
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri("https://github.com/Darumacho/Narval-Souls/releases/download/release/Narval.Souls.zip"), gameZip, _onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors de l'installation: {ex}");
            }
        }

        private async void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((NarvalVersion)e.UserState).ToString();
                PlayButton.Content = "Installation...";
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                    File.Delete(gameZip);
                    File.WriteAllText(versionFile, onlineVersion);
                });
                VersionText.Text = onlineVersion;
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors du téléchargement: {ex}");
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
            LoadPatchNotes();
        }

        private void LoadPatchNotes()
        {
            try
            {
                WebClient webClient = new WebClient();
                PatchNotesText.Text = webClient.DownloadString("https://github.com/Darumacho/Narval-Souls/releases/download/release/PatchNotes.txt");
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
                if (File.Exists(gameExe))
                {
                    Process.Start(new ProcessStartInfo(gameExe) { WorkingDirectory = rootPath });
                    Close();
                }
                else
                {
                    CheckForUpdates();
                }
            }
        }

        private void Return_Click(object sender, RoutedEventArgs e)
        {
            Main main = new Main();
            main.Show();
            Close();
        }
    }

    struct NarvalVersion
    {
        internal static NarvalVersion zero = new NarvalVersion(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal NarvalVersion(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }

        internal NarvalVersion(string _version)
        {
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0; minor = 0; subMinor = 0;
                return;
            }
            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(NarvalVersion other)
        {
            return major != other.major || minor != other.minor || subMinor != other.subMinor;
        }

        public override string ToString() => $"{major}.{minor}.{subMinor}";
    }
}
