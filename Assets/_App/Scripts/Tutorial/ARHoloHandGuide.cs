using UnityEngine;
using TMPro;

namespace RecycleRush.Tutorial
{
    public enum GuideGestureType
    {
        None,
        SingleGrab,      // Tek Elle Tutma İkonu
        GravityPull,     // Uzaktan Çekme Işını & Çekme Hareketi
        BimanualTear     // İki Elle Çekip Ayırma
    }

    /// <summary>
    /// AR ortamında VR hareketlerini (Tut, Uzaktan Çek, İki Elle Ayır)
    /// oyuncunun gözünün önüne animasyonlu hologram grafiklerle gösteren rehber bileşen.
    /// </summary>
    public class ARHoloHandGuide : MonoBehaviour
    {
        [Header("🔤 Boyut & Font")]
        [Tooltip("El / Hareket simgelerinin boyutu")]
        [Range(0.2f, 4.0f)] public float gestureFontSize = 1.1f;

        [Tooltip("Özel Yazı Tipi (Boş bırakılırsa varsayılan ChakraPetch atanır)")]
        public TMP_FontAsset customFont;

        [Header("📍 Konum & Animasyon")]
        [Tooltip("Hedefe göre dikey/yatay konum ofseti")]
        public Vector3 customOffset = new Vector3(0, 0.22f, 0);

        [Tooltip("İkonların yanıp sönme / geçiş hızı")]
        [Range(0.5f, 8.0f)] public float animationSpeed = 2.5f;

        [Header("🎨 Renkler")]
        public Color grabColor = new Color(0.2f, 1f, 0.5f, 0.95f);
        public Color pullColor = new Color(1f, 0.9f, 0.2f, 0.95f);
        public Color tearColor = new Color(1f, 0.35f, 0.35f, 0.95f);

        private GuideGestureType _currentGesture = GuideGestureType.None;
        private TextMeshPro _gestureText;
        private Transform _targetTransform;

        private void Awake()
        {
            SetupGestureVisuals();
        }

        private void SetupGestureVisuals()
        {
            if (_gestureText != null) return;

            _gestureText = gameObject.AddComponent<TextMeshPro>();
            if (customFont != null) _gestureText.font = customFont;
            _gestureText.fontSize = gestureFontSize;
            _gestureText.fontStyle = FontStyles.Bold;
            _gestureText.alignment = TextAlignmentOptions.Center;
            _gestureText.color = grabColor;

            gameObject.SetActive(false);
        }

        public void ShowGesture(GuideGestureType gesture, Transform target = null)
        {
            if (_gestureText == null) SetupGestureVisuals();
            if (customFont != null && _gestureText != null) _gestureText.font = customFont;

            _currentGesture = gesture;
            _targetTransform = target;

            if (gesture == GuideGestureType.None)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _currentGesture = GuideGestureType.None;
            _targetTransform = null;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_currentGesture == GuideGestureType.None) return;

            if (_targetTransform != null)
            {
                transform.position = _targetTransform.position + customOffset;
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 lookDir = transform.position - mainCam.transform.position;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }

            float cycle = Mathf.PingPong(Time.time * animationSpeed, 1f);

            if (_gestureText != null)
            {
                _gestureText.fontSize = gestureFontSize;

                switch (_currentGesture)
                {
                    case GuideGestureType.SingleGrab:
                        _gestureText.text = cycle > 0.5f ? "[GRIP & HOLD]" : "[REACH & GRAB]";
                        _gestureText.color = Color.Lerp(new Color(0.2f, 0.8f, 1f), grabColor, cycle);
                        break;

                    case GuideGestureType.GravityPull:
                        _gestureText.text = cycle > 0.5f ? ">>> [PULL FAST]" : "--- [AIM RAY]";
                        _gestureText.color = Color.Lerp(pullColor, Color.cyan, cycle);
                        break;

                    case GuideGestureType.BimanualTear:
                        _gestureText.text = cycle > 0.5f ? "<< [PULL APART] >>" : ">> [GRAB BOTH] <<";
                        _gestureText.color = Color.Lerp(tearColor, new Color(1f, 0.9f, 0.2f), cycle);
                        break;
                }
            }
        }
    }
}
