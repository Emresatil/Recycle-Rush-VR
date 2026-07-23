using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Singleton Instance
    public static AudioManager Instance { get; private set; }

    [Header("BGM Settings")]
    [Tooltip("Fabrika ortamına uygun arkaplan müziği")]
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioClip _bgmClip;
    [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.3f;

    [Header("UI Sounds")]
    [Tooltip("Menü ve buton tıklama sesleri")]
    [SerializeField] private AudioSource _uiSource;
    [SerializeField] private AudioClip _uiClickClip;
    [SerializeField, Range(0f, 1f)] private float _uiVolume = 0.8f;

    [Header("Machine Sounds")]
    [Tooltip("Sürekli çalışan bant/motor sesi için Hoparlör")]
    [SerializeField] private AudioSource _engineSource;
    [Tooltip("Sürekli çalışan motorun MP3 dosyası")]
    [SerializeField] private AudioClip _engineClip;
    [Tooltip("Kolu çektiğimizde çıkacak mekanik ses")]
    [SerializeField] private AudioClip _leverClip;

    [Header("SFX Clips")]
    [Tooltip("Doğru atık atıldığında çalacak (Ding) ses")]
    [SerializeField] private AudioClip _dingClip;
    [Tooltip("Yanlış atık atıldığında çalacak (Buzzer) ses")]
    [SerializeField] private AudioClip _buzzerClip;
    [Tooltip("Obje tutulduğunda (Grab) çalacak ses")]
    [SerializeField] private AudioClip _grabClip;
    [Tooltip("Obje bırakıldığında (Release) çalacak ses")]
    [SerializeField] private AudioClip _releaseClip;
    [Tooltip("Obje yere düştüğünde ve ceza kesildiğinde çalacak ses")]
    [SerializeField] private AudioClip _floorPenaltyClip;
    [Tooltip("Kombo yapıldığında çalacak özel (Combo!) sesi")]
    [SerializeField] private AudioClip _comboClip;

    private void Awake()
    {
        // Singleton (Tekil Örnek) kalıbı kurgusu
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // BGM (Background Music) Başlatma
        if (_bgmSource != null && _bgmClip != null)
        {
            _bgmSource.clip = _bgmClip;
            _bgmSource.volume = _bgmVolume;
            _bgmSource.loop = true;
            _bgmSource.Play();
        }

        // Motor (Engine Hum) Ayarlarını Koddan Yapalım (Kullanıcı arayüzle uğraşmasın)
        if (_engineSource != null && _engineClip != null)
        {
            _engineSource.clip = _engineClip;
            _engineSource.loop = true; // Döngüyü koddan açtık
            _engineSource.Stop(); // Başlangıçta oynamasın (Play On Awake açıksa diye)
        }
    }

    private void OnEnable()
    {
        // BinTrigger'daki spagetti olmayan OnWasteProcessed event'ini dinlemeye başla
        BinTrigger.OnWasteProcessed += HandleWasteProcessed;
        
        // Hakan'ın kurduğu altyapıdaki eventlere abone ol (Motor ve Kol sesleri)
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        MachineLever.OnLeverPulledAction += PlayLeverSound;
    }

    private void OnDisable()
    {
        // Dinlemeyi bırak (Memory Leak önlemi)
        BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        MachineLever.OnLeverPulledAction -= PlayLeverSound;
    }

    /// <summary>
    /// Kutuya atık girdiğinde BinTrigger tarafından gönderilen sinyali yakalar.
    /// </summary>
    private void HandleWasteProcessed(SortResultData data)
    {
        AudioClip clipToPlay = data.IsCorrect ? _dingClip : _buzzerClip;
        
        if (clipToPlay != null && _uiSource != null)
        {
            // 3D ses çok kısıldığı için bunu her yerden duyulabilen 2D Arcade sese çevirdik
            _uiSource.PlayOneShot(clipToPlay, 1.0f);
        }
    }

    /// <summary>
    /// Oyun durumu değiştiğinde Motor sesini (Engine Hum) açıp kapatır.
    /// </summary>
    private void HandleGameStateChanged(GameState state)
    {
        if (_engineSource == null) return;

        if (state == GameState.Playing || state == GameState.Tutorial)
        {
            if (!_engineSource.isPlaying)
                _engineSource.Play();
        }
        else if (state == GameState.GameOver || state == GameState.MainMenu || state == GameState.Initialization)
        {
            if (_engineSource.isPlaying)
                _engineSource.Stop();
        }
    }

    /// <summary>
    /// Hakan'ın yazdığı kol çekilme sinyali geldiğinde mekanik "çotank" sesini çalar.
    /// </summary>
    private void PlayLeverSound()
    {
        if (_uiSource != null && _leverClip != null)
        {
            _uiSource.PlayOneShot(_leverClip, 1.0f);
        }
    }

    /// <summary>
    /// Oyuncu bir objeyi tuttuğunda (Grab) 3D uzayda ses çıkarır.
    /// </summary>
    public void PlayGrabSound(Vector3 position)
    {
        if (_grabClip != null && _uiSource != null)
        {
            _uiSource.PlayOneShot(_grabClip, 1.0f);
        }
    }

    /// <summary>
    /// Oyuncu objeyi fırlattığında/bıraktığında (Release) ses çıkarır.
    /// </summary>
    public void PlayReleaseSound(Vector3 position)
    {
        if (_releaseClip != null && _uiSource != null)
        {
            _uiSource.PlayOneShot(_releaseClip, 1.0f);
        }
    }

    /// <summary>
    /// UI (Arayüz) butonlarına tıklandığında 2D tıklama sesi çıkarır.
    /// Unity Editor üzerinden Butonların OnClick() kısmına bu metodu bağlayabiliriz.
    /// </summary>
    public void PlayUIClick()
    {
        if (_uiSource != null && _uiClickClip != null)
        {
            // PlayOneShot, arkaplanı durdurmadan sadece tek seferlik sesi çalar (2D)
            _uiSource.PlayOneShot(_uiClickClip, _uiVolume);
        }
    }

    public void PlayFloorPenaltySound()
    {
        if (_floorPenaltyClip != null && _uiSource != null)
        {
            _uiSource.PlayOneShot(_floorPenaltyClip, 1.0f);
        }
    }

    /// <summary>
    /// Oyuncu ardışık doğru atışlar yapıp kombo eşiklerine ulaştığında çalacak sesi çıkarır.
    /// </summary>
    public void PlayComboSound()
    {
        if (_comboClip != null && _uiSource != null)
        {
            _uiSource.PlayOneShot(_comboClip, 1.0f);
        }
    }
}
