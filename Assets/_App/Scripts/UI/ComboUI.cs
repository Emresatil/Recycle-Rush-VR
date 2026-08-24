using UnityEngine;
using TMPro;
using System.Collections;

namespace RecycleRush.UI
{
    public class ComboUI : MonoBehaviour
    {
        [Header("UI Referansları")]
        [Tooltip("Kombo yazısının çıkacağı metin (Örn: Kombo x3!)")]
        [SerializeField] private TextMeshProUGUI _comboText;

        [Header("Animasyon Ayarları")]
        [Tooltip("Kombo yazısının ekranda kalma süresi")]
        [SerializeField] private float _displayDuration = 2f;
        
        [Tooltip("Küçülüp kaybolma hızı")]
        [SerializeField] private float _fadeSpeed = 3f;

        private Coroutine _hideCoroutine;

        private void Awake()
        {
            if (_comboText != null)
            {
                // Başlangıçta yazıyı görünmez (şeffaf) yap
                Color c = _comboText.color;
                c.a = 0f;
                _comboText.color = c;
            }
        }

        private void OnEnable()
        {
            BinTrigger.OnComboChanged += HandleComboChanged;
        }

        private void OnDisable()
        {
            BinTrigger.OnComboChanged -= HandleComboChanged;
        }

        private void HandleComboChanged(int currentCombo)
        {
            if (_comboText == null) return;

            if (currentCombo > 1)
            {
                // Kombo varsa yazıyı göster ve efekti başlat
                _comboText.text = $"Kombo x{currentCombo}!";
                
                if (_hideCoroutine != null)
                {
                    StopCoroutine(_hideCoroutine);
                }
                _hideCoroutine = StartCoroutine(ShowAndFadeRoutine());
            }
            else
            {
                // Kombo kırıldıysa (0 veya 1 ise) yazıyı anında gizle
                if (_hideCoroutine != null)
                {
                    StopCoroutine(_hideCoroutine);
                    _hideCoroutine = null;
                }
                
                Color c = _comboText.color;
                c.a = 0f;
                _comboText.color = c;
            }
        }

        private IEnumerator ShowAndFadeRoutine()
        {
            // Yazıyı anında görünür (Opak) yap ve biraz büyüt
            Color c = _comboText.color;
            c.a = 1f;
            _comboText.color = c;
            
            _comboText.transform.localScale = Vector3.one * 1.5f;

            // Yazı biraz ekranda kalsın
            yield return new WaitForSeconds(_displayDuration);

            // Sönümlenerek kaybolsun ve küçülsün (Fade Out & Shrink)
            while (c.a > 0f)
            {
                c.a -= Time.deltaTime * _fadeSpeed;
                _comboText.color = c;
                
                float scale = Mathf.Max(1f, _comboText.transform.localScale.x - (Time.deltaTime * _fadeSpeed));
                _comboText.transform.localScale = Vector3.one * scale;

                yield return null;
            }
            
            _comboText.transform.localScale = Vector3.one; // Boyutu sıfırla
        }
    }
}
