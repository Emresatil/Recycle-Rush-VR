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
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameState;
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnXpChanged += HandleXpChanged;
                LevelManager.Instance.OnLevelUp += HandleLevelUp;
                
                UpdateLevelText(LevelManager.Instance.CurrentLevel);
                HandleXpChanged(LevelManager.Instance.CurrentXP, LevelManager.Instance.RequiredXP);
                
                if (_xpSlider != null) _xpSlider.value = _targetProgress;
            }
            if (GameManager.Instance != null)
            {
                HandleGameState(GameManager.Instance.CurrentState);
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                HandleGameState(GameManager.Instance.CurrentState);
            }
            else
            {
                HandleGameState(GameState.MainMenu);
            }
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameState;
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnXpChanged -= HandleXpChanged;
                LevelManager.Instance.OnLevelUp -= HandleLevelUp;
            }
        }

        private void Update()
        {
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
            _targetProgress = 0f;
            if (_xpSlider != null) _xpSlider.value = 0f;
        }

        private void UpdateLevelText(int level)
        {
            if (_levelText != null)
            {
                _levelText.text = $"Level {level}";
            }
        }
    
        private void HandleGameState(GameState state)
        {
            bool show = (state == GameState.Playing);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = show ? 1f : 0f;
                _canvasGroup.blocksRaycasts = show;
                _canvasGroup.interactable = show;
            }
        }
    }
}
