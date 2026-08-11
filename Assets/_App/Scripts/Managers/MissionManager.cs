using UnityEngine;

namespace RecycleRush.Managers
{
    /// <summary>
    /// Oyundaki görev tiplerini belirler.
    /// </summary>
    public enum MissionType
    {
        None,
        CollectWaste,  // "5 Tane Cam Atık Topla"
        EarnXP,        // "200 XP Kazan"
        PlayTime       // "Oyunda 5 Dakika Geçir" vb.
    }

    /// <summary>
    /// Oyuncuya seviye bazlı görevler verir ve ilerlemelerini takip eder.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            Debug.Log("<color=green>[MissionManager]</color> Başlatıldı.");
        }
    }
}
