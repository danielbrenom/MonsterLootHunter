using Newtonsoft.Json;

namespace MonsterLootHunter.Data;

public class GarlandResponse
{
    [JsonProperty("item")]
    public GarlandItem Item { get; set; }
}
public class GarlandItem
{
    [JsonProperty("id")] public int Id { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
}