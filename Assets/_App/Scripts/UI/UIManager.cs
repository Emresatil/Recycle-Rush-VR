using UnityEngine;
using TMPro; // TextMeshPro (Yazılar) için gerekli

namespace RecycleRush.UI
{
    /// <summary>
    /// GameManager'ın durumlarını dinleyerek sahnede bulunan 3D Monitör (Ekran) üzerindeki yazıları ve butonları yönetir.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Ekran (Monitör) Yazıları")]
        [Tooltip("Süreyi gösterecek olan yazı bileşeni (Örn: 60)")]
        public TextMeshProUGUI timeText;
        [Tooltip("Oyun durumunu gösterecek yazı (Örn: OYUN BITTI)")]
        public TextMeshProUGUI statusText;

        [Header("Butonlar")]
        [Tooltip("Oyun bitince çıkacak olan Fiziksel Restart Butonu objesi")]
        public GameObject restartButtonObj;

        private void OnEnable()
        {
            // Event'leri dinlemeye başla
            if (GameManager.Instance != null)
            {
                GameManager.OnGameStateChanged += HandleGameState;
                GameManager.OnGameTimeUpdated += UpdateTimeDisplay;
                
                // Başlangıç durumunu hemen ekrana yansıt
                HandleGameState(GameManager.Instance.CurrentState);
            }
        }

        private void OnDisable()
        {
            // Bellek sızıntısını önlemek için dinlemeyi bırak
            GameManager.OnGameStateChanged -= HandleGameState;
            GameManager.OnGameTimeUpdated -= UpdateTimeDisplay;
        }

        /// <summary>
        /// Oyun durumu her değiştiğinde (MainMenu -> Playing -> GameOver) bu fonksiyon çalışır.
        /// </summary>
        private void HandleGameState(GameState state)
        {
            switch (state)
            {
                case GameState.Initialization:
                case GameState.MainMenu:
                    if (statusText != null) statusText.text = "SISTEM HAZIR\nBASLAMAK ICIN BUTONA BAS";
                    if (timeText != null) timeText.text = "60";
                    
                    // Restart butonunu ana menüde gizle
                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    break;
                    
                case GameState.Playing:
                    if (statusText != null) statusText.text = "GERI DONUSUM BASLADI";
                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    break;
                    
                case GameState.Paused:
                    if (statusText != null) statusText.text = "SISTEM DURAKLATILDI";
                    break;

                case GameState.GameOver:
                    if (statusText != null) statusText.text = "<color=red>SURE BITTI!</color>\nBAND DURDURULDU";
                    
                    // Oyun bittiğinde Restart butonunu ortaya çıkar!
                    if (restartButtonObj != null) restartButtonObj.SetActive(true);
                    break;
            }
        }

        /// <summary>
        /// GameManager'dan saniye saniye gelen kalan süre bilgisini ekrana (timeText) yazar.
        /// </summary>
        private void UpdateTimeDisplay(float remainingTime)
        {
            if (timeText != null)
            {
                // Süreyi tam sayıya (Örn: 59.4 -> 60) yuvarlayarak yazdır
                timeText.text = Mathf.CeilToInt(remainingTime).ToString();
                
                // Vurgu (Juice): Son 10 saniye kala yazıyı kırmızı yap!
                if (remainingTime <= 10f)
                {
                    timeText.color = Color.red;
                }
                else
                {
                    timeText.color = Color.white;
                }
            }
        }
    }
}
