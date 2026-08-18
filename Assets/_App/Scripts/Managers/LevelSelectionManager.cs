using System;
using System.Collections.Generic;
using UnityEngine;

namespace RecycleRush.Managers
{
    [Serializable]
    public class LevelData
    {
        public int LevelId;
        public bool IsUnlocked;
        public int StarsEarned; // 0, 1, 2, or 3
    }

    [Serializable]
    public class LevelSelectionSaveData
    {
        public List<LevelData> Levels = new List<LevelData>();
    }

    /// <summary>
    /// Oyundaki 15 seviyelik haritayı (Level Selection Board) ve oyuncunun bu seviyelerdeki ilerlemesini yönetir.
    /// Kilitli seviyeleri, yıldız sistemini ve yeni bir seviye başlatma işlemlerini kontrol eder.
    /// </summary>
    public class LevelSelectionManager : MonoBehaviour
    {
        public static LevelSelectionManager Instance { get; private set; }

        public const int TOTAL_LEVELS = 15;

        // Tıklanılan ve şu an aktif oynanan aşama (Stage) numarası
        public int CurrentPlayingLevelId { get; private set; } = 1;

        private List<LevelData> _levelList = new List<LevelData>();

        // Mevcut oynanan bölüm içindeki anlık istatistikler (Yıldız değerlendirmesi için)
        private int _xpEarnedInCurrentLevel = 0;
        private int _maxComboInCurrentLevel = 0;
        
        // UI kartlarını güncellemek için event
        public static event Action OnLevelDataUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            InitializeDefaultLevels();
            LoadFromPlayerPrefs(); // <-- EKLENDİ: Oyunu açınca kayıtlı seviyeleri yükle
            Debug.Log("<color=green>[LevelSelectionManager]</color> Başlatıldı.");
        }

        private void OnEnable()
        {
            MissionManager.OnMissionCompleted += HandleMissionCompleted;
            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
            BinTrigger.OnComboChanged += HandleComboChanged;
        }

        private void OnDisable()
        {
            MissionManager.OnMissionCompleted -= HandleMissionCompleted;
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
            BinTrigger.OnComboChanged -= HandleComboChanged;
        }

        private void InitializeDefaultLevels()
        {
            _levelList.Clear();
            for (int i = 1; i <= TOTAL_LEVELS; i++)
            {
                _levelList.Add(new LevelData
                {
                    LevelId = i,
                    IsUnlocked = (i == 1), // İlk seviye her zaman açık
                    StarsEarned = 0
                });
            }
        }

        public LevelData GetLevelData(int levelId)
        {
            return _levelList.Find(l => l.LevelId == levelId);
        }

        /// <summary>
        /// Seçilen bölüme girildiğinde çağrılır. İstatistikleri sıfırlar.
        /// </summary>
        public void StartLevel(int levelId)
        {
            LevelData data = GetLevelData(levelId);
            if (data == null || !data.IsUnlocked) return;

            CurrentPlayingLevelId = levelId;
            _xpEarnedInCurrentLevel = 0;
            _maxComboInCurrentLevel = 0;

            // Oyunu/Görevi bu seviyeye göre baştan kur
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.GenerateMissionForLevel(levelId);
            }

            Debug.Log($"<color=cyan>[LevelSelectionManager]</color> Aşama (Stage) {levelId} başladı!");
        }

        // Yıldız değerlendirmesi için verileri topla
        private void HandleWasteProcessed(SortResultData data)
        {
            if (data.IsCorrect && data.XpChange > 0)
            {
                _xpEarnedInCurrentLevel += data.XpChange;
            }
        }

        private void HandleComboChanged(int newCombo)
        {
            if (newCombo > _maxComboInCurrentLevel)
            {
                _maxComboInCurrentLevel = newCombo;
            }
        }

        private void HandleMissionCompleted(MissionData missionData)
        {
            // Görev bitince bölüm (stage) de tamamlanmış sayılıyor
            CompleteCurrentLevel();
        }

        private void CompleteCurrentLevel()
        {
            LevelData data = GetLevelData(CurrentPlayingLevelId);
            if (data == null) return;

            int earnedStars = EvaluateStars(CurrentPlayingLevelId);
            
            if (earnedStars > data.StarsEarned)
            {
                data.StarsEarned = earnedStars;
            }

            Debug.Log($"<color=yellow>[LevelSelectionManager]</color> Aşama {CurrentPlayingLevelId} Tamamlandı! Kazanılan Yıldız: {earnedStars}");

            // Bir sonraki bölümü aç
            UnlockNextLevel(CurrentPlayingLevelId);

            OnLevelDataUpdated?.Invoke();
        }

        private int EvaluateStars(int levelId)
        {
            int stars = 1; // Görevi tamamladığı için (OnMissionCompleted) 1. yıldız garanti

            int targetXp = levelId * 100; // Örn: 3. levelde 300 XP
            if (_xpEarnedInCurrentLevel >= targetXp)
            {
                stars++; // 2. yıldız
            }

            int targetCombo = 3; // En az x3 kombo istiyoruz
            if (_maxComboInCurrentLevel >= targetCombo)
            {
                stars++; // 3. yıldız
            }

            return stars;
        }

        private void UnlockNextLevel(int currentLevelId)
        {
            if (currentLevelId < TOTAL_LEVELS)
            {
                LevelData nextLevel = GetLevelData(currentLevelId + 1);
                if (nextLevel != null && !nextLevel.IsUnlocked)
                {
                    nextLevel.IsUnlocked = true;
                    Debug.Log($"<color=green>[LevelSelectionManager]</color> Yeni Aşama Açıldı: {nextLevel.LevelId}");
                    SaveToPlayerPrefs(); // <-- EKLENDİ: Yeni seviye açılınca hemen kaydet
                }
            }
        }

        // ==========================================
        // 💾 SABRİ EMRE İÇİN SAVE/LOAD YARDIMCILARI + OTOMATİK KAYIT
        // ==========================================
        public LevelSelectionSaveData GetSaveData()
        {
            return new LevelSelectionSaveData { Levels = this._levelList };
        }

        public void LoadSaveData(LevelSelectionSaveData data)
        {
            if (data != null && data.Levels != null && data.Levels.Count > 0)
            {
                this._levelList = data.Levels;
                OnLevelDataUpdated?.Invoke();
            }
        }

        private void SaveToPlayerPrefs()
        {
            LevelSelectionSaveData data = GetSaveData();
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("LevelSelectionProgress", json);
            PlayerPrefs.Save();
            Debug.Log("<color=green>[LevelSelectionManager]</color> Bölüm ilerlemesi diske kaydedildi.");
        }

        private void LoadFromPlayerPrefs()
        {
            if (PlayerPrefs.HasKey("LevelSelectionProgress"))
            {
                string json = PlayerPrefs.GetString("LevelSelectionProgress");
                LevelSelectionSaveData data = JsonUtility.FromJson<LevelSelectionSaveData>(json);
                LoadSaveData(data);
                Debug.Log("<color=green>[LevelSelectionManager]</color> Eski bölüm ilerlemesi yüklendi.");
            }
        }
    }
}
