using System;
using UnityEngine;
using RecycleRush.Managers;

namespace RecycleRush.Core
{
    public enum PollutionState
    {
        Clean,      // 0 - 24%
        Mild,       // 25 - 49%
        Critical,   // 50 - 74%
        Danger,     // 75 - 99%
        GameOver    // 100%
    }

    public struct PollutionStats
    {
        public float PeakPollution;
        public float TotalPollutionAdded;
        public float TotalPollutionReduced;
        public int WasteRecoveredBeforePenalty;
    }

    /// <summary>
    /// Odadaki kirlilik (hayatta kalma/can barı) durumunu takip eden merkezi sistem. (SRP)
    /// Sahnede bulunan aktif çöp sayısına göre kirlilik yüzdesini anlık hesaplar.
    /// Çöpler spawn oldukça kirlilik artar; kutuya atıldıkça/yok oldukça kirlilik düşer.
    /// </summary>
    public class RoomPollutionManager : MonoBehaviour
    {
        public static RoomPollutionManager Instance { get; private set; }

        [Header("Atık Kapasitesi & Kirlilik Ayarları")]
        [Tooltip("Sahnede izin verilen maksimum çöp sayısı (Bu sayıya ulaşıldığında kirlilik %100 olur ve GameOver tetiklenir)")]
        [SerializeField] private int maxWasteCapacity = 15;

        [Tooltip("Maksimum kirlilik oranı (Örn: 100)")]
        [SerializeField] private float maxPollution = 100f;

        [Header("State Thresholds")]
        [SerializeField] private float mildThreshold = 25f;
        [SerializeField] private float criticalThreshold = 50f;
        [SerializeField] private float dangerThreshold = 75f;

        public float CurrentPollution { get; private set; }
        public PollutionState CurrentState { get; private set; }
        public PollutionStats Stats => _stats;
        public int CurrentActiveWasteCount { get; private set; }
        public int MaxWasteCapacity => maxWasteCapacity;

        private PollutionStats _stats;

        // --- EVENTS ---
        public static event Action<float> OnPollutionChanged; // UI veya barlar için
        public static event Action<PollutionState> OnPollutionStateChanged; // Partikül/Ses sistemleri için
        public static event Action<PollutionStats> OnGameOverTriggered; // GameManager için

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            ResetPollution();
        }

        private void OnEnable()
        {
            ObjectPoolManager.OnActiveWasteCountChanged += HandleActiveWasteCountChanged;
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            ObjectPoolManager.OnActiveWasteCountChanged -= HandleActiveWasteCountChanged;
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Update()
        {
            // Oynanış sırasında pool listesindeki gerçek aktif obje sayısını periyodik senkronize et
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                if (ObjectPoolManager.Instance != null)
                {
                    int currentCount = ObjectPoolManager.Instance.ActiveWasteCount;
                    if (currentCount != CurrentActiveWasteCount)
                    {
                        HandleActiveWasteCountChanged(currentCount);
                    }
                }
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.Playing)
            {
                // Oyun başladığında mevcut aktif çöplere göre kirliliği güncelle
                int count = ObjectPoolManager.Instance != null ? ObjectPoolManager.Instance.ActiveWasteCount : 0;
                HandleActiveWasteCountChanged(count);
            }
            else if (newState == GameState.MainMenu || newState == GameState.Placement || newState == GameState.Countdown || newState == GameState.Tutorial)
            {
                // Menüde veya hazırlıkta kirliliği sıfır tut
                ResetPollution();
            }
        }

        /// <summary>
        /// Sahnede bulunan aktif atık sayısı değiştikçe kirlilik yüzdesini anında günceller.
        /// </summary>
        private void HandleActiveWasteCountChanged(int activeCount)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            CurrentActiveWasteCount = Mathf.Max(0, activeCount);
            float targetPollution = Mathf.Clamp(((float)CurrentActiveWasteCount / Mathf.Max(1, maxWasteCapacity)) * maxPollution, 0f, maxPollution);

            SetPollution(targetPollution);
        }

        private void SetPollution(float newPollution)
        {
            if (CurrentState == PollutionState.GameOver && newPollution < maxPollution) return;

            CurrentPollution = newPollution;

            if (CurrentPollution > _stats.PeakPollution)
            {
                _stats.PeakPollution = CurrentPollution;
            }

            OnPollutionChanged?.Invoke(CurrentPollution);
            CheckThresholds();
        }

        public void AddPollution(float amount)
        {
            if (CurrentState == PollutionState.GameOver) return;

            SetPollution(Mathf.Clamp(CurrentPollution + amount, 0f, maxPollution));
            _stats.TotalPollutionAdded += amount;
        }

        public void ReducePollution(float amount)
        {
            if (CurrentState == PollutionState.GameOver) return;

            SetPollution(Mathf.Clamp(CurrentPollution - amount, 0f, maxPollution));
            _stats.TotalPollutionReduced += amount;
        }

        public void RecordWasteRecovered()
        {
            _stats.WasteRecoveredBeforePenalty++;
        }

        public void ResetPollution()
        {
            CurrentPollution = 0f;
            CurrentActiveWasteCount = 0;
            CurrentState = PollutionState.Clean;
            _stats = new PollutionStats();
            
            OnPollutionChanged?.Invoke(CurrentPollution);
            OnPollutionStateChanged?.Invoke(CurrentState);
        }

        private void CheckThresholds()
        {
            PollutionState newState = PollutionState.Clean;

            if (CurrentPollution >= maxPollution)
            {
                newState = PollutionState.GameOver;
            }
            else if (CurrentPollution >= dangerThreshold)
            {
                newState = PollutionState.Danger;
            }
            else if (CurrentPollution >= criticalThreshold)
            {
                newState = PollutionState.Critical;
            }
            else if (CurrentPollution >= mildThreshold)
            {
                newState = PollutionState.Mild;
            }

            if (newState != CurrentState)
            {
                CurrentState = newState;
                OnPollutionStateChanged?.Invoke(CurrentState);

                if (CurrentState == PollutionState.GameOver)
                {
                    Debug.Log("<color=red>[RoomPollutionManager]</color> Kirlilik %100 oldu! (Maksimum atık limitine ulaşıldı). GAME OVER tetikleniyor.");
                    OnGameOverTriggered?.Invoke(_stats);
                }
            }
        }
    }
}
