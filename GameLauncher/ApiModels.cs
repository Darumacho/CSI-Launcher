using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameLauncher
{
    // --- Shared ---

    class Stats
    {
        public int PvMax { get; set; }
        public int EgMax { get; set; }
        public int Attaque { get; set; }
        public int Defense { get; set; }
        public int Arcane { get; set; }
        public int Sagesse { get; set; }
        public int Vitesse { get; set; }
        public int Finesse { get; set; }
    }

    class Multipliers
    {
        public double? PvMax { get; set; }
        public double? EgMax { get; set; }
        public double? Attaque { get; set; }
        public double? Defense { get; set; }
        public double? Arcane { get; set; }
        public double? Sagesse { get; set; }
        public double? Vitesse { get; set; }
        public double? Finesse { get; set; }
    }

    class ElementalResistance
    {
        [JsonPropertyName("element_id")]
        public int ElementId { get; set; }
        public double Multiplier { get; set; }
    }

    class LearnSpell
    {
        public int Level { get; set; }
        public int SkillId { get; set; }
    }

    class Drop
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public double Probability { get; set; }
    }

    class StatusEffect
    {
        [JsonPropertyName("status_id")]
        public int StatusId { get; set; }
        public int Probability { get; set; }
    }

    class StatEffect
    {
        [JsonPropertyName("stat_id")]
        public int StatId { get; set; }
        public int Value { get; set; }
        public int Turns { get; set; }
    }

    // --- Skills ---

    class Skill
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int GameId { get; set; }
        public int SkillType { get; set; }
        public int UtilityCategory { get; set; }
        public int Availability { get; set; }
        public int? ElementId { get; set; }
        public int EgCost { get; set; }
        public int PowerCost { get; set; }
        public int PowerGain { get; set; }
        public int DamageCategory { get; set; }
        public int DamageType { get; set; }
        public int TargetType { get; set; }
        public int Hits { get; set; }
        public int Accuracy { get; set; }
        public int Priority { get; set; }
        public string Formula { get; set; }
        public int Variance { get; set; }
        public bool HasCritical { get; set; }
        public int PercentagePvHealed { get; set; }
        public int PercentageEgHealed { get; set; }
        public int PercentageTpHealed { get; set; }
        public bool IsEnemySkill { get; set; }
        public string SpecialEffect { get; set; }
        public List<StatEffect> StatsBuff { get; set; }
        public List<StatEffect> StatsDebuff { get; set; }
        public List<StatusEffect> StatusInflicted { get; set; }
        public List<StatusEffect> StatusHealed { get; set; }
    }

    // --- Items ---

    class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int GameId { get; set; }
        public int Icon { get; set; }
        public int Value { get; set; }
        public string Type { get; set; }
        public int ItemType { get; set; }
        public bool IsKeyItem { get; set; }
        public bool IsMaterial { get; set; }
        public int Priority { get; set; }
        public int? FlatDamage { get; set; }
        public int? FlatHeal { get; set; }
        public string SpecialEffect { get; set; }
        public List<int> StatusImmunity { get; set; }
        public List<int> GrantsSkills { get; set; }
        public Stats Stats { get; set; }
    }

    // --- Weapons ---

    class Weapon
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int GameId { get; set; }
        public int Icon { get; set; }
        public int Value { get; set; }
        public int Rarity { get; set; }
        public int? WeaponTypeId { get; set; }
        public string WeaponTypeName { get; set; }
        public bool IsTwoHanded { get; set; }
        public string Formula { get; set; }
        public int? BonusHit { get; set; }
        public int? ElementId { get; set; }
        public List<int> GrantsSkills { get; set; }
        public List<StatusEffect> StatusInflicted { get; set; }
        public bool NoSkills { get; set; }
        public int? CriticalRate { get; set; }
        public Stats Stats { get; set; }
        public Multipliers Multipliers { get; set; }
    }

    // --- Armors ---

    class Armor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int GameId { get; set; }
        public int Icon { get; set; }
        public int Value { get; set; }
        public int Rarity { get; set; }
        public int? ArmorTypeId { get; set; }
        public string ArmorTypeName { get; set; }
        public int Slot { get; set; }
        public List<int> GrantsSkills { get; set; }
        public List<ElementalResistance> ElementalResistance { get; set; }
        public int? CriticalRate { get; set; }
        public Stats Stats { get; set; }
        public Multipliers Multipliers { get; set; }
    }

    // --- Characters ---

    class Character
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public int GameId { get; set; }
        public string Appearance { get; set; }
        public string Height { get; set; }
        public string Weight { get; set; }
        public string Biography { get; set; }
        public List<int> UsableWeapons { get; set; }
        public List<int> UsableArmors { get; set; }
        public List<LearnSpell> LearnSpells { get; set; }
        public Stats AttributeValues { get; set; }
        public List<int> ElementalTypes { get; set; }
        public List<ElementalResistance> ElementalResistance { get; set; }
        public double? PhysicalTaken { get; set; }
        public double? MagicalTaken { get; set; }
        public Stats Stats { get; set; }
    }

    // --- Enemies ---

    class Enemy
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int GameId { get; set; }
        public string Appearance { get; set; }
        public bool IsBoss { get; set; }
        public bool IsSuperboss { get; set; }
        public List<LearnSpell> LearnSpells { get; set; }
        public List<Drop> Drops { get; set; }
        public List<ElementalResistance> ElementalResistance { get; set; }
        public double? PhysicalTaken { get; set; }
        public double? MagicalTaken { get; set; }
        public List<int> StatusImmunity { get; set; }
        public Stats Stats { get; set; }
    }

    // --- Elements ---

    class Element
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Csi1Exclusive { get; set; }
        public bool Csi2Exclusive { get; set; }
        public bool CsiRogueExclusive { get; set; }
    }

    // --- Icons ---

    class RandomIcon
    {
        public string Category { get; set; }
        public string Name { get; set; }
        public int GameId { get; set; }
        public int Icon { get; set; }
    }

    // --- Statuses ---

    class Status
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int GameId { get; set; }
        public int Icon { get; set; }
        public int RestrictionType { get; set; }
        public int? MinTurns { get; set; }
        public int? MaxTurns { get; set; }
        public int? Footsteps { get; set; }
        public bool EndsAfterBattle { get; set; }
        public bool EndsAfterTurn { get; set; }
        public bool EndsAfterAction { get; set; }
        public string SpecialEffect { get; set; }
        public Multipliers Multipliers { get; set; }
        public List<ElementalResistance> ElementalResistance { get; set; }
        public List<int> StatusImmunity { get; set; }
    }

    // --- Player ---

    class WeaponType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Csi1Exclusive { get; set; }
        public bool Csi2Exclusive { get; set; }
        public bool CsiRogueExclusive { get; set; }
    }

    class ArmorType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Csi1Exclusive { get; set; }
        public bool Csi2Exclusive { get; set; }
        public bool CsiRogueExclusive { get; set; }
    }

    class CharacterRole
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Icon { get; set; }
    }

    class UnlockAchievementResponse
    {
        public bool Success { get; set; }
        public bool AlreadyUnlocked { get; set; }
    }

    class Badge
    {
        public string Slug { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public DateTimeOffset EarnedAt { get; set; }
    }

    class PlayerResponse
    {
        public string Username { get; set; }
        public string Token { get; set; }
    }

    class Achievement
    {
        public int GameId { get; set; }
        public int InternalId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int PointsValue { get; set; }
        public DateTimeOffset UnlockedAt { get; set; }
    }

    class AchievementCatalogEntry
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int InternalId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int PointsValue { get; set; }
        public string IconUrl { get; set; }
    }

    class PlayerProfile
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Description { get; set; }
        public string AvatarUrl { get; set; }
        public string Email { get; set; }
        public int? Money { get; set; }
        public int? Eloges { get; set; }
        public int? PremiumMoney { get; set; }
        public List<Achievement> Achievements { get; set; }
    }

    class SubscriptionInfo
    {
        public bool Active { get; set; }
        public string Status { get; set; }
        public string Tier { get; set; }
        public string Color { get; set; }
    }

    class CloudSave
    {
        public int Id { get; set; }
        public string GameSlug { get; set; }
        public string GameLabel { get; set; }
        public string FileName { get; set; }
        public string Label { get; set; }
        public long SizeBytes { get; set; }
        public string Sha256 { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
    }

    class CloudSavesResponse
    {
        public string Tier { get; set; }
        public int Quota { get; set; }
        public int Used { get; set; }
        public Dictionary<string, string> KnownGames { get; set; }
        public List<CloudSave> Saves { get; set; }
    }

    class CharacterModelRef
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Filename { get; set; }
        public string Gender { get; set; }
    }

    class PlayerCharacter
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TeamSlot { get; set; }
        public string ClassName { get; set; }
        public int ClassLevel { get; set; }
        public int Experience { get; set; }
        public int CurrentPv { get; set; }
        public int PvMax { get; set; }
        public int CurrentEg { get; set; }
        public int EgMax { get; set; }
        public CharacterModelRef Model { get; set; }
    }

    class EquipmentEntry
    {
        public string Slot { get; set; }
        public string Type { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public int IconId { get; set; }
    }

    // Le serveur renvoie `equipment` comme un tableau JSON quand tous les slots
    // sont occupés, mais comme un objet (clés numériques en string) dès qu'un
    // slot est vide (PHP : array associatif non-séquentiel -> objet JSON).
    class EquipmentListConverter : JsonConverter<List<EquipmentEntry>>
    {
        public override List<EquipmentEntry> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            var result = new List<EquipmentEntry>();

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                    result.Add(element.Deserialize<EquipmentEntry>(options));
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                    result.Add(property.Value.Deserialize<EquipmentEntry>(options));
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, List<EquipmentEntry> value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value, options);
    }

    class CharacterDetail
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string ClassName { get; set; }
        public int ClassLevel { get; set; }
        public int Experience { get; set; }
        public int CurrentPv { get; set; }
        public int PvMax { get; set; }
        public int CurrentEg { get; set; }
        public int EgMax { get; set; }
        public CharacterModelRef Model { get; set; }
        public Stats Stats { get; set; }
        [JsonConverter(typeof(EquipmentListConverter))]
        public List<EquipmentEntry> Equipment { get; set; }
    }

    class InventoryItem
    {
        public string Type { get; set; }
        public int Id { get; set; }
        public int Qty { get; set; }
        public string Name { get; set; }
        public string IconUrl { get; set; }
        public int? Slot { get; set; }
    }

    class InventoryResponse
    {
        public int InventoryLimit { get; set; }
        public int UsedSlots { get; set; }
        public List<InventoryItem> Items { get; set; }
    }

    class NotificationEntry
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    class NotificationsResponse
    {
        public int Unread { get; set; }
        public List<NotificationEntry> Notifications { get; set; }
    }

    class ConversationPreview
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string Tier { get; set; }
        public string AvatarUrl { get; set; }
        public int LastId { get; set; }
    }

    class PlayerSearchResult
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
    }

    class ChatMessage
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public bool IsMine { get; set; }
        public string Content { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    class ConversationHistory
    {
        public int OtherId { get; set; }
        public string OtherName { get; set; }
        public List<ChatMessage> Messages { get; set; }
    }

    class SendMessageResponse
    {
        public int Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    class VaultResponse
    {
        public int VaultLimit { get; set; }
        public int UsedSlots { get; set; }
        public List<InventoryItem> Items { get; set; }
    }

    class BankResponse
    {
        public int Money { get; set; }
        public int BankBalance { get; set; }
        public bool CanDeposit { get; set; }
        public int WithdrawalTaxPct { get; set; }
        public int BaseInterestPct { get; set; }
        public int BonusInterestPct { get; set; }
        public int TotalInterestPct { get; set; }
        public string NextInterestDate { get; set; }
        public DateTimeOffset? LastInterestAt { get; set; }
    }
}
