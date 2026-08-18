using UnityEngine;

namespace RecycleRush.Interaction
{
    /// <summary>
    /// WasteGlue sistemini yöneten kontrolcü. İki elin objeleri tutup tutmadığını ve mesafeyi ölçer.
    /// SRP gereği sadece mantıksal kontrolleri (ayrılma şartlarını) yürütür.
    /// </summary>
    [RequireComponent(typeof(WasteGlue))]
    public class CompositeWasteController : MonoBehaviour
    {
        [Tooltip("İki çöpü koparmak için gereken esneme mesafesi (metre). (0.15m'de spawn olurlar)")]
        [SerializeField] private float separationDistanceThreshold = 0.35f;

        private WasteGlue _glue;

        private void Awake()
        {
            _glue = GetComponent<WasteGlue>();
        }

        private void Update()
        {
            if (_glue == null || !_glue.IsActive) return;

            // Güvenlik kontrolü
            if (_glue.partA == null || _glue.partB == null)
            {
                _glue.BreakGlue();
                return;
            }

            // KOPMA MANTIĞI (FOOLPROOF): 
            // İster iki eliyle tutsun, ister biri bir yere takılsın. Mesafe 0.35m'yi geçerse KOPAR!
            // (Simulator'de iki eli aynı anda "Selected" tutmak zor olduğu için bu çok daha güvenlidir).
            float distance = Vector3.Distance(_glue.partA.transform.position, _glue.partB.transform.position);

            if (distance >= separationDistanceThreshold)
            {
                // Haptic titreşim gönder (Eğer tutuluyorsa)
                if (RecycleRush.Core.HapticManager.Instance != null)
                {
                    if (_glue.partA.isSelected && _glue.partA.firstInteractorSelecting is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor inputA)
                        RecycleRush.Core.HapticManager.Instance.TriggerGrabHaptic(inputA);
                        
                    if (_glue.partB.isSelected && _glue.partB.firstInteractorSelecting is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor inputB)
                        RecycleRush.Core.HapticManager.Instance.TriggerGrabHaptic(inputB);
                }

                // KOPAR!
                _glue.BreakGlue();
            }
        }
    }
}
