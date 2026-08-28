using UnityEngine;
using RecycleRush.Core;
using RecycleRush.Core.Packages;

namespace RecycleRush.Managers
{
    public class PackageRewardManager : MonoBehaviour
    {
        [Header("Paket Tamamlama Ödülleri")]
        [SerializeField] private int _bonusScore = 25;
        [SerializeField] private int _bonusXP = 10;
        [SerializeField] private float _pollutionReduction = 3f;

        private void OnEnable()
        {
            PackageChallengeTracker.OnPackageCompleted += HandlePackageCompleted;
        }

        private void OnDisable()
        {
            PackageChallengeTracker.OnPackageCompleted -= HandlePackageCompleted;
        }

        private void HandlePackageCompleted()
        {
            // Puan ve XP (Eğer ScoreManager/LevelManager üzerinden yönetiliyorsa)
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(_bonusScore);
                Debug.Log($"<color=#FFD700>[RewardManager]</color> Bonus Skor Eklendi: +{_bonusScore}");
            }

            if (LevelManager.Instance != null && _bonusXP > 0)
            {
                LevelManager.Instance.AddXp(_bonusXP);
                Debug.Log($"<color=#FFD700>[RewardManager]</color> Bonus XP Eklendi: +{_bonusXP}");
            }

            // Kirlilik Temizliği
            if (RoomPollutionManager.Instance != null)
            {
                RoomPollutionManager.Instance.ReducePollution(_pollutionReduction);
                Debug.Log($"<color=#FFD700>[RewardManager]</color> Bonus Temizlik: -{_pollutionReduction}% Kirlilik");
            }
        }
    }
}