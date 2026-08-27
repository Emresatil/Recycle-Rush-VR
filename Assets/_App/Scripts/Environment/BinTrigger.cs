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
    public int CoinChange;
    public int XpChange;
    public float HapticDuration;
    public float HapticAmplitude;
    public Vector3 ActionPosition;

    public WasteType TargetBinType;
    public bool WasGoldenWaste;
    public float ReactionTime;
    
    public RecycleRush.Core.PrecisionSystem.PrecisionResult PrecisionData;
    public WasteType ProcessedWasteType;
    public GameObject ProcessedWaste;
}

[RequireComponent(typeof(Collider))]
public class BinTrigger : MonoBehaviour
{
    private static System.Collections.Generic.Dictionary<WasteType, UnityEngine.Transform> _binRegistry = new System.Collections.Generic.Dictionary<WasteType, UnityEngine.Transform>();
    
    [Header("Precision (Hassasiyet) Ayarları")]
    [SerializeField] private bool _useDynamicRadius = true;
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

    public static event Action<SortResultData> OnWasteProcessed;

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
        if (GameManager.Instance != null && 
            GameManager.Instance.CurrentState != GameState.Playing && 
            GameManager.Instance.CurrentState != GameState.Tutorial)
        {
            return;
        }

        if (other.isTrigger) return;

        bool isGlued = false;
        var glueA = other.transform.root.GetComponentInChildren<RecycleRush.Interaction.WasteGlue>();
        if (glueA != null && glueA.IsActive) isGlued = true;

        if (!isGlued)
        {
            var allGlues = FindObjectsByType<RecycleRush.Interaction.WasteGlue>(FindObjectsSortMode.None);
            foreach (var g in allGlues)
            {
                if (g.IsActive && g.partB != null && g.partB.transform.root == other.transform.root)
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

        var dirty = other.transform.root.GetComponentInChildren<RecycleRush.Interaction.DirtyWasteController>();
        if (dirty != null && dirty.IsDirty)
        {
            Debug.Log("<color=red>[BinTrigger]</color> Bu çöp çamurlu! Kutuyu kirletmemek için önce yıkamalısın.");
            RejectWaste(other.attachedRigidbody);
            return;
        }

        WasteType incomingType = GetWasteTypeFromCollider(other);
        
        bool isGoldenWaste = false;
        var physicsTuner = other.transform.root.GetComponentInChildren<RecycleRush.Interaction.ARWastePhysicsTuner>();
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
            Destroy(Instantiate(particleToSpawn, spawnPosition, Quaternion.identity), 3f);
        }
        
        float reactionTime = 0f;
        if (physicsTuner != null)
        {
            reactionTime = Time.time - physicsTuner.SpawnTime;
        }

        RecycleRush.Core.PrecisionSystem.PrecisionResult precisionResult = default;
        if (isCorrect && RecycleRush.Core.PrecisionSystem.PrecisionManager.Instance != null)
        {
            float radius = _useDynamicRadius ? Mathf.Min(_binCollider.bounds.extents.x, _binCollider.bounds.extents.z) : _precisionRadius;
            precisionResult = RecycleRush.Core.PrecisionSystem.PrecisionManager.Instance.CalculatePrecision(
                transform, _binCollider.bounds.center, radius, other.transform.position, incomingType, _acceptedWasteType
            );
            
            if (precisionResult.Tier == RecycleRush.Core.PrecisionSystem.PrecisionTier.Perfect)
            {
                _correctHapticAmplitude = 1.0f;
                _correctHapticDuration = 0.3f;
            }
        }

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
            WasGoldenWaste = isGoldenWaste,
            ReactionTime = reactionTime,
            PrecisionData = precisionResult,
            ProcessedWaste = other.transform.root.gameObject
        };

        Debug.Log($"<color=magenta>[BinTrigger]</color> OnWasteProcessed sinyali fırlatılıyor! Puan: {resultData.ScoreChange} | Coin: {resultData.CoinChange} | XP: {resultData.XpChange}");

