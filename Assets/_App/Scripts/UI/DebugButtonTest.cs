using UnityEngine;

public class DebugButtonTest : MonoBehaviour
{
    public void TestClick()
    {
        Debug.Log("<color=magenta>[DebugButtonTest]</color> Butona tıklandı! GameManager Singleton üzerinden başlatılıyor...");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PrepareToStart();
        }
        else
        {
            Debug.LogError("<color=red>[DebugButtonTest]</color> GameManager.Instance bulunamadı! FindFirstObjectByType ile sahnede aranıyor...");
            GameManager gm = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            if (gm != null)
            {
                Debug.LogWarning($"<color=orange>[DebugButtonTest]</color> GameManager bulundu! Obje Adı: {gm.gameObject.name}. Obje aktif mi? {gm.gameObject.activeInHierarchy}");
                if (!gm.gameObject.activeInHierarchy) 
                {
                     gm.gameObject.SetActive(true);
                     Debug.LogWarning("<color=orange>[DebugButtonTest]</color> GameManager objesi kapalı kalmış, zorla açıldı!");
                }
                gm.PrepareToStart();
            }
            else
            {
                Debug.LogError("<color=red>[DebugButtonTest]</color> Sahnede HİÇBİR GameManager bulunamadı! Obje gerçekten yok olmuş veya script silinmiş.");
            }
        }
    }
}

