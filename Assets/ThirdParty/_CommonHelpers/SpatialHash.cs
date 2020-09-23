using System.Collections.Generic;
using UnityEngine;

public class SpatialHash3<T>
{
    public readonly float CellSize = 1;

    private Dictionary<Vector3Int, List<(Vector3, T)>> hash = new Dictionary<Vector3Int, List<(Vector3, T)>>();

    public SpatialHash3(float cellSize)
    {
        CellSize = cellSize;
    }

    public void Add(Vector3 v, T elem)
    {
        var intV = Vector3Int.FloorToInt(v / CellSize);
        List<(Vector3, T)> item = null;
        if (!hash.TryGetValue(intV, out item))
        {
            item = hash[intV] = new List<(Vector3, T)>();
        }

        item.Add((v, elem));
    }

    public IEnumerable<(Vector3, T)> GetNeighbors(Vector3 v, float radius)
    {
        var intV = Vector3Int.RoundToInt(v / CellSize);
        var max = Vector3Int.FloorToInt((v + Vector3.one * radius) / CellSize);
        var min = Vector3Int.FloorToInt((v - Vector3.one * radius) / CellSize);
        for (int x = min.x; x <= max.x; x++)
        for (int y = min.y; y <= max.y; y++)
        for (int z = min.z; z <= max.z; z++)
        {
            var vv = new Vector3Int(x, y, z);
            List<(Vector3, T)> items = null;
            if (hash.TryGetValue(vv, out items))
            {
                if (items != null)
                    foreach (var elem in items)
                        yield return elem;
            }
        }
    }
}

public class SpatialHash2<T>
{
    public readonly float CellSize = 1;

    private Dictionary<Vector2Int, List<(Vector2, T)>> hash = new Dictionary<Vector2Int, List<(Vector2, T)>>();

    public SpatialHash2(float cellSize)
    {
        CellSize = cellSize;
    }

    public void Add(Vector2 v, T elem)
    {
        var intV = Vector2Int.FloorToInt(v / CellSize);
        List<(Vector2, T)> item = null;
        if (!hash.TryGetValue(intV, out item))
        {
            item = hash[intV] = new List<(Vector2, T)>();
        }

        item.Add((v, elem));
    }

    public IEnumerable<(Vector2, T)> GetNeighbors(Vector2 v, float radius)
    {
        var intV = Vector2Int.RoundToInt(v / CellSize);
        var max = Vector2Int.FloorToInt((v + Vector2.one * radius) / CellSize);
        var min = Vector2Int.FloorToInt((v - Vector2.one * radius) / CellSize);
        for (int x = min.x; x <= max.x; x++)
        for (int y = min.y; y <= max.y; y++)
        {
            var vv = new Vector2Int(x, y);
            List<(Vector2, T)> items = null;
            if (hash.TryGetValue(vv, out items))
            {
                if (items != null)
                    foreach (var elem in items)
                        yield return elem;
            }
        }
    }
}

public class SpatialHash1<T>
{
    public readonly float CellSize = 1;

    private Dictionary<int, List<(float, T)>> hash = new Dictionary<int, List<(float, T)>>();

    public SpatialHash1(float cellSize)
    {
        CellSize = cellSize;
    }

    public void Add(float v, T elem)
    {
        var intV = Mathf.FloorToInt(v / CellSize);
        List<(float, T)> item = null;
        if (!hash.TryGetValue(intV, out item))
        {
            item = hash[intV] = new List<(float, T)>();
        }

        item.Add((v, elem));
    }

    public IEnumerable<(float, T)> GetNeighbors(float v, float radius)
    {
        var intV = Mathf.RoundToInt(v / CellSize);
        var max = Mathf.FloorToInt((v + radius) / CellSize);
        var min = Mathf.FloorToInt((v - radius) / CellSize);
        for (int x = min; x <= max; x++)
        {
            List<(float, T)> items = null;
            if (hash.TryGetValue(x, out items))
            {
                if (items != null)
                    foreach (var elem in items)
                        yield return elem;
            }
        }
    }
}
