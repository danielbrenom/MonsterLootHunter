using System.Linq;
using System.Text.RegularExpressions;

namespace MonsterLootHunter.Utils
{
    public static class LootIdentifierConstants
    {
        public const string Leather = "Leather";
        private static readonly string[] LeatherSuffixes = { "skin", "hide" };
        private static readonly string LeatherPattern = string.Join("|", LeatherSuffixes.Select(Regex.Escape));
        public static readonly Regex LeatherRegex = new(LeatherPattern, RegexOptions.IgnoreCase);

        public const string Reagents = "Reagents";
        private static readonly string[] ReagentSuffixes = { "secretion", "blood", "fat", "wing", "umbrella" };
        private static readonly string ReagentsPattern = string.Join("|", ReagentSuffixes.Select(Regex.Escape));
        public static readonly Regex ReagentsRegex = new(ReagentsPattern, RegexOptions.IgnoreCase);

        public const string Bone = "Bone";
        public const string Ingredients = "Ingredients";
    }
}