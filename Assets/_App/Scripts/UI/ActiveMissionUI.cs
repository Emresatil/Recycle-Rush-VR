using UnityEngine;
using TMPro;
using RecycleRush.Managers;
using System.Collections;

namespace RecycleRush.UI
{
    public class ActiveMissionUI : MonoBehaviour
    {
        [Header("UI Referansları")]
        [Tooltip("Görev başlığını ve hedefini gösteren metin")]
        [SerializeField] private TextMeshProUGUI _missionDescriptionText;
        
        [Tooltip("Görevin ne kadar ilerlediğini gösteren metin (Örn: 2/5)")]
        [SerializeField] private TextMeshProUGUI _missionProgressText;
        
        [Tooltip("Görev tamamlandığında çıkacak yeşil renk veya animasyon için arkaplan")]
        [SerializeField] private UnityEngine.UI.Image _backgroundImage;

        private Color _originalBackgroundColor;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            if (_backgroundImage != null)
            {
                _originalBackgroundColor = _backgroundImage.color;
            }
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameState;
            MissionManager.OnMissionProgressUpdated += HandleProgressUpdated;
            MissionManager.OnMissionCompleted += HandleMissionCompleted;
            
            if (GameManager.Instance != null)
            {
                HandleGameState(GameManager.Instance.CurrentState);
            }
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameState;
            MissionManager.OnMissionProgressUpdated -= HandleProgressUpdated;
            MissionManager.OnMissionCompleted -= HandleMissionCompleted;
        }

        private void Start()
        {
            if (MissionManager.Instance != null && MissionManager.Instance.ActiveMission != null)
            {
                UpdateUI(MissionManager.Instance.ActiveMission);
            }
            
            if (GameManager.Instance != null)
            {
                HandleGameState(GameManager.Instance.CurrentState);
            }
            else
            {
                HandleGameState(GameState.MainMenu);
            }
        }

        private void HandleProgressUpdated(MissionData data)
        {
            UpdateUI(data);
        }

        private void HandleMissionCompleted(MissionData data)
        {
            UpdateUI(data);
            StartCoroutine(ShowCompletionEffect());
        }

        private void UpdateUI(MissionData data)
        {
            if (_missionDescriptionText != null)
            {
                _missionDescriptionText.text = data.Description;
            }

            if (_missionProgressText != null)
            {
                _missionProgressText.text = $"{data.CurrentAmount} / {data.TargetAmount}";
            }
        }

        private IEnumerator ShowCompletionEffect()
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color = Color.green;
                
                if (_missionDescriptionText != null) 
                    _missionDescriptionText.text = "GÖREV TAMAMLANDI!";
                
                yield return new WaitForSeconds(2f);
                
                _backgroundImage.color = _originalBackgroundColor;
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
