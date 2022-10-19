using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;

/// <summary>
/// Central control loop for game, player commands are issued to this, acts as
/// a receiver for player commands and distributes current game state to players.
/// </summary>
public class ServerController : MonoBehaviour
{
    public Map Map;

    public Dictionary<ushort, Unit> Units = new Dictionary<ushort, Unit>();

    public GameState GameState = new GameState
    {
        UnitStates = new Dictionary<ushort, UnitState>()
    };

    private ushort _nextId = 0;

    private void Start()
    {
        GenerateMap();
        // Scan initially for units in scene (should always be children of server)
        var childUnits = GetComponentsInChildren<Unit>();
        foreach (var unit in childUnits)
        {
            Units[_nextId] = unit;
            _nextId++;
        }
    }

    private void GenerateMap()
    {
        Map = Map.Brownian(50, 50, 0.5f);
        var mapGo = new GameObject("Map");
        mapGo.transform.SetParent(transform);
        var mr = gameObject.AddComponent<MeshRenderer>();
        var mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = Map.HeightMapMesh();
    }

    private void Update()
    {
        foreach (ushort unitId in Units.Keys)
        {
            GameState.UnitStates[unitId] = new UnitState
            {
                Position = Units[unitId].transform.position,
                Orientation = Units[unitId].transform.rotation.eulerAngles.y,
                Stance = Units[unitId].Stance
            };
        }
    }
}

/// <summary>
/// The game state that is transferred to player controllers
/// </summary>
public class GameState
{
    //TileType[,] MapState; May not need whole map state each cycle.

    // Data for all the units on the map.
    public Dictionary<ushort, UnitState> UnitStates = new Dictionary<ushort, UnitState>();

    // QQ: similar list for buildings on map (or consider them units).
}

// Used to position and animate unit on player side.
public class UnitState
{
    public Vector3 Position;
    public float Orientation;
    // qq: may be worth storing directional data or current target.
    public UnitStance Stance;
}