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

        private void Start()
        {
            // Unity'de kodların çalışma sırası rastgele olabildiği için, ScoreManager.Instance'ın
            // Awake() içinde hazır olmasını bekliyoruz. Bu yüzden dinlemeye Start() içinde başlıyoruz.
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += UpdateScoreDisplay;
                
                // İlk açılış puanını yazdır.
                UpdateScoreDisplay(ScoreManager.Instance.CurrentScore);
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
