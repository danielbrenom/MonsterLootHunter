using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using MonsterLootHunter.Data;

namespace MonsterLootHunter.Helpers
{
    public partial class ScrapperSanitizer
    {
        private LootData LootData { get; set; }

        [GeneratedRegex("(\\d+\\.?\\d*)")]
        private static partial Regex CoordinatesRegex();

        public Task<LootData> ParseResponse(HtmlDocument document, string lootName)
        {
            LootData = new LootData(lootName);
            var body = document.DocumentNode.QuerySelector("div#bodyContent");
            var bodyContent = body.QuerySelector("div.mw-content-ltr div.mw-parser-output");
            LootData.LootLocations.AddRange(GetDutyDrops(bodyContent));
            LootData.LootLocations.AddRange(GetMonsterDropsFromTable(bodyContent));
            LootData.LootLocations.AddRange(GetPossibleRecipe(bodyContent));
            LootData.LootLocations.AddRange(GetPossibleTreasureHunts(bodyContent));
            LootData.LootLocations.AddRange(GetPossibleDesynthesis(bodyContent));
            LootData.LootLocations.AddRange(GetPossibleGathering(bodyContent));
            LootData.LootLocations.AddRange(GetPossibleGatheringTable(bodyContent));
            LootData.LootPurchaseLocations.AddRange(GetVendorPurchases(bodyContent));
            return Task.FromResult(LootData);
        }

        private static IEnumerable<LootDrops> GetDutyDrops(HtmlNode node)
        {
            try
            {
                var dutyHeader = node.QuerySelector("h3").QuerySelector("span#Duties");
                if (dutyHeader is null || !dutyHeader.InnerText.Contains("dut", System.StringComparison.OrdinalIgnoreCase)) return Enumerable.Empty<LootDrops>();

                var dutyList = node.QuerySelector("ul")?.QuerySelectorAll("li");
                var dutyListSanitized = dutyList?.Select(el => el.InnerText).ToList();
                if (dutyListSanitized is null) return Enumerable.Empty<LootDrops>();

                return dutyListSanitized.Select(duty => new LootDrops
                {
                    MobName = "Duty",
                    MobLocation = Regex.Replace(duty, @"&#.+;", string.Empty),
                    MobFlag = string.Empty
                });
            }
            catch (System.Exception)
            {
                return Enumerable.Empty<LootDrops>();
            }
        }

        private static IEnumerable<LootDrops> GetMonsterDropsFromTable(HtmlNode node)
        {
            try
            {
                var dropList = node.QuerySelector("table.item tbody").QuerySelectorAll("tr").ToList();
                if (!dropList.Any()) return Enumerable.Empty<LootDrops>();

                dropList.RemoveAt(0);
                return dropList.Select(vendorNode => vendorNode.QuerySelectorAll("td").ToList())
                               .Select(lootInformation => new { lootInformation, locationAndFlag = lootInformation.Last().InnerText.Split("(") })
                               .Select(extractedInfo => new { info = extractedInfo, flag = extractedInfo.locationAndFlag.Length > 1 ? $"({extractedInfo.locationAndFlag[1]}" : string.Empty })
                               .Select(t => new LootDrops
                                {
                                    MobName = t.info.lootInformation[0].InnerText.Replace("\n", ""),
                                    MobLocation = t.info.locationAndFlag[0].Replace("\n", "").TrimEnd(),
                                    MobFlag = t.flag.Replace("\n", "")
                                });
            }
            catch (System.Exception)
            {
                return Enumerable.Empty<LootDrops>();
            }
        }

