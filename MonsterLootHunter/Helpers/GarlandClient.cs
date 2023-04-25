using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MonsterLootHunter.Data;
using Newtonsoft.Json;

namespace MonsterLootHunter.Helpers;

public class GarlandClient : IDisposable
{
    private static HttpClient _client;
    private const string BaseAddress = "https://www.garlandtools.org/";
    private const string ItemPath = "db/doc/item/en/3/{0}.json";

    public GarlandClient()
    {
        _client = new();
        _client.BaseAddress = new Uri(BaseAddress);
    }

    public async Task<string> GetItemName(uint itemId, CancellationToken cancellation)
    {
        var searchPath = string.Format(ItemPath, itemId);
        using var response = await _client.GetAsync(searchPath, cancellation);
        var content = await response.Content.ReadAsStringAsync(cancellation);
        var data = JsonConvert.DeserializeObject<GarlandResponse>(content);
        return data.Item.Name;
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}