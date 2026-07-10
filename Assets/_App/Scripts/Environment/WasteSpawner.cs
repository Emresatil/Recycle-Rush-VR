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
    public float spawnWidthOffset = 0.4f;

    // Tekrarı önlemek için son üretilen çöpü hafızada tutuyoruz
    private GameObject _lastSpawnedPrefab;

    void Start()
    {
        if (wastePrefabs.Length > 0 && spawnPoint != null)
        {
            StartCoroutine(SpawnRoutine());
        }
        else
        {
            Debug.LogWarning("WasteSpawner: Prefab listesi veya Spawn Point boş!");
        }
    }

    IEnumerator SpawnRoutine()
    {
        // Oyun başlarken oyuncuya 2 saniye hazırlanma payı ver
        yield return new WaitForSeconds(2f);

        while (true)
        {
            SpawnWaste();

            // Bir sonraki üretim için rastgele bir süre bekle (Daha organik hissettirir)
            float randomWait = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(randomWait);
        }
    }

    void SpawnWaste()
    {
        // 1. Özellik: Anti-Tekrar Sistemi ile üst üste aynı çöpün düşme ihtimalini azalt
        GameObject selectedPrefab = GetRandomPrefab();

        // 2. Özellik: Rastgele Konum (Offset). Çöpler ip gibi aynı hizada düşmesin, bant genişliğine yayılsın
        // Bandın Z ekseninde (ileri doğru) aktığını varsayarak sağa sola (X ekseninde) kaydırıyoruz.
        Vector3 randomOffset = new Vector3(Random.Range(-spawnWidthOffset, spawnWidthOffset), 0f, 0f);
        Vector3 finalSpawnPosition = spawnPoint.position + randomOffset;

        // 3. Özellik: Rastgele Rotasyon. Çöpler dümdüz değil, farklı açılarla düşsün ki fizik motoru yuvarlasın
        Quaternion randomRotation = Random.rotation;

        // Obje üretimi
        Instantiate(selectedPrefab, finalSpawnPosition, randomRotation);
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