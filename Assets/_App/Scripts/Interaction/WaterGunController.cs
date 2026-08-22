using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RecycleRush.Interaction
{
    /// <summary>
    /// Su tabancasÄ±nÄ±n etkileÅŸim ve fÄ±ÅŸkÄ±rtma mantÄ±ÄŸÄ±nÄ± yÃ¶netir (SRP).
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class WaterGunController : MonoBehaviour
    {
        [Header("Water Settings")]
        [Tooltip("Su fÄ±ÅŸkÄ±rtma efekti (Particle System)")]
        public ParticleSystem waterEffect;

        [Tooltip("Suyun menzili (Raycast mesafesi)")]
        public float range = 2f;

        [Tooltip("TabancanÄ±n namlu ucu (Raycast Ã§Ä±kÄ±ÅŸ noktasÄ±)")]
        public Transform muzzlePoint;

        [Tooltip("Su sÄ±kma Ã§apÄ± (SphereCast kalÄ±nlÄ±ÄŸÄ±)")]
        public float hitRadius = 0.1f;

        private XRGrabInteractable _grabInteractable;
        private bool _isFiring = false;

        private void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();
            
            if (waterEffect != null)
            {
                waterEffect.Stop();
            }
        }

        private void OnEnable()
        {
            _grabInteractable.activated.AddListener(StartFiring);
            _grabInteractable.deactivated.AddListener(StopFiring);
        }

        private void OnDisable()
        {
            _grabInteractable.activated.RemoveListener(StartFiring);
            _grabInteractable.deactivated.RemoveListener(StopFiring);
            StopFiring(new UnityEngine.XR.Interaction.Toolkit.DeactivateEventArgs());
        }

        private void StartFiring(UnityEngine.XR.Interaction.Toolkit.ActivateEventArgs args)
        {
            _isFiring = true;
            if (waterEffect != null)
            {
                waterEffect.Play();
            }
        }

        private void StopFiring(UnityEngine.XR.Interaction.Toolkit.DeactivateEventArgs args)
        {
            _isFiring = false;
            if (waterEffect != null)
            {
                waterEffect.Stop();
            }
        }

        private void Update()
        {
            if (!_isFiring) return;

            // PerformanslÄ± temizlik: Raycast / SphereCast ile hedefi bul
            Transform origin = muzzlePoint != null ? muzzlePoint : transform;
            
            Ray ray = new Ray(origin.position, origin.forward);
            
            // SphereCast kullanarak oyuncunun tam isabet ettirmesini kolaylaÅŸtÄ±rÄ±yoruz
            if (Physics.SphereCast(ray, hitRadius, out RaycastHit hit, range))
            {
                var dirtyWaste = hit.collider.GetComponentInParent<DirtyWasteController>();
                if (dirtyWaste != null && dirtyWaste.IsDirty)
                {
                    dirtyWaste.Wash();
                }
            }
        }
    }
}


