using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Stores prefabs and references for generated content.
/// </summary>
[CreateAssetMenu(fileName = "GameData", menuName = "GameData", order = 1)]
public class GameData : ScriptableObject
{
    private static GameData _current;
    public static GameData Main
    {
        get
        {
            if (_current == null)
            {
                _current = (GameData)AssetDatabase.LoadAssetAtPath("Assets/GameData.asset", typeof(GameData));
            }
            return _current;
        }
    }

    public Material MapMaterial;

    /// <summary>
    /// Listed in order according to the enum UnitType.
    /// </summary>
    public GameObject[] UnitPrefabs;

    /// <summary>
    /// Listed in order according to the enum TileType.
    /// </summary>
    public GameObject[] MapTilePrefabs;
}
