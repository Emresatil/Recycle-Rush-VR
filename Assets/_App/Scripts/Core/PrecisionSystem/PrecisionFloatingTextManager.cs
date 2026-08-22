using UnityEngine;
using TMPro;
using System.Collections;

namespace RecycleRush.Core.PrecisionSystem
{
    /// <summary>
    /// Kutu üzerinde çıkan "PERFECT +100" gibi kaybolan 3D yazıları yönetir.
    /// Tamamen kod üzerinden dinamik olarak TextMeshPro üretir, böylece Inspector'dan prefab atamaya gerek kalmaz.
    /// </summary>
    public class PrecisionFloatingTextManager : MonoBehaviour
    {
        private void OnEnable()
        {
            PrecisionManager.OnPrecisionCalculated += SpawnFloatingText;
        }

        private void OnDisable()
        {
            PrecisionManager.OnPrecisionCalculated -= SpawnFloatingText;
        }

        private void SpawnFloatingText(PrecisionResult result)
        {
            // Eğer isabet çok kötüyse yazı çıkarmaya gerek yok (isteğe bağlı)
            if (result.Tier == PrecisionTier.Normal) return;

            // 1. Yeni bir 3D obje oluştur
            GameObject textObj = new GameObject($"FloatingText_{result.Tier}");
            // Çarpışma noktasının yarım metre üstünden başlasın
            textObj.transform.position = result.HitPoint + new Vector3(0, 0.4f, 0);

            // 2. TextMeshPro bileşeni ekle
            TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = $"<b>{result.Tier.ToString().ToUpper()} +{result.BonusScore}</b>"; // BOLD tag eklendi
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 4.5f; // Boyutu biraz daha büyütüldü (3 -> 4.5)
            tmp.fontStyle = FontStyles.Bold; // TMP Bold stili aktif edildi
            
            // Rengi PrecisionSettings'ten al
            if (PrecisionManager.Instance != null && PrecisionManager.Instance.Settings != null)
            {
                switch (result.Tier)
                {
                    case PrecisionTier.Perfect: tmp.color = PrecisionManager.Instance.Settings.PerfectColor; break;
                    case PrecisionTier.Great: tmp.color = PrecisionManager.Instance.Settings.GreatColor; break;
                    case PrecisionTier.Good: tmp.color = PrecisionManager.Instance.Settings.GoodColor; break;
                }
            }

            // 3. Yazının oyuncunun kamerasına (VR Gözlüğü) bakmasını sağla
            // Genelde Camera.main VR'da CenterEyeAnchor veya Main Camera'dır.
            if (Camera.main != null)
            {
                // Yazı ters dönmesin diye lookAt mantığını çeviriyoruz
                textObj.transform.rotation = Quaternion.LookRotation(textObj.transform.position - Camera.main.transform.position);
            }

            // 4. Animasyon Coroutine'ini başlat
            StartCoroutine(AnimateAndDestroyText(tmp));
        }

        private IEnumerator AnimateAndDestroyText(TextMeshPro tmp)
        {
            float duration = 1.2f;
            float elapsed = 0f;
            
            Vector3 startPos = tmp.transform.position;
            // Yukarı doğru hafif yükselme hedefi
            Vector3 targetPos = startPos + new Vector3(0, 0.5f, 0); 
            Color startColor = tmp.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Yukarı doğru süzülme
                tmp.transform.position = Vector3.Lerp(startPos, targetPos, t);

                // Yavaşça şeffaflaşma (Fade out)
                Color c = tmp.color;
                c.a = Mathf.Lerp(startColor.a, 0f, t);
                tmp.color = c;

                yield return null;
            }

            Destroy(tmp.gameObject);
        }
    }
}
