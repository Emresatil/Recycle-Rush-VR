using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RecycleRush.Interaction
{
    /// <summary>
    /// Handles the logic for pulling distant objects towards the player's hand (Jedi Pull / Gravity Grab).
    /// Follows Single Responsibility Principle (SRP) by only managing the pull trajectory and state, 
    /// leaving grab logic to XRGrabInteractable.
    /// </summary>
    [RequireComponent(typeof(XRRayInteractor))]
    public class GravityPullController : MonoBehaviour
    {
        [Header("Pull Settings")]
        [SerializeField, Tooltip("How fast the object travels to the hand.")]
        private float _pullSpeed = 5f;
        
        [SerializeField, Tooltip("The curve of the object's trajectory.")]
        private AnimationCurve _pullCurve;

        private XRRayInteractor _rayInteractor;
        private Rigidbody _targetRigidbody;
        private bool _isPulling = false;

        private void Awake()
        {
            _rayInteractor = GetComponent<XRRayInteractor>();
        }

        private void OnEnable()
        {
            // Subscribe to XR Ray selection events to detect when an object is targeted from afar
            _rayInteractor.selectEntered.AddListener(OnSelectEntered);
            _rayInteractor.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            _rayInteractor.selectEntered.RemoveListener(OnSelectEntered);
            _rayInteractor.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // TODO: Validate distance. If object is far, start the pulling sequence.
            Debug.Log($"[GravityPullController] Target acquired: {args.interactableObject.transform.name}. Starting pull sequence.");
            
            _targetRigidbody = args.interactableObject.transform.GetComponent<Rigidbody>();
            if (_targetRigidbody != null)
            {
                _isPulling = true;
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            // Reset state when object arrives at hand or is dropped
            Debug.Log($"[GravityPullController] Target released or arrived.");
            _isPulling = false;
            _targetRigidbody = null;
        }

        private void Update()
        {
            if (_isPulling && _targetRigidbody != null)
            {
                ProcessPullPhysics();
            }
        }

        /// <summary>
        /// Calculates and applies the physics force/lerp required to pull the object smoothly.
        /// </summary>
        private void ProcessPullPhysics()
        {
            // TODO: Mathematics for the Bezier curve pull will be implemented here.
        }
    }
}
