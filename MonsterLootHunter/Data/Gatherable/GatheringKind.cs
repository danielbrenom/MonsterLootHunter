namespace MonsterLootHunter.Data.Gatherable;

public enum GatheringKind : byte
{
    Mining = 0,
    Quarrying = 1,
    Logging = 2,
    Harvesting = 3,
    Spearfishing = 4,
    Botanist = 5,
    Miner = 6,
    Fisher = 7,
    Multiple = 8,
    Unknown = byte.MaxValue,
};
