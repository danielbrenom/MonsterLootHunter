namespace MonsterLootHunter.Utils;

public static class StringExtensions
{
    public static string Replaces(this string target, params string[] replacements)
    {
        if (replacements.Length % 2 != 0)
            throw new ArgumentException("Invalid number of replacements, must be even");

        for (var i = 0; i < replacements.Length; i += 2)
            target = target.Replace(replacements[i], replacements[i + 1]);

        return target;
    }
}
