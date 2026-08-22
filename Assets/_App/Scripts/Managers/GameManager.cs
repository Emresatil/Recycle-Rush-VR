using System;
using System.Collections.Generic;
using UnityEngine;
using RecycleRush.Managers; // <-- EKLENDİ (AchievementData için)

// Oyunun anlık durumlarını belirten State yapısı
public enum GameState
{
    Initialization,
    MainMenu,
    Placement, // AR ortamında kutuların yerleştirildiği evre
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
    public float AveragePrecision; // Ortalama İsabet Kalitesi (0-100)
    public float BestPrecision;    // O maçtaki en yüksek isabet skoru
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
    
    [Header("Medals")]
    public List<string> EarnedMedals; // Oturum sonu kazanılan madalyalar
}

public class GameManager : MonoBehaviour
{
    // Singleton Pattern: GameManager'a her yerden güvenle ve tek bir instance üzerinden ulaşabilmek için.
    public static GameManager Instance { get; private set; }

    [Header("Session Data")]
    public GameSessionData CurrentSession;
    
    // Oyun sonu ekranı (UI), Analytics veya SaveManager'ın güvenle okuyabileceği Immutable (değiştirilemez) Snapshot
    public GameSessionData FinalSessionReport { get; private set; }

    [Header("Game Timers")]
    [Tooltip("Oyunun toplam süresi (saniye cinsinden)")]
    [SerializeField] private float _gameDuration = 60f;
    
    [Header("UI / Modüller")]
    [Tooltip("Fiziksel butonların bulunduğu modül (Play, Settings vb.)")]
    public GameObject buttonsModule;
    
    [Tooltip("Oyun başladığında animasyonla belirecek çevre modülleri (Boş bırakırsanız otomatik bulur)")]
    public GameObject[] environmentModules;
    
    private Vector3 _buttonsOriginalPos;
    private Quaternion _buttonsOriginalRot;
    private bool _hasSavedButtonsTransform = false;
    private Coroutine _hideButtonsCoroutine;
    
    // Oyun durumunun okunabilmesi ama sadece bu sınıf tarafından değiştirilebilmesi için Property
    public GameState CurrentState { get; private set; }
    public GameState PreviousState { get; private set; }
    
    public float RemainingTime { get; private set; }
    
    [Header("Dalga (Wave) Altyapısı (Skeleton)")]
    public int CurrentWave { get; private set; }
    [SerializeField] private int _maxWave = 5;

    // Event'ler (Olaylar): Spagetti kodu engeller. Diğer sınıflar sadece bu eventleri dinler.
    // Örneğin; UI yöneticisi OnGameStateChanged'i dinler ve GameOver gelince bitiş panelini açar.
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<float> OnGameTimeUpdated;

