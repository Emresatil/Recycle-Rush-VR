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

        [Header("Combo Settings")]
        public int ComboCount { get; private set; }
        public int CurrentMultiplier { get; private set; } = 1;

        // Event: Puan değiştiğinde UI'a haber verecek olan sinyal (C# Action). 
        // Bu sayede UI kodu ile Score kodu birbirine yapışmaz (Decoupling).
        public Action<int> OnScoreChanged;
        
        // Event: Kombo değiştiğinde UI'a haber verecek olan sinyal. Parametreler: (ComboCount, Multiplier)
        public Action<int, int> OnComboChanged;

        // YENİ: En Yüksek İlk 3 Skor Verisi
        public int[] HighScores { get; private set; } = new int[3];

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
            
            // Geriye dönük uyumluluk: Eski sistemden kalan tekli rekoru kaybetmemek için onu 1. sıraya aktarıyoruz
            if (PlayerPrefs.HasKey("HighScore"))
            {
                int oldScore = PlayerPrefs.GetInt("HighScore");
                PlayerPrefs.SetInt("HighScore1", Mathf.Max(oldScore, PlayerPrefs.GetInt("HighScore1", 0)));
                PlayerPrefs.DeleteKey("HighScore"); // Eski anahtarı sil
                PlayerPrefs.Save();
            }

            // Adım 1: Oyun başlarken hafızadaki eski 3 rekoru yükle
            HighScores[0] = PlayerPrefs.GetInt("HighScore1", 0);
            HighScores[1] = PlayerPrefs.GetInt("HighScore2", 0);
            HighScores[2] = PlayerPrefs.GetInt("HighScore3", 0);
        }

        private void Start()
        {
            // Oyun başladığında ilk puanı (sıfır) yayınla ki UI ekranda "0" yazsın.
            OnScoreChanged?.Invoke(CurrentScore);
        }

        private void OnEnable()
        {
            // Gün 9'da yazılan BinTrigger kodundaki statik sinyali (Event) dinlemeye başla
            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
            // Gün 11'de eklenen DestroyZone sinyalini dinlemeye başla (Kaçırılan atıklar)
            DestroyZone.OnWasteMissed += HandleWasteMissed;
            // Anti-Cheat: Yere düşüp zamanı dolan atıklar için FloorZone sinyalini dinle
            RecycleRush.Environment.FloorZone.OnWasteMissedFloor += HandleWasteMissed;
            
            // YENİ: Oyunun durum değişikliklerini dinle (GameOver olduğunda skoru kaydetmek için)
            if (GameManager.Instance != null || true) // GameManager statik event olduğu için direkt sınıftan da dinlenebilir
            {
                GameManager.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            // Memory leak (Bellek sızıntısı) önlemek için obje kapanırken dinlemeyi bırak
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
            DestroyZone.OnWasteMissed -= HandleWasteMissed;
            RecycleRush.Environment.FloorZone.OnWasteMissedFloor -= HandleWasteMissed;
            
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        /// <summary>
        /// GameManager'dan gelen oyun durumu değişikliklerini yakalar.
        /// </summary>
        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                SaveHighScore();
            }
        }

        /// <summary>
        /// Oyun bittiğinde güncel skoru kontrol eder, ilk 3'e girdiyse listeye ekleyip kaydeder.
        /// </summary>
        private void SaveHighScore()
        {
            // Mevcut skoru listeye dahil edip büyükten küçüğe sıralayalım
            int[] allScores = new int[4];
            allScores[0] = HighScores[0];
            allScores[1] = HighScores[1];
            allScores[2] = HighScores[2];
            allScores[3] = CurrentScore;
            
            Array.Sort(allScores);
            Array.Reverse(allScores); // Büyükten küçüğe çevir
            
            bool isNewHighScore = false;
            
            // Sıralanmış listenin sadece ilk 3'ünü kaydet (En düşük 4. skor çöpe gider)
            for (int i = 0; i < 3; i++)
            {
                if (HighScores[i] != allScores[i])
                {
                    isNewHighScore = true;
                }
                HighScores[i] = allScores[i];
                PlayerPrefs.SetInt("HighScore" + (i + 1), HighScores[i]);
            }
            
            if (isNewHighScore)
            {
                PlayerPrefs.Save(); // Veriyi anında diske yazmayı garantiler
                Debug.Log($"<color=green>[ScoreManager]</color> YENİ REKOR! Skorlar güncellendi: {HighScores[0]}, {HighScores[1]}, {HighScores[2]}");
            }
            else
            {
                Debug.Log($"[ScoreManager] Oyun bitti. Skor: {CurrentScore}. İlk 3'e girilemedi.");
            }
        }

        /// <summary>
        /// BinTrigger'dan gelen veri paketini (SortResultData) işler ve puanı günceller.
        /// </summary>
        private void HandleWasteProcessed(SortResultData data)
        {
            if (data.IsCorrect)
            {
                // Doğru atış yapıldıysa komboyu artır
                IncreaseCombo();
                
                // Doğru kutuya atıldıysa puanı katlayıcı ile çarparak ekle
                AddScore(data.ScoreChange * CurrentMultiplier);
            }
            else
            {
                // Yanlış kutuya atıldıysa komboyu sıfırla
                ResetCombo();
                
                // data.ScoreChange eksi bir sayı (-5) olarak geldiği için onu Mathf.Abs ile pozitife çevirip siliyoruz.
                SubtractScore(Mathf.Abs(data.ScoreChange));
            }
        }

        /// <summary>
        /// DestroyZone'dan (Bant Sonu) kaçırılan atıklar için gelen ceza puanını işler.
        /// </summary>
        private void HandleWasteMissed(int penaltyScore)
        {
            // Atık kaçırılırsa kombo anında sıfırlanır
            ResetCombo();
            
            // penaltyScore negatif geldiği için (-5) Mutlak değerini alıyoruz.
            SubtractScore(Mathf.Abs(penaltyScore));
        }

        /// <summary>
        /// Doğru atış yapıldığında komboyu ve katlayıcıyı hesaplar.
        /// </summary>
        private void IncreaseCombo()
        {
            ComboCount++;

            // Katlayıcı (Multiplier) Kuralları
            if (ComboCount >= 5)
            {
                CurrentMultiplier = 3; // 5 ve üzeri doğru atışta x3
            }
            else if (ComboCount >= 3)
            {
                CurrentMultiplier = 2; // 3 doğru atışta x2
            }
            else
            {
                CurrentMultiplier = 1; // Başlangıç durumu
            }

            // UI'a kombonun arttığını haber ver
            OnComboChanged?.Invoke(ComboCount, CurrentMultiplier);
            
            // Eğer oyuncu tam çarpan (multiplier) eşiklerine ulaştıysa KOMBO SESİNİ çal!
            if (ComboCount == 3 || ComboCount == 5)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayComboSound();
                }
            }
            
            Debug.Log($"<color=orange>[ScoreManager]</color> Kombo: {ComboCount} | Çarpan: x{CurrentMultiplier}");
        }

        /// <summary>
        /// Hata yapıldığında (veya atık kaçırıldığında) komboyu sıfırlar.
        /// </summary>
        private void ResetCombo()
        {
            if (ComboCount > 0)
            {
                ComboCount = 0;
                CurrentMultiplier = 1;
                
                // UI'a kombonun sıfırlandığını haber ver
                OnComboChanged?.Invoke(ComboCount, CurrentMultiplier);
                
                Debug.Log("<color=red>[ScoreManager]</color> Kombo Sıfırlandı!");
            }
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
        /// Oyunu yeniden başlatırken skoru ve komboyu sıfırlar.
        /// </summary>
        public void ResetScore()
        {
            CurrentScore = _startingScore;
            ResetCombo();
            OnScoreChanged?.Invoke(CurrentScore);
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
