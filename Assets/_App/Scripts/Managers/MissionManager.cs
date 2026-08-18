using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RecycleRush.Managers
{
    public enum MissionType
    {
        None,
        CollectWaste,  // "5 Tane Cam Atık Topla"
        EarnXP         // "200 XP Kazan"
    }

    [System.Serializable]
    public class MissionData
    {
        public MissionType Type;
        public WasteType TargetWaste; // Sadece CollectWaste için
        public int TargetAmount;
        public int CurrentAmount;
        public string Description;
        public int RewardXP;
        public int RewardCoins;
        public bool IsCompleted => CurrentAmount >= TargetAmount;
    }

    [System.Serializable]
    public struct MissionSaveData
    {
        public bool HasActiveMission;
        public MissionData SavedMission;
    }

    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        public MissionData ActiveMission { get; private set; }

        public static event Action<MissionData> OnMissionProgressUpdated;
        public static event Action<MissionData> OnMissionCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // İlk görevi oluştur
            GenerateMissionForLevel(1);
        }

        private void OnEnable()
        {
            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
        }

        private void OnDisable()
        {
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
        }

        public void GenerateMissionForLevel(int level)
        {
            ActiveMission = new MissionData();
            
            // Rastgele bir görev tipi seç (1 veya 2)
            int randomType = Random.Range(1, 3);
            ActiveMission.Type = (MissionType)randomType;

            if (ActiveMission.Type == MissionType.CollectWaste)
            {
                // Rastgele bir atık türü seç
                Array wasteTypes = Enum.GetValues(typeof(WasteType));
                // Untagged'i hariç tutmak için 0 ile Length-1 arası (Untagged genelde sondaydı, ama rastgele seçelim)
                ActiveMission.TargetWaste = (WasteType)Random.Range(0, 4); // Paper, Glass, Plastic, Metal
                
                ActiveMission.TargetAmount = 3 + (level * 2); // Örn: Lvl 1 -> 5 Tane
                ActiveMission.Description = $"{ActiveMission.TargetAmount} Tane {ActiveMission.TargetWaste} At!";
            }
            else if (ActiveMission.Type == MissionType.EarnXP)
            {
                ActiveMission.TargetAmount = 50 + (level * 50); // Örn: Lvl 1 -> 100 XP
                ActiveMission.Description = $"{ActiveMission.TargetAmount} XP Kazan!";
            }

            ActiveMission.CurrentAmount = 0;
            ActiveMission.RewardXP = 50 * level;
            ActiveMission.RewardCoins = 20 * level;

            Debug.Log($"<color=green>[MissionManager]</color> Yeni Görev Verildi: {ActiveMission.Description}");
            OnMissionProgressUpdated?.Invoke(ActiveMission);
        }

        private void HandleWasteProcessed(SortResultData data)
        {
            if (ActiveMission == null || ActiveMission.IsCompleted) return;

            bool progressMade = false;

            if (ActiveMission.Type == MissionType.CollectWaste)
            {
                // Sadece DOĞRU atışlar ve istenen çöp ise say!
                if (data.IsCorrect && data.ProcessedWasteType == ActiveMission.TargetWaste)
                {
                    ActiveMission.CurrentAmount++;
                    progressMade = true;
                }
            }
            else if (ActiveMission.Type == MissionType.EarnXP)
            {
                // Kazanılan XP'yi göreve ekle
                if (data.XpChange > 0)
                {
                    ActiveMission.CurrentAmount += data.XpChange;
                    progressMade = true;
                }
            }

            if (progressMade)
            {
                // Sınırı aşmamak için Clamp
                ActiveMission.CurrentAmount = Mathf.Clamp(ActiveMission.CurrentAmount, 0, ActiveMission.TargetAmount);
                OnMissionProgressUpdated?.Invoke(ActiveMission);

                if (ActiveMission.IsCompleted)
                {
                    CompleteMission();
                }
            }
        }

        private void CompleteMission()
        {
            Debug.Log($"<color=yellow>[MissionManager]</color> GÖREV TAMAMLANDI: {ActiveMission.Description}");
            
            // Ödülleri ver
            if (LevelManager.Instance != null) LevelManager.Instance.AddXp(ActiveMission.RewardXP);
            if (EconomyManager.Instance != null) EconomyManager.Instance.AddCoins(ActiveMission.RewardCoins);

            OnMissionCompleted?.Invoke(ActiveMission);

            // Kısa bir süre sonra (veya bir sonraki levelde) yeni görev verilebilir.
            // Şu anlık LevelUp eventine bağlı bıraktık, ama anında da yenilenebilir.
        }

        // --- SAVE / LOAD SİSTEMİ ENTEGRASYONU ---
        public MissionSaveData GetSaveData()
        {
            return new MissionSaveData
            {
                HasActiveMission = (ActiveMission != null && !ActiveMission.IsCompleted),
                SavedMission = ActiveMission
            };
        }

        public void LoadSaveData(MissionSaveData data)
        {
            if (data.HasActiveMission && data.SavedMission != null)
            {
                ActiveMission = data.SavedMission;
                OnMissionProgressUpdated?.Invoke(ActiveMission);
                Debug.Log($"<color=green>[MissionManager]</color> Görev yüklendi: {ActiveMission.Description}");
            }
            else
            {
                // Kayıtlı görev yoksa (veya tamamlanmışsa) mevcut levele göre yeni üret
                int currentLvl = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : 1;
                GenerateMissionForLevel(currentLvl);
            }
        }
    }
}
