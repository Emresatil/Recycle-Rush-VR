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

        private void Awake()
        {
            if (_backgroundImage != null)
            {
                _originalBackgroundColor = _backgroundImage.color;
            }
        }

        private void OnEnable()
        {
            MissionManager.OnMissionProgressUpdated += HandleProgressUpdated;
            MissionManager.OnMissionCompleted += HandleMissionCompleted;
        }

        private void OnDisable()
        {
            MissionManager.OnMissionProgressUpdated -= HandleProgressUpdated;
            MissionManager.OnMissionCompleted -= HandleMissionCompleted;
        }

        private void Start()
        {
            // İlk açılışta mevcut görevi çek
            if (MissionManager.Instance != null && MissionManager.Instance.ActiveMission != null)
            {
                UpdateUI(MissionManager.Instance.ActiveMission);
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
                // Görev bittiğinde kısa süreliğine yeşil yap
                _backgroundImage.color = Color.green;
                
                if (_missionDescriptionText != null) 
                    _missionDescriptionText.text = "MISSION COMPLETED!";
                
                yield return new WaitForSeconds(2f);
                
                // Rengi eski haline döndür
                _backgroundImage.color = _originalBackgroundColor;
            }
        }
    }
}
