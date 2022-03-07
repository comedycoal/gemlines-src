using UnityEngine;

public static class Utility
{
    public static Vector2Int Direction(int x, int y)
    {
        if (x == 0) return new Vector2Int(0, 1);
        if (y == 0) return new Vector2Int(1, 0);
        if (x == y) return new Vector2Int(1, 1);
        if (x + y == 0) return new Vector2Int(1, -1);
        return Vector2Int.zero;
    }
    public static Vector2Int Direction(Vector2Int vect)
    {
        return Direction(vect.x, vect.y);
    }
}