        OnWasteProcessed?.Invoke(resultData);

        // YENİ: Objeyi pat diye silmek yerine yutulma efekti (Swallow) Coroutine'i başlatıyoruz
        StartCoroutine(SwallowRoutine(other.transform.root.gameObject));
    }

    // --- 3. YUTULMA EFEKTİ (SWALLOW VİSUAL) ---
    private System.Collections.IEnumerator SwallowRoutine(GameObject wasteObject)
    {
        Rigidbody rb = wasteObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Collider[] colliders = wasteObject.GetComponentsInChildren<Collider>();
        foreach(var c in colliders) c.enabled = false;

        Vector3 startScale = wasteObject.transform.localScale;
        Vector3 startPos = wasteObject.transform.position;
        Vector3 targetPos = transform.position + Vector3.up * 0.15f; 

        float duration = 0.18f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            wasteObject.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            wasteObject.transform.position = Vector3.Lerp(startPos, targetPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        wasteObject.transform.localScale = startScale;
        foreach(var c in colliders) c.enabled = true;
        if (rb != null) rb.isKinematic = false;

        ObjectPoolManager.Instance.ReturnToPool(wasteObject);
    }

    public static WasteType GetWasteTypeFromCollider(Collider col)
    {
        Transform rootTransform = col.transform.root;
        foreach (Transform child in rootTransform.GetComponentsInChildren<Transform>(true))
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
        if (obj.CompareTag("Paper")) { type = WasteType.Paper; return true; }
        if (obj.CompareTag("Glass")) { type = WasteType.Glass; return true; }
        if (obj.CompareTag("Plastic")) { type = WasteType.Plastic; return true; }
        if (obj.CompareTag("Metal")) { type = WasteType.Metal; return true; }
        type = WasteType.Untagged;
        return false;
    }

    private void RejectWaste(Rigidbody rb)
    {
        if (rb != null)
        {
            var grab = rb.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab != null && grab.isSelected)
            {
                grab.enabled = false;
                grab.enabled = true;
            }

            rb.linearVelocity = Vector3.zero;
            rb.AddForce(new Vector3(0, 1.5f, -0.5f), ForceMode.Impulse);
        }

        if (_failParticlePrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 0.5f, 0);
            Destroy(Instantiate(_failParticlePrefab, spawnPosition, Quaternion.identity), 3f);
        }
    }

    public static UnityEngine.Transform GetBinTransform(WasteType type)
    {
        if (_binRegistry != null && _binRegistry.TryGetValue(type, out UnityEngine.Transform binTransform))
        {
            return binTransform;
        }
        return null;
    }

    public static event System.Action<int> OnComboChanged;

    [Header("Aim Assist (Görünmez Nişan Desteği)")]
    [SerializeField] private bool _enableAimAssist = true;
    [SerializeField] private float _assistRadius = 1.3f;
    [SerializeField] private float _assistForce = 7f;
    private Collider[] _assistColliders = new Collider[10];

    // --- 1. AIM ASSIST (MANYETİK ÇEKİM) ---
    private void FixedUpdate()
    {
        if (!_enableAimAssist) return;
        
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.5f, _assistRadius, _assistColliders);
        for (int i = 0; i < numColliders; i++)
        {
            Collider col = _assistColliders[i];
            if (col.isTrigger) continue;
            
            Rigidbody rb = col.attachedRigidbody;
            if (rb != null)
            {
                if (rb.linearVelocity.magnitude > 0.8f) 
                {
                    var grab = rb.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                    if (grab != null && !grab.isSelected)
                    {
                        WasteType type = GetWasteTypeFromCollider(col);
                        if (type != WasteType.Untagged && type == _acceptedWasteType) 
                        {
                            Vector3 dirToBin = (transform.position + Vector3.up * 0.2f) - rb.transform.position;
                            rb.AddForce(dirToBin.normalized * _assistForce, ForceMode.Acceleration);
                        }
                    }
                }
            }
        }
    }
}
