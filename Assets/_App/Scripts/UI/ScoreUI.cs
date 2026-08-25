using UnityEngine;
using TMPro; 
using RecycleRush.Core; 

namespace RecycleRush.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))] 
    public class ScoreUI : MonoBehaviour
    {
        [Header("Arayüz Hissiyatı (Juice) Ayarları")]
        [SerializeField, Tooltip("Puan değiştiğinde yazının büyüme katsayısı")] 
        private float _popScaleMultiplier = 1.3f;
        
        [SerializeField, Tooltip("Yazının eski rengine ve boyutuna dönme hızı")] 
        private float _lerpSpeed = 5f;

        private TextMeshProUGUI _scoreText;
        private int _previousScore = 0;
        private CanvasGroup _canvasGroup;
        
        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private Color _targetColor = Color.white;

        private void Awake()
        {
            _scoreText = GetComponent<TextMeshProUGUI>();
            _originalScale = _scoreText.transform.localScale;
            _targetScale = _originalScale;
            
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameState;
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += UpdateScoreDisplay;
                UpdateScoreDisplay(ScoreManager.Instance.CurrentScore);
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
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
            }
        }

        private void Update()
        {
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
            _scoreText.text = $"Score: {newScore}";

            if (_previousScore != 0 || newScore != 0) 
            {
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
