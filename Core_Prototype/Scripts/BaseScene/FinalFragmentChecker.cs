using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InfinityCode.OnlineMapsExamples; // Needed for marker stuff

public class FinalFragmentsChecker : MonoBehaviour
{
    [Header("Fragments Required (IDs)")]
    public List<string> requiredFragmentIDs = new List<string>();

    [Header("Popup Prefab")]
    public GameObject popupPrefab; // TMP prefab with button
    [TextArea] public string popupMessage = "You unlocked all fragments! Go to the final location.";

    [Header("Final Marker Data")]
    public OnlineMaps map;
    public OnlineMaps_MarkerInteractionManager markerManager;
    public MapMarkerData finalMarkerData;

    private bool popupShown = false;
    private bool markerCreated = false;
    private GameObject currentPopup;

    private void Start()
    {
        Invoke(nameof(CheckFragmentsOnSceneLoad), 0.5f);
    }

    private void CheckFragmentsOnSceneLoad()
    {
        if (popupShown) return;
        var inv = InventoryManager.Instance;
        if (inv == null) return;

        foreach (var id in requiredFragmentIDs)
        {
            if (inv.GetCount(id) <= 0) return;
        }

        ShowPopup();
    }

    private void ShowPopup()
    {
        if (popupShown || popupPrefab == null) return;
        popupShown = true;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[FinalFragmentsChecker] No Canvas found!");
            return;
        }

        currentPopup = Instantiate(popupPrefab, canvas.transform);
        currentPopup.transform.localScale = Vector3.one;
        currentPopup.transform.localPosition = Vector3.zero;

        TextMeshProUGUI textComp = currentPopup.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
        {
            textComp.text = popupMessage;
        }

        Button btn = currentPopup.GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnContinuePressed);
        }

        currentPopup.SetActive(true);
    }

    private void OnContinuePressed()
    {
        if (currentPopup != null)
            Destroy(currentPopup);

        if (!markerCreated)
        {
            markerCreated = true;
            CreateFinalMarker();
        }
    }

    private void CreateFinalMarker()
    {
        if (map == null || markerManager == null || finalMarkerData == null)
        {
            Debug.LogError("[FinalFragmentsChecker] Missing references, cannot create final marker.");
            return;
        }

        var marker = map.markerManager.Create(
            new Vector2((float)finalMarkerData.longitude, (float)finalMarkerData.latitude),
            finalMarkerData.defaultIcon,
            finalMarkerData.markerLabel
        );

        marker.OnClick += m =>
            markerManager.SendMessage("OnMarkerClick", new object[] { m, finalMarkerData });

        map.Redraw();
        Debug.Log("[FinalFragmentsChecker] FINAL MARKER CREATED!");
    }
}
