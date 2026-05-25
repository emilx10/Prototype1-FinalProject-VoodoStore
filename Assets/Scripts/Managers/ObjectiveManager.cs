using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class TaskRow
{
    public TMP_Text taskText;       // Text for the task
    public GameObject strikeLine;   // Strike line over text
}

[System.Serializable]
public class ObjectiveRow
{
    public TMP_Text objectiveText;  // Text for ingredients or title
}

public enum MissionType
{
    BuyItems,
    MergeItems,
    SellItems
}

[System.Serializable]
public class Mission
{
    public string missionText;      // e.g. "Buy Agate, Mint, Floral Oil" or "Merge items"
    public MissionType type;
    public bool completed = false;  // Strike line state
}

[System.Serializable]
public class Objective
{
    public string potionDisplayName;       // Name of potion
    public List<string> ingredients;       // Ingredient names
    public List<Mission> missions;         // Missions for this objective
    public List<bool> discovered;          // Ingredient discovery
}

public class ObjectiveManager : MonoBehaviour
{
    [Header("Objectives List")]
    [SerializeField] public List<Objective> objectives;

    [Header("Ledger UI")]
    [SerializeField] public GameObject ledgerPanel;

    [Header("Objectives Section")]
    [SerializeField] public List<ObjectiveRow> objectiveRows; // Assign manually

    [Header("Tasks Section")]
    [SerializeField] public List<TaskRow> taskRows;           // Assign manually

    [Header("Daily Limit")]
    [SerializeField] private int investigationsPerDay = 1;
    private int investigationsLeftToday;

    [Header("Investigation Cost")]
    [SerializeField] private int investigationCost = 5;

    [Header("SoundManager")]
    public AudioManager ad;
    [SerializeField] float vol, pitch;

    private GameManager gameManager;

    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    void Start()
    {
        investigationsLeftToday = investigationsPerDay;

        // Initialize discovery and missions
        foreach (var obj in objectives)
        {
            obj.discovered = new List<bool>();
            for (int i = 0; i < obj.ingredients.Count; i++)
                obj.discovered.Add(false);

            foreach (var m in obj.missions)
                m.completed = false;
        }

        RefreshObjectivesUI();
        RefreshTasksUI();
    }

    #region Ledger Toggle / Daily Limit

    public void ToggleLedger()
    {
        ledgerPanel.SetActive(!ledgerPanel.activeSelf);
        // Update tasks every time player opens ledger
        UpdateTasksFromInventory(gameManager.GetInventoryItems());
    }

    public bool CanInvestigateToday() => investigationsLeftToday > 0;
    public bool CanAffordInvestigation() => gameManager != null && gameManager.coins >= investigationCost;
    public void ResetDailyInvestigations() => investigationsLeftToday = investigationsPerDay;

    #endregion

    #region Objectives (Ingredients)

    public void RefreshObjectivesUI()
    {
        if (objectives.Count == 0 || objectiveRows.Count == 0) return;

        Objective obj = objectives[0];

        for (int i = 0; i < objectiveRows.Count; i++)
        {
            ObjectiveRow row = objectiveRows[i];
            if (row.objectiveText == null) continue;

            if (i == 0)
            {
                // First row = Potion title
                row.objectiveText.text = $"Craft a {obj.potionDisplayName} Potion";
            }
            else
            {
                int ingIndex = i - 1;
                if (ingIndex < obj.ingredients.Count)
                    row.objectiveText.text = obj.discovered[ingIndex] ? obj.ingredients[ingIndex] : "???";
                else
                    row.objectiveText.text = ""; // hide extra rows
            }
        }
    }

    #endregion

    #region Tasks (Missions)

    public void RefreshTasksUI()
    {
        if (objectives.Count == 0 || taskRows.Count == 0) return;

        Objective obj = objectives[0];

        for (int i = 0; i < taskRows.Count && i < obj.missions.Count; i++)
        {
            TaskRow row = taskRows[i];
            Mission mission = obj.missions[i];

            if (row.strikeLine != null)
                row.strikeLine.SetActive(mission.completed);

            if (row.taskText != null)
            {
                if (mission.type == MissionType.BuyItems)
                {
                    string progress = mission.completed ? "1/1" : "0/1";
                    row.taskText.text = $"{mission.missionText} {progress}";
                }
                else
                {
                    row.taskText.text = mission.missionText;
                }
            }
        }
    }

    /// <summary>
    /// Check all missions against player's inventory and updates strike lines.
    /// Call this when opening ledger or after buying/merging items.
    /// </summary>
    public void UpdateTasksFromInventory(List<InventoryItem> playerInventory)
    {
        if (objectives.Count == 0 || taskRows.Count == 0) return;

        Objective obj = objectives[0];

        for (int i = 0; i < obj.missions.Count && i < taskRows.Count; i++)
        {
            Mission mission = obj.missions[i];
            TaskRow row = taskRows[i];

            switch (mission.type)
            {
                case MissionType.BuyItems:
                    {
                        string requiredItem = mission.missionText.Trim().ToLower();

                        bool found = false;

                        foreach (var inv in playerInventory)
                        {
                            if (inv.count > 0 &&
                                inv.itemName.Trim().ToLower() == requiredItem)
                            {
                                found = true;
                                break;
                            }
                        }

                        mission.completed = found;
                        break;
                    }

                case MissionType.MergeItems:
                    // For merge items, you must set mission.completed = true manually when merge occurs
                    break;
            }

            // Update StrikeLine
            if (row.strikeLine != null)
                row.strikeLine.SetActive(mission.completed);

            if (row.taskText != null)
            {
                if (mission.type == MissionType.BuyItems)
                {
                    string progress = mission.completed ? "1/1" : "0/1";
                    row.taskText.text = $"{mission.missionText} {progress}";
                }
                else
                {
                    row.taskText.text = mission.missionText;
                }
            }
        }
    }

    #endregion

    #region Investigate

    public bool InvestigateItem(string itemName)
    {
        if (investigationsLeftToday <= 0 || gameManager.coins < investigationCost)
            return false;

        investigationsLeftToday--;
        gameManager.coins -= investigationCost;
        gameManager.UpdateCoinsUI();

        string key = itemName.ToLower().Trim();
        bool revealedSomething = false;

        foreach (var obj in objectives)
        {
            for (int i = 0; i < obj.ingredients.Count; i++)
            {
                if (!obj.discovered[i] && obj.ingredients[i].ToLower().Trim() == key)
                {
                    obj.discovered[i] = true;
                    revealedSomething = true;
                }
            }
        }

        if (revealedSomething)
            RefreshObjectivesUI();

        gameManager.PopulateInventoryPanel();
        return revealedSomething;
    }
    public void CompleteMission(MissionType type)
    {
        if (objectives.Count == 0) return;

        Objective obj = objectives[0];

        foreach (var mission in obj.missions)
        {
            if (mission.type == type && !mission.completed)
            {
                mission.completed = true;
                ad.PlaySfx(vol, SFX.Objective, pitch);
                break; // complete only the first matching one
            }
        }

        RefreshTasksUI();
    }
    #endregion
}
