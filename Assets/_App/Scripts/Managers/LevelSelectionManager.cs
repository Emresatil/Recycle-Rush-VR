using UnityEngine;

namespace RecycleRush.Managers
{
    /// <summary>
    /// Oyundaki 15 seviyelik haritayı (Level Selection Board) ve oyuncunun bu seviyelerdeki ilerlemesini yönetir.
    /// Kilitli seviyeleri, yıldız sistemini ve yeni bir seviye başlatma işlemlerini kontrol eder.
    /// </summary>
    public class LevelSelectionManager : MonoBehaviour
    {
        public static LevelSelectionManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            Debug.Log("<color=green>[LevelSelectionManager]</color> Başlatıldı.");
        }
    }
}
