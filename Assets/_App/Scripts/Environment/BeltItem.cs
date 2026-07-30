using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Her atık objeye otomatik eklenen bileşen.
/// Spawn anında objeyi Kinematic yapar ve en yakın banta kaydeder.
/// </summary>
public class BeltItem : MonoBehaviour
{
    private Rigidbody _rb;
    private XRGrabInteractable _grab;
    private bool _isOnBelt = false;
    private bool _isGrabbed = false;

    private Vector3 _initialLocalPos;
    private Quaternion _initialLocalRot;
    private Transform _childRbTransform;

    private static List<BeltMovement> _allBelts = new List<BeltMovement>();
    private BeltMovement _currentBelt;

    public bool IsOnBelt => _isOnBelt;
    public bool IsGrabbed => _isGrabbed;

    public static void RegisterBelt(BeltMovement belt)
    {
        if (belt != null && !_allBelts.Contains(belt)) _allBelts.Add(belt);
    }

    public static void UnregisterBelt(BeltMovement belt)
    {
        _allBelts.Remove(belt);
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) _rb = GetComponentInChildren<Rigidbody>();
        
        _grab = GetComponent<XRGrabInteractable>();
        if (_grab == null) _grab = GetComponentInChildren<XRGrabInteractable>();

        Rigidbody childRb = GetComponentInChildren<Rigidbody>();
        if (childRb != null && childRb.gameObject != this.gameObject)
        {
            _childRbTransform = childRb.transform;
            _initialLocalPos = _childRbTransform.localPosition;
            _initialLocalRot = _childRbTransform.localRotation;
        }
    }

    private void OnEnable()
    {
        if (_grab != null)
        {
            _grab.selectEntered.AddListener(OnGrabbed);
            _grab.selectExited.AddListener(OnReleased);
        }

        // Spawn olduğunda direkt Kinematic — banta kaydet
        AttachToBelt();
    }

    private void OnDisable()
    {
        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(OnGrabbed);
            _grab.selectExited.RemoveListener(OnReleased);
        }

        DetachFromBelt();

        // Havuza dönerken orijinal offset'e dön (Kayma bug'ı çözümü)
        // Çünkü oyuncu child Rigidbody'i tutup uzağa götürdüğünde root burada kalıyordu.
        // Havuzdan tekrar çıktığında mesh'in kaymasını önlemek için fabrika ayarlarına döndürüyoruz.
        if (_childRbTransform != null)
        {
            _childRbTransform.localPosition = _initialLocalPos;
            _childRbTransform.localRotation = _initialLocalRot;
        }
    }

    /// <summary>
    /// Objeyi banttan zorla ayırır, fiziksel düşüşünü başlatır ve diğer objelerin önünü açar.
    /// </summary>
    public void DetachFromBelt()
    {
        _isOnBelt = false;
        
        if (_currentBelt != null)
        {
            _currentBelt.UntrackItem(this);
            _currentBelt = null;
        }

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.constraints = RigidbodyConstraints.None;
        }
    }

    private void AttachToBelt()
    {
        // En yakın bant scriptini bul
        BeltMovement nearestBelt = null;
        float minDist = float.MaxValue;
        foreach (var belt in _allBelts)
        {
            if (belt == null) continue;
            float dist = Vector3.Distance(transform.position, belt.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestBelt = belt;
            }
        }

        if (nearestBelt == null)
        {
            // Bant yoksa normal dynamic bırak
            _rb.isKinematic = false;
            _rb.useGravity = true;
            return;
        }

        _currentBelt = nearestBelt;
        _isOnBelt = true;
        _isGrabbed = false;

        // Dik tut
        Vector3 euler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

        // KİNETİK MOD — Fizik çarpışma hesaplaması tamamen devre dışı
        // Not: Kinematic bir objeye hız (velocity) verilemez, bu Unity 6'da kırmızı hata verir.
        // isKinematic = true yapıldığında zaten obje fizik motorundan kopup hızını sıfırlar.
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.None;

        // TAM HİZALAMA (HAVADA GİTME VE BATAĞA SAPLANMA ÇÖZÜMÜ)
        // Rotasyonu değiştirdiğimiz için fizik motoruna yerleri güncellemesini söyle
        Physics.SyncTransforms();

        if (_currentBelt.BeltCollider != null)
        {
            Collider myCol = GetComponentInChildren<Collider>();
            if (myCol != null)
            {
                // Bantın en üst noktası (yüzeyi)
                float beltTopY = _currentBelt.BeltCollider.bounds.max.y;
                // Objenin en alt noktası (tabanı)
                float myBottomY = myCol.bounds.min.y;
                
                // Aradaki farkı bul ve objeyi tam olarak bant yüzeyine oturt
                float diff = beltTopY - myBottomY;
                transform.position += new Vector3(0, diff, 0);
                
                // Rigidbody pozisyonunu anında senkronize et (Eğer rb alt objedeyse pozisyonunu bozmamak için SyncTransforms kullanıyoruz)
                Physics.SyncTransforms();
            }
        }

        _currentBelt.TrackItem(this);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        _isGrabbed = true;
        if (_currentBelt != null)
        {
            _currentBelt.UntrackItem(this);
            _currentBelt = null;
        }
        _isOnBelt = false;

        // Obje tutulduğunda fizik motorunu XR Interaction Toolkit yönetir. 
        // Burada isKinematic'e dokunmak obje elinizdeyken fiziksel patlamalara (elden uçup kaybolmasına) sebep olur!
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        _isGrabbed = false;
        // XR Interaction Toolkit'in tutarsız geri yükleme işlemini ezmek için Update döngüsüne bırakıyoruz.
    }

    private void Update()
    {
        // Eğer obje elde değilse VE banta kayıtlı değilse, KESİNLİKLE fiziksel olarak düşmeli!
        // XR Interaction Toolkit bazen saniyeler sonra bile Kinematic durumunu geri yükleyebiliyor.
        // Bu yüzden her karede kontrol edip gerekirse kilitleri zorla açıyoruz.
        if (!_isGrabbed && !_isOnBelt && _rb != null)
        {
            if (_rb.isKinematic)
            {
                _rb.isKinematic = false;
                _rb.useGravity = true;
                _rb.constraints = RigidbodyConstraints.None;
            }
        }
    }
}
