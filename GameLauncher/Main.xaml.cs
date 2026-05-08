using System;
using System.Media;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GameLauncher
{
    /// <summary>
    /// Logique d'interaction pour Main.xaml
    /// </summary>
    public partial class Main : Window
    {
        public Main()
        {
            InitializeComponent();
        }
        private void ButtonClicked_1(object sender, RoutedEventArgs e)
        {
            CSIWindow csi1 = new CSIWindow();
            //this.SoundClick();
            csi1.Show();
            this.Close();
        }
        private void ButtonClicked_2(object sender, RoutedEventArgs e)
        {
            CSIIWindow csi2 = new CSIIWindow();
            //this.SoundClick();
            csi2.Show();
            this.Close();
        }

        private void ButtonClicked_R(object sender, RoutedEventArgs e)
        {
            CSRWindow csir = new CSRWindow();
            //this.SoundClick();
            csir.Show();
            this.Close();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            InstallPathBox.Text = AppSettings.InstallPath;
            LoadPatchNotes();
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

        private void SoundClick()
        {
            //SoundPlayer start = new SoundPlayer("C:/Users/Darumacho/Documents/Visual Studio 2019/MultiLauncher/GameLauncher/sound/Startup.wav");
            string Audio_FilePath = AppDomain.CurrentDomain.BaseDirectory + "sound\\Startup.wav";
            SoundPlayer start = new SoundPlayer(Audio_FilePath);
            start.Load();
            start.Play();
        }
    }
}
