using System.Linq;
using System.Text.RegularExpressions;

namespace MonsterLootHunter.Utils
{
    public static class LootIdentifierConstants
    {
        private const uint Ingredients = 44;
        private const uint Stone = 47;
        private const uint Metal = 48;
        private const uint Cloth = 50;
        private const uint Leather = 51;
        private const uint Bone = 52;
        private const uint Reagents = 53;
        public static readonly uint[] CategoryIds = { Ingredients, Stone, Metal, Cloth, Leather, Bone, Reagents };

        private static readonly string[] ExclusionSuffixes = { "approved", "grade", "enchanted" };
        private static readonly string ExclusionPattern = string.Join("|", ExclusionSuffixes.Select(Regex.Escape));
        public static readonly Regex ExclusionRegex = new(ExclusionPattern, RegexOptions.IgnoreCase);
    }
}