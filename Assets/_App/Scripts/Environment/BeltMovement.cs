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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
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
}
