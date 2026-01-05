using System.Text.RegularExpressions;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using MonsterLootHunter.Data;
using MonsterLootHunter.Utils;
using System.Collections.Concurrent;

namespace MonsterLootHunter.Clients
{
    public partial class WikiParser
    {
        private delegate IEnumerable<LootDrops> Processors(HtmlNode node);

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

        [GeneratedRegex(@"&#.+;")]
        private static partial Regex UnicodeCharacterRemovalRegex();

        public static Task<LootData> ParseResponse(HtmlDocument document, LootData lootData)
        {
            var body = document.DocumentNode;
            var bodyContent = body.QuerySelector("div.mw-content-ltr div.mw-parser-output") ??
                              body.QuerySelector("div.mw-content-ltr.mw-parser-output");
            var concurrentList = new ConcurrentBag<LootDrops>();
            Parallel.Invoke(() => { AddToBag(GetDutyDrops, bodyContent); },
                            () => { AddToBag(GetMonsterDropsFromTable, bodyContent); },
                            () => { AddToBag(GetPossibleRecipe, bodyContent); },
                            () => { AddToBag(GetPossibleTreasureHunts, bodyContent); },
                            () => { AddToBag(GetPossibleDesynthesis, bodyContent); },
                            () => { AddToBag(GetPossibleGathering, bodyContent); },
                            () => { AddToBag(GetPossibleGatheringTable, bodyContent); },
                            () => { lootData.LootPurchaseLocations.AddRange(GetVendorPurchases(bodyContent)); });

            lootData.LootLocations.AddRange(concurrentList.AsEnumerable());
            return Task.FromResult(lootData);

            void AddToBag(Processors processor, HtmlNode node)
            {
                foreach (var gather in processor(node)) concurrentList.Add(gather);
            }
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

            var dutyDrops = new ConcurrentBag<LootDrops>();
            Parallel.ForEach(dutyListSanitized, duty =>
            {
                dutyDrops.Add(new LootDrops
                {
                    MobName = "Duty",
                    MobLocation = UnicodeCharacterRemovalRegex().Replace(duty, string.Empty),
                    MobFlag = string.Empty,
                    MobLevel = string.Empty
                });
            });

            return dutyDrops.AsEnumerable();
        }

        private static IEnumerable<LootDrops> GetMonsterDropsFromTable(HtmlNode node)
        {
            var dropList = node.QuerySelector("table.item tbody")?.QuerySelectorAll("tr").ToList();
            if (dropList is null || dropList.Count == 0)
                return [];

            dropList.RemoveAt(0);

            var dutyDrops = new ConcurrentBag<LootDrops>();
            Parallel.ForEach(dropList, drops =>
            {
                var info = drops.QuerySelectorAll("td").ToList();
                var flagWasParsed = CoordinatesRegex().TryMatches(info.TryGet(nodes => nodes.Last().InnerText), out var flagParsed);

                dutyDrops.Add(new LootDrops
                {
                    MobName = info.TryGet(nodes => nodes[0].InnerText).Replace("\n", ""),
                    MobLocation = info.TryGet(nodes => nodes.Last().InnerText).Split("(")[0].Replace("\n", "").TrimEnd(),
                    MobFlag = flagWasParsed ? $"({flagParsed[0]},{flagParsed[1]})" : string.Empty,
                    MobLevel = info.TryGet(nodes => nodes[1].InnerText).Replace("\n", ""),
                });
            });

            return dutyDrops.AsEnumerable();
        }

