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
    
    [Header("UI / Modüller")]
    [Tooltip("Fiziksel butonların bulunduğu modül (Play, Settings vb.)")]
    public GameObject buttonsModule;
    
    private Vector3 _buttonsOriginalPos;
    private Quaternion _buttonsOriginalRot;
    private bool _hasSavedButtonsTransform = false;
    private Coroutine _hideButtonsCoroutine;
    
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
        
        // Modülleri Duruma Göre Otomatik Yönet (Butonlar vb.)
        if (buttonsModule != null)
        {
            if (!_hasSavedButtonsTransform)
            {
                _buttonsOriginalPos = buttonsModule.transform.position;
                _buttonsOriginalRot = buttonsModule.transform.rotation;
                _hasSavedButtonsTransform = true;
            }

            if (CurrentState == GameState.MainMenu || CurrentState == GameState.GameOver)
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
            }
            else if (CurrentState == GameState.ReadyToStart)
            {
                // Hemen gizlemek yerine devrilme animasyonu başlat
                if (_hideButtonsCoroutine != null) StopCoroutine(_hideButtonsCoroutine);
                _hideButtonsCoroutine = StartCoroutine(HideButtonsRoutine());
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

    /// <summary>
    /// Play butonuna basıldığında sistemi kol çekilmeye (Vardiya başlangıcına) hazırlar.
    /// </summary>
    public void PrepareToStart()
    {
        if (CurrentState == GameState.MainMenu || CurrentState == GameState.GameOver)
        {
            ChangeState(GameState.ReadyToStart);
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

    /// <summary>
    /// Süre bittiğinde oyunu bitirir.
    /// </summary>
    private void EndGame()
    {
        ChangeState(GameState.GameOver);
        // İstenirse burada oyun sonunda yapılacak özel işlemler çağrılabilir.
    }

    /// <summary>
    /// Butonların arkaya doğru taş gibi devrilip (Domino etkisi) bir süre sonra kaybolmasını sağlar.
    /// </summary>
    private System.Collections.IEnumerator HideButtonsRoutine()
    {
        if (buttonsModule == null) yield break;

        // 1) Butonların gerçek merkezini bul (Pivot 0,0,0 olduğu için devrilme bozuluyordu)
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (Transform child in buttonsModule.transform)
        {
            if (child.name != "EventSystem")
            {
                center += child.position;
                count++;
            }
        }
        if (count > 0) center /= count;

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
    }
}
