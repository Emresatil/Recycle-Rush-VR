using UnityEngine;
using UnityEngine.SceneManagement;

namespace RecycleRush.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("UI Referansları")]
        [Tooltip("Açılıp kapanacak olan ana menü paneli veya Canvas'ı")]
        [SerializeField] private GameObject _pauseMenuPanel;

        private void Awake()
        {
            // Başlangıçta menünün kapalı olduğundan emin ol
            if (_pauseMenuPanel != null)
            {
                _pauseMenuPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // Oyun durum değişikliklerini dinle
            if (GameManager.Instance != null)
            {
                GameManager.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (_pauseMenuPanel == null) return;

            // Oyun duraklatıldıysa menüyü aç, oynuyorsa kapat
            if (newState == GameState.Paused)
            {
                _pauseMenuPanel.SetActive(true);
            }
            else if (newState == GameState.Playing)
            {
                _pauseMenuPanel.SetActive(false);
            }
        }

        // ==========================================
        // UI BUTONLARI İÇİN FONKSİYONLAR
        // ==========================================

        /// <summary>
        /// "Devam Et" butonuna tıklandığında çağrılır.
        /// </summary>
        public void OnResumeButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
        }

        /// <summary>
        /// "Yeniden Başlat" butonuna tıklandığında çağrılır.
        /// </summary>
        public void OnRestartButtonClicked()
        {
            // Zaman ölçeğini (TimeScale) düzelt ki yeni sahne donmuş başlamasın
            Time.timeScale = 1f; 
            
            // Mevcut sahneyi yeniden yükle
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }

        /// <summary>
        /// "Çıkış" butonuna tıklandığında çağrılır.
        /// </summary>
        public void OnQuitButtonClicked()
        {
            Debug.Log("<color=red>[PauseMenuController]</color> Oyundan çıkılıyor...");
            Application.Quit();
        }
    }
}
