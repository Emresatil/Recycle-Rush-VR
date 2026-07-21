using System.Collections;
using UnityEngine;

public class WasteSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Üretilecek atık prefab'larının listesi")]
    public GameObject[] wastePrefabs;
    
    [Tooltip("Atıkların düşeceği başlangıç noktası")]
    public Transform spawnPoint;
    
    [Header("Organik Zamanlama (Zorluk)")]
    [Tooltip("En az kaç saniyede bir atık düşsün?")]
    public float minSpawnInterval = 1.5f;
    [Tooltip("En fazla kaç saniyede bir atık düşsün?")]
    public float maxSpawnInterval = 3.5f;

    [Header("Organik Konum (Dağılım)")]
    [Tooltip("Çöpler bandın sağına/soluna ne kadar kayarak düşebilir?")]
    public float spawnWidthOffset = 0.1f; // 0.4 çok fazlaydı, bandı aşıyordu. 0.1'e düşürdük.

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
        // Zorluk çarpanı arttıkça (örn: 1.5x) bekleme süresi kısalmalıdır. Bu yüzden böleriz.
        minSpawnInterval = _baseMinSpawnInterval / multiplier;
        maxSpawnInterval = _baseMaxSpawnInterval / multiplier;
        
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
        if (newState == GameState.Playing)
        {
            // Sadece oyun aktifken spawn işlemini başlat
            if (_spawnCoroutine == null)
            {
                _spawnCoroutine = StartCoroutine(SpawnRoutine());
            }
        }
        else
        {
            // Pause veya GameOver durumunda üretimi durdur
            if (_spawnCoroutine != null)
            {
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
            SpawnWaste();

            // 1. Özellik: Bir sonraki üretim için rastgele bir süre bekle
            float randomWait = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(randomWait);
        }
    }

    void SpawnWaste()
    {
        // 2. Özellik: Anti-Tekrar Sistemi
        GameObject selectedPrefab = GetRandomPrefab();

        // 3. Özellik: Rastgele Konum (Offset). Genişlik 0.1'e düşürüldü ki bandı aşmasınlar
        Vector3 randomOffset = new Vector3(Random.Range(-spawnWidthOffset, spawnWidthOffset), 0f, 0f);
        Vector3 finalSpawnPosition = spawnPoint.position + randomOffset;

        // 4. Özellik: DÜZELTME - Çöpler yatay düşmesin diye sadece Y ekseninde (kendi etrafında) döndürüyoruz!
        // Eskiden Random.rotation idi, bu yüzden şişeler yan veya ters doğuyordu.
        Quaternion uprightRandomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Obje üretimi yerine havuzdan çek (Daha performanslı)
        ObjectPoolManager.Instance.SpawnFromPool(selectedPrefab.tag, selectedPrefab, finalSpawnPosition, uprightRandomRotation);
    }

    // Üst üste aynı objenin gelmesini engelleyen özel fonksiyon
    GameObject GetRandomPrefab()
    {
        GameObject selected = null;
        int maxAttempts = 3; // Sonsuz döngüyü önlemek için 3 deneme hakkı
        
        for (int i = 0; i < maxAttempts; i++)
        {
            int randomIndex = Random.Range(0, wastePrefabs.Length);
            selected = wastePrefabs[randomIndex];
            
            // Eğer seçilen obje bir öncekiyle aynı DEĞİLSE döngüden çık ve onu kullan
            if (selected != _lastSpawnedPrefab)
            {
                break;
            }
        }

        _lastSpawnedPrefab = selected; // Hafızayı güncelle
        return selected;
    }
}