        private static IEnumerable<LootPurchase> GetVendorPurchases(HtmlNode node)
        {
            try
            {
                var purchaseHeader = node.QuerySelectorAll("h3").ToList();
                var purchaseTopHeader = node.QuerySelectorAll("h2").ToList();
                if (!purchaseHeader.Any(n => n.QuerySelector("span#Purchase") != null || n.QuerySelector("span#Purchased") != null || n.QuerySelector("span#Purchased_From") != null) ||
                    purchaseTopHeader.All(n => n.QuerySelector("span#Acquisition") == null))
                    return Enumerable.Empty<LootPurchase>();
                var purchaseList = node.QuerySelector("table.npc tbody")?.QuerySelectorAll("tr").ToList();
                if (purchaseList is null || !purchaseList.Any()) return Enumerable.Empty<LootPurchase>();

                purchaseList.RemoveAt(0);
                return purchaseList.Select(vendorNode => vendorNode.QuerySelectorAll("td").ToList())
                                   .Select(purchaseInformation => new { purchaseInformation, locationAndFlag = purchaseInformation[1].InnerText.Split("(") })
                                   .Select(t => new LootPurchase
                                    {
                                        Vendor = t.purchaseInformation[0].InnerText.Replace("\n", ""),
                                        Location = t.locationAndFlag[0].Replace("\n", "").TrimEnd(),
                                        FlagPosition = $"({t.locationAndFlag[1]}".Replace("\n", ""),
                                        Cost = t.purchaseInformation[2].InnerText.Replace("&#160;", "").Replace("\n", ""),
                                        CostType = t.purchaseInformation[2].QuerySelector("span a").Attributes["title"].Value
                                    });
            }
            catch (System.Exception)
            {
                return Enumerable.Empty<LootPurchase>();
            }
        }

        private static IEnumerable<LootDrops> GetPossibleRecipe(HtmlNode node)
        {
            try
            {
                var recipeBox = node.QuerySelector("div.recipe-box");
                if (recipeBox is null) return Enumerable.Empty<LootDrops>();

                var recipeData = recipeBox.QuerySelector("div.wrapper").QuerySelectorAll("dd").ToList();
                return new[]
                {
                    new LootDrops
                    {
                        MobName = $"Crafter Class: {recipeData[2].QuerySelectorAll("a").ToList()[1].InnerText}",
                        MobLocation = $"Recipe Lvl: {recipeData[3].InnerText}",
                        MobFlag = string.Empty
                    }
                };
            }
            catch (System.Exception)
            {
                return Enumerable.Empty<LootDrops>();
            }
        }

        private static IEnumerable<LootDrops> GetPossibleTreasureHunts(HtmlNode node)
        {
            try
            {
                var pageHeaders = node.QuerySelectorAll("h3").ToList();
                if (pageHeaders.All(hNode => hNode.QuerySelector("span#Treasure_Hunt") is null)) return Enumerable.Empty<LootDrops>();

                var treasureHeader = pageHeaders.First(hNode => hNode.QuerySelector("span#Treasure_Hunt") is not null);
                var treasureMapList = treasureHeader.NextSibling.NextSibling.QuerySelectorAll("li");

                return treasureMapList.Select(treasureMap => new LootDrops
                {
                    MobName = "Treasure Map",
                    MobLocation = treasureMap.QuerySelectorAll("a").ToList().Last().InnerText, MobFlag = string.Empty
                });
            }
            catch (System.Exception)
            {
                return Enumerable.Empty<LootDrops>();
            }
        }

        private static IEnumerable<LootDrops> GetPossibleDesynthesis(HtmlNode node)
        {
            try
            {
                var pageHeaders = node.QuerySelectorAll("h3").ToList();
                if (pageHeaders.All(hNode => hNode.QuerySelector("span#Desynthesis") is null &&
                                             hNode.QuerySelector("span#_Desynthesis") is null)) return Enumerable.Empty<LootDrops>();

                var desynthesisHeader = pageHeaders.First(hNode => hNode.QuerySelector("span#Desynthesis") is not null ||
                                                                   hNode.QuerySelector("span#_Desynthesis") is not null);
                var desynthesisList = desynthesisHeader.NextSibling.NextSibling.QuerySelectorAll("li");
                return desynthesisList.Select(treasureMap => new LootDrops
                {
                    MobName = "Desynthesis",
                    MobLocation = treasureMap.QuerySelectorAll("a").ToList().Last().InnerText,
                    MobFlag = string.Empty
                });
            }
            catch (System.Exception)
            {
                return Enumerable.Empty<LootDrops>();
            }
        }

