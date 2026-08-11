using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems; // PlaneClassification için gerekli

namespace RecycleRush.Environment
{
    /// <summary>
    /// AR ortamındaki görünmez zeminlere anlık (Runtime) olarak fiziksel sürtünme katsayısı ekleyen 
    /// AAA standartlarında yüzey yöneticisidir. Manuel PhysicMaterial üretmeye gerek bırakmaz.
    /// </summary>
    [RequireComponent(typeof(ARPlaneManager))]
    public class ARSurfaceConfigurator : MonoBehaviour
    {
        [Header("Zemin (Floor/Halı) Ayarları")]
        [Tooltip("Gerçek zeminlere uygulanacak yüksek sürtünme katsayısı (Kaymayı önler)")]
        [SerializeField] private float floorDynamicFriction = 0.8f;
        [SerializeField] private float floorStaticFriction = 0.8f;
        
        [Header("Masa/Koltuk (Table/Seat) Ayarları")]
        [Tooltip("Masalara uygulanacak ortalama sürtünme katsayısı")]
        [SerializeField] private float tableDynamicFriction = 0.5f;
        [SerializeField] private float tableStaticFriction = 0.5f;

        private ARPlaneManager _planeManager;
        
        // Bellek (Memory) optimizasyonu: Zeminler sürekli değiştiği için materyalleri bir kere üretip önbellekte (Cache) tutuyoruz.
        private PhysicsMaterial _floorMaterial;
        private PhysicsMaterial _tableMaterial;

        private void Awake()
        {
            _planeManager = GetComponent<ARPlaneManager>();
            
            // Unity Editöründe manuel dosya (Asset) yaratmaya gerek kalmadan PhysicMaterial'leri RAM'de oluşturuyoruz!
            _floorMaterial = new PhysicsMaterial("AR_FloorFriction")
            {
                dynamicFriction = floorDynamicFriction,
                staticFriction = floorStaticFriction,
                frictionCombine = PhysicsMaterialCombine.Maximum // Halıya değen objenin maksimum sürtünmeye maruz kalması için
            };

            _tableMaterial = new PhysicsMaterial("AR_TableFriction")
            {
                dynamicFriction = tableDynamicFriction,
                staticFriction = tableStaticFriction,
                frictionCombine = PhysicsMaterialCombine.Average
            };
        }

        private void OnEnable()
        {
            _planeManager.planesChanged += OnPlanesChanged;
        }

        private void OnDisable()
        {
            _planeManager.planesChanged -= OnPlanesChanged;
        }

        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            // Yeni taranan zeminler odaya eklendikçe onlara müdahale et
            foreach (var plane in args.added)
            {
                ApplyPhysicsToPlane(plane);
            }
            
            // Eğer zemin güncellenirse (Örneğin AR kamerası onun Masa değil Halı olduğuna karar verirse) fiziğini anında güncelle
            foreach (var plane in args.updated)
            {
                ApplyPhysicsToPlane(plane);
            }
        }

        private void ApplyPhysicsToPlane(ARPlane plane)
        {
            // Unity'nin kronik "İnce Zemin (Plane) Çarpışma Hatası" (Tunneling/Ghosting)
            // Çöplerin (Defter, çubuk vb.) 0 kalınlığındaki AR zeminlerinden içinden geçip düşmesini engellemek için
            // AR Foundation'ın ürettiği o incecik MeshCollider'ı iptal edip, yerine etli/kalın bir BoxCollider örüyoruz!
            
            MeshCollider meshCollider = plane.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                meshCollider.enabled = false; // Güvenilmez ince ağı kapat
            }

            BoxCollider boxCollider = plane.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = plane.gameObject.AddComponent<BoxCollider>();
            }

            // AR zemininin (Halının/Masanın) anlık boyutunu alıp BoxCollider'ı ona uydur. 
            // Y ekseninde (Yükseklik) 5 santimlik bir "Et Kalınlığı" ver! 
            boxCollider.size = new Vector3(plane.size.x, 0.05f, plane.size.y);
            boxCollider.center = new Vector3(0, -0.025f, 0); // Yüzeyin tam hizasında kalması için merkezi aşağı kaydır

            // Zeminin tipine göre uygun sürtünmeyi uygula
            if (plane.classification == PlaneClassification.Floor)
            {
                boxCollider.sharedMaterial = _floorMaterial;
                Debug.Log("<color=green>[AR Zemin]</color> Zemin tespit edildi, Etli BoxCollider ve Halı sürtünmesi eklendi.");
            }
            else if (plane.classification == PlaneClassification.Table || 
                     plane.classification == PlaneClassification.Seat)
            {
                boxCollider.sharedMaterial = _tableMaterial;
                Debug.Log("<color=yellow>[AR Zemin]</color> Masa/Koltuk tespit edildi, Etli BoxCollider ve Masa sürtünmesi eklendi.");
            }
            else
            {
                // Duvar (Wall) veya belirsiz (None) ise standart masa sürtünmesini varsayılan olarak ver
                boxCollider.sharedMaterial = _tableMaterial;
            }
        }
    }
}
