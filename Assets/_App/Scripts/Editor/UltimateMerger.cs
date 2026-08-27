#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using RecycleRush.Managers;
using RecycleRush.UI;
using TMPro;

public class UltimateMerger : MonoBehaviour
{
    [MenuItem("Tools/3. Tum Arkadas Sistemlerini Ana Sahneye Kopyala")]
    public static void MergeEverything()
    {
        string friendScenePath = "Assets/_App/Scenes/FriendScenes/AR_SceneUnderstanding_Test.unity";
        Scene currentScene = EditorSceneManager.GetActiveScene();
        
        if (currentScene.name != "MainGame_AR" && currentScene.name != "MainGame")
        {
            Debug.LogError("<color=red>[HATA]</color> Lutfen once kendi ana oyun sahnene (MainGame_AR) gecis yap!");
            return;
        }

        GameObject myCore = GameObject.Find("CoreSystems_Module");
        GameObject myXROrigin = GameObject.Find("XR Origin (XR Rig)");
        
        Scene friendScene = EditorSceneManager.OpenScene(friendScenePath, OpenSceneMode.Additive);
        
        GameObject friendCore = null;
        GameObject friendXROrigin = null;
        GameObject portalSpawner = null;
        GameObject levelBoard = null;

        foreach (var rootObj in friendScene.GetRootGameObjects())
        {
            if (rootObj.name.Contains("Core_Managers")) friendCore = rootObj;
            if (rootObj.name.Contains("XR Origin")) friendXROrigin = rootObj;
            if (rootObj.name.Contains("Portal_Spawner")) portalSpawner = rootObj;
            if (rootObj.name.Contains("LevelSelectionBoard")) levelBoard = rootObj;
        }

        if (myCore != null && friendCore != null)
        {
            void CopyScript<T>() where T : MonoBehaviour
            {
                T friendScript = friendCore.GetComponent<T>();
                if (friendScript != null)
                {
                    T myScript = myCore.GetComponent<T>();
                    if (myScript == null) myScript = myCore.AddComponent<T>();
                    EditorUtility.CopySerialized(friendScript, myScript);
                }
            }

            CopyScript<MissionManager>();
            CopyScript<EconomyManager>();
            CopyScript<LevelSelectionManager>();
            CopyScript<LevelManager>();
            CopyScript<AudioManager>();
            CopyScript<VFXManager>();
            
            CopyScript<ObjectPoolManager>();
        }

        GameObject copiedPanelCanvas = null;
        if (friendXROrigin != null && myXROrigin != null)
        {
            Transform friendPanelCanvas = friendXROrigin.transform.Find("PanelCanvas");
            if (friendPanelCanvas != null)
            {
                Transform existing = myXROrigin.transform.Find("PanelCanvas");
                if (existing != null) DestroyImmediate(existing.gameObject);

                copiedPanelCanvas = Instantiate(friendPanelCanvas.gameObject, myXROrigin.transform);
                copiedPanelCanvas.name = "PanelCanvas";
            }
        }

        if (portalSpawner != null && GameObject.Find("Portal_Spawner") == null)
        {
            GameObject p = Instantiate(portalSpawner);
            p.name = "Portal_Spawner";
            SceneManager.MoveGameObjectToScene(p, currentScene);
        }

        GameObject copiedLevelBoard = null;
        if (levelBoard != null)
        {
            GameObject existingBoard = GameObject.Find("LevelSelectionBoard");
            if (existingBoard == null)
            {
                copiedLevelBoard = Instantiate(levelBoard);
                copiedLevelBoard.name = "LevelSelectionBoard";
                SceneManager.MoveGameObjectToScene(copiedLevelBoard, currentScene);
            }
            else
            {
                copiedLevelBoard = existingBoard;
            }
        }

        var uiManager = myCore.GetComponent<UIManager>();
        if (uiManager != null)
        {
            if (copiedPanelCanvas != null)
            {
                foreach (Transform child in copiedPanelCanvas.transform)
                {
                    if (child.name.Contains("MissionPanel")) uiManager.missionPanel = child.gameObject;
                    if (child.name.Contains("XPPanel")) uiManager.xpPanel = child.gameObject;
                    if (child.name.Contains("SafetyWarning")) uiManager.safetyWarningPanel = child.gameObject;
                }

                var texts = copiedPanelCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in texts)
                {
                    if (t.name.Contains("PowerupText") || t.name.Contains("MagnetText")) uiManager.powerupNotificationText = t;
                    if (t.name.Contains("EventText") || t.name.Contains("HourglassText")) uiManager.eventNotificationText = t;
                    if (t.name.Contains("TimeText")) uiManager.timeText = t;
                }
            }

            if (copiedLevelBoard != null)
            {
                uiManager.levelSelectionBoard = copiedLevelBoard;
            }
            EditorUtility.SetDirty(uiManager);
        }

        EditorSceneManager.CloseScene(friendScene, true);
        EditorSceneManager.MarkSceneDirty(currentScene);
        Debug.Log("<color=cyan>TUM ISLEM BASARIYLA TAMAMLANDI!</color> Arkadasinin tum arayuzu ve sistemleri senin sahnene aktarildi.");
    }
}
#endif

