using System;
using UnityEngine;

public enum WasteType
{
    Paper = 0,
    Glass = 1,
    Plastic = 2,
    Metal = 3,
    Untagged = 4,
    Hourglass = 5,
    Magnet = 6
}

public struct SortResultData
{
    public bool IsCorrect;
    public WasteType ProcessedWasteType;
    public int ScoreChange;
    public int CoinChange;
    public int XpChange;
    public float HapticDuration;
    public float HapticAmplitude;
    public Vector3 ActionPosition;
    
    // Emre's additions
    public WasteType TargetBinType;
    public bool WasGoldenWaste;
    public float ReactionTime;
    public RecycleRush.Core.PrecisionSystem.PrecisionResult PrecisionData;
    public GameObject ProcessedWaste;
}

[RequireComponent(typeof(Collider))]
public class BinTrigger : MonoBehaviour
{
    [Header("Kutu Ayarları")]
    [SerializeField] private WasteType _acceptedWasteType;

    [Header("Doğru Eşleşme (Correct) Parametreleri")]
    [SerializeField] private int _correctScore = 10;
    [SerializeField] private int _correctCoin = 5;
    [SerializeField] private int _correctXp = 20;
    [SerializeField] private float _correctHapticDuration = 0.2f;
    [SerializeField] private float _correctHapticAmplitude = 0.5f;

    [Header("Yanlış Eşleşme (Incorrect) Parametreleri")]
    [SerializeField] private int _incorrectScore = -5;
    [SerializeField] private int _incorrectCoin = 0;
    [SerializeField] private int _incorrectXp = 0;
    [SerializeField] private float _incorrectHapticDuration = 0.4f;
    [SerializeField] private float _incorrectHapticAmplitude = 0.8f;

    [Header("Precision (Hassasiyet) Ayarları")]
    [SerializeField] private float _precisionRadius = 0.2f;
    [SerializeField] private bool _useDynamicRadius = true;

    [Header("Görsel Efektler")]
    [SerializeField] private GameObject _successParticlePrefab;
    [SerializeField] private GameObject _failParticlePrefab;

    public static event Action<SortResultData> OnWasteProcessed;
    public static event Action<int> OnComboChanged;

    private static int _currentCombo = 0;
    private static System.Collections.Generic.Dictionary<WasteType, Transform> _binRegistry = new System.Collections.Generic.Dictionary<WasteType, Transform>();

    private Collider _binCollider;

    private void Awake()
    {
        if (_acceptedWasteType != WasteType.Untagged && _acceptedWasteType != WasteType.Hourglass)
        {
            _binRegistry[_acceptedWasteType] = transform;
        }

        _binCollider = GetComponent<Collider>();
        if (_binCollider != null)
        {
            _binCollider.isTrigger = true;
        }
    }

    public static Transform GetBinTransform(WasteType type)
    {
        if (_binRegistry.TryGetValue(type, out Transform binTransform))
        {
            return binTransform;
        }
        return null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        
        GameObject wasteObj = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (!wasteObj.activeInHierarchy) return;

        if (GameManager.Instance != null && 
            GameManager.Instance.CurrentState != GameState.Playing && 
            GameManager.Instance.CurrentState != GameState.Tutorial)
        {
            return;
        }

        if (other.isTrigger) return;

        // --- COMPOSITE WASTE KONTROLÜ ---
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
            RejectWaste(other.attachedRigidbody);
            return;
        }

