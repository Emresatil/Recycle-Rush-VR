using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RecycleRush.Interaction
{
    [RequireComponent(typeof(XRBaseInteractor))]
    public class GravityPullController : MonoBehaviour
    {
        [SerializeField, Tooltip("Cooldown between successful pulls to prevent spamming.")]
        private float _pullCooldown = 0.5f;

        [Header("Haptics")]
        [SerializeField] private float _hapticDuration = 0.1f;
        [SerializeField] private float _hapticIntensity = 0.5f;

        private XRBaseInteractor _interactor;
        private XRInteractionManager _interactionManager;
        
        private float _lastPullTime = -1f;

        private void Awake()
        {
            _interactor = GetComponent<XRBaseInteractor>();
            
            // Interaction Manager'ı otomatik bul (Sahnedeki XR Origin setup'ından gelir)
            _interactionManager = FindAnyObjectByType<XRInteractionManager>();
            if (_interactionManager == null)
            {
                Debug.LogError("[GravityPullController] XRInteractionManager sahne içinde bulunamadı!");
            }
        }

        private void OnEnable()
        {
            // XR Ray'in bir nesneyi seçtiği anı dinle
            _interactor.selectEntered.AddListener(OnSelectEntered);
        }

        private void OnDisable()
        {
            _interactor.selectEntered.RemoveListener(OnSelectEntered);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            IXRSelectInteractable targetInteractable = args.interactableObject;
            if (targetInteractable == null) return;

            if (!(targetInteractable is XRGrabInteractable grabInteractable)) return;

            float distance = Vector3.Distance(transform.position, targetInteractable.transform.position);

            // Uzaklık sınırını (2.5m / 10m vb.) tamamen kaldırdık! 
            // Lazerin değdiği her şeyi çekebilecek. Zamanla yarışan oyuncu için en iyisi budur.

            if (distance > 0.5f)
            {
                if (Time.time - _lastPullTime < _pullCooldown) return;

                _lastPullTime = Time.time;
                
                GravityPullMotion motion = targetInteractable.transform.GetComponent<GravityPullMotion>();
                if (motion == null) 
                    motion = targetInteractable.transform.gameObject.AddComponent<GravityPullMotion>();
                
                Transform attachPoint = _interactor.attachTransform != null ? _interactor.attachTransform : this.transform;
                
                // Seçimi iptal ETMİYORUZ. Obje şu an XR Toolkit tarafından 'Tutuluyor' (Selected).
                // Sadece fiziksel pozisyon takibini geçici olarak kapatıp kendi uçuşumuzu başlatıyoruz.
                motion.StartPull(attachPoint, grabInteractable, (m) => OnPullCompleted(targetInteractable, m));
            }
        }

        private void OnPullCompleted(IXRSelectInteractable targetInteractable, GravityPullMotion motion)
        {
            if (targetInteractable == null || targetInteractable.transform == null) return;
            SendHapticFeedback();
        }

        private IEnumerator CancelSelectionNextFrame(IXRSelectInteractable target)
        {
            yield return null;
            if (target != null && target.isSelected && _interactionManager != null)
            {
                _interactionManager.SelectCancel((IXRSelectInteractor)_interactor, target);
            }
        }

        private void SendHapticFeedback()
        {
            // Eğer HapticManager varsa o kullanılabilir, ancak doğrudan kontrolcüye titreşim yolluyoruz:
            var controller = GetComponent<XRBaseController>();
            if (controller != null)
            {
                controller.SendHapticImpulse(_hapticIntensity, _hapticDuration);
            }
        }
    }
}
