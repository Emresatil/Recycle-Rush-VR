using System;
using UnityEngine;

// Oyunun anlık durumlarını belirten State yapısı
public enum GameState
{
    Initialization,
    MainMenu,
    ReadyToStart,
    Tutorial,
    Countdown, // Eklendi: UIManager'ın kullandığı geri sayım durumu
    Playing,
    Paused,
    GameOver
}

public class GameManager : MonoBehaviour
{
    // Singleton Pattern: GameManager'a her yerden güvenle ve tek bir instance üzerinden ulaşabilmek için.
    public static GameManager Instance { get; private set; }

    [Header("Oyun Ayarları")]
    [Tooltip("Oyunun toplam süresi (saniye cinsinden)")]
    [SerializeField] private float _gameDuration = 60f;
    
    [Header("AR Odaklı Sistemler")]
    [Tooltip("Odaya rastgele çöp yağdıran Spawner sistemi")]
    public RecycleRush.AR_Features.PortalSpawner wasteSpawner;
    
    // Oyun durumunun okunabilmesi ama sadece bu sınıf tarafından değiştirilebilmesi için Property
    public GameState CurrentState { get; private set; }
    
    public float RemainingTime { get; private set; }

    // Event'ler (Olaylar): Spagetti kodu engeller. Diğer sınıflar sadece bu eventleri dinler.
    // Örneğin; UI yöneticisi OnGameStateChanged'i dinler ve GameOver gelince bitiş panelini açar.
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<float> OnGameTimeUpdated;

    private void Awake()
    {
        // Singleton Kurulumu
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        // GameManager sahneler arası geçişte yok olmasın isteniyorsa aşağıdaki kod açılabilir:
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // GameManager artık çevre modüllerini veya fiziksel butonları gizlemiyor.

        // Oyun ilk açıldığında hazırlık evresinden geçer, ardından ana menü (veya doğrudan oyun) başlar.
        ChangeState(GameState.Initialization);
        
        // Oyun artık otomatik BAŞLAMAYACAK. 
        // Oyuncunun makinedeki kolu (Lever) çekmesini beklemek için MainMenu (veya bekleme) durumunda kalıyoruz.
        ChangeState(GameState.MainMenu);
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
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return; // Zaten o durumdaysak işlem yapma

        CurrentState = newState;
        Debug.Log($"[GameManager] Oyun durumu değişti: {CurrentState}");

        // Durum değişikliğini tüm sisteme yayınla (Broadcast)
        OnGameStateChanged?.Invoke(CurrentState);
    }

    /// <summary>
    /// Play veya Restart butonuna basıldığında sistemi kol çekilmeye (Vardiya başlangıcına) hazırlar.
    /// </summary>
    public void PrepareToStart()
    {
        if (CurrentState == GameState.MainMenu || CurrentState == GameState.GameOver)
        {
            RemainingTime = _gameDuration;
            OnGameTimeUpdated?.Invoke(RemainingTime); // Ekrandaki zaman yazısını anında 60 yap

            if (RecycleRush.Core.ScoreManager.Instance != null)
            {
                RecycleRush.Core.ScoreManager.Instance.ResetScore(); // Skoru ve komboyu sıfırla
            }

            // Kol (Lever) konveyör bandı ile birlikte silindiği için 
            // ReadyToStart yerine direkt Geri Sayım (Countdown) durumuna geçerek oyunu başlat.
            ChangeState(GameState.Countdown);
        }
    }

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
        
        // Geri sayım bittiğinde odaya çöpleri yağdırmaya başla!
        if (wasteSpawner != null)
        {
            wasteSpawner.StartSpawning();
        }
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
            
            if (wasteSpawner != null) wasteSpawner.StopSpawning();
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
            
            if (wasteSpawner != null) wasteSpawner.StartSpawning();
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

    /// <summary>
    /// Süre bittiğinde oyunu bitirir.
    /// </summary>
    private void EndGame()
    {
        ChangeState(GameState.GameOver);
        
        if (wasteSpawner != null) wasteSpawner.StopSpawning();
    }

    // Taşıma bandı ve buton animasyonları AR Room-Scale sistemine geçildiği için silinmiştir.
}
