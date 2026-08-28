using System;
using UnityEngine;

public enum WasteType
{
    Paper,
    Glass,
    Plastic,
    Metal,
    Untagged,
    Hourglass,
    Magnet
}

public struct SortResultData
{
    public bool IsCorrect;
    public int ScoreChange;
    public int CoinChange; // Gün 7: Kazanılacak Para
    public int XpChange; // Gün 7: Kazanılacak XP
    public float HapticDuration;
    public float HapticAmplitude;
    public Vector3 ActionPosition; // Ses ve Partikül efektlerinin nerede çıkacağı

    // Analitik Sistemi İçin Eklenen Veriler:
    public WasteType TargetBinType; // Hangi kutuya atıldı
    public WasteType ProcessedWasteType; // İşlenen çöp türü
    public bool WasGoldenWaste;     // Atılan obje altın çöp müydü?
    public float ReactionTime;      // Oyuncunun çöpü yakalayıp atma süresi
    
    // YENİ: Precision (Hassasiyet) verisi
    public RecycleRush.Core.PrecisionSystem.PrecisionResult PrecisionData;
    public GameObject ProcessedWaste;
}

[RequireComponent(typeof(Collider))]
public class BinTrigger : MonoBehaviour
{
    private static System.Collections.Generic.Dictionary<WasteType, UnityEngine.Transform> _binRegistry = new System.Collections.Generic.Dictionary<WasteType, UnityEngine.Transform>();
    [Header("Precision (Hassasiyet) Ayarları")]
    [Tooltip("Kutunun çarpışma sınırlarından yarıçapı otomatik hesaplar")]
    [SerializeField] private bool _useDynamicRadius = true;
    [Tooltip("Dinamik kapalıysa kullanılacak manuel yarıçap (Metre)")]
    [SerializeField] private float _precisionRadius = 0.5f;

    [Header("Kutu Ayarları")]
    [SerializeField] private WasteType _acceptedWasteType;

    [Header("Doğru Eşleşme Parametreleri")]
    [SerializeField] private int _correctScore = 10;
    [SerializeField] private int _correctCoin = 5;
    [SerializeField] private int _correctXp = 20;
    [SerializeField] private float _correctHapticDuration = 0.2f;
    [SerializeField] private float _correctHapticAmplitude = 0.5f;

    [Header("Yanlış Eşleşme Parametreleri")]
    [SerializeField] private int _incorrectScore = -5;
    [SerializeField] private int _incorrectCoin = 0;
    [SerializeField] private int _incorrectXp = 0;
    [SerializeField] private float _incorrectHapticDuration = 0.4f;
    [SerializeField] private float _incorrectHapticAmplitude = 0.8f;

    [Header("Görsel Efektler")]
    [SerializeField] private GameObject _successParticlePrefab;
    [SerializeField] private GameObject _failParticlePrefab;

    [Header("Aim Assist (Görünmez Nişan Desteği)")]
    [Tooltip("Doğru kutuya yakın fırlatılan çöpleri hafifçe kutunun ağzına yönlendirir")]
    [SerializeField] private bool _enableAimAssist = true;
    [SerializeField] private float _assistRadius = 1.3f;
    [SerializeField] private float _assistForce = 7f;
    private Collider[] _assistColliders = new Collider[10];

    public static event Action<SortResultData> OnWasteProcessed;
#pragma warning disable CS0067
    public static event Action<int> OnComboChanged;
#pragma warning restore CS0067

    private Collider _binCollider;

