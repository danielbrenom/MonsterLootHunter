using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using MonsterLootHunter.Data;

namespace MonsterLootHunter.Helpers
{
    public static class ScrapperSanitizer
    {
        public static LootData ParseResponse(HtmlDocument document, string lootName)
        {
            var lootData = new LootData(lootName);
            var body = document.DocumentNode.QuerySelector("div#bodyContent");
            var bodyContent = body.QuerySelector("div.mw-content-ltr div.mw-parser-output");
            lootData.LootLocations = GetMonsterDrops(bodyContent);
            lootData.LootLocations.AddRange(GetMonsterDropsFromTable(bodyContent));
            lootData.LootPurchaseLocations = GetVendorPurchases(bodyContent);
            return lootData;
        }

        private static List<LootDrops> GetMonsterDrops(HtmlNode node)
        {
            try
            {
                var dropList = node.QuerySelector("ul").QuerySelectorAll("li");
                var dropListSanitized = dropList.Select(el => el.InnerText).ToList();
                return (from drop in dropListSanitized
                        select drop.Split("-")
                        into monsterNameAndLocation
                        let flag = monsterNameAndLocation[1].Split("(")
                        select new LootDrops
                        {
                            MobName = monsterNameAndLocation[0].TrimEnd().TrimStart(),
                            MobLocation = flag[0].TrimEnd().TrimStart(),
                            MobFlag = flag.Length > 1 ? $"({flag[1].Split(")")[0]})" : "N/A",
                        })
                   .ToList();
            }
            catch (System.Exception e)
            {
                PluginLog.Error($"Error mouting drop from list {e.Message}, {e.StackTrace}");
                return new List<LootDrops>();
            }
        }

        private static IEnumerable<LootDrops> GetMonsterDropsFromTable(HtmlNode node)
        {
            try
            {
                var dropList = node.QuerySelector("table.item tbody").QuerySelectorAll("tr").ToList();
                dropList.RemoveAt(0);
                return (from vendorNode in dropList
                    select vendorNode.QuerySelectorAll("td").ToList()
                    into lootInformation
                    let locationAndFlag = lootInformation.Last().InnerText.Split("(")
                    let flag = locationAndFlag.Length > 1 ? $"({locationAndFlag[1]}" : string.Empty
                    select new LootDrops { MobName = lootInformation[0].InnerText.Replace("\n", ""), MobLocation = locationAndFlag[0].Replace("\n", "").TrimEnd(), MobFlag = flag.Replace("\n", "") }).ToList();
            }
            catch (System.Exception e)
            {
                PluginLog.Error($"Error mouting drop from table {e.Message}, {e.StackTrace}");
                return new List<LootDrops>();
            }
        }

        private static List<LootPurchase> GetVendorPurchases(HtmlNode node)
        {
            try
            {
                var purchaseList = node.QuerySelector("table.npc tbody").QuerySelectorAll("tr").ToList();
                purchaseList.RemoveAt(0);
                return (from vendorNode in purchaseList
                    select vendorNode.QuerySelectorAll("td").ToList()
                    into purchaseInformation
                    let locationAndFlag = purchaseInformation[1].InnerText.Split("(")
                    select new LootPurchase
                    {
                        Vendor = purchaseInformation[0].InnerText.Replace("\n", ""),
                        Location = locationAndFlag[0].Replace("\n", "").TrimEnd(),
                        FlagPosition = $"({locationAndFlag[1]}".Replace("\n", ""),
                        Cost = purchaseInformation[2].InnerText.Replace("&#160;", "").Replace("\n", ""),
                        CostType = purchaseInformation[2].QuerySelector("span a").Attributes["title"].Value
                    }).ToList();
            }
            catch (System.Exception)
            {
                return new List<LootPurchase>();
            }
        }
    }
}