using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameLauncher
{
    static class ApiService
    {
        private static readonly HttpClient _http = new();
        private const string BaseUrl = "https://csi-world.xyz/api";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };

        private static async Task<T> GetAsync<T>(string url)
        {
            string json = await _http.GetStringAsync(url);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        private static async Task<TResult> PostAsync<TResult>(string url, object body)
        {
            var response = await _http.PostAsJsonAsync(url, body);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TResult>(json, _jsonOptions);
        }

        public static Task<List<Skill>>     GetSkillsAsync()          => GetAsync<List<Skill>>($"{BaseUrl}/skills");
        public static Task<Skill>           GetSkillAsync(int id)      => GetAsync<Skill>($"{BaseUrl}/skills/{id}");

        public static Task<List<Item>>      GetItemsAsync()            => GetAsync<List<Item>>($"{BaseUrl}/items");
        public static Task<Item>            GetItemAsync(int id)       => GetAsync<Item>($"{BaseUrl}/items/{id}");

        public static Task<List<Weapon>>    GetWeaponsAsync()          => GetAsync<List<Weapon>>($"{BaseUrl}/weapons");
        public static Task<Weapon>          GetWeaponAsync(int id)     => GetAsync<Weapon>($"{BaseUrl}/weapons/{id}");

        public static Task<List<Armor>>     GetArmorsAsync()           => GetAsync<List<Armor>>($"{BaseUrl}/armors");
        public static Task<Armor>           GetArmorAsync(int id)      => GetAsync<Armor>($"{BaseUrl}/armors/{id}");

        public static Task<List<Character>> GetCharactersAsync()       => GetAsync<List<Character>>($"{BaseUrl}/characters");
        public static Task<Character>       GetCharacterAsync(int id)  => GetAsync<Character>($"{BaseUrl}/characters/{id}");

        public static Task<List<Enemy>>     GetEnemiesAsync()          => GetAsync<List<Enemy>>($"{BaseUrl}/enemies");
        public static Task<Enemy>           GetEnemyAsync(int id)      => GetAsync<Enemy>($"{BaseUrl}/enemies/{id}");

        public static Task<List<Status>>    GetStatusesAsync()         => GetAsync<List<Status>>($"{BaseUrl}/statuses");
        public static Task<Status>          GetStatusAsync(int id)     => GetAsync<Status>($"{BaseUrl}/statuses/{id}");

        public static Task<List<Element>>      GetElementsAsync()          => GetAsync<List<Element>>($"{BaseUrl}/elements");

        public static Task<List<WeaponType>>    GetWeaponTypesAsync()       => GetAsync<List<WeaponType>>($"{BaseUrl}/weapon-types");
        public static Task<List<ArmorType>>     GetArmorTypesAsync()        => GetAsync<List<ArmorType>>($"{BaseUrl}/armor-types");
        public static Task<List<CharacterRole>> GetCharacterRolesAsync()    => GetAsync<List<CharacterRole>>($"{BaseUrl}/character-roles");

        public static Task<RandomIcon>      GetRandomIconAsync(int? gameId = null)
            => GetAsync<RandomIcon>(gameId.HasValue ? $"{BaseUrl}/icons/random?gameId={gameId}" : $"{BaseUrl}/icons/random");

        public static Task<PlayerResponse>  LoginAsync(string username, string password)
            => PostAsync<PlayerResponse>($"{BaseUrl}/player/login", new { username, password });

        public static Task<PlayerResponse>  RegisterAsync(string username, string password)
            => PostAsync<PlayerResponse>($"{BaseUrl}/player/register", new { username, password });

        public static Task<PlayerProfile>  GetPlayerAsync(string username)
            => GetAsync<PlayerProfile>($"{BaseUrl}/player/{username}");

        public static Task<List<AchievementCatalogEntry>> GetAchievementsCatalogAsync(int gameId)
            => GetAsync<List<AchievementCatalogEntry>>($"{BaseUrl}/achievements?gameId={gameId}");

        public static async Task<List<Badge>> GetMyBadgesAsync(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/player/me/badges");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Badge>>(json, _jsonOptions);
        }

        public static async Task<List<PlayerCharacter>> GetMyCharactersAsync(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/player/me/characters");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<PlayerCharacter>>(json, _jsonOptions);
        }

        public static async Task<CharacterDetail> GetMyCharacterDetailAsync(string token, int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/player/me/characters/{id}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CharacterDetail>(json, _jsonOptions);
        }

        public static async Task<InventoryResponse> GetMyInventoryAsync(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/player/me/inventory");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<InventoryResponse>(json, _jsonOptions);
        }

        public static async Task<NotificationsResponse> GetMyNotificationsAsync(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/player/me/notifications");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<NotificationsResponse>(json, _jsonOptions);
        }

        public static async Task MarkNotificationsReadAsync(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/player/me/notifications/read-all");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            await _http.SendAsync(request);
        }

        public static async Task<List<ConversationPreview>> GetMyMessagesAsync(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/player/me/messages");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ConversationPreview>>(json, _jsonOptions);
        }

        public static async Task<List<PlayerSearchResult>> SearchPlayersAsync(string token, string query)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/players/search?q={Uri.EscapeDataString(query)}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<PlayerSearchResult>>(json, _jsonOptions);
        }

        public static async Task<ConversationHistory> GetConversationAsync(string token, int otherId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/player/me/messages/{otherId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ConversationHistory>(json, _jsonOptions);
        }

        public static async Task SendMessageAsync(string token, int otherId, string content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/player/me/messages/{otherId}")
            {
                Content = JsonContent.Create(new { content })
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(await ExtractErrorAsync(response));
        }

        public static async Task<VaultResponse> GetMyVaultAsync(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/player/me/vault");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<VaultResponse>(json, _jsonOptions);
        }

        public static async Task<BankResponse> GetMyBankAsync(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/player/me/bank");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BankResponse>(json, _jsonOptions);
        }

        public static async Task<PlayerProfile> GetMyProfileAsync(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/player/me");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PlayerProfile>(json, _jsonOptions);
        }

        public static async Task<SubscriptionInfo> GetMySubscriptionAsync(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/player/me/subscription");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SubscriptionInfo>(json, _jsonOptions);
        }

        public static Task<UnlockAchievementResponse> UnlockAchievementAsync(string token, int gameId, int achievementId)
            => PostAsync<UnlockAchievementResponse>($"{BaseUrl}/achievements/unlock", new { token, gameId, achievementId });

        public static async Task<CloudSavesResponse> GetCloudSavesAsync(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/cloud-saves");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            string json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CloudSavesResponse>(json, _jsonOptions);
        }

        public static async Task UploadCloudSaveAsync(string token, string filePath, string gameSlug, string label)
        {
            using var content = new MultipartFormDataContent();
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
            content.Add(fileContent, "file", Path.GetFileName(filePath));
            content.Add(new StringContent(gameSlug), "gameSlug");
            if (!string.IsNullOrEmpty(label))
                content.Add(new StringContent(label), "label");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/cloud-saves/upload") { Content = content };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(await ExtractErrorAsync(response));
        }

        public static async Task<byte[]> DownloadCloudSaveAsync(string token, int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/cloud-saves/{id}/download");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(await ExtractErrorAsync(response));
            return await response.Content.ReadAsByteArrayAsync();
        }

        public static async Task DeleteCloudSaveAsync(string token, int id)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/cloud-saves/{id}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(await ExtractErrorAsync(response));
        }

        private static async Task<string> ExtractErrorAsync(HttpResponseMessage response)
        {
            try
            {
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var err))
                    return err.GetString();
            }
            catch { }
            return $"Erreur HTTP {(int)response.StatusCode}";
        }
    }
}
