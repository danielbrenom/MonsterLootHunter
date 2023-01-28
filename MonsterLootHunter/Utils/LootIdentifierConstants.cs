using System.Linq;
using System.Text.RegularExpressions;

namespace MonsterLootHunter.Utils
{
    public static class LootIdentifierConstants
    {
        public const string Leather = "Leather";
        public const string Reagents = "Reagents";
        public const string Bone = "Bone";
        public const string Ingredients = "Ingredients";
        public const string Cloth = "Cloth";

        private static readonly string[] ExclusionSuffixes = { "approved", "grade", "enchanted" };
        private static readonly string ExclusionPattern = string.Join("|", ExclusionSuffixes.Select(Regex.Escape));
        public static readonly Regex ExclusionRegex = new(ExclusionPattern, RegexOptions.IgnoreCase);
    }
}