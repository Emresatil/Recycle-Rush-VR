using System;
using UnityEngine;

namespace RecycleRush.Managers
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Header("Ekonomi Ayarları")]
        [Tooltip("Oyuna başlarken verilecek başlangıç parası")]
        [SerializeField] private int _startingCoins = 0;

        // Okunabilir Properti (Dışarıdan salt okunur)
        public int CurrentCoins { get; private set; }

        // --- GEVŞEK BAĞLILIK (LOOSE COUPLING) İÇİN EVENTLER ---
        // UI (Arayüz) sisteminin Coin sayacını güncellemesi için fırlatılır
        public event Action<int> OnCoinsChanged; 

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this); // Core objesini toptan silmemesi icin sadece scripti siliyoruz;
                return;
            }
            Instance = this;
            
            // Eğer daha önceden yüklenmiş bir kayıt yoksa varsayılan parayı ata
            if (CurrentCoins == 0 && _startingCoins > 0)
            {
                CurrentCoins = _startingCoins;
            }
        }

        private void OnEnable()
        {
            // BinTrigger'dan gelen doğru atış (puan/Coin) sinyallerine abone ol
            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
        }

        private void OnDisable()
        {
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
        }

        /// <summary>
        /// BinTrigger'dan event fırlatıldığında otomatik çalışır.
        /// </summary>
        private void HandleWasteProcessed(SortResultData data)
        {
            // Sadece doğru kutuya atılmışsa Coin ver
            if (data.IsCorrect && data.CoinChange > 0)
            {
                AddCoins(data.CoinChange);
            }
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;

            // Etkinlik aktifse 2 katı Coin kazan!
            if (EventManager.Instance != null && EventManager.Instance.CurrentEvent == GameEventType.DoubleCoins)
            {
                amount *= 2;
                Debug.Log("<color=magenta>[EconomyManager]</color> DOUBLE COINS ETKİNLİĞİ AKTİF! 2 Katı Altın kazanıldı.");
            }

            CurrentCoins += amount;
            Debug.Log($"<color=yellow>[EconomyManager]</color> {amount} Coin kazanıldı! Mevcut Coin: {CurrentCoins}");

            // UI'a bilgi ver
            OnCoinsChanged?.Invoke(CurrentCoins);
        }

        /// <summary>
        /// Market harcamaları vb. durumlar için kullanılır. Başarılı olursa True döner.
        /// </summary>
        public bool SpendCoins(int amount)
        {
            if (amount <= 0 || CurrentCoins < amount) 
            {
                Debug.LogWarning($"<color=orange>[EconomyManager]</color> Yetersiz bakiye! İstenen: {amount}, Mevcut: {CurrentCoins}");
                return false;
            }

            CurrentCoins -= amount;
            Debug.Log($"<color=red>[EconomyManager]</color> {amount} Coin harcandı. Kalan Coin: {CurrentCoins}");

            // UI'a bilgi ver
            OnCoinsChanged?.Invoke(CurrentCoins);
            
            return true;
        }

        // ==========================================
        // 💾 SABRİ EMRE İÇİN SAVE/LOAD YARDIMCILARI
        // ==========================================
        [Serializable]
        public class EconomySaveData
        {
            public int Coins;
        }

        public EconomySaveData GetSaveData()
        {
            return new EconomySaveData
            {
                Coins = this.CurrentCoins
            };
        }

        public void LoadSaveData(EconomySaveData data)
        {
            if (data == null) return;
            
            this.CurrentCoins = data.Coins;
            OnCoinsChanged?.Invoke(CurrentCoins);
        }
    }
}
