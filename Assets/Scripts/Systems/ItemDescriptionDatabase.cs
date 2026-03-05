using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemDescription
{
    public string itemName;
    [TextArea(2, 5)]
    public string description;
}

public class ItemDescriptionDatabase : MonoBehaviour
{
    public List<ItemDescription> descriptions;

    Dictionary<string, string> lookup = new Dictionary<string, string>();

    void Awake()
    {
        foreach (var d in descriptions)
        {
            lookup[d.itemName] = d.description;
        }
    }

    public string GetDescription(string itemName)
    {
        if (lookup.TryGetValue(itemName, out string desc))
            return desc;

        return "No description.";
    }
}