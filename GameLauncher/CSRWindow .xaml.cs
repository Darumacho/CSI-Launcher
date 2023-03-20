using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;

namespace GameLauncher
{
    enum CSRLauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CSRWindow : Window
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
                        PlayButton.Content = "Jouer à CSI Rogue";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Échec de l'installation";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "Téléchargement en cours";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Téléchargement en cours";
                        break;
                    default:
                        break;
                }
            }
        }

        public CSRWindow()
        {
            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            rootPath += "/CSI Rogue";
            bool exists = System.IO.Directory.Exists(rootPath);
            if (!exists)
                System.IO.Directory.CreateDirectory(rootPath);
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "Build.zip");
            gameExe = Path.Combine(rootPath, "Build", "Game.exe");
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                VersionR localVersion = new VersionR(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    //URL Version
                    VersionR onlineVersion = new VersionR(webClient.DownloadString("https://www.dropbox.com/s/udosqsch0c03lew/Version.txt?dl=1"));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Erreur lors de la mise à jour: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, VersionR.zero);
            }
        }

        private void InstallGameFiles(bool _isUpdate, VersionR _onlineVersion)
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
                    _onlineVersion = new VersionR(webClient.DownloadString("https://www.dropbox.com/s/udosqsch0c03lew/Version.txt?dl=1"));
                }

                //URL Version
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                //URL Zip
                webClient.DownloadFileAsync(new Uri("https://www.dropbox.com/s/sdw7vddvdwkvlx0/Build.zip?dl=1"), gameZip, _onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors de l'installation: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((VersionR)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

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
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "Build");
                Process.Start(startInfo);

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
    }

    struct VersionR
    {
        internal static VersionR zero = new VersionR(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal VersionR(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }
        internal VersionR(string _version)
        {
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(VersionR _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
