using System;
using System.Collections.Generic;
// TODO [Post-Merge]: Verify GameManager state transitions and missing prefab references after the massive PR merge.

using UnityEngine;
using RecycleRush.Managers; // <-- EKLENDÄ° (AchievementData iÃ§in)

// Oyunun anlÄ±k durumlarÄ±nÄ± belirten State yapÄ±sÄ±
public enum GameState
{
    Initialization,
    MainMenu,
    ReadyToStart,
    Placement, // AR ortamÄ±nda kutularÄ±n yerleÅŸtirildiÄŸi evre
    Tutorial,
    Countdown,
    Playing,
    Paused,
    GameOver
}

[System.Serializable]
public struct GameSessionData
{
    [Header("Basic Stats")]
    public int TotalCorrectThrows;
    public int TotalIncorrectThrows;
    public int GoldenWastesCollected;
    public float TotalPlayTime;

    [Header("Precision Stats (New)")]
    public int TotalPerfectThrows;
    public int TotalGreatThrows;
    public int TotalGoodThrows;
    public float AveragePrecision; // Ortalama Ä°sabet Kalitesi (0-100)
    public float BestPrecision;    // O maÃ§taki en yÃ¼ksek isabet skoru
    public int PrecisionBonusScore;
    public int MaxPrecisionStreak; // O maçtaki en yüksek Perfect serisi

    // YENİ: Precision Consistency (Tutarlılık) 
    public float PrecisionM2;      // Varyans hesabı için Welford algoritması ara değeri (Gösterilmez, hesaplama içindir)
    public float PrecisionConsistency; // %0 - %100 arası oyuncu istikrarı

    [Header("Combo Stats")]
    public int MaxCombo;
    public int LongestStreak;     // En uzun doğru atış serisi
    public int GraceUsedCount;    // Kaç kere kombo affı kullanıldı

    [Header("Golden Waste Stats")]
    public int GoldenWastesMissed; // Kaçırılan altın çöpler

    public int MaxPrecisionStreak; // O maÃ§taki en yÃ¼ksek Perfect serisi
    
    // YENÄ°: Precision Consistency (TutarlÄ±lÄ±k) 
    public float PrecisionM2;      // Varyans hesabÄ± iÃ§in Welford algoritmasÄ± ara deÄŸeri (GÃ¶sterilmez, hesaplama iÃ§indir)
    public float PrecisionConsistency; // %0 - %100 arasÄ± oyuncu istikrarÄ±
    
    [Header("Combo Stats")]
    public int MaxCombo;
    public int LongestStreak;     // En uzun doÄŸru atÄ±ÅŸ serisi
    public int GraceUsedCount;    // KaÃ§ kere kombo affÄ± kullanÄ±ldÄ±
    
    [Header("Golden Waste Stats")]
    public int GoldenWastesMissed; // KaÃ§Ä±rÄ±lan altÄ±n Ã§Ã¶pler
    
    [Header("Score Breakdown")]
    public int BaseScore;
    public int ComboBonusScore;
    public int GoldenWasteBonusScore;
    public int PenaltyScore;

    [Header("Economy Breakdown")]
    public int BaseXP;
    public int ComboXP;
    public int GoldenXP;
    public int EarnedXP; // Total XP

    public int BaseCoin;
    public int ComboCoin;
    public int GoldenCoin;
    public int EarnedCoins; // Total Coins

    [Header("Performance")]
    public float AccuracyPercentage;
    public string PerformanceGrade; // S, A, B, C, D

    [Header("Grade Breakdown (Total 100)")]
    public float AccuracyGradeScore; // Max 40
    public float PrecisionGradeScore;// Max 25
    public float ComboGradeScore;    // Max 20
    public float GoldenGradeScore;   // Max 15
    public float TotalGradeScore;

    [Header("New Records & Deltas")]
    public bool IsNewHighScore;
    public int ScoreDelta; // Yeni skor ile eski en iyi skor arasındaki fark

    public int ScoreDelta; // Yeni skor ile eski en iyi skor arasÄ±ndaki fark
    
    public bool IsNewBestAccuracy;
    public float AccuracyDelta;

    public bool IsNewBestCombo;
    public int ComboDelta;

    [Header("Session Efficiency")]
    public float ScorePerMinute;
    public float XPPerMinute;
    public float ThrowsPerMinute;

    [Header("Next Goal")]
    public string SuggestedNextGoal;

    [Header("Highlight")]
    public string SessionHighlight; // Oturumun öne çıkan anı

    public string SessionHighlight; // Oturumun Ã¶ne Ã§Ä±kan anÄ±
    
    [Header("Medals")]
    public List<string> EarnedMedals; // Oturum sonu kazanÄ±lan madalyalar
}

public class GameManager : MonoBehaviour
{
    // Singleton Pattern: GameManager'a her yerden gÃ¼venle ve tek bir instance Ã¼zerinden ulaÅŸabilmek iÃ§in.
    public static GameManager Instance { get; private set; }

    [Header("Session Data")]
    public GameSessionData CurrentSession;

    // Oyun sonu ekranı (UI), Analytics veya SaveManager'ın güvenle okuyabileceği Immutable (değiştirilemez) Snapshot
    
    // Oyun sonu ekranÄ± (UI), Analytics veya SaveManager'Ä±n gÃ¼venle okuyabileceÄŸi Immutable (deÄŸiÅŸtirilemez) Snapshot
    public GameSessionData FinalSessionReport { get; private set; }

