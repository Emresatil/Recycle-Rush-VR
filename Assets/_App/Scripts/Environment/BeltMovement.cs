using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bantın kendisinin scripti. BeltItem'ları FixedUpdate'te MovePosition ile taşır.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BeltMovement : MonoBehaviour
{
    [Header("Bant Ayarları")]
    public float speed = 2f;
    public Vector3 direction = Vector3.right;

    private Rigidbody _rb;
    private Collider _beltCollider;
    public Collider BeltCollider => _beltCollider;
    private float _baseSpeed;

    // Banta kayıtlı ve taşınacak çöplerin listesi
    private List<BeltItem> _trackedItems = new List<BeltItem>();

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // Bantın kendisi de Kinematic olmalı
        _rb.isKinematic = true;
        _rb.useGravity = false;
        
        _beltCollider = GetComponent<Collider>();
        if (_beltCollider == null) _beltCollider = GetComponentInChildren<Collider>();
        
        _baseSpeed = speed;
    }

    private void OnEnable()
    {
        BeltItem.RegisterBelt(this);
        DifficultyManager.OnDifficultyLevelChanged += UpdateBeltSpeed;
    }

    private void OnDisable()
    {
        BeltItem.UnregisterBelt(this);
        DifficultyManager.OnDifficultyLevelChanged -= UpdateBeltSpeed;
    }

    private void UpdateBeltSpeed(float multiplier)
    {
        speed = _baseSpeed * multiplier;
    }

    public void TrackItem(BeltItem item)
    {
        if (!_trackedItems.Contains(item))
            _trackedItems.Add(item);
    }

    public void UntrackItem(BeltItem item)
    {
        if (_trackedItems.Contains(item))
            _trackedItems.Remove(item);
    }

    void FixedUpdate()
    {
        Vector3 moveDir = direction.normalized;

        // Bantın sonunu (kenarını) hesapla
        float beltEdge = 0f;
        if (_beltCollider != null)
        {
            if (moveDir.x > 0) beltEdge = _beltCollider.bounds.max.x;
            else if (moveDir.x < 0) beltEdge = _beltCollider.bounds.min.x;
            else if (moveDir.z > 0) beltEdge = _beltCollider.bounds.max.z;
            else if (moveDir.z < 0) beltEdge = _beltCollider.bounds.min.z;
        }

        // Listeyi sondan başa dönerek güvenli silme yapıyoruz
        for (int i = _trackedItems.Count - 1; i >= 0; i--)
        {
            BeltItem item = _trackedItems[i];
            
            if (item == null || !item.gameObject.activeInHierarchy || !item.IsOnBelt || item.IsGrabbed)
            {
                _trackedItems.RemoveAt(i);
                continue;
            }

            Rigidbody itemRb = item.GetComponentInChildren<Rigidbody>();
            if (itemRb == null) continue;

            // Bantın kenarına geldik mi? (Fiziksel merkeze göre kontrol et ki uzun objeler erken kopmasın)
            if (_beltCollider != null)
            {
                bool pastEdge = false;
                
                // Objenin gerçek geometrik merkezini al (Eğer collider yoksa mecburen root pozisyonu)
                Collider itemCol = item.GetComponentInChildren<Collider>();
                Vector3 checkPos = itemCol != null ? itemCol.bounds.center : item.transform.position;

                // Objenin ağırlık merkezi (center) bantın tam ucuna geldiğinde kopar
                if (moveDir.x > 0 && checkPos.x >= beltEdge - 0.05f) pastEdge = true;
                else if (moveDir.x < 0 && checkPos.x <= beltEdge + 0.05f) pastEdge = true;
                else if (moveDir.z > 0 && checkPos.z >= beltEdge - 0.05f) pastEdge = true;
                else if (moveDir.z < 0 && checkPos.z <= beltEdge + 0.05f) pastEdge = true;

                if (pastEdge)
                {
                    // Bant sonu: Objeyi banttan kopar ve serbest düşüşe bırak
                    item.DetachFromBelt();
                    
                    // Ağırlık merkezi zaten uçurumda olduğu için, bu hız onu mükemmel bir şekilde devirecektir
                    itemRb.AddForce(moveDir * 2f, ForceMode.VelocityChange);
                    
                    // DetachFromBelt listeyi temizlediği için bu objeyi daha fazla işlemeyeceğiz
                    continue;
                }
            }

            // Önümüzde başka bir çöp var mı? (Çarpışma önleyici tren sistemi)
            Vector3 aheadPos = item.transform.position + (moveDir * 0.3f);
            Collider[] hits = Physics.OverlapSphere(aheadPos, 0.18f); 
            bool blocked = false;
            foreach (var h in hits)
            {
                if (h.transform.root == item.transform.root) continue;
                
                BeltItem other = h.GetComponentInParent<BeltItem>();
                if (other != null && other != item && other.IsOnBelt)
                {
                    // Eğer iki çöp EXACTLY aynı koordinatta doğmuşsa birbirlerini bloklamasınlar
                    if (Vector3.Distance(item.transform.position, other.transform.position) < 0.05f) continue;
                    
                    blocked = true; 
                    break;
                }
            }

            // Eğer önümüz boşsa, objenin ROOT transform'unu taşı (Kinematic olduğu için güvenli)
            if (!blocked)
            {
                item.transform.position += moveDir * speed * Time.fixedDeltaTime;
                Physics.SyncTransforms();
            }
        }
    }
}
