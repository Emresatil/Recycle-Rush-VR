using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace RecycleRush.AR_Features
{
    /// <summary>
    /// AR ortamı tarandığında (Zemin bulunduğunda) geri dönüşüm kutularını otomatik olarak
    /// o zeminin merkezine (veya kameraya yakın bir noktasına) yerleştirir.
    /// </summary>
    [RequireComponent(typeof(ARPlaneManager))]
    public class ARBinPlacer : MonoBehaviour
    {
        [Tooltip("Sahnede otomatik yerleştirilecek Çöp Kutuları modülü (Örn: RecyclingArea_Module)")]
        public GameObject recyclingBinsModule;

        [Tooltip("Kutular oyuncudan en az ne kadar uzakta olmalı? (Metre)")]
        public float distanceFromPlayer = 1.5f;

        [Tooltip("Yerleştirme işleminden sonra plane algılamayı kapatalım mı? (Performans için önerilir)")]
        public bool disablePlaneDetectionAfterPlacement = true;

        private ARPlaneManager _planeManager;
        private bool _isPlaced = false;

        private void Awake()
        {
            _planeManager = GetComponent<ARPlaneManager>();
        }

        private void OnEnable()
        {
            if (_planeManager != null)
            {
                _planeManager.trackablesChanged.AddListener(OnTrackablesChanged);
            }
        }

        private void OnDisable()
        {
            if (_planeManager != null)
            {
                _planeManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
            }
        }

        private void Start()
        {
            // Oyun başlarken kutuları görünmez yap (AR taranana kadar havada asılı durmasınlar)
            if (recyclingBinsModule != null)
            {
                recyclingBinsModule.SetActive(false);
            }
            else
            {
                Debug.LogWarning("<color=orange>[ARBinPlacer]</color> Çöp kutusu modülü atanmamış!");
            }
        }

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (_isPlaced) return;

            // Yeni eklenen düzlemleri (Plane) kontrol et
            foreach (var plane in args.added)
            {
                // Sadece yatay zeminleri (Yer) hedef alıyoruz, duvarları değil
                if (plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    PlaceBins(plane);
                    break; // Bir kere yerleştirmek yeterli
                }
            }
        }

        private void PlaceBins(ARPlane plane)
        {
            if (recyclingBinsModule == null) return;

            // Kutuyu aktif hale getir
            recyclingBinsModule.SetActive(true);

            // Kutunun pozisyonunu düzlemin merkezine ayarla
            // Alternatif: Kamera pozisyonundan ileriye doğru 'distanceFromPlayer' kadar uzaklıkta ama y ekseni zemin hizasında
            Transform mainCam = Camera.main.transform;
            Vector3 forwardDirection = mainCam.forward;
            forwardDirection.y = 0; // Sadece yatay eksende ileri bak
            forwardDirection.Normalize();

            Vector3 targetPosition = mainCam.position + forwardDirection * distanceFromPlayer;
            targetPosition.y = plane.center.y; // Yüksekliği, algılanan zeminin yüksekliğine eşitle

            recyclingBinsModule.transform.position = targetPosition;
            
            // Kutuların yüzü (ön tarafı) oyuncuya dönük olsun
            recyclingBinsModule.transform.LookAt(new Vector3(mainCam.position.x, recyclingBinsModule.transform.position.y, mainCam.position.z));

            Debug.Log($"<color=green>[ARBinPlacer]</color> Çöp Kutuları başarıyla zemine yerleştirildi! (Yükseklik: {plane.center.y})");
            _isPlaced = true;

            if (disablePlaneDetectionAfterPlacement && _planeManager != null)
            {
                _planeManager.enabled = false;
                // Mevcut görünen plane görsellerini gizle
                foreach (var p in _planeManager.trackables)
                {
                    p.gameObject.SetActive(false);
                }
            }
        }
    }
}
