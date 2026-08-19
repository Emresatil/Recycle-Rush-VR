using System.Collections;
using UnityEngine;

public class WasteSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Üretilecek atık prefab'larının listesi")]
    public GameObject[] wastePrefabs;
    
    [Header("Surface Integration")]
    [Tooltip("Eğer sahnede ISurfaceProvider varsa çöpler bu yüzey türünün üzerinde doğar.")]
    public RecycleRush.Environment.SurfaceType targetSurfaceType = RecycleRush.Environment.SurfaceType.Any;
    
    private RecycleRush.Environment.ISurfaceProvider _surfaceProvider;

    [Tooltip("Atıkların düşeceği başlangıç noktası")]
    public Transform spawnPoint;

    [Header("Golden Waste Settings")]
    [Tooltip("Nadir (Joker) olarak düşecek Altın Çöp Prefab'ı")]
    public GameObject goldenWastePrefab;
    [Tooltip("Altın Çöp'ün çıkma ihtimali (Yüzde % olarak)")]
    [Range(0f, 100f)] public float goldenSpawnChance = 5f;

    [Header("Visual Effects")]
    [Tooltip("Çöplerin çıktığı portalın görsel animatörü (İsteğe bağlı)")]
    public RecycleRush.Environment.PortalAnimator portalAnimator;
    
    // YENİ (Analytics): Altın çöp üretildiğinde dinleyicilere (AnalyticsManager) fırlatılan sinyal
    public static event System.Action OnGoldenWasteSpawned;
    
    [Header("Organik Zamanlama (Zorluk)")]
    [Tooltip("En az kaç saniyede bir atık düşsün?")]
    public float minSpawnInterval = 0.8f;
    [Tooltip("En fazla kaç saniyede bir atık düşsün?")]
    public float maxSpawnInterval = 1.5f;



    // Tekrarı önlemek için son üretilen çöpü hafızada tutuyoruz
    private GameObject _lastSpawnedPrefab = null;

    [Header("Composite Waste Settings")]
    [Tooltip("Görsel bağ/bant materyali. Composite çöpleri bağlarken kullanılır.")]
    [SerializeField] private Material _tapeMaterial;

    [Header("Dirty Waste Settings")]
    [Tooltip("Kirli çöplerin üzerine eklenecek çamur/kir balçığı görseli (Prefab)")]
    [SerializeField] private GameObject _dirtVisualPrefab;

    private Coroutine _spawnCoroutine;

    private float _baseMinSpawnInterval;
    private float _baseMaxSpawnInterval;

    private void Awake()
    {
        if (spawnPoint == null)
        {
            spawnPoint = this.transform; // Eğer atanmamışsa kendi transformunu kullan
        }

        // Sahnede ISurfaceProvider arayüzünü uygulayan bir yönetici bul
        var components = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var comp in components)
        {
            if (comp is RecycleRush.Environment.ISurfaceProvider provider)
            {
                _surfaceProvider = provider;
                break;
            }
        }

        // Zorluk seviyesi değiştikçe baz alınacak orijinal değerleri önbelleğe (Cache) alıyoruz.
        _baseMinSpawnInterval = minSpawnInterval;
        _baseMaxSpawnInterval = maxSpawnInterval;
    }

    private void OnEnable()
    {
        // Event dinleyicisini ekle (Abone ol)
        GameManager.OnGameStateChanged += HandleGameStateChanged;
        DifficultyManager.OnDifficultyLevelChanged += UpdateSpawnSpeed;
    }

    private void OnDisable()
    {
        // Script veya obje kapandığında Event aboneliğini kaldır (Memory leak önlemi)
        GameManager.OnGameStateChanged -= HandleGameStateChanged;
        DifficultyManager.OnDifficultyLevelChanged -= UpdateSpawnSpeed;
    }

    /// <summary>
    /// DifficultyManager'dan gelen hız çarpanına göre atık üretme sıklığını günceller.
    /// </summary>
    private void UpdateSpawnSpeed(float multiplier)
    {
        // Zorluk arttıkça bekleme süresi kısalır ama çakışmayı önlemek için minimum 0.6s sınır konur
        minSpawnInterval = Mathf.Max(0.6f, _baseMinSpawnInterval / multiplier);
        maxSpawnInterval = Mathf.Max(1.0f, _baseMaxSpawnInterval / multiplier);
        
        Debug.Log($"<color=cyan>[WasteSpawner]</color> Yeni zorluğa uyarlandı! Üretim süresi: {minSpawnInterval:F1}s - {maxSpawnInterval:F1}s");
    }

    private void Start()
    {
        if (wastePrefabs.Length == 0 || spawnPoint == null)
        {
            Debug.LogWarning("WasteSpawner: Prefab listesi veya Spawn Point boş!");
            return;
        }

        // Eğer oyun bizden önce çoktan Playing statüsüne geçmişse (Start çalışma sırası farkından) manuel tetikle
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
        {
            HandleGameStateChanged(GameState.Playing);
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        Debug.Log($"<color=magenta>[WasteSpawner]</color> Oyun Durumu Yakalandı: {newState}");
        if (newState == GameState.Playing)
        {
            // Sadece oyun aktifken spawn işlemini başlat
            if (_spawnCoroutine == null)
            {
                Debug.Log("<color=magenta>[WasteSpawner]</color> Coroutine BAŞLATILIYOR!");
                _spawnCoroutine = StartCoroutine(SpawnRoutine());
            }
        }
        else
        {
            // Pause veya GameOver durumunda üretimi durdur
            if (_spawnCoroutine != null)
            {
                Debug.Log("<color=magenta>[WasteSpawner]</color> Coroutine DURDURULUYOR!");
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }
        }
    }

    IEnumerator SpawnRoutine()
    {
        // Oyun başlarken oyuncuya 2 saniye hazırlanma payı ver
        yield return new WaitForSeconds(2f);

        while (true)
        {
            bool spawned = SpawnWaste();

            if (spawned)
            {
                // 1. Özellik: Eşit zaman aralıkları. (Rastgelelik kaldırıldı, tam orta değer kullanılıyor)
                float fixedWait = (minSpawnInterval + maxSpawnInterval) / 2f;
                yield return new WaitForSeconds(fixedWait);
            }
            else
            {
                // Eğer doğma noktası doluysa tam tur (örn: 3 saniye) beklemek yerine 0.2sn sonra tekrar dene!
                // Bu sayede aralarda oluşan devasa boşluklar (atlama sorunu) tamamen çözüldü.
                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    bool SpawnWaste()
    {
        // Null koruması
        if (wastePrefabs == null || wastePrefabs.Length == 0) return false;

        // 3. Özellik: Sabit Konum (Y ekseninde çakışma önleyici 0.15m yükseklik)
        Vector3 fixedOffset = new Vector3(0f, 0.15f, 0f);
        Vector3 finalSpawnPosition = Vector3.zero;

        // Yüzey (Surface) mimarisine göre doğma noktasını belirle
        if (_surfaceProvider != null && _surfaceProvider.TryGetRandomSurfacePoint(targetSurfaceType, out var surfaceData))
        {
            finalSpawnPosition = surfaceData.Position + fixedOffset;
        }
        else if (spawnPoint != null)
        {
            if (_surfaceProvider == null)
            {
                Debug.LogWarning("<color=red>[DEDEKTİF]</color> Sahnede ISurfaceProvider (Örn: SurfaceManager) YOK! Eski havadan doğma noktasına geçiliyor.");
            }
            else
            {
                Debug.LogWarning($"<color=red>[DEDEKTİF]</color> Sahnede '{targetSurfaceType}' türünde bir MockSurface YOK veya Yüzey Bulunamadı! Eski noktaya geçiliyor.");
            }
            finalSpawnPosition = spawnPoint.position + fixedOffset;
        }
        else
        {
            return false;
        }

        // 1.5 Özellik: Doğma noktasının henüz boşalıp boşalmadığını kontrol et (Üst üste doğup patlamayı %100 engeller)
        Collider[] existingColliders = Physics.OverlapSphere(finalSpawnPosition, 0.15f);
        foreach (var col in existingColliders)
        {
            if (spawnPoint != null && col.transform.root != spawnPoint.root && !col.isTrigger && col.attachedRigidbody != null)
            {
                // Sadece çöpler engellesin (Bandın kendisi engel sayılmasın!)
                if (HasWasteTag(col.gameObject))
                {
                    return false; // Dolu olduğu için üretemedik
                }
            }
        }

        float randomRoll = Random.Range(0f, 100f);
        
        // 5. Özel: Altın Çöp Şansı (Zar atma mantığı)
        GameObject selectedPrefab = null;
        if (goldenWastePrefab != null)
        {
            if (randomRoll <= goldenSpawnChance)
            {
                selectedPrefab = goldenWastePrefab;
                Debug.Log($"<color=yellow>[WasteSpawner]</color> BÜYÜK ŞANS! Altın Çöp üretiliyor!");
                OnGoldenWasteSpawned?.Invoke(); // Analiz sistemine haber ver
            }
        }

        // Eğer altın çöp çıkmadıysa normal rastgele çöplerden seç
        if (selectedPrefab == null)
        {
            selectedPrefab = GetRandomPrefab();
        }

        if (selectedPrefab == null) return false;

        Debug.Log($"<color=magenta>[WasteSpawner]</color> Çöp üretimi tetiklendi: {selectedPrefab.name}");

        // 4. Özellik: Dik Rotasyon
        Quaternion uprightRandomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Obje üretimi veya havuzdan çekim
        GameObject spawnedA = ObjectPoolManager.Instance.SpawnFromPool(selectedPrefab.tag, selectedPrefab, finalSpawnPosition, uprightRandomRotation);
        
        // Composite Waste (Yapışık Çöp) Mantığı
        float compositeChance = GetCompositeChance();
        bool spawnComposite = (randomRoll > goldenSpawnChance) && (Random.Range(0f, 100f) <= compositeChance);
        
        // Dirty Waste (Kirli Çöp) Mantığı - Composite ile çakışmaz!
        bool spawnDirty = false;
        if (!spawnComposite && randomRoll > goldenSpawnChance)
        {
            float dirtyChance = GetDirtyChance();
            spawnDirty = (Random.Range(0f, 100f) <= dirtyChance);
        }

        if (spawnComposite && _tapeMaterial != null)
        {
            GameObject selectedB = GetRandomPrefab(); // İkinci rastgele çöp
            if (selectedB != null)
            {
                Vector3 offsetPos = finalSpawnPosition + new Vector3(0.35f, 0, 0); // Objeler iç içe girip patlamasın diye aralığı açtık
                GameObject spawnedB = ObjectPoolManager.Instance.SpawnFromPool(selectedB.tag, selectedB, offsetPos, uprightRandomRotation);

                // Bileşenleri güvenli şekilde al veya ekle (Runtime Allocation'u önlemek için)
                var glue = spawnedA.GetComponent<RecycleRush.Interaction.WasteGlue>();
                if (glue == null) glue = spawnedA.AddComponent<RecycleRush.Interaction.WasteGlue>();

                var ctrl = spawnedA.GetComponent<RecycleRush.Interaction.CompositeWasteController>();
                if (ctrl == null) ctrl = spawnedA.AddComponent<RecycleRush.Interaction.CompositeWasteController>();

                // Bağla
                var interactableA = spawnedA.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                var interactableB = spawnedB.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                
                if (interactableA != null && interactableB != null)
                {
                    glue.Bind(interactableA, interactableB, _tapeMaterial);
                    Debug.Log($"<color=cyan>[Composite Waste]</color> İki çöp birbirine yapıştırıldı! ({selectedPrefab.name} + {selectedB.name})");
                }
            }
        }
        else if (spawnDirty)
        {
            var dirtyCtrl = spawnedA.GetComponent<RecycleRush.Interaction.DirtyWasteController>();
            if (dirtyCtrl == null) dirtyCtrl = spawnedA.AddComponent<RecycleRush.Interaction.DirtyWasteController>();

            // Kir görseli prefab'ı verilmişse ve objede henüz yoksa child olarak ekle
            if (dirtyCtrl.dirtVisual == null && _dirtVisualPrefab != null)
            {
                var visual = Instantiate(_dirtVisualPrefab, spawnedA.transform);
                visual.transform.localPosition = Vector3.zero;
                // Kutu gibi büyük, şişe gibi uzun objelere otomatik sarmalaması için boyutu çöpün boyutuna uydurabiliriz
                visual.transform.localScale = Vector3.one; 
                dirtyCtrl.dirtVisual = visual;
            }

            dirtyCtrl.InitializeDirtyState();
            Debug.Log($"<color=brown>[Dirty Waste]</color> Kirli çöp üretildi: {selectedPrefab.name}");
        }

        // Eğer portalımız varsa, çöp çıktığı anda şişme animasyonunu oynat!
        if (portalAnimator != null)
        {
            portalAnimator.PlaySpawnEffect();
        }
        
        return true; // Başarıyla üretildi
    }

    // Üst üste aynı objenin gelmesini engelleyen ve NULL elemanları süzcen fonksiyon
    GameObject GetRandomPrefab()
    {
        // 1) Önce listedeki sadece NULL OLMAYAN (geçerli) prefab'ları topla
        System.Collections.Generic.List<GameObject> validPrefabs = new System.Collections.Generic.List<GameObject>();
        foreach (var p in wastePrefabs)
        {
            if (p != null) validPrefabs.Add(p);
        }

        if (validPrefabs.Count == 0) return null;

        GameObject selected = null;
        int maxAttempts = 3;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            int randomIndex = Random.Range(0, validPrefabs.Count);
            selected = validPrefabs[randomIndex];
            
            if (selected != _lastSpawnedPrefab || validPrefabs.Count == 1)
            {
                break;
            }
        }

        _lastSpawnedPrefab = selected;
        return selected;
    }

    private bool HasWasteTag(GameObject obj)
    {
        // Untagged bile olsa üzerinde ARWastePhysicsTuner ve Altın Çöp tiki varsa engel say (Çakışma önleyici)
        var tuner = obj.GetComponentInChildren<RecycleRush.Interaction.ARWastePhysicsTuner>();
        if (tuner != null && tuner.isGoldenWaste)
        {
            return true;
        }

        return obj.CompareTag("Paper") || 
               obj.CompareTag("Glass") || 
               obj.CompareTag("Plastic") || 
               obj.CompareTag("Metal");
    }

    private float GetCompositeChance()
    {
        // Seviyeye göre dinamik artan Composite şansı
        if (DifficultyManager.Instance == null) return 5f;
        int lvl = DifficultyManager.Instance.CurrentLevel;
        
        if (lvl == 0) return 5f; // Level 1-3
        if (lvl == 1) return 8f; // Level 4-6
        if (lvl == 2) return 11f; // Level 7-9
        return 15f; // Level 10+ (Arcade/Max)
    }

    private float GetDirtyChance()
    {
        // Seviyeye göre artan Kirlilik Şansı (Oyuncunun belirttiği tablo)
        if (DifficultyManager.Instance == null) return 0f;
        int lvl = DifficultyManager.Instance.CurrentLevel;

        if (lvl == 0) return 0f;  // Level 1-3: %0
        if (lvl == 1) return 5f;  // Level 4-6: %5
        if (lvl == 2) return 8f;  // Level 7-9: %8
        if (lvl == 3) return 12f; // Level 10-12: %12
        return 15f;               // Level 13-15+: %15
    }
}