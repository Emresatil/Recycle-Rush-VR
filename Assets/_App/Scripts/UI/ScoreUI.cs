using UnityEngine;
using TMPro; 
using RecycleRush.Core; 

namespace RecycleRush.UI
{
    /// <summary>
    /// ScoreManager'dan gelen (Event) sinyallerini dinleyip ekrandaki yazıyı güncelleyen UI sınıfı.
    /// Profesyonel Lerp (Matematiksel İnterpolasyon) kullanılarak pürüzsüz animasyonlar sağlanır.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))] // Hata önleyici: Bu script sadece TextMeshPro olan objeye eklenebilir.
    public class ScoreUI : MonoBehaviour
    {
        [Header("Arayüz Hissiyatı (Juice) Ayarları")]
        [SerializeField, Tooltip("Puan değiştiğinde yazının büyüme katsayısı")] 
        private float _popScaleMultiplier = 1.3f;
        
        [SerializeField, Tooltip("Yazının eski rengine ve boyutuna dönme hızı")] 
        private float _lerpSpeed = 5f;

        private TextMeshProUGUI _scoreText;
        private int _previousScore = 0;
        
        // Pürüzsüz (Smooth) geçiş hedefleri
        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private Color _targetColor = Color.white;

        private void Awake()
        {
            // Inspector'dan sürüklemeyi unutmalara karşı güvenli önbellekleme (Caching)
            _scoreText = GetComponent<TextMeshProUGUI>();
            
            _originalScale = _scoreText.transform.localScale;
            _targetScale = _originalScale; // Hedef boyut her zaman orijinal boyuttur
        }

        private void Start()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += UpdateScoreDisplay;
                UpdateScoreDisplay(ScoreManager.Instance.CurrentScore);
            }
        }

        private void OnDisable()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
            }
        }

        private void Update()
        {
            // PROFESYONEL DOKUNUŞ (Lerp): 
            // Coroutine ile kaba saba bekleyip eski haline döndürmek yerine,
            // her karede (frame) yazıyı pürüzsüz bir şekilde (tereyağı gibi) hedef boyutuna ve rengine küçültür.
            if (_scoreText.transform.localScale != _targetScale)
            {
                _scoreText.transform.localScale = Vector3.Lerp(_scoreText.transform.localScale, _targetScale, Time.deltaTime * _lerpSpeed);
            }

            if (_scoreText.color != _targetColor)
            {
                _scoreText.color = Color.Lerp(_scoreText.color, _targetColor, Time.deltaTime * _lerpSpeed);
            }
        }

        private void UpdateScoreDisplay(int newScore)
        {
            _scoreText.text = $"Puan: {newScore}";

            if (_previousScore != 0 || newScore != 0) 
            {
                // Puan geldiğinde (veya düştüğünde) anında rengi ve boyutu patlatıyoruz (Pop).
                // Update() fonksiyonu zaten onu zamanla yavaşça geriye (beyaza ve orijinal boyuta) çekecektir.
                if (newScore < _previousScore)
                {
                    _scoreText.color = Color.red; 
                }
                else if (newScore > _previousScore)
                {
                    _scoreText.color = Color.green; 
                }

                _scoreText.transform.localScale = _originalScale * _popScaleMultiplier;
            }

            _previousScore = newScore;
        }
    }
}
