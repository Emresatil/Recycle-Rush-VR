using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RecycleRush.Interaction
{
    /// <summary>
    /// Composite Waste sisteminde iki çöpü görsel olarak bağlayan ve referanslarını tutan sınıf (SRP - Sadece Veri/Görsel).
    /// </summary>
    public class WasteGlue : MonoBehaviour
    {
        [Tooltip("Bant görselini çizecek LineRenderer.")]
        public LineRenderer bandRenderer;

        public XRGrabInteractable partA;
        public XRGrabInteractable partB;

        // Objeleri fiziksel olarak bir arada tutacak eklem
        private FixedJoint _joint;

        public bool IsActive { get; private set; } = false;

        private void Awake()
        {
            if (bandRenderer == null)
            {
                bandRenderer = gameObject.AddComponent<LineRenderer>();
                bandRenderer.startWidth = 0.05f;
                bandRenderer.endWidth = 0.05f;
                bandRenderer.useWorldSpace = true;
                bandRenderer.positionCount = 2;
                bandRenderer.enabled = false;
            }
        }

        public void Bind(XRGrabInteractable a, XRGrabInteractable b, Material tapeMaterial)
        {
            partA = a;
            partB = b;
            
            if (bandRenderer != null && tapeMaterial != null)
            {
                bandRenderer.material = tapeMaterial;
            }

            // Objeleri fiziksel olarak birbirine mühürle! 
            // (Yoksa biri alınıp gidilir, diğeri yerde kalır ve bant sonsuza uzar)
            if (partA != null && partB != null)
            {
                Rigidbody rbB = partB.GetComponent<Rigidbody>();
                if (rbB != null)
                {
                    _joint = partA.gameObject.AddComponent<FixedJoint>();
                    _joint.connectedBody = rbB;
                    // Kopma kuvvetini sonsuz yapıyoruz ki sadece Bimanual kodumuz ile kopsun (saçmalamasın)
                    _joint.breakForce = Mathf.Infinity;
                    _joint.breakTorque = Mathf.Infinity;
                }
            }

            IsActive = true;
            if (bandRenderer != null) bandRenderer.enabled = true;
        }

        private void Update()
        {
            if (IsActive && partA != null && partB != null && bandRenderer != null)
            {
                // Görsel bandı iki objenin arasına çiz (esniyor gibi görünür)
                bandRenderer.SetPosition(0, partA.transform.position);
                bandRenderer.SetPosition(1, partB.transform.position);
            }
        }

        public void BreakGlue()
        {
            IsActive = false;
            
            // Fiziksel bağlantıyı tamamen yok et
            if (_joint != null)
            {
                Destroy(_joint);
                _joint = null;
            }

            partA = null;
            partB = null;

            if (bandRenderer != null)
            {
                bandRenderer.enabled = false;
            }

            Debug.Log("<color=orange>[Composite Waste]</color> Çöpler başarıyla birbirinden ayrıldı! (CARRT)");
        }

        private void OnDisable()
        {
            // Object Pool güvenliği (kirli veri kalmasın)
            IsActive = false;
            if (_joint != null)
            {
                Destroy(_joint);
                _joint = null;
            }
            partA = null;
            partB = null;
            if (bandRenderer != null) bandRenderer.enabled = false;
        }
    }
}
