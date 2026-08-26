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
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (_comboText != null)
            {
                Color c = _comboText.color;
                c.a = 0f;
                _comboText.color = c;
            }
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameState;
            BinTrigger.OnComboChanged += HandleComboChanged;
            
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
            BinTrigger.OnComboChanged -= HandleComboChanged;
        }

        private void HandleComboChanged(int currentCombo)
        {
            if (_comboText == null) return;

            if (currentCombo > 1)
            {
                _comboText.text = $"Kombo x{currentCombo}!";
                
                if (_hideCoroutine != null)
                {
                    StopCoroutine(_hideCoroutine);
                }
                _hideCoroutine = StartCoroutine(ShowAndFadeRoutine());
            }
            else
            {
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
            Color c = _comboText.color;
            c.a = 1f;
            _comboText.color = c;
            
            _comboText.transform.localScale = Vector3.one * 1.5f;

            yield return new WaitForSeconds(_displayDuration);

            while (c.a > 0f)
            {
                c.a -= Time.deltaTime * _fadeSpeed;
                _comboText.color = c;
                
                float scale = Mathf.Max(1f, _comboText.transform.localScale.x - (Time.deltaTime * _fadeSpeed));
                _comboText.transform.localScale = Vector3.one * scale;

                yield return null;
            }
            
            _comboText.transform.localScale = Vector3.one;
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
