using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RecycleRush.Core;
using RecycleRush.Environment;

namespace RecycleRush.Managers
{
    public enum AchievementCategory
    {
        Skill,
        Progress,
        Collection,
        Survival
    }

    [Serializable]
    public class AchievementData
    {
        public string Id;
        public string Title;
        public string Description;
        public AchievementCategory Category;
        public int CurrentProgress;
        public int TargetProgress;
        public bool IsUnlocked;
        public bool IsHidden;
        public int RewardXP;
        public int RewardCoin;
        public string UnlockedDate;
    }

    [Serializable]
    public class AchievementSaveData
    {
        public int Version;
        public List<AchievementData> Achievements = new List<AchievementData>();
    }

    public struct AchievementStats
    {
        public float TotalCompletionPercentage;
        public int UnlockedCount;
        public int TotalCount;
        public string LatestUnlockedId;
        public string RarestUnlockedId; // Yerel en nadir (mantıksal olarak en az % oranı olan veya en zor)
    }

    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        public static event Action<AchievementData> OnAchievementUnlocked;
        public static event Action<AchievementData, float> OnAchievementProgress; // YENİ: İlerleme sinyali

        [Header("Settings")]
        [Tooltip("Veritabanı versiyonu. Yeni başarımlar eklendiğinde bunu 1 artırın.")]
        public int DatabaseVersion = 2; // Dil değişikliği için versiyonu 2'ye çektik, JSON güncellenecek.

        public AchievementSaveData CurrentData { get; private set; }
        private string _savePath;

        // Geçici takip verileri (Aynı oyundaki/zincirdeki seri takipler için)
        private int _currentGoldenWasteRunCount = 0;
        private int _currentCorrectThrowStreak = 0;
        