    [Header("Game Timers")]
    [Tooltip("Oyunun toplam sÃ¼resi (saniye cinsinden)")]
    [SerializeField] private float _gameDuration = 60f;


    // Oyun durumunun okunabilmesi ama sadece bu sınıf tarafından değiştirilebilmesi için Property
    
    [Header("UI / ModÃ¼ller")]
    [Tooltip("Fiziksel butonlarÄ±n bulunduÄŸu modÃ¼l (Play, Settings vb.)")]
    public GameObject buttonsModule;
    
    [Tooltip("Oyun baÅŸladÄ±ÄŸÄ±nda animasyonla belirecek Ã§evre modÃ¼lleri (BoÅŸ bÄ±rakÄ±rsanÄ±z otomatik bulur)")]
    public GameObject[] environmentModules;
    
    private Vector3 _buttonsOriginalPos;
    private Quaternion _buttonsOriginalRot;
    private bool _hasSavedButtonsTransform = false;
    private Coroutine _hideButtonsCoroutine;
    
    // Oyun durumunun okunabilmesi ama sadece bu sÄ±nÄ±f tarafÄ±ndan deÄŸiÅŸtirilebilmesi iÃ§in Property
    public GameState CurrentState { get; private set; }
    public GameState PreviousState { get; private set; }

    public float RemainingTime { get; private set; }


    
    [Header("Dalga (Wave) AltyapÄ±sÄ± (Skeleton)")]
    public int CurrentWave { get; private set; }
    [SerializeField] private int _maxWave = 5;

    // Event'ler (Olaylar): Spagetti kodu engeller. DiÄŸer sÄ±nÄ±flar sadece bu eventleri dinler.
    // Ã–rneÄŸin; UI yÃ¶neticisi OnGameStateChanged'i dinler ve GameOver gelince bitiÅŸ panelini aÃ§ar.
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<float> OnGameTimeUpdated;

