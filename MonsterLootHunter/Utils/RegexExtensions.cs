using System.Text.RegularExpressions;

namespace MonsterLootHunter.Utils;

public static class RegexExtensions
{
    public static bool TryMatches(this Regex regex, string input, out MatchCollection matches)
    {
        matches = default!;

        try
        {
            if (regex.Matches(input) is not { } temp || temp.Count == 0)
                return false;

            matches = temp;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
