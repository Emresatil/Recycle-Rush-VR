using UnityEngine;
using RecycleRush.Core;
using System.Collections;

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

        private void OnEnable()
        {
            // Görev tamamlandığında Konfeti patlat
            MissionManager.OnMissionCompleted += HandleMissionCompleted;
            // Speed Mode başladığında Ekranı parlat
            EventManager.OnGameEventStarted += HandleGameEventStarted;
        }

        private void OnDisable()
        {
            MissionManager.OnMissionCompleted -= HandleMissionCompleted;
            EventManager.OnGameEventStarted -= HandleGameEventStarted;
        }

        private void HandleMissionCompleted(MissionData data)
        {
            PlayConfetti();
        }

        private void HandleGameEventStarted(GameEventType eventType)
        {
            if (eventType == GameEventType.SpeedMode)
            {
                PlaySpeedModeFlash();
            }
        }

        /// <summary>
        /// Kameranın 1.5 metre önünde konfeti patlatır.
        /// </summary>
        public void PlayConfetti()
        {
            if (confettiPrefab != null && Camera.main != null)
            {
                // Kameranın önüne ve biraz yukarısına hizala
                Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 1.5f + Vector3.up * 0.5f;
                GameObject confetti = Instantiate(confettiPrefab, spawnPos, Quaternion.identity);
                
                // Efektin 5 saniye sonra sahneyi kirletmemesi için silinmesi
                Destroy(confetti, 5f);
                Debug.Log("<color=yellow>[VFXManager]</color> Görev tamamlandı, Konfeti patladı!");
            }
        }

        /// <summary>
        /// Speed Mode (Hız Modu) parlamasını tetikler.
        /// </summary>
        public void PlaySpeedModeFlash()
        {
            if (speedModeScreenFlash != null)
            {
                speedModeScreenFlash.SetActive(true);
                Debug.Log("<color=yellow>[VFXManager]</color> Speed Mode Parlaması tetiklendi!");
                
                // Yarım saniye sonra efekti kapat
                Invoke(nameof(HideSpeedModeFlash), 0.5f);
            }
        }

        private void HideSpeedModeFlash()
        {
            if (speedModeScreenFlash != null)
            {
                speedModeScreenFlash.SetActive(false);
            }
        }

        /// <summary>
        /// Altın çöp için parıltı partikülünü yaratır ve objeye bağlar.
        /// GoldenWasteVFX scripti tarafından çağrılır.
        /// </summary>
        public GameObject CreateGoldenSparkle(Transform parentTransform)
        {
            if (goldenSparklePrefab != null)
            {
                // Atığın tam ortasında oluştur ve onu ebeveyn (parent) olarak ata ki çöp düşerken onu takip etsin
                GameObject sparkle = Instantiate(goldenSparklePrefab, parentTransform.position, Quaternion.identity, parentTransform);
                return sparkle;
            }
            return null;
        }
    }
}
