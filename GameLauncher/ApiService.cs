using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameLauncher
{
    static class ApiService
    {
        private static readonly HttpClient _http = new();
        private const string BaseUrl = "http://csi-world.xyz/api";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static async Task<T> GetAsync<T>(string url)
        {
            string json = await _http.GetStringAsync(url);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public static Task<List<Skill>>     GetSkillsAsync()     => GetAsync<List<Skill>>($"{BaseUrl}/skills");
        public static Task<Skill>           GetSkillAsync(int id) => GetAsync<Skill>($"{BaseUrl}/skills/{id}");

        public static Task<List<Item>>      GetItemsAsync()      => GetAsync<List<Item>>($"{BaseUrl}/items");
        public static Task<Item>            GetItemAsync(int id)  => GetAsync<Item>($"{BaseUrl}/items/{id}");

        public static Task<List<Weapon>>    GetWeaponsAsync()    => GetAsync<List<Weapon>>($"{BaseUrl}/weapons");
        public static Task<Weapon>          GetWeaponAsync(int id) => GetAsync<Weapon>($"{BaseUrl}/weapons/{id}");

        public static Task<List<Armor>>     GetArmorsAsync()     => GetAsync<List<Armor>>($"{BaseUrl}/armors");
        public static Task<Armor>           GetArmorAsync(int id) => GetAsync<Armor>($"{BaseUrl}/armors/{id}");

        public static Task<List<Character>> GetCharactersAsync() => GetAsync<List<Character>>($"{BaseUrl}/characters");
        public static Task<Character>       GetCharacterAsync(int id) => GetAsync<Character>($"{BaseUrl}/characters/{id}");

        public static Task<List<Enemy>>     GetEnemiesAsync()    => GetAsync<List<Enemy>>($"{BaseUrl}/enemies");
        public static Task<Enemy>           GetEnemyAsync(int id) => GetAsync<Enemy>($"{BaseUrl}/enemies/{id}");

        public static Task<List<Status>>    GetStatusesAsync()   => GetAsync<List<Status>>($"{BaseUrl}/statuses");
        public static Task<Status>          GetStatusAsync(int id) => GetAsync<Status>($"{BaseUrl}/statuses/{id}");

        public static Task<RandomIcon>      GetRandomIconAsync() => GetAsync<RandomIcon>($"{BaseUrl}/icons/random");
    }
}
