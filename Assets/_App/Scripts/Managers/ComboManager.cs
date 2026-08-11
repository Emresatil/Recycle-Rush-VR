using UnityEngine;
using RecycleRush.Core;
using System;

namespace RecycleRush.Managers
{
    /// <summary>
    /// Oyuncunun arka arkaya yaptığı doğru atışları (Combo) takip eder ve puan çarpanı (Multiplier) uygular.
    /// Kademeli Rank ve Süre Limitli Combo (Combo Window) sistemlerini barındırır.
    /// </summary>
    public class ComboManager : MonoBehaviour
    {
        public static ComboManager Instance { get; private set; }

        [Header("Combo Settings")]
        [Tooltip("Kombonun kırılması için gereken maksimum eylemsizlik süresi (saniye)")]
        public float comboWindowSeconds = 15f;
        
        public int CurrentCombo { get; private set; } = 0;
        public int CurrentMultiplier { get; private set; } = 1;
        
        // XP ve Coin Çarpanları
        public float CurrentXPMultiplier { get; private set; } = 1f;
        public float CurrentCoinMultiplier { get; private set; } = 1f;
        
        // Kombo Affı (Grace Period)
        public bool HasGrace { get; private set; } = false;
        private bool _hasUsedGraceThisChain = false;

        // UI için event (Combo Sayısı, Çarpan, Kademe Atladı Mı?)
        public static event Action<int, int, bool> OnComboChanged; 
        // Kombo kırıldığında (Yanlış atış veya süre aşımı)
        public static event Action OnComboBroken; 
        // Kombo affı kazanıldığında
        public static event Action OnComboGraceEarned;
        // Kombo affı kullanıldığında (Grace)
        public static event Action OnComboGraceUsed;

        private float _lastThrowTime = 0f;
        private bool _isComboActive = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null); // DontDestroyOnLoad koruması
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            // Atışları ve kaçırılanları dinle
            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
            DestroyZone.OnWasteMissed += HandleWasteMissed;
            RecycleRush.Environment.FloorZone.OnWasteMissedFloor += HandleWasteMissed;
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
            DestroyZone.OnWasteMissed -= HandleWasteMissed;
            RecycleRush.Environment.FloorZone.OnWasteMissedFloor -= HandleWasteMissed;
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Update()
        {
            // Zaman aşımı (Combo Window) Kontrolü
            if (_isComboActive && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                if (Time.time - _lastThrowTime > comboWindowSeconds)
                {
                    Debug.Log($"<color=orange>[ComboManager]</color> {comboWindowSeconds} saniye boyunca atış yapılmadı! Kombo Kırıldı.");
                    BreakCombo();
                }
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver || state == GameState.MainMenu)
            {
                BreakCombo(silent: true);
            }
        }

        private void HandleWasteProcessed(SortResultData data)
        {
            if (data.IsCorrect)
            {
                AddCombo();
            }
            else
            {
                BreakCombo();
            }
        }

        private void HandleWasteMissed(int penalty)
        {
            BreakCombo();
        }
        
        public void AddCombo()
        {
            CurrentCombo++;
            _lastThrowTime = Time.time;
            _isComboActive = true;

            int newMultiplier = CalculateMultiplier(CurrentCombo);
            bool isRankUp = newMultiplier > CurrentMultiplier;
            CurrentMultiplier = newMultiplier;
            
            // XP ve Coin çarpanlarını da merkezi olarak ComboManager'dan hesaplıyoruz
            CurrentXPMultiplier = 1f + (CurrentMultiplier - 1) * 0.5f; // Örn: x2 skorda XP x1.5 olur
            CurrentCoinMultiplier = 1f + (CurrentMultiplier - 1) * 0.25f; // Örn: x2 skorda Coin x1.25 olur

            // SessionData (Oturum Verisi) içerisindeki Max Combo bilgisini otomatik güncelle
            if (GameManager.Instance != null && CurrentCombo > GameManager.Instance.CurrentSession.MaxCombo)
            {
                var session = GameManager.Instance.CurrentSession;
                session.MaxCombo = CurrentCombo;
                GameManager.Instance.CurrentSession = session;
            }

            // Eğer çarpan x4 (veya daha yüksek) ise ve bu zincirde hiç af kullanılmadıysa oyuncuya 1 hata yapma hakkı (Grace) ver
            if (CurrentMultiplier >= 4 && !HasGrace && !_hasUsedGraceThisChain)
            {
                HasGrace = true;
                Debug.Log("<color=yellow>[ComboManager]</color> Kombo Affı (Grace) KAZANILDI!");
                OnComboGraceEarned?.Invoke();
            }

            Debug.Log($"<color=green>[ComboManager]</color> Kombo: {CurrentCombo} | Çarpan: x{CurrentMultiplier} | XP x{CurrentXPMultiplier} | Coin x{CurrentCoinMultiplier}");
            OnComboChanged?.Invoke(CurrentCombo, CurrentMultiplier, isRankUp);
        }

        public void BreakCombo(bool silent = false)
        {
            if (CurrentCombo > 0)
            {
                if (!silent && HasGrace)
                {
                    // Grace kullan
                    HasGrace = false;
                    _hasUsedGraceThisChain = true; // Bu kombo zincirinde bir daha Grace alınamayacak!

                    // Dengeli Düşüş (Step Down):
                    if (CurrentMultiplier >= 5) 
                        CurrentCombo = 8; // x5'ten x4'e düş (8 kombo)
                    else 
                        CurrentCombo = 5; // x4'ten x3'e düş (5 kombo)

                    CurrentMultiplier = CalculateMultiplier(CurrentCombo);
                    
                    CurrentXPMultiplier = 1f + (CurrentMultiplier - 1) * 0.5f;
                    CurrentCoinMultiplier = 1f + (CurrentMultiplier - 1) * 0.25f;
                    
                    Debug.Log($"<color=yellow>[ComboManager]</color> Kombo Affı KULLANILDI! Kombo x{CurrentMultiplier} seviyesine ({CurrentCombo}) düştü.");
                    OnComboGraceUsed?.Invoke();
                    
                    // Rank atlama false olarak güncellemeyi yolla
                    OnComboChanged?.Invoke(CurrentCombo, CurrentMultiplier, false);
                    return;
                }

                if (!silent)
                {
                    Debug.Log("<color=red>[ComboManager]</color> Kombo SIFIRLANDI!");
                    OnComboBroken?.Invoke();
                }
                
                CurrentCombo = 0;
                CurrentMultiplier = 1;
                CurrentXPMultiplier = 1f;
                CurrentCoinMultiplier = 1f;
                HasGrace = false;
                _hasUsedGraceThisChain = false; // Zincir tamamen koptuğu için sıfırla
                _isComboActive = false;
                
                // UI'ı güncellemek için sıfırlanmış veriyi gönder (rank up false)
                OnComboChanged?.Invoke(CurrentCombo, CurrentMultiplier, false);
            }
        }

        private int CalculateMultiplier(int combo)
        {
            // Kademeli Rank Sistemi: 3=x2, 5=x3, 8=x4, 12=x5
            if (combo >= 12) return 5;
            if (combo >= 8) return 4;
            if (combo >= 5) return 3;
            if (combo >= 3) return 2;
            
            return 1;
        }
    }
}
