using System;
using UnityEngine;

public enum WasteType
{
    Paper,
    Glass,
    Plastic,
    Metal,
    Untagged
}

public struct SortResultData
{
    public bool IsCorrect;
    public int ScoreChange;
    public int CoinChange; // Gün 7: Kazanılacak Para
    public int XpChange; // Gün 7: Kazanılacak XP
    public float HapticDuration;
    public float HapticAmplitude;
    public Vector3 ActionPosition;
    public WasteType TargetBinType;
    public bool WasGoldenWaste;
    public float ReactionTime;
    public RecycleRush.Core.PrecisionSystem.PrecisionResult PrecisionData;
}

[RequireComponent(typeof(Collider))]
public class BinTrigger : MonoBehaviour
{
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
            Debug.Log("<color=red>[BinTrigger]</color> Bu çöp başka bir çöpe yapışık! Önce iki elinle ayırmalısın.");
            RejectWaste(other.attachedRigidbody);
            return;
        }

        // --- DIRTY WASTE KONTROLÜ ---
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
            GameObject spawnedParticle = Instantiate(particleToSpawn, spawnPosition, Quaternion.identity);
            Destroy(spawnedParticle, 3f);
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

            CoinChange
            HapticDuration = isCorrect ? _correctHapticDuration : _incorrectHapticDuration,
            HapticAmplitude = isCorrect ? _correctHapticAmplitude : _incorrectHapticAmplitude,
            TargetBinType = _acceptedWasteType,
            WasGoldenWaste = isGoldenWaste,
            ReactionTime = reactionTime,
            PrecisionData = precisionResult
        };


        OnWasteProcessed?.Invoke(resultData);

        ObjectPoolManager.Instance.ReturnToPool(other.transform.root.gameObject);
    }

    private WasteType GetWasteTypeFromCollider(Collider col)
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

    private bool CheckTag(GameObject obj, out WasteType type)
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
            rb.AddForce(new Vector3(0, 3f, -2f), ForceMode.Impulse);
        }

        if (_failParticlePrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 0.5f, 0);
            Destroy(Instantiate(_failParticlePrefab, spawnPosition, Quaternion.identity), 3f);
        }
    }
}
