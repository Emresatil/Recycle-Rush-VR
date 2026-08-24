using UnityEngine;
using RecycleRush.Managers;

namespace RecycleRush.Environment
{
    /// <summary>
    /// Altın çöplerin (Golden Waste) yere düşene kadar parlama efektini (VFX) kontrol eder.
    /// Bu script sadece Altın Çöp prefablarına (GoldenWastePrefab) eklenmelidir.
    /// </summary>
    public class GoldenWasteVFX : MonoBehaviour
    {
        private GameObject _sparkleInstance;

        private void OnEnable()
        {
            // Çöp havuza (Object Pool) geri gönderilip tekrar doğabileceği için Start yerine OnEnable kullanıyoruz
            if (VFXManager.Instance != null)
            {
                // Parıltıyı oluştur ve bu çöpe bağla
                _sparkleInstance = VFXManager.Instance.CreateGoldenSparkle(transform);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Herhangi bir yüzeye (Zemin, Kutu, Bant) çarptığında parıltıyı sil
            if (_sparkleInstance != null)
            {
                Destroy(_sparkleInstance);
                _sparkleInstance = null;
            }
        }

        private void OnDisable()
        {
            // Eğer çöp havada yok edilirse (veya havuza dönerse) efekti temizle
            if (_sparkleInstance != null)
            {
                Destroy(_sparkleInstance);
                _sparkleInstance = null;
            }
        }
    }
}
