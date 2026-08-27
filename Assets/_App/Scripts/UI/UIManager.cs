using UnityEngine;
using TMPro; // TextMeshPro (YazÄ±lar) iÃ§in gerekli
using System.Collections; // Coroutine (Lerp animasyonlarÄ±) iÃ§in gerekli
using UnityEngine.UI;
using UnityEngine.InputSystem;
using RecycleRush.Managers; // EKLENDÄ°: MissionManager, LevelSelectionManager gibi yÃ¶neticilere eriÅŸmek iÃ§in

namespace RecycleRush.UI
{
    /// <summary>
    /// GameManager ve EventManager durumlarını dinleyerek sahnede bulunan Monitör/UI arayüzlerini ve panelleri yönetir.
    /// GameManager'Ä±n durumlarÄ±nÄ± dinleyerek sahnede bulunan 3D MonitÃ¶r (Ekran) Ã¼zerindeki yazÄ±larÄ± ve butonlarÄ± yÃ¶netir.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // Singleton Instance (Objeleri Destroy etmeden, panelleri dolu olana Ã¶ncelik verir)
        public static UIManager Instance { get; private set; }

        [Header("Ekran (MonitÃ¶r) YazÄ±larÄ±")]
        [Tooltip("SÃ¼reyi gÃ¶sterecek olan yazÄ± bileÅŸeni (Ã–rn: 60)")]
        public TextMeshProUGUI timeText;
        [Tooltip("Oyun durumunu gösterecek yazı (Örn: RECYCLING STARTED / SYSTEM ONLINE)")]
        [Tooltip("Oyun durumunu gÃ¶sterecek yazÄ± (Ã–rn: OYUN BITTI)")]
        public TextMeshProUGUI statusText;

        [Header("Butonlar")]
        [Tooltip("Oyun bitince Ã§Ä±kacak olan Fiziksel Restart Butonu objesi")]
        public GameObject restartButtonObj;

        [Header("Kombo Sistemi")]
        [Tooltip("Kombo yazÄ±sÄ±nÄ± gÃ¶sterecek TextMeshPro bileÅŸeni")]
        public TextMeshProUGUI comboText;

        [Header("Etkinlik ve Power-Up Bildirimleri")]
        [Tooltip("Aktif Etkinliği (Frenzy vb.) gösterecek yazı")]
        public TextMeshProUGUI eventNotificationText;
        [Tooltip("Aktif Power-Up'ı (Magnet, Hourglass) gösterecek yazı")]
        public TextMeshProUGUI powerupNotificationText;

        [Header("Paneller ve Arayüz Kontrolleri")]
        
        [Header("Etkinlik ve Power-Up Bildirimleri")]
        [Tooltip("Aktif EtkinliÄŸi (Frenzy vb.) gÃ¶sterecek yazÄ±")]
        public TextMeshProUGUI eventNotificationText;
        [Tooltip("Aktif Power-Up'Ä± (Magnet, Hourglass) gÃ¶sterecek yazÄ±")]
        public TextMeshProUGUI powerupNotificationText;
        
        [Header("Paneller ve ArayÃ¼z Kontrolleri")]
        [Tooltip("Ayarlar (Settings) Paneli")]
        public GameObject settingsPanel;
        [Tooltip("Duraklatma (Pause) Paneli")]
        public GameObject pausePanel;
        [Tooltip("Oyun Bitti (GameOver) Paneli")]
        public GameObject gameOverPanel;
        [Tooltip("Oyun BittiÄŸinde son skoru gÃ¶sterecek TextMeshProUGUI bileÅŸeni")]
        public TextMeshProUGUI gameOverFinalScoreText;
        [Tooltip("Oyun iÃ§i UI Duraklatma (Pause) Butonu objesi")]
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
        
        [Header("Oyun Ä°Ã§i ve MenÃ¼ Panelleri")]
        public GameObject levelSelectionBoard;
        public GameObject missionPanel;
        public GameObject xpPanel;
        public GameObject comboPanel;
        
        [Header("AR GÃ¼venlik ArayÃ¼zÃ¼")]
        [Tooltip("OdanÄ±n Ä±ÅŸÄ±ÄŸÄ± kapandÄ±ÄŸÄ±nda veya AR takip bozulduÄŸunda Ã§Ä±kacak 'OdayÄ± AydÄ±nlatÄ±n' paneli")]
        public GameObject safetyWarningPanel;

