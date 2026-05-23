using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        private static readonly HttpClient _http = new HttpClient();

        private static readonly string LauncherVersionFile =
            Path.Combine(AppSettings.ConfigDir, "LauncherVersion.txt");

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
            RefreshAccountUI();
            if (!string.IsNullOrEmpty(AppSettings.PlayerToken))
            {
                WriteTokenFiles(AppSettings.PlayerToken);
                _ = FetchAndStoreProfile();
            }
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
                var csiVis = (value == LauncherStatus.ready || value == LauncherStatus.failed) ? Visibility.Visible : Visibility.Collapsed;
                CSI_ExportButton.Visibility = csiVis;
                CSI_ImportButton.Visibility = csiVis;
                CSI_UninstallButton.Visibility = csiVis;
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
            _ = LoadItemOfTheDay(
                CSI_ItemOfTheDayBox,
                CSI_ItemOfTheDayTitle, CSI_ItemOfTheDayCategory,
                CSI_ItemOfTheDayIcon,  CSI_ItemOfTheDayProps, 0);
        }

        private void CSI_CheckForUpdates()
        {
            if (File.Exists(_csiVersionFile))
            {
                var local = new GameVersion(File.ReadAllText(_csiVersionFile));
                CSI_VersionText.Text = local.ToString();
                try
                {
                    var online = new GameVersion(_http.GetStringAsync(
                        "https://github.com/Darumacho/CSI-Forever/releases/download/release/Version.txt")
                        .GetAwaiter().GetResult());
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

        private async void CSI_InstallGameFiles(bool isUpdate, GameVersion version)
        {
            try
            {
                if (isUpdate)
                    CsiStatus = LauncherStatus.downloadingUpdate;
                else
                {
                    CsiStatus = LauncherStatus.downloadingGame;
                    version = new GameVersion(await _http.GetStringAsync(
                        "https://github.com/Darumacho/CSI-Forever/releases/download/release/Version.txt"));
                    CSI_VersionText.Text = version.ToString();
                }
                await DownloadWithProgressAsync(
                    "https://github.com/Darumacho/CSI-Forever/releases/download/release/CSI.Forever.zip",
                    _csiZip,
                    (recv, total) => Dispatcher.Invoke(() => CSI_PlayButton.Content = total > 0
                        ? $"Téléchargement - {recv / (1024 * 1024)} sur {total / (1024 * 1024)}Mo"
                        : $"Téléchargement - {recv / (1024 * 1024)}Mo"));
                string ver = version.ToString();
                CSI_PlayButton.Content = "Installation...";
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(_csiZip, _csiRoot, true);
                    File.Delete(_csiZip);
                    File.WriteAllText(_csiVersionFile, ver);
                });
                CSI_VersionText.Text = ver;
                CsiStatus = LauncherStatus.ready;
                if (!string.IsNullOrEmpty(AppSettings.PlayerToken))
                    WriteTokenFiles(AppSettings.PlayerToken);
            }
            catch (Exception ex)
            {
                CsiStatus = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors de l'installation: {ex}");
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
                CSI_PatchNotesText.Text = _http.GetStringAsync(
                    "https://github.com/Darumacho/CSI-Forever/releases/download/release/PatchNotes.txt")
                    .GetAwaiter().GetResult();
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
                var csiiVis = (value == LauncherStatus.ready || value == LauncherStatus.failed) ? Visibility.Visible : Visibility.Collapsed;
                CSII_ExportButton.Visibility = csiiVis;
                CSII_ImportButton.Visibility = csiiVis;
                CSII_UninstallButton.Visibility = csiiVis;
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
            _ = LoadItemOfTheDay(
                CSII_ItemOfTheDayBox,
                CSII_ItemOfTheDayTitle, CSII_ItemOfTheDayCategory,
                CSII_ItemOfTheDayIcon,  CSII_ItemOfTheDayProps, 1);
        }

        private void CSII_CheckForUpdates()
        {
            if (File.Exists(_csiiVersionFile))
            {
                var local = new GameVersion(File.ReadAllText(_csiiVersionFile));
                CSII_VersionText.Text = local.ToString();
                try
                {
                    var online = new GameVersion(_http.GetStringAsync(
                        "https://www.dropbox.com/s/udosqsch0c03lew/Version.txt?dl=1")
                        .GetAwaiter().GetResult());
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

        private async void CSII_InstallGameFiles(bool isUpdate, GameVersion version)
        {
            try
            {
                if (isUpdate)
                    CsiiStatus = LauncherStatus.downloadingUpdate;
                else
                {
                    CsiiStatus = LauncherStatus.downloadingGame;
                    version = new GameVersion(await _http.GetStringAsync(
                        "https://www.dropbox.com/s/udosqsch0c03lew/Version.txt?dl=1"));
                    CSII_VersionText.Text = version.ToString();
                }
                await DownloadWithProgressAsync(
                    "https://www.dropbox.com/s/sdw7vddvdwkvlx0/Build.zip?dl=1",
                    _csiiZip,
                    (recv, total) => Dispatcher.Invoke(() => CSII_PlayButton.Content = total > 0
                        ? $"Téléchargement - {recv / (1024 * 1024)} sur {total / (1024 * 1024)}Mo"
                        : $"Téléchargement - {recv / (1024 * 1024)}Mo"));
                string ver = version.ToString();
                CSII_PlayButton.Content = "Installation...";
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(_csiiZip, _csiiRoot, true);
                    File.Delete(_csiiZip);
                    File.WriteAllText(_csiiVersionFile, ver);
                });
                CSII_VersionText.Text = ver;
                CsiiStatus = LauncherStatus.ready;
                if (!string.IsNullOrEmpty(AppSettings.PlayerToken))
                    WriteTokenFiles(AppSettings.PlayerToken);
            }
            catch (Exception ex)
            {
                CsiiStatus = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors de l'installation: {ex}");
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
                CSII_PatchNotesText.Text = _http.GetStringAsync(
                    "https://github.com/Darumacho/CSII-Forever/releases/download/release/PatchNotes.txt")
                    .GetAwaiter().GetResult();
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
                var csrVis = (value == LauncherStatus.ready || value == LauncherStatus.failed) ? Visibility.Visible : Visibility.Collapsed;
                CSR_ExportButton.Visibility = csrVis;
                CSR_ImportButton.Visibility = csrVis;
                CSR_UninstallButton.Visibility = csrVis;
            }
        }

        private void LoadCSRGame()
        {
            _csrRoot        = Path.Combine(AppSettings.InstallPath, "CSI Rogue");
            if (!Directory.Exists(_csrRoot)) Directory.CreateDirectory(_csrRoot);
            _csrVersionFile = Path.Combine(_csrRoot, "Version.txt");
            _csrZip         = Path.Combine(_csrRoot, "Build.zip");
            _csrExe         = Path.Combine(_csrRoot, "CSI Roguidoune", "Game.exe");

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
            _ = LoadItemOfTheDay(
                CSR_ItemOfTheDayBox,
                CSR_ItemOfTheDayTitle, CSR_ItemOfTheDayCategory,
                CSR_ItemOfTheDayIcon,  CSR_ItemOfTheDayProps, 2);
        }

        private void CSR_CheckForUpdates()
        {
            if (File.Exists(_csrVersionFile))
            {
                var local = new GameVersion(File.ReadAllText(_csrVersionFile));
                CSR_VersionText.Text = local.ToString();
                try
                {
                    var online = new GameVersion(_http.GetStringAsync(
                        "https://www.dropbox.com/s/udosqsch0c03lew/Version.txt?dl=1")
                        .GetAwaiter().GetResult());
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

        private async void CSR_InstallGameFiles(bool isUpdate, GameVersion version)
        {
            try
            {
                if (isUpdate)
                    CsrStatus = LauncherStatus.downloadingUpdate;
                else
                {
                    CsrStatus = LauncherStatus.downloadingGame;
                    version = new GameVersion(await _http.GetStringAsync(
                        "https://www.dropbox.com/s/9k566zu9r1doxtt/Version.txt?dl=1"));
                    CSR_VersionText.Text = version.ToString();
                }
                await DownloadWithProgressAsync(
                    "https://www.dropbox.com/s/6gzo9x64gi5c0dw/Build.zip?dl=1",
                    _csrZip,
                    (recv, total) => Dispatcher.Invoke(() => CSR_PlayButton.Content = total > 0
                        ? $"Téléchargement - {recv / (1024 * 1024)} sur {total / (1024 * 1024)}Mo"
                        : $"Téléchargement - {recv / (1024 * 1024)}Mo"));
                string ver = version.ToString();
                CSR_PlayButton.Content = "Installation...";
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(_csrZip, _csrRoot, true);
                    File.Delete(_csrZip);
                    File.WriteAllText(_csrVersionFile, ver);
                });
                CSR_VersionText.Text = ver;
                CsrStatus = LauncherStatus.ready;
                if (!string.IsNullOrEmpty(AppSettings.PlayerToken))
                    WriteTokenFiles(AppSettings.PlayerToken);
            }
            catch (Exception ex)
            {
                CsrStatus = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors de l'installation: {ex}");
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
                CSR_PatchNotesText.Text = _http.GetStringAsync(
                    "https://github.com/Darumacho/CSI-Rogue/releases/download/release/PatchNotes.txt")
                    .GetAwaiter().GetResult();
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
                var narvalVis = (value == LauncherStatus.ready || value == LauncherStatus.failed) ? Visibility.Visible : Visibility.Collapsed;
                Narval_ExportButton.Visibility = narvalVis;
                Narval_ImportButton.Visibility = narvalVis;
                Narval_UninstallButton.Visibility = narvalVis;
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
            _ = LoadItemOfTheDay(
                Narval_ItemOfTheDayBox,
                Narval_ItemOfTheDayTitle, Narval_ItemOfTheDayCategory,
                Narval_ItemOfTheDayIcon,  Narval_ItemOfTheDayProps, 3);
        }

        private void Narval_CheckForUpdates()
        {
            if (File.Exists(_narvalVersionFile))
            {
                var local = new GameVersion(File.ReadAllText(_narvalVersionFile));
                Narval_VersionText.Text = local.ToString();
                try
                {
                    var online = new GameVersion(_http.GetStringAsync(
                        "https://github.com/Darumacho/Narval-Souls/releases/download/release/Version.txt")
                        .GetAwaiter().GetResult());
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

        private async void Narval_InstallGameFiles(bool isUpdate, GameVersion version)
        {
            try
            {
                if (isUpdate)
                    NarvalStatus = LauncherStatus.downloadingUpdate;
                else
                {
                    NarvalStatus = LauncherStatus.downloadingGame;
                    version = new GameVersion(await _http.GetStringAsync(
                        "https://github.com/Darumacho/Narval-Souls/releases/download/release/Version.txt"));
                    Narval_VersionText.Text = version.ToString();
                }
                await DownloadWithProgressAsync(
                    "https://github.com/Darumacho/Narval-Souls/releases/download/release/Narval.Souls.zip",
                    _narvalZip,
                    (recv, total) => Dispatcher.Invoke(() => Narval_PlayButton.Content = total > 0
                        ? $"Téléchargement - {recv / (1024 * 1024)} sur {total / (1024 * 1024)}Mo"
                        : $"Téléchargement - {recv / (1024 * 1024)}Mo"));
                string ver = version.ToString();
                Narval_PlayButton.Content = "Installation...";
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(_narvalZip, _narvalRoot, true);
                    File.Delete(_narvalZip);
                    File.WriteAllText(_narvalVersionFile, ver);
                });
                Narval_VersionText.Text = ver;
                NarvalStatus = LauncherStatus.ready;
                if (!string.IsNullOrEmpty(AppSettings.PlayerToken))
                    WriteTokenFiles(AppSettings.PlayerToken);
            }
            catch (Exception ex)
            {
                NarvalStatus = LauncherStatus.failed;
                MessageBox.Show($"Erreur lors de l'installation: {ex}");
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
                Narval_PatchNotesText.Text = _http.GetStringAsync(
                    "https://github.com/Darumacho/Narval-Souls/releases/download/release/PatchNotes.txt")
                    .GetAwaiter().GetResult();
            }
            catch { Narval_PatchNotesText.Text = "Notes de mise à jour indisponibles."; }
        }

        // ─── Daily item box ───────────────────────────────────────────────────────

        private async Task LoadItemOfTheDay(
            Border container,
            TextBlock titleBlock, TextBlock categoryBlock,
            Image iconImage, StackPanel propsPanel,
            int gameId)
        {
            try
            {
                RandomIcon rand = null;
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    var candidate = await ApiService.GetRandomIconAsync();
                    if (candidate.Category is "weapon" or "armor" or "item" && candidate.GameId == gameId)
                    { rand = candidate; break; }
                }
                if (rand == null) return;
                container.Visibility = Visibility.Visible;

                string categoryLabel = rand.Category switch
                {
                    "weapon" => "Arme du jour",
                    "armor"  => "Armure du jour",
                    _        => "Objet du jour"
                };
                GameConstants.GameNames.TryGetValue(rand.GameId, out string gameName);
                titleBlock.Text    = rand.Name;
                categoryBlock.Text = $"{categoryLabel} · {gameName ?? $"Jeu {rand.GameId}"}";

                try
                {
                    iconImage.Source = new BitmapImage(
                        new Uri($"https://csi-world.xyz/api/icon/{rand.GameId}/{rand.Icon}"));
                }
                catch { }

                propsPanel.Children.Clear();
                try
                {
                    var statusesTask = ApiService.GetStatusesAsync();
                    var elementsTask = ApiService.GetElementsAsync();

                    switch (rand.Category)
                    {
                        case "weapon":
                            var weaponsTask = ApiService.GetWeaponsAsync();
                            await Task.WhenAll(statusesTask, elementsTask, weaponsTask);
                            var w = weaponsTask.Result.FirstOrDefault(x => x.Name == rand.Name);
                            if (w != null) PopulateWeapon(w, propsPanel,
                                statusesTask.Result.ToDictionary(s => s.Id, s => s.Name),
                                elementsTask.Result.ToDictionary(e => e.Id, e => e.Name));
                            break;
                        case "armor":
                            var armorsTask = ApiService.GetArmorsAsync();
                            await Task.WhenAll(elementsTask, armorsTask);
                            var a = armorsTask.Result.FirstOrDefault(x => x.Name == rand.Name);
                            if (a != null) PopulateArmor(a, propsPanel,
                                elementsTask.Result.ToDictionary(e => e.Id, e => e.Name));
                            break;
                        case "item":
                            var itemsTask = ApiService.GetItemsAsync();
                            await Task.WhenAll(statusesTask, itemsTask);
                            var i = itemsTask.Result.FirstOrDefault(x => x.Name == rand.Name);
                            if (i != null) PopulateItem(i, propsPanel,
                                statusesTask.Result.ToDictionary(s => s.Id, s => s.Name));
                            break;
                    }
                }
                catch { }
            }
            catch { }
        }

        private void PopulateWeapon(Weapon w, StackPanel p,
            Dictionary<int, string> statusNames,
            Dictionary<int, string> elementNames)
        {
            AddDesc(w.Description, p);
            AddProp("Type", w.WeaponTypeName, p);
            AddProp("Valeur", $"{w.Value} Dollawrs", p);
            if (w.Rarity > 0) AddProp("Rareté", RarityLabel(w.Rarity), p);
            if (w.ElementId.HasValue)
                AddProp("Élément", elementNames.TryGetValue(w.ElementId.Value, out var en) ? en : w.ElementId.Value.ToString(), p);
            if (w.BonusHit.HasValue) AddProp("Coups supp.", $"+{w.BonusHit}", p);
            if (w.CriticalRate.HasValue) AddProp("Critique", $"{w.CriticalRate}%", p);
            if (w.NoSkills) AddProp("Compétences", "Aucune", p);
            if (w.GrantsSkills?.Count > 0) AddProp("Octroie", $"{w.GrantsSkills.Count} compétence(s)", p);
            AddStatusList("Inflige", w.StatusInflicted, statusNames, p);
            AddStats(w.Stats, p);
            AddMultipliers(w.Multipliers, p);
        }

        private void PopulateArmor(Armor a, StackPanel p,
            Dictionary<int, string> elementNames)
        {
            AddDesc(a.Description, p);
            AddProp("Type", a.ArmorTypeName, p);
            //AddProp("Emplacement", a.Slot.ToString(), p);
            AddProp("Valeur", $"{a.Value} Dollawrs", p);
            if (a.Rarity > 0) AddProp("Rareté", RarityLabel(a.Rarity), p);
            if (a.CriticalRate.HasValue) AddProp("Critique", $"{a.CriticalRate}%", p);
            if (a.GrantsSkills?.Count > 0) AddProp("Octroie", $"{a.GrantsSkills.Count} compétence(s)", p);
            AddElementalResistances(a.ElementalResistance, elementNames, p);
            AddStats(a.Stats, p);
            AddMultipliers(a.Multipliers, p);
        }

        private void PopulateItem(Item i, StackPanel p,
            Dictionary<int, string> statusNames)
        {
            AddDesc(i.Description, p);
            AddProp("Valeur", $"{i.Value} Dollawrs", p);
            if (i.IsKeyItem) AddProp("Objet clé", "Oui", p);
            if (i.IsMaterial) AddProp("Matériau", "Oui", p);
            if (i.FlatHeal.HasValue && i.FlatHeal != 0) AddProp("Soin PV", $"+{i.FlatHeal}", p);
            if (i.FlatDamage.HasValue && i.FlatDamage != 0) AddProp("Dégâts", i.FlatDamage.ToString(), p);
            if (!string.IsNullOrWhiteSpace(i.SpecialEffect)) AddProp("Effet", i.SpecialEffect, p);
            AddStatusImmunity(i.StatusImmunity, statusNames, p);
            if (i.GrantsSkills?.Count > 0) AddProp("Octroie", $"{i.GrantsSkills.Count} compétence(s)", p);
            AddStats(i.Stats, p);
        }

        private static void AddDesc(string desc, StackPanel p)
        {
            if (string.IsNullOrWhiteSpace(desc)) return;
            p.Children.Add(new TextBlock
            {
                FontFamily   = new FontFamily("Optimus"),
                FontSize     = 10,
                FontStyle    = FontStyles.Italic,
                Foreground   = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)),
                TextWrapping = TextWrapping.Wrap,
                Text         = desc,
                Margin       = new Thickness(0, 0, 0, 5)
            });
        }

        private static void AddProp(string label, string value, StackPanel p)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            p.Children.Add(new TextBlock
            {
                FontFamily   = new FontFamily("Optimus"),
                FontSize     = 11,
                Foreground   = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Text         = $"{label}: {value}",
                Margin       = new Thickness(0, 1, 0, 1)
            });
        }

        private static void AddStats(Stats s, StackPanel p)
        {
            if (s == null) return;
            if (s.PvMax != 0)   AddProp("PV",  s.PvMax.ToString(),   p);
            if (s.EgMax != 0)   AddProp("EG",  s.EgMax.ToString(),   p);
            if (s.Attaque != 0) AddProp("ATQ", s.Attaque.ToString(), p);
            if (s.Defense != 0) AddProp("DEF", s.Defense.ToString(), p);
            if (s.Arcane != 0)  AddProp("ARC", s.Arcane.ToString(),  p);
            if (s.Sagesse != 0) AddProp("SAG", s.Sagesse.ToString(), p);
            if (s.Vitesse != 0) AddProp("VIT", s.Vitesse.ToString(), p);
            if (s.Finesse != 0) AddProp("FIN", s.Finesse.ToString(), p);
        }

        private static void AddMultipliers(Multipliers m, StackPanel p)
        {
            if (m == null) return;
            if (m.PvMax.HasValue)   AddProp("PV ×",  m.PvMax.Value.ToString("0.##"),   p);
            if (m.EgMax.HasValue)   AddProp("EG ×",  m.EgMax.Value.ToString("0.##"),   p);
            if (m.Attaque.HasValue) AddProp("ATQ ×", m.Attaque.Value.ToString("0.##"), p);
            if (m.Defense.HasValue) AddProp("DEF ×", m.Defense.Value.ToString("0.##"), p);
            if (m.Arcane.HasValue)  AddProp("ARC ×", m.Arcane.Value.ToString("0.##"),  p);
            if (m.Sagesse.HasValue) AddProp("SAG ×", m.Sagesse.Value.ToString("0.##"), p);
            if (m.Vitesse.HasValue) AddProp("VIT ×", m.Vitesse.Value.ToString("0.##"), p);
            if (m.Finesse.HasValue) AddProp("FIN ×", m.Finesse.Value.ToString("0.##"), p);
        }

        private static void AddStatusList(string label, List<StatusEffect> effects,
            Dictionary<int, string> statusNames, StackPanel p)
        {
            if (effects == null || effects.Count == 0) return;
            foreach (var e in effects)
            {
                statusNames.TryGetValue(e.StatusId, out string name);
                string display = name ?? $"#{e.StatusId}";
                AddProp(label, e.Probability < 100 ? $"{display} ({e.Probability}%)" : display, p);
            }
        }

        private static void AddStatusImmunity(List<StatusEffect> effects,
            Dictionary<int, string> statusNames, StackPanel p)
        {
            if (effects == null || effects.Count == 0) return;
            foreach (var e in effects)
            {
                statusNames.TryGetValue(e.StatusId, out string name);
                AddProp("Immunité", name ?? $"#{e.StatusId}", p);
            }
        }

        private static void AddElementalResistances(List<ElementalResistance> resistances,
            Dictionary<int, string> elementNames, StackPanel p)
        {
            if (resistances == null || resistances.Count == 0) return;
            foreach (var r in resistances)
            {
                elementNames.TryGetValue(r.ElementId, out string name);
                AddProp("Résistance", $"{name ?? $"#{r.ElementId}"} ×{r.Multiplier:0.##}", p);
            }
        }

        private static string RarityLabel(int rarity) => rarity switch
        {
            1 => "Ordinaire",
            2 => "Peu commun",
            3 => "Rare",
            4 => "Épique",
            5 => "Légendaire",
            6 => "Unique",
            7 => "Héroïque",
            8 => "Séraphin",
            9 => "Nacré",
            _ => rarity.ToString()
        };

        // ─── Launcher update ─────────────────────────────────────────────────────

        private void LoadLauncherVersion()
        {
            try
            {
                string onlineVersion = _http.GetStringAsync(
                    "https://github.com/Darumacho/CSI-Launcher/releases/download/prod/Version.txt")
                    .GetAwaiter().GetResult().Trim();
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

        private async void DownloadLauncherUpdate(string newVersion)
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string exeDir  = Path.GetDirectoryName(exePath);
            string tempZip = Path.Combine(exeDir, "launcher_update.zip");
            string tempDir = Path.Combine(exeDir, "launcher_update_temp");

            try
            {
                await DownloadWithProgressAsync(
                    "https://github.com/Darumacho/CSI-Launcher/releases/download/prod/CSILauncher.zip",
                    tempZip,
                    (recv, total) => Dispatcher.Invoke(() =>
                        LauncherVersionText.Text = total > 0 ? $"{recv * 100 / total}%" : "..."));

                LauncherVersionText.Text = "Installation...";

                await Task.Run(() =>
                {
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                    ZipFile.ExtractToDirectory(tempZip, tempDir);
                    File.Delete(tempZip);
                });

                string newExe = Directory.GetFiles(tempDir, "CSILauncher.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (newExe == null)
                {
                    LauncherVersionText.Text = $"v{CurrentVersion}";
                    MessageBox.Show("Fichier introuvable dans l'archive de mise à jour.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    Directory.Delete(tempDir, true);
                    return;
                }

                Directory.CreateDirectory(AppSettings.ConfigDir);
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

                Application.Current.Shutdown();
            }
            catch (Exception)
            {
                LauncherVersionText.Text = $"v{CurrentVersion}";
                MessageBox.Show("Échec de la mise à jour du launcher.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                if (File.Exists(tempZip)) File.Delete(tempZip);
            }
        }

        // ─── Download helper ─────────────────────────────────────────────────────

        // onProgress is called from a thread-pool thread — callers must use Dispatcher.Invoke for UI updates.
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
                using var req = new HttpRequestMessage(HttpMethod.Head, zipUrl);
                using var resp = _http.Send(req);
                long bytes = resp.Content.Headers.ContentLength ?? -1;
                if (bytes > 0)
                {
                    string size = bytes >= 1024L * 1024 * 1024
                        ? $"{bytes / (1024.0 * 1024 * 1024):F1} Go"
                        : $"{bytes / (1024 * 1024)} Mo";
                    sizeInfo = $"\nEspace requis : environ {size}";
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

        // ─── Account panel ───────────────────────────────────────────────────────

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            bool isOpen = AccountPanelBorder.Visibility == Visibility.Visible;
            SettingsPanelBorder.Visibility = Visibility.Collapsed;
            ContactPanelBorder.Visibility  = Visibility.Collapsed;
            AccountPanelBorder.Visibility  = isOpen ? Visibility.Collapsed : Visibility.Visible;
            SettingsColumn.Width = isOpen ? new GridLength(0) : new GridLength(300);
        }

        private void RefreshAccountUI()
        {
            bool loggedIn = !string.IsNullOrEmpty(AppSettings.PlayerToken);
            AccountLoggedOutPanel.Visibility = loggedIn ? Visibility.Collapsed : Visibility.Visible;
            AccountLoggedInPanel.Visibility  = loggedIn ? Visibility.Visible   : Visibility.Collapsed;
            if (!loggedIn) return;

            AccountWelcomeText.Text = AppSettings.PlayerUsername;

            int? achCount = AppSettings.PlayerAchievementCount;
            int? achScore = AppSettings.PlayerAchievementScore;
            if (achCount.HasValue)
            {
                string label = achCount.Value == 1 ? "succès" : "succès";
                AccountAchievementText.Text = $"{achCount} {label} · {achScore ?? 0} G";
                AccountAchievementBadge.Visibility = Visibility.Visible;
            }
            else
            {
                AccountAchievementBadge.Visibility = Visibility.Collapsed;
            }

            string avatarUrl = AppSettings.PlayerAvatarUrl;
            if (!string.IsNullOrEmpty(avatarUrl))
                try { AccountAvatarImage.Source = new BitmapImage(new Uri(avatarUrl)); } catch { }
            else
                AccountAvatarImage.Source = null;

            bool hasMoney = AppSettings.PlayerMoney.HasValue || AppSettings.PlayerPremiumMoney.HasValue;
            AccountMoneyPanel.Visibility = hasMoney ? Visibility.Visible : Visibility.Collapsed;
            if (hasMoney)
            {
                AccountMoneyText.Text        = $"{AppSettings.PlayerMoney ?? 0} Dollawrs";
                AccountPremiumMoneyText.Text = $"{AppSettings.PlayerPremiumMoney ?? 0} Crédits MasterMoney";
            }

            string desc = AppSettings.PlayerDescription;
            AccountDescriptionPanel.Visibility = !string.IsNullOrEmpty(desc) ? Visibility.Visible : Visibility.Collapsed;
            AccountDescriptionText.Text = desc ?? "";

            /*string email = AppSettings.PlayerEmail;
            AccountEmailText.Visibility = !string.IsNullOrEmpty(email) ? Visibility.Visible : Visibility.Collapsed;
            AccountEmailText.Text = email ?? "";*/
        }

        private async Task FetchAndStoreProfile()
        {
            try
            {
                var meTask     = ApiService.GetMyProfileAsync(AppSettings.PlayerToken);
                var pubTask    = ApiService.GetPlayerAsync(AppSettings.PlayerUsername);
                await Task.WhenAll(meTask, pubTask);

                var me  = meTask.Result;
                var pub = pubTask.Result;

                AppSettings.PlayerAvatarUrl    = me.AvatarUrl;
                AppSettings.PlayerEmail        = me.Email;
                AppSettings.PlayerDescription  = me.Description;
                AppSettings.PlayerMoney        = me.Money;
                AppSettings.PlayerPremiumMoney = me.PremiumMoney;

                var achievements = pub.Achievements;
                AppSettings.PlayerAchievementCount = achievements?.Count;
                AppSettings.PlayerAchievementScore = achievements?.Sum(a => a.PointsValue);

                RefreshAccountUI();
                if (!string.IsNullOrEmpty(me.Email))
                    ContactEmailBox.Text = me.Email;
            }
            catch { }
        }

        private async void AccountLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = AccountUsernameBox.Text.Trim();
            string password = AccountPasswordBox.Password;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                AccountStatusText.Text = "Remplis tous les champs.";
                AccountStatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                return;
            }
            AccountStatusText.Text = "Connexion...";
            AccountStatusText.Foreground = new SolidColorBrush(Colors.White);
            try
            {
                var result = await ApiService.LoginAsync(username, password);
                AppSettings.PlayerToken    = result.Token;
                AppSettings.PlayerUsername = result.Username;
                WriteTokenFiles(result.Token);
                AccountPasswordBox.Clear();
                AccountStatusText.Text = "";
                RefreshAccountUI();
                _ = FetchAndStoreProfile();
            }
            catch
            {
                AccountStatusText.Text = "Identifiant ou mot de passe incorrect.";
                AccountStatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
        }

        private async void AccountRegister_Click(object sender, RoutedEventArgs e)
        {
            string username = AccountUsernameBox.Text.Trim();
            string password = AccountPasswordBox.Password;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                AccountStatusText.Text = "Remplis tous les champs.";
                AccountStatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                return;
            }
            AccountStatusText.Text = "Création du compte...";
            AccountStatusText.Foreground = new SolidColorBrush(Colors.White);
            try
            {
                var result = await ApiService.RegisterAsync(username, password);
                AppSettings.PlayerToken    = result.Token;
                AppSettings.PlayerUsername = result.Username;
                WriteTokenFiles(result.Token);
                AccountPasswordBox.Clear();
                AccountStatusText.Text = "";
                RefreshAccountUI();
                _ = FetchAndStoreProfile();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                AccountStatusText.Text = "Ce nom d'utilisateur est déjà pris.";
                AccountStatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                AccountStatusText.Text = "Nom invalide ou mot de passe trop court (8 caractères min).";
                AccountStatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
            catch
            {
                AccountStatusText.Text = "Erreur lors de la création du compte.";
                AccountStatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
        }

        private void AccountLogout_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.PlayerToken        = null;
            AppSettings.PlayerUsername     = null;
            AppSettings.PlayerAvatarUrl    = null;
            AppSettings.PlayerEmail        = null;
            AppSettings.PlayerDescription  = null;
            AppSettings.PlayerMoney             = null;
            AppSettings.PlayerPremiumMoney      = null;
            AppSettings.PlayerAchievementCount  = null;
            AppSettings.PlayerAchievementScore  = null;
            DeleteTokenFiles();
            RefreshAccountUI();
        }

        private static string[] GameExePaths() => new[]
        {
            Path.Combine(AppSettings.InstallPath, "CSI Forever",  "CSIForever",   "Game.exe"),
            Path.Combine(AppSettings.InstallPath, "CSII Forever",                 "Game.exe"),
            Path.Combine(AppSettings.InstallPath, "CSI Rogue",   "CSI Roguidoune", "Game.exe"),
            Path.Combine(AppSettings.InstallPath, "Narval Souls", "Narval Souls", "Game.exe"),
        };

        private static void WriteTokenFiles(string token)
        {
            foreach (string exe in GameExePaths())
            {
                if (!File.Exists(exe)) continue;
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(exe), "Token.ini"), $"TOKEN={token}");
            }
        }

        private static void DeleteTokenFiles()
        {
            foreach (string exe in GameExePaths())
            {
                if (!File.Exists(exe)) continue;
                string tokenFile = Path.Combine(Path.GetDirectoryName(exe), "Token.ini");
                if (File.Exists(tokenFile)) File.Delete(tokenFile);
            }
        }

        // ─── Launcher patch notes ─────────────────────────────────────────────────

        private void LoadPatchNotes()
        {
            try
            {
                PatchNotesText.Text = _http.GetStringAsync(
                    "https://github.com/Darumacho/CSI-Launcher/releases/download/prod/PatchNotes.txt")
                    .GetAwaiter().GetResult();
            }
            catch
            {
                PatchNotesText.Text = "Notes de mise à jour indisponibles.";
            }
        }
    }

    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate,
        notInstalled
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
