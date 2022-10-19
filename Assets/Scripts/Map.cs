using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct Map
{
    public ushort Width;
    public ushort Length;
    public TileType[,] TileData; //should be of size, width by length
    public float[,] HeightMap; //should be of size, (width + 1) by (length + 1)

    public float GetHeight(float x, float z)
    {
        // just interpolate in x and y and average.
        int x_i = Mathf.FloorToInt(x);
        int z_i = Mathf.FloorToInt(z);

        float x_r = x - x_i;
        float z_r = z - z_i;

        // QQ: make better, work out triange and use eqn of plane to find z.
        if (z_r + x_r < 1)
        {
            Vector3 n = Vector3.Cross(
                new Vector3(1, HeightMap[x_i + 1, z_i] - HeightMap[x_i, z_i], 0),
                new Vector3(0, HeightMap[x_i, z_i + 1] - HeightMap[x_i, z_i], 1)).normalized;
            Vector3 p = new Vector3(x_i, HeightMap[x_i, z_i], z_i);
            return (Vector3.Dot(p, n) - z * n.z - x * n.x) / n.y;
        }
        else
        {
            Vector3 n = Vector3.Cross(
                new Vector3(-1, HeightMap[x_i, z_i + 1] - HeightMap[x_i + 1, z_i + 1], 0 ),
                new Vector3( 0, HeightMap[x_i + 1, z_i] - HeightMap[x_i + 1, z_i + 1], -1)).normalized;
            Vector3 p = new Vector3(x_i + 1, HeightMap[x_i + 1, z_i + 1], z_i + 1);
            return (Vector3.Dot(p, n) - z * n.z - x * n.x) / n.y;
        }
    }

    public static Map RandomMap(ushort width, ushort length, float minRange, float maxRange)
    {
        var m = new Map();
        m.Width = width;
        m.Length = length;

        m.TileData = new TileType[width, length];
        m.HeightMap = new float[width + 1, length + 1];

        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= length; y++)
            {
                m.HeightMap[x, y] = Random.Range(minRange, maxRange);
            }
        }

        return m;
    }

    public static Map Brownian(ushort width, ushort length, float volatility)
    {
        var m = new Map();
        m.Width = width;
        m.Length = length;

        m.TileData = new TileType[width, length];
        m.HeightMap = new float[width + 1, length + 1];

        m.HeightMap[0, 0] = volatility * Random.Range(0f, 2.5f);

        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= length; y++)
            {
                float div = 0f;
                if (x > 0)
                {
                    m.HeightMap[x, y] +=
                        Mathf.Max(0, m.HeightMap[x - 1, y] +
                        Random.Range(-volatility, volatility));
                    div++;
                }
                if (y > 0)
                {
                    m.HeightMap[x, y] +=
                        Mathf.Max(0, m.HeightMap[x, y - 1] +
                        Random.Range(-volatility, volatility));
                    div++;
                }
                if (div > 0)
                {
                    m.HeightMap[x, y] /= div;
                }
            }
        }

        return m;
    }


    /// <summary>
    /// Generates a mesh from the height map.
    /// </summary>
    public Mesh HeightMapMesh()
    {
        Vector3[] vertices = new Vector3[(Width + 1) * (Length + 1)];
        for (int x = 0; x <= Width; x++)
        {
            for (int z = 0; z <= Length; z++)
            {
                vertices[x * (Length + 1) + z] = new Vector3(x, HeightMap[x, z], z);
            }
        }

        int[] triangles = new int[Width * Length * 3 * 2];
        for (int x = 0; x < Width; x++)
        {
            for (int z = 0; z < Length; z++)
            {
                //base (bottom left, vertex)
                triangles[6 * (x * Length + z)] = (Length + 1) * x + z;
                triangles[6 * (x * Length + z) + 1] = (Length + 1) * x + z + 1;
                triangles[6 * (x * Length + z) + 2] = (Length + 1) * (x + 1) + z;

                triangles[6 * (x * Length + z) + 3] = (Length + 1) * x + z + 1;
                triangles[6 * (x * Length + z) + 4] = (Length + 1) * (x + 1) + z + 1;
                triangles[6 * (x * Length + z) + 5] = (Length + 1) * (x + 1) + z;
            }
        }

        var m = new Mesh
        {
            vertices = vertices,
            triangles = triangles
        };
        m.RecalculateBounds();
        m.RecalculateNormals();
        m.RecalculateTangents();

        return m;

        // 4 by 4 example
        /* 20 21 22 23 24
         * 15 16 17 18 19
         * 10 11 12 13 14
         * 5  6  7  8  9
         * 0  1  2  3  4
         */
    }

    // Failure cases return null path.
    // Uses tile data to find shortest path to target, diagonal movement allowed.
    public List<Vector2> ShortestPathAStar(Vector2Int startPosition, Vector2Int endPosition)
    {
        // declare expansion arrays to make adding new nodes easier.
        int[] expansions_x = new int[] { 0, 0, 1, -1, 1, -1, 1, -1 };
        int[] expansions_y = new int[] { 1, -1, 0, 0, -1, 1, 1, -1 };
        float rt2 = Mathf.Sqrt(2);

        // feel like these should be indext or at least mapped to grid posns for efficency.
        List<Node> openList = new List<Node>();
        // processedPosns should always point to node with lowest f value in that position,
        // at that point in algorithm, so we can query current optimality.
        Dictionary<Vector2Int, Node> processedPosns = new Dictionary<Vector2Int, Node>();

        var startNode = new Node
        {
            Position = startPosition,
            F = 0,
            Parent = null
        };
        openList.Add(startNode);
        processedPosns[startPosition] = startNode;

        while (openList.Count > 0)
        {
            // select node with lowest F. (might be able to do more efficiently)
            Node Q = openList.OrderBy(n => n.F).First();
            openList.Remove(Q);

            Node[] successors = Enumerable.Range(0, 8)
                .Select(i => new Node
                {
                    Position = Q.Position + new Vector2Int(expansions_x[i], expansions_y[i]),
                    Parent = Q
                }).ToArray();
            for (int i = 0; i < 8; i++)
            {
                var s = successors[i]; //shorthand reference s.
                //is node position valid? if not continue. (chuck out)
                if (s.Position.x < 0 || s.Position.x >= Width ||
                    s.Position.y < 0 || s.Position.y >= Length ||
                    TileData[s.Position.x, s.Position.y] > 0)
                {
                    continue; // succesor is invalid (can't walk here).
                }
                if (s.Position == endPosition)
                {
                    // success! compute path and return..
                    var traceBackNode = s;
                    List<Vector2> outputPath = new List<Vector2> { endPosition };
                    while (traceBackNode.Parent != null)
                    {
                        traceBackNode = traceBackNode.Parent;
                        outputPath.Add(traceBackNode.Position);
                    }
                    outputPath.Reverse();
                    return outputPath;
                }
                // calc cost with heuristic.
                s.F = Q.F + i < 4 ? 1 : rt2 + (endPosition - s.Position).sqrMagnitude;
                if (processedPosns.ContainsKey(s.Position) &&
                    processedPosns[s.Position].F <= s.F)
                {
                    continue; // successor is sub optimal.
                }
                openList.Add(s);
                processedPosns[s.Position] = s;
            }
        }
        return null;
    }
}

// contans enum for types of tile, down the line this will match with
// content to be displayed and the data in MapData.Data 
public enum TileType
{
    Clear = 0, 
    Tree = 1
}

public class Node
{
    public Vector2Int Position;
    public Node Parent;
    public float F;
}