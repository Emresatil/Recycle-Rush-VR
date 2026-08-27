using UnityEngine;
using TMPro;

namespace RecycleRush.Tutorial
{
    /// <summary>
    /// Oyuncunun görüş alanında rahat bir mesafede (HUD / Floating Card) yüzen,
    /// aktif adımı, talimatları ve tamamlanma bildirimlerini gösteren AR Hologram Bilgi Kartı.
    /// Tüm boyut, mesafe ve font ayarları Inspector üzerinden kolayca düzenlenebilir.
    /// </summary>
    public class ARHoloStepCard : MonoBehaviour
    {
        [Header("📐 Konumlandırma & Mesafe")]
        [Tooltip("Kartın oyuncu kamerasından uzaklığı (metre)")]
        [Range(0.5f, 3.0f)] public float distanceFromCamera = 1.4f;

        [Tooltip("Kartın kameraya göre dikey yüksekliği (metre, + yukarı / - aşağı)")]
        [Range(-1.0f, 1.0f)] public float heightOffset = 0.05f;

        [Tooltip("Kamerayı takip etme yumuşaklık hızı")]
        [Range(1.0f, 15.0f)] public float followSpeed = 5.0f;

        [Tooltip("Tüm kartın genel ölçeği")]
        [Range(0.01f, 2.0f)] public float cardScale = 0.35f;

        [Header("🔤 Font Boyutları (Font Sizes)")]
        [Tooltip("Adım Başlığı Font Boyutu")]
        [Range(0.2f, 5.0f)] public float titleFontSize = 1.4f;

        [Tooltip("Açıklama / Talimat Font Boyutu")]
        [Range(0.1f, 3.0f)] public float instructionFontSize = 0.85f;

        [Tooltip("İlerleme Göstergesi ([ ● ● ○ ○ ]) Font Boyutu")]
        [Range(0.1f, 3.0f)] public float progressFontSize = 0.75f;

        [Header("📦 Metin Kutusu Genişliği & Satır Kaydırma")]
        [Tooltip("Açıklama metninin yatay genişliği (Geniş olursa kelimeler alt alta kırılmaz)")]
        public float instructionBoxWidth = 3.2f;
        [Tooltip("Açıklama metninin dikey kutu yüksekliği")]
        public float instructionBoxHeight = 1.2f;

        [Header("📍 Dikey Pozisyon Aralıkları (Y Offsets)")]
        public float titleYOffset = 0.35f;
        public float instructionYOffset = 0.05f;
        public float progressYOffset = -0.28f;

        [Header("🎨 Renk Ayarları")]
        public Color titleColor = new Color(0.2f, 0.9f, 1f, 1f); // Neon Cyan
        public Color instructionColor = Color.white;
        public Color progressColor = new Color(1f, 0.85f, 0.2f, 1f); // Gold

        private TextMeshPro _stepTitleText;
        private TextMeshPro _stepInstructionText;
        private TextMeshPro _stepProgressText;
        private GameObject _canvasRoot;

        private void Awake()
        {
            SetupHUDCard();
        }

