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
        // Fizik motorunda bandı ileri doğru hareket ettiriyoruz gibi gösterip, 
        // sonra pozisyonunu geri alıyoruz. 
        // Bu sayede bandın üzerindeki objeler sürtünmeyle o yöne doğru kayarken,
        // bandın kendisi yerinde sabit kalıyor.
        Vector3 movement = direction.normalized * speed * Time.fixedDeltaTime;
        
        // Bandı geriye çek...
        rb.position -= movement;
        
        // ...ve tekrar ileriye doğru MovePosition ile taşı.
        // MovePosition fizik motorunu tetikler ve üzerindeki objeleri de beraberinde sürükler.
        rb.MovePosition(rb.position + movement);
    }

    // =====================================================
    // İLK TEMAS ÇÖZÜMÜ (Bounce & Stabilize)
    // Obje banda ilk çarptığı anda:
    // 1. Dikey hızı (Y) sıfırlanır → sekme (bounce) olmaz
    // 2. Dönme hızı sıfırlanır → savrulma olmaz
    // 3. Rotasyon kilitlenir → titreme (jitter) olmaz
    // 4. Obje dik pozisyona getirilir
    // 5. Collision Detection "Continuous" yapılır → clipping engellenir
    // =====================================================
    void OnCollisionEnter(Collision collision)
    {
        Rigidbody itemRb = collision.rigidbody;
        if (itemRb != null && !itemRb.isKinematic)
        {
            // Sekmeyi önle: dikey hızı sıfırla, yatay hızı koru
            Vector3 vel = itemRb.linearVelocity;
            vel.y = 0f;
            itemRb.linearVelocity = vel;
            
            // Dönme hızını tamamen sıfırla
            itemRb.angularVelocity = Vector3.zero;
            
            // Objeyi anında dik pozisyona getir (Y ekseni rotasyonunu koru)
            Vector3 currentEuler = itemRb.transform.eulerAngles;
            itemRb.transform.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);

            // Rotasyonu kilitle → Titreme (jitter) tamamen engellenir.
            // Spawner'dan gelen tüm objeler otomatik olarak bu ayarı alır,
            // tek tek Inspector'dan ayarlamaya gerek kalmaz.
            itemRb.constraints = RigidbodyConstraints.FreezeRotation;
            
            // İçinden geçmeyi (clipping) önle
            itemRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    // =====================================================
    // BANTTAN ÇIKIŞ ÇÖZÜMÜ
    // Obje banttan ayrıldığında (oyuncu eline aldığında veya
    // bandın sonundan düştüğünde) rotasyon kilidini geri aç.
    // Böylece oyuncu objeyi serbestçe döndürüp fırlatabilir.
    // =====================================================
    void OnCollisionExit(Collision collision)
    {
        Rigidbody itemRb = collision.rigidbody;
        if (itemRb != null && !itemRb.isKinematic)
        {
            // Rotasyon kilidini kaldır (oyuncu serbestçe döndürebilsin)
            itemRb.constraints = RigidbodyConstraints.None;
        }
    }
}
