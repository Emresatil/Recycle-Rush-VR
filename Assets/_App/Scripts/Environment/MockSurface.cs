using UnityEngine;

namespace RecycleRush.Environment
{
    /// <summary>
    /// Single Responsibility Principle: Sadece bu objenin ne tür bir sahte (mock) yüzey olduğunu tanımlar.
    /// Unity Editor'de Zemin veya Masa olarak kullanacağın objelere bu kodu eklemelisin.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MockSurface : MonoBehaviour
    {
        [Tooltip("Bu yüzeyin türü nedir? (Zemin, Masa vs.)")]
        [SerializeField] private SurfaceType _surfaceType = SurfaceType.Floor;
        public SurfaceType Type => _surfaceType;

        private Collider _col;
        public Collider SurfaceCollider 
        {
            get
            {
                if (_col == null) _col = GetComponent<Collider>();
                return _col;
            }
        }
    }
}
