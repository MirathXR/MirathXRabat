using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIManager : MonoBehaviour
{
    [Header("References")]
    public Transform hotbarParent;           // Persistent hotbar container (to assign)
    public Transform mainInventoryParent;    // Main inventory container (to assign)
    public GameObject slotPrefab;            // Slot prefab (to assign)

    [HideInInspector] public BuilderManager builderManager; // linked at runtime if present

    private List<GameObject> hotbarSlots = new List<GameObject>();
    private List<GameObject> mainSlots = new List<GameObject>();
    private Dictionary<string, GameObject> slotMap = new Dictionary<string, GameObject>();

    private void Awake()
    {
        // try link builder manager at runtime (null in treasure hunt)
        if (builderManager == null) builderManager = FindObjectOfType<BuilderManager>();

        CacheSlots();
        InventoryManager.OnItemAdded += OnItemAdded;
        InventoryManager.OnInventoryCleared += OnInventoryCleared;
        RefreshUI();
    }

    private void OnDestroy()
    {
        InventoryManager.OnItemAdded -= OnItemAdded;
        InventoryManager.OnInventoryCleared -= OnInventoryCleared;
    }

    private void CacheSlots()
    {
        hotbarSlots.Clear();
        mainSlots.Clear();
        if (hotbarParent != null)
        {
            foreach (Transform t in hotbarParent) hotbarSlots.Add(t.gameObject);
        }
        if (mainInventoryParent != null)
        {
            foreach (Transform t in mainInventoryParent) mainSlots.Add(t.gameObject);
        }
    }

    private void OnInventoryCleared()
    {
        slotMap.Clear();
        RefreshUI();
    }

    private void OnItemAdded(Item item, int count)
    {
        if (item == null) return;

        // if count <= 0: remove slot
        if (count <= 0)
        {
            if (slotMap.TryGetValue(item.id, out var slot))
            {
                ClearSlotVisual(slot);
                slotMap.Remove(item.id);
            }
            return;
        }

        // update existing slot
        if (slotMap.TryGetValue(item.id, out var existing))
        {
            UpdateSlotCount(existing, count);
            return;
        }

        // find target slot (hotbar if eligible)
        GameObject target = null;
        if (item.isHotbarEligible)
        {
            target = hotbarSlots.Find(s =>
            {
                var icon = s.transform.Find("Icon")?.GetComponent<Image>();
                return icon != null && icon.sprite == null;
            });
        }

        if (target == null)
        {
            target = mainSlots.Find(s =>
            {
                var icon = s.transform.Find("Icon")?.GetComponent<Image>();
                return icon != null && icon.sprite == null;
            });
        }

        if (target == null && mainInventoryParent != null && slotPrefab != null)
        {
            target = Instantiate(slotPrefab, mainInventoryParent);
            target.transform.localScale = Vector3.one;
            mainSlots.Add(target);
        }

        if (target != null)
        {
            FillSlotVisual(target, item, count);
            slotMap[item.id] = target;
        }
    }

    private void FillSlotVisual(GameObject slot, Item item, int count)
    {
        var icon = slot.transform.Find("Icon")?.GetComponent<Image>();
        var countText = slot.transform.Find("CountText")?.GetComponent<TMP_Text>();
        var btn = slot.GetComponent<Button>();

        if (icon != null) { icon.sprite = item.icon; icon.enabled = true; }
        if (countText != null) countText.text = count > 1 ? "x" + count : "";

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                Debug.Log("[UI] Slot clicked: " + item.displayName);
                // call builder only if present
                if (builderManager == null)
                    builderManager = FindObjectOfType<BuilderManager>();

                if (builderManager != null)
                    builderManager.SelectItem(item);
            });
        }
    }

    private void UpdateSlotCount(GameObject slot, int count)
    {
        var countText = slot.transform.Find("CountText")?.GetComponent<TMP_Text>();
        if (countText != null) countText.text = count > 1 ? "x" + count : "";

        if (count <= 0)
        {
            ClearSlotVisual(slot);
        }
    }

    private void ClearSlotVisual(GameObject slot)
    {
        var icon = slot.transform.Find("Icon")?.GetComponent<Image>();
        var countText = slot.transform.Find("CountText")?.GetComponent<TMP_Text>();
        if (icon != null) icon.sprite = null;
        if (countText != null) countText.text = "";
    }

    public void RefreshUI()
    {
        if (InventoryManager.Instance == null) return;
        slotMap.Clear();
        CacheSlots();

        // clear visuals
        foreach (var s in hotbarSlots) ClearSlotVisual(s);
        foreach (var s in mainSlots) ClearSlotVisual(s);

        // populate
        foreach (var entry in InventoryManager.Instance.GetInventoryList())
            OnItemAdded(entry.itemData, entry.count);
    }
}
