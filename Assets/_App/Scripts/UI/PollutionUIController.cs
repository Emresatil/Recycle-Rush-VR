using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RecycleRush.Core;

namespace RecycleRush.UI
{
    public class PollutionUIController : MonoBehaviour
    {
        [Header("UI Referansları")]
        [Tooltip("Kirliliği gösterecek dolum barı (Slider)")]
        [SerializeField] private Slider _pollutionBar;
        
        [Tooltip("Kirlilik yüzdesini gösterecek metin (örn: %25)")]
        [SerializeField] private TextMeshProUGUI _pollutionText;
        
        [Tooltip("Barın rengini değiştirmek için (opsiyonel) Image referansı")]
        [SerializeField] private Image _barFillImage;

        [Header("Renk Ayarları (Opsiyonel)")]
        [SerializeField] private Color _cleanColor = Color.green;
        [SerializeField] private Color _mildColor = Color.yellow;
        [SerializeField] private Color _dangerColor = Color.red;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            if (_pollutionBar != null)
            {
                _pollutionBar.minValue = 0f;
                _pollutionBar.maxValue = 100f;
            }
            UpdateUI(0f);
            
            if (GameManager.Instance != null)
            {
                HandleGameState(GameManager.Instance.CurrentState);
            }
            else
            {
                HandleGameState(GameState.MainMenu);
            }
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameState;
            RoomPollutionManager.OnPollutionChanged += UpdateUI;
            
            if (GameManager.Instance != null)
            {
                HandleGameState(GameManager.Instance.CurrentState);
            }
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameState;
            RoomPollutionManager.OnPollutionChanged -= UpdateUI;
        }

        private void UpdateUI(float currentPollution)
        {
            if (_pollutionBar != null)
            {
                _pollutionBar.value = currentPollution;
            }

            if (_pollutionText != null)
            {
                _pollutionText.text = $"%{Mathf.RoundToInt(currentPollution)}";
            }

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
