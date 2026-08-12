using UnityEngine;

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

        private void Start()
        {
            // İleride buraya kilit/yıldız güncelleme kodları gelecek.
        }
    }
}
