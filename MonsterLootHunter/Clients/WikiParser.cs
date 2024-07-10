using System.Text.RegularExpressions;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using MonsterLootHunter.Data;
using MonsterLootHunter.Utils;

namespace MonsterLootHunter.Clients
{
    public partial class WikiParser
    {
        [GeneratedRegex(@"(\d+\.?\d*)", RegexOptions.Compiled)]
        private static partial Regex CoordinatesRegex();

        [GeneratedRegex("\\d+", RegexOptions.Compiled)]
        private static partial Regex LevelRegex();

        [GeneratedRegex(@"(\d{1,2}:\d{1,2}\s(?:am|pm))", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex GatherTimeRegex();

        [GeneratedRegex("(?:patch)|(?:tree)|(?:logging)|(?:quarry)|(?:harves)|(?:mining)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex LocationNameRegex();

        [GeneratedRegex(@"(?:-\s+)(.+)(?:\s+\()", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex LocationNameAlternativeRegex();

        public static Task<LootData> ParseResponse(HtmlDocument document, LootData lootData)
        {
            var body = document.DocumentNode.QuerySelector("div#bodyContent");
            var bodyContent = body.QuerySelector("div.mw-content-ltr div.mw-parser-output");
            lootData.LootLocations.AddRange(GetDutyDrops(bodyContent));
            lootData.LootLocations.AddRange(GetMonsterDropsFromTable(bodyContent));
            lootData.LootLocations.AddRange(GetPossibleRecipe(bodyContent));
            lootData.LootLocations.AddRange(GetPossibleTreasureHunts(bodyContent));
            lootData.LootLocations.AddRange(GetPossibleDesynthesis(bodyContent));
            lootData.LootLocations.AddRange(GetPossibleGathering(bodyContent));
            lootData.LootLocations.AddRange(GetPossibleGatheringTable(bodyContent));
            lootData.LootPurchaseLocations.AddRange(GetVendorPurchases(bodyContent));
            return Task.FromResult(lootData);
        }

        private static IEnumerable<LootDrops> GetDutyDrops(HtmlNode node)
        {
            var dutyHeader = node.QuerySelector("h3").QuerySelector("span#Duties");
            if (dutyHeader is null || !dutyHeader.InnerText.Contains("dut", StringComparison.OrdinalIgnoreCase))
                return [];

            var dutyList = node.QuerySelector("ul")?.QuerySelectorAll("li");
            var dutyListSanitized = dutyList?.Select(el => el.InnerText).ToList();
            if (dutyListSanitized is null)
                return [];

            return dutyListSanitized.Select(duty => new LootDrops
            {
                MobName = "Duty",
                MobLocation = Regex.Replace(duty, @"&#.+;", string.Empty),
                MobFlag = string.Empty,
                MobLevel = string.Empty
            });
        }

        private static IEnumerable<LootDrops> GetMonsterDropsFromTable(HtmlNode node)
        {
            var dropList = node.QuerySelector("table.item tbody")?.QuerySelectorAll("tr").ToList();
            if (dropList is null || dropList.Count == 0)
                return [];

            dropList.RemoveAt(0);

            return dropList.Select(drop => drop.QuerySelectorAll("td").ToList())
                           .Select(info => new { info, flagParsed = CoordinatesRegex().Matches(info.TryGet(nodes => nodes.Last().InnerText)) })
                           .Select(data => new LootDrops
                            {
                                MobName = data.info.TryGet(nodes => nodes[0].InnerText).Replace("\n", ""),
                                MobLocation = data.info.TryGet(nodes => nodes.Last().InnerText).Split("(")[0].Replace("\n", "").TrimEnd(),
                                MobFlag = data.flagParsed.Count == 2 ? $"({data.flagParsed[0]},{data.flagParsed[1]})" : string.Empty,
                                MobLevel = data.info.TryGet(nodes => nodes[1].InnerText).Replace("\n", ""),
                            });
        }

        private static IEnumerable<LootPurchase> GetVendorPurchases(HtmlNode node)
        {
            var purchaseHeader = node.QuerySelectorAll("h3").ToList();
            var purchaseTopHeader = node.QuerySelectorAll("h2").ToList();
            if (!purchaseHeader.Any(n => n.QuerySelector("span#Purchase") != null || n.QuerySelector("span#Purchased") != null || n.QuerySelector("span#Purchased_From") != null) ||
                purchaseTopHeader.All(n => n.QuerySelector("span#Acquisition") == null))
                return [];

            var purchaseList = node.QuerySelector("table.npc tbody")?.QuerySelectorAll("tr").ToList();
            if (purchaseList is null || purchaseList.Count == 0)
                return [];

            purchaseList.RemoveAt(0);
            return purchaseList.Select(vendorNode => vendorNode.QuerySelectorAll("td").ToList())
                               .Select(purchaseInformation => new { purchaseInformation, locationAndFlag = purchaseInformation.TryGet(nodes => nodes[1].InnerText).Split("(") })
                               .Select(t => new LootPurchase
                                {
                                    Vendor = t.purchaseInformation.TryGet(nodes => nodes[0].InnerText).Replace("\n", ""),
                                    Location = t.locationAndFlag[0].Replace("\n", "").TrimEnd(),
                                    FlagPosition = $"({t.locationAndFlag[1]}".Replace("\n", ""),
                                    Cost = t.purchaseInformation.TryGet(nodes => nodes[2].InnerText).Replace("&#160;", "").Replace("\n", ""),
                                    CostType = t.purchaseInformation.TryGet(nodes => nodes[2].QuerySelector("span a").Attributes["title"].Value)
                                });
        }

        private static IEnumerable<LootDrops> GetPossibleRecipe(HtmlNode node)
        {
            var recipeBox = node.QuerySelector("div.recipe-box");
            if (recipeBox is null)
                return [];

            var recipeData = recipeBox.QuerySelector("div.wrapper").QuerySelectorAll("dd").ToList();
            return new[]
            {
                new LootDrops
                {
                    MobName = $"Crafter Class: {recipeData.TryGet(nodes => nodes[2].QuerySelectorAll("a").ToList()[1].InnerText)}",
                    MobLocation = string.Empty,
                    MobFlag = string.Empty,
                    MobLevel = recipeData.TryGet(nodes => nodes[3].InnerText),
                }
            };
        }

        private static IEnumerable<LootDrops> GetPossibleTreasureHunts(HtmlNode node)
        {
            var pageHeaders = node.QuerySelectorAll("h3").ToList();
            if (pageHeaders.All(hNode => hNode.QuerySelector("span#Treasure_Hunt") is null))
                return [];

            var treasureHeader = pageHeaders.First(hNode => hNode.QuerySelector("span#Treasure_Hunt") is not null);
            var treasureMapList = treasureHeader.NextSibling.NextSibling.QuerySelectorAll("li");

            return treasureMapList.Select(treasureMap => new LootDrops
            {
                MobName = "Treasure Map",
                MobLocation = treasureMap.QuerySelectorAll("a").ToList().Last().InnerText,
                MobFlag = string.Empty,
                MobLevel = string.Empty,
            });
        }

        private static IEnumerable<LootDrops> GetPossibleDesynthesis(HtmlNode node)
        {
            var pageHeaders = node.QuerySelectorAll("h3").ToList();
            if (pageHeaders.All(hNode => hNode.QuerySelector("span#Desynthesis") is null &&
                                         hNode.QuerySelector("span#_Desynthesis") is null))
                return [];

            var desynthesisHeader = pageHeaders.First(hNode => hNode.QuerySelector("span#Desynthesis") is not null ||
                                                               hNode.QuerySelector("span#_Desynthesis") is not null);
            var desynthesisList = desynthesisHeader.NextSibling.NextSibling.QuerySelectorAll("li");
            return desynthesisList.Select(treasureMap => new LootDrops
            {
                MobName = "Desynthesis",
                MobLocation = treasureMap.QuerySelectorAll("a").ToList().Last().InnerText,
                MobFlag = string.Empty,
                MobLevel = string.Empty,
            });
        }

        private static IEnumerable<LootDrops> GetPossibleGathering(HtmlNode node)
        {
            var pageHeaders = node.QuerySelectorAll("h3").ToList();
            if (pageHeaders.All(hNode => hNode.QuerySelector("span#Gathering") is null &&
                                         hNode.QuerySelector("span#Gathered") is null))
                return [];

            var gatherHeader = pageHeaders.First(hNode => hNode.QuerySelector("span#Gathering") is not null ||
                                                          hNode.QuerySelector("span#Gathered") is not null);
            var gatherList = gatherHeader.NextSibling.NextSibling.QuerySelectorAll("li").ToList();

            if (gatherList.Count != 0)
            {
                if (!gatherList.First().InnerText.Contains("Reduction"))
                    return Gathering(gatherList);

                gatherList.RemoveAt(0);
                return AetherialReduction(gatherList);
            }

            var gatherableInfo = gatherHeader.NextSibling.NextSibling;
            return gatherableInfo is not null && gatherableInfo.Name != "table" ? Gathered(gatherableInfo) : [];

            IEnumerable<LootDrops> Gathered(HtmlNode gatheredNode)
            {
                var anchors = gatheredNode.QuerySelectorAll("a").ToList();
                var flag = anchors.LastOrDefault()?.NextSibling.InnerText ?? string.Empty;
                var flagParsed = CoordinatesRegex().Matches(flag);
                var gatherTime = GatherTimeRegex().Matches(node.InnerText ?? string.Empty).FirstOrDefault()?.Value ?? string.Empty;
                var locationName = anchors.Count > 1 ? anchors.First(text => !LocationNameRegex().Match(text.InnerText).Success)?.InnerText : string.Empty;
                return new[]
                {
                    new LootDrops
                    {
                        MobName = anchors.First().InnerText,
                        MobLocation = $"{locationName}-{anchors.Last().InnerText}-{gatherTime}",
                        MobFlag = flagParsed.Count == 2 ? $"({flagParsed[0].Value},{flagParsed[1].Value})" : string.Empty,
                        MobLevel = LevelRegex().Matches(gatheredNode.ChildNodes.First().InnerText).FirstOrDefault()?.Value ?? string.Empty,
                    }
                };
            }

            IEnumerable<LootDrops> Gathering(IEnumerable<HtmlNode> gatheringList) =>
                from gatherNode in gatheringList
                let anchors = gatherNode.QuerySelectorAll("a").ToList()
                let flag = anchors.LastOrDefault()?.NextSibling.InnerText ?? string.Empty
                let flagParsed = CoordinatesRegex().Matches(flag)
                let locationName = anchors.FirstOrDefault(text => !LocationNameRegex().Match(text.InnerText).Success)?.InnerText
                let locationAlternativeName = locationName is null ? LocationNameAlternativeRegex().Match(anchors.First().NextSibling.InnerText).Groups[1].Value : string.Empty
                select new LootDrops
                {
                    MobName = anchors.First().InnerText,
                    MobLocation = $"{locationName ?? locationAlternativeName}-{anchors.Last().InnerText}",
                    MobFlag = flagParsed.Count == 2 ? $"({flagParsed[0].Value},{flagParsed[1].Value})" : string.Empty,
                    MobLevel = LevelRegex().Matches(gatherNode.ChildNodes.First().InnerText).FirstOrDefault()?.Value ?? string.Empty,
                };

            IEnumerable<LootDrops> AetherialReduction(IEnumerable<HtmlNode> reductionList) =>
                reductionList.Select(htmlNode => htmlNode.QuerySelectorAll("a").Last())
                             .Select(itemName => new LootDrops { MobName = itemName.InnerText, MobLocation = "Aetherial Reduction", MobFlag = string.Empty, MobLevel = string.Empty });
        }

        private static IEnumerable<LootDrops> GetPossibleGatheringTable(HtmlNode node)
        {
            var gatheringTable = node.QuerySelector("table.gathering-role");
            if (gatheringTable is null)
                return [];

            var gatheringList = gatheringTable.QuerySelector("tbody").QuerySelectorAll("tr").ToList();
            gatheringList.RemoveAt(0);

            return from gatherNode in gatheringList
                   select gatherNode.QuerySelectorAll("td").ToList()
                   into columns
                   let flagParsed = CoordinatesRegex().Matches(columns.Last().InnerText)
                   select new LootDrops
                   {
                       MobName = columns.TryGet(nodes => nodes[0].ChildNodes[1].InnerText),
                       MobLocation = columns.TryGet(nodes => nodes[1].QuerySelector("a").InnerText),
                       MobFlag = flagParsed.Count == 2 ? $"({flagParsed[0].Value},{flagParsed[1].Value})" : string.Empty,
                       MobLevel = columns.TryGet(nodes => nodes[2].ChildNodes[0].InnerText),
                   };
        }
    }
}
