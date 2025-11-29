using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

public class BuilderManager : MonoBehaviour
{
    [Header("References")]
    public Transform buildParent;
    public InventoryUIManager inventoryUIManager;
    public Canvas builderCanvas;
    public Button resetButton;
    public Button gravityButton;
    public ARRaycastManager arRaycastManager;

    [Header("Prefabs")]
    public GameObject basePrefab;
    public GameObject shaftPrefab;
    public GameObject capitolPrefab;

    [Header("Popups")]
    public GameObject completionPopupPrefab;
    public GameObject collapsePopupPrefab;
    public GameObject fragmentPopupPrefab;
    public Item fragmentItem;

    [Header("Completion Text")]
    public string completionMessage = "Your structure stands! Great job!";
    public float gravityTestDelay = 1.5f;
    public float collapseThreshold = 0.01f;

    // Runtime
    private Item currentSelectedItem;
    private GameObject lastPlacedObject;
    private Vector3 lastTapWorld;

    private Dictionary<string, GameObject> itemPrefabMap;

    private void Awake()
    {
        itemPrefabMap = new Dictionary<string, GameObject>
        {
            { "Column_Base", basePrefab },
            { "Column_Shaft", shaftPrefab },
            { "Column_Capitol", capitolPrefab }
        };

        if (inventoryUIManager == null)
            inventoryUIManager = FindObjectOfType<InventoryUIManager>();

        if (inventoryUIManager != null)
            inventoryUIManager.builderManager = this;
    }

    private void Start()
    {
        if (resetButton != null) resetButton.onClick.AddListener(ResetStructure);
        if (gravityButton != null) gravityButton.onClick.AddListener(OnGravityButtonPressed);
    }

    public void SelectItem(Item item)
    {
        if (item == null) return;
        currentSelectedItem = item;
    }

    private void Update()
    {
        if (currentSelectedItem == null) return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            if (TryGetTapWorld(out lastTapWorld))
                PlaceSelectedItem();
        }
#else
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began && !IsPointerOverUI(t.fingerId))
            {
                if (TryGetTapWorld(out lastTapWorld, t.position))
                    PlaceSelectedItem();
            }
        }