    private void Awake()
    {
        // Thread-safe / Scene-transition safe Singleton Kurulumu (SOLID Uyumluluğu)
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] Sahnede birden fazla GameManager bulundu, kopya siliniyor.");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        // GameManager'ın sahneler arası referans kaybı yaşamaması için kalıcı yapılması (Önerilen)
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
        Debug.Log("<color=red>[GameManager]</color> Kirlilik maksimuma ulaştı! Oyun sona eriyor.");
        EndGame();
    }

    private void Start()
    {
        // Çevre modülleri inspector'dan atanmadıysa otomatik olarak bul
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

        // Oyun ilk açıldığında hazırlık evresinden geçer, ardından ana menü (veya doğrudan oyun) başlar.
        ChangeState(GameState.Initialization);
        
        // Oyun artık otomatik BAŞLAMAYACAK. 
        // Oyuncunun makinedeki kolu (Lever) çekmesini beklemek için MainMenu (veya bekleme) durumunda kalıyoruz.
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
    }

    private void Update()
    {
        // Zamanlayıcı sadece oyun oynanırken çalışır. (Pause veya GameOver'da durur).
        if (CurrentState == GameState.Playing)
        {
            UpdateTimer();
        }
    }

    /// <summary>
    /// Oyunun durumunu güvenli bir şekilde değiştirir ve diğer sistemlere anons eder.
    /// Mantıksız geçişler (Örn: MainMenu -> Playing) engellenir.
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return; // Zaten o durumdaysak işlem yapma
        
        // State Transition Validation (Geçiş Kontrol Kalkanı - SOLID/Defensive Programming)
        bool isValidTransition = true;
        switch (CurrentState)
        {
            case GameState.MainMenu:
                if (newState == GameState.Playing || newState == GameState.Paused || newState == GameState.GameOver) isValidTransition = false;
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
                if (newState != GameState.Playing && newState != GameState.MainMenu) isValidTransition = false;
                break;
            case GameState.GameOver:
                if (newState != GameState.Countdown && newState != GameState.MainMenu) isValidTransition = false;
                break;
        }

        if (!isValidTransition)
        {
            Debug.LogError($"[GameManager] HATA (Geçersiz Geçiş): {CurrentState} durumundan {newState} durumuna geçilemez!");
            return;
        }

        PreviousState = CurrentState;
        
        // Eski state'den çıkış işlemlerini çalıştır (FSM OnExit)
        OnStateExit(PreviousState);
        
        CurrentState = newState;
        Debug.Log($"[GameManager] Oyun durumu değişti: {PreviousState} -> {CurrentState}");
        
        // Yeni state'e giriş işlemlerini çalıştır (FSM OnEnter)
        OnStateEnter(CurrentState);
        
        // Modülleri Duruma Göre Otomatik Yönet (Butonlar vb.)
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

                // Eğer Ana Menüye dönüldüyse çevreyi tekrar gizle (Scale 0)
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
                // Sadece Butonlar görünür durumdaysa devrilme animasyonu başlat (örn: MainMenu'den gelirsek)
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

        // Durum değişikliğini tüm sisteme yayınla (Broadcast)
        OnGameStateChanged?.Invoke(CurrentState);
    }
    
    #region FSM (Finite State Machine) Lifecycle Fonksiyonları
    
    /// <summary>
    /// Bir duruma (State) girildiğinde sadece bir kez çalışacak olan işlemler.
    /// (Ses, animasyon, ışık vb. sistemlerin yönetimi için idealdir).
    /// </summary>
    private void OnStateEnter(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                // Örn: Müzik başlayabilir, Spawner'a "hızlan" denilebilir.
                break;
            case GameState.Paused:
                // Örn: Çevre sesleri (Ambient) kısılabilir.
                break;
            case GameState.GameOver:
                // Örn: Tüm çöpler dondurulabilir, BGM durdurulabilir.
                break;
        }
    }

    /// <summary>
    /// Bir durumdan (State) çıkıldığında sadece bir kez çalışacak olan temizlik işlemleri.
    /// </summary>
    private void OnStateExit(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                // Ana menüden çıkarken menü sesleri kapatılabilir.
                break;
        }
    }
    
    #endregion

    /// <summary>
    /// Oyunu tamamen sıfırlar ve yeniden başlatır. 
    /// Timer, SessionData, Skor, Kombo, Çöpler ve Dalgalar (Wave) sıfırlanır.
    /// </summary>
    public void RestartGame()
    {
        if (CurrentState == GameState.MainMenu || CurrentState == GameState.GameOver || CurrentState == GameState.Paused)
        {
            // 1. Timer Sıfırla
            RemainingTime = _gameDuration;
            OnGameTimeUpdated?.Invoke(RemainingTime);

            // 2. Session Data Sıfırla
            CurrentSession = new GameSessionData();
            CurrentSession.EarnedMedals = new List<string>(); // Listeyi başlat

            // 3. Skor ve Komboyu Sıfırla
            if (RecycleRush.Core.ScoreManager.Instance != null)
            {
                RecycleRush.Core.ScoreManager.Instance.ResetScore();
            }
            
            // 4. Havuzdaki çöpleri temizle (Eğer ObjectPoolManager varsa)
            // if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.ReturnAllToPool();

            // 5. Motor hızını sıfırla (Paused durumundan geliyorsak zaman durmuş olabilir)
            Time.timeScale = 1f;
            
            // 6. Dalga sistemini (Wave) sıfırla
            CurrentWave = 1;

            // 7. Geri Sayım (Countdown) durumuna geçerek oyunu başlat.
            ChangeState(GameState.Countdown);
        }
    }

    #region Wave Skeleton (İleride Geliştirilecek)
    
    /// <summary>
    /// Yeni bir dalgayı (Wave) başlatır. Zorluk seviyesi (Hız, Spawn süresi) burada artırılabilir.
    /// (Solid: Açık/Kapalı prensibine uygun genişleme alanı)
    /// </summary>
    public void StartWave()
    {
        Debug.Log($"[GameManager] Wave {CurrentWave} Başlıyor!");
        // TODO: Spawner sistemine "Daha hızlı üret" sinyali gönderilebilir.
    }

    /// <summary>
    /// Mevcut dalgayı (Wave) sonlandırır.
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
    /// Oyunu (veya gerekirse öğreticiyi) başlatır.
    /// </summary>
    public void StartGame()
    {
        // Eğitimi hiç tamamlamamışsa (0 ise) veya anahtar yoksa Tutorial'e geç
        if (PlayerPrefs.GetInt("TutorialDone", 0) == 0)
        {
            ChangeState(GameState.Tutorial);
        }
        else
        {
            RemainingTime = _gameDuration;
            ChangeState(GameState.Playing);
        }
    }

    /// <summary>
    /// UIManager geri sayım animasyonunu bitirdiğinde bu fonksiyonu çağırır ve oyunu asıl o zaman başlatır.
    /// </summary>
    public void FinishCountdown()
    {
        RemainingTime = _gameDuration;
        ChangeState(GameState.Playing);
    }

    /// <summary>
    /// Oyunu duraklatır.
    /// </summary>
    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
            Time.timeScale = 0f; // Fizik motorunu ve update sürelerini durdurur
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
            Time.timeScale = 1f; // Motoru tekrar normal hızına getirir
        }
    }

    /// <summary>
    /// Oyunu durdurur veya devam ettirir (VR menü tuşu için idealdir).
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
    /// Geri sayım sistemini günceller.
    /// </summary>
    private void UpdateTimer()
    {
        if (RemainingTime > 0)
        {
            RemainingTime -= Time.deltaTime;
            
            // Eğer süre sıfırın altına düştüyse sıfıra sabitle.
            if (RemainingTime < 0) RemainingTime = 0;

            // UI gibi diğer sistemlerin zamanı saniye saniye güncelleyebilmesi için event fırlatıyoruz.
            // Optimizasyon notu: İstenirse sadece tamsayı değiştiğinde (saniyede 1) fırlatılabilir.
            OnGameTimeUpdated?.Invoke(RemainingTime);

            if (RemainingTime <= 0)
            {
                EndGame();
            }
        }
    }
    
    #region VR Application Pause Koruması
    
    /// <summary>
    /// Oyuncu VR gözlüğünü (Quest vb.) kafasından çıkardığında veya uygulama arka plana düştüğünde çalışır.
    /// Sürenin boşa akmasını engeller.
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && CurrentState == GameState.Playing)
        {
            Debug.Log("<color=orange>[GameManager]</color> VR Gözlüğü çıkarıldı veya oyun alta alındı! Otomatik Pause Devrede.");
            PauseGame();
        }
    }

    /// <summary>
    /// Uygulama odağı (Focus) kaybettiğinde (Quest arayüzü açıldığında) çalışır.
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
    /// Süre dolduğunda veya can bittiğinde çağrılır.
    /// </summary>
    private void EndGame()
    {
        // Oyun sonu istatistiklerini hesapla
        CompileEndgameStats();
        
        ChangeState(GameState.GameOver);
        // İstenirse burada oyun sonunda yapılacak özel işlemler çağrılabilir.
    }

    /// <summary>
    /// Oyun bittiğinde istatistikleri (İsabet Oranı vb.) hesaplar.
    /// </summary>
    private void CompileEndgameStats()
    {
        int totalThrows = CurrentSession.TotalCorrectThrows + CurrentSession.TotalIncorrectThrows;
        
        // 1. İsabet Oranı
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
            CurrentSession.EarnedCoins += 50; // Ustalık Bonusu
        }
        
        // 5. Session Efficiency (Oturum Verimliliği)
        float playMinutes = CurrentSession.TotalPlayTime > 0f ? CurrentSession.TotalPlayTime / 60f : 1f;
        CurrentSession.ScorePerMinute = finalScore / playMinutes;
        CurrentSession.XPPerMinute = CurrentSession.EarnedXP / playMinutes;
        CurrentSession.ThrowsPerMinute = totalThrows / playMinutes;

        // 6. Kayıt (Save), Deltalar ve Yeni Rekor Kontrolü
        if (RecycleRush.Managers.SaveManager.Instance != null)
        {
            var saveData = RecycleRush.Managers.SaveManager.Instance.CurrentData;
            bool dataChanged = false;
            
            // Deltaları hesapla (Güncellemeden önce)
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
            if (saveData.MatchHistory.Count > 10)
            {
                saveData.MatchHistory.RemoveAt(saveData.MatchHistory.Count - 1); // 10'dan fazlasını sil
            }
            
            RecycleRush.Managers.SaveManager.Instance.SaveGame();
        }
        
        // 8. Next Goal Generator (Sonraki Hedef Üretici)
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
        if (CurrentSession.EarnedMedals == null) CurrentSession.EarnedMedals = new List<string>();
        CurrentSession.EarnedMedals.Clear();
        
        if (CurrentSession.AccuracyPercentage >= 95f) CurrentSession.EarnedMedals.Add("Precision Medal");
        if (CurrentSession.MaxCombo >= 12) CurrentSession.EarnedMedals.Add("Combo Medal");
        if (CurrentSession.GoldenWastesCollected > 0 && CurrentSession.GoldenWastesMissed == 0) CurrentSession.EarnedMedals.Add("Golden Medal");
        if (CurrentSession.GraceUsedCount == 0 && totalThrows > 10) CurrentSession.EarnedMedals.Add("Survivor Medal");
        if (CurrentSession.ScorePerMinute >= 400f) CurrentSession.EarnedMedals.Add("Efficiency Medal");
        
        // 10. Session Highlight (Oturumun Öne Çıkan Anı)
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
        // Oyun sonu paneli açıkken yanlışlıkla arka planda bir çöp çöp kutusuna düşerse, raporun bozulmaması için veriyi donduruyoruz.
        var snapshot = CurrentSession;
        if (CurrentSession.EarnedMedals != null)
        {
            snapshot.EarnedMedals = new List<string>(CurrentSession.EarnedMedals); // Listeyi derin kopyala (Deep Copy)
        }
        FinalSessionReport = snapshot;
    }

    /// <summary>
    /// Butonların arkaya doğru taş gibi devrilip (Domino etkisi) bir süre sonra kaybolmasını sağlar.
    /// </summary>
    private System.Collections.IEnumerator HideButtonsRoutine()
    {
        if (buttonsModule == null) yield break;

        // 1) Butonların gerçek merkezini bul (3. butonun merkezi kaydırmaması için sadece Play ve Setting baz alınır)
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (Transform child in buttonsModule.transform)
        {
            // Sadece isminde Play veya Setting geçen butonları merkeze dahil et (Exit butonunu yoksay)
            if (child.name.Contains("Play") || child.name.Contains("Setting"))
            {
                center += child.position;
                count++;
            }
        }
        
        if (count > 0) 
            center /= count;
        else 
            center = buttonsModule.transform.position; // Fallback garantisi

        // 2) Devrilme noktasını (Pivot) merkezin yarım metre altı (sanki zemine değdiği yer) olarak ayarla
        Vector3 pivotPoint = center + Vector3.down * 0.5f;
        
        // 3) Hangi eksen etrafında dönecek? (Kendi sağına doğru olan eksen etrafında dönerse arkaya yatar)
        Vector3 rotationAxis = buttonsModule.transform.right;

        float duration = 1.0f; // 1 saniyede devrilir
        float elapsed = 0f;
        
        float totalAngle = 90f; // Arkaya tam yatması için 90 derece
        float currentAngle = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Düşme hızını ivmelendirmek için t'nin karesini (Ease-In) alıyoruz
            float t = elapsed / duration;
            t = t * t; 
            
            float targetAngle = Mathf.Lerp(0f, totalAngle, t);
            float deltaAngle = targetAngle - currentAngle;
            
            // Objeyi kendi merkezi etrafında devir!
            buttonsModule.transform.RotateAround(pivotPoint, rotationAxis, deltaAngle);
            currentAngle = targetAngle;
            
            yield return null;
        }

        // Tamamen yere çarptıktan sonra oyuncunun bunu algılaması için 1 saniye yerde beklesin
        yield return new WaitForSeconds(1.0f);

        // Son olarak sahneden gizle
        buttonsModule.SetActive(false);
        _hideButtonsCoroutine = null;

        // Butonlar kaybolduktan sonra çevreyi Arcade animasyonla ortaya çıkar
        StartCoroutine(RevealEnvironmentRoutine());
    }

    /// <summary>
    /// Makine, bant ve kutuları "Pop-up" (büyüyerek ve yaylanarak) Arcade stiliyle ortaya çıkarır.
    /// </summary>
    private System.Collections.IEnumerator RevealEnvironmentRoutine()
    {
        float duration = 0.8f; // Animasyon süresi (0.8 saniye çok dinamik durur)
        float elapsed = 0f;
        
        // EaseOutBack formülü için sabitler (Hafifçe 1.0'ı geçip geri döner)
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Ease Out Back matematiği
            float t_minus_1 = t - 1f;
            float scaleValue = 1f + c3 * Mathf.Pow(t_minus_1, 3f) + c1 * Mathf.Pow(t_minus_1, 2f);

            // Çok küçülmesini engellemek için min 0 sınırla
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

        // Animasyon bitince tam %100 (1.0) boyutuna sabitle
        foreach (var module in environmentModules)
        {
            if (module != null)
            {
                module.transform.localScale = Vector3.one;
            }
        }
    }
}
