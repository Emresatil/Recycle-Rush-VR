using UnityEngine;
using TMPro; // TextMeshPro (Yazılar) için gerekli
using System.Collections; // Coroutine (Lerp animasyonları) için gerekli
using System.Collections.Generic; // Queue<T> için gerekli
using UnityEngine.UI;
using UnityEngine.InputSystem;
using RecycleRush.Managers;

namespace RecycleRush.UI
{
    /// <summary>
    /// GameManager ve EventManager durumlarını dinleyerek sahnede bulunan Monitör/UI arayüzlerini ve panelleri yönetir.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // Singleton Instance (Objeleri Destroy etmeden, panelleri dolu olana öncelik verir)
        public static UIManager Instance { get; private set; }

        [Header("Ekran (Monitör) Yazıları")]
        [Tooltip("Süreyi gösterecek olan yazı bileşeni (Örn: 60)")]
        public TextMeshProUGUI timeText;
        [Tooltip("Oyun durumunu gösterecek yazı (Örn: RECYCLING STARTED / SYSTEM ONLINE)")]
        public TextMeshProUGUI statusText;

        [Header("Butonlar")]
        [Tooltip("Oyun bitince çıkacak olan Fiziksel Restart Butonu objesi")]
        public GameObject restartButtonObj;

        [Header("Kombo Sistemi")]
        [Tooltip("Kombo yazısını gösterecek TextMeshPro bileşeni")]
        public TextMeshProUGUI comboText;

        [Header("Etkinlik ve Power-Up Bildirimleri")]
        [Tooltip("Aktif Etkinliği (Frenzy vb.) gösterecek yazı")]
        public TextMeshProUGUI eventNotificationText;
        [Tooltip("Aktif Power-Up'ı (Magnet, Hourglass) gösterecek yazı")]
        public TextMeshProUGUI powerupNotificationText;

        [Header("Paneller ve Arayüz Kontrolleri")]
        [Tooltip("Ayarlar (Settings) Paneli")]
        public GameObject settingsPanel;
        [Tooltip("Duraklatma (Pause) Paneli")]
        public GameObject pausePanel;
        [Tooltip("Oyun Bitti (GameOver) Paneli")]
        public GameObject gameOverPanel;
        [Tooltip("Oyun Bittiğinde son skoru gösterecek TextMeshProUGUI bileşeni")]
        public TextMeshProUGUI gameOverFinalScoreText;
        [Tooltip("Oyun içi UI Duraklatma (Pause) Butonu objesi")]
        public GameObject pauseButtonUIObj;

        [Header("Oyun İçi ve Menü Panelleri")]
        [Tooltip("Oyun başında aktif olan Seviye Seçim Panosu")]
        public GameObject levelSelectionBoard;
        [Tooltip("Oyun başladığında açılacak Görev Paneli (ActiveMissionUI)")]
        public GameObject missionPanel;
        [Tooltip("Oyun başladığında açılacak XP Bar Paneli")]
        public GameObject xpPanel;
        [Tooltip("Oyun başladığında açılacak Kombo Paneli")]
        public GameObject comboPanel;
        [Tooltip("Oyun başladığında açılacak Kirlilik (Pollution) Paneli")]
        public GameObject pollutionPanel;
        [Tooltip("Oyun başladığında açılacak Skor Paneli / Texti")]
        public GameObject scorePanel;

        [Header("Ses Ayarları (Slider)")]
        [Tooltip("Müzik (BGM) seviyesi için Slider")]
        public Slider bgmSlider;
        [Tooltip("Ses Efektleri (SFX) seviyesi için Slider")]
        public Slider sfxSlider;

        [Header("VR Girdi (Input)")]
        [Tooltip("VR Menü/Geri tuşu (ESC) Input Action referansı")]
        public InputActionReference menuPauseAction;

        [Header("Achievement UI (Başarım Sistemi)")]
        [Tooltip("Başarım bildirim paneli (Canvas'ta yukardan inen Toast Message)")]
        public GameObject achievementPanel;
        [Tooltip("Başarımın başlığını gösterecek TextMeshProUGUI")]
        public TextMeshProUGUI achievementTitleText;
        [Tooltip("Başarımın açıklamasını gösterecek TextMeshProUGUI")]
        public TextMeshProUGUI achievementDescText;

