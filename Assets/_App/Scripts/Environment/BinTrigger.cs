using System;
using UnityEngine;

// Atık türleri için Enum yapısı (Inspector'dan kolayca seçilebilmesi için)
public enum WasteType
{
    Paper,
    Glass,
    Plastic,
    Metal,
    Untagged // Atık olmayan objeler (oyuncunun eli vb.) için
}

// Event üzerinden diğer sistemlere (Manager'lara) aktarılacak paket veri yapısı
public struct SortResultData
{
    public bool IsCorrect;
    public int ScoreChange;
    public float HapticDuration;
    public float HapticAmplitude;
    public Vector3 ActionPosition; // Ses ve Partikül efektlerinin nerede çıkacağı

    // Analitik Sistemi İçin Eklenen Veriler:
    public WasteType TargetBinType; // Hangi kutuya atıldı
    public bool WasGoldenWaste;     // Atılan obje altın çöp müydü?
    public float ReactionTime;      // Oyuncunun çöpü yakalayıp atma süresi
    
    // YENİ: Precision (Hassasiyet) verisi
    public RecycleRush.Core.PrecisionSystem.PrecisionResult PrecisionData;
}

[RequireComponent(typeof(Collider))]
public class BinTrigger : MonoBehaviour
{
    [Header("Precision (Hassasiyet) Ayarları")]
    [Tooltip("Kutunun çarpışma sınırlarından yarıçapı otomatik hesaplar")]
    [SerializeField] private bool _useDynamicRadius = true;
    [Tooltip("Dinamik kapalıysa kullanılacak manuel yarıçap (Metre)")]
    [SerializeField] private float _precisionRadius = 0.5f;

    [Header("Kutu Ayarları")]
    [Tooltip("Bu kutunun kabul ettiği doğru atık türü")]
    [SerializeField] private WasteType _acceptedWasteType;

    [Header("Doğru Eşleşme (Correct) Parametreleri")]
    [SerializeField] private int _correctScore = 10;
    [SerializeField] private float _correctHapticDuration = 0.2f;
    [SerializeField] private float _correctHapticAmplitude = 0.5f;

    [Header("Yanlış Eşleşme (Incorrect) Parametreleri")]
    [SerializeField] private int _incorrectScore = -5;
    [SerializeField] private float _incorrectHapticDuration = 0.4f;
    [SerializeField] private float _incorrectHapticAmplitude = 0.8f;

    [Header("Görsel Efektler (VFX)")]
    [Tooltip("Doğru kutuya atıldığında çıkacak yeşil patlama efekti (Prefab)")]
    [SerializeField] private GameObject _successParticlePrefab;
    [Tooltip("Yanlış kutuya atıldığında çıkacak kırmızı duman efekti (Prefab)")]
    [SerializeField] private GameObject _failParticlePrefab;

    // Sistemler arası spagetti bağlantıları engelleyen (Loose Coupling) statik Action Event'imiz.
    // ScoreManager, AudioManager ve HapticManager sadece bu event'e Abone (Subscribe) olacaktır.
    public static event Action<SortResultData> OnWasteProcessed;

    private Collider _binCollider;

