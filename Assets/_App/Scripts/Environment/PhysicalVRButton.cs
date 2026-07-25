using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace RecycleRush.Environment
{
    /// <summary>
    /// VR elleriyle (Collider) fiziksel olarak basılabilen, UnityEvent tetikleyen endüstri standartlarında (AAA) buton sistemi.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class PhysicalVRButton : MonoBehaviour
    {
        [Header("Button Settings")]
        [Tooltip("Butonun aşağı-yukarı hareket eden kısmı (Örn: Butonun kırmızı görsel objesi)")]
        [SerializeField] private Transform _pushPart;
        
        [Tooltip("Butonun Y ekseninde ne kadar aşağı çökeceği (Metre cinsinden, genelde 0.02f iyidir)")]
        [SerializeField] private float _pressDistance = 0.02f;
        
        [Tooltip("Butonun aşağı inme ve eski haline dönme (yaylanma) animasyon hızı")]
        [SerializeField] private float _springSpeed = 0.05f;

        public enum ButtonType { Custom, Play, Pause, Retry, Settings, Exit }

        [Header("Game Actions & Feedback")]
        [Tooltip("Butonun oyun içindeki görevi (Özel bir görevse Custom seçip OnPressed eventini kullanın)")]
        public ButtonType buttonType = ButtonType.Custom;
        
        [Tooltip("Butona basıldığında çıkacak görsel efekt Prefab'ı (Oyun içinde otomatik doğup silinecektir)")]
        [SerializeField] private GameObject _pressVFXPrefab;

        [Header("Events (Olaylar)")]
        [Tooltip("Butona tam olarak basıldığında tetiklenecek fonksiyonlar (Örn: GameManager.StartGame)")]
        public UnityEvent OnPressed;
        
        [Tooltip("Buton serbest bırakıldığında tetiklenecek fonksiyonlar")]
        public UnityEvent OnReleased;

        [Header("Audio")]
        [Tooltip("Butona basıldığında çalınacak donanımsal Klik sesi")]
        [SerializeField] private AudioClip _clickSound;

        private AudioSource _audioSource;
        private Vector3 _originalLocalPos;
        private bool _isPressed = false;
        
        // Debounce: Birden fazla parmağın aynı anda butona basması durumunda oyunun bug'a girmesini engeller.
        private int _collidersInTrigger = 0; 

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false; // Oyun başlar başlamaz kendi kendine çalmasın
            
            if (_pushPart != null)
            {
                // Butonun orijinal (başlangıç) pozisyonunu hafızaya al ki el çekilince nereye döneceğini bilsin.
                _originalLocalPos = _pushPart.localPosition;
            }
            else
            {
                Debug.LogWarning($"<color=orange>[PhysicalVRButton]</color> {gameObject.name} üzerinde Push Part atanmadı! Lütfen Inspector'dan atayın.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // İstenirse buraya 'if (!other.CompareTag("PlayerHand")) return;' eklenerek sadece elin basması sağlanabilir.
            
            _collidersInTrigger++;

            // Eğer buton zaten basılı DEĞİLSE ve içeriye en az 1 cisim girdiyse butonu çökert
            if (!_isPressed && _collidersInTrigger > 0)
            {
                PressButton();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            _collidersInTrigger--;
            
            // Güvenlik önlemi: Sayacın sıfırın altına düşüp bug yaratmasını engeller
            if (_collidersInTrigger < 0) _collidersInTrigger = 0;

            // Eğer buton basılıysa ve içerideki TÜM cisimler (eller) çıktıysa butonu serbest bırak
            if (_isPressed && _collidersInTrigger == 0)
            {
                ReleaseButton();
            }
        }

        public void PressButton()
        {
            if (_isPressed) return; // Zaten basılıysa tekrar basma
            
            _isPressed = true;
            
            // Klik sesi çal
            if (_clickSound != null)
            {
                _audioSource.PlayOneShot(_clickSound);
            }

            // Butonu aşağı it ve lazerle basıldıysa otomatik geri yaylandır (Auto-Release)
            StopAllCoroutines();
            StartCoroutine(PressAndAutoReleaseRoutine());
            
            // --- GÖRSEL VE DOKUNSAL (HAPTIC) EFEKTLER ---
            if (_pressVFXPrefab != null)
            {
                // Efekti butonun ortasında yoktan var et (Instantiate)
                GameObject vfxObj = Instantiate(_pressVFXPrefab, transform.position, Quaternion.identity);
                
                // Eğer içinde ParticleSystem varsa otomatik çalışıp süresi bitince yok olsun
                ParticleSystem ps = vfxObj.GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                {
                    Destroy(vfxObj, ps.main.duration + ps.main.startLifetime.constantMax);
                }
                else
                {
                    // Her ihtimale karşı 2 saniye sonra sil ki sahnede çöp kalmasın
                    Destroy(vfxObj, 2f);
                }
            }

            if (RecycleRush.Core.HapticManager.Instance != null)
            {
                // Butona basıldığında tok bir dokunsal geri bildirim ver (Şiddet: 0.3, Süre: 0.1s)
                RecycleRush.Core.HapticManager.Instance.TriggerGlobalHaptic(0.3f, 0.1f);
            }
            
            // Otomatik GameManager Aksiyonları
            ExecuteAction();

            // Inspector'dan atanan özel fonksiyonları tetikle!
            OnPressed?.Invoke();
        }

        /// <summary>
        /// Lazer ışınıyla (XR Raycast) basıldığında butonun çöktükten 0.2 saniye sonra otomatik yaylanıp yukarı çıkmasını sağlar.
        /// </summary>
        private IEnumerator PressAndAutoReleaseRoutine()
        {
            yield return StartCoroutine(MoveButtonRoutine(_originalLocalPos.y - _pressDistance));
            
            // 0.2 saniye çökük vaziyette kalsın (Tok basılma hissi için)
            yield return new WaitForSeconds(0.2f);

            // Eğer içeride fiziksel bir el yoksa (lazerle basıldıysa) otomatik geri kaldır!
            if (_collidersInTrigger == 0 && _isPressed)
            {
                ReleaseButton();
            }
        }

        public void ReleaseButton()
        {
            if (!_isPressed) return;
            _isPressed = false;

            // Butonu fiziksel olarak yukarı çek (Yaylanma hissi)
            StopAllCoroutines();
            StartCoroutine(MoveButtonRoutine(_originalLocalPos.y));
            
            OnReleased?.Invoke();
        }

        /// <summary>
        /// Butonun aşağı veya yukarı doğru yumuşak (Smooth) bir şekilde kaymasını sağlayan Animasyon Motoru.
        /// </summary>
        private IEnumerator MoveButtonRoutine(float targetY)
        {
            if (_pushPart == null) yield break;

            float elapsed = 0f;
            Vector3 startPos = _pushPart.localPosition;
            Vector3 endPos = new Vector3(startPos.x, targetY, startPos.z);

            while (elapsed < _springSpeed)
            {
                elapsed += Time.deltaTime;
                // Lerp (Linear Interpolation) ile pürüzsüz hareket
                _pushPart.localPosition = Vector3.Lerp(startPos, endPos, elapsed / _springSpeed);
                yield return null;
            }

            _pushPart.localPosition = endPos;
        }

        /// <summary>
        /// Butonun seçilen türüne göre GameManager aksiyonlarını otomatik çalıştırır.
        /// </summary>
        private void ExecuteAction()
        {
            if (GameManager.Instance == null) return;

            switch (buttonType)
            {
                case ButtonType.Play:
                case ButtonType.Retry:
                    GameManager.Instance.PrepareToStart();
                    break;
                case ButtonType.Pause:
                    if (GameManager.Instance.CurrentState == GameState.Paused)
                        GameManager.Instance.ResumeGame();
                    else
                        GameManager.Instance.PauseGame();
                    break;
                case ButtonType.Settings:
                    // Sahnede panelleri atanmış olan UIManager'ı bul
                    RecycleRush.UI.UIManager targetManager = RecycleRush.UI.UIManager.Instance;
                    
                    if (targetManager == null || targetManager.settingsPanel == null)
                    {
                        RecycleRush.UI.UIManager[] managers = FindObjectsOfType<RecycleRush.UI.UIManager>();
                        foreach (var mgr in managers)
                        {
                            if (mgr.settingsPanel != null)
                            {
                                targetManager = mgr;
                                break;
                            }
                        }
                    }

                    if (targetManager != null)
                    {
                        targetManager.OpenSettingsPanel();
                    }
                    else
                    {
                        Debug.LogWarning("<color=red>[PhysicalVRButton]</color> Settings Panel atanmış bir UIManager bulunamadı!");
                    }
                    break;
                case ButtonType.Exit:
                    Debug.Log("<color=red>[PhysicalVRButton]</color> Exit butonuna basıldı. Oyundan/Editörden çıkılıyor...");
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                    break;
                case ButtonType.Custom:
                default:
                    // Sadece UnityEvent (OnPressed) çalışır
                    break;
            }
        }
    }
}