        private Coroutine _comboAnimationCoroutine;
        private Coroutine _countdownCoroutine;

        // Başarım kuyruğu (Aynı anda birden fazla açılırsa sırayla göster)
        private Queue<Managers.AchievementData> _achievementQueue = new Queue<Managers.AchievementData>();
        private bool _isShowingAchievement = false;

        private Vector3 _originalStatusScale = Vector3.one;
        private Vector3 _originalComboScale = Vector3.one;

        private void Awake()
        {
            // Panelleri dolu olan UIManager'ı öncelikli olarak Instance kabul et (Hiçbir objeyi silmeden)
            if (Instance == null || settingsPanel != null)
            {
                Instance = this;
            }

            if (statusText != null)
            {
                _originalStatusScale = statusText.transform.localScale;
            }
            if (comboText != null)
            {
                _originalComboScale = comboText.transform.localScale;
            }
        }

        private void OnEnable()
        {
            // Event'leri dinlemeye başla
            GameManager.OnGameStateChanged += HandleGameState;
            GameManager.OnMagnetStarted += HandleMagnetStarted;
            GameManager.OnMagnetTimeUpdated += HandleMagnetTimeUpdated;
            GameManager.OnMagnetEnded += HandleMagnetEnded;
            GameManager.OnHourglassUsed += HandleHourglassUsed;
            GameManager.OnGameTimeUpdated += UpdateTimeDisplay;

            // Etkinlik yöneticisi (Frenzy vb.) dinleyicisi
            EventManager.OnGameEventStarted += HandleGameEventStarted;
            EventManager.OnGameEventEnded += HandleGameEventEnded;

            if (menuPauseAction != null && menuPauseAction.action != null)
            {
                menuPauseAction.action.Enable();
                menuPauseAction.action.performed += OnMenuButtonPressed;
            }
        }

