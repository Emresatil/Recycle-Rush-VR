using UnityEngine;
using TMPro;
using System.Collections;

namespace RecycleRush.Tutorial
{
    /// <summary>
    /// Oyuncunun görüş alanında rahat bir mesafede (HUD / Floating Card) yüzen,
    /// aktif adımı, talimatları ve tamamlanma bildirimlerini gösteren AR Hologram Bilgi Kartı.
    /// </summary>
    public class ARHoloStepCard : MonoBehaviour
    {
        [Header("Konumlandırma Ayarları")]
        [SerializeField] private float _distanceFromCamera = 1.3f;
        [SerializeField] private float _heightOffset = -0.15f;
        [SerializeField] private float _followSpeed = 4.5f;

        private TextMeshPro _stepTitleText;
        private TextMeshPro _stepInstructionText;
        private TextMeshPro _stepProgressText;
        private GameObject _canvasRoot;
        private Coroutine _fadeRoutine;

        private void Awake()
        {
            SetupHUDCard();
        }

        private void SetupHUDCard()
        {
            _canvasRoot = new GameObject("HoloCardCanvas");
            _canvasRoot.transform.SetParent(transform, false);

            // Başlık
            GameObject titleObj = new GameObject("StepTitle");
            titleObj.transform.SetParent(_canvasRoot.transform, false);
            titleObj.transform.localPosition = new Vector3(0, 0.12f, 0);
            _stepTitleText = titleObj.AddComponent<TextMeshPro>();
            _stepTitleText.fontSize = 7f;
            _stepTitleText.color = new Color(0.3f, 0.9f, 1f, 1f); // Neon Cyan
            _stepTitleText.fontStyle = FontStyles.Bold;
            _stepTitleText.alignment = TextAlignmentOptions.Center;

            // Talimat Metni
            GameObject descObj = new GameObject("StepInstruction");
            descObj.transform.SetParent(_canvasRoot.transform, false);
            descObj.transform.localPosition = new Vector3(0, -0.02f, 0);
            _stepInstructionText = descObj.AddComponent<TextMeshPro>();
            _stepInstructionText.fontSize = 5f;
            _stepInstructionText.color = Color.white;
            _stepInstructionText.alignment = TextAlignmentOptions.Center;
            _stepInstructionText.textWrappingMode = TextWrappingModes.Normal;
            _stepInstructionText.rectTransform.sizeDelta = new Vector2(1.6f, 0.4f);

            // İlerleme Noktaları (örn: [ ● ○ ○ ○ ])
            GameObject progObj = new GameObject("StepProgress");
            progObj.transform.SetParent(_canvasRoot.transform, false);
            progObj.transform.localPosition = new Vector3(0, -0.14f, 0);
            _stepProgressText = progObj.AddComponent<TextMeshPro>();
            _stepProgressText.fontSize = 4.5f;
            _stepProgressText.color = new Color(1f, 0.85f, 0.2f, 1f); // Gold Yellow
            _stepProgressText.alignment = TextAlignmentOptions.Center;

            gameObject.SetActive(false);
        }

        public void DisplayStep(string title, string instruction, string progress)
        {
            gameObject.SetActive(true);

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
                forward.Normalize();

                Vector3 targetPos = cam.transform.position + forward * _distanceFromCamera + Vector3.up * _heightOffset;
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

            Vector3 desiredPos = cam.transform.position + forward * _distanceFromCamera + Vector3.up * _heightOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * _followSpeed);

            // Yumuşak dönüş (Billboard)
            Vector3 lookDir = transform.position - cam.transform.position;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * _followSpeed);
            }
        }
    }
}