        // Aynı oturumda aynı ilerleme bildiriminin (örn: %50) tekrar tekrar çıkmasını engellemek için
        private HashSet<string> _notifiedThresholds = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            _savePath = Path.Combine(Application.persistentDataPath, "Achievements.json");
            LoadAchievements();
        }

        private void OnEnable()
        {
            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
            ComboManager.OnComboChanged += HandleComboChanged;
            ComboManager.OnComboGraceUsed += HandleGraceUsed;
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
            ComboManager.OnComboChanged -= HandleComboChanged;
            ComboManager.OnComboGraceUsed -= HandleGraceUsed;
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void LoadAchievements()
        {
            if (File.Exists(_savePath))
            {
                try
                {
                    string json = File.ReadAllText(_savePath);
                    CurrentData = JsonUtility.FromJson<AchievementSaveData>(json);
                    
                    if (CurrentData.Version < DatabaseVersion)
                    {
                        MergeWithDefaultDatabase();
                        CurrentData.Version = DatabaseVersion;
                        SaveAchievements();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AchievementManager] Dosya okuma hatası: {e.Message}");
                    InitializeDefaultDatabase();
                }
            }
            else
            {
                InitializeDefaultDatabase();
            }
        }

        public void SaveAchievements()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentData, true);
                File.WriteAllText(_savePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AchievementManager] Kayıt hatası: {e.Message}");
            }
        }

        private void InitializeDefaultDatabase()
        {
            CurrentData = new AchievementSaveData();
            CurrentData.Version = DatabaseVersion;
            
            CurrentData.Achievements = new List<AchievementData>
            {
                new AchievementData { Id = "FirstGoldenTrash", Title = "First Golden Trash", Description = "Sorted your first Golden Trash correctly.", Category = AchievementCategory.Collection, TargetProgress = 1, IsHidden = false, RewardXP = 50, RewardCoin = 10 },
                new AchievementData { Id = "ComboMaster", Title = "Combo Master", Description = "Reached x5 combo multiplier.", Category = AchievementCategory.Skill, TargetProgress = 1, IsHidden = false, RewardXP = 100, RewardCoin = 25 },
                new AchievementData { Id = "Sharpshooter", Title = "Sharpshooter", Description = "Made 50 correct throws in total.", Category = AchievementCategory.Progress, TargetProgress = 50, IsHidden = false, RewardXP = 200, RewardCoin = 50 },
                new AchievementData { Id = "Graceful", Title = "Graceful", Description = "Used the combo grace mechanic.", Category = AchievementCategory.Survival, TargetProgress = 1, IsHidden = false, RewardXP = 50, RewardCoin = 10 },
                new AchievementData { Id = "Recycler100", Title = "Recycler", Description = "Made 100 correct throws in total.", Category = AchievementCategory.Progress, TargetProgress = 100, IsHidden = false, RewardXP = 500, RewardCoin = 100 },
                new AchievementData { Id = "GoldenHunter10", Title = "Golden Hunter", Description = "Collected 10 Golden Trashes.", Category = AchievementCategory.Collection, TargetProgress = 10, IsHidden = false, RewardXP = 300, RewardCoin = 75 },
                new AchievementData { Id = "NoGraceMaster", Title = "Cool-Headed Master", Description = "Reached x5 combo without using grace.", Category = AchievementCategory.Skill, TargetProgress = 1, IsHidden = false, RewardXP = 250, RewardCoin = 50 },
                new AchievementData { Id = "GoldenRain", Title = "Golden Rain", Description = "Caught 3 Golden Trashes in a single run.", Category = AchievementCategory.Collection, TargetProgress = 3, IsHidden = true, RewardXP = 400, RewardCoin = 100 },
                new AchievementData { Id = "LastSecond", Title = "Last Second", Description = "Reached x5 combo in the last 5 seconds.", Category = AchievementCategory.Skill, TargetProgress = 1, IsHidden = true, RewardXP = 500, RewardCoin = 200 },
                new AchievementData { Id = "PerfectStreak", Title = "Perfect Streak", Description = "Made 20 correct throws without using grace.", Category = AchievementCategory.Skill, TargetProgress = 20, IsHidden = true, RewardXP = 1000, RewardCoin = 300 },
                new AchievementData { Id = "FlawlessWave", Title = "Flawless Cleaner", Description = "Completed a wave without making any mistakes.", Category = AchievementCategory.Skill, TargetProgress = 1, IsHidden = false, RewardXP = 150, RewardCoin = 30 },
                new AchievementData { Id = "Wave10", Title = "Wave Conqueror", Description = "Reached Wave 10.", Category = AchievementCategory.Survival, TargetProgress = 10, IsHidden = false, RewardXP = 400, RewardCoin = 100 }
            };
            
            SaveAchievements();
        }

        private void MergeWithDefaultDatabase()
        {
            AchievementSaveData defaultData = new AchievementSaveData();
            List<AchievementData> defaultList = new List<AchievementData>
            {
                new AchievementData { Id = "FirstGoldenTrash", Title = "First Golden Trash", Description = "Sorted your first Golden Trash correctly.", Category = AchievementCategory.Collection, TargetProgress = 1, IsHidden = false, RewardXP = 50, RewardCoin = 10 },
                new AchievementData { Id = "ComboMaster", Title = "Combo Master", Description = "Reached x5 combo multiplier.", Category = AchievementCategory.Skill, TargetProgress = 1, IsHidden = false, RewardXP = 100, RewardCoin = 25 },
                new AchievementData { Id = "Sharpshooter", Title = "Sharpshooter", Description = "Made 50 correct throws in total.", Category = AchievementCategory.Progress, TargetProgress = 50, IsHidden = false, RewardXP = 200, RewardCoin = 50 },
                new AchievementData { Id = "Graceful", Title = "Graceful", Description = "Used the combo grace mechanic.", Category = AchievementCategory.Survival, TargetProgress = 1, IsHidden = false, RewardXP = 50, RewardCoin = 10 },
                new AchievementData { Id = "Recycler100", Title = "Recycler", Description = "Made 100 correct throws in total.", Category = AchievementCategory.Progress, TargetProgress = 100, IsHidden = false, RewardXP = 500, RewardCoin = 100 },
                new AchievementData { Id = "GoldenHunter10", Title = "Golden Hunter", Description = "Collected 10 Golden Trashes.", Category = AchievementCategory.Collection, TargetProgress = 10, IsHidden = false, RewardXP = 300, RewardCoin = 75 },
                new AchievementData { Id = "NoGraceMaster", Title = "Cool-Headed Master", Description = "Reached x5 combo without using grace.", Category = AchievementCategory.Skill, TargetProgress = 1, IsHidden = false, RewardXP = 250, RewardCoin = 50 },
                new AchievementData { Id = "GoldenRain", Title = "Golden Rain", Description = "Caught 3 Golden Trashes in a single run.", Category = AchievementCategory.Collection, TargetProgress = 3, IsHidden = true, RewardXP = 400, RewardCoin = 100 },
                new AchievementData { Id = "LastSecond", Title = "Last Second", Description = "Reached x5 combo in the last 5 seconds.", Category = AchievementCategory.Skill, TargetProgress = 1, IsHidden = true, RewardXP = 500, RewardCoin = 200 },
                new AchievementData { Id = "PerfectStreak", Title = "Perfect Streak", Description = "Made 20 correct throws without using grace.", Category = AchievementCategory.Skill, TargetProgress = 20, IsHidden = true, RewardXP = 1000, RewardCoin = 300 },
                new AchievementData { Id = "FlawlessWave", Title = "Flawless Cleaner", Description = "Completed a wave without making any mistakes.", Category = AchievementCategory.Skill, TargetProgress = 1, IsHidden = false, RewardXP = 150, RewardCoin = 30 },
                new AchievementData { Id = "Wave10", Title = "Wave Conqueror", Description = "Reached Wave 10.", Category = AchievementCategory.Survival, TargetProgress = 10, IsHidden = false, RewardXP = 400, RewardCoin = 100 }
            };

            foreach (var achievement in defaultList)
            {
                var existing = CurrentData.Achievements.Find(a => a.Id == achievement.Id);
                if (existing != null)
                {
                    achievement.CurrentProgress = existing.CurrentProgress;
                    achievement.IsUnlocked = existing.IsUnlocked;
                    achievement.UnlockedDate = existing.UnlockedDate;
                }
            }
            CurrentData.Achievements = defaultList;
        }

        public void AddProgress(string id, int amount = 1)
        {
            var ach = CurrentData.Achievements.Find(a => a.Id == id);
            if (ach == null || ach.IsUnlocked) return;

            float oldPercentage = (float)ach.CurrentProgress / ach.TargetProgress;
            
            ach.CurrentProgress += amount;
            
            float newPercentage = (float)ach.CurrentProgress / ach.TargetProgress;

            if (ach.CurrentProgress >= ach.TargetProgress)
            {
                UnlockAchievement(ach);
            }
            else
            {
                // %50, %75 ve %90 eşiklerini kontrol et
                CheckProgressThreshold(ach, oldPercentage, newPercentage, 0.5f);
                CheckProgressThreshold(ach, oldPercentage, newPercentage, 0.75f);
                CheckProgressThreshold(ach, oldPercentage, newPercentage, 0.9f);
            }
            
            SaveAchievements();
        }

        private void CheckProgressThreshold(AchievementData ach, float oldPerc, float newPerc, float threshold)
        {
            if (oldPerc < threshold && newPerc >= threshold)
            {
                string thresholdKey = ach.Id + "_" + threshold.ToString();
                
                // Eğer bu oturumda bu eşik daha önce bildirilmediyse
                if (!_notifiedThresholds.Contains(thresholdKey))
                {
                    _notifiedThresholds.Add(thresholdKey);
                    OnAchievementProgress?.Invoke(ach, newPerc);
                }
            }
        }

        private void UnlockAchievement(AchievementData ach)
        {
            if (ach.IsUnlocked) return;

            ach.CurrentProgress = ach.TargetProgress;
            ach.IsUnlocked = true;
            ach.UnlockedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            Debug.Log($"<color=yellow>[AchievementManager]</color> Başarım Açıldı: {ach.Title}");
            OnAchievementUnlocked?.Invoke(ach);
            
            SaveAchievements();
        }

        [ContextMenu("Reset All Achievements (Sıfırla)")]
        public void ResetAllAchievements()
        {
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }
            _notifiedThresholds.Clear();
            InitializeDefaultDatabase();
            Debug.Log("<color=green>[AchievementManager]</color> Tüm başarımlar başarıyla sıfırlandı!");
        }

        [ContextMenu("Test - Unlock First Achievement (Test Aç)")]
        public void TestUnlockFirst()
        {
            if (CurrentData != null && CurrentData.Achievements.Count > 0)
            {
                var ach = CurrentData.Achievements[0];
                ach.IsUnlocked = false;
                UnlockAchievement(ach);
            }
        }

        // --- İstatistik (Statistics) Metodu ---
        public AchievementStats GetAchievementStatistics()
        {
            int total = CurrentData.Achievements.Count;
            int unlocked = 0;
            string latest = "";
            DateTime latestDate = DateTime.MinValue;
            
            foreach (var ach in CurrentData.Achievements)
            {
                if (ach.IsUnlocked)
                {
                    unlocked++;
                    if (DateTime.TryParse(ach.UnlockedDate, out DateTime parsedDate))
                    {
                        if (parsedDate > latestDate)
                        {
                            latestDate = parsedDate;
                            latest = ach.Title;
                        }
                    }
                }
            }

            float percentage = total > 0 ? ((float)unlocked / total) * 100f : 0f;

            return new AchievementStats
            {
                TotalCount = total,
                UnlockedCount = unlocked,
                TotalCompletionPercentage = percentage,
                LatestUnlockedId = string.IsNullOrEmpty(latest) ? "Yok" : latest,
                RarestUnlockedId = "Henüz Veri Yok" // İleride Global Cloud verisi gelince entegre edilebilir
            };
        }

        // --- Event Dinleyicileri (Event Listeners) ---

        private void HandleWasteProcessed(SortResultData data)
        {
            if (data.IsCorrect)
            {
                AddProgress("Sharpshooter", 1);
                AddProgress("Recycler100", 1);
                
                _currentCorrectThrowStreak++;
                AddProgress("PerfectStreak", 1);

                if (data.WasGoldenWaste)
                {
                    AddProgress("FirstGoldenTrash", 1);
                    AddProgress("GoldenHunter10", 1);
                    
                    _currentGoldenWasteRunCount++;
                    if (_currentGoldenWasteRunCount >= 3)
                    {
                        AddProgress("GoldenRain", 3);
                    }
                }
            }
            else
            {
                _currentCorrectThrowStreak = 0;
                
                var ps = CurrentData.Achievements.Find(a => a.Id == "PerfectStreak");
                if (ps != null && !ps.IsUnlocked)
                {
                    ps.CurrentProgress = 0;
                    SaveAchievements();
                }
            }
        }

        private void HandleComboChanged(int comboCount, int multiplier, bool isRankUp)
        {
            if (multiplier >= 5)
            {
                AddProgress("ComboMaster", 1);

                if (GameManager.Instance != null && GameManager.Instance.RemainingTime <= 5f && GameManager.Instance.RemainingTime > 0)
                {
                    AddProgress("LastSecond", 1);
                }

                if (ComboManager.Instance != null && !ComboManager.Instance.HasGrace)
                {
                    if (_currentCorrectThrowStreak >= 12)
                    {
                        AddProgress("NoGraceMaster", 1);
                    }
                }
            }
        }

        private void HandleGraceUsed()
        {
            AddProgress("Graceful", 1);
            
            _currentCorrectThrowStreak = 0;
            var ps = CurrentData.Achievements.Find(a => a.Id == "PerfectStreak");
            if (ps != null && !ps.IsUnlocked) { ps.CurrentProgress = 0; }
            SaveAchievements();
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver || state == GameState.MainMenu)
            {
                _currentGoldenWasteRunCount = 0;
                _currentCorrectThrowStreak = 0;
                
                var ps = CurrentData.Achievements.Find(a => a.Id == "PerfectStreak");
                if (ps != null && !ps.IsUnlocked) { ps.CurrentProgress = 0; }
                SaveAchievements();
            }
        }
    }
}
