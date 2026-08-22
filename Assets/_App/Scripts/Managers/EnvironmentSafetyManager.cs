using System;
using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace RecycleRush.Managers
{
    /// <summary>
    /// Odanın ışık seviyesini veya genel VR kafa takibi (Head Tracking) durumunu kontrol eder.
    /// Takip bozulduğunda (örn: karanlık ortam) oyunu durdurur ve uyarı panelini tetikler.
    /// </summary>
    public class EnvironmentSafetyManager : MonoBehaviour
    {
        public static EnvironmentSafetyManager Instance { get; private set; }

        [Header("Güvenlik Ayarları")]
        [Tooltip("Eğer true ise, kafa takibinin kopması durumunda oyun otomatik olarak duraklatılır.")]
        public bool autoPauseGame = true;

        [Header("Test (Debug)")]
        [Tooltip("Editördeyken 'Karanlık Oda' uyarısını test etmek için bu tiki açın.")]
        public bool simulateLowLightWarning = false;

        // Olay (Event): Güvenlik durumu değiştiğinde tetiklenir.
        // True: Takip bozuldu (Uyarı Göster), False: Her şey normale döndü (Uyarıyı Kapat)
        public static event Action<bool> OnSafetyWarningTriggered;

        private bool _isWarningActive = false;
        private bool _wasPausedBySafety = false;
        private List<InputDevice> _headDevices = new List<InputDevice>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // VR Gözlük cihazlarını bul
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, _headDevices);
        }

        private void Update()
        {
            bool hasTrackingIssue = false;

#if UNITY_EDITOR
            // Editör simülasyonu
            if (simulateLowLightWarning)
            {
                hasTrackingIssue = true;
            }
#endif

            // Cihaz kafa takibini kontrol et
            if (!hasTrackingIssue && _headDevices.Count > 0)
            {
                foreach (var device in _headDevices)
                {
                    if (device.isValid)
                    {
                        // Takip (Tracking) durumunu al
                        if (device.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked))
                        {
                            if (!isTracked)
                            {
                                hasTrackingIssue = true;
                                break;
                            }
                        }
                        
                        // Alternatif: Tracking state'in direkt olarak "None" veya kısıtlı olup olmadığına bakılabilir
                        if (device.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState state))
                        {
                            if (state == InputTrackingState.None)
                            {
                                hasTrackingIssue = true;
                                break;
                            }
                        }
                    }
                }
            }

            // Durum değiştiyse Event fırlat ve oyuna müdahale et
            if (hasTrackingIssue != _isWarningActive)
            {
                SetWarningState(hasTrackingIssue);
            }
        }

        private void SetWarningState(bool isWarningActive)
        {
            _isWarningActive = isWarningActive;
            
            // UI ve diğer sistemleri uyar
            OnSafetyWarningTriggered?.Invoke(_isWarningActive);

            if (autoPauseGame && GameManager.Instance != null)
            {
                if (_isWarningActive)
                {
                    // Eğer oyun "Playing" durumundaysa duraklat
                    if (GameManager.Instance.CurrentState == GameState.Playing)
                    {
                        Debug.Log("<color=red>[EnvironmentSafetyManager]</color> Takip kayboldu! Oyun güvenlik sebebiyle duraklatılıyor.");
                        GameManager.Instance.PauseGame();
                        _wasPausedBySafety = true;
                    }
                }
                else
                {
                    // Sorun düzeldiğinde, eğer oyunu bu sistem durdurduysa geri başlat
                    if (_wasPausedBySafety && GameManager.Instance.CurrentState == GameState.Paused)
                    {
                        Debug.Log("<color=green>[EnvironmentSafetyManager]</color> Takip geri geldi! Oyun devam ediyor.");
                        GameManager.Instance.ResumeGame();
                        _wasPausedBySafety = false;
                    }
                }
            }
        }
    }
}
