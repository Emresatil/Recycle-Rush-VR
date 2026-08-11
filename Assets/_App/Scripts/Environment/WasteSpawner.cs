using System.Collections;
using UnityEngine;

public class WasteSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Üretilecek atık prefab'larının listesi")]
    public GameObject[] wastePrefabs;
    
    [Tooltip("Atıkların düşeceği başlangıç noktası")]
    public Transform spawnPoint;

    [Header("Visual Effects")]
    [Tooltip("Çöplerin çıktığı portalın görsel animatörü (İsteğe bağlı)")]
    public RecycleRush.Environment.PortalAnimator portalAnimator;
    
    [Header("Organik Zamanlama (Zorluk)")]
    [Tooltip("En az kaç saniyede bir atık düşsün?")]
    public float minSpawnInterval = 0.8f;
    [Tooltip("En fazla kaç saniyede bir atık düşsün?")]
    public float maxSpawnInterval = 1.5f;



    // Tekrarı önlemek için son üretilen çöpü hafızada tutuyoruz
    private GameObject _lastSpawnedPrefab;

    private Coroutine _spawnCoroutine;

    private float _baseMinSpawnInterval;
    private float _baseMaxSpawnInterval;

    private void Awake()
    {
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
        if (spawnPoint == null || wastePrefabs == null || wastePrefabs.Length == 0) return false;

        // 3. Özellik: Sabit Konum (Y ekseninde çakışma önleyici 0.15m yükseklik)
        Vector3 fixedOffset = new Vector3(0f, 0.15f, 0f);
        Vector3 finalSpawnPosition = spawnPoint.position + fixedOffset;

        // 1.5 Özellik: Doğma noktasının henüz boşalıp boşalmadığını kontrol et (Üst üste doğup patlamayı %100 engeller)
        Collider[] existingColliders = Physics.OverlapSphere(finalSpawnPosition, 0.15f);
        foreach (var col in existingColliders)
        {
            if (col.transform.root != spawnPoint.root && !col.isTrigger && col.attachedRigidbody != null)
            {
                // Sadece çöpler engellesin (Bandın kendisi engel sayılmasın!)
                if (HasWasteTag(col.gameObject))
                {
                    return false; // Dolu olduğu için üretemedik
                }
            }
        }

        GameObject selectedPrefab = GetRandomPrefab();
        if (selectedPrefab == null) return false;

        Debug.Log($"<color=magenta>[WasteSpawner]</color> Çöp üretimi tetiklendi: {selectedPrefab.name}");

        // 4. Özellik: Dik Rotasyon
        Quaternion uprightRandomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Obje üretimi veya havuzdan çekim
        ObjectPoolManager.Instance.SpawnFromPool(selectedPrefab.tag, selectedPrefab, finalSpawnPosition, uprightRandomRotation);
        
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
        return obj.CompareTag("Paper") || 
               obj.CompareTag("Glass") || 
               obj.CompareTag("Plastic") || 
               obj.CompareTag("Metal");
    }
}