using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BeltMovement : MonoBehaviour
{
    [Header("Bant Ayarları")]
    [Tooltip("Bandın objeleri taşıma hızı")]
    public float speed = 2f;
    
    [Tooltip("Taşıma yönü (X ekseninde hareket için 1, 0, 0)")]
    public Vector3 direction = Vector3.right;

    private Rigidbody rb;
    private float _baseSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _baseSpeed = speed; // Orijinal hızı önbelleğe (Cache) alıyoruz
    }

    private void OnEnable()
    {
        // Event dinleyicisini ekle
        DifficultyManager.OnDifficultyLevelChanged += UpdateBeltSpeed;
    }

    private void OnDisable()
    {
        // Script veya obje kapandığında Event aboneliğini kaldır
        DifficultyManager.OnDifficultyLevelChanged -= UpdateBeltSpeed;
    }

    /// <summary>
    /// DifficultyManager'dan gelen hız çarpanına göre bant hızını günceller.
    /// </summary>
    private void UpdateBeltSpeed(float multiplier)
    {
        speed = _baseSpeed * multiplier;
        Debug.Log($"<color=cyan>[BeltMovement]</color> Yeni zorluğa uyarlandı! Bant Hızı: {speed:F1}");
    }

    void Start()
    {
        // Bandın fiziksel olarak düşmemesi ve sabit kalması için isKinematic yapıyoruz
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        // Bandın kendisini yerinde sabit tutuyoruz.
        // Objelerin hareketi OnCollisionStay içinde fiziksel hız (linearVelocity) ile sağlanır.
    }

    // =====================================================
    // BANT ÜZERİNDE HAREKET VE İLK TEMAS
    // Obje banda çarptığında ve üzerinde kaldığı sürece:
    // 1. Dikey hızı (Y) korunur, yatay hızı bandın yönü ve hızına eşitlenir.
    // 2. Bandın üzerindeyse dik kalması için X ve Z rotasyonları kilitlenir.
    // =====================================================
    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody itemRb = collision.rigidbody;
        if (itemRb != null && !itemRb.isKinematic)
        {
            // Sekmeyi önle: dikey hızı sıfırla
            Vector3 vel = itemRb.linearVelocity;
            vel.y = 0f;
            itemRb.linearVelocity = vel;
            
            // Dönme hızını sıfırla
            itemRb.angularVelocity = Vector3.zero;
            
            // Objeyi anında dik pozisyona getir
            Vector3 currentEuler = itemRb.transform.eulerAngles;
            itemRb.transform.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);

            // Bant üzerindeyse devrilmemesi için rotasyonu kilitle
            itemRb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            
            // İçinden geçmeyi (clipping) önle
            itemRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        Rigidbody itemRb = collision.rigidbody;
        if (itemRb != null && !itemRb.isKinematic)
        {
            // Obje bant üzerindeyken bandın yönünde pürüzsüzce kaydır (Teleport/Fırlama tamamen engellenir)
            Vector3 targetVel = direction.normalized * speed;
            itemRb.linearVelocity = new Vector3(targetVel.x, itemRb.linearVelocity.y, targetVel.z);
        }
    }

    // =====================================================
    // BANTTAN ÇIKIŞ ÇÖZÜMÜ
    // Obje banttan ayrıldığında (düştüğünde veya fırladığında)
    // rotasyon kilidini aç, böylece doğal şekilde devrilebilir.
    // =====================================================
    private void OnCollisionExit(Collision collision)
    {
        Rigidbody itemRb = collision.rigidbody;
        if (itemRb != null && !itemRb.isKinematic)
        {
            // Banttan çıkınca kilitleri tamamen aç
            itemRb.constraints = RigidbodyConstraints.None;
        }
    }
}