    private void Awake()
    {
        // GC Optimizasyonu: GetComponent çağrısını Awake içinde Cache'liyoruz.
        _binCollider = GetComponent<Collider>();
        
        // Kutunun çarpışma sınırının mutlaka Trigger modunda olduğundan emin oluyoruz.
        if (_binCollider != null)
        {
            _binCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // GÜVENLİK: Eğer oyun aktif veya öğretici modunda değilse puan hesaplama ve atığı yoksay!
        if (GameManager.Instance != null && 
            GameManager.Instance.CurrentState != GameState.Playing && 
            GameManager.Instance.CurrentState != GameState.Tutorial)
        {
            Debug.Log("<color=red>[BinTrigger]</color> Oyun aktif değil! Atık kutuya girse de puan kazandırmaz.");
            return;
        }

        Debug.Log($"<color=orange>[BinTrigger]</color> Kutunun içine bir şey girdi! Giren şeyin adı: {other.name}");

        // Giren objenin atık türünü alıyoruz.
        WasteType incomingType = GetWasteTypeFromCollider(other);
        
        Debug.Log($"<color=yellow>[BinTrigger]</color> {other.name} objesinin Tag kontrolü yapıldı. Bulunan Atık Türü: {incomingType}");

        // Altın Çöp (Golden Waste) kontrolü yapıyoruz.
        bool isGoldenWaste = false;
        var physicsTuner = other.transform.root.GetComponentInChildren<RecycleRush.Interaction.ARWastePhysicsTuner>();
        if (physicsTuner != null && physicsTuner.isGoldenWaste)
        {
            isGoldenWaste = true;
        }

        // Eğer giren obje bir atık değilse ve Altın Çöp de değilse işlemi iptal et.
        if (incomingType == WasteType.Untagged && !isGoldenWaste) 
        {
            Debug.Log("<color=red>[BinTrigger]</color> Bu obje Untagged (Etiketsiz) olduğu için puanlama yapılmadı ve silinmedi!");
            return;
        }

        // Doğruluk mantığı: Giren atığın türü, kutunun kabul ettiği türe eşit mi? YADA obje Altın Çöp mü (Evrensel Joker)?
        bool isCorrect = (incomingType == _acceptedWasteType) || isGoldenWaste;
        
        // Eğer Altın çöp doğru kutuya girerse (zaten isCorrect her türlü true olur), ekstra bonus veriyoruz.
        int finalScoreChange = isCorrect ? _correctScore : _incorrectScore;
        if (isGoldenWaste) 
        {
            finalScoreChange *= 5; // Altın çöp puanı 5'e katlar!
            Debug.Log("<color=yellow>[Golden Waste]</color> Kutuya Altın Çöp girdi! Puan 5'e katlandı.");
        }
        
        // --- GÖRSEL EFEKT (PARTİCÜL) ÇAĞIRMA ---
        GameObject particleToSpawn = isCorrect ? _successParticlePrefab : _failParticlePrefab;
        if (particleToSpawn != null)
        {
            // Kutunun merkezinden yarım metre yukarıda oluştur
            Vector3 spawnPosition = transform.position + new Vector3(0, 0.5f, 0);
            GameObject spawnedParticle = Instantiate(particleToSpawn, spawnPosition, Quaternion.identity);
            
            Debug.Log($"<color=white>[VFX]</color> Partikül oluşturuldu: {spawnedParticle.name} at {spawnPosition}");
            
            // Sahne kirlenmesin diye efekti 3 saniye sonra otomatik sil
            Destroy(spawnedParticle, 3f);
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[VFX]</color> Kutuda {(isCorrect ? "Success" : "Fail")} particle prefab'i eksik!");
        }
        
        Debug.Log($"<color=cyan>[BinTrigger]</color> Kutu Türü: {_acceptedWasteType} | Gelen Çöp Türü: {incomingType} | Eşleşme: {isCorrect}");

        // Tepki Süresi (Reaction Time) Hesaplama
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
            HapticDuration = isCorrect ? _correctHapticDuration : _incorrectHapticDuration,
            HapticAmplitude = isCorrect ? _correctHapticAmplitude : _incorrectHapticAmplitude,
            TargetBinType = _acceptedWasteType,
            WasGoldenWaste = isGoldenWaste,
            ReactionTime = reactionTime,
            PrecisionData = precisionResult
        };

        Debug.Log($"<color=magenta>[BinTrigger]</color> OnWasteProcessed sinyali fırlatılıyor! Puan değişimi: {resultData.ScoreChange}");

        // Event'i fırlat.
        OnWasteProcessed?.Invoke(resultData);

        // İşlem tamamlandıktan sonra atık objesini sahneden yok et.
        // Çöplerin içi içe geçmiş prefablar olma ihtimaline karşı her zaman en dıştaki (Root) objeyi siliyoruz.
        Debug.Log($"<color=green>[BinTrigger]</color> {other.transform.root.name} objesi havuza geri gönderildi.");
        ObjectPoolManager.Instance.ReturnToPool(other.transform.root.gameObject);
    }

    /// <summary>
    /// Objenin neresine (Root, Mesh, Collider) Tag konulduğunu bilemeyeceğimiz için,
    /// objenin tamamını (kendisini ve tüm alt çocuklarını) tarayıp Tag'i bulur. (Foolproof)
    /// </summary>
    private WasteType GetWasteTypeFromCollider(Collider col)
    {
        // En dış (Root) objeyi bul (Bu sayede prefab'ın en tepesine ulaşırız)
        Transform rootTransform = col.transform.root;

        // Root objenin kendisine ve BÜTÜN alt objelerine (çocuklarına) sırayla bak
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
}
