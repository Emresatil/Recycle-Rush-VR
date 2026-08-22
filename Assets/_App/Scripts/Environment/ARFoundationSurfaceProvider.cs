using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace RecycleRush.Environment
{
    /// <summary>
    /// Gerçek dünyadaki (Quest gözlüğünün taradığı) AR yüzeylerini (Masa, Zemin vb.) alır 
    /// ve ISurfaceProvider arayüzü ile Spawner'a teslim eder.
    /// </summary>
    [RequireComponent(typeof(ARPlaneManager))]
    public class ARFoundationSurfaceProvider : MonoBehaviour, ISurfaceProvider
    {
        private ARPlaneManager _planeManager;

        private void Awake()
        {
            _planeManager = GetComponent<ARPlaneManager>();
        }

        public bool TryGetRandomSurfacePoint(SurfaceType surfaceType, out SurfaceData surfaceData)
        {
            surfaceData = default;
            
            if (_planeManager == null || _planeManager.trackables.count == 0)
                return false;

            List<ARPlane> validPlanes = new List<ARPlane>();

            // Quest'in odada taradığı tüm gerçek yüzeyleri kontrol et
            foreach (var plane in _planeManager.trackables)
            {
                // Sadece yere paralel (yatay) olan yüzeylerde çöp doğmasını istiyoruz
                if (plane.alignment != PlaneAlignment.HorizontalUp)
                    continue;

                // Spawner masa istiyorsa sadece gerçek masaları (Table), zemin istiyorsa zeminleri (Floor) filtrele
                if (surfaceType == SurfaceType.Any)
                {
                    validPlanes.Add(plane);
                }
                else if (surfaceType == SurfaceType.Floor && (plane.classifications & PlaneClassifications.Floor) != 0)
                {
                    validPlanes.Add(plane);
                }
                else if (surfaceType == SurfaceType.Table && (plane.classifications & PlaneClassifications.Table) != 0)
                {
                    validPlanes.Add(plane);
                }
            }

            if (validPlanes.Count == 0) return false;

            // Bulunan uygun gerçek yüzeylerden rastgele birini seç
            ARPlane selectedPlane = validPlanes[Random.Range(0, validPlanes.Count)];

            // Yüzeyin merkezine yakın, kenarlardan biraz içeride rastgele bir nokta seç (Çöpler düşmesin diye %80'lik alan)
            Vector2 extents = selectedPlane.extents;
            float randomX = Random.Range(-extents.x * 0.8f, extents.x * 0.8f);
            float randomZ = Random.Range(-extents.y * 0.8f, extents.y * 0.8f);

            // Yerel koordinatı dünya (oyun) koordinatına çevir
            Vector3 localPoint = new Vector3(randomX, 0, randomZ);
            Vector3 worldPoint = selectedPlane.transform.TransformPoint(localPoint);

            surfaceData = new SurfaceData
            {
                Position = worldPoint,
                Rotation = selectedPlane.transform.rotation,
                Type = GetSurfaceTypeFromClassifications(selectedPlane.classifications),
                BoundsSize = new Vector3(extents.x * 2, 0, extents.y * 2)
            };

            return true;
        }

        public List<SurfaceData> GetAllSurfaces()
        {
            List<SurfaceData> allData = new List<SurfaceData>();
            if (_planeManager == null) return allData;

            foreach (var plane in _planeManager.trackables)
            {
                if (plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    allData.Add(new SurfaceData
                    {
                        Position = plane.center,
                        Rotation = plane.transform.rotation,
                        Type = GetSurfaceTypeFromClassifications(plane.classifications),
                        BoundsSize = new Vector3(plane.extents.x * 2, 0, plane.extents.y * 2)
                    });
                }
            }
            return allData;
        }

        private SurfaceType GetSurfaceTypeFromClassifications(PlaneClassifications classifications)
        {
            if ((classifications & PlaneClassifications.Floor) != 0)
                return SurfaceType.Floor;
            if ((classifications & PlaneClassifications.Table) != 0)
                return SurfaceType.Table;
            if ((classifications & PlaneClassifications.WallFace) != 0)
                return SurfaceType.Wall;
                
            return SurfaceType.Any;
        }
    }
}
