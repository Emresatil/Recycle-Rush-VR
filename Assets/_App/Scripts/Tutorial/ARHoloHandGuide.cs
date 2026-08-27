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
        [Header("Görsel Ayarlar")]
        [SerializeField] private float _animationSpeed = 3f;

        private GuideGestureType _currentGesture = GuideGestureType.None;
        private TextMeshPro _gestureText;
        private Transform _targetTransform;
        private Vector3 _customOffset = Vector3.up * 0.25f;

        private void Awake()
        {
            SetupGestureVisuals();
        }

        private void SetupGestureVisuals()
        {
            _gestureText = gameObject.AddComponent<TextMeshPro>();
            _gestureText.fontSize = 18;
            _gestureText.fontStyle = FontStyles.Bold;
            _gestureText.alignment = TextAlignmentOptions.Center;
            _gestureText.color = new Color(0.2f, 1f, 0.6f, 0.95f); // Neon Green / Hologram

            gameObject.SetActive(false);
        }

        public void ShowGesture(GuideGestureType gesture, Transform target = null)
        {
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

            // Hedef varsa hedefin üzerinde konumlan
            if (_targetTransform != null)
            {
                transform.position = _targetTransform.position + _customOffset;
            }

            // Kameraya doğru dön (Billboard)
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 lookDir = transform.position - mainCam.transform.position;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }

            // Animasyonlu el / hareket ikonları
            float cycle = Mathf.PingPong(Time.time * _animationSpeed, 1f);

            switch (_currentGesture)
            {
                case GuideGestureType.SingleGrab:
                    _gestureText.text = cycle > 0.5f ? "✊ [GRIP]" : "🖐 [REACH]";
                    _gestureText.color = Color.Lerp(new Color(0.2f, 0.8f, 1f), new Color(0.2f, 1f, 0.4f), cycle);
                    break;

                case GuideGestureType.GravityPull:
                    _gestureText.text = cycle > 0.5f ? "⚡ ➔ ✊ [PULL]" : "⚡ ➔ 🖐 [AIM RAY]";
                    _gestureText.color = Color.Lerp(Color.yellow, Color.cyan, cycle);
                    break;

                case GuideGestureType.BimanualTear:
                    _gestureText.text = cycle > 0.5f ? "⬅ ✊  ||  ✊ ➡\n[PULL APART]" : "🖐 ➡  ||  ⬅ 🖐\n[GRAB BOTH]";
                    _gestureText.color = Color.Lerp(new Color(1f, 0.4f, 0.4f), new Color(1f, 0.9f, 0.2f), cycle);
                    break;
            }
        }
    }
}
