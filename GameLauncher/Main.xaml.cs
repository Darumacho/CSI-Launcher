using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameLauncher
{
    public partial class Main : Window
    {
        // ─── Launcher ───────────────────────────────────────────────────────────

        private static readonly string LauncherVersionFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LauncherVersion.txt");

        private string CurrentVersion =>
            File.Exists(LauncherVersionFile) ? File.ReadAllText(LauncherVersionFile).Trim() : "1.0";

        public Main()
        {
            InitializeComponent();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            InstallPathBox.Text = AppSettings.InstallPath;
            AutoUpdateCheckBox.IsChecked = AppSettings.AutoUpdate;
            ApplyBackground(AppSettings.Background);
            foreach (ComboBoxItem item in BackgroundComboBox.Items)
                if ((string)item.Tag == AppSettings.Background) { BackgroundComboBox.SelectedItem = item; break; }
            LoadPatchNotes();
            LoadLauncherVersion();
        }

        // ─── Panel switching ─────────────────────────────────────────────────────

        private void ShowPanel(string name)
        {
            HomePanel.Visibility   = name == "Home"   ? Visibility.Visible : Visibility.Collapsed;
            CSIPanel.Visibility    = name == "CSI"    ? Visibility.Visible : Visibility.Collapsed;
            CSIIPanel.Visibility   = name == "CSII"   ? Visibility.Visible : Visibility.Collapsed;
            CSRPanel.Visibility    = name == "CSR"    ? Visibility.Visible : Visibility.Collapsed;
            NarvalPanel.Visibility = name == "Narval" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SidebarHome_Click(object sender, RoutedEventArgs e) => ShowPanel("Home");

        private void SidebarCSI_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel("CSI");
            if (!_csiLoaded) { LoadCSIGame(); _csiLoaded = true; }
        }

        private void SidebarCSII_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel("CSII");
            if (!_csiiLoaded) { LoadCSIIGame(); _csiiLoaded = true; }
        }

        private void SidebarCSR_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel("CSR");
            if (!_csrLoaded) { LoadCSRGame(); _csrLoaded = true; }
        }

        private void SidebarNarval_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel("Narval");
            if (!_narvalLoaded) { LoadNarvalGame(); _narvalLoaded = true; }
        }

        // ─── CSI Forever ────────────────────────────────────────────────────────

        private bool _csiLoaded;
        private string _csiRoot, _csiVersionFile, _csiZip, _csiExe;
        private LauncherStatus _csiStatus;
        private LauncherStatus CsiStatus
        {
            get => _csiStatus;
            set
            {
                _csiStatus = value;
                CSI_PlayButton.Content = value == LauncherStatus.ready         ? "Jouer à CSI Forever"
                                       : value == LauncherStatus.failed        ? "Échec de l'installation"
                                       : value == LauncherStatus.notInstalled  ? "Installer"
                                                                               : "Téléchargement en cours";
            }
        }

        private void LoadCSIGame()
        {
            _csiRoot        = Path.Combine(AppSettings.InstallPath, "CSI Forever");
            if (!Directory.Exists(_csiRoot)) Directory.CreateDirectory(_csiRoot);
            _csiVersionFile = Path.Combine(_csiRoot, "Version.txt");
            _csiZip         = Path.Combine(_csiRoot, "Build.zip");
            _csiExe         = Path.Combine(_csiRoot, "CSIForever/Game.exe");

            if (!File.Exists(_csiExe))
                CsiStatus = LauncherStatus.notInstalled;
            else if (AppSettings.AutoUpdate)
                CSI_CheckForUpdates();
            else
            {
                if (File.Exists(_csiVersionFile))
                    CSI_VersionText.Text = File.ReadAllText(_csiVersionFile).Trim();
                CsiStatus = LauncherStatus.ready;
            }
            CSI_LoadPatchNotes();
        }

        private void CSI_CheckForUpdates()
        {
            if (File.Exists(_csiVersionFile))
            {
                var local = new GameVersion(File.ReadAllText(_csiVersionFile));
                CSI_VersionText.Text = local.ToString();
                try
                {
                    var online = new GameVersion(new WebClient().DownloadString(
                        "https://github.com/Darumacho/CSI-Forever/releases/download/release/Version.txt"));
                    if (online.IsDifferentThan(local))
                        CSI_InstallGameFiles(true, online);
                    else
                        CsiStatus = LauncherStatus.ready;
                }
                catch (Exception ex)
                {
                    CsiStatus = LauncherStatus.failed;
                    MessageBox.Show($"Erreur lors de la mise à jour: {ex}");
                }
            }
            else
            {
                CSI_InstallGameFiles(false, GameVersion.Zero);
            }
        }

        private void CSI_InstallGameFiles(bool isUpdate, GameVersion version)
        {
            try
            {
                var wc = new WebClient();
                if (isUpdate)
                    CsiStatus = LauncherStatus.downloadingUpdate;
                else
                {
                    CsiStatus = LauncherStatus.downloadingGame;
                    version = new GameVersion(wc.DownloadString(
                        "https://github.com/Darumacho/CSI-Forever/releases/download/release/Version.txt"));
                    CSI_VersionText.Text = version.ToString();
                }
                wc.DownloadProgressChanged += (s, pe) =>
                {
                    long recv = pe.BytesReceived / (1024 * 1024);
                    long total = pe.TotalBytesToReceive / (1024 * 1024);
                    Dispatcher.Invoke(() => CSI_PlayButton.Content = total > 0
                        ? $"Téléchargement - {recv} sur {total}Mo"
                        : $"Téléchargement - {recv}Mo");
                };
                wc.DownloadFileCompleted += CSI_DownloadCompleted;
                wc.DownloadFileAsync(
                    new Uri("https://github.com/Darumacho/CSI-Forever/releases/download/release/CSI.Forever.zip"),
                    _csiZip, version);
            }
            catch (Exception ex)
            {
                CsiStatus = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors de l'installation: {ex}");
            }
        }

        private async void CSI_DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string ver = ((GameVersion)e.UserState).ToString();
                CSI_PlayButton.Content = "Installation...";
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(_csiZip, _csiRoot, true);
                    File.Delete(_csiZip);
                    File.WriteAllText(_csiVersionFile, ver);
                });
                CSI_VersionText.Text = ver;
                CsiStatus = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                CsiStatus = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors du téléchargement: {ex}");
            }
        }

        private void CSI_PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_csiExe) && _csiStatus == LauncherStatus.ready)
                Process.Start(new ProcessStartInfo(_csiExe) { WorkingDirectory = _csiRoot });
            else if (_csiStatus == LauncherStatus.failed)
            {
                if (File.Exists(_csiExe))
                    Process.Start(new ProcessStartInfo(_csiExe) { WorkingDirectory = _csiRoot });
                else
                    CSI_CheckForUpdates();
            }
            else if (_csiStatus == LauncherStatus.notInstalled)
            {
                if (ConfirmInstall("CSI Forever",
                    "https://github.com/Darumacho/CSI-Forever/releases/download/release/CSI.Forever.zip"))
                    CSI_CheckForUpdates();
            }
        }

        private void CSI_LoadPatchNotes()
        {
            try
            {
                CSI_PatchNotesText.Text = new WebClient().DownloadString(
                    "https://github.com/Darumacho/CSI-Forever/releases/download/release/PatchNotes.txt");
            }
            catch { CSI_PatchNotesText.Text = "Notes de mise à jour indisponibles."; }
        }

        // ─── CSII Forever ───────────────────────────────────────────────────────

        private bool _csiiLoaded;
        private string _csiiRoot, _csiiVersionFile, _csiiZip, _csiiExe;
        private LauncherStatus _csiiStatus;
        private LauncherStatus CsiiStatus
        {
            get => _csiiStatus;
            set
            {
                _csiiStatus = value;
                CSII_PlayButton.Content = value == LauncherStatus.ready        ? "Jouer à CSII Forever"
                                        : value == LauncherStatus.failed       ? "Échec de l'installation"
                                        : value == LauncherStatus.notInstalled ? "Installer"
                                                                               : "Téléchargement en cours";
            }
        }

        private void LoadCSIIGame()
        {
            _csiiRoot        = Path.Combine(AppSettings.InstallPath, "CSII Forever");
            if (!Directory.Exists(_csiiRoot)) Directory.CreateDirectory(_csiiRoot);
            _csiiVersionFile = Path.Combine(_csiiRoot, "Version.txt");
            _csiiZip         = Path.Combine(_csiiRoot, "Build.zip");
            _csiiExe         = Path.Combine(_csiiRoot, "Game.exe");

            if (!File.Exists(_csiiExe))
                CsiiStatus = LauncherStatus.notInstalled;
            else if (AppSettings.AutoUpdate)
                CSII_CheckForUpdates();
            else
            {
                if (File.Exists(_csiiVersionFile))
                    CSII_VersionText.Text = File.ReadAllText(_csiiVersionFile).Trim();
                CsiiStatus = LauncherStatus.ready;
            }
            CSII_LoadPatchNotes();
        }

        private void CSII_CheckForUpdates()
        {
            if (File.Exists(_csiiVersionFile))
            {
                var local = new GameVersion(File.ReadAllText(_csiiVersionFile));
                CSII_VersionText.Text = local.ToString();
                try
                {
                    var online = new GameVersion(new WebClient().DownloadString(
                        "https://www.dropbox.com/s/udosqsch0c03lew/Version.txt?dl=1"));
                    if (online.IsDifferentThan(local))
                        CSII_InstallGameFiles(true, online);
                    else
                        CsiiStatus = LauncherStatus.ready;
                }
                catch (Exception ex)
                {
                    CsiiStatus = LauncherStatus.failed;
                    MessageBox.Show($"Erreur lors de la mise à jour: {ex}");
                }
            }
            else
            {
                CSII_InstallGameFiles(false, GameVersion.Zero);
            }
        }

        private void CSII_InstallGameFiles(bool isUpdate, GameVersion version)
        {
            try
            {
                var wc = new WebClient();
                if (isUpdate)
                    CsiiStatus = LauncherStatus.downloadingUpdate;
                else
                {
                    CsiiStatus = LauncherStatus.downloadingGame;
                    version = new GameVersion(wc.DownloadString(
                        "https://www.dropbox.com/s/udosqsch0c03lew/Version.txt?dl=1"));
                    CSII_VersionText.Text = version.ToString();
                }
                wc.DownloadProgressChanged += (s, pe) =>
                {
                    long recv = pe.BytesReceived / (1024 * 1024);
                    long total = pe.TotalBytesToReceive / (1024 * 1024);
                    Dispatcher.Invoke(() => CSII_PlayButton.Content = total > 0
                        ? $"Téléchargement - {recv} sur {total}Mo"
                        : $"Téléchargement - {recv}Mo");
                };
                wc.DownloadFileCompleted += CSII_DownloadCompleted;
                wc.DownloadFileAsync(
                    new Uri("https://www.dropbox.com/s/sdw7vddvdwkvlx0/Build.zip?dl=1"),
                    _csiiZip, version);
            }
            catch (Exception ex)
            {
                CsiiStatus = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors de l'installation: {ex}");
            }
        }

        private async void CSII_DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string ver = ((GameVersion)e.UserState).ToString();
                CSII_PlayButton.Content = "Installation...";
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(_csiiZip, _csiiRoot, true);
                    File.Delete(_csiiZip);
                    File.WriteAllText(_csiiVersionFile, ver);
                });
                CSII_VersionText.Text = ver;
                CsiiStatus = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                CsiiStatus = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors du téléchargement: {ex}");
            }
        }

        private void CSII_PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_csiiExe) && _csiiStatus == LauncherStatus.ready)
                Process.Start(new ProcessStartInfo(_csiiExe) { WorkingDirectory = _csiiRoot });
            else if (_csiiStatus == LauncherStatus.failed)
                CSII_CheckForUpdates();
            else if (_csiiStatus == LauncherStatus.notInstalled)
            {
                if (ConfirmInstall("CSII Forever",
                    "https://www.dropbox.com/s/sdw7vddvdwkvlx0/Build.zip?dl=1"))
                    CSII_CheckForUpdates();
            }
        }

        private void CSII_LoadPatchNotes()
        {
            try
            {
                CSII_PatchNotesText.Text = new WebClient().DownloadString(
                    "https://github.com/Darumacho/CSII-Forever/releases/download/release/PatchNotes.txt");
            }
            catch { CSII_PatchNotesText.Text = "Notes de mise à jour indisponibles."; }
        }

        // ─── CSI Rogue ──────────────────────────────────────────────────────────

        private bool _csrLoaded;
        private string _csrRoot, _csrVersionFile, _csrZip, _csrExe;
        private LauncherStatus _csrStatus;
        private LauncherStatus CsrStatus
        {
            get => _csrStatus;
            set
            {
                _csrStatus = value;
                CSR_PlayButton.Content = value == LauncherStatus.ready        ? "Jouer à CSI Rogue"
                                       : value == LauncherStatus.failed       ? "Échec de l'installation"
                                       : value == LauncherStatus.notInstalled ? "Installer"
                                                                              : "Téléchargement en cours";
            }
        }

        private void LoadCSRGame()
        {
            _csrRoot        = Path.Combine(AppSettings.InstallPath, "CSI Rogue");
            if (!Directory.Exists(_csrRoot)) Directory.CreateDirectory(_csrRoot);
            _csrVersionFile = Path.Combine(_csrRoot, "Version.txt");
            _csrZip         = Path.Combine(_csrRoot, "Build.zip");
            _csrExe         = Path.Combine(_csrRoot, "Game.exe");

            if (!File.Exists(_csrExe))
                CsrStatus = LauncherStatus.notInstalled;
            else if (AppSettings.AutoUpdate)
                CSR_CheckForUpdates();
            else
            {
                if (File.Exists(_csrVersionFile))
                    CSR_VersionText.Text = File.ReadAllText(_csrVersionFile).Trim();
                CsrStatus = LauncherStatus.ready;
            }
            CSR_LoadPatchNotes();
        }

        private void CSR_CheckForUpdates()
        {
            if (File.Exists(_csrVersionFile))
            {
                var local = new GameVersion(File.ReadAllText(_csrVersionFile));
                CSR_VersionText.Text = local.ToString();
                try
                {
                    var online = new GameVersion(new WebClient().DownloadString(
                        "https://www.dropbox.com/s/udosqsch0c03lew/Version.txt?dl=1"));
                    if (online.IsDifferentThan(local))
                        CSR_InstallGameFiles(true, online);
                    else
                        CsrStatus = LauncherStatus.ready;
                }
                catch (Exception ex)
                {
                    CsrStatus = LauncherStatus.failed;
                    MessageBox.Show($"Erreur lors de la mise à jour: {ex}");
                }
            }
            else
            {
                CSR_InstallGameFiles(false, GameVersion.Zero);
            }
        }

        private void CSR_InstallGameFiles(bool isUpdate, GameVersion version)
        {
            try
            {
                var wc = new WebClient();
                if (isUpdate)
                    CsrStatus = LauncherStatus.downloadingUpdate;
                else
                {
                    CsrStatus = LauncherStatus.downloadingGame;
                    version = new GameVersion(wc.DownloadString(
                        "https://www.dropbox.com/s/9k566zu9r1doxtt/Version.txt?dl=1"));
                    CSR_VersionText.Text = version.ToString();
                }
                wc.DownloadProgressChanged += (s, pe) =>
                {
                    long recv = pe.BytesReceived / (1024 * 1024);
                    long total = pe.TotalBytesToReceive / (1024 * 1024);
                    Dispatcher.Invoke(() => CSR_PlayButton.Content = total > 0
                        ? $"Téléchargement - {recv} sur {total}Mo"
                        : $"Téléchargement - {recv}Mo");
                };
                wc.DownloadFileCompleted += CSR_DownloadCompleted;
                wc.DownloadFileAsync(
                    new Uri("https://www.dropbox.com/s/6gzo9x64gi5c0dw/Build.zip?dl=1"),
                    _csrZip, version);
            }
            catch (Exception ex)
            {
                CsrStatus = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors de l'installation: {ex}");
            }
        }

        private async void CSR_DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string ver = ((GameVersion)e.UserState).ToString();
                CSR_PlayButton.Content = "Installation...";
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(_csrZip, _csrRoot, true);
                    File.Delete(_csrZip);
                    File.WriteAllText(_csrVersionFile, ver);
                });
                CSR_VersionText.Text = ver;
                CsrStatus = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                CsrStatus = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors du téléchargement: {ex}");
            }
        }

        private void CSR_PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_csrExe) && _csrStatus == LauncherStatus.ready)
                Process.Start(new ProcessStartInfo(_csrExe) { WorkingDirectory = _csrRoot });
            else if (_csrStatus == LauncherStatus.failed)
                CSR_CheckForUpdates();
            else if (_csrStatus == LauncherStatus.notInstalled)
            {
                if (ConfirmInstall("CSI Rogue",
                    "https://www.dropbox.com/s/6gzo9x64gi5c0dw/Build.zip?dl=1"))
                    CSR_CheckForUpdates();
            }
        }

        private void CSR_LoadPatchNotes()
        {
            try
            {
                CSR_PatchNotesText.Text = new WebClient().DownloadString(
                    "https://github.com/Darumacho/CSI-Rogue/releases/download/release/PatchNotes.txt");
            }
            catch { CSR_PatchNotesText.Text = "Notes de mise à jour indisponibles."; }
        }

        // ─── Narval Souls ───────────────────────────────────────────────────────

        private bool _narvalLoaded;
        private string _narvalRoot, _narvalVersionFile, _narvalZip, _narvalExe;
        private LauncherStatus _narvalStatus;
        private LauncherStatus NarvalStatus
        {
            get => _narvalStatus;
            set
            {
                _narvalStatus = value;
                Narval_PlayButton.Content = value == LauncherStatus.ready        ? "Jouer à Narval Souls"
                                          : value == LauncherStatus.failed       ? "Échec de l'installation"
                                          : value == LauncherStatus.notInstalled ? "Installer"
                                                                                 : "Téléchargement en cours";
            }
        }

        private void LoadNarvalGame()
        {
            _narvalRoot        = Path.Combine(AppSettings.InstallPath, "Narval Souls");
            if (!Directory.Exists(_narvalRoot)) Directory.CreateDirectory(_narvalRoot);
            _narvalVersionFile = Path.Combine(_narvalRoot, "Version.txt");
            _narvalZip         = Path.Combine(_narvalRoot, "Build.zip");
            _narvalExe         = Path.Combine(_narvalRoot, "Narval Souls/Game.exe");

            if (!File.Exists(_narvalExe))
                NarvalStatus = LauncherStatus.notInstalled;
            else if (AppSettings.AutoUpdate)
                Narval_CheckForUpdates();
            else
            {
                if (File.Exists(_narvalVersionFile))
                    Narval_VersionText.Text = File.ReadAllText(_narvalVersionFile).Trim();
                NarvalStatus = LauncherStatus.ready;
            }
            Narval_LoadPatchNotes();
        }

        private void Narval_CheckForUpdates()
        {
            if (File.Exists(_narvalVersionFile))
            {
                var local = new GameVersion(File.ReadAllText(_narvalVersionFile));
                Narval_VersionText.Text = local.ToString();
                try
                {
                    var online = new GameVersion(new WebClient().DownloadString(
                        "https://github.com/Darumacho/Narval-Souls/releases/download/release/Version.txt"));
                    if (online.IsDifferentThan(local))
                        Narval_InstallGameFiles(true, online);
                    else
                        NarvalStatus = LauncherStatus.ready;
                }
                catch (Exception ex)
                {
                    NarvalStatus = LauncherStatus.failed;
                    MessageBox.Show($"Erreur lors de la mise à jour: {ex}");
                }
            }
            else
            {
                Narval_InstallGameFiles(false, GameVersion.Zero);
            }
        }

        private void Narval_InstallGameFiles(bool isUpdate, GameVersion version)
        {
            try
            {
                var wc = new WebClient();
                if (isUpdate)
                    NarvalStatus = LauncherStatus.downloadingUpdate;
                else
                {
                    NarvalStatus = LauncherStatus.downloadingGame;
                    version = new GameVersion(wc.DownloadString(
                        "https://github.com/Darumacho/Narval-Souls/releases/download/release/Version.txt"));
                    Narval_VersionText.Text = version.ToString();
                }
                wc.DownloadProgressChanged += (s, pe) =>
                {
                    long recv = pe.BytesReceived / (1024 * 1024);
                    long total = pe.TotalBytesToReceive / (1024 * 1024);
                    Dispatcher.Invoke(() => Narval_PlayButton.Content = total > 0
                        ? $"Téléchargement - {recv} sur {total}Mo"
                        : $"Téléchargement - {recv}Mo");
                };
                wc.DownloadFileCompleted += Narval_DownloadCompleted;
                wc.DownloadFileAsync(
                    new Uri("https://github.com/Darumacho/Narval-Souls/releases/download/release/Narval.Souls.zip"),
                    _narvalZip, version);
            }
            catch (Exception ex)
            {
                NarvalStatus = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors de l'installation: {ex}");
            }
        }

        private async void Narval_DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string ver = ((GameVersion)e.UserState).ToString();
                Narval_PlayButton.Content = "Installation...";
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(_narvalZip, _narvalRoot, true);
                    File.Delete(_narvalZip);
                    File.WriteAllText(_narvalVersionFile, ver);
                });
                Narval_VersionText.Text = ver;
                NarvalStatus = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                NarvalStatus = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors du téléchargement: {ex}");
            }
        }

        private void Narval_PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_narvalExe) && _narvalStatus == LauncherStatus.ready)
                Process.Start(new ProcessStartInfo(_narvalExe) { WorkingDirectory = _narvalRoot });
            else if (_narvalStatus == LauncherStatus.failed)
            {
                if (File.Exists(_narvalExe))
                    Process.Start(new ProcessStartInfo(_narvalExe) { WorkingDirectory = _narvalRoot });
                else
                    Narval_CheckForUpdates();
            }
            else if (_narvalStatus == LauncherStatus.notInstalled)
            {
                if (ConfirmInstall("Narval Souls",
                    "https://github.com/Darumacho/Narval-Souls/releases/download/release/Narval.Souls.zip"))
                    Narval_CheckForUpdates();
            }
        }

        private void Narval_LoadPatchNotes()
        {
            try
            {
                Narval_PatchNotesText.Text = new WebClient().DownloadString(
                    "https://github.com/Darumacho/Narval-Souls/releases/download/release/PatchNotes.txt");
            }
            catch { Narval_PatchNotesText.Text = "Notes de mise à jour indisponibles."; }
        }

        // ─── Launcher update ─────────────────────────────────────────────────────

        private void LoadLauncherVersion()
        {
            try
            {
                string onlineVersion = new WebClient().DownloadString(
                    "https://github.com/Darumacho/CSI-Launcher/releases/download/prod/Version.txt").Trim();
                LauncherVersionText.Text = $"v{onlineVersion}";

                if (onlineVersion != CurrentVersion && AppSettings.AutoUpdate)
                    PromptLauncherUpdate(onlineVersion);
            }
            catch { }
        }

        private void PromptLauncherUpdate(string newVersion)
        {
            var result = MessageBox.Show(
                $"Une nouvelle version du launcher est disponible (v{newVersion}).\nMettre à jour maintenant ?",
                "Mise à jour disponible",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
                DownloadLauncherUpdate(newVersion);
        }

        private void DownloadLauncherUpdate(string newVersion)
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string exeDir  = Path.GetDirectoryName(exePath);
            string tempZip = Path.Combine(exeDir, "launcher_update.zip");
            string tempDir = Path.Combine(exeDir, "launcher_update_temp");

            var wc = new WebClient();
            wc.DownloadProgressChanged += (s, pe) =>
            {
                Dispatcher.Invoke(() => LauncherVersionText.Text = pe.ProgressPercentage > 0
                    ? $"{pe.ProgressPercentage}%"
                    : "...");
            };
            wc.DownloadFileCompleted += async (s, e) =>
            {
                if (e.Error != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        LauncherVersionText.Text = $"v{CurrentVersion}";
                        MessageBox.Show("Échec de la mise à jour du launcher.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    File.Delete(tempZip);
                    return;
                }

                Dispatcher.Invoke(() => LauncherVersionText.Text = "Installation...");

                await Task.Run(() =>
                {
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                    ZipFile.ExtractToDirectory(tempZip, tempDir);
                    File.Delete(tempZip);
                });

                string newExe = Directory.GetFiles(tempDir, "CSILauncher.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (newExe == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        LauncherVersionText.Text = $"v{CurrentVersion}";
                        MessageBox.Show("Fichier introuvable dans l'archive de mise à jour.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    Directory.Delete(tempDir, true);
                    return;
                }

                File.WriteAllText(LauncherVersionFile, newVersion);

                string scriptPath = Path.Combine(exeDir, "launcher_update.bat");
                File.WriteAllText(scriptPath,
                    "@echo off\r\n" +
                    "timeout /t 2 /nobreak > nul\r\n" +
                    $"move /y \"{newExe}\" \"{exePath}\"\r\n" +
                    $"rmdir /s /q \"{tempDir}\"\r\n" +
                    $"start \"\" \"{exePath}\"\r\n" +
                    "del \"%~f0\"");

                Process.Start(new ProcessStartInfo(scriptPath)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });

                Dispatcher.Invoke(() => Application.Current.Shutdown());
            };

            wc.DownloadFileAsync(
                new Uri("https://github.com/Darumacho/CSI-Launcher/releases/download/prod/CSILauncher.zip"),
                tempZip);
        }

        // ─── Settings / Contact ───────────────────────────────────────────────────

        private void ApplyBackground(string filename)
        {
            BackgroundImage.Source = new BitmapImage(new Uri($"/images/{filename}", UriKind.Relative));
        }

        private void SettingsToggle_Click(object sender, RoutedEventArgs e)
        {
            bool isOpen = SettingsPanelBorder.Visibility == Visibility.Visible;
            ContactPanelBorder.Visibility = Visibility.Collapsed;
            SettingsPanelBorder.Visibility = isOpen ? Visibility.Collapsed : Visibility.Visible;
            SettingsColumn.Width = isOpen ? new GridLength(0) : new GridLength(300);
        }

        private void ContactButton_Click(object sender, RoutedEventArgs e)
        {
            bool isOpen = ContactPanelBorder.Visibility == Visibility.Visible;
            SettingsPanelBorder.Visibility = Visibility.Collapsed;
            ContactPanelBorder.Visibility = isOpen ? Visibility.Collapsed : Visibility.Visible;
            SettingsColumn.Width = isOpen ? new GridLength(0) : new GridLength(300);
        }

        private async void SendContact_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ContactEmailBox.Text))
            {
                ContactStatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                ContactStatusText.Text = "L'adresse email est requise.";
                return;
            }

            if (string.IsNullOrEmpty(AppSettings.SmtpEmail) || string.IsNullOrEmpty(AppSettings.SmtpPassword))
            {
                ContactStatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                ContactStatusText.Text = "Service d'envoi non configuré (smtp.cfg introuvable).";
                return;
            }

            ContactSendButton.IsEnabled = false;
            ContactStatusText.Foreground = new SolidColorBrush(Colors.White);
            ContactStatusText.Text = "Envoi en cours...";

            try
            {
                string senderEmail = ContactEmailBox.Text.Trim();
                string name        = ContactNameBox.Text.Trim();
                string subject     = string.IsNullOrWhiteSpace(ContactSubjectBox.Text) ? "(sans sujet)" : ContactSubjectBox.Text.Trim();
                string bodyText    = string.IsNullOrWhiteSpace(name)
                    ? $"De : {senderEmail}\n\n{ContactMessageBox.Text.Trim()}"
                    : $"De : {name} ({senderEmail})\n\n{ContactMessageBox.Text.Trim()}";

                var message = new MailMessage
                {
                    From    = new MailAddress(AppSettings.SmtpEmail, "CSI Launcher"),
                    Subject = subject,
                    Body    = bodyText
                };
                message.To.Add("darumacho@csi-world.xyz");
                message.ReplyToList.Add(new MailAddress(senderEmail, name));

                using var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl   = true,
                    Credentials = new NetworkCredential(AppSettings.SmtpEmail, AppSettings.SmtpPassword)
                };

                await smtp.SendMailAsync(message);

                ContactStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x3a, 0x9e, 0x5f));
                ContactStatusText.Text = "Message envoyé ! \nMerci bien, guidoune !";
                ContactEmailBox.Clear();
                ContactNameBox.Clear();
                ContactSubjectBox.Clear();
                ContactMessageBox.Clear();
            }
            catch (Exception ex)
            {
                ContactStatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                ContactStatusText.Text = $"Erreur : {ex.Message}";
            }
            finally
            {
                ContactSendButton.IsEnabled = true;
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = InstallPathBox.Text
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                InstallPathBox.Text = dialog.SelectedPath;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.InstallPath = InstallPathBox.Text;
            AppSettings.AutoUpdate  = AutoUpdateCheckBox.IsChecked == true;
            if (BackgroundComboBox.SelectedItem is ComboBoxItem selected)
            {
                AppSettings.Background = (string)selected.Tag;
                ApplyBackground(AppSettings.Background);
            }
            SettingsPanelBorder.Visibility = Visibility.Collapsed;
            SettingsColumn.Width = new GridLength(0);
        }

        // ─── Folder buttons ──────────────────────────────────────────────────────

        private void OpenFolder(string subFolder)
        {
            string path = Path.Combine(AppSettings.InstallPath, subFolder);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        private bool ConfirmInstall(string gameName, string zipUrl)
        {
            string sizeInfo = "";
            try
            {
                var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(zipUrl);
                req.Method = "HEAD";
                req.Timeout = 6000;
                req.AllowAutoRedirect = true;
                using (var resp = (System.Net.HttpWebResponse)req.GetResponse())
                {
                    long bytes = resp.ContentLength;
                    if (bytes > 0)
                    {
                        string size = bytes >= 1024L * 1024 * 1024
                            ? $"{bytes / (1024.0 * 1024 * 1024):F1} Go"
                            : $"{bytes / (1024 * 1024)} Mo";
                        sizeInfo = $"\nEspace requis : environ {size}";
                    }
                }
            }
            catch { }

            return MessageBox.Show(
                $"{gameName} n'est pas encore installé.{sizeInfo}\n\nVoulez-vous l'installer maintenant ?",
                $"Installer {gameName}",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        private void CSI_FolderButton_Click(object sender, RoutedEventArgs e)    => OpenFolder("CSI Forever");
        private void CSII_FolderButton_Click(object sender, RoutedEventArgs e)   => OpenFolder("CSII Forever");
        private void CSR_FolderButton_Click(object sender, RoutedEventArgs e)    => OpenFolder("CSI Rogue");
        private void Narval_FolderButton_Click(object sender, RoutedEventArgs e) => OpenFolder("Narval Souls");

        private void CSI_Uninstall_Click(object sender, RoutedEventArgs e)
            => UninstallGame("CSI Forever", _csiRoot, () => { CsiStatus = LauncherStatus.notInstalled; CSI_VersionText.Text = ""; });
        private void CSII_Uninstall_Click(object sender, RoutedEventArgs e)
            => UninstallGame("CSII Forever", _csiiRoot, () => { CsiiStatus = LauncherStatus.notInstalled; CSII_VersionText.Text = ""; });
        private void CSR_Uninstall_Click(object sender, RoutedEventArgs e)
            => UninstallGame("CSI Rogue", _csrRoot, () => { CsrStatus = LauncherStatus.notInstalled; CSR_VersionText.Text = ""; });
        private void Narval_Uninstall_Click(object sender, RoutedEventArgs e)
            => UninstallGame("Narval Souls", _narvalRoot, () => { NarvalStatus = LauncherStatus.notInstalled; Narval_VersionText.Text = ""; });

        private void UninstallGame(string gameName, string gameRoot, Action onUninstalled)
        {
            if (string.IsNullOrEmpty(gameRoot) || !Directory.Exists(gameRoot))
            {
                MessageBox.Show("Le jeu n'est pas installé.", "Désinstallation", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show(
                    $"Êtes-vous sûr de vouloir désinstaller {gameName} ?\n\nTous les fichiers du jeu seront supprimés définitivement.",
                    $"Désinstaller {gameName}",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            try
            {
                Directory.Delete(gameRoot, recursive: true);
                onUninstalled();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la désinstallation : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CSI_ExportSave_Click(object sender, RoutedEventArgs e)
            => ExportSaves("CSI Forever", _csiRoot);
        private void CSII_ExportSave_Click(object sender, RoutedEventArgs e)
            => ExportSaves("CSII Forever", _csiiRoot);
        private void CSR_ExportSave_Click(object sender, RoutedEventArgs e)
            => ExportSaves("CSI Rogue", _csrRoot);
        private void Narval_ExportSave_Click(object sender, RoutedEventArgs e)
            => ExportSaves("Narval Souls", _narvalRoot);

        private void CSI_ImportSave_Click(object sender, RoutedEventArgs e)
            => ImportSaves("CSI Forever", _csiRoot, _csiExe);
        private void CSII_ImportSave_Click(object sender, RoutedEventArgs e)
            => ImportSaves("CSII Forever", _csiiRoot, _csiiExe);
        private void CSR_ImportSave_Click(object sender, RoutedEventArgs e)
            => ImportSaves("CSI Rogue", _csrRoot, _csrExe);
        private void Narval_ImportSave_Click(object sender, RoutedEventArgs e)
            => ImportSaves("Narval Souls", _narvalRoot, _narvalExe);

        private void ExportSaves(string gameName, string searchRoot)
        {
            if (string.IsNullOrEmpty(searchRoot) || !Directory.Exists(searchRoot))
            {
                MessageBox.Show("Le jeu n'est pas installé.", "Export impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFiles = Directory.GetFiles(searchRoot, "*.rvdata2", SearchOption.AllDirectories)
                .Where(f => Path.GetFileNameWithoutExtension(f).IndexOf("save", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            string gameIniPath = Directory.GetFiles(searchRoot, "Game.ini", SearchOption.AllDirectories)
                .FirstOrDefault();
            bool hasIni = gameIniPath != null;

            if (saveFiles.Count == 0)
            {
                MessageBox.Show("Aucune sauvegarde trouvée.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string defaultName = $"{gameName} Sauvegarde - {DateTime.Now:yyyy-MM-dd}.zip";
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = defaultName,
                DefaultExt = ".zip",
                Filter = "Archives ZIP|*.zip",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                if (File.Exists(dlg.FileName)) File.Delete(dlg.FileName);

                using (var zip = ZipFile.Open(dlg.FileName, ZipArchiveMode.Create))
                {
                    foreach (var file in saveFiles)
                    {
                        string entry = Path.GetRelativePath(searchRoot, file);
                        zip.CreateEntryFromFile(file, entry);
                    }
                    if (hasIni)
                    {
                        string iniEntry = Path.GetRelativePath(searchRoot, gameIniPath);
                        zip.CreateEntryFromFile(gameIniPath, iniEntry);
                    }
                }

                MessageBox.Show(
                    $"{saveFiles.Count} sauvegarde(s) exportée(s) avec succès.",
                    "Export réussi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportSaves(string gameName, string searchRoot, string gameExePath)
        {
            if (string.IsNullOrEmpty(searchRoot) || !Directory.Exists(searchRoot))
            {
                MessageBox.Show("Le jeu n'est pas installé.", "Import impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = $"Importer une sauvegarde — {gameName}",
                DefaultExt = ".zip",
                Filter = "Archives de sauvegarde|*.zip",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                using (var zip = ZipFile.OpenRead(dlg.FileName))
                {
                    // Locate and validate Game.ini inside the archive
                    var iniEntry = zip.Entries.FirstOrDefault(e =>
                        Path.GetFileName(e.FullName).Equals("Game.ini", StringComparison.OrdinalIgnoreCase));

                    if (iniEntry == null)
                    {
                        MessageBox.Show("Archive invalide : Game.ini introuvable.", "Import impossible",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string archiveTitle = ReadGameIniTitle(iniEntry);
                    string installedIniPath = Path.Combine(
                        Path.GetDirectoryName(gameExePath) ?? searchRoot, "Game.ini");

                    if (File.Exists(installedIniPath))
                    {
                        string installedTitle = ReadGameIniTitle(installedIniPath);
                        if (!string.Equals(archiveTitle, installedTitle, StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show(
                                $"Cette archive n'est pas compatible avec {gameName}.\n\n" +
                                $"Titre dans l'archive : {archiveTitle ?? "(inconnu)"}\n" +
                                $"Titre attendu : {installedTitle ?? "(inconnu)"}",
                                "Import impossible", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    // Extract .rvdata2 save files
                    var saveEntries = zip.Entries.Where(e =>
                        e.FullName.EndsWith(".rvdata2", StringComparison.OrdinalIgnoreCase) &&
                        Path.GetFileNameWithoutExtension(e.FullName)
                            .IndexOf("save", StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();

                    if (saveEntries.Count == 0)
                    {
                        MessageBox.Show("Aucune sauvegarde dans cette archive.", "Import",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    string rootFull = Path.GetFullPath(searchRoot) + Path.DirectorySeparatorChar;
                    int count = 0;
                    foreach (var entry in saveEntries)
                    {
                        string dest = Path.GetFullPath(Path.Combine(searchRoot, entry.FullName));
                        if (!dest.StartsWith(rootFull)) continue; // zip-slip guard
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        entry.ExtractToFile(dest, overwrite: true);
                        count++;
                    }

                    MessageBox.Show($"{count} sauvegarde(s) importée(s) avec succès.", "Import réussi",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'import : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string ReadGameIniTitle(ZipArchiveEntry entry)
        {
            using (var reader = new StreamReader(entry.Open()))
                return ParseGameIniTitle(reader);
        }

        private static string ReadGameIniTitle(string filePath)
        {
            using (var reader = new StreamReader(filePath))
                return ParseGameIniTitle(reader);
        }

        private static string ParseGameIniTitle(StreamReader reader)
        {
            bool inGame = false;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.StartsWith("["))
                {
                    inGame = line.Equals("[Game]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (inGame && line.StartsWith("Title=", StringComparison.OrdinalIgnoreCase))
                    return line.Substring("Title=".Length).Trim();
            }
            return null;
        }

        // ─── External links ───────────────────────────────────────────────────────

        private void WebsiteButton_Click(object sender, RoutedEventArgs e)
            => Process.Start(new ProcessStartInfo("http://csi-world.xyz") { UseShellExecute = true });

        private void GithubButton_Click(object sender, RoutedEventArgs e)
            => Process.Start(new ProcessStartInfo("https://github.com/Darumacho") { UseShellExecute = true });

        private void DiscordButton_Click(object sender, RoutedEventArgs e)
            => Process.Start(new ProcessStartInfo("https://discord.gg/Crs7zQbaqg") { UseShellExecute = true });

        // ─── Launcher patch notes ─────────────────────────────────────────────────

        private void LoadPatchNotes()
        {
            try
            {
                PatchNotesText.Text = new WebClient().DownloadString(
                    "https://github.com/Darumacho/CSI-Launcher/releases/download/prod/PatchNotes.txt");
            }
            catch
            {
                PatchNotesText.Text = "Notes de mise à jour indisponibles.";
            }
        }
    }

    struct GameVersion
    {
        internal static GameVersion Zero = new GameVersion(0, 0, 0);

        private short major, minor, subMinor;

        internal GameVersion(short major, short minor, short subMinor)
        {
            this.major    = major;
            this.minor    = minor;
            this.subMinor = subMinor;
        }

        internal GameVersion(string version)
        {
            string[] parts = version.Trim().Split('.');
            if (parts.Length != 3)
            {
                major = minor = subMinor = 0;
                return;
            }
            major    = short.Parse(parts[0]);
            minor    = short.Parse(parts[1]);
            subMinor = short.Parse(parts[2]);
        }

        internal bool IsDifferentThan(GameVersion other)
            => major != other.major || minor != other.minor || subMinor != other.subMinor;

        public override string ToString() => $"{major}.{minor}.{subMinor}";
    }
}
