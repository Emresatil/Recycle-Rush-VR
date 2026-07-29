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
            
            // Obje silinmiş, havuza dönmüş veya deaktif olmuş olabilir
            if (item == null || !item.gameObject.activeInHierarchy || !item.IsOnBelt || item.IsGrabbed)
            {
                _trackedItems.RemoveAt(i);
                continue;
            }

            Rigidbody itemRb = item.GetComponent<Rigidbody>();
            if (itemRb == null) continue;

            // Bantın kenarına geldik mi?
            if (_beltCollider != null)
            {
                bool pastEdge = false;
                if (moveDir.x > 0 && itemRb.position.x >= beltEdge - 0.15f) pastEdge = true;
                else if (moveDir.x < 0 && itemRb.position.x <= beltEdge + 0.15f) pastEdge = true;
                else if (moveDir.z > 0 && itemRb.position.z >= beltEdge - 0.15f) pastEdge = true;
                else if (moveDir.z < 0 && itemRb.position.z <= beltEdge + 0.15f) pastEdge = true;

                if (pastEdge)
                {
                    // Bant sonu: Kinematic'i kapat, serbest düşüşe (grinder'a) geçsin
                    itemRb.isKinematic = false;
                    itemRb.useGravity = true;
                    itemRb.constraints = RigidbodyConstraints.None;
                    
                    _trackedItems.RemoveAt(i);
                    continue; // Bu objeyi taşımayı bırak
                }
            }

            // Kilit nokta: Önünde başka bir çöp (BeltItem) var mı? (Çarpışma önleyici tren sistemi)
            // Kinematic objeler birbirine çarpamayacağı için bunu OverlapSphere ile biz kontrol ediyoruz
            Vector3 aheadPos = itemRb.position + (moveDir * 0.3f);
            Collider[] hits = Physics.OverlapSphere(aheadPos, 0.18f); // 18cm ilerisini tara
            bool blocked = false;
            foreach (var h in hits)
            {
                if (h.transform.root == item.transform.root) continue;
                
                BeltItem other = h.GetComponentInParent<BeltItem>();
                if (other != null && other != item && other.IsOnBelt)
                {
                    blocked = true; // Önümüzde yığılma var, dur!
                    break;
                }
            }

            // Eğer önümüz boşsa, Kinematic objeyi MovePosition ile yumuşakça ileri taşı
            if (!blocked)
            {
                Vector3 nextPos = itemRb.position + moveDir * speed * Time.fixedDeltaTime;
                itemRb.MovePosition(nextPos);
            }
        }
    }
}
