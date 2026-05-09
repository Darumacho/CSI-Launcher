using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Media;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameLauncher
{
    /// <summary>
    /// Logique d'interaction pour Main.xaml
    /// </summary>
    public partial class Main : Window
    {
        private static readonly string LauncherVersionFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LauncherVersion.txt");

        private string CurrentVersion =>
            File.Exists(LauncherVersionFile) ? File.ReadAllText(LauncherVersionFile).Trim() : "1.0";

        public Main()
        {
            InitializeComponent();
        }
        private void ButtonClicked_1(object sender, RoutedEventArgs e)
        {
            CSIWindow csi1 = new CSIWindow();
            csi1.Show();
            this.Close();
        }
        private void ButtonClicked_2(object sender, RoutedEventArgs e)
        {
            CSIIWindow csi2 = new CSIIWindow();
            csi2.Show();
            this.Close();
        }

        private void ButtonClicked_R(object sender, RoutedEventArgs e)
        {
            CSRWindow csir = new CSRWindow();
            csir.Show();
            this.Close();
        }

        private void ButtonClicked_Narval(object sender, RoutedEventArgs e)
        {
            NarvalWindow narval = new NarvalWindow();
            narval.Show();
            this.Close();
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

        private void LoadLauncherVersion()
        {
            try
            {
                WebClient webClient = new WebClient();
                string onlineVersion = webClient.DownloadString("https://github.com/Darumacho/CSI-Launcher/releases/download/prod/Version.txt").Trim();
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
            string exeDir = Path.GetDirectoryName(exePath);
            string tempZip = Path.Combine(exeDir, "launcher_update.zip");
            string tempDir = Path.Combine(exeDir, "launcher_update_temp");

            WebClient webClient = new WebClient();
            webClient.DownloadProgressChanged += (s, pe) =>
            {
                Dispatcher.Invoke(() => LauncherVersionText.Text = pe.ProgressPercentage > 0
                    ? $"{pe.ProgressPercentage}%"
                    : "...");
            };
            webClient.DownloadFileCompleted += async (s, e) =>
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

            webClient.DownloadFileAsync(
                new Uri("https://github.com/Darumacho/CSI-Launcher/releases/download/prod/CSILauncher.zip"),
                tempZip);
        }

        private void ApplyBackground(string filename)
        {
            BackgroundImage.Source = new BitmapImage(new Uri($"/images/{filename}", UriKind.Relative));
        }

        private void SettingsToggle_Click(object sender, RoutedEventArgs e)
        {
            SettingsColumn.Width = SettingsColumn.Width.Value == 0
                ? new GridLength(300)
                : new GridLength(0);
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
            AppSettings.AutoUpdate = AutoUpdateCheckBox.IsChecked == true;
            if (BackgroundComboBox.SelectedItem is ComboBoxItem selected)
            {
                AppSettings.Background = (string)selected.Tag;
                ApplyBackground(AppSettings.Background);
            }
            SettingsColumn.Width = new GridLength(0);
        }

        private void LoadPatchNotes()
        {
            try
            {
                WebClient webClient = new WebClient();
                PatchNotesText.Text = webClient.DownloadString("https://github.com/Darumacho/CSI-Launcher/releases/download/prod/PatchNotes.txt");
            }
            catch
            {
                PatchNotesText.Text = "Notes de mise à jour indisponibles.";
            }
        }
    }
}