        // --- DIRTY WASTE KONTROLÜ ---
        var dirty = other.transform.root.GetComponentInChildren<RecycleRush.Interaction.DirtyWasteController>();
        if (dirty != null && dirty.IsDirty)
        {
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

        bool isCorrect = (incomingType == _acceptedWasteType) || (incomingType == WasteType.Hourglass) || (incomingType == WasteType.Magnet) || isGoldenWaste;
        
        // --- POWER-UP MANTIĞI ---
        if (incomingType == WasteType.Hourglass && isCorrect)
        {
            if (GameManager.Instance != null) GameManager.Instance.AddTime(10f);
        }
        if (incomingType == WasteType.Magnet && isCorrect)
        {
            if (GameManager.Instance != null) GameManager.Instance.ActivateMagnet(10f);
        }

        // --- KOMBO SİSTEMİ ---
        if (isCorrect)
        {
            _currentCombo++;
            if (_currentCombo > 1) OnComboChanged?.Invoke(_currentCombo);
        }
        else
        {
            if (_currentCombo > 0)
            {
                _currentCombo = 0;
                OnComboChanged?.Invoke(0);
            }
        }

        int finalScoreChange = isCorrect ? _correctScore : -_incorrectScore;
        if (isGoldenWaste) finalScoreChange *= 5;
        
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
            
            if (precisionResult.Tier == RecycleRush.Core.PrecisionSystem.PrecisionTier.Perfect)
            {
                _correctHapticAmplitude = 1.0f;
                _correctHapticDuration = 0.3f;
            }
        }

        SortResultData resultData = new SortResultData
        {
            IsCorrect = isCorrect,
            ProcessedWasteType = incomingType,
            ActionPosition = wasteObj.transform.position,
            ScoreChange = finalScoreChange,
            CoinChange = isCorrect ? _correctCoin : -_incorrectCoin,
            XpChange = isCorrect ? _correctXp : 0,
            HapticDuration = isCorrect ? _correctHapticDuration : _incorrectHapticDuration,
            HapticAmplitude = isCorrect ? _correctHapticAmplitude : _incorrectHapticAmplitude,
            TargetBinType = _acceptedWasteType,
            WasGoldenWaste = isGoldenWaste,
            ReactionTime = reactionTime,
            PrecisionData = precisionResult,
            ProcessedWaste = other.transform.root.gameObject
        };

        OnWasteProcessed?.Invoke(resultData);
        ObjectPoolManager.Instance.ReturnToPool(wasteObj);
    }

    public static WasteType GetWasteTypeFromCollider(Collider col)
    {
        if (col == null) return WasteType.Untagged;
        GameObject wasteObj = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
        foreach (Transform child in wasteObj.GetComponentsInChildren<Transform>(true))
        {
            if (CheckTag(child.gameObject, out WasteType type)) 
                return type;
        }
        return WasteType.Untagged;
    }

    public static bool CheckTag(GameObject obj, out WasteType type)
    {
        if (obj.CompareTag("Paper")) { type = WasteType.Paper; return true; }
        if (obj.CompareTag("Glass")) { type = WasteType.Glass; return true; }
        if (obj.CompareTag("Plastic")) { type = WasteType.Plastic; return true; }
        if (obj.CompareTag("Metal")) { type = WasteType.Metal; return true; }
        if (obj.CompareTag("Hourglass")) { type = WasteType.Hourglass; return true; }
        if (obj.CompareTag("Magnet")) { type = WasteType.Magnet; return true; }
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

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_binCollider == null) _binCollider = GetComponent<Collider>();
        if (_binCollider == null) return;

        float radius = _useDynamicRadius ? Mathf.Min(_binCollider.bounds.extents.x, _binCollider.bounds.extents.z) : _precisionRadius;
        Vector3 center = _binCollider.bounds.center;
        
        float perfectRatio = 0.2f; 
        float greatRatio = 0.5f;
        float goodRatio = 0.8f;
        
        Color perfectCol = new Color(1f, 0.84f, 0f, 0.5f);
        Color greatCol = new Color(0.13f, 0.59f, 0.95f, 0.5f);
        Color goodCol = new Color(0.3f, 0.8f, 0.3f, 0.5f);
        Color normalCol = new Color(1f, 1f, 1f, 0.2f);
        
        if (Application.isPlaying && RecycleRush.Core.PrecisionSystem.PrecisionManager.Instance != null && RecycleRush.Core.PrecisionSystem.PrecisionManager.Instance.Settings != null)
        {
            var settings = RecycleRush.Core.PrecisionSystem.PrecisionManager.Instance.Settings;
            perfectCol = settings.PerfectColor;
            greatCol = settings.GreatColor;
            goodCol = settings.GoodColor;
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
}