        private void Start()
        {
            // Başarım Panelini başlangıçta gizle
            if (achievementPanel != null)
            {
                achievementPanel.SetActive(false);
            }

            // Başlangıçta panelleri ve metinleri başlangıç durumuna (MainMenu) ayarla
            if (GameManager.Instance != null)
            {
                HandleGameState(GameManager.Instance.CurrentState);
            }
            else
            {
                HandleGameState(GameState.MainMenu);
            }

            BinTrigger.OnComboChanged += HandleComboChanged;
            if (Managers.ComboManager.Instance != null)
            {
                Managers.ComboManager.OnComboChanged += HandleComboChangedLegacy;
                Managers.ComboManager.OnComboBroken += HandleComboBroken;
            }

            // Başarım yöneticisine bağlan
            if (Managers.AchievementManager.Instance != null)
            {
                Managers.AchievementManager.OnAchievementUnlocked += HandleAchievementUnlocked;
                Managers.AchievementManager.OnAchievementProgress += HandleAchievementProgress;
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
            if (pausePanel != null)
            {
                SetupPauseButtons();
                pausePanel.SetActive(false);
            }
            if (gameOverPanel != null)
            {
                SetupGameOverButtons();
                gameOverPanel.SetActive(false);
            }
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
            GameManager.OnMagnetStarted -= HandleMagnetStarted;
            GameManager.OnMagnetTimeUpdated -= HandleMagnetTimeUpdated;
            GameManager.OnMagnetEnded -= HandleMagnetEnded;
            GameManager.OnHourglassUsed -= HandleHourglassUsed;
            GameManager.OnGameTimeUpdated -= UpdateTimeDisplay;

            EventManager.OnGameEventStarted -= HandleGameEventStarted;
            EventManager.OnGameEventEnded -= HandleGameEventEnded;

            BinTrigger.OnComboChanged -= HandleComboChanged;
            if (Managers.ComboManager.Instance != null)
            {
                Managers.ComboManager.OnComboChanged -= HandleComboChangedLegacy;
                Managers.ComboManager.OnComboBroken -= HandleComboBroken;
            }

            if (menuPauseAction != null && menuPauseAction.action != null)
            {
                menuPauseAction.action.performed -= OnMenuButtonPressed;
                menuPauseAction.action.Disable();
            }

            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            if (Managers.AchievementManager.Instance != null)
            {
                Managers.AchievementManager.OnAchievementUnlocked -= HandleAchievementUnlocked;
                Managers.AchievementManager.OnAchievementProgress -= HandleAchievementProgress;
            }
        }

        #region Power-Up & Event UI Handlers

        private void HandleGameEventStarted(GameEventType eventType)
        {
            if (eventNotificationText != null)
            {
                eventNotificationText.gameObject.SetActive(true);
                eventNotificationText.text = $"EVENT: {eventType.ToString().ToUpper()}!";
                eventNotificationText.color = Color.magenta;
            }
        }

        private void HandleGameEventEnded()
        {
            if (eventNotificationText != null)
            {
                eventNotificationText.gameObject.SetActive(false);
            }
        }

        private void HandleMagnetStarted(float duration)
        {
            if (powerupNotificationText != null)
            {
                powerupNotificationText.gameObject.SetActive(true);
                powerupNotificationText.color = Color.cyan;
                powerupNotificationText.text = $"MAGNET ACTIVE: {Mathf.CeilToInt(duration)}s";
            }
        }

        private void HandleMagnetTimeUpdated(float remainingTime)
        {
            if (powerupNotificationText != null && GameManager.Instance != null && GameManager.Instance.IsMagnetActive)
            {
                powerupNotificationText.text = $"MAGNET ACTIVE: {Mathf.CeilToInt(remainingTime)}s";
            }
        }

        private void HandleMagnetEnded()
        {
            if (powerupNotificationText != null)
            {
                powerupNotificationText.gameObject.SetActive(false);
            }
        }

        private void HandleHourglassUsed(float timeAdded)
        {
            if (powerupNotificationText != null)
            {
                StartCoroutine(ShowHourglassNotification(timeAdded));
            }
        }

        private System.Collections.IEnumerator ShowHourglassNotification(float timeAdded)
        {
            bool wasMagnetActive = GameManager.Instance != null && GameManager.Instance.IsMagnetActive;

            powerupNotificationText.gameObject.SetActive(true);
            powerupNotificationText.color = Color.green;
            powerupNotificationText.text = $"+{timeAdded} SECONDS!";

            yield return new WaitForSeconds(2f);

            if (wasMagnetActive && GameManager.Instance != null && GameManager.Instance.IsMagnetActive)
            {
                HandleMagnetTimeUpdated(GameManager.Instance.MagnetRemainingTime);
                powerupNotificationText.color = Color.cyan;
            }
            else
            {
                powerupNotificationText.gameObject.SetActive(false);
            }
        }

        #endregion

        /// <summary>
        /// Oyun durumu her değiştiğinde (MainMenu -> Playing -> GameOver) bu fonksiyon çalışır.
        /// </summary>
        public void HandleGameState(GameState state)
        {
            switch (state)
            {
                case GameState.Initialization:
                case GameState.MainMenu:
                    if (statusText != null)
                    {
                        statusText.text = "SYSTEM ONLINE\n<color=yellow>PRESS PLAY BUTTON</color>";
                        statusText.transform.localScale = _originalStatusScale;
                    }
                    if (timeText != null)
                    {
                        timeText.text = "Time: 60";
                        timeText.gameObject.SetActive(false);
                    }

                    // Bildirim metinlerini ve butonları gizle
                    if (eventNotificationText != null) eventNotificationText.gameObject.SetActive(false);
                    if (powerupNotificationText != null) powerupNotificationText.gameObject.SetActive(false);
                    if (comboText != null) comboText.gameObject.SetActive(false);
                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(false);
                    if (pausePanel != null) pausePanel.SetActive(false);
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);

                    // SADECE LevelSelectionBoard aktif olsun, oyun içi paneller kapansın
                    if (levelSelectionBoard != null) levelSelectionBoard.SetActive(true);
                    if (missionPanel != null) missionPanel.SetActive(false);
                    if (xpPanel != null) xpPanel.SetActive(false);
                    if (comboPanel != null) comboPanel.SetActive(false);
                    if (pollutionPanel != null) pollutionPanel.SetActive(false);
                    if (scorePanel != null) scorePanel.SetActive(false);
                    break;

                case GameState.Placement:
                    if (statusText != null)
                    {
                        if (PlayerPrefs.GetInt("TutorialDone", 0) == 0)
                            statusText.text = "<color=yellow>TUTORIAL</color>\nPULL THE LEVER TO START";
                        else
                            statusText.text = "SYSTEM READY\nPULL THE LEVER TO START";
                        statusText.transform.localScale = _originalStatusScale;
                    }
                    if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(true);
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);

                    if (levelSelectionBoard != null) levelSelectionBoard.SetActive(true);
                    if (missionPanel != null) missionPanel.SetActive(false);
                    if (xpPanel != null) xpPanel.SetActive(false);
                    if (comboPanel != null) comboPanel.SetActive(false);
                    if (pollutionPanel != null) pollutionPanel.SetActive(false);
                    if (scorePanel != null) scorePanel.SetActive(false);
                    break;

                case GameState.Countdown:
                    if (levelSelectionBoard != null) levelSelectionBoard.SetActive(false);
                    if (missionPanel != null) missionPanel.SetActive(false);
                    if (xpPanel != null) xpPanel.SetActive(false);
                    if (comboPanel != null) comboPanel.SetActive(false);
                    if (pollutionPanel != null) pollutionPanel.SetActive(false);
                    if (scorePanel != null) scorePanel.SetActive(false);

                    if (timeText != null) timeText.gameObject.SetActive(false);
                    if (eventNotificationText != null) eventNotificationText.gameObject.SetActive(false);
                    if (powerupNotificationText != null) powerupNotificationText.gameObject.SetActive(false);
                    if (comboText != null) comboText.gameObject.SetActive(false);

                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    if (pausePanel != null) pausePanel.SetActive(false);
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    if (settingsPanel != null) settingsPanel.SetActive(false);
                    if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(false);
                    if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
                    _countdownCoroutine = StartCoroutine(StartCountdownAnimation());
                    break;

                case GameState.Playing:
                    // Seviye seçim panosunu gizle, Oyun içi HUD panellerini aç
                    if (levelSelectionBoard != null) levelSelectionBoard.SetActive(false);
                    if (missionPanel != null) missionPanel.SetActive(true);
                    if (xpPanel != null) xpPanel.SetActive(true);
                    if (comboPanel != null) comboPanel.SetActive(true);
                    if (pollutionPanel != null) pollutionPanel.SetActive(true);
                    if (scorePanel != null) scorePanel.SetActive(true);

                    if (timeText != null) timeText.gameObject.SetActive(true);
                    if (statusText != null)
                    {
                        statusText.text = "RECYCLING STARTED";
                        statusText.transform.localScale = _originalStatusScale;
                        StartCoroutine(ClearStatusTextAfterDelay(2f));
                    }
                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    if (pausePanel != null) pausePanel.SetActive(false);
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    if (pauseButtonUIObj != null)
                        pauseButtonUIObj.SetActive(true);
                    break;

                case GameState.Tutorial:
                    if (levelSelectionBoard != null) levelSelectionBoard.SetActive(false);
                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    if (pausePanel != null) pausePanel.SetActive(false);
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(false);
                    break;

                case GameState.Paused:
                    if (statusText != null)
                    {
                        statusText.text = "SYSTEM PAUSED";
                        statusText.transform.localScale = _originalStatusScale;
                    }
                    if (levelSelectionBoard != null) levelSelectionBoard.SetActive(false);
                    if (pausePanel != null)
                    {
                        SetupPauseButtons();
                        pausePanel.SetActive(true);
                    }
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(false);
                    break;

                case GameState.GameOver:
                    if (levelSelectionBoard != null) levelSelectionBoard.SetActive(false);
                    if (statusText != null)
                    {
                        statusText.text = "<color=red>TIME'S UP!</color>\nRECYCLING STOPPED";
                        statusText.transform.localScale = _originalStatusScale;
                    }

                    // Oyun bittiğinde GameOver panelini aç ve butonları dinamik olarak bağla
                    if (gameOverPanel != null)
                    {
                        SetupGameOverButtons();
                        gameOverPanel.SetActive(true);
                    }
                    if (gameOverFinalScoreText != null && Core.ScoreManager.Instance != null)
                    {
                        gameOverFinalScoreText.text = $"FINAL SCORE: {Core.ScoreManager.Instance.CurrentScore}";
                    }

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

        #region Combo Animations

        private void HandleComboChanged(int combo)
        {
            if (comboText == null) return;
            if (combo > 1)
            {
                comboText.gameObject.SetActive(true);
                comboText.text = $"{combo}x COMBO!";
                comboText.color = new Color(1f, 0.84f, 0f);
                if (_comboAnimationCoroutine != null) StopCoroutine(_comboAnimationCoroutine);
                _comboAnimationCoroutine = StartCoroutine(ComboPopAnimation());
            }
            else if (combo == 0)
            {
                comboText.gameObject.SetActive(false);
            }
        }

        private void HandleComboChangedLegacy(int comboCount, int multiplier, bool isRankUp)
        {
            if (comboText == null) return;

            if (comboCount == 0)
            {
                if (!comboText.text.Contains("BROKEN"))
                {
                    comboText.gameObject.SetActive(false);
                }
                return;
            }

            if (isRankUp)
            {
                comboText.gameObject.SetActive(true);
                comboText.text = $"{multiplier}x COMBO!";
                comboText.color = new Color(1f, 0.84f, 0f);

                if (_comboAnimationCoroutine != null) StopCoroutine(_comboAnimationCoroutine);
                _comboAnimationCoroutine = StartCoroutine(ComboPopAnimation());
            }
        }

        private void HandleComboBroken()
        {
            if (comboText == null) return;

            comboText.gameObject.SetActive(true);
            comboText.color = Color.red;
            comboText.text = "COMBO BROKEN!";

            if (_comboAnimationCoroutine != null) StopCoroutine(_comboAnimationCoroutine);
            _comboAnimationCoroutine = StartCoroutine(ComboPopAnimation());
        }

        private IEnumerator ComboPopAnimation()
        {
            Vector3 originalScale = _originalComboScale;
            Vector3 targetScale = originalScale * 1.3f; // %30 pop juice

            float duration = 0.15f; // Büyüme süresi
            float elapsed = 0f;

            // Büyüme (Scale Up)
            while (elapsed < duration)
            {
                if (comboText != null) comboText.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (comboText != null) comboText.transform.localScale = targetScale;

            elapsed = 0f;
            duration = 0.25f; // Küçülme süresi

            // Küçülme (Scale Down)
            while (elapsed < duration)
            {
                if (comboText != null) comboText.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (comboText != null) comboText.transform.localScale = originalScale;
            yield return new WaitForSeconds(1.5f);

            if (comboText != null)
            {
                comboText.gameObject.SetActive(false);
            }

            _comboAnimationCoroutine = null;
        }

        #endregion

        /// <summary>
        /// 3-2-1-GO animasyonlu geri sayım yapar.
        /// </summary>
        private IEnumerator StartCountdownAnimation()
        {
            if (statusText == null)
            {
                Debug.LogWarning("<color=orange>[UIManager]</color> statusText atanmamış! Geri sayım atlanıp oyun başlatılıyor.");
                yield return null;
                _countdownCoroutine = null;
                if (GameManager.Instance != null) GameManager.Instance.FinishCountdown();
                yield break;
            }

            string[] countTexts = { "<color=yellow>3</color>", "<color=orange>2</color>", "<color=red>1</color>", "<color=green>GO!</color>" };
            Vector3 originalScale = _originalStatusScale;
            Vector3 targetScale = originalScale * 1.3f;

            foreach (string text in countTexts)
            {
                if (statusText == null) yield break;
                statusText.text = text;

                float elapsed = 0f;
                float duration = 0.15f;
                while (elapsed < duration)
                {
                    if (statusText == null) yield break;
                    statusText.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                if (statusText == null) yield break;
                statusText.transform.localScale = targetScale;

                elapsed = 0f;
                duration = 0.85f;
                while (elapsed < duration)
                {
                    if (statusText == null) yield break;
                    statusText.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                if (statusText == null) yield break;
                statusText.transform.localScale = originalScale;
            }

            if (statusText != null)
            {
                statusText.text = "";
                statusText.transform.localScale = _originalStatusScale;
            }
            _countdownCoroutine = null;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.FinishCountdown();
            }
        }

        private IEnumerator ClearStatusTextAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                if (statusText != null)
                {
                    statusText.text = "";
                    statusText.transform.localScale = _originalStatusScale;
                }
            }
        }

        #region Menu & Button Handlers

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

        #region Button Auto-Binding & Setup

        public void SetupGameOverButtons()
        {
            if (gameOverPanel == null) return;

            // Görev tamamlandı mı veya bölüm geçildi mi kontrol et
            bool isMissionCompleted = false;
            if (MissionManager.Instance != null && MissionManager.Instance.ActiveMission != null)
            {
                isMissionCompleted = MissionManager.Instance.ActiveMission.IsCompleted;
            }
            else if (LevelSelectionManager.Instance != null)
            {
                int currentStage = LevelSelectionManager.Instance.CurrentPlayingLevelId;
                LevelData data = LevelSelectionManager.Instance.GetLevelData(currentStage);
                if (data != null && data.StarsEarned > 0)
                {
                    isMissionCompleted = true;
                }
            }

            UnityEngine.UI.Button[] buttons = gameOverPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var btn in buttons)
            {
                string btnName = btn.name.ToLower();
                TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                Text uiText = btn.GetComponentInChildren<Text>(true);
                string label = (tmp != null ? tmp.text : (uiText != null ? uiText.text : "")).ToLower();

                if (btnName.Contains("next") || label.Contains("next") || label.Contains("sonraki") || label.Contains("ileri"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(StartNextLevelUI);
                    btn.interactable = isMissionCompleted;
                    btn.gameObject.SetActive(isMissionCompleted); // Görev bitmediyse Next Level görünmesin / tıklanamasın
                    Debug.Log($"<color=green>[UIManager]</color> GameOver Panel: '{btn.name}' -> StartNextLevelUI bağlandı (Aktif: {isMissionCompleted})");
                }
                else if (btnName.Contains("restart") || btnName.Contains("retry") || label.Contains("restart") || label.Contains("tekrar") || label.Contains("yeniden"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(RestartGameUI);
                    btn.interactable = true;
                    btn.gameObject.SetActive(true);
                    Debug.Log($"<color=green>[UIManager]</color> GameOver Panel: '{btn.name}' -> RestartGameUI bağlandı!");
                }
                else if (btnName.Contains("exit") || btnName.Contains("quit") || label.Contains("exit") || label.Contains("quit") || label.Contains("çıkış"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(QuitApplication);
                    btn.interactable = true;
                    btn.gameObject.SetActive(true);
                    Debug.Log($"<color=green>[UIManager]</color> GameOver Panel: '{btn.name}' -> QuitApplication bağlandı!");
                }
            }
        }

        public void SetupPauseButtons()
        {
            if (pausePanel == null) return;

            UnityEngine.UI.Button[] buttons = pausePanel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var btn in buttons)
            {
                string btnName = btn.name.ToLower();
                TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                Text uiText = btn.GetComponentInChildren<Text>(true);
                string label = (tmp != null ? tmp.text : (uiText != null ? uiText.text : "")).ToLower();

                if (btnName.Contains("resume") || btnName.Contains("continue") || label.Contains("resume") || label.Contains("devam"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ResumeGameUI);
                    Debug.Log($"<color=green>[UIManager]</color> Pause Panel: '{btn.name}' -> ResumeGameUI bağlandı!");
                }
                else if (btnName.Contains("restart") || btnName.Contains("retry") || label.Contains("restart") || label.Contains("tekrar"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.gameObject.SetActive(false); // Pause panelindeki restart butonunu tamamen kaldır
                    Debug.Log($"<color=green>[UIManager]</color> Pause Panel: '{btn.name}' -> Restart butonu kaldırıldı ve gizlendi.");
                }
                else if (btnName.Contains("settings") || label.Contains("settings") || label.Contains("ayar"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(OpenSettingsPanel);
                    Debug.Log($"<color=green>[UIManager]</color> Pause Panel: '{btn.name}' -> OpenSettingsPanel bağlandı!");
                }
                else if (btnName.Contains("exit") || btnName.Contains("quit") || label.Contains("exit") || label.Contains("çıkış"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(QuitApplication);
                    Debug.Log($"<color=green>[UIManager]</color> Pause Panel: '{btn.name}' -> QuitApplication bağlandı!");
                }
            }
        }

        #endregion

        public void OpenSettingsPanel()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
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

        public void RestartGameUI()
        {
            Debug.Log("<color=green>[UIManager]</color> RestartGameUI tetiklendi!");

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (timeText != null) timeText.gameObject.SetActive(false);
            if (eventNotificationText != null) eventNotificationText.gameObject.SetActive(false);
            if (powerupNotificationText != null) powerupNotificationText.gameObject.SetActive(false);
            if (comboText != null) comboText.gameObject.SetActive(false);

            Time.timeScale = 1f;

            if (Core.ScoreManager.Instance != null)
            {
                Core.ScoreManager.Instance.ResetScore();
            }

            if (LevelSelectionManager.Instance != null)
            {
                int currentLvl = LevelSelectionManager.Instance.CurrentPlayingLevelId;
                LevelSelectionManager.Instance.StartLevel(currentLvl);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PrepareToStart();
            }
        }

        public void StartNextLevelUI()
        {
            Debug.Log("<color=green>[UIManager]</color> StartNextLevelUI tetiklendi!");

            if (LevelSelectionManager.Instance != null)
            {
                int nextLevelId = LevelSelectionManager.Instance.CurrentPlayingLevelId + 1;
                LevelData nextLevel = LevelSelectionManager.Instance.GetLevelData(nextLevelId);

                if (nextLevel != null && nextLevel.IsUnlocked)
                {
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    if (pausePanel != null) pausePanel.SetActive(false);

                    if (Core.ScoreManager.Instance != null)
                    {
                        Core.ScoreManager.Instance.ResetScore();
                    }

                    LevelSelectionManager.Instance.StartLevel(nextLevelId);

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.PrepareToStart();
                    }
                }
                else
                {
                    Debug.LogWarning($"<color=orange>[UIManager]</color> Sonraki seviye ({nextLevelId}) kilitli veya bulunamadı!");
                }
            }
        }

        public void ResumeGameUI()
        {
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        }

        public void QuitApplication()
        {
            Debug.Log("[UIManager] Uygulamadan Çıkılıyor...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Achievement UI

        private void HandleAchievementUnlocked(Managers.AchievementData data)
        {
            _achievementQueue.Enqueue(data);
            if (!_isShowingAchievement)
            {
                StartCoroutine(DisplayAchievementRoutine());
            }
        }

        private void HandleAchievementProgress(Managers.AchievementData data, float progress)
        {
            // İsteğe bağlı ilerleme bildirimleri
        }

        private IEnumerator DisplayAchievementRoutine()
        {
            _isShowingAchievement = true;
            while (_achievementQueue.Count > 0)
            {
                var data = _achievementQueue.Dequeue();
                if (achievementPanel != null)
                {
                    if (achievementTitleText != null) achievementTitleText.text = data.Title;
                    if (achievementDescText != null) achievementDescText.text = data.Description;

                    achievementPanel.SetActive(true);
                    yield return new WaitForSeconds(3.5f);
                    achievementPanel.SetActive(false);
                    yield return new WaitForSeconds(0.5f);
                }
            }
            _isShowingAchievement = false;
        }

        #endregion
    }
}