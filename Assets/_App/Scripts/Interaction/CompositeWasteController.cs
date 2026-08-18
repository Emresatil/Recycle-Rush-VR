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
        [Tooltip("İki çöpü koparmak için gereken esneme mesafesi (metre). (0.35m'de spawn olurlar)")]
        [SerializeField] private float separationDistanceThreshold = 0.85f;

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

            bool bothSelected = _glue.partA.isSelected && _glue.partB.isSelected;
            
            // Fiziksel eklemi duruma göre (Katı veya Esnek) güncelle
            _glue.SetBimanualState(bothSelected);

            // Sadece iki elle tutuluyorsa kopmasına izin ver!
            // Böylece Gravity Grab ile çekerken veya tek elle sallarken yanlışlıkla kopmaz.
            if (!bothSelected) return;

            float distance = Vector3.Distance(_glue.partA.transform.position, _glue.partB.transform.position);

            if (distance >= separationDistanceThreshold)
            {
                // Haptic titreşim gönder
                if (RecycleRush.Core.HapticManager.Instance != null)
                {
                    if (_glue.partA.firstInteractorSelecting is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor inputA)
                        RecycleRush.Core.HapticManager.Instance.TriggerGrabHaptic(inputA);
                        
                    if (_glue.partB.firstInteractorSelecting is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor inputB)
                        RecycleRush.Core.HapticManager.Instance.TriggerGrabHaptic(inputB);
                }

                // KOPAR!
                _glue.BreakGlue();
            }
        }
    }
}
