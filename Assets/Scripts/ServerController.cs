using UnityEngine;
using System.Collections;

/// <summary>
/// Central control loop for game, player commands are issued to this, acts as
/// a receiver for player commands and distributes current game state to players.
/// </summary>
public class ServerController : MonoBehaviour
{
    public Map Map;

    private void Start()
    {
        GenerateMap();
    }


    private void GenerateMap()
    {
        Map = Map.Brownian(50, 50, 0.25f);
        var mapGo = new GameObject("Map");
        mapGo.transform.SetParent(transform);
        var mr = gameObject.AddComponent<MeshRenderer>();
        var mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = Map.HeightMapMesh();
    }

}
