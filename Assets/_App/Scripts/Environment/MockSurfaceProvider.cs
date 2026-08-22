using System.Collections.Generic;
using UnityEngine;

namespace RecycleRush.Environment
{
    /// <summary>
    /// Single Responsibility Principle: Sadece Editor (Test) ortamındaki Mock yüzeylerin verisini ISurfaceProvider arayüzü ile sunmaktan sorumludur.
    /// AR veya Spawner işlemleriyle ilgilenmez, sadece yüzey hesabı yapar.
    /// </summary>
    public class MockSurfaceProvider : MonoBehaviour, ISurfaceProvider
    {
        [Tooltip("Eğer boş bırakılırsa sahnede 'MockSurface' eklenmiş tüm objeleri otomatik bulur.")]
        [SerializeField] private List<MockSurface> _mockSurfaces = new List<MockSurface>();

        private void Awake()
        {
            if (_mockSurfaces.Count == 0)
            {
                _mockSurfaces.AddRange(FindObjectsByType<MockSurface>(FindObjectsSortMode.None));
            }
        }

        public bool TryGetRandomSurfacePoint(SurfaceType surfaceType, out SurfaceData surfaceData)
        {
            surfaceData = default;
            List<MockSurface> validSurfaces = new List<MockSurface>();

            // İstenen türdeki yüzeyleri filtrele
            foreach (var surface in _mockSurfaces)
            {
                if (surfaceType == SurfaceType.Any || surface.Type == surfaceType)
                {
                    validSurfaces.Add(surface);
                }
            }

            if (validSurfaces.Count == 0) return false;

            // Filtrelenen yüzeylerden rastgele birini seç
            MockSurface selectedSurface = validSurfaces[Random.Range(0, validSurfaces.Count)];
            Collider col = selectedSurface.SurfaceCollider;
            
            if (col == null) return false;

            Bounds bounds = col.bounds;

            // Yüzeyin üst kısmında (örneğin masanın üstü) rastgele bir koordinat belirle
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);
            float yPos = bounds.max.y; 

            surfaceData = new SurfaceData
            {
                Position = new Vector3(randomX, yPos, randomZ),
                Rotation = selectedSurface.transform.rotation,
                Type = selectedSurface.Type,
                BoundsSize = bounds.size
            };

            return true;
        }

        public List<SurfaceData> GetAllSurfaces()
        {
            List<SurfaceData> allData = new List<SurfaceData>();
            foreach (var surface in _mockSurfaces)
            {
                if (surface.SurfaceCollider != null)
                {
                    allData.Add(new SurfaceData
                    {
                        Position = surface.SurfaceCollider.bounds.center,
                        Rotation = surface.transform.rotation,
                        Type = surface.Type,
                        BoundsSize = surface.SurfaceCollider.bounds.size
                    });
                }
            }
            return allData;
        }
    }
}
