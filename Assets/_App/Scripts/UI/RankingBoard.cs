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
            // Cihaz hafızasından ilk 3 rekoru çek
            int score1 = PlayerPrefs.GetInt("HighScore1", 0);
            int score2 = PlayerPrefs.GetInt("HighScore2", 0);
            int score3 = PlayerPrefs.GetInt("HighScore3", 0);
            
            // Etkileyici bir Arcade tasarımıyla 3 satır halinde ekrana yazdır (Skor kısmı Altın Sarısı olacak)
            _text.text = $"BEST SCORES\n" +
                         $"<color=#FFD700>1.</color> {score1}\n" +
                         $"<color=#FFD700>2.</color> {score2}\n" +
                         $"<color=#FFD700>3.</color> {score3}";
        }
    }
}
