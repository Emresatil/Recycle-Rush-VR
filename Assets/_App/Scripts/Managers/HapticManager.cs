using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace RecycleRush.Core
{
    /// <summary>
    /// VR donanım seviyesinde (Native XR API) kontrolcü titreşimlerini yöneten profesyonel Singleton sınıfı.
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
            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
        }

        private void OnDisable()
        {
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
        }

        private void HandleWasteProcessed(SortResultData data)
        {
            Debug.Log($"<color=magenta>[Haptic/Global]</color> Kutuya atıldı! Doğru mu: {data.IsCorrect}");
            if (data.IsCorrect)
            {
                // Doğru Kutu: Tok ve kısa 'Ding' hissi (Şiddet: 0.6, Süre: 0.15s)
                TriggerGlobalHaptic(0.6f, 0.15f);
            }
            else
            {
                // Yanlış Kutu: Rahatsız edici, güçlü ve uzun 'Buzzer' hissi (Şiddet: 1.0, Süre: 0.5s)
                TriggerGlobalHaptic(1.0f, 0.5f);
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
        /// Doğrudan donanım API'sine inerek (Native XR) tüm bağlı VR kontrolcülerini aynı anda titreştirir.
        /// Unity'nin standart cihaz listesinden (InputDevices) donanımı bulduğu için %100 güvenilir ve profesyoneldir.
        /// </summary>
        public void TriggerGlobalHaptic(float intensity, float duration)
        {
            List<InputDevice> devices = new List<InputDevice>();
            // Sadece kontrolcü özelliklerine sahip cihazları (Quest Sağ/Sol Kol) tespit et
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
            
            Debug.Log($"<color=magenta>[Haptic/Global]</color> Bulunan VR Kontrolcü Sayısı: {devices.Count}");

            foreach (var device in devices)
            {
                HapticCapabilities capabilities;
                // Cihaz titreşim motoruna (Haptic) sahipse doğrudan donanıma elektrik (Impulse) yolla
                if (device.TryGetHapticCapabilities(out capabilities))
                {
                    Debug.Log($"<color=magenta>[Haptic/Global]</color> Cihaz: {device.name}, Titreşim Özelliği var mı: {capabilities.supportsImpulse}");
                    if (capabilities.supportsImpulse)
                    {
                        device.SendHapticImpulse(0, intensity, duration);
                    }
                }
                else
                {
                    Debug.LogWarning($"<color=red>[Haptic/Global]</color> Cihaz: {device.name} için titreşim özelliği okunamadı!");
                }
            }
        }
    }
}