        [Tooltip("MÃ¼zik (BGM) seviyesi iÃ§in Slider")]
        public Slider bgmSlider;
        [Tooltip("Ses Efektleri (SFX) seviyesi iÃ§in Slider")]
        public Slider sfxSlider;

        [Header("VR Girdi (Input)")]
        [Tooltip("VR MenÃ¼/Geri tuÅŸu (ESC) Input Action referansÄ±")]
        public InputActionReference menuPauseAction;

        private Coroutine _comboAnimationCoroutine;
        private Coroutine _countdownCoroutine;

        // Başarım kuyruğu (Aynı anda birden fazla açılırsa sırayla göster)
        private Queue<Managers.AchievementData> _achievementQueue = new Queue<Managers.AchievementData>();
        private bool _isShowingAchievement = false;

        private Vector3 _originalStatusScale = Vector3.one;
        private Vector3 _originalComboScale = Vector3.one;

        private void Awake()
        {
            // Panelleri dolu olan UIManager'Ä± Ã¶ncelikli olarak Instance kabul et (HiÃ§bir objeyi silmeden)
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
            // Event'leri dinlemeye baÅŸla
            GameManager.OnGameStateChanged += HandleGameState;
            GameManager.OnMagnetStarted += HandleMagnetStarted;
            GameManager.OnMagnetTimeUpdated += HandleMagnetTimeUpdated;
            GameManager.OnMagnetEnded += HandleMagnetEnded;
            GameManager.OnHourglassUsed += HandleHourglassUsed;
            GameManager.OnGameTimeUpdated += UpdateTimeDisplay;
            GameManager.OnMagnetStarted += HandleMagnetStarted;
            GameManager.OnMagnetTimeUpdated += HandleMagnetTimeUpdated;
            GameManager.OnMagnetEnded += HandleMagnetEnded;
            GameManager.OnHourglassUsed += HandleHourglassUsed;
            
            // GÃ¼venlik uyarÄ±sÄ± dinleyicisi
            Managers.EnvironmentSafetyManager.OnSafetyWarningTriggered += HandleSafetyWarning;

            // Etkinlik yÃ¶neticisi (Frenzy vb.) dinleyicisi
            Managers.EventManager.OnGameEventStarted += HandleGameEventStarted;
            Managers.EventManager.OnGameEventEnded += HandleGameEventEnded;

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
            // BaÅŸlangÄ±Ã§ durumunu hemen ekrana yansÄ±t
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
            
            // ScoreManager Ã¼zerinden kombo olaylarÄ±nÄ± dinlemeye baÅŸla (Awake sonrasÄ± olduÄŸu iÃ§in Instance hazÄ±rdÄ±r)
            if (Core.ScoreManager.Instance != null)
            {
                RecycleRush.Managers.ComboManager.OnComboChanged += HandleComboChanged;
            }
            
            // BaÅŸlangÄ±Ã§ta yazÄ±larÄ± gizle
            if (comboText != null) comboText.gameObject.SetActive(false);
            if (eventNotificationText != null) eventNotificationText.gameObject.SetActive(false);
            if (powerupNotificationText != null) powerupNotificationText.gameObject.SetActive(false);

            // SliderlarÄ± AudioManager'a baÄŸla
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
            
            // Panelleri baÅŸlangÄ±Ã§ta gizle
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (pausePanel != null)
            {
                SetupPauseButtons();
                pausePanel.SetActive(false);
                
                UnityEngine.UI.Button[] buttons = pausePanel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (var btn in buttons)
                {
                    if (btn.name.ToLower().Contains("exit") || btn.name.ToLower().Contains("quit"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(QuitApplication);
                        Debug.Log($"<color=green>[UIManager]</color> Pause Panel: '{btn.name}' butonu QuitApplication metoduna otomatik baÄŸlandÄ±!");
                    }
                }
            }
            if (gameOverPanel != null)
            {
                SetupGameOverButtons();
                gameOverPanel.SetActive(false);
            }
                
                // Oyuncu Inspector'dan baÄŸlamayÄ± unutursa diye Game Over panelindeki butonlarÄ± otomatik bulup baÄŸlÄ±yoruz:
                UnityEngine.UI.Button[] buttons = gameOverPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                foreach (var btn in buttons)
                {
                    string btnName = btn.name.ToLower();
                    if (btnName.Contains("exit") || btnName.Contains("quit"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(QuitApplication);
                        Debug.Log($"<color=green>[UIManager]</color> '{btn.name}' butonu QuitApplication metoduna otomatik baÄŸlandÄ±!");
                    }
                    else if (btnName.Contains("restart") || btnName.Contains("retry"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(RestartGameUI);
                        Debug.Log($"<color=green>[UIManager]</color> '{btn.name}' butonu RestartGameUI metoduna otomatik baÄŸlandÄ±!");
                    }
                    else if (btnName.Contains("next"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(StartNextLevelUI);
                        Debug.Log($"<color=green>[UIManager]</color> '{btn.name}' butonu StartNextLevelUI metoduna otomatik baÄŸlandÄ±!");
                    }
                }
            }
            if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(false);
            
            // GÃ¼venlik panelini baÅŸlangÄ±Ã§ta gizle
            if (safetyWarningPanel != null) safetyWarningPanel.SetActive(false);
        }

        private void Update()
        {
            // PC testi iÃ§in klavyeden ESC tuÅŸu (Yeni Input System kullanÄ±larak)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleMenuPauseToggle();
            }
        }

        private void OnDisable()
        {
            // Bellek sÄ±zÄ±ntÄ±sÄ±nÄ± Ã¶nlemek iÃ§in dinlemeyi bÄ±rak
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
            GameManager.OnMagnetStarted -= HandleMagnetStarted;
            GameManager.OnMagnetTimeUpdated -= HandleMagnetTimeUpdated;
            GameManager.OnMagnetEnded -= HandleMagnetEnded;
            GameManager.OnHourglassUsed -= HandleHourglassUsed;
            
            Managers.EnvironmentSafetyManager.OnSafetyWarningTriggered -= HandleSafetyWarning;
            
            Managers.EventManager.OnGameEventStarted -= HandleGameEventStarted;
            Managers.EventManager.OnGameEventEnded -= HandleGameEventEnded;
            
            if (Core.ScoreManager.Instance != null)
            {
                RecycleRush.Managers.ComboManager.OnComboChanged -= HandleComboChanged;
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
        /// Oyun durumu her deÄŸiÅŸtiÄŸinde (MainMenu -> Playing -> GameOver) bu fonksiyon Ã§alÄ±ÅŸÄ±r.
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
                    if (timeText != null) timeText.text = "Time: 60";
                    
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
                    break;
                    
                case GameState.ReadyToStart:
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

                    if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(true); // Butona basÄ±lÄ±nca da Pause butonu gÃ¶rÃ¼nsÃ¼n!
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
                    break;
                    
                case GameState.Playing:
                    if (statusText != null) 
                    {
                        statusText.text = "RECYCLING STARTED";
                        // Oyuncunun Ã¶nÃ¼nÃ¼ kapatmamak iÃ§in yazÄ±yÄ± 2 saniye sonra silen bir coroutine baÅŸlatÄ±yoruz
                        StartCoroutine(ClearStatusTextAfterDelay(2f));
                    }
                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    if (pausePanel != null) pausePanel.SetActive(false);
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    if (pauseButtonUIObj != null) 
                        pauseButtonUIObj.SetActive(true);
                    else
                        Debug.LogWarning("<color=red>[UIManager]</color> Pause Button UI Obj atanmamÄ±ÅŸ (None)! Pause butonu gÃ¶rÃ¼nmÃ¼yor olabilir.");
                        
                    if (levelSelectionBoard != null) levelSelectionBoard.SetActive(false);
                    if (missionPanel != null) missionPanel.SetActive(true);
                    if (xpPanel != null) xpPanel.SetActive(true);
                    if (comboPanel != null) comboPanel.SetActive(true);
                    break;
                    
                case GameState.Countdown:
                    if (levelSelectionBoard != null) levelSelectionBoard.SetActive(false);
                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    if (pausePanel != null) pausePanel.SetActive(false);
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    if (pauseButtonUIObj != null)
                        pauseButtonUIObj.SetActive(true);
                    break;

                case GameState.Tutorial:
                    // TutorialManager yazÄ±larÄ± kendisi yÃ¶netecek, burada sadece butonu gizliyoruz
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
                    if (statusText != null) statusText.text = "SYSTEM PAUSED";
                    if (levelSelectionBoard != null) levelSelectionBoard.SetActive(false);
                    if (pausePanel != null) pausePanel.SetActive(true);
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    if (pauseButtonUIObj != null) pauseButtonUIObj.SetActive(false);
                    break;

                case GameState.GameOver:
                    if (levelSelectionBoard != null) levelSelectionBoard.SetActive(false);
                    if (statusText != null)
                    if (statusText != null) statusText.text = "<color=red>TIME'S UP!</color>\nCONVEYOR STOPPED";
                    
                    // Oyun bittiÄŸinde GameOver panelini aÃ§ ve son skoru yazdÄ±r!
                    if (gameOverPanel != null) 
                    {
                        statusText.text = "<color=red>TIME'S UP!</color>\nRECYCLING STOPPED";
                        statusText.transform.localScale = _originalStatusScale;
                    }

                    // Oyun bittiğinde GameOver panelini aç ve butonları dinamik olarak bağla
                    if (gameOverPanel != null)
                    {
                        SetupGameOverButtons();
                        gameOverPanel.SetActive(true);
                        
                        // Next Level butonunun aktiflik durumunu gÃ¶reve gÃ¶re ayarla
                        bool isMissionCompleted = false;
                        if (MissionManager.Instance != null && MissionManager.Instance.ActiveMission != null)
                        {
                            isMissionCompleted = MissionManager.Instance.ActiveMission.IsCompleted;
                        }

                        UnityEngine.UI.Button[] buttons = gameOverPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                        foreach (var btn in buttons)
                        {
                            if (btn.name.ToLower().Contains("next"))
                            {
                                btn.interactable = isMissionCompleted; // TamamlanmadÄ±ysa basÄ±lamaz (soluk) olur
                                // Ä°stenirse tamamen gizlemek iÃ§in: btn.gameObject.SetActive(isMissionCompleted);
                            }
                        }
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
        /// GÃ¼venlik yÃ¶neticisinden gelen dÃ¼ÅŸÃ¼k Ä±ÅŸÄ±k / takip koptu uyarÄ±larÄ±nÄ± yÃ¶netir.
        /// </summary>
        private void HandleSafetyWarning(bool isWarningActive)
        {
            if (safetyWarningPanel != null)
            {
                safetyWarningPanel.SetActive(isWarningActive);
            }
            else
            {
                if (isWarningActive)
                {
                    Debug.LogWarning("<color=red>[UIManager]</color> GÃ¼venlik uyarÄ±sÄ± tetiklendi fakat Inspector'da 'Safety Warning Panel' atanmamÄ±ÅŸ!");
                }
            }
        }

        /// <summary>
        /// GameManager'dan saniye saniye gelen kalan sÃ¼re bilgisini ekrana (timeText) yazar.
        /// </summary>
        private void UpdateTimeDisplay(float remainingTime)
        {
            if (timeText != null)
            {
                timeText.text = $"Time: {Mathf.CeilToInt(remainingTime)}";

                // Vurgu (Juice): Son 10 saniye kala yazıyı kırmızı yap!
                // SÃ¼reyi tam sayÄ±ya (Ã–rn: 59.4 -> 60) yuvarlayarak baÅŸÄ±na 'Time:' Ã¶n ekiyle yazdÄ±r
                timeText.text = $"Time: {Mathf.CeilToInt(remainingTime)}";
                
                // Vurgu (Juice): Son 10 saniye kala yazÄ±yÄ± kÄ±rmÄ±zÄ± yap!
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
        /// <summary>
        /// Kombo deÄŸiÅŸtiÄŸinde tetiklenir ve Pop (Patlama) animasyonunu baÅŸlatÄ±r.
        /// </summary>
        private void HandleComboChanged(int comboCount, int multiplier, bool isRankUp = false)
        {
            if (comboText == null) return;

            if (multiplier > 1)
            {
                // Kombo varsa yazÄ±yÄ± aktif et ve metni ayarla
                comboText.gameObject.SetActive(true);
                comboText.text = $"{multiplier}x COMBO!";
                comboText.color = new Color(1f, 0.84f, 0f);

                if (_comboAnimationCoroutine != null) StopCoroutine(_comboAnimationCoroutine);
                comboText.color = new Color(1f, 0.84f, 0f); // AltÄ±n SarÄ±sÄ± (Gold)

                // Varsa Ã¶nceki animasyonu durdur ki Ã§akÄ±ÅŸmasÄ±n
                if (_comboAnimationCoroutine != null)
                {
                    StopCoroutine(_comboAnimationCoroutine);
                }
                
                // Yeni Pop animasyonunu baÅŸlat
                _comboAnimationCoroutine = StartCoroutine(ComboPopAnimation());
            }
            else
            {
                // KatlayÄ±cÄ± yoksa (Kombo sÄ±fÄ±rlandÄ±ysa) yazÄ±yÄ± gizle
                comboText.gameObject.SetActive(false);
            }
        }

        private IEnumerator ClearStatusTextAfterDelay(float delay)
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
            yield return new WaitForSeconds(delay);
            if (statusText != null)
            {
                statusText.text = "";
            }
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
        /// 3-2-1-BAÅLA ÅŸeklinde profesyonel, animasyonlu (Pop & Lerp) geri sayÄ±m yapar.
        /// </summary>
        private IEnumerator StartCountdownAnimation()
        {
            Debug.Log("<color=yellow>[UIManager]</color> StartCountdownAnimation Coroutine'i baÅŸladÄ±!");
            
            // EÄŸer Unity'de arayÃ¼z yazÄ±sÄ± (statusText) atanmamÄ±ÅŸsa, oyunu kitlememek iÃ§in direkt baÅŸlat
            if (statusText == null) 
            {
                Debug.LogWarning("<color=orange>[UIManager]</color> statusText atanmamÄ±ÅŸ (veya silinmiÅŸ)! Geri sayÄ±m atlanÄ±p oyun baÅŸlatÄ±lÄ±yor.");
                // C# Event Ã§akÄ±ÅŸmasÄ±nÄ± Ã¶nlemek iÃ§in (Reentrancy Bug) 1 frame bekleyip Ã¶yle baÅŸlatÄ±yoruz
                yield return null; 
                if (GameManager.Instance != null) GameManager.Instance.FinishCountdown();
                yield break;
            }
            
            Debug.Log("<color=yellow>[UIManager]</color> statusText mevcut, geri sayÄ±m dÃ¶ngÃ¼sÃ¼ne giriliyor...");

            string[] countTexts = { "<color=yellow>3</color>", "<color=orange>2</color>", "<color=red>1</color>", "<color=green>GO!</color>" };
            Vector3 originalScale = _originalStatusScale;
            Vector3 targetScale = originalScale * 1.3f;
            
            // DÃœZELTME: YazÄ±nÄ±n Inspector'daki orijinal Scale (Ã¶lÃ§ek) deÄŸerini al (VR projelerinde UI genelde 0.005 gibi ufak deÄŸerlerdir)
            Vector3 originalScale = statusText.transform.localScale;
            // SÄ±fÄ±rlanma ihtimaline karÅŸÄ± koruma
            if (originalScale.magnitude < 0.0001f) originalScale = Vector3.one;
            
            Vector3 targetScale = originalScale * 2f; // %100 bÃ¼yÃ¼t (Daha vurucu bir etki iÃ§in)

            foreach (string text in countTexts)
            {
                if (statusText == null) yield break;
                statusText.text = text;

                
                // TODO: AudioManager Ã¼zerinden "Bip" sesi Ã§aldÄ±rma buraya eklenecek
                
                // BÃ¼yÃ¼me (Scale Up) - HÄ±zlÄ±ca patlama efekti (Pop)
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

                // KÃ¼Ã§Ã¼lme (Scale Down) - YavaÅŸÃ§a eski haline dÃ¶nme ve bekleme
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
            // Geri sayÄ±m bitti, yazÄ±yÄ± temizle ve oyunu asÄ±l ÅŸimdi baÅŸlat!
            statusText.text = ""; 
            
            Debug.Log("<color=yellow>[UIManager]</color> Geri sayÄ±m animasyonu tamamlandÄ±, FinishCountdown Ã§aÄŸrÄ±lÄ±yor...");
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
        /// <summary>
        /// YazÄ±yÄ± bir anda bÃ¼yÃ¼tÃ¼p sonra yavaÅŸÃ§a normal boyutuna indiren (Lerp) Juice animasyonu.
        /// </summary>
        private IEnumerator ComboPopAnimation()
        {
            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = originalScale * 1.5f; // %50 bÃ¼yÃ¼t
            
            float duration = 0.15f; // BÃ¼yÃ¼me sÃ¼resi
            float elapsed = 0f;

            // BÃ¼yÃ¼me (Scale Up)
            while (elapsed < duration)
            {
                comboText.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            comboText.transform.localScale = targetScale;
            
            elapsed = 0f;
            duration = 0.25f; // KÃ¼Ã§Ã¼lme sÃ¼resi (Daha yumuÅŸak)
            
            // KÃ¼Ã§Ã¼lme (Scale Down)
            while (elapsed < duration)
            {
                comboText.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            comboText.transform.localScale = originalScale;
            
            // Oyuncunun yazÄ±yÄ± okuyabilmesi iÃ§in 1 saniye bekle
            yield return new WaitForSeconds(1.0f);
            
            // Ekranda sÃ¼rekli kalmamasÄ± iÃ§in yazÄ±yÄ± gizle
            comboText.gameObject.SetActive(false);
            
            _comboAnimationCoroutine = null;
        }

        // --- YENÄ° EKLENEN PANEL VE MENÃœ KONTROL METOTLARI ---

        private void OnMenuButtonPressed(InputAction.CallbackContext context)
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
            else
            {
                Debug.LogWarning("<color=red>[UIManager]</color> Settings Panel aÃ§Ä±lmaya Ã§alÄ±ÅŸÄ±ldÄ± ancak Inspector'da 'Settings Panel' deÄŸiÅŸkeni ATANMAMIÅ (None)! LÃ¼tfen UIManager bileÅŸenindeki boÅŸluÄŸa paneli sÃ¼rÃ¼kleyin.");
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

            // EKLENDÄ°: Restart yapÄ±ldÄ±ÄŸÄ±nda aktif gÃ¶revi sÄ±fÄ±rlamak iÃ§in mevcut bÃ¶lÃ¼mÃ¼ baÅŸtan kur
            if (LevelSelectionManager.Instance != null)
            {
                int currentLvl = LevelSelectionManager.Instance.CurrentPlayingLevelId;
                LevelSelectionManager.Instance.StartLevel(currentLvl);
                Debug.Log($"<color=cyan>[UIManager]</color> Restart butonuna basÄ±ldÄ±. AÅŸama {currentLvl} ve gÃ¶revleri sÄ±fÄ±rlandÄ±.");
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

                
                if (nextLevel != null && nextLevel.IsUnlocked)
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.PrepareToStart();
                    }
                }
                else
                {
                    Debug.LogWarning($"<color=orange>[UIManager]</color> Sonraki seviye ({nextLevelId}) kilitli veya bulunamadı!");
                    LevelSelectionManager.Instance.StartLevel(nextLevelId);
                    Debug.Log($"<color=green>[UIManager]</color> Next Level butonuna basÄ±ldÄ±. AÅŸama {nextLevelId} hazÄ±rlanÄ±yor.");
                }
                else
                {
                    Debug.LogWarning($"<color=orange>[UIManager]</color> Sonraki seviye ({nextLevelId}) kilitli veya bulunamadÄ±!");
                }
            }
        }

        public void ResumeGameUI()
        {
            if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
        }

        // --- YENÄ° EKLENEN EVENT VE POWER-UP UI METOTLARI ---

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
                // Mevcut bir Magnet varsa yazÄ±sÄ±nÄ± ezmemek iÃ§in kÄ±sa bir bildirim gÃ¶sterip eski haline dÃ¶nÃ¼lebilir.
                // Veya ÅŸimdilik 2 saniye boyunca sÃ¼renin eklendiÄŸini gÃ¶sterebiliriz.
                StartCoroutine(ShowHourglassNotification(timeAdded));
            }
        }

        private IEnumerator ShowHourglassNotification(float timeAdded)
        {
            bool wasMagnetActive = GameManager.Instance != null && GameManager.Instance.IsMagnetActive;
            
            powerupNotificationText.gameObject.SetActive(true);
            powerupNotificationText.color = Color.green;
            powerupNotificationText.text = $"+{timeAdded} SECONDS!";
            
            yield return new WaitForSeconds(2f);
            
            if (wasMagnetActive && GameManager.Instance != null && GameManager.Instance.IsMagnetActive)
            {
                // Magnet geri dÃ¶nsÃ¼n
                HandleMagnetTimeUpdated(GameManager.Instance.MagnetRemainingTime);
                powerupNotificationText.color = Color.cyan;
            }
            else
            {
                powerupNotificationText.gameObject.SetActive(false);
            }
        }

        public void QuitApplication()
        {
            Debug.Log("[UIManager] Uygulamadan Çıkılıyor...");
            Debug.Log("[UIManager] Uygulamadan Ã‡Ä±kÄ±lÄ±yor... (Exit Butonu Tetiklendi)");
            
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
}


