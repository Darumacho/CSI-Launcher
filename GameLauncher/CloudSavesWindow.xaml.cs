using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GameLauncher
{
    public partial class CloudSavesWindow : Window
    {
        private readonly string _gameName;
        private readonly string _gameSlug;
        private readonly string _searchRoot;
        private readonly string _gameExePath;
        private readonly string _token;

        public CloudSavesWindow(string gameName, string gameSlug, string searchRoot, string gameExePath)
        {
            InitializeComponent();
            _gameName    = gameName;
            _gameSlug    = gameSlug;
            _searchRoot  = searchRoot;
            _gameExePath = gameExePath;
            _token       = AppSettings.PlayerToken;

            TitleText.Text = $"Sauvegardes cloud — {gameName}";
            _ = LoadSavesAsync();
        }

        private async Task LoadSavesAsync()
        {
            UploadButton.IsEnabled = false;
            StatusText.Foreground = Brushes.White;
            StatusText.Text = "Chargement...";
            try
            {
                var response = await ApiService.GetCloudSavesAsync(_token);
                QuotaText.Text = $"{response.Used} / {response.Quota} emplacements utilisés ({response.Tier ?? "aucun abonnement"})";

                var items = response.Saves
                    .Where(s => s.GameSlug == _gameSlug)
                    .OrderByDescending(s => s.UploadedAt)
                    .Select(s => new CloudSaveDisplay(s))
                    .ToList();

                SavesList.ItemsSource = items;
                UploadButton.IsEnabled = response.Quota > 0;
                StatusText.Text = response.Quota > 0
                    ? (items.Count == 0 ? "Aucune sauvegarde cloud pour ce jeu." : "")
                    : "Aucun abonnement actif : l'envoi est désactivé, mais tu gardes l'accès à tes sauvegardes déjà stockées.";
            }
            catch (Exception ex)
            {
                StatusText.Foreground = Brushes.OrangeRed;
                StatusText.Text = $"Erreur de chargement : {ex.Message}";
                UploadButton.IsEnabled = false;
            }
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_searchRoot) || !Directory.Exists(_searchRoot))
            {
                MessageBox.Show("Le jeu n'est pas installé.", "Sauvegarde impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFiles = Main.CollectSaveFiles(_searchRoot);
            if (saveFiles.Count == 0)
            {
                MessageBox.Show("Aucune sauvegarde locale trouvée.", "Sauvegarde", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string tempZip = Path.Combine(Path.GetTempPath(), $"{_gameSlug}_{Guid.NewGuid():N}.zip");
            UploadButton.IsEnabled = false;
            StatusText.Foreground = Brushes.White;
            StatusText.Text = "Envoi en cours...";
            try
            {
                Main.CreateSaveZip(_searchRoot, saveFiles, tempZip);
                string label = $"{_gameName} - {DateTime.Now:yyyy-MM-dd HH:mm}";
                await ApiService.UploadCloudSaveAsync(_token, tempZip, _gameSlug, label);
                StatusText.Text = "Sauvegarde envoyée avec succès.";
                await LoadSavesAsync();
            }
            catch (Exception ex)
            {
                StatusText.Foreground = Brushes.OrangeRed;
                StatusText.Text = $"Échec de l'envoi : {ex.Message}";
                UploadButton.IsEnabled = true;
            }
            finally
            {
                try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { }
            }
        }

        private async void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((Button)sender).Tag;
            if (MessageBox.Show(
                    "Restaurer cette sauvegarde écrasera les sauvegardes actuelles du jeu installé. Continuer ?",
                    "Confirmer la restauration", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            if (string.IsNullOrEmpty(_searchRoot) || !Directory.Exists(_searchRoot))
            {
                MessageBox.Show("Le jeu n'est pas installé.", "Restauration impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string tempZip = Path.Combine(Path.GetTempPath(), $"{_gameSlug}_restore_{Guid.NewGuid():N}.zip");
            StatusText.Foreground = Brushes.White;
            StatusText.Text = "Téléchargement en cours...";
            try
            {
                byte[] data = await ApiService.DownloadCloudSaveAsync(_token, id);
                await File.WriteAllBytesAsync(tempZip, data);

                var result = Main.ExtractSaveZip(tempZip, _searchRoot, _gameExePath, _gameName);
                if (!result.Success)
                {
                    StatusText.Foreground = result.IsInformational ? Brushes.White : Brushes.OrangeRed;
                    StatusText.Text = result.Message;
                    return;
                }

                StatusText.Foreground = Brushes.White;
                StatusText.Text = $"{result.Count} sauvegarde(s) restaurée(s) avec succès.";
            }
            catch (Exception ex)
            {
                StatusText.Foreground = Brushes.OrangeRed;
                StatusText.Text = $"Échec de la restauration : {ex.Message}";
            }
            finally
            {
                try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((Button)sender).Tag;
            if (MessageBox.Show("Supprimer définitivement cette sauvegarde cloud ?", "Confirmer la suppression",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                await ApiService.DeleteCloudSaveAsync(_token, id);
                StatusText.Foreground = Brushes.White;
                StatusText.Text = "Sauvegarde supprimée.";
                await LoadSavesAsync();
            }
            catch (Exception ex)
            {
                StatusText.Foreground = Brushes.OrangeRed;
                StatusText.Text = $"Échec de la suppression : {ex.Message}";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private class CloudSaveDisplay
        {
            public int Id { get; }
            public string DisplayLabel { get; }
            public string SubText { get; }

            public CloudSaveDisplay(CloudSave save)
            {
                Id = save.Id;
                DisplayLabel = !string.IsNullOrWhiteSpace(save.Label) ? save.Label : save.FileName;
                SubText = $"{save.UploadedAt:yyyy-MM-dd HH:mm} · {FormatSize(save.SizeBytes)}";
            }

            private static string FormatSize(long bytes) =>
                bytes >= 1024 * 1024 ? $"{bytes / (1024.0 * 1024.0):0.#} Mo" : $"{bytes / 1024.0:0.#} Ko";
        }
    }
}
