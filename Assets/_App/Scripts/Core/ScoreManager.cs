using UnityEngine;
using System;

namespace RecycleRush.Core
{
    /// <summary>
    /// Oyunun puan sistemini yöneten çekirdek sınıf.
    /// Singleton Pattern ve Event-Driven (Olay Güdümlü) mimari kullanılarak yazılmıştır.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        // Singleton Instance - Sahnede sadece 1 tane olmasını garanti eder ve diğer kodların kolayca ulaşmasını sağlar.
        public static ScoreManager Instance { get; private set; }

        [Header("Score Settings")]
        [SerializeField, Tooltip("Oyuna başlanacak varsayılan puan")]
        private int _startingScore = 0;

        // CurrentScore - Dışarıdan okunabilir (get), ama sadece bu sınıfın içinden değiştirilebilir (private set).
        // (Buna Encapsulation / Kapsülleme prensibi denir)
        public int CurrentScore { get; private set; }

        // Event: Puan değiştiğinde UI'a haber verecek olan sinyal (C# Action). 
        // Bu sayede UI kodu ile Score kodu birbirine yapışmaz (Decoupling).
        public Action<int> OnScoreChanged;

        private void Awake()
        {
            // Singleton Kurulumu
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                // Eğer sahneye yanlışlıkla 2. bir ScoreManager eklenirse oyunun çökmesini engeller
                Debug.LogWarning("Sahneye birden fazla ScoreManager eklendi! Fazlalık siliniyor.");
                Destroy(gameObject);
                return;
            }

            CurrentScore = _startingScore;
        }

        private void Start()
        {
            // Oyun başladığında ilk puanı (sıfır) yayınla ki UI ekranda "0" yazsın.
            OnScoreChanged?.Invoke(CurrentScore);
        }

        /// <summary>
        /// Doğru atık eşleşmesinde çağrılır ve puan ekler.
        /// </summary>
        /// <param name="amount">Eklenecek puan miktarı</param>
        public void AddScore(int amount)
        {
            if (amount <= 0) return; // Güvenlik: Koda yanlışlıkla eksi değer girilmesini engeller.

            CurrentScore += amount;
            OnScoreChanged?.Invoke(CurrentScore); // Skoru güncellediğini tüm sisteme duyur
            
            Debug.Log($"[ScoreManager] Başarılı Atış! +{amount} Puan | Toplam: {CurrentScore}");
        }

        /// <summary>
        /// Yanlış atık eşleşmesinde çağrılır ve puan düşer.
        /// </summary>
        /// <param name="amount">Silinecek puan miktarı</param>
        public void SubtractScore(int amount)
        {
            if (amount <= 0) return;

            CurrentScore -= amount;
            
            // Eğer puanın eksiye düşmesini istemiyorsak sınırlandırıyoruz:
            CurrentScore = Mathf.Max(0, CurrentScore); 

            OnScoreChanged?.Invoke(CurrentScore); // Skoru güncellediğini tüm sisteme duyur
            
            Debug.Log($"[ScoreManager] Yanlış Kutu! -{amount} Puan | Toplam: {CurrentScore}");
        }
    }
}