    private void Awake()
    {
        // Thread-safe / Scene-transition safe Singleton Kurulumu (SOLID UyumluluÄŸu)
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] Sahnede birden fazla GameManager bulundu, kopya siliniyor.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // GameManager'Ä±n sahneler arasÄ± referans kaybÄ± yaÅŸamamasÄ± iÃ§in kalÄ±cÄ± yapÄ±lmasÄ± (Ã–nerilen)
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.OnAchievementUnlocked += HandleAchievementUnlocked;
        }
        RecycleRush.Core.RoomPollutionManager.OnGameOverTriggered += HandlePollutionGameOver;
    }

    private void OnDisable()
    {
        if (AchievementManager.Instance != null)
        {
            AchievementManager.OnAchievementUnlocked -= HandleAchievementUnlocked;
        }
        RecycleRush.Core.RoomPollutionManager.OnGameOverTriggered -= HandlePollutionGameOver;
    }

    private void HandlePollutionGameOver(RecycleRush.Core.PollutionStats stats)
    {
        Debug.Log("<color=red>[GameManager]</color> Kirlilik maksimuma ulaÅŸtÄ±! Oyun sona eriyor.");
        EndGame();
    }

    private void Start()
    {

        // Ã‡evre modÃ¼lleri inspector'dan atanmadÄ±ysa otomatik olarak bul
        if (environmentModules == null || environmentModules.Length == 0)
        {
            environmentModules = new GameObject[]
            {
                GameObject.Find("ConveyorSystem_Module"),
                GameObject.Find("RecyclingArea_Module"),
                GameObject.Find("scoreboard"),
                GameObject.Find("QC_Drone")
            };
        }

        // Oyun ilk aÃ§Ä±ldÄ±ÄŸÄ±nda hazÄ±rlÄ±k evresinden geÃ§er, ardÄ±ndan ana menÃ¼ (veya doÄŸrudan oyun) baÅŸlar.
        ChangeState(GameState.Initialization);

        // Oyun artık otomatik BAŞLAMAYACAK. 
        // Oyuncunun makinedeki kolu (Lever) çekmesini beklemek için MainMenu (veya bekleme) durumunda kalıyoruz.
        
        // Oyun artÄ±k otomatik BAÅLAMAYACAK. 
        // Oyuncunun makinedeki kolu (Lever) Ã§ekmesini beklemek iÃ§in MainMenu (veya bekleme) durumunda kalÄ±yoruz.
        ChangeState(GameState.MainMenu);
    }

    // --- Achievement Manager Entegrasyonu ---
    private void HandleAchievementUnlocked(AchievementData achData)
    {
        var session = CurrentSession;
        session.EarnedXP += achData.RewardXP;
        session.EarnedCoins += achData.RewardCoin;
        CurrentSession = session;

        Debug.Log($"<color=green>[GameManager]</color> Başarım Ödülü Alındı: +{achData.RewardXP} XP | +{achData.RewardCoin} Coin");
        
        Debug.Log($"<color=green>[GameManager]</color> BaÅŸarÄ±m Ã–dÃ¼lÃ¼ AlÄ±ndÄ±: +{achData.RewardXP} XP | +{achData.RewardCoin} Coin");
    }

    private void Update()
    {
        // ZamanlayÄ±cÄ± sadece oyun oynanÄ±rken Ã§alÄ±ÅŸÄ±r. (Pause veya GameOver'da durur).
        if (CurrentState == GameState.Playing)
        {
            UpdateTimer();
        }
    }

    /// <summary>
    /// Oyunun durumunu gÃ¼venli bir ÅŸekilde deÄŸiÅŸtirir ve diÄŸer sistemlere anons eder.
    /// MantÄ±ksÄ±z geÃ§iÅŸler (Ã–rn: MainMenu -> Playing) engellenir.
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return; // Zaten o durumdaysak işlem yapma

        // State Transition Validation (Geçiş Kontrol Kalkanı - SOLID/Defensive Programming)
        if (CurrentState == newState) return; // Zaten o durumdaysak iÅŸlem yapma
        
        // State Transition Validation (GeÃ§iÅŸ Kontrol KalkanÄ± - SOLID/Defensive Programming)
        bool isValidTransition = true;
        switch (CurrentState)
        {
            case GameState.MainMenu:
                if (newState == GameState.Paused || newState == GameState.GameOver) isValidTransition = false;
                break;
            case GameState.Placement:
                if (newState != GameState.Countdown && newState != GameState.MainMenu) isValidTransition = false;
                break;
            case GameState.Countdown:
                if (newState != GameState.Playing && newState != GameState.MainMenu) isValidTransition = false;
                break;
            case GameState.Playing:
                if (newState != GameState.Paused && newState != GameState.GameOver) isValidTransition = false;
                break;
            case GameState.Paused:
                if (newState != GameState.Playing && newState != GameState.MainMenu && newState != GameState.Countdown) isValidTransition = false;
                break;
            case GameState.GameOver:
                if (newState != GameState.Countdown && newState != GameState.MainMenu) isValidTransition = false;
                break;
        }

        if (!isValidTransition)
        {
            Debug.LogError($"[GameManager] HATA (GeÃ§ersiz GeÃ§iÅŸ): {CurrentState} durumundan {newState} durumuna geÃ§ilemez!");
            return;
        }

        PreviousState = CurrentState;

        // Eski state'den çıkış işlemlerini çalıştır (FSM OnExit)
        
        // Eski state'den Ã§Ä±kÄ±ÅŸ iÅŸlemlerini Ã§alÄ±ÅŸtÄ±r (FSM OnExit)
        OnStateExit(PreviousState);

        CurrentState = newState;
        Debug.Log($"[GameManager] Oyun durumu değişti: {PreviousState} -> {CurrentState}");

        // Yeni state'e giriş işlemlerini çalıştır (FSM OnEnter)
        OnStateEnter(CurrentState);

        Debug.Log($"[GameManager] Oyun durumu deÄŸiÅŸti: {PreviousState} -> {CurrentState}");
        
        // Yeni state'e giriÅŸ iÅŸlemlerini Ã§alÄ±ÅŸtÄ±r (FSM OnEnter)
        OnStateEnter(CurrentState);
        
        // ModÃ¼lleri Duruma GÃ¶re Otomatik YÃ¶net (Butonlar vb.)
        if (buttonsModule != null)
        {
            if (!_hasSavedButtonsTransform)
            {
                _buttonsOriginalPos = buttonsModule.transform.position;
                _buttonsOriginalRot = buttonsModule.transform.rotation;
                _hasSavedButtonsTransform = true;
            }

            if (CurrentState == GameState.MainMenu)
            {
                if (_hideButtonsCoroutine != null)
                {
                    StopCoroutine(_hideButtonsCoroutine);
                    _hideButtonsCoroutine = null;
                }
                
                // Eski haline (dik konumuna ve orijinal pozisyonuna) geri getir
                buttonsModule.transform.position = _buttonsOriginalPos;
                buttonsModule.transform.rotation = _buttonsOriginalRot;
                buttonsModule.SetActive(true);

                // EÄŸer Ana MenÃ¼ye dÃ¶nÃ¼ldÃ¼yse Ã§evreyi tekrar gizle (Scale 0)
                if (environmentModules != null)
                {
                    foreach (var module in environmentModules)
                    {
                        if (module != null) module.transform.localScale = Vector3.zero;
                    }
                }
            }
            else if (CurrentState == GameState.Placement || CurrentState == GameState.Countdown)
            {
                // Sadece Butonlar gÃ¶rÃ¼nÃ¼r durumdaysa devrilme animasyonu baÅŸlat (Ã¶rn: MainMenu'den gelirsek)
                if (buttonsModule.activeSelf)
                {
                    if (_hideButtonsCoroutine != null) StopCoroutine(_hideButtonsCoroutine);
                    _hideButtonsCoroutine = StartCoroutine(HideButtonsRoutine());
                }
            }
            else
            {
                if (_hideButtonsCoroutine != null) StopCoroutine(_hideButtonsCoroutine);
                buttonsModule.SetActive(false);
            }
        }

        // Durum deÄŸiÅŸikliÄŸini tÃ¼m sisteme yayÄ±nla (Broadcast)
        OnGameStateChanged?.Invoke(CurrentState);
    }

    #region FSM (Finite State Machine) Lifecycle Fonksiyonları

    
    #region FSM (Finite State Machine) Lifecycle FonksiyonlarÄ±
    
    /// <summary>
    /// Bir duruma (State) girildiÄŸinde sadece bir kez Ã§alÄ±ÅŸacak olan iÅŸlemler.
    /// (Ses, animasyon, Ä±ÅŸÄ±k vb. sistemlerin yÃ¶netimi iÃ§in idealdir).
    /// </summary>
    private void OnStateEnter(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                // Ã–rn: MÃ¼zik baÅŸlayabilir, Spawner'a "hÄ±zlan" denilebilir.
                break;
            case GameState.Paused:
                // Ã–rn: Ã‡evre sesleri (Ambient) kÄ±sÄ±labilir.
                break;
            case GameState.GameOver:
                // Ã–rn: TÃ¼m Ã§Ã¶pler dondurulabilir, BGM durdurulabilir.
                break;
        }
    }

    /// <summary>
    /// Bir durumdan (State) Ã§Ä±kÄ±ldÄ±ÄŸÄ±nda sadece bir kez Ã§alÄ±ÅŸacak olan temizlik iÅŸlemleri.
    /// </summary>
    private void OnStateExit(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                // Ana menÃ¼den Ã§Ä±karken menÃ¼ sesleri kapatÄ±labilir.
                break;
        }
    }

    #endregion

    /// <summary>
    /// Oyunu tamamen sıfırlar ve yeniden başlatır. 
    /// </summary>
    public void RestartGame()
    {
        PrepareToStart();
    }

    /// <summary>
    /// Oyunu (veya gerekirse öğreticiyi) başlatır.
    /// Oyunu tamamen sÄ±fÄ±rlar ve yeniden baÅŸlatÄ±r. 
    /// Timer, SessionData, Skor, Kombo, Ã‡Ã¶pler ve Dalgalar (Wave) sÄ±fÄ±rlanÄ±r.
    /// </summary>
    public void RestartGame()
    {
        if (CurrentState == GameState.MainMenu || CurrentState == GameState.GameOver || CurrentState == GameState.Paused)
        {
            // 1. Timer SÄ±fÄ±rla
            RemainingTime = _gameDuration;
            OnGameTimeUpdated?.Invoke(RemainingTime);

            // 2. Session Data SÄ±fÄ±rla
            CurrentSession = new GameSessionData();
            CurrentSession.EarnedMedals = new List<string>(); // Listeyi baÅŸlat

            // 3. Skor ve Komboyu SÄ±fÄ±rla
            if (RecycleRush.Core.ScoreManager.Instance != null)
            {
                RecycleRush.Core.ScoreManager.Instance.ResetScore();
            }
            
            // 4. Havuzdaki Ã§Ã¶pleri temizle (EÄŸer ObjectPoolManager varsa)
            // if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.ReturnAllToPool();

            // 5. Motor hÄ±zÄ±nÄ± sÄ±fÄ±rla (Paused durumundan geliyorsak zaman durmuÅŸ olabilir)
            Time.timeScale = 1f;
            
            // 6. Dalga sistemini (Wave) sÄ±fÄ±rla
            CurrentWave = 1;

            // 7. Geri SayÄ±m (Countdown) durumuna geÃ§erek oyunu baÅŸlat.
            ChangeState(GameState.Countdown);
        }
    }

    #region Wave Skeleton (Ä°leride GeliÅŸtirilecek)
    
    /// <summary>
    /// Yeni bir dalgayÄ± (Wave) baÅŸlatÄ±r. Zorluk seviyesi (HÄ±z, Spawn sÃ¼resi) burada artÄ±rÄ±labilir.
    /// (Solid: AÃ§Ä±k/KapalÄ± prensibine uygun geniÅŸleme alanÄ±)
    /// </summary>
    public void StartWave()
    {
        Debug.Log($"[GameManager] Wave {CurrentWave} BaÅŸlÄ±yor!");
        // TODO: Spawner sistemine "Daha hÄ±zlÄ± Ã¼ret" sinyali gÃ¶nderilebilir.
    }

    /// <summary>
    /// Mevcut dalgayÄ± (Wave) sonlandÄ±rÄ±r.
    /// </summary>
    public void EndWave()
    {
        Debug.Log($"[GameManager] Wave {CurrentWave} Bitti!");
        if (CurrentWave < _maxWave)
        {
            CurrentWave++;
            StartWave();
        }
        else
        {
            EndGame(); // Son dalga bittiyse oyun biter
        }
    }
    
    #endregion

    /// <summary>
    /// Oyunu (veya gerekirse Ã¶ÄŸreticiyi) baÅŸlatÄ±r.
    /// </summary>
    public void StartGame()
    {
        // EÄŸitimi hiÃ§ tamamlamamÄ±ÅŸsa (0 ise) veya anahtar yoksa Tutorial'e geÃ§
        if (PlayerPrefs.GetInt("TutorialDone", 0) == 0)
        {
            ChangeState(GameState.Tutorial);
        }
        else
        {
            PrepareToStart();
        }
    }

    /// <summary>
    /// UIManager geri sayÄ±m animasyonunu bitirdiÄŸinde bu fonksiyonu Ã§aÄŸÄ±rÄ±r ve oyunu asÄ±l o zaman baÅŸlatÄ±r.
    /// </summary>
    public void FinishCountdown()
    {
        ChangeState(GameState.Playing);
    }

    /// <summary>
    /// Oyunu duraklatÄ±r.
    /// </summary>
    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
            Time.timeScale = 0f; // Fizik motorunu ve update sÃ¼relerini durdurur
        }
    }

    /// <summary>
    /// Duran oyunu devam ettirir.
    /// </summary>
    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            ChangeState(GameState.Playing);
            Time.timeScale = 1f; // Motoru tekrar normal hÄ±zÄ±na getirir
        }
    }

    /// <summary>
    /// Oyunu durdurur veya devam ettirir (VR menÃ¼ tuÅŸu iÃ§in idealdir).
    /// </summary>
    public void TogglePauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            PauseGame();
        }
        else if (CurrentState == GameState.Paused)
        {
            ResumeGame();
        }
    }

    /// <summary>
    /// Geri sayÄ±m sistemini gÃ¼nceller.
    /// </summary>
    private void UpdateTimer()
    {
        if (RemainingTime > 0)
        {
            RemainingTime -= Time.deltaTime;

            // Eğer süre sıfırın altına düştüyse sıfıra sabitle.
            
            // EÄŸer sÃ¼re sÄ±fÄ±rÄ±n altÄ±na dÃ¼ÅŸtÃ¼yse sÄ±fÄ±ra sabitle.
            if (RemainingTime < 0) RemainingTime = 0;

            // UI gibi diÄŸer sistemlerin zamanÄ± saniye saniye gÃ¼ncelleyebilmesi iÃ§in event fÄ±rlatÄ±yoruz.
            // Optimizasyon notu: Ä°stenirse sadece tamsayÄ± deÄŸiÅŸtiÄŸinde (saniyede 1) fÄ±rlatÄ±labilir.
            OnGameTimeUpdated?.Invoke(RemainingTime);

            if (RemainingTime <= 0)
            {
                EndGame();
            }
        }
    }

    #region VR Application Pause Koruması

    
    #region VR Application Pause KorumasÄ±
    
    /// <summary>
    /// Oyuncu VR gÃ¶zlÃ¼ÄŸÃ¼nÃ¼ (Quest vb.) kafasÄ±ndan Ã§Ä±kardÄ±ÄŸÄ±nda veya uygulama arka plana dÃ¼ÅŸtÃ¼ÄŸÃ¼nde Ã§alÄ±ÅŸÄ±r.
    /// SÃ¼renin boÅŸa akmasÄ±nÄ± engeller.
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && CurrentState == GameState.Playing)
        {
            Debug.Log("<color=orange>[GameManager]</color> VR GÃ¶zlÃ¼ÄŸÃ¼ Ã§Ä±karÄ±ldÄ± veya oyun alta alÄ±ndÄ±! Otomatik Pause Devrede.");
            PauseGame();
        }
    }

    /// <summary>
    /// Uygulama odaÄŸÄ± (Focus) kaybettiÄŸinde (Quest arayÃ¼zÃ¼ aÃ§Ä±ldÄ±ÄŸÄ±nda) Ã§alÄ±ÅŸÄ±r.
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && CurrentState == GameState.Playing)
        {
            PauseGame();
        }
    }

    #endregion

    /// <summary>
    /// SÃ¼re dolduÄŸunda veya can bittiÄŸinde Ã§aÄŸrÄ±lÄ±r.
    /// </summary>
    private void EndGame()
    {
        // Oyun sonu istatistiklerini hesapla
        CompileEndgameStats();

        ChangeState(GameState.GameOver);
        // Ä°stenirse burada oyun sonunda yapÄ±lacak Ã¶zel iÅŸlemler Ã§aÄŸrÄ±labilir.
    }

    /// <summary>
    /// Oyun bittiÄŸinde istatistikleri (Ä°sabet OranÄ± vb.) hesaplar.
    /// </summary>
    private void CompileEndgameStats()
    {
        int totalThrows = CurrentSession.TotalCorrectThrows + CurrentSession.TotalIncorrectThrows;

        // 1. İsabet Oranı
        
        // 1. Ä°sabet OranÄ±
        if (totalThrows > 0)
        {
            CurrentSession.AccuracyPercentage = ((float)CurrentSession.TotalCorrectThrows / totalThrows) * 100f;
        }
        else
        {
            CurrentSession.AccuracyPercentage = 0f;
        }

        // 2. Golden Waste Rate
        float goldenWasteRate = 0f;
        int totalGoldenSpawned = CurrentSession.GoldenWastesCollected + CurrentSession.GoldenWastesMissed;
        if (totalGoldenSpawned > 0)
            goldenWasteRate = ((float)CurrentSession.GoldenWastesCollected / totalGoldenSpawned) * 100f;

        // 3. Performans Harf Notu (Grade) ve Kırılımı (Breakdown)
            
        // 3. Performans Harf Notu (Grade) ve KÄ±rÄ±lÄ±mÄ± (Breakdown)
        float comboScore = Mathf.Clamp01((float)CurrentSession.MaxCombo / 20f) * 100f;

        CurrentSession.AccuracyGradeScore = CurrentSession.AccuracyPercentage * 0.40f; // Max 40
        CurrentSession.PrecisionGradeScore = CurrentSession.AveragePrecision * 0.25f;  // Max 25
        CurrentSession.ComboGradeScore = comboScore * 0.20f;                           // Max 20
        CurrentSession.GoldenGradeScore = goldenWasteRate * 0.15f;                     // Max 15
        CurrentSession.TotalGradeScore = CurrentSession.AccuracyGradeScore + CurrentSession.PrecisionGradeScore + CurrentSession.ComboGradeScore + CurrentSession.GoldenGradeScore;

        if (CurrentSession.TotalGradeScore >= 90f) CurrentSession.PerformanceGrade = "Eco Legend";
        else if (CurrentSession.TotalGradeScore >= 80f) CurrentSession.PerformanceGrade = "Master Recycler";
        else if (CurrentSession.TotalGradeScore >= 70f) CurrentSession.PerformanceGrade = "Green Worker";
        else if (CurrentSession.TotalGradeScore >= 50f) CurrentSession.PerformanceGrade = "Clean Rookie";
        else CurrentSession.PerformanceGrade = "Beginner Collector";

        // 4. Ekonomi (XP ve Coin)
        int finalScore = RecycleRush.Core.ScoreManager.Instance != null ? RecycleRush.Core.ScoreManager.Instance.CurrentScore : 0;

        CurrentSession.BaseXP = finalScore / 10;
        // XP Dengelemesi (Min: 25, Max: 500)
        CurrentSession.EarnedXP = Mathf.Clamp(CurrentSession.BaseXP, 25, 500);

        CurrentSession.BaseCoin = CurrentSession.TotalCorrectThrows / 2;
        CurrentSession.GoldenCoin = CurrentSession.GoldenWastesCollected * 20;

        CurrentSession.EarnedCoins = CurrentSession.BaseCoin + CurrentSession.GoldenCoin;
        if (CurrentSession.AccuracyPercentage >= 90f)
        {
            CurrentSession.EarnedCoins += 50; // UstalÄ±k Bonusu
        }

        // 5. Session Efficiency (Oturum Verimliliği)
        
        // 5. Session Efficiency (Oturum VerimliliÄŸi)
        float playMinutes = CurrentSession.TotalPlayTime > 0f ? CurrentSession.TotalPlayTime / 60f : 1f;
        CurrentSession.ScorePerMinute = finalScore / playMinutes;
        CurrentSession.XPPerMinute = CurrentSession.EarnedXP / playMinutes;
        CurrentSession.ThrowsPerMinute = totalThrows / playMinutes;

        // 6. KayÄ±t (Save), Deltalar ve Yeni Rekor KontrolÃ¼
        if (RecycleRush.Managers.SaveManager.Instance != null)
        {
            var saveData = RecycleRush.Managers.SaveManager.Instance.CurrentData;
            bool dataChanged = false;

            // Deltaları hesapla (Güncellemeden önce)
            
            // DeltalarÄ± hesapla (GÃ¼ncellemeden Ã¶nce)
            CurrentSession.ScoreDelta = finalScore - saveData.HighestScore;
            CurrentSession.AccuracyDelta = CurrentSession.AccuracyPercentage - saveData.BestAccuracy;
            CurrentSession.ComboDelta = CurrentSession.MaxCombo - saveData.BestCombo;

            if (CurrentSession.ScoreDelta > 0)
            {
                saveData.HighestScore = finalScore;
                CurrentSession.IsNewHighScore = true;
                dataChanged = true;
            }
            if (CurrentSession.AccuracyDelta > 0)
            {
                saveData.BestAccuracy = CurrentSession.AccuracyPercentage;
                CurrentSession.IsNewBestAccuracy = true;
                dataChanged = true;
            }
            if (CurrentSession.ComboDelta > 0)
            {
                saveData.BestCombo = CurrentSession.MaxCombo;
                CurrentSession.IsNewBestCombo = true;
                dataChanged = true;
            }
            if (CurrentSession.GoldenWastesCollected > saveData.MostGoldenWaste)
            {
                saveData.MostGoldenWaste = CurrentSession.GoldenWastesCollected;
                dataChanged = true;
            }

            // Oyun sonu kazanımlarını kalıcı profile ekle
            saveData.XP += CurrentSession.EarnedXP;
            saveData.Coins += CurrentSession.EarnedCoins;

            // 7. Match History (Maç Geçmişi) Kaydı
            
            // Oyun sonu kazanÄ±mlarÄ±nÄ± kalÄ±cÄ± profile ekle
            saveData.XP += CurrentSession.EarnedXP;
            saveData.Coins += CurrentSession.EarnedCoins;
            
            // 7. Match History (MaÃ§ GeÃ§miÅŸi) KaydÄ±
            var newRecord = new RecycleRush.Managers.MatchHistoryRecord
            {
                Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Score = finalScore,
                Grade = CurrentSession.PerformanceGrade,
                Accuracy = CurrentSession.AccuracyPercentage,
                MaxCombo = CurrentSession.MaxCombo,
                GoldenWastes = CurrentSession.GoldenWastesCollected
            };

            if (saveData.MatchHistory == null) saveData.MatchHistory = new System.Collections.Generic.List<RecycleRush.Managers.MatchHistoryRecord>();

            saveData.MatchHistory.Insert(0, newRecord); // En başa (en yeni) ekle
            
            saveData.MatchHistory.Insert(0, newRecord); // En baÅŸa (en yeni) ekle
            if (saveData.MatchHistory.Count > 10)
            {
                saveData.MatchHistory.RemoveAt(saveData.MatchHistory.Count - 1); // 10'dan fazlasÄ±nÄ± sil
            }

            RecycleRush.Managers.SaveManager.Instance.SaveGame();
        }

        // 8. Next Goal Generator (Sonraki Hedef Üretici)
        
        // 8. Next Goal Generator (Sonraki Hedef Ãœretici)
        if (CurrentSession.AccuracyPercentage < 90f)
            CurrentSession.SuggestedNextGoal = "Reach 90% Accuracy!";
        else if (CurrentSession.MaxCombo < 8)
            CurrentSession.SuggestedNextGoal = "Reach x4 Combo Multiplier! (8 Streak)";
        else if (CurrentSession.GoldenWastesCollected < 5)
            CurrentSession.SuggestedNextGoal = "Catch at least 5 Golden Wastes next time!";
        else if (CurrentSession.ScoreDelta <= 0)
            CurrentSession.SuggestedNextGoal = "Focus on beating your High Score!";
        else
            CurrentSession.SuggestedNextGoal = "You're playing great, keep breaking records!";

        // 9. Performance Medals (Performans Madalyaları)
            
        // 9. Performance Medals (Performans MadalyalarÄ±)
        if (CurrentSession.EarnedMedals == null) CurrentSession.EarnedMedals = new List<string>();
        CurrentSession.EarnedMedals.Clear();

        if (CurrentSession.AccuracyPercentage >= 95f) CurrentSession.EarnedMedals.Add("Precision Medal");
        if (CurrentSession.MaxCombo >= 12) CurrentSession.EarnedMedals.Add("Combo Medal");
        if (CurrentSession.GoldenWastesCollected > 0 && CurrentSession.GoldenWastesMissed == 0) CurrentSession.EarnedMedals.Add("Golden Medal");
        if (CurrentSession.GraceUsedCount == 0 && totalThrows > 10) CurrentSession.EarnedMedals.Add("Survivor Medal");
        if (CurrentSession.ScorePerMinute >= 400f) CurrentSession.EarnedMedals.Add("Efficiency Medal");

        // 10. Session Highlight (Oturumun Öne Çıkan Anı)
        if (CurrentSession.IsNewHighScore)
        
        // 10. Session Highlight (Oturumun Ã–ne Ã‡Ä±kan AnÄ±)
        if (CurrentSession.IsNewHighScore) 
            CurrentSession.SessionHighlight = $"Legendary Run: New High Score of {finalScore}!";
        else if (CurrentSession.MaxCombo >= 15)
            CurrentSession.SessionHighlight = $"Combo Master: {CurrentSession.MaxCombo} Streak";
        else if (CurrentSession.AccuracyPercentage >= 90f)
            CurrentSession.SessionHighlight = $"Precision Run: %{CurrentSession.AccuracyPercentage:F1} Accuracy";
        else if (CurrentSession.GoldenWastesCollected >= 5)
            CurrentSession.SessionHighlight = $"Golden Hunter: {CurrentSession.GoldenWastesCollected} GoldenWastes Caught";
        else if (CurrentSession.GraceUsedCount > 0 && CurrentSession.MaxCombo >= 8)
            CurrentSession.SessionHighlight = $"Recovery Master: Recovered with Grace to reach x4 Multiplier";
        else
            CurrentSession.SessionHighlight = $"Solid Effort: {finalScore} Score";

        string medalsString = CurrentSession.EarnedMedals.Count > 0 ? string.Join(", ", CurrentSession.EarnedMedals) : "None";

        Debug.Log($"<color=cyan>[End of Session Report]</color>\nHighlight: {CurrentSession.SessionHighlight}\nGrade: {CurrentSession.PerformanceGrade} ({CurrentSession.TotalGradeScore:F1}/100) | Accuracy: %{CurrentSession.AccuracyPercentage:F1} | Score: {finalScore} (Delta: {CurrentSession.ScoreDelta})\nEarned XP: {CurrentSession.EarnedXP} | Suggested Goal: {CurrentSession.SuggestedNextGoal}\nMedals Earned: {medalsString}");

        // 11. End Session Snapshot (Immutable Copy)
        // Oyun sonu paneli aÃ§Ä±kken yanlÄ±ÅŸlÄ±kla arka planda bir Ã§Ã¶p Ã§Ã¶p kutusuna dÃ¼ÅŸerse, raporun bozulmamasÄ± iÃ§in veriyi donduruyoruz.
        var snapshot = CurrentSession;
        if (CurrentSession.EarnedMedals != null)
        {
            snapshot.EarnedMedals = new List<string>(CurrentSession.EarnedMedals); // Listeyi derin kopyala (Deep Copy)
        }
        FinalSessionReport = snapshot;
    }

    #region AR Power-Up (Magnet & Hourglass) Injections

    public static event Action<float> OnMagnetStarted;
    public static event Action<float> OnMagnetTimeUpdated;
    public static event Action OnMagnetEnded;
    public static event Action<float> OnHourglassUsed;

    public bool IsMagnetActive { get; private set; }
    public float MagnetRemainingTime { get; private set; }

    public void PrepareToStart()
    {
        if (CurrentState == GameState.MainMenu || CurrentState == GameState.GameOver || CurrentState == GameState.Paused || CurrentState == GameState.Playing)
        {
            float calculatedDuration = _gameDuration;
            if (RecycleRush.Managers.LevelSelectionManager.Instance != null)
    /// <summary>
    /// ButonlarÄ±n arkaya doÄŸru taÅŸ gibi devrilip (Domino etkisi) bir sÃ¼re sonra kaybolmasÄ±nÄ± saÄŸlar.
    /// </summary>
    private System.Collections.IEnumerator HideButtonsRoutine()
    {
        if (buttonsModule == null) yield break;

        // 1) ButonlarÄ±n gerÃ§ek merkezini bul (3. butonun merkezi kaydÄ±rmamasÄ± iÃ§in sadece Play ve Setting baz alÄ±nÄ±r)
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (Transform child in buttonsModule.transform)
        {
            // Sadece isminde Play veya Setting geÃ§en butonlarÄ± merkeze dahil et (Exit butonunu yoksay)
            if (child.name.Contains("Play") || child.name.Contains("Setting"))
            {
                int currentLvl = RecycleRush.Managers.LevelSelectionManager.Instance.CurrentPlayingLevelId;
                calculatedDuration = _gameDuration + ((currentLvl - 1) * 10f);
            }
            RemainingTime = calculatedDuration;
            OnGameTimeUpdated?.Invoke(RemainingTime);

            if (RecycleRush.Core.ScoreManager.Instance != null)
            {
                RecycleRush.Core.ScoreManager.Instance.ResetScore();
            }
        }
        
        if (count > 0) 
            center /= count;
        else 
            center = buttonsModule.transform.position; // Fallback garantisi

        // 2) Devrilme noktasÄ±nÄ± (Pivot) merkezin yarÄ±m metre altÄ± (sanki zemine deÄŸdiÄŸi yer) olarak ayarla
        Vector3 pivotPoint = center + Vector3.down * 0.5f;
        
        // 3) Hangi eksen etrafÄ±nda dÃ¶necek? (Kendi saÄŸÄ±na doÄŸru olan eksen etrafÄ±nda dÃ¶nerse arkaya yatar)
        Vector3 rotationAxis = buttonsModule.transform.right;

        float duration = 1.0f; // 1 saniyede devrilir
        float elapsed = 0f;
        
        float totalAngle = 90f; // Arkaya tam yatmasÄ± iÃ§in 90 derece
        float currentAngle = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // DÃ¼ÅŸme hÄ±zÄ±nÄ± ivmelendirmek iÃ§in t'nin karesini (Ease-In) alÄ±yoruz
            float t = elapsed / duration;
            t = t * t; 
            
            float targetAngle = Mathf.Lerp(0f, totalAngle, t);
            float deltaAngle = targetAngle - currentAngle;
            
            // Objeyi kendi merkezi etrafÄ±nda devir!
            buttonsModule.transform.RotateAround(pivotPoint, rotationAxis, deltaAngle);
            currentAngle = targetAngle;
            
            yield return null;
        }

        // Tamamen yere Ã§arptÄ±ktan sonra oyuncunun bunu algÄ±lamasÄ± iÃ§in 1 saniye yerde beklesin
        yield return new WaitForSeconds(1.0f);

            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnAllToPool();
            }

            Time.timeScale = 1f;
            ChangeState(GameState.Countdown);
        }
    }

    public void ActivateMagnet(float duration)
    {
        IsMagnetActive = true;
        MagnetRemainingTime = duration;
        OnMagnetStarted?.Invoke(duration);
        StartCoroutine(MagnetRoutine());
    }

    private System.Collections.IEnumerator MagnetRoutine()
    {
        while (MagnetRemainingTime > 0)
        {
            MagnetRemainingTime -= Time.deltaTime;
            OnMagnetTimeUpdated?.Invoke(MagnetRemainingTime);
        // Butonlar kaybolduktan sonra Ã§evreyi Arcade animasyonla ortaya Ã§Ä±kar
        StartCoroutine(RevealEnvironmentRoutine());
    }

    /// <summary>
    /// Makine, bant ve kutularÄ± "Pop-up" (bÃ¼yÃ¼yerek ve yaylanarak) Arcade stiliyle ortaya Ã§Ä±karÄ±r.
    /// </summary>
    private System.Collections.IEnumerator RevealEnvironmentRoutine()
    {
        float duration = 0.8f; // Animasyon sÃ¼resi (0.8 saniye Ã§ok dinamik durur)
        float elapsed = 0f;
        
        // EaseOutBack formÃ¼lÃ¼ iÃ§in sabitler (HafifÃ§e 1.0'Ä± geÃ§ip geri dÃ¶ner)
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Ease Out Back matematiÄŸi
            float t_minus_1 = t - 1f;
            float scaleValue = 1f + c3 * Mathf.Pow(t_minus_1, 3f) + c1 * Mathf.Pow(t_minus_1, 2f);

            // Ã‡ok kÃ¼Ã§Ã¼lmesini engellemek iÃ§in min 0 sÄ±nÄ±rla
            if (scaleValue < 0f) scaleValue = 0f;

            Vector3 currentScale = Vector3.one * scaleValue;

            foreach (var module in environmentModules)
            {
                if (module != null)
                {
                    module.transform.localScale = currentScale;
                }
            }

            yield return null;
        }
        IsMagnetActive = false;
        OnMagnetEnded?.Invoke();
    }

    public void AddTime(float seconds)
    {
        if (CurrentState == GameState.Playing)
        {
            RemainingTime += seconds;
            OnGameTimeUpdated?.Invoke(RemainingTime);
            OnHourglassUsed?.Invoke(seconds);
        }
    }

    // --- FRIEND'S ADDED METHODS ---
    public bool IsMagnetActive { get; private set; }
    public float MagnetRemainingTime { get; private set; }
    
    public static event System.Action<float> OnMagnetStarted;
    public static event System.Action<float> OnMagnetTimeUpdated;
    public static event System.Action OnMagnetEnded;
    public static event System.Action<float> OnHourglassUsed;

    public void PrepareToStart()
    {
        UnityEngine.Debug.Log($"<color=white>[GameManager]</color> PrepareToStart Cagirildi!");
        if (CurrentState == GameState.MainMenu || CurrentState == GameState.GameOver) { ChangeState(GameState.Countdown); }
    }

    public void ActivateMagnet(float duration)
    {
        if (CurrentState == GameState.Playing)
        {
            StartCoroutine(MagnetRoutine(duration));
        }
    }

    private System.Collections.IEnumerator MagnetRoutine(float duration)
    {
        IsMagnetActive = true;
        MagnetRemainingTime = duration;
        OnMagnetStarted?.Invoke(duration);

        while (MagnetRemainingTime > 0)
        {
            MagnetRemainingTime -= UnityEngine.Time.deltaTime;
            OnMagnetTimeUpdated?.Invoke(MagnetRemainingTime);
            yield return null;
        }

        MagnetRemainingTime = 0;
        IsMagnetActive = false;
        OnMagnetTimeUpdated?.Invoke(0);
        OnMagnetEnded?.Invoke();
    }
    
    public void TriggerHourglassEvent(float seconds)
    {
        OnHourglassUsed?.Invoke(seconds);
    }
}
    #endregion