        private static IEnumerable<LootDrops> GetPossibleGathering(HtmlNode node)
        {
            try
            {
                var pageHeaders = node.QuerySelectorAll("h3").ToList();
                if (pageHeaders.All(hNode => hNode.QuerySelector("span#Gathering") is null &&
                                             hNode.QuerySelector("span#Gathered") is null)) return Enumerable.Empty<LootDrops>();

                var gatherHeader = pageHeaders.First(hNode => hNode.QuerySelector("span#Gathering") is not null ||
                                                              hNode.QuerySelector("span#Gathered") is not null);
                var gatherList = gatherHeader.NextSibling.NextSibling.QuerySelectorAll("li").ToList();

                if (gatherList.Any())
                {
                    if (!gatherList.First().InnerText.Contains("Reduction")) return Gathering(gatherList);
                    gatherList.RemoveAt(0);
                    return AetherialReduction(gatherList);
                }

                var gatherableInfo = gatherHeader.NextSibling.NextSibling;
                return gatherableInfo is not null ? Gathered(gatherableInfo) : Enumerable.Empty<LootDrops>();

                IEnumerable<LootDrops> Gathered(HtmlNode gatheredNode)
                {
                    var anchors = gatheredNode.QuerySelectorAll("a").ToList();
                    var flag = anchors.LastOrDefault()?.NextSibling.InnerText ?? string.Empty;
                    var flagParsed = CoordinatesRegex().Matches(flag);
                    var gatherTime = gatheredNode.ChildNodes.LastOrDefault()?.InnerText.Replace("\n", string.Empty);
                    return new[]
                    {
                        new LootDrops
                        {
                            MobName = $"{gatheredNode.ChildNodes.First().InnerText[..^2]} {anchors.First().InnerText}",
                            MobLocation = $"{anchors[1].InnerText}-{anchors.Last().InnerText}-{gatherTime}",
                            MobFlag = $"({flagParsed[0].Value},{flagParsed[1].Value})"
                        }
                    };
                }

                IEnumerable<LootDrops> Gathering(IEnumerable<HtmlNode> gatheringList) =>
                    from gatherNode in gatheringList
                    let anchors = gatherNode.QuerySelectorAll("a").ToList()
                    let flag = anchors.LastOrDefault()?.NextSibling.InnerText ?? string.Empty
                    let flagParsed = Regex.Matches(flag, @"(\d+\.?\d*)")
                    select new LootDrops
                    {
                        MobName = $"{gatherNode.ChildNodes.First().InnerText}{anchors.First().InnerText}",
                        MobLocation = $"{anchors[1].InnerText}-{anchors.Last().InnerText}",
                        MobFlag = $"({flagParsed[0].Value},{flagParsed[1].Value})"
                    };

                IEnumerable<LootDrops> AetherialReduction(IEnumerable<HtmlNode> reductionList) =>
                    reductionList.Select(htmlNode => htmlNode.QuerySelectorAll("a").Last())
                                 .Select(itemName => new LootDrops { MobName = itemName.InnerText, MobLocation = "Aetherial Reduction", MobFlag = string.Empty });
            }
            catch (System.Exception)
            {
                return Enumerable.Empty<LootDrops>();
            }
        }

        private static IEnumerable<LootDrops> GetPossibleGatheringTable(HtmlNode node)
        {
            try
            {
                var gatheringTable = node.QuerySelector("table.gathering-role");
                if (gatheringTable is null) return Enumerable.Empty<LootDrops>();


                var gatheringList = gatheringTable.QuerySelector("tbody").QuerySelectorAll("tr").ToList();
                gatheringList.RemoveAt(0);

                return from gatherNode in gatheringList
                    select gatherNode.QuerySelectorAll("td").ToList()
                    into columns
                    let flagNumbers = CoordinatesRegex().Matches(columns.Last().InnerText)
                    select new LootDrops
                    {
                        MobName = columns[0].ChildNodes[1].InnerText,
                        MobLocation = columns[1].QuerySelector("a").InnerText,
                        MobFlag = $"({flagNumbers[0].Value},{flagNumbers[1].Value})"
                    };
            }
            catch (System.Exception)
            {
                return Enumerable.Empty<LootDrops>();
            }
        }
    }
}