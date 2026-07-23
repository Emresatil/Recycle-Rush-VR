using UnityEngine;
using TMPro; // TextMeshPro (Yazılar) için gerekli
using System.Collections; // Coroutine (Lerp animasyonları) için gerekli
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace RecycleRush.UI
{
    /// <summary>
    /// GameManager'ın durumlarını dinleyerek sahnede bulunan 3D Monitör (Ekran) üzerindeki yazıları ve butonları yönetir.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // Singleton Instance (Objeleri Destroy etmeden, panelleri dolu olana öncelik verir)
        public static UIManager Instance { get; private set; }

        [Header("Ekran (Monitör) Yazıları")]
        [Tooltip("Süreyi gösterecek olan yazı bileşeni (Örn: 60)")]
        public TextMeshProUGUI timeText;
        [Tooltip("Oyun durumunu gösterecek yazı (Örn: OYUN BITTI)")]
        public TextMeshProUGUI statusText;

        [Header("Butonlar")]
        [Tooltip("Oyun bitince çıkacak olan Fiziksel Restart Butonu objesi")]
        public GameObject restartButtonObj;

        [Header("Kombo Sistemi")]
        [Tooltip("Kombo yazısını gösterecek TextMeshPro bileşeni")]
        public TextMeshProUGUI comboText;
        
        [Header("Paneller ve Arayüz Kontrolleri")]
        [Tooltip("Ayarlar (Settings) Paneli")]
        public GameObject settingsPanel;
        [Tooltip("Duraklatma (Pause) Paneli")]
        public GameObject pausePanel;
        [Tooltip("Oyun içi UI Duraklatma (Pause) Butonu objesi")]
        public GameObject pauseButtonUIObj;
        
        [Tooltip("Müzik (BGM) seviyesi için Slider")]
        public Slider bgmSlider;
        [Tooltip("Ses Efektleri (SFX) seviyesi için Slider")]
        public Slider sfxSlider;

        [Header("VR Girdi (Input)")]
        [Tooltip("VR Menü/Geri tuşu (ESC) Input Action referansı")]
        public InputActionReference menuPauseAction;

        private Coroutine _comboAnimationCoroutine;

        private void Awake()
        {
            // Panelleri dolu olan UIManager'ı öncelikli olarak Instance kabul et (Hiçbir objeyi silmeden)
            if (Instance == null || settingsPanel != null)
            {
                Instance = this;
            }
        }

        private void OnEnable()
        {
            // Event'leri dinlemeye başla (Eventler statik olduğu için Instance beklemeden abone olabiliriz)
            GameManager.OnGameStateChanged += HandleGameState;
            GameManager.OnGameTimeUpdated += UpdateTimeDisplay;

            if (menuPauseAction != null && menuPauseAction.action != null)
            {
                menuPauseAction.action.Enable();
                menuPauseAction.action.performed += OnMenuButtonPressed;
            }
        }

        private void Start()
        {
            // Başlangıç durumunu hemen ekrana yansıt
            if (GameManager.Instance != null)
            {
                HandleGameState(GameManager.Instance.CurrentState);
            }
            
            // ScoreManager üzerinden kombo olaylarını dinlemeye başla (Awake sonrası olduğu için Instance hazırdır)
            if (Core.ScoreManager.Instance != null)
            {
                Core.ScoreManager.Instance.OnComboChanged += HandleComboChanged;
            }
            
            // Başlangıçta kombo yazısını gizle
            if (comboText != null)
            {
                comboText.gameObject.SetActive(false);
            }

            // Sliderları AudioManager'a bağla
            if (bgmSlider != null && AudioManager.Instance != null)
            {
                bgmSlider.onValueChanged.AddListener(AudioManager.Instance.SetBGMVolume);
                AudioManager.Instance.SetBGMVolume(bgmSlider.value);
            }
            if (sfxSlider != null && AudioManager.Instance != null)
            {
                sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
                AudioManager.Instance.SetSFXVolume(sfxSlider.value);
            }
            
            // Panelleri başlangıçta gizle
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(false);
        }

        private void Update()
        {
            // PC testi için klavyeden ESC tuşu (Yeni Input System kullanılarak)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleMenuPauseToggle();
            }
        }

        private void OnDisable()
        {
            // Bellek sızıntısını önlemek için dinlemeyi bırak
            GameManager.OnGameStateChanged -= HandleGameState;
            GameManager.OnGameTimeUpdated -= UpdateTimeDisplay;
            
            if (Core.ScoreManager.Instance != null)
            {
                Core.ScoreManager.Instance.OnComboChanged -= HandleComboChanged;
            }

            if (menuPauseAction != null && menuPauseAction.action != null)
            {
                menuPauseAction.action.performed -= OnMenuButtonPressed;
                menuPauseAction.action.Disable();
            }
        }

        /// <summary>
        /// Oyun durumu her değiştiğinde (MainMenu -> Playing -> GameOver) bu fonksiyon çalışır.
        /// </summary>
        private void HandleGameState(GameState state)
        {
            switch (state)
            {
                case GameState.Initialization:
                case GameState.MainMenu:
                    if (statusText != null) 
                    {
                        statusText.text = "SYSTEM ONLINE\n<color=yellow>PRESS PLAY BUTTON</color>";
                    }
                    if (timeText != null) timeText.text = "Time: 60";
                    
                    // Restart ve Pause butonlarını gizle
                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(false);
                    break;
                    
                case GameState.ReadyToStart:
                    if (statusText != null) 
                    {
                        if (PlayerPrefs.GetInt("TutorialDone", 0) == 0)
                            statusText.text = "<color=yellow>TUTORIAL</color>\nPULL THE LEVER TO START";
                        else
                            statusText.text = "SYSTEM READY\nPULL THE LEVER TO START";
                    }
                    if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(true); // Butona basılınca da Pause butonu görünsün!
                    break;
                    
                case GameState.Playing:
                    if (statusText != null) statusText.text = "RECYCLING STARTED";
                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    if (pausePanel != null) pausePanel.SetActive(false);
                    if (pauseButtonUIObj != null) 
                        pauseButtonUIObj.SetActive(true);
                    else
                        Debug.LogWarning("<color=red>[UIManager]</color> Pause Button UI Obj atanmamış (None)! Pause butonu görünmüyor olabilir.");
                    break;
                    
                case GameState.Countdown:
                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    if (pausePanel != null) pausePanel.SetActive(false);
                    if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(false);
                    StartCoroutine(StartCountdownAnimation());
                    break;
                    
                case GameState.Tutorial:
                    // TutorialManager yazıları kendisi yönetecek, burada sadece butonu gizliyoruz
                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    if (pausePanel != null) pausePanel.SetActive(false);
                    if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(false);
                    break;
                    
                case GameState.Paused:
                    if (statusText != null) statusText.text = "SYSTEM PAUSED";
                    if (pausePanel != null) pausePanel.SetActive(true);
                    if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(false);
                    break;

                case GameState.GameOver:
                    if (statusText != null) statusText.text = "<color=red>TIME'S UP!</color>\nCONVEYOR STOPPED";
                    
                    // Oyun bittiğinde Restart butonunu ortaya çıkar!
                    if (restartButtonObj != null) restartButtonObj.SetActive(true);
                    if (pausePanel != null) pausePanel.SetActive(false);
                    if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(false);
                    break;
            }
        }

        /// <summary>
        /// GameManager'dan saniye saniye gelen kalan süre bilgisini ekrana (timeText) yazar.
        /// </summary>
        private void UpdateTimeDisplay(float remainingTime)
        {
            if (timeText != null)
            {
                // Süreyi tam sayıya (Örn: 59.4 -> 60) yuvarlayarak başına 'Time:' ön ekiyle yazdır
                timeText.text = $"Time: {Mathf.CeilToInt(remainingTime)}";
                
                // Vurgu (Juice): Son 10 saniye kala yazıyı kırmızı yap!
                if (remainingTime <= 10f)
                {
                    timeText.color = Color.red;
                }
                else
                {
                    timeText.color = Color.white;
                }
            }
        }

        /// <summary>
        /// Kombo değiştiğinde tetiklenir ve Pop (Patlama) animasyonunu başlatır.
        /// </summary>
        private void HandleComboChanged(int comboCount, int multiplier)
        {
            if (comboText == null) return;

            if (multiplier > 1)
            {
                // Kombo varsa yazıyı aktif et ve metni ayarla
                comboText.gameObject.SetActive(true);
                comboText.text = $"{multiplier}x COMBO!";
                comboText.color = new Color(1f, 0.84f, 0f); // Altın Sarısı (Gold)

                // Varsa önceki animasyonu durdur ki çakışmasın
                if (_comboAnimationCoroutine != null)
                {
                    StopCoroutine(_comboAnimationCoroutine);
                }
                
                // Yeni Pop animasyonunu başlat
                _comboAnimationCoroutine = StartCoroutine(ComboPopAnimation());
            }
            else
            {
                // Katlayıcı yoksa (Kombo sıfırlandıysa) yazıyı gizle
                comboText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 3-2-1-BAŞLA şeklinde profesyonel, animasyonlu (Pop & Lerp) geri sayım yapar.
        /// </summary>
        private IEnumerator StartCountdownAnimation()
        {
            // Eğer Unity'de arayüz yazısı (statusText) atanmamışsa, oyunu kitlememek için direkt başlat
            if (statusText == null) 
            {
                Debug.LogWarning("<color=orange>[UIManager]</color> statusText atanmamış! Geri sayım atlanıp oyun başlatılıyor.");
                // C# Event çakışmasını önlemek için (Reentrancy Bug) 1 frame bekleyip öyle başlatıyoruz
                yield return null; 
                if (GameManager.Instance != null) GameManager.Instance.FinishCountdown();
                yield break;
            }

            string[] countTexts = { "<color=yellow>3</color>", "<color=orange>2</color>", "<color=red>1</color>", "<color=green>GO!</color>" };
            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = originalScale * 2f; // %100 büyüt (Daha vurucu bir etki için)

            foreach (string text in countTexts)
            {
                statusText.text = text;
                
                // TODO: AudioManager üzerinden "Bip" sesi çaldırma buraya eklenecek
                
                // Büyüme (Scale Up) - Hızlıca patlama efekti (Pop)
                float elapsed = 0f;
                float duration = 0.15f;
                while (elapsed < duration)
                {
                    statusText.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                statusText.transform.localScale = targetScale;

                // Küçülme (Scale Down) - Yavaşça eski haline dönme ve bekleme
                elapsed = 0f;
                duration = 0.85f;
                while (elapsed < duration)
                {
                    statusText.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                statusText.transform.localScale = originalScale;
            }

            // Geri sayım bitti, yazıyı temizle ve oyunu asıl şimdi başlat!
            statusText.text = ""; 
            if (GameManager.Instance != null)
            {
                GameManager.Instance.FinishCountdown();
            }
        }

        /// <summary>
        /// Yazıyı bir anda büyütüp sonra yavaşça normal boyutuna indiren (Lerp) Juice animasyonu.
        /// </summary>
        private IEnumerator ComboPopAnimation()
        {
            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = originalScale * 1.5f; // %50 büyüt
            
            float duration = 0.15f; // Büyüme süresi
            float elapsed = 0f;

            // Büyüme (Scale Up)
            while (elapsed < duration)
            {
                comboText.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            comboText.transform.localScale = targetScale;
            
            elapsed = 0f;
            duration = 0.25f; // Küçülme süresi (Daha yumuşak)
            
            // Küçülme (Scale Down)
            while (elapsed < duration)
            {
                comboText.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            comboText.transform.localScale = originalScale;
            
            // Oyuncunun yazıyı okuyabilmesi için 1 saniye bekle
            yield return new WaitForSeconds(1.0f);
            
            // Ekranda sürekli kalmaması için yazıyı gizle
            comboText.gameObject.SetActive(false);
            
            _comboAnimationCoroutine = null;
        }

        // --- YENİ EKLENEN PANEL VE MENÜ KONTROL METOTLARI ---

        private void OnMenuButtonPressed(InputAction.CallbackContext context)
        {
            HandleMenuPauseToggle();
        }

        private void HandleMenuPauseToggle()
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettingsPanel();
                return;
            }

            if (GameManager.Instance != null && 
                (GameManager.Instance.CurrentState == GameState.Playing || GameManager.Instance.CurrentState == GameState.Paused))
            {
                GameManager.Instance.TogglePauseGame();
            }
        }

        public void OpenSettingsPanel()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning("<color=red>[UIManager]</color> Settings Panel açılmaya çalışıldı ancak Inspector'da 'Settings Panel' değişkeni ATANMAMIŞ (None)! Lütfen UIManager bileşenindeki boşluğa paneli sürükleyin.");
            }
        }

        public void CloseSettingsPanel()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void PauseGameUI()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseGame();
            }
        }

        public void ResumeGameUI()
        {
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        }

        public void QuitApplication()
        {
            Debug.Log("[UIManager] Uygulamadan Çıkılıyor... (Exit Butonu Tetiklendi)");
            Application.Quit();
        }
    }
}
