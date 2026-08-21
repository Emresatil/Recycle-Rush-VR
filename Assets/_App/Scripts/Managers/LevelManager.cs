using System;
using UnityEngine;

namespace RecycleRush.Managers
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Seviye Ayarları")]
        [Tooltip("Başlangıç seviyesi")]
        [SerializeField] private int _startingLevel = 1;
        
        [Tooltip("İlk seviye atlamak için gereken taban XP miktarı")]
        [SerializeField] private int _baseRequiredXp = 100;
        
        [Tooltip("Her seviyede gereken XP'nin ne kadar katlanarak artacağı (Üstel Formül Çarpanı)")]
        [SerializeField] private float _xpMultiplier = 1.5f;

        // Okunabilir Propertiler (Dışarıdan salt okunur)
        public int CurrentLevel { get; private set; }
        public int CurrentXP { get; private set; }
        public int RequiredXP { get; private set; }

        // --- GEVŞEK BAĞLILIK (LOOSE COUPLING) İÇİN EVENTLER ---
        // UI (Arayüz) sisteminin XP çubuğunu güncellemesi için fırlatılır
        public event Action<int, int> OnXpChanged; 
        
        // Zorluk sistemi veya UI'ın seviye atlamayı kutlaması için fırlatılır (eskiSeviye, yeniSeviye)
        public event Action<int, int> OnLevelUp;   

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Eğer daha önceden yüklenmiş bir kayıt yoksa varsayılan ayarlarla başlat
            if (CurrentLevel == 0)
            {
                InitializeDefaults();
            }
        }

        private void OnEnable()
        {
            // BinTrigger'dan gelen doğru atış (puan/XP) sinyallerine abone ol
            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
        }

        private void OnDisable()
        {
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
        }

        private void InitializeDefaults()
        {
            CurrentLevel = _startingLevel;
            CurrentXP = 0;
            CalculateRequiredXp();
        }

        /// <summary>
        /// Üstel (Exponential) formül ile bir sonraki seviye için gereken XP'yi hesaplar.
        /// Formül: TabanXP * (Çarpan ^ (MevcutSeviye - 1))
        /// </summary>
        private void CalculateRequiredXp()
        {
            // Pow = Üs Alma fonksiyonu
            float calculatedXp = _baseRequiredXp * Mathf.Pow(_xpMultiplier, CurrentLevel - 1);
            RequiredXP = Mathf.FloorToInt(calculatedXp);
        }

        /// <summary>
        /// BinTrigger'dan event fırlatıldığında otomatik çalışır.
        /// </summary>
        private void HandleWasteProcessed(SortResultData data)
        {
            // Sadece doğru kutuya atılmışsa XP ver
            if (data.IsCorrect && data.XpChange > 0)
            {
                AddXp(data.XpChange);
            }
        }

        public void AddXp(int amount)
        {
            if (amount <= 0) return;

            CurrentXP += amount;
            Debug.Log($"<color=cyan>[LevelManager]</color> {amount} XP kazanıldı! Mevcut XP: {CurrentXP}/{RequiredXP}");

            // UI'a bilgi ver (Bar dolsun)
            OnXpChanged?.Invoke(CurrentXP, RequiredXP);

            // XP yeterliyse seviye atla
            while (CurrentXP >= RequiredXP)
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            int oldLevel = CurrentLevel;
            
            // Mevcut XP'den harcananı çıkar ve seviyeyi artır
            CurrentXP -= RequiredXP;
            CurrentLevel++;

            // Yeni seviyenin hedefini hesapla
            CalculateRequiredXp();

            Debug.Log($"<color=green>[LevelManager]</color> SEVİYE ATLANDI! Yeni Seviye: {CurrentLevel}. Sonraki Seviye İçin Gereken XP: {RequiredXP}");

            // Sistemlere seviye atlandığını duyur
            OnLevelUp?.Invoke(oldLevel, CurrentLevel);
            OnXpChanged?.Invoke(CurrentXP, RequiredXP);
        }

        // ==========================================
        // 💾 SABRİ EMRE İÇİN SAVE/LOAD YARDIMCILARI
        // ==========================================
        [Serializable]
        public class LevelSaveData
        {
            public int Level;
            public int CurrentXp;
        }

        public LevelSaveData GetSaveData()
        {
            return new LevelSaveData
            {
                Level = this.CurrentLevel,
                CurrentXp = this.CurrentXP
            };
        }

        public void LoadSaveData(LevelSaveData data)
        {
            if (data == null) return;
            
            this.CurrentLevel = Mathf.Max(_startingLevel, data.Level);
            this.CurrentXP = Mathf.Max(0, data.CurrentXp);
            
            CalculateRequiredXp();
            OnXpChanged?.Invoke(CurrentXP, RequiredXP);
        }
    }
}
