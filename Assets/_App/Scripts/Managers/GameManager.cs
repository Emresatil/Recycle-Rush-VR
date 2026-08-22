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
        Debug.Log($"<color=cyan>[GameManager]</color> Awake çalışıyor. Obje adı: {gameObject.name}");
        // Singleton Kurulumu
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"<color=orange>[GameManager]</color> Zaten bir Instance var! Bu kopya ({gameObject.name}) yok ediliyor.");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        Debug.Log($"<color=green>[GameManager]</color> Instance başarıyla atandı: {Instance.gameObject.name}");
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
        Debug.Log($"<color=white>[GameManager]</color> PrepareToStart ÇAĞRILDI! Mevcut Durum: {CurrentState}");
        
        if (CurrentState == GameState.MainMenu || CurrentState == GameState.GameOver)
        {
            Debug.Log("<color=white>[GameManager]</color> Durum uygun, Geri sayım (Countdown) başlatılıyor...");
            
            // EKLENDİ: Dinamik Süre (Level başı 10 saniye ekle)
            float calculatedDuration = _gameDuration;
            if (RecycleRush.Managers.LevelSelectionManager.Instance != null)
            {
                int currentLvl = RecycleRush.Managers.LevelSelectionManager.Instance.CurrentPlayingLevelId;
                // İlk seviye 60 saniye, her seviyede +10 saniye
                calculatedDuration = _gameDuration + ((currentLvl - 1) * 10f);
            }

            RemainingTime = calculatedDuration;
            OnGameTimeUpdated?.Invoke(RemainingTime); // Ekrandaki zaman yazısını anında güncelle

            if (RecycleRush.Core.ScoreManager.Instance != null)
            {
                RecycleRush.Core.ScoreManager.Instance.ResetScore(); // Skoru ve komboyu sıfırla
            }

            // EKLENDİ: Oyun yeniden başladığında yerdeki tüm çöpleri temizle
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnAllToPool();
            }

            // Kol (Lever) konveyör bandı ile birlikte silindiği için 
            // ReadyToStart yerine direkt Geri Sayım (Countdown) durumuna geçerek oyunu başlat.
            ChangeState(GameState.Countdown);
        }
        else
        {
            Debug.LogWarning($"<color=orange>[GameManager]</color> PrepareToStart reddedildi! Sebebi: CurrentState şu an {CurrentState}, ancak MainMenu veya GameOver olması bekleniyor.");
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
            ChangeState(GameState.Playing);
        }
    }

    /// <summary>
    /// UIManager geri sayım animasyonunu bitirdiğinde bu fonksiyonu çağırır ve oyunu asıl o zaman başlatır.
    /// </summary>
    public void FinishCountdown()
    {
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

    // Hourglass efekti için event
    public static event Action<float> OnHourglassUsed;

    /// <summary>
    /// Oyun süresine dışarıdan süre eklemek için kullanılır (Örn: Power-Up)
    /// </summary>
    public void AddTime(float secondsToAdd)
    {
        if (CurrentState == GameState.Playing && RemainingTime > 0)
        {
            RemainingTime += secondsToAdd;
            Debug.Log($"<color=yellow>[GameManager]</color> +{secondsToAdd} saniye eklendi! Yeni süre: {RemainingTime:F1}");
            OnGameTimeUpdated?.Invoke(RemainingTime);
            OnHourglassUsed?.Invoke(secondsToAdd);
        }
    }

    /// <summary>
    /// Mıknatıs (Magnet) gücünün açık olup olmadığını tutar.
    /// </summary>
    public bool IsMagnetActive { get; private set; }
    
    // UI için Magnet kalan süresini tutar
    public float MagnetRemainingTime { get; private set; }

    // Magnet durumları için eventler
    public static event Action<float> OnMagnetStarted;
    public static event Action<float> OnMagnetTimeUpdated;
    public static event Action OnMagnetEnded;

    /// <summary>
    /// Mıknatıs gücünü dışarıdan (Örn: BinTrigger) tetiklemek için çağrılır.
    /// </summary>
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
        Debug.Log($"<color=magenta>[GameManager]</color> Mıknatıs GÜCÜ AKTİF! {duration} saniye sürecek.");
        
        OnMagnetStarted?.Invoke(duration);
        
        while (MagnetRemainingTime > 0)
        {
            MagnetRemainingTime -= Time.deltaTime;
            OnMagnetTimeUpdated?.Invoke(MagnetRemainingTime);
            yield return null;
        }
        
        MagnetRemainingTime = 0;
        IsMagnetActive = false;
        OnMagnetTimeUpdated?.Invoke(0);
        OnMagnetEnded?.Invoke();
        Debug.Log($"<color=magenta>[GameManager]</color> Mıknatıs GÜCÜ BİTTİ!");
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
