using UnityEngine;
using RecycleRush.Core;
using System;

namespace RecycleRush.Managers
{
    /// <summary>
    /// Oyuncunun arka arkaya yaptığı doğru atışları (Combo) takip eder ve puan çarpanı (Multiplier) uygular.
    /// </summary>
    public class ComboManager : MonoBehaviour
    {
        public static ComboManager Instance { get; private set; }

        [Header("Combo Settings")]
        public int CurrentCombo { get; private set; } = 0;
        public float CurrentMultiplier { get; private set; } = 1f;

        public static event Action<int, float> OnComboChanged; // UI için event (Combo Sayısı, Çarpan)
        public static event Action OnComboBroken; // Kombo kırıldığında (Yanlış atış vs.)

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null); // DontDestroyOnLoad koruması
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            // İleride BinTrigger veya ScoreManager eventlerine buradan abone olacağız.
        }

        private void OnDisable()
        {
            // Event aboneliklerini kaldır.
        }
        
        public void AddCombo()
        {
            // Kombo artırma mantığı buraya yazılacak.
        }

        public void BreakCombo()
        {
            // Kombo sıfırlama mantığı buraya yazılacak.
        }
    }
}
