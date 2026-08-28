using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GameLauncher
{
    public partial class AchievementsWindow : Window
    {
        public AchievementsWindow(string gameName, int gameId)
        {
            InitializeComponent();
            TitleText.Text = $"Succès — {gameName}";
            _ = LoadAsync(gameId);
        }

        private async Task LoadAsync(int gameId)
        {
            StatusText.Foreground = Brushes.White;
            StatusText.Text = "Chargement...";
            try
            {
                var catalogTask = ApiService.GetAchievementsCatalogAsync(gameId);
                string username = AppSettings.PlayerUsername;
                var playerTask = !string.IsNullOrEmpty(username) ? ApiService.GetPlayerAsync(username) : null;

                var catalog = await catalogTask;
                var unlocked = playerTask != null
                    ? (await playerTask).Achievements?.Where(a => a.GameId == gameId).ToList() ?? new List<Achievement>()
                    : new List<Achievement>();

                var unlockedByInternalId = unlocked
                    .GroupBy(a => a.InternalId)
                    .ToDictionary(g => g.Key, g => g.First());

                var items = catalog
                    .Select(c => new AchievementDisplay(c, unlockedByInternalId.TryGetValue(c.InternalId, out var u) ? u : null))
                    .OrderByDescending(d => d.UnlockedAt.HasValue)
                    .ThenByDescending(d => d.UnlockedAt)
                    .ThenBy(d => d.InternalId)
                    .ToList();

                AchievementsList.ItemsSource = items;

                int totalScore = unlocked.Sum(a => a.PointsValue);
                SummaryText.Text = $"{unlocked.Count} / {catalog.Count} débloqués · {totalScore} G";
                StatusText.Text = string.IsNullOrEmpty(username) ? "Connecte-toi pour voir tes succès débloqués." : "";
            }
            catch (Exception ex)
            {
                StatusText.Foreground = Brushes.OrangeRed;
                StatusText.Text = $"Erreur de chargement : {ex.Message}";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private class AchievementDisplay
        {
            public int InternalId { get; }
            public string IconUrl { get; }
            public string Name { get; }
            public string Description { get; }
            public string PointsText { get; }
            public string StatusText { get; }
            public Brush StatusColor { get; }
            public Brush TitleColor { get; }
            public Brush CardBackground { get; }
            public Brush CardBorder { get; }
            public double IconOpacity { get; }
            public DateTimeOffset? UnlockedAt { get; }

            public AchievementDisplay(AchievementCatalogEntry entry, Achievement unlocked)
            {
                InternalId  = entry.InternalId;
                IconUrl     = "https://csi-world.xyz" + entry.IconUrl;
                Name        = entry.Name;
                Description = entry.Description;
                PointsText  = $"{entry.PointsValue} G";
                UnlockedAt  = unlocked?.UnlockedAt;

                bool isUnlocked = unlocked != null;
                StatusText     = isUnlocked ? $"Débloqué le {unlocked.UnlockedAt:dd/MM/yyyy}" : "Verrouillé";
                StatusColor    = isUnlocked ? new SolidColorBrush(Color.FromRgb(0x7a, 0xd9, 0x7a)) : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
                TitleColor     = isUnlocked ? Brushes.White : new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
                CardBackground = isUnlocked ? new SolidColorBrush(Color.FromArgb(0x33, 0x4C, 0xAF, 0x50)) : new SolidColorBrush(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF));
                CardBorder     = isUnlocked ? new SolidColorBrush(Color.FromArgb(0x88, 0x4C, 0xAF, 0x50)) : Brushes.Transparent;
                IconOpacity    = isUnlocked ? 1.0 : 0.35;
            }
        }
    }
}
