using UnityEngine;

public class DebugButtonTest : MonoBehaviour
{
    public void TestClick()
    {
        Debug.Log("<color=magenta>[DebugButtonTest]</color> Butona tıklandı! GameManager başlatılıyor...");
        
        if (GameManager.Instance != null) {
            Debug.Log("<color=green>GameManager.Instance BULUNDU!</color> Oyun başlatılıyor...");
            GameManager.Instance.PrepareToStart();
        } else {
            Debug.LogError("<color=red>GameManager.Instance HALA NULL!</color>");
        }
    }
}