    private void Awake()
    {
        _binCollider = GetComponent<Collider>();
        _binRegistry[_acceptedWasteType] = transform;
        if (_binCollider != null)
        {
            _binCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.isTrigger) return;

        // Atık objesini (Rigidbody'si varsa onu, yoksa collider objesini) bul
        GameObject wasteObj = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (wasteObj == null || !wasteObj.activeInHierarchy || wasteObj.transform.localScale.sqrMagnitude < 0.001f) return;

        // Failsafe: Havuz yöneticisini veya konteyneri asla çöp sayıp havuza atmaya çalışma
        if (ObjectPoolManager.Instance != null)
        {
            if (wasteObj == ObjectPoolManager.Instance.gameObject) return;
            if (ObjectPoolManager.Instance.PoolContainer != null && (wasteObj == ObjectPoolManager.Instance.PoolContainer.gameObject || wasteObj.transform == ObjectPoolManager.Instance.PoolContainer)) return;
        }

        if (GameManager.Instance != null && 
            GameManager.Instance.CurrentState != GameState.Playing && 
            GameManager.Instance.CurrentState != GameState.Tutorial)
        {
            return;
        }

        // --- COMPOSITE WASTE KONTROLÜ ---
        bool isGlued = false;
        var glueA = wasteObj.GetComponentInChildren<RecycleRush.Interaction.WasteGlue>();
        if (glueA != null && glueA.IsActive) isGlued = true;

        if (!isGlued)
        {
            var allGlues = FindObjectsByType<RecycleRush.Interaction.WasteGlue>(FindObjectsSortMode.None);
            foreach (var g in allGlues)
            {
                if (g.IsActive && g.partB != null && (g.partB.gameObject == wasteObj || g.partB.transform.IsChildOf(wasteObj.transform)))
                {
                    isGlued = true;
                    break;
                }
            }
        }

        if (isGlued)
        {
            Debug.Log("<color=red>[BinTrigger]</color> Bu çöp başka bir çöpe yapışık! Önce iki elinle ayırmalısın.");
            RejectWaste(other.attachedRigidbody);
            return;
        }

        // --- DIRTY WASTE KONTROLÜ ---
        var dirty = wasteObj.GetComponentInChildren<RecycleRush.Interaction.DirtyWasteController>();
        if (dirty != null && dirty.IsDirty)
        {
            Debug.Log("<color=red>[BinTrigger]</color> Bu çöp çamurlu! Kutuyu kirletmemek için önce yıkamalısın.");
            RejectWaste(other.attachedRigidbody);
            return;
        }

        WasteType incomingType = GetWasteTypeFromCollider(other);
        
        // Power-Up Kontrolleri
        if (incomingType == WasteType.Hourglass)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddTime(10f);
            }
            StartCoroutine(SwallowRoutine(wasteObj));
            return;
        }

        if (incomingType == WasteType.Magnet)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ActivateMagnet(15f);
            }
            StartCoroutine(SwallowRoutine(wasteObj));
            return;
        }

        bool isGoldenWaste = false;
        var physicsTuner = wasteObj.GetComponentInChildren<RecycleRush.Interaction.ARWastePhysicsTuner>();
        if (physicsTuner != null && physicsTuner.isGoldenWaste)
        {
            isGoldenWaste = true;
        }

        if (incomingType == WasteType.Untagged && !isGoldenWaste) 
        {
            return;
        }

        bool isCorrect = (incomingType == _acceptedWasteType) || isGoldenWaste;
        
        int finalScoreChange = isCorrect ? _correctScore : _incorrectScore;
        if (isGoldenWaste) 
        {
            finalScoreChange *= 5;
        }
        
        GameObject particleToSpawn = isCorrect ? _successParticlePrefab : _failParticlePrefab;
        if (particleToSpawn != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 0.5f, 0);
            GameObject spawnedParticle = Instantiate(particleToSpawn, spawnPosition, Quaternion.identity);
            Destroy(spawnedParticle, 3f);
        }
        
        float reactionTime = 0f;
        if (physicsTuner != null)
        {
            reactionTime = Time.time - physicsTuner.SpawnTime;
        }

        // --- PRECISION HESAPLAMA ---
        RecycleRush.Core.PrecisionSystem.PrecisionResult precisionResult = default;
        if (isCorrect && RecycleRush.Core.PrecisionSystem.PrecisionManager.Instance != null)
        {
            float radius = _useDynamicRadius ? Mathf.Min(_binCollider.bounds.extents.x, _binCollider.bounds.extents.z) : _precisionRadius;
            precisionResult = RecycleRush.Core.PrecisionSystem.PrecisionManager.Instance.CalculatePrecision(
                transform, _binCollider.bounds.center, radius, other.transform.position, incomingType, _acceptedWasteType
            );
            
            // Eğer precision'dan ek bir skor veya haptic geldiyse, final puanlara ekleyebiliriz (veya ScoreManager ekler)
            // Biz haptic gücünü tier'a göre artıralım:
            if (precisionResult.Tier == RecycleRush.Core.PrecisionSystem.PrecisionTier.Perfect)
            {
                _correctHapticAmplitude = 1.0f; // Güçlü çift darbe hissi için
                _correctHapticDuration = 0.3f;
            }
        }

        // Diğer Manager sınıflarına yayınlanacak veri paketi
        SortResultData resultData = new SortResultData
        {
            IsCorrect = isCorrect,
            ActionPosition = transform.position,
            ScoreChange = finalScoreChange,
            CoinChange = isCorrect ? _correctCoin : _incorrectCoin,
            XpChange = isCorrect ? _correctXp : _incorrectXp,
            HapticDuration = isCorrect ? _correctHapticDuration : _incorrectHapticDuration,
            HapticAmplitude = isCorrect ? _correctHapticAmplitude : _incorrectHapticAmplitude,
            TargetBinType = _acceptedWasteType,
            ProcessedWasteType = incomingType,
            WasGoldenWaste = isGoldenWaste,
            ReactionTime = reactionTime,
            PrecisionData = precisionResult,
            ProcessedWaste = wasteObj
        };

        Debug.Log($"<color=magenta>[BinTrigger]</color> OnWasteProcessed sinyali fırlatılıyor! Puan: {resultData.ScoreChange} | Coin: {resultData.CoinChange} | XP: {resultData.XpChange}");

        // Event'i fırlat.
        OnWasteProcessed?.Invoke(resultData);

        // YENİ: Objeyi pat diye silmek yerine yutulma efekti (Swallow) Coroutine'i başlatıyoruz
        StartCoroutine(SwallowRoutine(wasteObj));
    }

    // --- YUTULMA EFEKTİ (SWALLOW VISUAL) ---
    private System.Collections.IEnumerator SwallowRoutine(GameObject wasteObject)
    {
        if (wasteObject == null) yield break;

        var grab = wasteObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() ?? 
                   wasteObject.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null && grab.isSelected && grab.interactionManager != null)
        {
            grab.interactionManager.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
        }

        Rigidbody rb = wasteObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }

        Collider[] colliders = wasteObject.GetComponentsInChildren<Collider>();
        foreach (var c in colliders)
        {
            if (c != null) c.enabled = false;
        }

        Vector3 startScale = wasteObject.transform.localScale;
        Vector3 startPos = wasteObject.transform.position;
        Vector3 targetPos = transform.position + Vector3.up * 0.15f; 

        float duration = 0.18f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (wasteObject == null) yield break;
            float t = elapsed / duration;
            wasteObject.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            wasteObject.transform.position = Vector3.Lerp(startPos, targetPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (wasteObject != null)
        {
            wasteObject.transform.localScale = startScale;
            foreach (var c in colliders)
            {
                if (c != null) c.enabled = true;
            }
            if (rb != null) rb.isKinematic = false;

            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnToPool(wasteObject);
            }
            else
            {
                Destroy(wasteObject);
            }
        }
    }

    // --- AIM ASSIST (MANYETİK ÇEKİM) ---
    private void FixedUpdate()
    {
        if (!_enableAimAssist) return;
        
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.5f, _assistRadius, _assistColliders);
        for (int i = 0; i < numColliders; i++)
        {
            Collider col = _assistColliders[i];
            if (col == null || col.isTrigger) continue;
            
            Rigidbody rb = col.attachedRigidbody;
            if (rb != null)
            {
                if (rb.linearVelocity.magnitude > 0.8f) 
                {
                    var grab = rb.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                    if (grab != null && !grab.isSelected)
                    {
                        WasteType type = GetWasteTypeFromCollider(col);
                        var tuner = col.GetComponentInChildren<RecycleRush.Interaction.ARWastePhysicsTuner>();
                        bool isGold = tuner != null && tuner.isGoldenWaste;
                        if (type != WasteType.Untagged && (type == _acceptedWasteType || isGold)) 
                        {
                            Vector3 dirToBin = (transform.position + Vector3.up * 0.2f) - rb.transform.position;
                            rb.AddForce(dirToBin.normalized * _assistForce, ForceMode.Acceleration);
                        }
                    }
                }
            }
        }
    }

    public static WasteType GetWasteTypeFromCollider(Collider col)
    {
        if (col == null) return WasteType.Untagged;
        GameObject target = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
        foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
        {
            if (CheckTag(child.gameObject, out WasteType type)) 
            {
                return type;
            }
        }
        return WasteType.Untagged;
    }

    public static bool CheckTag(GameObject obj, out WasteType type)
    {
        if (obj == null) { type = WasteType.Untagged; return false; }
        if (obj.CompareTag("Paper")) { type = WasteType.Paper; return true; }
        if (obj.CompareTag("Glass")) { type = WasteType.Glass; return true; }
        if (obj.CompareTag("Plastic")) { type = WasteType.Plastic; return true; }
        if (obj.CompareTag("Metal")) { type = WasteType.Metal; return true; }
        if (obj.CompareTag("Hourglass")) { type = WasteType.Hourglass; return true; }
        if (obj.CompareTag("Magnet")) { type = WasteType.Magnet; return true; }
        type = WasteType.Untagged;
        return false;
    }

    public static Transform GetBinTransform(WasteType type)
    {
        BinTrigger[] bins = FindObjectsByType<BinTrigger>(FindObjectsSortMode.None);
        foreach (var b in bins)
        {
            if (b._acceptedWasteType == type) return b.transform;
        }
        return null;
    }

    public static bool TryGetBinsCenter(out Vector3 center)
    {
        center = Vector3.zero;
        BinTrigger[] bins = FindObjectsByType<BinTrigger>(FindObjectsSortMode.None);
        if (bins != null && bins.Length > 0)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var b in bins)
            {
                if (b != null && b.gameObject.activeInHierarchy)
                {
                    sum += b.transform.position;
                    count++;
                }
            }
            if (count > 0)
            {
                center = sum / count;
                return true;
            }
        }
        return false;
    }

