using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RecycleRush.Interaction
{
    public class WasteGlue : MonoBehaviour
    {
        public LineRenderer bandRenderer;

        public XRGrabInteractable partA;
        public XRGrabInteractable partB;

        public bool IsActive { get; private set; } = false;

        private Vector3 _localPosB_in_A;
        private Quaternion _localRotB_in_A;

        private Vector3 _localPosA_in_B;
        private Quaternion _localRotA_in_B;

        private FixedJoint _floorJoint;

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
            
            if (bandRenderer == null)
            {
                bandRenderer = gameObject.AddComponent<LineRenderer>();
                bandRenderer.startWidth = 0.04f;
                bandRenderer.endWidth = 0.04f;
            }
            if (tapeMaterial != null) bandRenderer.material = tapeMaterial;

            if (partA != null && partB != null)
            {
                // Mükemmel hizalama için ilk konumları kaydediyoruz
                _localPosB_in_A = partA.transform.InverseTransformPoint(partB.transform.position);
                _localRotB_in_A = Quaternion.Inverse(partA.transform.rotation) * partB.transform.rotation;

                _localPosA_in_B = partB.transform.InverseTransformPoint(partA.transform.position);
                _localRotA_in_B = Quaternion.Inverse(partB.transform.rotation) * partA.transform.rotation;

                EnsureFloorJoint();
            }

            IsActive = true;
            if (bandRenderer != null) bandRenderer.enabled = true;
        }

        public void SetBimanualState(bool isBimanual)
        {
            // Eski joint sistemini sildik, artık LateUpdate içinde %100 kusursuz hizalama yapıyoruz.
        }

        private void LateUpdate()
        {
            if (!IsActive || partA == null || partB == null) return;

            bool aSelected = partA.isSelected;
            bool bSelected = partB.isSelected;

            if (aSelected && !bSelected)
            {
                // SADECE A ELDE
                DestroyFloorJoint();
                SetKinematic(partB.GetComponent<Rigidbody>(), true);
                
                // B'yi matematiksel olarak A'ya zımbala (Jitter İMKANSIZ)
                partB.transform.position = partA.transform.TransformPoint(_localPosB_in_A);
                partB.transform.rotation = partA.transform.rotation * _localRotB_in_A;
            }
            else if (bSelected && !aSelected)
            {
                // SADECE B ELDE
                DestroyFloorJoint();
                SetKinematic(partA.GetComponent<Rigidbody>(), true);

                // A'yı matematiksel olarak B'ye zımbala
                partA.transform.position = partB.transform.TransformPoint(_localPosA_in_B);
                partA.transform.rotation = partB.transform.rotation * _localRotA_in_B;
            }
            else if (aSelected && bSelected)
            {
                // İKİSİ DE ELDE (Çekip Koparma Modu)
                DestroyFloorJoint();
                
                // XR sisteminin iki objeyi de serbestçe çekebilmesi için kinematic kapatılır
                SetKinematic(partA.GetComponent<Rigidbody>(), false);
                SetKinematic(partB.GetComponent<Rigidbody>(), false);
            }
            else
            {
                // İKİSİ DE YERDE (Düşüyorlar veya duruyorlar)
                SetKinematic(partA.GetComponent<Rigidbody>(), false);
                SetKinematic(partB.GetComponent<Rigidbody>(), false);
                EnsureFloorJoint();
            }

            // Çizgiyi (Bantı) Güncelle
            if (bandRenderer != null)
            {
                bandRenderer.SetPosition(0, partA.transform.position);
                bandRenderer.SetPosition(1, partB.transform.position);
            }
        }

        private void SetKinematic(Rigidbody rb, bool state)
        {
            if (rb != null && rb.isKinematic != state)
            {
                rb.isKinematic = state;
            }
        }

        private void EnsureFloorJoint()
        {
            if (_floorJoint == null && partA != null && partB != null)
            {
                Rigidbody rbB = partB.GetComponent<Rigidbody>();
                if (rbB != null)
                {
                    _floorJoint = partA.gameObject.AddComponent<FixedJoint>();
                    _floorJoint.connectedBody = rbB;
                    _floorJoint.breakForce = Mathf.Infinity;
                    _floorJoint.breakTorque = Mathf.Infinity;
                }
            }
        }

        private void DestroyFloorJoint()
        {
            if (_floorJoint != null)
            {
                Destroy(_floorJoint);
                _floorJoint = null;
            }
        }

        public void BreakGlue()
        {
            IsActive = false;
            DestroyFloorJoint();
            SetKinematic(partA?.GetComponent<Rigidbody>(), false);
            SetKinematic(partB?.GetComponent<Rigidbody>(), false);

            partA = null;
            partB = null;

            if (bandRenderer != null)
            {
                bandRenderer.enabled = false;
            }
        }

        private void OnDisable()
        {
            IsActive = false;
            DestroyFloorJoint();
            SetKinematic(partA?.GetComponent<Rigidbody>(), false);
            SetKinematic(partB?.GetComponent<Rigidbody>(), false);
            
            partA = null;
            partB = null;
            if (bandRenderer != null) bandRenderer.enabled = false;
        }
    }
}
