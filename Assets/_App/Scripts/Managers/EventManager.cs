using System;
using UnityEngine;

namespace RecycleRush.Managers
{
    public enum GameEventType
    {
        None,
        SpeedMode,   // Atık düşme hızı/sıklığı artar
        LuckyDrop,   // Altın çöp ihtimali tavan yapar
        SlowMotion,  // Oyun yavaşlar (Zaman bükülmesi)
        DoubleXP,    // 2x XP kazanımı
        DoubleCoins, // 2x Coin kazanımı
        FrenzyMode   // Aşırı seri atık düşmesi
    }

    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

        [Header("Etkinlik Zamanlayıcı Ayarları")]
        [Tooltip("İki etkinlik arasında beklenecek minimum süre (saniye)")]
        [SerializeField] private float _minCooldown = 30f;
        [Tooltip("İki etkinlik arasında beklenecek maksimum süre (saniye)")]
        [SerializeField] private float _maxCooldown = 45f;
        
        [Tooltip("Bir etkinliğin ne kadar süreceği (saniye)")]
        [SerializeField] private float _eventDuration = 15f;

        // Okunabilir Propertiler
        public GameEventType CurrentEvent { get; private set; } = GameEventType.None;
        public bool IsEventActive => CurrentEvent != GameEventType.None;

        // --- GEVŞEK BAĞLILIK (LOOSE COUPLING) İÇİN EVENTLER ---
        public static event Action<GameEventType> OnGameEventStarted;
        public static event Action OnGameEventEnded;

        private float _timer;
        private bool _isCooldown;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this); // Core objesini toptan silmemesi icin sadece scripti siliyoruz;
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Oyuna bekleme süresiyle başla
            StartCooldown();
        }

        private void Update()
        {
            // Sadece oyun oynanırken etkinlik süresi işlesin
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            _timer -= Time.deltaTime;

            if (_timer <= 0f)
            {
                if (_isCooldown)
                {
                    // Bekleme bitti, rastgele bir etkinlik başlat
                    StartRandomEvent();
                }
                else
                {
                    // Etkinlik süresi bitti, normal duruma (beklemeye) geç
                    EndCurrentEvent();
                }
            }
        }

        private void StartCooldown()
        {
            _isCooldown = true;
            _timer = UnityEngine.Random.Range(_minCooldown, _maxCooldown);
            Debug.Log($"<color=gray>[EventManager]</color> Bekleme moduna geçildi. Sonraki etkinlik {_timer:F1} saniye sonra.");
        }

        private void StartRandomEvent()
        {
            _isCooldown = false;
            _timer = _eventDuration;

            // 1 ile 6 arasında rastgele bir etkinlik seç (0 = None)
            Array values = Enum.GetValues(typeof(GameEventType));
            // Length - 1 yapıyoruz ki None (0) gelmesin, indeks 1'den başlasın
            int randomIndex = UnityEngine.Random.Range(1, values.Length);
            CurrentEvent = (GameEventType)values.GetValue(randomIndex);

            Debug.Log($"<color=magenta>[EventManager]</color> YENİ ETKİNLİK BAŞLADI: {CurrentEvent} (Süre: {_eventDuration}s)");

            // Etkinlik SlowMotion ise zamanı hemen yavaşlat (GameManager.Pause etkilememek için dikkatli olmalıyız)
            if (CurrentEvent == GameEventType.SlowMotion)
            {
                Time.timeScale = 0.5f;
            }

            // Diğer sistemlere duyur
            OnGameEventStarted?.Invoke(CurrentEvent);
        }

        private void EndCurrentEvent()
        {
            Debug.Log($"<color=gray>[EventManager]</color> Etkinlik Bitti: {CurrentEvent}");

            // Etkinlik bittiğinde SlowMotion kapat
            if (CurrentEvent == GameEventType.SlowMotion && Time.timeScale > 0f) 
            {
                Time.timeScale = 1f;
            }

            CurrentEvent = GameEventType.None;
            OnGameEventEnded?.Invoke();

            // Yeniden bekleme moduna geç
            StartCooldown();
        }
    }
}
