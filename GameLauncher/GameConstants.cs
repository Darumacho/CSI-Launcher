using System.Collections.Generic;

namespace GameLauncher
{
    static class GameConstants
    {
        public static readonly IReadOnlyDictionary<int, string> GameNames = new Dictionary<int, string>
        {
            { 0, "CSI Forever" },
            { 1, "CSII Forever" },
            { 2, "CSI Rogue" },
            { 3, "Narval Souls" },
        };

        public static readonly IReadOnlyDictionary<int, string> CurrencyNames = new Dictionary<int, string>
        {
            { 0, "Dollawrs" },
            { 1, "Dollawrs" },
            { 2, "Dollawrs" },
            { 3, "Orbes" },
        };

        private static readonly IReadOnlyDictionary<int, (string Label, byte R, byte G, byte B)> RarityCSI =
            new Dictionary<int, (string, byte, byte, byte)>
            {
                { 1,  ("Commun",         255, 255, 255) },
                { 2,  ("Peu commun",      12, 201,  12) },
                { 3,  ("Rare",            81, 168, 245) },
                { 4,  ("Remarquable",    197,  84, 235) },
                { 5,  ("Légendaire",     240,   5,   5) },
                { 6,  ("Unique",         255, 138,  20) },
                { 7,  ("Héroïque",       255, 255, 128) },
                { 8,  ("Séraphin",       254, 161, 255) },
                { 9,  ("Nacré",            0, 225, 255) },
                { 10, ("Surnaturel",     189, 135, 255) },
                { 11, ("Fabuleux",        18, 255, 148) },
                { 12, ("Immaculé",       235,  52, 128) },
            };

        private static readonly IReadOnlyDictionary<int, (string Label, byte R, byte G, byte B)> RarityNarval =
            new Dictionary<int, (string, byte, byte, byte)>
            {
                { 1,  ("Commun",         255, 255, 255) },
                { 2,  ("Peu commun",      12, 201,  12) },
                { 3,  ("Rare",            81, 168, 245) },
                { 4,  ("Remarquable",    197,  84, 235) },
                { 5,  ("Légendaire",     255, 150,   0) },
                { 6,  ("Spécial",         255,   0, 102) },
                { 7,  ("Hérétique",      185,   0,   0) },
                { 8,  ("Séraphin",       255, 105, 180) },
                { 9,  ("Séraphin",255, 105, 180) },
                { 10, ("Animique",    18, 255, 148) },
                { 11, ("Consacré",         0, 255, 255) },
            };

        public static (string Label, byte R, byte G, byte B) GetRarity(int rarity, int gameId)
        {
            var table = gameId == 3 ? RarityNarval : RarityCSI;
            return table.TryGetValue(rarity, out var info) ? info : (rarity.ToString(), (byte)255, (byte)255, (byte)255);
        }

        // Source de vérité : Subscriptions.md (SubscriptionPlan::TIER_COLORS côté serveur)
        public static readonly IReadOnlyDictionary<string, (byte R, byte G, byte B)> SubscriptionTierColors =
            new Dictionary<string, (byte, byte, byte)>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "bronze",  (0xcd, 0x7f, 0x32) },
                { "argent",  (0xc0, 0xc0, 0xc0) },
                { "gold",    (0xf5, 0xc5, 0x42) },
                { "platine", (0x63, 0x54, 0xd6) },
            };

        // Dégradé indigo → violet du texte pour le tier Platine (haut, milieu, bas)
        public static readonly (byte R, byte G, byte B)[] PlatineGradient =
        {
            (0x8f, 0xa3, 0xff),
            (0x63, 0x54, 0xd6),
            (0x4b, 0x3a, 0xa8),
        };

        public static readonly IReadOnlyDictionary<string, string> SubscriptionTierBadges =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "bronze",  "/images/CSIWO/premium_tier_bronze.png" },
                { "argent",  "/images/CSIWO/premium_tier_argent.png" },
                { "gold",    "/images/CSIWO/premium_tier_gold.png" },
                { "platine", "/images/CSIWO/premium_tier_platine.png" },
            };

        public const string CSIForeverDescription =
            "Incarnez les Chosen Ones et parcourez le monde afin de faire le plein de butin et accessoirement empêcher l'effrondrement du monde !";

        public const string CSIIForeverDescription =
            "Rassemblez les meilleures guidounes de l'univers afin de former la plus grande troupe de mercenaires que cette terre ait connu !";

        public const string CSIRogueDescription =
            "Percez les mystères de la labyrinthique Impasse Protéenne !\nChaque partie est unique : survivez, ramassez, recommencez.";

        public const string NarvalDescription =
            "Voyagez à travers le royaume de Narvalie pour rétablir l'ordre et repousser la peste démoniaque.";
    }
}
