using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

namespace RecycleRush.Interaction
{
    /// <summary>
    /// Sadece obje tutulduğunda ve bırakıldığında ses efektlerini çalar.
    /// Fiziğe veya başka bir şeye müdahale etmez.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class WasteAudioFeedback : MonoBehaviour
    {
        private XRGrabInteractable _grabInteractable;

        private void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();
        }

        private void OnEnable()
        {
            if (_grabInteractable != null)
            {
                // XR Interaction Toolkit event'lerine abone ol
                _grabInteractable.selectEntered.AddListener(OnGrabbed);
                _grabInteractable.selectExited.AddListener(OnReleased);
            }
        }

        private void OnDisable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                _grabInteractable.selectExited.RemoveListener(OnReleased);
            }
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGrabSound(transform.position);
            }

            // Haptic (Titreşim) Geri Bildirimi
            if (RecycleRush.Core.HapticManager.Instance != null && 
                args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controllerInteractor)
            {
                RecycleRush.Core.HapticManager.Instance.TriggerHaptic(controllerInteractor, 0.5f, 0.1f);
            }
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayReleaseSound(transform.position);
            }
        }
    }
}