        private void SetupHUDCard()
        {
            if (_canvasRoot != null) return;

            _canvasRoot = new GameObject("HoloCardCanvas");
            _canvasRoot.transform.SetParent(transform, false);
            _canvasRoot.transform.localScale = Vector3.one * cardScale;

            // 1) Başlık
            GameObject titleObj = new GameObject("StepTitle");
            titleObj.transform.SetParent(_canvasRoot.transform, false);
            titleObj.transform.localPosition = new Vector3(0, titleYOffset, 0);
            _stepTitleText = titleObj.AddComponent<TextMeshPro>();
            _stepTitleText.fontSize = titleFontSize;
            _stepTitleText.color = titleColor;
            _stepTitleText.fontStyle = FontStyles.Bold;
            _stepTitleText.alignment = TextAlignmentOptions.Center;

            // 2) Talimat Metni
            GameObject descObj = new GameObject("StepInstruction");
            descObj.transform.SetParent(_canvasRoot.transform, false);
            descObj.transform.localPosition = new Vector3(0, instructionYOffset, 0);
            _stepInstructionText = descObj.AddComponent<TextMeshPro>();
            _stepInstructionText.fontSize = instructionFontSize;
            _stepInstructionText.color = instructionColor;
            _stepInstructionText.alignment = TextAlignmentOptions.Center;
            _stepInstructionText.textWrappingMode = TextWrappingModes.Normal;
            _stepInstructionText.rectTransform.sizeDelta = new Vector2(instructionBoxWidth, instructionBoxHeight);

            // 3) İlerleme Göstergesi
            GameObject progObj = new GameObject("StepProgress");
            progObj.transform.SetParent(_canvasRoot.transform, false);
            progObj.transform.localPosition = new Vector3(0, progressYOffset, 0);
            _stepProgressText = progObj.AddComponent<TextMeshPro>();
            _stepProgressText.fontSize = progressFontSize;
            _stepProgressText.color = progressColor;
            _stepProgressText.alignment = TextAlignmentOptions.Center;

            ApplySettings();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Inspector'dan bir değer değiştirildiğinde anında görsel olarak uygular.
        /// </summary>
        public void ApplySettings()
        {
            if (_canvasRoot != null)
            {
                _canvasRoot.transform.localScale = Vector3.one * cardScale;
            }

            if (_stepTitleText != null)
            {
                _stepTitleText.fontSize = titleFontSize;
                _stepTitleText.color = titleColor;
                _stepTitleText.transform.localPosition = new Vector3(0, titleYOffset, 0);
            }

            if (_stepInstructionText != null)
            {
                _stepInstructionText.fontSize = instructionFontSize;
                _stepInstructionText.color = instructionColor;
                _stepInstructionText.rectTransform.sizeDelta = new Vector2(instructionBoxWidth, instructionBoxHeight);
                _stepInstructionText.transform.localPosition = new Vector3(0, instructionYOffset, 0);
            }

            if (_stepProgressText != null)
            {
                _stepProgressText.fontSize = progressFontSize;
                _stepProgressText.color = progressColor;
                _stepProgressText.transform.localPosition = new Vector3(0, progressYOffset, 0);
            }
        }

        private void OnValidate()
        {
            ApplySettings();
        }

        public void DisplayStep(string title, string instruction, string progress)
        {
            if (_canvasRoot == null) SetupHUDCard();

            gameObject.SetActive(true);
            ApplySettings();

            if (_stepTitleText != null) _stepTitleText.text = title;
            if (_stepInstructionText != null) _stepInstructionText.text = instruction;
            if (_stepProgressText != null) _stepProgressText.text = progress;

            SnapToPlayerView();
        }

        public void ShowSuccess(string message = "HARİKA! ADIM TAMAMLANDI ✔")
        {
            if (_stepInstructionText != null)
            {
                _stepInstructionText.text = $"<color=green>{message}</color>";
            }
        }

        private void SnapToPlayerView()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 forward = cam.transform.forward;
                forward.y = 0;
                if (forward.sqrMagnitude < 0.001f) forward = cam.transform.forward;
                forward.Normalize();

                Vector3 targetPos = cam.transform.position + forward * distanceFromCamera + Vector3.up * heightOffset;
                transform.position = targetPos;
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            }
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 forward = cam.transform.forward;
            forward.y = 0;
            if (forward.sqrMagnitude < 0.001f) forward = cam.transform.forward;
            forward.Normalize();

            Vector3 desiredPos = cam.transform.position + forward * distanceFromCamera + Vector3.up * heightOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * followSpeed);

            // Yumuşak dönüş (Billboard)
            Vector3 lookDir = transform.position - cam.transform.position;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * followSpeed);
            }
        }
    }
}
