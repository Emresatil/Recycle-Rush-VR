using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RecycleRush.Managers;
using System.Collections;

namespace RecycleRush.UI
{
    public class XPBarUI : MonoBehaviour
    {
        [Header("UI Referansları")]
        [Tooltip("Dolan XP çubuğu")]
        [SerializeField] private Slider _xpSlider;
        
        [Tooltip("Mevcut seviyeyi yazan metin (Örn: Level 2)")]
        [SerializeField] private TextMeshProUGUI _levelText;

        [Header("Animasyon Ayarları")]
        [Tooltip("Çubuğun dolma hızı")]
        [SerializeField] private float _fillSpeed = 5f;

        private float _targetProgress = 0f;

        private void OnEnable()
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnXpChanged += HandleXpChanged;
                LevelManager.Instance.OnLevelUp += HandleLevelUp;
                
                // Başlangıç değerlerini çek
                UpdateLevelText(LevelManager.Instance.CurrentLevel);
                HandleXpChanged(LevelManager.Instance.CurrentXP, LevelManager.Instance.RequiredXP);
                
                // Barın başlangıçta aniden dolması için Lerp'siz eşitle
                if (_xpSlider != null) _xpSlider.value = _targetProgress;
            }
        }

        private void OnDisable()
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnXpChanged -= HandleXpChanged;
                LevelManager.Instance.OnLevelUp -= HandleLevelUp;
            }
        }

        private void Update()
        {
            // Slider değerini yumuşak bir şekilde hedefe doğru doldur (Lerp)
            if (_xpSlider != null && _xpSlider.value != _targetProgress)
            {
                _xpSlider.value = Mathf.Lerp(_xpSlider.value, _targetProgress, Time.deltaTime * _fillSpeed);
            }
        }

        private void HandleXpChanged(int currentXp, int requiredXp)
        {
            if (requiredXp > 0)
            {
                _targetProgress = (float)currentXp / requiredXp;
            }
        }

        private void HandleLevelUp(int oldLevel, int newLevel)
        {
            UpdateLevelText(newLevel);
            // Seviye atladığında bar sıfırlanır
            _targetProgress = 0f;
            if (_xpSlider != null) _xpSlider.value = 0f; // Anında sıfırla ki geriye doğru lerp olmasın
        }

        private void UpdateLevelText(int level)
        {
            if (_levelText != null)
            {
                _levelText.text = $"Level {level}";
            }
        }
    }
}
