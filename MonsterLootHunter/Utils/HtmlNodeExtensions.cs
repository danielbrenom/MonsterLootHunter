using HtmlAgilityPack;

namespace MonsterLootHunter.Utils;

public static class HtmlNodeExtensions
{
    public static string TryGet(this IList<HtmlNode> nodes, Func<IList<HtmlNode>, string> selector)
    {
        var nodeText = string.Empty;

        try
        {
            nodeText = selector.Invoke(nodes);
        }
        catch (Exception)
        {
            //Suppress error and return empty
        }

        return nodeText;
    }
}