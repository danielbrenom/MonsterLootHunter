using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using MonsterLootHunter.Data;

namespace MonsterLootHunter.Helpers
{
    public class ScrapperSanitizer : IServiceType
    {
        private LootData LootData { get; set; }

        public Task<LootData> ParseResponse(HtmlDocument document, string lootName)
        {
            return Task.Run(() =>
            {
                LootData = new LootData(lootName);
                var body = document.DocumentNode.QuerySelector("div#bodyContent");
                var bodyContent = body.QuerySelector("div.mw-content-ltr div.mw-parser-output");
                LootData.LootLocations.AddRange(GetDutyDrops(bodyContent));
                LootData.LootLocations.AddRange(GetMonsterDropsFromTable(bodyContent));
                LootData.LootPurchaseLocations.AddRange(GetVendorPurchases(bodyContent));
                return LootData;
            });
        }

        private static IEnumerable<LootDrops> GetDutyDrops(HtmlNode node)
        {
            try
            {
                var dutyHeader = node.QuerySelector("h3");
                if (dutyHeader is null || !dutyHeader.InnerText.Contains("dut", System.StringComparison.OrdinalIgnoreCase)) return Enumerable.Empty<LootDrops>();

                var dutyList = node.QuerySelector("ul")?.QuerySelectorAll("li");
                var dutyListSanitized = dutyList?.Select(el => el.InnerText).ToList();
                if (dutyListSanitized is null) return Enumerable.Empty<LootDrops>();
                
                return dutyListSanitized.Select(duty => new LootDrops
                {
                    MobName = "Duty",
                    MobLocation = Regex.Replace(duty, @"&#.+;", string.Empty),
                    MobFlag = "N/A"
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
    }
}