#if UNITY_EDITOR
    // --- PRECISION CALIBRATION TOOL (GIZMOS) ---
    private void OnDrawGizmos()
    {
        if (_binCollider == null) _binCollider = GetComponent<Collider>();
        if (_binCollider == null) return;

        float radius = _useDynamicRadius ? Mathf.Min(_binCollider.bounds.extents.x, _binCollider.bounds.extents.z) : _precisionRadius;
        Vector3 center = _binCollider.bounds.center;
        
        // Settings yoksa varsayılan oranlar
        float perfectRatio = 0.2f; 
        float greatRatio = 0.5f;
        float goodRatio = 0.8f;
        
        Color perfectCol = new Color(1f, 0.84f, 0f, 0.5f); // Altın
        Color greatCol = new Color(0.13f, 0.59f, 0.95f, 0.5f); // Mavi
        Color goodCol = new Color(0.3f, 0.8f, 0.3f, 0.5f); // Yeşil
        Color normalCol = new Color(1f, 1f, 1f, 0.2f); // Beyaz
        
        if (Application.isPlaying && RecycleRush.Core.PrecisionSystem.PrecisionManager.Instance != null && RecycleRush.Core.PrecisionSystem.PrecisionManager.Instance.Settings != null)
        {
            var settings = RecycleRush.Core.PrecisionSystem.PrecisionManager.Instance.Settings;
            perfectCol = settings.PerfectColor;
            greatCol = settings.GreatColor;
            goodCol = settings.GoodColor;
            
            // Ayarlardan okunan kalibrasyon oranları
            perfectRatio = settings.PerfectRadiusPercent;
            greatRatio = settings.GreatRadiusPercent;
            goodRatio = settings.GoodRadiusPercent;
        }

        UnityEditor.Handles.color = normalCol;
        UnityEditor.Handles.DrawWireDisc(center, transform.up, radius);
        
        UnityEditor.Handles.color = goodCol;
        UnityEditor.Handles.DrawWireDisc(center, transform.up, radius * goodRatio);
        
        UnityEditor.Handles.color = greatCol;
        UnityEditor.Handles.DrawWireDisc(center, transform.up, radius * greatRatio);
        
        UnityEditor.Handles.color = perfectCol;
        UnityEditor.Handles.DrawWireDisc(center, transform.up, radius * perfectRatio);
    }
#endif
    private void RejectWaste(Rigidbody rb)
    {
        if (rb != null)
        {
            var grab = rb.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() ??
                       rb.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab != null && grab.isSelected)
            {
                if (grab.interactionManager != null)
                {
                    grab.interactionManager.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
                }
                grab.enabled = false;
                grab.enabled = true;
            }

            if (rb.isKinematic) rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Fırlatma gücünü inanılmaz derecede kıstık. Sadece kutunun içinden hafifçe sekip hemen dibine (yere) düşecek.
            rb.AddForce(new Vector3(0, 1.5f, -0.5f), ForceMode.Impulse);
        }

        if (_failParticlePrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 0.5f, 0);
            Destroy(Instantiate(_failParticlePrefab, spawnPosition, Quaternion.identity), 3f);
        }
    }
}