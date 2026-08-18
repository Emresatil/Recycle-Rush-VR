using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RecycleRush.Managers;

namespace RecycleRush.UI
{
    /// <summary>
    /// Seviye seçim panosundaki tek bir seviye butonunu (kartını) yönetir.
    /// Kilit durumu, seviye numarası ve yıldız sayısı gibi arayüz güncellemelerini sağlar.
    /// </summary>
    public class LevelCardUI : MonoBehaviour
    {
        [Header("Seviye Ayarları")]
        [Tooltip("Bu kartın temsil ettiği seviye numarası (Örn: 1'den 15'e kadar)")]
        [SerializeField] private int _levelNumber;

        [Header("UI Referansları")]
        [SerializeField] private Button _levelButton;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private GameObject _lockedOverlay;
        
        [Header("Yıldız İkonları")]
        [Tooltip("Sırasıyla: 1. Yıldız, 2. Yıldız, 3. Yıldız ikonları (Aktif/Pasif durumları yönetilecek)")]
        [SerializeField] private GameObject[] _starIcons;
        [SerializeField] private Color _earnedStarColor = Color.yellow;
        [SerializeField] private Color _emptyStarColor = Color.gray;

        private void OnEnable()
        {
            if (_levelButton != null)
            {
                _levelButton.onClick.RemoveListener(OnLevelButtonClicked);
                _levelButton.onClick.AddListener(OnLevelButtonClicked);
            }
            LevelSelectionManager.OnLevelDataUpdated -= UpdateUI;
            LevelSelectionManager.OnLevelDataUpdated += UpdateUI;
            
            // EKLENDİ: Panel her açıldığında (SetActive(true) olduğunda) güncel durumu kontrol et
            UpdateUI();
        }

        private void OnDisable()
        {
            if (_levelButton != null)
            {
                _levelButton.onClick.RemoveListener(OnLevelButtonClicked);
            }
            LevelSelectionManager.OnLevelDataUpdated -= UpdateUI;
        }

        private void Start()
        {
            if (_levelText != null)
            {
                _levelText.text = _levelNumber.ToString();
            }
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (LevelSelectionManager.Instance == null) return;

            LevelData data = LevelSelectionManager.Instance.GetLevelData(_levelNumber);
            if (data == null) return;

            // Kilit durumu ve buton tıklanabilirliği
            if (_lockedOverlay != null)
            {
                _lockedOverlay.SetActive(!data.IsUnlocked);
            }
            
            if (_levelButton != null)
            {
                _levelButton.interactable = data.IsUnlocked;
            }

            // Yıldızları güncelle
            UpdateStars(data.StarsEarned);
        }

        private void UpdateStars(int earnedStars)
        {
            if (_starIcons == null || _starIcons.Length == 0) return;

            for (int i = 0; i < _starIcons.Length; i++)
            {
                if (_starIcons[i] != null)
                {
                    // İkonun rengini kazanılan yıldız sayısına göre belirle (Image bileşenini arıyoruz)
                    Image starImg = _starIcons[i].GetComponent<Image>();
                    if (starImg != null)
                    {
                        starImg.color = (i < earnedStars) ? _earnedStarColor : _emptyStarColor;
                    }
                }
            }
        }

        private void OnLevelButtonClicked()
        {
            Debug.Log($"<color=white>[LevelCardUI]</color> {_levelNumber}. Seviye Butonuna tıklandı!");
            if (LevelSelectionManager.Instance != null)
            {
                LevelSelectionManager.Instance.StartLevel(_levelNumber);
            }
        }
    }
}
