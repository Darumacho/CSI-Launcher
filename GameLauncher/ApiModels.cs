using System.Collections.Generic;
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
        public string Icon { get; set; }
        public int Value { get; set; }
        public string Type { get; set; }
        public int ItemType { get; set; }
        public bool IsKeyItem { get; set; }
        public bool IsMaterial { get; set; }
        public int Priority { get; set; }
        public int? FlatDamage { get; set; }
        public int? FlatHeal { get; set; }
        public string SpecialEffect { get; set; }
        public List<StatusEffect> StatusImmunity { get; set; }
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
        public string Icon { get; set; }
        public int Value { get; set; }
        public int Rarity { get; set; }
        public int WeaponTypeId { get; set; }
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
        public string Icon { get; set; }
        public int Value { get; set; }
        public int Rarity { get; set; }
        public int ArmorTypeId { get; set; }
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
        public List<StatusEffect> StatusImmunity { get; set; }
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
        public string Icon { get; set; }
    }

    // --- Statuses ---

    class Status
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int GameId { get; set; }
        public string Icon { get; set; }
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
        public List<StatusEffect> StatusImmunity { get; set; }
    }
}
