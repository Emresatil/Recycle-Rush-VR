using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WasteSpawner : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField, Tooltip("Üretilecek atık prefab'larının listesi (Object Pool için).")]
    private GameObject[] _wastePrefabs;

    [SerializeField, Tooltip("Her bir prefab türünden kaç adet önbelleğe alınacak (Cache).")]
    private int _poolSizePerPrefab = 5;

    [Header("Spawn Settings")]
    [SerializeField, Tooltip("Atıkların havada süzülmeye başlayacağı üretim noktası.")]
    private Transform _spawnPoint;

    [SerializeField, Tooltip("İki üretim arasındaki saniye cinsinden süre.")]
    private float _spawnInterval = 3f;

    [SerializeField, Tooltip("Üretim süresinin düşebileceği minimum sınır (Maksimum Zorluk).")]
    private float _minSpawnInterval = 0.5f;

    // Object Pool Listesi
    private List<GameObject> _objectPool = new List<GameObject>();
    
    // Coroutine Referansı (İleride durdurmak gerekirse diye Cache'lendi)
    private Coroutine _spawnCoroutine;

    private void Awake()
    {
        // Update veya oyun akışı içinde Instantiate çağırmamak için 
        // tüm objeleri Awake içinde üretip (Allocate) havuza atıyoruz.
        InitializePool();
    }

    private void Start()
    {
        if (_wastePrefabs != null && _wastePrefabs.Length > 0 && _spawnPoint != null)
        {
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
        else
        {
            Debug.LogWarning("WasteSpawner: Prefab listesi veya Spawn Point atanmamış!");
        }
    }

    /// <summary>
    /// Tüm prefab'lardan belirtilen sayı kadar üretip inaktif şekilde havuza ekler.
    /// </summary>
    private void InitializePool()
    {
        foreach (var prefab in _wastePrefabs)
        {
            for (int i = 0; i < _poolSizePerPrefab; i++)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.SetActive(false); // Ekranda görünmemesi için kapat
                _objectPool.Add(obj);
            }
        }
    }

    /// <summary>
    /// Havuzdan inaktif (kullanılmayan) rastgele bir obje çeker.
    /// </summary>
    private GameObject GetPooledObject()
    {
        if (_objectPool.Count == 0) return null;

        // Rastgelelik hissini artırmak için havuzda rastgele bir noktadan aramaya başla
        int startIndex = Random.Range(0, _objectPool.Count);
        
        for (int i = 0; i < _objectPool.Count; i++)
        {
            int index = (startIndex + i) % _objectPool.Count;
            if (!_objectPool[index].activeInHierarchy)
            {
                return _objectPool[index]; // Boşta olan ilk objeyi döndür
            }
        }

        Debug.LogWarning("WasteSpawner: Havuzda boş obje kalmadı! _poolSizePerPrefab değerini artırın.");
        return null;
    }

    private IEnumerator SpawnRoutine()
    {
        // Oyun başladığında oyuncunun hazırlanması için 2 saniye tolerans
        yield return new WaitForSeconds(2f);

        while (true)
        {
            SpawnWaste();
            yield return new WaitForSeconds(_spawnInterval);
        }
    }

    private void SpawnWaste()
    {
        GameObject wasteItem = GetPooledObject();
        if (wasteItem != null)
        {
            // Objeyi spawn noktasına taşı ve Antigravity için rastgele bir dönüşle başlat
            wasteItem.transform.position = _spawnPoint.position;
            wasteItem.transform.rotation = Random.rotation; 
            wasteItem.SetActive(true); // Objeyi sahneye sür
        }
    }

    /// <summary>
    /// ScoreManager tarafından puan barajları (50, 100, 200) aşıldığında tetiklenecek Event Listener.
    /// Dinamik zorluk (Dynamic Difficulty) artışını sağlar.
    /// </summary>
    public void OnScoreThresholdReached(int currentScore)
    {
        if (currentScore >= 200)
        {
            SetSpawnInterval(1.0f); // Çok hızlı (Arcade mode)
        }
        else if (currentScore >= 100)
        {
            SetSpawnInterval(1.5f); // Hızlı
        }
        else if (currentScore >= 50)
        {
            SetSpawnInterval(2.0f); // Orta
        }
    }

    private void SetSpawnInterval(float newInterval)
    {
        // Yeni sürenin, belirlenen minimum sürenin altına inmesini (oyunun çökmesini) engeller
        _spawnInterval = Mathf.Max(newInterval, _minSpawnInterval);
        Debug.Log($"[WasteSpawner] Zorluk Arttı! Yeni Üretim Aralığı: {_spawnInterval} saniye.");
    }
}