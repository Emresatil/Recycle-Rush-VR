using UnityEngine;
using TMPro;
using RecycleRush.Core;

namespace RecycleRush.UI
{
    /// <summary>
    /// Bu script eklendiği herhangi bir TextMeshPro objesini otomatik olarak bir "Rekor Tablosuna" dönüştürür.
    /// Cihaz hafızasındaki en yüksek skoru çeker ve altın sarısı renkle ekrana yansıtır.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class RankingBoard : MonoBehaviour
    {
        private TextMeshProUGUI _text;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            // Script aktif olduğunda (veya oyun başladığında) rekoru hemen ekrana yaz
            UpdateHighScoreDisplay();
            
            // Oyun bittiğinde rekor kırılmış olabilir, o anı yakalamak için GameManager'ı dinle
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameState state)
        {
            // Oyun bittiğinde skoru tekrar güncelle
            if (state == GameState.GameOver)
            {
                // ScoreManager rekoru kaydetsin diye çok kısa bir süre bekle (Garantiye almak için)
                Invoke(nameof(UpdateHighScoreDisplay), 0.5f);
            }
        }

        private void UpdateHighScoreDisplay()
        {
            // Cihaz hafızasından rekoru çek (Eğer hiç oynanmamışsa 0 döndürür)
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            
            // Etkileyici bir Arcade tasarımıyla ekrana yazdır (Skor kısmı Altın Sarısı olacak)
            _text.text = $"BEST SCORE\n<color=#FFD700>{highScore}</color>";
        }
    }
}
