using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class InventoryEntry
{
    public Item itemData;
    public int count;

    public InventoryEntry(Item data, int c)
    {
        itemData = data;
        count = c;
    }
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    // Inventory storage (MAIN inventory)
    private Dictionary<string, int> counts = new Dictionary<string, int>();
    private Dictionary<string, Item> lookup = new Dictionary<string, Item>();

    // Ordered list for hotbar / scene persistence
    public List<InventoryEntry> collectedItems = new List<InventoryEntry>();

    // Events
    public static event Action<Item, int> OnItemAdded;
    public static event Action OnInventoryCleared;

    [Header("Completion")]
    public int requiredUniqueCount = 999;
    public GameObject completionPopupPrefab;

    [Header("UI Root (persistent canvas)")]
    public GameObject Canvas_InventoryUI;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Canvas_InventoryUI == null) return;

        bool showUI = scene.name != "1_MainMenuScene" && scene.name != "5_QuizScene";
        Canvas_InventoryUI.SetActive(showUI);

        if (completionPopupPrefab != null)
        {
            var existingPopup = GameObject.Find(completionPopupPrefab.name + "(Clone)");
            if (existingPopup != null)
                existingPopup.SetActive(false);
        }

        if (showUI)
            RefreshInventoryUI();
    }

    // ================= Core Inventory =================

    public void AddItem(Item data)
    {
        if (data == null) return;

        if (!lookup.ContainsKey(data.id))
            lookup[data.id] = data;

        if (!counts.ContainsKey(data.id))
            counts[data.id] = 0;

        if (data.stackable)
            counts[data.id]++;
        else if (counts[data.id] == 0)
            counts[data.id] = 1;

        // Update collectedItems
        InventoryEntry entry = collectedItems.Find(e => e.itemData.id == data.id);
        if (entry != null)
            entry.count = counts[data.id];
        else
            collectedItems.Add(new InventoryEntry(data, counts[data.id]));

        OnItemAdded?.Invoke(data, counts[data.id]);

       
        if (GetFragmentCount() >= requiredUniqueCount)
            ShowCompletionPopup();
    }

    public int GetFragmentCount()
    {
        // Fragments are all the items in main inventory (for now-to be changed)
        return lookup.Count;
    }

    public int GetCount(string id) => counts.TryGetValue(id, out var v) ? v : 0;
    public Item GetData(string id) => lookup.TryGetValue(id, out var d) ? d : null;
    public Dictionary<string, int> GetAll() => new Dictionary<string, int>(counts);
    public int GetUniqueCount() => lookup.Count;

    public bool ConsumeOne(string id)
    {
        if (!counts.ContainsKey(id) || counts[id] <= 0) return false;
        counts[id]--;
        OnItemAdded?.Invoke(lookup[id], counts[id]);

        InventoryEntry entry = collectedItems.Find(e => e.itemData.id == id);
        if (entry != null)
            entry.count = counts[id];

        return true;
    }

    public void ClearAll()
    {
        counts.Clear();
        lookup.Clear();
        collectedItems.Clear();
        OnInventoryCleared?.Invoke();
    }

    private void ShowCompletionPopup()
    {
        if (completionPopupPrefab != null)
        {
            if (GameObject.Find(completionPopupPrefab.name + "(Clone)") == null)
            {
                var popup = Instantiate(completionPopupPrefab, Canvas_InventoryUI.transform);
                popup.SetActive(true);
            }
        }
        else
        {
            Debug.Log("All required items collected — go build the column!");
        }
    }

    public void RefreshInventoryUI()
    {
        if (Canvas_InventoryUI == null) return;

        var ui = Canvas_InventoryUI.GetComponentInChildren<InventoryUIManager>();
        if (ui != null)
            ui.RefreshUI();
    }

    public List<InventoryEntry> GetInventoryList()
    {
        return new List<InventoryEntry>(collectedItems);
    }
}
