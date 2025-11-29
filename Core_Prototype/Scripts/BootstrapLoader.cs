using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    [Header("Persistent Prefabs")]
    public GameObject inventoryManagerPrefab;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // Instantiate InventoryManager if it doesn't exist
        if (FindObjectOfType<InventoryManager>() == null && inventoryManagerPrefab != null)
        {
            GameObject mgr = Instantiate(inventoryManagerPrefab);
            mgr.name = "InventoryManager";
            DontDestroyOnLoad(mgr);
            Debug.Log("[BootstrapLoader] InventoryManager instantiated.");
        }

        // Load the first real scene (MainMenuScene, index 1)
        if (SceneManager.sceneCountInBuildSettings > 1)
        {
            string mainMenuPath = SceneUtility.GetScenePathByBuildIndex(1);
            string mainMenuName = System.IO.Path.GetFileNameWithoutExtension(mainMenuPath);
            SceneManager.LoadScene(mainMenuName);
            Debug.Log("[BootstrapLoader] Loading MainMenuScene: " + mainMenuName);
        }
        else
        {
            Debug.LogError("[BootstrapLoader] Build Settings missing scenes!");
        }
    }
}
