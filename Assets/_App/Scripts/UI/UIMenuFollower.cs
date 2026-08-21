using UnityEngine;

namespace RecycleRush.UI
{
    /// <summary>
    /// 3D World Space menülerin (örneğin Pause Menüsü) oyuncunun görüş alanını (Camera)
    /// yumuşak (smooth) bir şekilde takip etmesini sağlar.
    /// </summary>
    public class UIMenuFollower : MonoBehaviour
    {
        [Header("Takip Ayarları")]
        [Tooltip("Takip edilecek hedef (Genellikle Main Camera veya XR Origin Camera)")]
        [SerializeField] private Transform _target;
        
        [Tooltip("Hedefe göre menünün nerede duracağı (X: Sağa/Sola, Y: Yukarı/Aşağı, Z: İleri/Geri)")]
        [SerializeField] private Vector3 _offset = new Vector3(0, -0.2f, 1.0f);
        
        [Tooltip("Menünün takip etme hızı. Düşük değerler daha yumuşak (laggy) takip sağlar.")]
        [SerializeField] private float _followSmoothness = 5f;
        
        [Tooltip("Menü her zaman hedefe doğru dönsün mü?")]
        [SerializeField] private bool _lookAtTarget = true;

        private void Start()
        {
            if (_target == null && Camera.main != null)
            {
                // Hedef atanmamışsa otomatik olarak ana kamerayı bul
                _target = Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Hedefin pozisyonunu, hedefin BAKIŞ YÖNÜNE (rotation) göre hesapla.
            // Bu sayede oyuncu nereye bakarsa baksın, offset oyuncunun önüne düşer.
            Vector3 targetPosition = _target.position + (_target.right * _offset.x) + (_target.up * _offset.y) + (_target.forward * _offset.z);

            // Yumuşak geçişle (Lerp) menüyü hedefe doğru hareket ettir
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.unscaledDeltaTime * _followSmoothness);

            if (_lookAtTarget)
            {
                // Menünün yüzü her zaman oyuncuya baksın
                Vector3 lookDirection = transform.position - _target.position;
                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    // Rotasyonu da yumuşakça (Slerp) uygula (Time.unscaledDeltaTime kullanıyoruz çünkü oyun Pause olabilir)
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.unscaledDeltaTime * _followSmoothness);
                }
            }
        }
    }
}
