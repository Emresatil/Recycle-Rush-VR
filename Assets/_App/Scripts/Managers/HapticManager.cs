using UnityEngine;


namespace RecycleRush.Core
{
    /// <summary>
    /// XR API kullanarak kontrolcü titreşimlerini (Haptic Feedback) merkezi olarak yöneten sistem.
    /// Singleton mimarisi ile her yerden erişilebilir.
    /// </summary>
    public class HapticManager : MonoBehaviour
    {
        public static HapticManager Instance { get; private set; }

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
            // BinTrigger'dan gelen kutuya atılma sinyallerini dinle
            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
        }

        private void OnDisable()
        {
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
        }

        /// <summary>
        /// Obje kutuya girdiğinde skora göre farklı titreşimler verir.
        /// </summary>
        private void HandleWasteProcessed(SortResultData data)
        {
            if (data.IsCorrect)
            {
                // Doğru Kutu: Tatmin edici, orta şiddette kısa titreşim
                TriggerGlobalHaptic(0.5f, 0.2f);
            }
            else
            {
                // Yanlış Kutu: Rahatsız edici, yüksek şiddette uzun titreşim (Buzzer hissi)
                TriggerGlobalHaptic(0.8f, 0.4f);
            }
        }

        /// <summary>
        /// Sadece belirli bir kontrolcüyü (objeyi tutan eli) titreştirir.
        /// </summary>
        public void TriggerHaptic(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor interactor, float intensity, float duration)
        {
            if (interactor == null) return;
            interactor.SendHapticImpulse(intensity, duration);
        }

        /// <summary>
        /// Önemli anlarda her iki eldeki kontrolcüyü birden titreştirir.
        /// </summary>
        public void TriggerGlobalHaptic(float intensity, float duration)
        {
            // Sahnede olan tüm Controller Interactor'ları bul (Sağ ve Sol el)
            var interactors = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor>(FindObjectsSortMode.None);
            foreach (var interactor in interactors)
            {
                interactor.SendHapticImpulse(intensity, duration);
            }
        }
    }
}
