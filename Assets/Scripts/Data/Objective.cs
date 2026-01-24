using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Objective
{
    [Header("UI Text")]
    public string potionDisplayName;   // Love, Fire, etc.

    [Header("Recipe Ingredients")]
    public List<string> ingredients;   // Ruby, Oil, Herb etc.

    [HideInInspector]
    public List<bool> discovered;      // Tracks which ingredient slots are revealed
}