        private static IEnumerable<LootPurchase> GetVendorPurchases(HtmlNode node)
        {
            var purchaseHeader = node.QuerySelectorAll("h3").ToList();
            var purchaseTopHeader = node.QuerySelectorAll("h2").ToList();
            if (!CheckContainsVendorInfo())
                return [];

            var purchaseList = node.QuerySelector("table.npc tbody")?.QuerySelectorAll("tr").ToList();
            if (purchaseList is null || purchaseList.Count == 0)
                return [];

            purchaseList.RemoveAt(0);

            var vendors = new ConcurrentBag<LootPurchase>();

            Parallel.ForEach(purchaseList, vendorNode =>
            {
                var vendor = vendorNode.QuerySelectorAll("td").ToList();
                var locationAndFlag = vendor.TryGet(nodes => nodes[1].InnerText).Split("(");
                var costType = vendor.TryGet(nodes => nodes[3].QuerySelector("span a").Attributes["title"].Value).Replace("\n", "").TrimEnd();
                costType = Regex.Replace(costType, @"\&.+\;", " ");

                vendors.Add(new LootPurchase
                {
                    Vendor = vendor.TryGet(nodes => nodes[0].InnerText).Replace("\n", ""),
                    Location = locationAndFlag[0].Replace("\n", "").TrimEnd(),
                    FlagPosition = $"({locationAndFlag[1]}".Replace("\n", ""),
                    Cost = vendor.TryGet(nodes => nodes[3].InnerText).Replaces("&#160;", string.Empty, "\n", string.Empty),
                    CostType = costType
                });
            });

            return vendors.AsEnumerable();

            bool CheckContainsVendorInfo() =>
                purchaseHeader.Any(n => n.InnerText.Contains("Purchase", StringComparison.InvariantCultureIgnoreCase)) ||
                purchaseTopHeader.Any(n => n.InnerText.Contains("Acquisition")) ||
                !purchaseHeader.Any(n => n.QuerySelector("span#Purchase") != null || n.QuerySelector("span#Purchased") != null || n.QuerySelector("span#Purchased_From") != null) ||
                purchaseTopHeader.All(n => n.QuerySelector("span#Acquisition") == null);
        }

        private static LootDrops[] GetPossibleRecipe(HtmlNode node)
        {
            var recipeBox = node.QuerySelector("div.recipe-box");
            if (recipeBox is null)
                return [];

            var recipeData = recipeBox.QuerySelector("div.wrapper").QuerySelectorAll("dd").ToList();
            return
            [
                new LootDrops
                {
                    MobName = $"Crafter Class: {recipeData.TryGet(nodes => nodes[2].QuerySelectorAll("a").ToList()[1].InnerText)}",
                    MobLocation = string.Empty,
                    MobFlag = string.Empty,
                    MobLevel = recipeData.TryGet(nodes => nodes[3].InnerText),
                }
            ];
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
                                         hNode.QuerySelector("span#_Desynthesis") is null &&
                                         !hNode.InnerText.Contains("Desynthesis", StringComparison.InvariantCultureIgnoreCase)))
                return [];

            var desynthesisList = node.QuerySelector("ul").QuerySelectorAll("li");
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
            return gatherableInfo.Name != "table" ? Gathered(gatherableInfo) : [];

            IEnumerable<LootDrops> Gathered(HtmlNode gatheredNode)
            {
                var anchors = gatheredNode.QuerySelectorAll("a").ToList();
                var flag = anchors.LastOrDefault()?.NextSibling.InnerText ?? string.Empty;
                var flagParsed = CoordinatesRegex().Matches(flag);
                var gatherTime = GatherTimeRegex().Matches(node.InnerText).FirstOrDefault()?.Value ?? string.Empty;
                var locationName = anchors.Count > 1 ? anchors.First(text => !LocationNameRegex().IsMatch(text.InnerText))?.InnerText : string.Empty;
                return
                [
                    new LootDrops
                    {
                        MobName = anchors.First().InnerText,
                        MobLocation = $"{locationName}-{anchors.Last().InnerText}-{gatherTime}",
                        MobFlag = flagParsed.Count == 2 ? $"({flagParsed[0].Value},{flagParsed[1].Value})" : string.Empty,
                        MobLevel = LevelRegex().Matches(gatheredNode.ChildNodes.First().InnerText).FirstOrDefault()?.Value ?? string.Empty,
                    }
                ];
            }

            IEnumerable<LootDrops> Gathering(IEnumerable<HtmlNode> gatheringList) =>
                from gatherNode in gatheringList
                let anchors = gatherNode.QuerySelectorAll("a").ToList()
                let flag = anchors.LastOrDefault()?.NextSibling.InnerText ?? string.Empty
                let flagParsed = CoordinatesRegex().Matches(flag)
                let locationName = anchors.FirstOrDefault(text => !LocationNameRegex().IsMatch(text.InnerText))?.InnerText
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
                       MobLocation =
                           $"{columns.TryGet(nodes => nodes[1].QuerySelectorAll("a").ToList()[0].InnerText)} - {columns.TryGet(nodes => nodes[1].QuerySelectorAll("a").ToList()[1].InnerText)}",
                       MobFlag = flagParsed.Count == 2 ? $"({flagParsed[0].Value},{flagParsed[1].Value})" : string.Empty,
                       MobLevel = columns.TryGet(nodes => nodes[2].ChildNodes[0].InnerText).Replace("\n", ""),
                   };
        }
    }
}
