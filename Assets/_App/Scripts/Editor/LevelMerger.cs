#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using RecycleRush.Managers;
using RecycleRush.UI;
using UnityEngine.SceneManagement;

public class LevelMerger : MonoBehaviour
{
    [MenuItem("Tools/2. Seviye Sistemini Ana Sahneme Tasi")]
    public static void MergeLevels()
    {
        string friendScenePath = "Assets/_App/Scenes/FriendScenes/AR_SceneUnderstanding_Test.unity";
        Scene currentScene = EditorSceneManager.GetActiveScene();
        
        if (currentScene.name != "MainGame_AR" && currentScene.name != "MainGame")
        {
            Debug.LogError("<color=red>[HATA]</color> Lutfen once kendi ana oyun sahnene (MainGame_AR) gecis yap!");
            return;
        }

        var core = GameObject.Find("CoreSystems_Module");
        if (core != null)
        {
            if (core.GetComponent<LevelSelectionManager>() == null)
            {
                core.AddComponent<LevelSelectionManager>();
                Debug.Log("<color=green>[LevelMerger]</color> LevelSelectionManager basariyla CoreSystems_Module'a eklendi!");
            }
        }

        Scene friendScene = EditorSceneManager.OpenScene(friendScenePath, OpenSceneMode.Additive);
        GameObject levelCanvasToCopy = null;
        
        foreach (var rootObj in friendScene.GetRootGameObjects())
        {
            var cards = rootObj.GetComponentsInChildren<LevelCardUI>(true);
            if (cards.Length > 0)
            {
                levelCanvasToCopy = rootObj;
                break;
            }
        }

        if (levelCanvasToCopy != null)
        {
            GameObject copy = Instantiate(levelCanvasToCopy);
            copy.name = levelCanvasToCopy.name + " (Arkadaştan Kopyalandi)";
            SceneManager.MoveGameObjectToScene(copy, currentScene);
            Debug.Log("<color=green>[LevelMerger]</color> Seviye Menusu (UI) basariyla kendi sahnene tasindi!");
        }
        else
        {
            Debug.LogWarning("Arkadasin sahnesinde Seviye Menusu bulunamadi!");
        }

        EditorSceneManager.CloseScene(friendScene, true);
        EditorSceneManager.MarkSceneDirty(currentScene);
        Debug.Log("<color=cyan>TUM SEVIYE SISTEMI TASINDI!</color> Lutfen Hierarchy'de yeni gelen Panel'i kontrol et.");
    }
}
#endif