#endif
    }

    private bool IsPointerOverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    private bool IsPointerOverUI(int fingerId) => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);

    private bool TryGetTapWorld(out Vector3 world)
    {
        return TryGetTapWorld(out world, Input.mousePosition);
    }

    private bool TryGetTapWorld(out Vector3 world, Vector2 screenPos)
    {
        world = Vector3.zero;
        if (arRaycastManager != null)
        {
            var hits = new List<ARRaycastHit>();
            if (arRaycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            {
                world = hits[0].pose.position;
                return true;
            }
        }
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float enter))
        {
            world = ray.GetPoint(enter);
            return true;
        }
        world = Camera.main.transform.position + Camera.main.transform.forward * 1f;
        return true;
    }

    private void PlaceSelectedItem()
    {
        if (currentSelectedItem == null || InventoryManager.Instance == null) return;

        int count = InventoryManager.Instance.GetCount(currentSelectedItem.id);
        if (count <= 0)
        {
            currentSelectedItem = null;
            return;
        }

        if (!CanPlace(currentSelectedItem)) return;

        if (!itemPrefabMap.TryGetValue(currentSelectedItem.id, out GameObject prefab) || prefab == null)
        {
            currentSelectedItem = null;
            return;
        }

        Vector3 placePos = ComputeSnapPosition(prefab);

        GameObject placed = Instantiate(prefab, placePos, prefab.transform.rotation, buildParent);
        placed.AddComponent<PlacedItem>().itemId = currentSelectedItem.id;

        Rigidbody rb = placed.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        InventoryManager.Instance.ConsumeOne(currentSelectedItem.id);
        lastPlacedObject = placed;
        currentSelectedItem = null;
    }

    private Vector3 ComputeSnapPosition(GameObject prefab)
    {
        if (lastPlacedObject == null) return lastTapWorld;

        Transform snapTop = lastPlacedObject.transform.Find("SnapPointTop");
        if (snapTop == null) return lastTapWorld;

        Transform prefabBottom = prefab.transform.Find("SnapPointBottom");
        Vector3 offset = prefabBottom != null ? prefabBottom.localPosition : Vector3.zero;

        Vector3 pos = snapTop.position - offset;
        pos.x = lastPlacedObject.transform.position.x;
        pos.z = lastPlacedObject.transform.position.z;
        return pos;
    }

    private bool CanPlace(Item item)
    {
        if (item == null) return false;
        if (item.id == "Column_Base") return true;
        if (item.id == "Column_Shaft") return HasPlaced("Column_Base");
        if (item.id == "Column_Capitol") return HasPlaced("Column_Shaft");
        return true;
    }

    private bool HasPlaced(string id)
    {
        return buildParent.GetComponentsInChildren<PlacedItem>().Any(x => x.itemId == id);
    }

    private void OnGravityButtonPressed()
    {
        var placedList = buildParent.GetComponentsInChildren<Transform>()
            .Where(t => t.GetComponent<PlacedItem>() != null)
            .Select(t => t.gameObject)
            .ToList();

        if (placedList.Count == 0) return;

        StartCoroutine(GravityTestCoroutine(placedList));
    }

    private IEnumerator GravityTestCoroutine(List<GameObject> placedList)
    {
        Dictionary<GameObject, Vector3> initialPositions = new Dictionary<GameObject, Vector3>();
        foreach (var go in placedList)
            initialPositions[go] = go.transform.position;

        foreach (var go in placedList)
        {
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb != null)
                rb.isKinematic = false;
        }

        yield return new WaitForSeconds(gravityTestDelay);

        bool collapsed = false;
        foreach (var go in placedList)
        {
            float deltaY = Mathf.Abs(go.transform.position.y - initialPositions[go].y);
            if (deltaY > collapseThreshold)
            {
                collapsed = true;
                break;
            }
        }

        if (collapsed) ShowCollapsePopup();
        else ShowCompletionPopup();

        foreach (var go in placedList)
        {
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    public void ResetStructure()
    {
        var placedItems = buildParent.GetComponentsInChildren<PlacedItem>().ToList();
        foreach (var pi in placedItems)
        {
            InventoryManager.Instance.AddItem(InventoryManager.Instance.GetData(pi.itemId));
            Destroy(pi.gameObject);
        }
        lastPlacedObject = null;
    }

    // ------------------ COMPLETION POPUP ------------------
    private void ShowCompletionPopup()
    {
        if (completionPopupPrefab == null || builderCanvas == null) return;

        GameObject popup = Instantiate(completionPopupPrefab, builderCanvas.transform);
        popup.SetActive(true);

        var txt = popup.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        if (txt != null) txt.text = completionMessage;

        var btn = popup.transform.Find("Continue")?.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                Destroy(popup);
                ShowFragmentPopup();
            });
        }
    }

    // ------------------ FRAGMENT POPUP ------------------
    private void ShowFragmentPopup()
    {
        if (fragmentPopupPrefab == null || builderCanvas == null || fragmentItem == null)
            return;

        GameObject popup = Instantiate(fragmentPopupPrefab, builderCanvas.transform);
        popup.SetActive(true);

        var txt = popup.transform.Find("Text_FragmentUnlocked")?.GetComponent<TextMeshProUGUI>();
        if (txt != null)
            txt.text = $"New fragment unlocked:\n{fragmentItem.displayName}";

        var img = popup.transform.Find("Image_FragmentIcon")?.GetComponent<Image>();
        if (img != null)
            img.sprite = fragmentItem.icon;

        var btn = popup.transform.Find("Continue_NextScene")?.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                InventoryManager.Instance.AddItem(fragmentItem);
                Destroy(popup);
                UnityEngine.SceneManagement.SceneManager.LoadScene("2_BaseScene");
            });
        }
    }

    private void ShowCollapsePopup()
    {
        if (collapsePopupPrefab == null || builderCanvas == null) return;

        GameObject popup = Instantiate(collapsePopupPrefab, builderCanvas.transform);
        popup.SetActive(true);

        var retryBtn = popup.transform.Find("Retry")?.GetComponent<Button>();
        if (retryBtn != null)
        {
            retryBtn.onClick.AddListener(() =>
            {
                ResetStructure();
                Destroy(popup);
            });
        }

        var txt = popup.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        if (txt != null)
            txt.text = "Your column collapsed!";
    }
}

// Helper
public class PlacedItem : MonoBehaviour
{
    public string itemId;
}
