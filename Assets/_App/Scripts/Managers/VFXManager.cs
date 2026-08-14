using UnityEngine;

namespace RecycleRush.Managers
{
    /// <summary>
    /// Oyundaki görsel efektleri (Konfeti, Ekran Parlaması, Neon efektleri vb.) merkezi olarak yönetir.
    /// Spagetti kodu engellemek için diğer sınıflar efektleri doğrudan oynatmaz, bu sınıftan talep eder.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Görsel Efekt Referansları (Partiküller)")]
        [Tooltip("Altın çöp (Golden Waste) düşerken çıkacak parıltı efekti")]
        public GameObject goldenSparklePrefab;
        
        [Tooltip("Başarılı bir görev sonrası veya seviye atlandığında patlayacak konfeti")]
        public GameObject confettiPrefab;

        [Header("UI & Ekran Efektleri")]
        [Tooltip("Speed Mode tetiklendiğinde ekranda kısa süreliğine belirecek parlama efekti (Örn: Beyaz veya Mavi bir Canvas Image)")]
        public GameObject speedModeScreenFlash;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Speed Mode (Hız Modu) parlamasını tetikler.
        /// </summary>
        public void PlaySpeedModeFlash()
        {
            if (speedModeScreenFlash != null)
            {
                // TODO: İleride DOTween veya Coroutine ile fade-in/fade-out animasyonu eklenecek
                speedModeScreenFlash.SetActive(true);
                Debug.Log("<color=yellow>[VFXManager]</color> Speed Mode Parlaması tetiklendi!");
            }
        }
    }
}
