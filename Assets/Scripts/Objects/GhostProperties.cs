using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "GhostProperties", menuName = "ScriptableObjects/GhostProperties")]
public class GhostProperties : ScriptableObject
{
    public Color[] ghostColors;

    public Vector3Int[] spawnPoints = new Vector3Int[4]
    {
        new Vector3Int(-3, 1,0),
        new Vector3Int(2, 1,0),
        new Vector3Int(-3, -1,0),
        new Vector3Int(2, -1,0),
    };
}