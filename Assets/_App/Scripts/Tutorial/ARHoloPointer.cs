using UnityEngine;
using TMPro;

namespace RecycleRush.Tutorial
{
    /// <summary>
    /// AR ortamında hedef objelerin (çöp, geri dönüşüm kutusu vb.) üzerinde beliren,
    /// yaylanarak (bounce) ve dönerek dikkat çeken 3D Hologram Yönlendirme Oku.
    /// Tüm görsel ve boyut ayarları Inspector üzerinden kolayca ayarlanabilir.
    /// </summary>
    public class ARHoloPointer : MonoBehaviour
    {
        [Header("🔤 Boyut & Font")]
        [Tooltip("3D Hologram Oku Font / Simge Boyutu")]
        [Range(0.2f, 5.0f)] public float pointerFontSize = 1.8f;

        [Header("🎨 Görsel Ayarlar")]
        [Tooltip("Varsayılan Ok Rengi")]
        public Color pointerColor = new Color(0.2f, 0.9f, 1f, 0.95f); // Hologram Cyan

        [Header("📍 Hareket & Yaylanma Ayarları")]
        [Tooltip("Hedefin üzerindeki dikey yükseklik mesafesi")]
        [Range(0.05f, 1.5f)] public float heightOffset = 0.30f;

        [Tooltip("Yaylanma genliği (yukarı-aşağı ne kadar oynasın)")]
        [Range(0.01f, 0.5f)] public float bounceAmplitude = 0.08f;

        [Tooltip("Yaylanma hızı")]
        [Range(0.5f, 10.0f)] public float bounceFrequency = 4.0f;

        private Transform _targetTransform;
        private TextMeshPro _arrowText;
        private GameObject _visualHolder;

        private void Awake()
        {
            SetupVisuals();
        }

        private void SetupVisuals()
        {
            if (_visualHolder != null) return;

            _visualHolder = new GameObject("VisualHolder");
            _visualHolder.transform.SetParent(transform, false);

            _arrowText = _visualHolder.AddComponent<TextMeshPro>();
            _arrowText.text = "▼";
            _arrowText.fontSize = pointerFontSize;
            _arrowText.color = pointerColor;
            _arrowText.fontStyle = FontStyles.Bold;
            _arrowText.alignment = TextAlignmentOptions.Center;

            gameObject.SetActive(false);
        }

        public void ApplySettings()
        {
            if (_arrowText != null)
            {
                _arrowText.fontSize = pointerFontSize;
                _arrowText.color = pointerColor;
            }
        }

        private void OnValidate()
        {
            ApplySettings();
        }

        /// <summary>
        /// İşaretçiyi belirli bir hedefe bağlar ve rengini/yazısını özelleştirir.
        /// </summary>
        public void SetTarget(Transform target, Color? color = null, string symbol = "▼", float extraHeight = 0f)
        {
            if (_visualHolder == null) SetupVisuals();

            _targetTransform = target;

            if (_arrowText != null)
            {
                if (color.HasValue) _arrowText.color = color.Value;
                if (!string.IsNullOrEmpty(symbol)) _arrowText.text = symbol;
                _arrowText.fontSize = pointerFontSize;
            }

            if (_targetTransform == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            UpdatePosition(true, extraHeight);
        }

        private void LateUpdate()
        {
            if (_targetTransform == null || !_targetTransform.gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }

            UpdatePosition(false);
        }

        private void UpdatePosition(bool instant = false, float extraHeight = 0f)
        {
            Vector3 centerPos = _targetTransform.position;
            float topY = centerPos.y;

            Collider[] colliders = _targetTransform.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                Bounds combinedBounds = new Bounds();
                bool hasBounds = false;

                foreach (var col in colliders)
                {
                    if (col != null && col.enabled && !col.isTrigger)
                    {
                        if (!hasBounds)
                        {
                            combinedBounds = col.bounds;
                            hasBounds = true;
                        }
                        else
                        {
                            combinedBounds.Encapsulate(col.bounds);
                        }
                    }
                }

                if (hasBounds)
                {
                    centerPos = combinedBounds.center;
                    topY = combinedBounds.max.y;
                }
            }

            float bounce = Mathf.Sin(Time.time * bounceFrequency) * bounceAmplitude;
            Vector3 targetPos = new Vector3(centerPos.x, topY + heightOffset + extraHeight + bounce, centerPos.z);

            transform.position = instant ? targetPos : Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 12f);

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 dirToCam = transform.position - mainCam.transform.position;
                if (dirToCam.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(dirToCam);
                }
            }
        }
    }
}
