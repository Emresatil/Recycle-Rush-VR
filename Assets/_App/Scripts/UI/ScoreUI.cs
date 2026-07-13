using UnityEngine;
using TMPro; // TextMeshPro kütüphanesi
using RecycleRush.Core; // ScoreManager'a ulaşmak için

namespace RecycleRush.UI
{
    /// <summary>
    /// ScoreManager'dan gelen (Event) sinyallerini dinleyip ekrandaki yazıyı güncelleyen UI sınıfı.
    /// </summary>
    public class ScoreUI : MonoBehaviour
    {
        [SerializeField, Tooltip("Ekranda puanı gösterecek olan TextMeshPro bileşeni")]
        private TextMeshProUGUI _scoreText;

        private void OnEnable()
        {
            // ScoreManager'daki "OnScoreChanged" sinyalini dinlemeye başla ve 
            // sinyal gelince UpdateScoreDisplay fonksiyonunu çalıştır!
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += UpdateScoreDisplay;
            }
        }

        private void OnDisable()
        {
            // Dinlemeyi bırak (Memory Leak - Bellek Sızıntısı önlemi)
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
            }
        }

        private void Start()
        {
            // İlk açılışta ekranda "New Text" yazmaması için manuel olarak ilk puanı (0) yazdır.
            if (ScoreManager.Instance != null && _scoreText != null)
            {
                UpdateScoreDisplay(ScoreManager.Instance.CurrentScore);
            }
        }

        /// <summary>
        /// Sinyal geldiğinde tetiklenen fonksiyon.
        /// </summary>
        private void UpdateScoreDisplay(int newScore)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"Puan: {newScore}";
            }
        }
    }
}
