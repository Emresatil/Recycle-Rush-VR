using UnityEngine;

public class WasteSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("List of waste prefabs to spawn.")]
    public GameObject[] wastePrefabs;
    
    [Tooltip("The position where waste will spawn.")]
    public Transform spawnPoint;
    
    [Tooltip("Time interval between spawns in seconds.")]
    public float spawnInterval = 3f;

    void Start()
    {
        // Eğer dizi boş değilse ve spawn noktası belirtilmişse üretimi başlat
        if (wastePrefabs.Length > 0 && spawnPoint != null)
        {
            // İlk üretimi 2 saniye sonra başlat, ardından 'spawnInterval' saniyede bir tekrarla
            InvokeRepeating(nameof(SpawnRandomWaste), 2f, spawnInterval);
        }
        else
        {
            Debug.LogWarning("WasteSpawner: Prefabs or Spawn Point not assigned!");
        }
    }

    void SpawnRandomWaste()
    {
        // Listeden rastgele bir sayı (index) seç
        int randomIndex = Random.Range(0, wastePrefabs.Length);
        GameObject selectedPrefab = wastePrefabs[randomIndex];

        // Seçilen prefab'ı sahnede üret
        Instantiate(selectedPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}