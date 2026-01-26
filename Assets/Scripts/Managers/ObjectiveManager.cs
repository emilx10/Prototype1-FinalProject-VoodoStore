using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ObjectiveManager : MonoBehaviour
{
    [Header("Objectives List")]
    public List<Objective> objectives;

    [Header("Ledger UI")]
    public GameObject ledgerPanel;
    public Transform objectivesTextParent;
    public GameObject objectiveTextPrefab;

    [Header("Daily Limit")]
    [SerializeField] private int investigationsPerDay = 1;
    private int investigationsLeftToday;

    [Header("Investigation Cost")]
    [SerializeField] private int investigationCost = 5;

    private GameManager gameManager;

    void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();
    }
    void Start()
    {
        ledgerPanel.SetActive(false);
        investigationsLeftToday = investigationsPerDay;

        // Initialize discovery tracking
        foreach (var obj in objectives)
        {
            obj.discovered = new List<bool>();
            for (int i = 0; i < obj.ingredients.Count; i++)
                obj.discovered.Add(false);
        }

        RefreshLedgerUI();
    }

    public bool CanInvestigateToday()
    {
        return investigationsLeftToday > 0;
    }

    public void ToggleLedger()
    {
        ledgerPanel.SetActive(!ledgerPanel.activeSelf);
    }

    public void RefreshLedgerUI()
    {
        foreach (Transform child in objectivesTextParent)
            Destroy(child.gameObject);

        foreach (Objective obj in objectives)
        {
            GameObject entry = Instantiate(objectiveTextPrefab, objectivesTextParent);
            TMP_Text txt = entry.GetComponent<TMP_Text>();

            string text = "Make a " + obj.potionDisplayName + " Potion\n";

            for (int i = 0; i < obj.ingredients.Count; i++)
            {
                text += obj.discovered[i] ? "• " + obj.ingredients[i] + "\n"
                                          : "• ?\n";
            }

            txt.text = text;
        }
    }

    public bool InvestigateItem(string itemName)
    {
        Debug.Log($"Investigate {itemName} | LeftToday:{investigationsLeftToday} | Coins:{gameManager.coins}");

        if (investigationsLeftToday <= 0)
            return false;

        if (gameManager.coins < investigationCost)
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
                if (obj.ingredients[i].ToLower().Trim() == key && !obj.discovered[i])
                {
                    obj.discovered[i] = true;
                    revealedSomething = true;
                }
            }
        }

        if (revealedSomething)
            RefreshLedgerUI();

        gameManager.PopulateInventoryPanel(); // refresh buttons
        return revealedSomething;
    }


    public void ResetDailyInvestigations()
    {
        investigationsLeftToday = investigationsPerDay;
    }

    public bool CanAffordInvestigation()
    {
        return gameManager != null && gameManager.coins >= investigationCost;
    }
}
