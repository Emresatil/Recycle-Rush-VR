using System;
using UnityEngine;

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
    /// Sadece değerleri hesaplar ve sınır aşımlarında diğer sistemlere (Event) haber verir.
    /// </summary>
    public class RoomPollutionManager : MonoBehaviour
    {
        public static RoomPollutionManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Maksimum kirlilik oranı (Örn: 100)")]
        [SerializeField] private float maxPollution = 100f;

        [Header("State Thresholds")]
        [SerializeField] private float mildThreshold = 25f;
        [SerializeField] private float criticalThreshold = 50f;
        [SerializeField] private float dangerThreshold = 75f;

        public float CurrentPollution { get; private set; }
        public PollutionState CurrentState { get; private set; }
        public PollutionStats Stats => _stats;

        private PollutionStats _stats;

        // --- EVENTS ---
        public static event Action<float> OnPollutionChanged; // UI veya barlar için
        public static event Action<PollutionState> OnPollutionStateChanged; // Partikül/Ses sistemleri için
        public static event Action<PollutionStats> OnGameOverTriggered; // GameManager için

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this); // Core objesini toptan silmemesi icin sadece scripti siliyoruz;
                return;
            }
            Instance = this;
            
            ResetPollution();
        }

        private void OnEnable()
        {
            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
        }

        private void OnDisable()
        {
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
        }

        private void HandleWasteProcessed(SortResultData data)
        {
            if (data.IsCorrect)
            {
                Debug.Log("<color=cyan>[RoomPollutionManager]</color> BinTrigger'dan doğru atış sinyali alındı! Kirlilik düşürülüyor...");
                // Sadece DOĞRU kutuya atıldığında odayı temizle (-2 kirlilik).
                // Yanlış atışlarda ceza YOKTUR (SRP ve Game Design kuralı).
                ReducePollution(2f);
            }
        }

        public void AddPollution(float amount)
        {
            if (CurrentState == PollutionState.GameOver) return; // Oyun bittiyse daha da artırma

            CurrentPollution = Mathf.Clamp(CurrentPollution + amount, 0, maxPollution);
            
            _stats.TotalPollutionAdded += amount;
            if (CurrentPollution > _stats.PeakPollution)
            {
                _stats.PeakPollution = CurrentPollution;
            }

            Debug.Log($"<color=orange>[RoomPollutionManager]</color> Kirlilik ARTTI (+{amount}). Mevcut: %{CurrentPollution}");

            OnPollutionChanged?.Invoke(CurrentPollution);
            CheckThresholds();
        }

        public void ReducePollution(float amount)
        {
            if (CurrentState == PollutionState.GameOver) return;

            float prev = CurrentPollution;
            CurrentPollution = Mathf.Clamp(CurrentPollution - amount, 0, maxPollution);
            
            float actualReduction = prev - CurrentPollution;
            _stats.TotalPollutionReduced += actualReduction;

            Debug.Log($"<color=green>[RoomPollutionManager]</color> Kirlilik TEMİZLENDİ (-{actualReduction}). Mevcut: %{CurrentPollution}");

            OnPollutionChanged?.Invoke(CurrentPollution);
            CheckThresholds();
        }

        public void RecordWasteRecovered()
        {
            _stats.WasteRecoveredBeforePenalty++;
        }

        public void ResetPollution()
        {
            CurrentPollution = 0f;
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
                    Debug.Log("<color=red>[RoomPollutionManager]</color> Kirlilik %100 oldu! GAME OVER çağrısı yapılıyor.");
                    OnGameOverTriggered?.Invoke(_stats);
                }
            }
        }
    }
}
