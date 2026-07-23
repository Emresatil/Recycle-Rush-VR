#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
using RecycleRush.UI;

public class RankingSetupTool
{
    [MenuItem("Recycle Rush/Fix Scoreboard & Create TV")]
    public static void SetupRankingTV()
    {
        // 1. Sahnedeki "scoreboard" objesini bul
        GameObject scoreboard = GameObject.Find("scoreboard");
        if (scoreboard == null)
        {
            Debug.LogError("Sahnenizde 'scoreboard' adında bir obje bulunamadı! Lütfen hierarchy'deki adının tam olarak 'scoreboard' olduğundan emin olun.");
            return;
        }

        // 2. GLB/Prefab kilitlerini kır (Sonsuzluk hatasının sebebi bu kilit!)
        if (PrefabUtility.IsPartOfPrefabInstance(scoreboard))
        {
            PrefabUtility.UnpackPrefabInstance(scoreboard, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        // Sonsuzluk (Infinity) Scale hatasını şimdi KESİN olarak düzelt
        // Kullanıcının attığı son resimde Y ve Z değeri 0.3772 idi.
        scoreboard.transform.localScale = new Vector3(0.3772f, 0.3772f, 0.3772f);
        
        // Obje uzay boşluğundaysa kameranın görebileceği makinenin yanına çekelim
        if (scoreboard.transform.position.x > 100 || scoreboard.transform.position.y > 100 || scoreboard.transform.position.x < -100)
        {
            scoreboard.transform.position = new Vector3(4.2f, 2.5f, -0.2f); // Son fotodaki konuma yakın
            scoreboard.transform.rotation = Quaternion.Euler(0, -90, 0); 
        }

        // 3. İçinde daha önce yanlışlıkla eklenmiş TMP yazıları varsa sil (Dev T harflerini yok eder)
        TextMeshProUGUI[] oldTexts = scoreboard.GetComponents<TextMeshProUGUI>();
        foreach (var t in oldTexts) Object.DestroyImmediate(t);

        // 4. İçinde doğru Canvas var mı kontrol et, yoksa oluştur
        Canvas canvas = scoreboard.GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("RankingCanvas");
            canvasObj.transform.SetParent(scoreboard.transform, false);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Boyutlandırma
            RectTransform rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800, 400);
            rt.localScale = new Vector3(0.005f, 0.005f, 0.005f);
            rt.localPosition = new Vector3(0, 0, -0.05f); // Ekrandan çok az öne çıkar ki içine girmesin
        }

        // 5. İçinde RankingBoard yazısı var mı kontrol et, yoksa oluştur
        RankingBoard board = canvas.GetComponentInChildren<RankingBoard>();
        if (board == null)
        {
            GameObject textObj = new GameObject("RankingText");
            textObj.transform.SetParent(canvas.transform, false);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 72;
            
            // RankingBoard scripti çalışınca yazıyı kendisi altın sarısı yapacak, şimdilik beyaz bırakıyoruz
            text.text = "BEST SCORE\n<color=#FFD700>0</color>";

            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(800, 400);
            textRt.localPosition = Vector3.zero;

            textObj.AddComponent<RankingBoard>();
        }

        Debug.Log("<color=green>[SIHİR GERÇEKLEŞTİ!]</color> Scoreboard Scale düzeltildi, dev harfler yok edildi ve Rekor Tablosu eklendi!");
    }
}
#endif
