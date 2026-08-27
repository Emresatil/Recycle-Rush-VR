using UnityEngine;
using TMPro;

namespace RecycleRush.Tutorial
{
    /// <summary>
    /// AR ortamında hedef objelerin (çöp, geri dönüşüm kutusu vb.) üzerinde beliren,
    /// yaylanarak (bounce) ve dönerek dikkat çeken 3D Hologram Yönlendirme Oku.
    /// </summary>
    public class ARHoloPointer : MonoBehaviour
    {
        [Header("Görsel Ayarlar")]
        [SerializeField] private Color _pointerColor = new Color(0.2f, 0.9f, 1f, 0.95f); // Hologram Camgöbeği (Cyan)
        [SerializeField] private float _bounceAmplitude = 0.12f;
        [SerializeField] private float _bounceFrequency = 4f;
        [SerializeField] private float _rotationSpeed = 60f;
        [SerializeField] private float _heightOffset = 0.35f;

        private Transform _targetTransform;
        private TextMeshPro _arrowText;
        private GameObject _visualHolder;
        private Vector3 _baseOffset;

        private void Awake()
        {
            SetupVisuals();
        }

        private void SetupVisuals()
        {
            _visualHolder = new GameObject("VisualHolder");
            _visualHolder.transform.SetParent(transform, false);

            // TextMeshPro ile 3D Keskin ve Parlak Hologram Ok
            _arrowText = _visualHolder.AddComponent<TextMeshPro>();
            _arrowText.text = "▼";
            _arrowText.fontSize = 26;
            _arrowText.color = _pointerColor;
            _arrowText.fontStyle = FontStyles.Bold;
            _arrowText.alignment = TextAlignmentOptions.Center;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// İşaretçiyi belirli bir hedefe bağlar ve rengini/yazısını özelleştirir.
        /// </summary>
        public void SetTarget(Transform target, Color? color = null, string symbol = "▼", float extraHeight = 0f)
        {
            _targetTransform = target;

            if (color.HasValue && _arrowText != null)
            {
                _arrowText.color = color.Value;
            }

            if (!string.IsNullOrEmpty(symbol) && _arrowText != null)
            {
                _arrowText.text = symbol;
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
            // Hedefin görsel sınırlarını (Collider Bounds) hesapla
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

            // Dikey eksende sinüs yaylanma hareketi
            float bounce = Mathf.Sin(Time.time * _bounceFrequency) * _bounceAmplitude;
            Vector3 targetPos = new Vector3(centerPos.x, topY + _heightOffset + extraHeight + bounce, centerPos.z);

            transform.position = instant ? targetPos : Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 12f);

            // Oyuncunun kamerasına dön (Billboard)
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
