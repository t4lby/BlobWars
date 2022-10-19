using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Allows the player to interact with their own units, select and commands.
/// Supports the sending of those commands to the ServerController.
/// </summary>
[RequireComponent(typeof(MouseDragSelection))]
public class PlayerController : MonoBehaviour
{

    // Dev target selection of unit and indication of that selection.
    // CLICK SELECTION (via game camera)
    // DRAG SELECTION.
    private MouseDragSelection _mouseDragSelection;

    public List<ushort> SelectedUnits;

    /// <summary>
    /// Display version of units, colliders should be disabled for these units.
    /// (do this on spawn).
    /// </summary>
    public Dictionary<ushort, Unit> DisplayUnits = new Dictionary<ushort, Unit>();

    // At the moment we are using server.gamestate and server.map, this should be
    // retrieved cyclicly in future from server via socket?
    private ServerController _server;

    // Use this for initialization
    void Awake()
    {
        _mouseDragSelection = GetComponent<MouseDragSelection>();
        _mouseDragSelection.OnSelectionEnd += OnMouseDragSelect;
        _server = FindObjectOfType<ServerController>();
    }

    private void Start()
    {
        CloneServerMap();
    }

    // Update is called once per frame
    void Update()
    {
        // Pull unit information from server. (temp offline is just server.gamestate)
        var gameState = _server.GameState;
        foreach (ushort unitID in gameState.UnitStates.Keys)
        {
            var unitState = gameState.UnitStates[unitID];
            if (!DisplayUnits.ContainsKey(unitID))
            {
                // instantiate unit.
                var unit =
                    Instantiate(GameData.Main.UnitPrefabs[(int)unitState.UnitType])
                    .GetComponent<Unit>();
                unit.transform.SetParent(transform);
                unit.IsServerUnit = false;
                unit.GetComponent<Collider>().enabled = false;
                DisplayUnits[unitID] = unit;
            }
            // Update unit position, rotation, hp etc.
            var dispUnit = DisplayUnits[unitID];
            dispUnit.transform.position = unitState.Position;
            dispUnit.transform.rotation = Quaternion.Euler(0, unitState.Orientation, 0);
            dispUnit.Stance = unitState.Stance;
        }

        // QQ: check for units to be deleted from display units.


        // Update selection graphics for player.


    }

    private void OnMouseDragSelect(Rect rect)
    {
        SelectedUnits.Clear();
        // Need to calculate bounding.

        // should have location of all units from server.

        // filter via longtitude and latitude out of viewbox.
        var camTransform = Camera.main.transform.transform;
        // calculate a min and max vertdot and horzdot relative from rect

        var minRay = Camera.main.ScreenPointToRay(rect.min);
        var maxRay = Camera.main.ScreenPointToRay(rect.max);
        //Debug.DrawLine(camTransform.position, camTransform.position + minRay.direction * 50, Color.blue, 30f);
        //Debug.DrawLine(camTransform.position, camTransform.position + maxRay.direction * 50, Color.red, 30f);

        var minVerDot = Vector3.Dot(minRay.direction, camTransform.up);
        var maxVerDot = Vector3.Dot(maxRay.direction, camTransform.up)
;
        var minHorDot = Vector3.Dot(minRay.direction, camTransform.right);
        var maxHorDot = Vector3.Dot(maxRay.direction, camTransform.right);

        foreach (var unitID in _server.GameState.UnitStates.Keys)
        {
            var unitDir = (_server.GameState.UnitStates[unitID].Position - camTransform.position).normalized;
            //Debug.DrawLine(camTransform.position, camTransform.position + unitDir * 50, Color.green, 30f);
            float posHor = Vector3.Dot(unitDir, camTransform.right);
            float posVer = Vector3.Dot(unitDir, camTransform.up);
            if (posVer >= minVerDot && posVer <= maxVerDot &&
                posHor >= minHorDot && posHor <= maxHorDot)
            {
                // Add unit ID to selection.
                SelectedUnits.Add(unitID);
            }
        }
    }

    private void CloneServerMap()
    {
        // get child map if one already exists
        var map = transform.Find("Map");
        if (map == null)
        {
            map = new GameObject("Map").transform;
        }
        map.SetParent(transform);
        var mr = map.gameObject.AddComponent<MeshRenderer>();
        mr.material = GameData.Main.MapMaterial;
        var mf = map.gameObject.AddComponent<MeshFilter>();
        mf.mesh = _server.Map.HeightMapMesh();
    }


}

public struct PlayerCommand
{
    public CommandType CommandID; // the ID of the command (walk, rally point, work, attack, build etc).
    public List<ushort> UnitIDs; // the ID's of the units to carry out this command.
    public Vector2 CommandPosition; // for if the target is a position on the map.
    public ushort CommandTargetID; // for if the target is a unit or tile.
}

public enum CommandType
{
    Walk,
    Work,
    Attack,
    Build
}
