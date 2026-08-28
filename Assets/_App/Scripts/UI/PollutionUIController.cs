using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RecycleRush.Core;

namespace RecycleRush.UI
{
    /// <summary>
    /// Kirlilik oranını holografik bir barda (Slider) ve yazıda (Text) gösterir.
    /// RoomPollutionManager.OnPollutionChanged event'ini dinler.
    /// </summary>
    public class PollutionUIController : MonoBehaviour
    {
        [Header("UI Referansları")]
        [Tooltip("Kirliliği gösterecek dolum barı (Slider)")]
        [SerializeField] private Slider _pollutionBar;
        
        [Tooltip("Kirlilik yüzdesini gösterecek metin (örn: %25)")]
        [SerializeField] private TextMeshProUGUI _pollutionText;
        
        [Tooltip("Barın rengini değiştirmek için (opsiyonel) Image referansı")]
        [SerializeField] private Image _barFillImage;

        [Header("Metin Formatı")]
        [Tooltip("Kirlilik başlığı (Örn: 'Pollution: %')")]
        [SerializeField] private string _labelPrefix = "Pollution: %";

        [Header("Renk Ayarları (Opsiyonel)")]
        [SerializeField] private Color _cleanColor = Color.green;
        [SerializeField] private Color _mildColor = Color.yellow;
        [SerializeField] private Color _dangerColor = Color.red;

        private void Awake()
        {
            if (string.IsNullOrEmpty(_labelPrefix) || _labelPrefix.ToUpper().Contains("KIRLILIK") || _labelPrefix.ToUpper().Contains("KİRLİLİK"))
            {
                _labelPrefix = "Pollution: %";
            }
        }

        private void Start()
        {
            if (_pollutionBar != null)
            {
                // Max değeri koddan sabitle (Normalde 100)
                _pollutionBar.minValue = 0f;
                _pollutionBar.maxValue = 100f; // Varsayılan max pollution
            }
            
            // Başlangıç değerini sıfırla
            UpdateUI(0f);
        }

        private void OnEnable()
        {
            RoomPollutionManager.OnPollutionChanged += UpdateUI;
        }

        private void OnDisable()
        {
            RoomPollutionManager.OnPollutionChanged -= UpdateUI;
        }

        private void UpdateUI(float currentPollution)
        {
            // Barı güncelle
            if (_pollutionBar != null)
            {
                _pollutionBar.value = currentPollution;
            }

            // Yazıyı güncelle
            if (_pollutionText != null)
            {
                // "Pollution: %25" gibi anlaşılır şekilde göster
                _pollutionText.text = $"{_labelPrefix}{Mathf.RoundToInt(currentPollution)}";
            }

            // Opsiyonel: Tehlikeye göre barın rengini değiştir
            if (_barFillImage != null)
            {
                if (currentPollution < 50f)
                {
                    _barFillImage.color = _cleanColor;
                }
                else if (currentPollution < 75f)
                {
                    _barFillImage.color = _mildColor;
                }
                else
                {
                    _barFillImage.color = _dangerColor;
                }
            }
        }
    }
}
