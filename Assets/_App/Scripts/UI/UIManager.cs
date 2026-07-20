using UnityEngine;
using TMPro; // TextMeshPro (Yazılar) için gerekli
using System.Collections; // Coroutine (Lerp animasyonları) için gerekli

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

        [Header("Kombo Sistemi")]
        [Tooltip("Kombo yazısını gösterecek TextMeshPro bileşeni")]
        public TextMeshProUGUI comboText;
        
        private Coroutine _comboAnimationCoroutine;

        private void OnEnable()
        {
            // Event'leri dinlemeye başla (Eventler statik olduğu için Instance beklemeden abone olabiliriz)
            GameManager.OnGameStateChanged += HandleGameState;
            GameManager.OnGameTimeUpdated += UpdateTimeDisplay;
        }

        private void Start()
        {
            // Başlangıç durumunu hemen ekrana yansıt
            if (GameManager.Instance != null)
            {
                HandleGameState(GameManager.Instance.CurrentState);
            }
            
            // ScoreManager üzerinden kombo olaylarını dinlemeye başla (Awake sonrası olduğu için Instance hazırdır)
            if (Core.ScoreManager.Instance != null)
            {
                Core.ScoreManager.Instance.OnComboChanged += HandleComboChanged;
            }
            
            // Başlangıçta kombo yazısını gizle
            if (comboText != null)
            {
                comboText.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            // Bellek sızıntısını önlemek için dinlemeyi bırak
            GameManager.OnGameStateChanged -= HandleGameState;
            GameManager.OnGameTimeUpdated -= UpdateTimeDisplay;
            
            if (Core.ScoreManager.Instance != null)
            {
                Core.ScoreManager.Instance.OnComboChanged -= HandleComboChanged;
            }
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
                    if (statusText != null) statusText.text = "SYSTEM READY\nPRESS BUTTON TO START";
                    if (timeText != null) timeText.text = "60";
                    
                    // Restart butonunu ana menüde gizle
                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    break;
                    
                case GameState.Playing:
                    if (statusText != null) statusText.text = "RECYCLING STARTED";
                    if (restartButtonObj != null) restartButtonObj.SetActive(false);
                    break;
                    
                case GameState.Paused:
                    if (statusText != null) statusText.text = "SYSTEM PAUSED";
                    break;

                case GameState.GameOver:
                    if (statusText != null) statusText.text = "<color=red>TIME'S UP!</color>\nCONVEYOR STOPPED";
                    
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

        /// <summary>
        /// Kombo değiştiğinde tetiklenir ve Pop (Patlama) animasyonunu başlatır.
        /// </summary>
        private void HandleComboChanged(int comboCount, int multiplier)
        {
            if (comboText == null) return;

            if (multiplier > 1)
            {
                // Kombo varsa yazıyı aktif et ve metni ayarla
                comboText.gameObject.SetActive(true);
                comboText.text = $"{multiplier}x COMBO!";
                comboText.color = new Color(1f, 0.84f, 0f); // Altın Sarısı (Gold)

                // Varsa önceki animasyonu durdur ki çakışmasın
                if (_comboAnimationCoroutine != null)
                {
                    StopCoroutine(_comboAnimationCoroutine);
                }
                
                // Yeni Pop animasyonunu başlat
                _comboAnimationCoroutine = StartCoroutine(ComboPopAnimation());
            }
            else
            {
                // Katlayıcı yoksa (Kombo sıfırlandıysa) yazıyı gizle
                comboText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Yazıyı bir anda büyütüp sonra yavaşça normal boyutuna indiren (Lerp) Juice animasyonu.
        /// </summary>
        private IEnumerator ComboPopAnimation()
        {
            Vector3 originalScale = Vector3.one;
            Vector3 targetScale = originalScale * 1.5f; // %50 büyüt
            
            float duration = 0.15f; // Büyüme süresi
            float elapsed = 0f;

            // Büyüme (Scale Up)
            while (elapsed < duration)
            {
                comboText.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            comboText.transform.localScale = targetScale;
            
            elapsed = 0f;
            duration = 0.25f; // Küçülme süresi (Daha yumuşak)
            
            // Küçülme (Scale Down)
            while (elapsed < duration)
            {
                comboText.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            comboText.transform.localScale = originalScale;
            _comboAnimationCoroutine = null;
        }
    }
}
