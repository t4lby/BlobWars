using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Allows the player to interact with their own units, select and commands.
/// Supports the sending of those commands to the ServerController.
/// </summary>
public class PlayerController : MonoBehaviour
{
    // Dev target selection of unit and indication of that selection.
    // CLICK SELECTION (via game camera)
    // DRAG SELECTION.

    public List<Unit> SelectedUnits;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            Ray selectionRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(selectionRay, out hit))
            {
                var unit = hit.transform.GetComponentInParent<Unit>();
                if (unit != null)
                {
                    SelectedUnits.Add(unit);
                }
            }
        }
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
