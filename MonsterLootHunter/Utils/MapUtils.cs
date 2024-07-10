namespace MonsterLootHunter.Utils;

public static class MapUtils
{
    public static int NodeToMap(double coord, double scale)
        => (int)(2 * coord + 2048 / scale + 100.9);

    public static int IntegerToInternal(int coord, double scale)
        => (int)(coord - 100 - 2048 / scale) / 2;
}
