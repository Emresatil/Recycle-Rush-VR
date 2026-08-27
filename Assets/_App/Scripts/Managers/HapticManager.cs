using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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
                Destroy(this); // Core objesini toptan silmemesi icin sadece scripti siliyoruz;
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
        public void TriggerHaptic(XRBaseInputInteractor interactor, float intensity, float duration)
        {
            if (interactor == null) return;
            interactor.SendHapticImpulse(intensity, duration);
        }

        /// <summary>
        /// Oyuncu standart bir çöpü eline aldığında çalınacak kısa ve net titreşim (Tatmin hissi).
        /// </summary>
        public void TriggerGrabHaptic(XRBaseInputInteractor interactor)
        {
            // Editör testleri için görsel log
            Debug.Log("<color=orange>[Haptic/Grab]</color> Obje tutuldu! Kısa 'Click' titreşimi gönderiliyor...");
            // Çok hafif ve kısa bir click (0.3 şiddetinde 0.1 saniye)
            TriggerHaptic(interactor, 0.3f, 0.1f);
        }

        /// <summary>
        /// Oyuncu "Altın Çöp" (Golden Waste) yakaladığında devreye giren efsanevi titreşim efekti.
        /// Kalp atışı / motor çalışması gibi ritmik bir his verir.
        /// </summary>
        public void TriggerGoldenWasteHaptic(XRBaseInputInteractor interactor)
        {
            if (interactor != null)
            {
                StartCoroutine(GoldenHapticRoutine(interactor));
            }
        }

        private IEnumerator GoldenHapticRoutine(XRBaseInputInteractor interactor)
        {
            // 3 kere ritmik olarak kalp atışı gibi vuracak
            for (int i = 0; i < 3; i++)
            {
                interactor.SendHapticImpulse(0.8f, 0.15f); // Güçlü vur
                yield return new WaitForSeconds(0.25f);    // Bekle
                interactor.SendHapticImpulse(0.4f, 0.1f);  // Hafif artçı vur
                yield return new WaitForSeconds(0.3f);     // Bekle
            }
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
