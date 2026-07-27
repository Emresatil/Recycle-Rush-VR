using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    // Her prefab (örneğin plastik, kağıt) için ayrı bir kuyruk (Queue) tutan sözlük.
    private Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();
    
    // Hangi tag'e sahip objenin hangi prefab'dan üretileceğini hatırlamak için sözlük.
    private Dictionary<string, GameObject> _prefabDictionary = new Dictionary<string, GameObject>();

    private void Awake()
    {
        // Singleton Deseni
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Objeyi havuzdan çeker. Havuz boşsa yeni bir tane Instantiate eder.
    /// </summary>
    public GameObject SpawnFromPool(string tag, GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        // Tag yerine Prefab ismini key olarak kullanıyoruz (Farklı objeler aynı tag'de karışmasın!)
        string poolKey = prefab.name.Replace("(Clone)", "").Trim();

        if (!_poolDictionary.ContainsKey(poolKey))
        {
            _poolDictionary[poolKey] = new Queue<GameObject>();
            _prefabDictionary[poolKey] = prefab;
        }

        GameObject objToSpawn = null;

        // Havuzdaki ölü/silinmiş referansları temizleyerek ilk geçerli canlı objeyi bul
        while (_poolDictionary[poolKey].Count > 0)
        {
            objToSpawn = _poolDictionary[poolKey].Dequeue();
            if (objToSpawn != null) break;
        }

        // Eğer havuz boşsa veya içindekilerin hepsi ölmüşse, yeni taze obje yarat
        if (objToSpawn == null)
        {
            objToSpawn = Instantiate(prefab);
            objToSpawn.name = poolKey; // (Clone) takısını temiz tut
        }

        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        objToSpawn.SetActive(true);

        // Önceki hareketinden kalan Fiziksel etkileri ve kilitleri tam fabrika ayarlarına sıfırla
        Rigidbody rb = objToSpawn.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.maxDepenetrationVelocity = 2.0f; // Çakışmalarda nesnelerin uzaya fırlatılmasını (Physics Explosion) engeller
        }

        // Tutma ve fırlatma seslerinin çalışması için WasteAudioFeedback bileşenini dinamik ekle
        if (objToSpawn.GetComponent<RecycleRush.Interaction.WasteAudioFeedback>() == null &&
            objToSpawn.GetComponentInChildren<RecycleRush.Interaction.WasteAudioFeedback>() == null)
        {
            objToSpawn.AddComponent<RecycleRush.Interaction.WasteAudioFeedback>();
        }

        return objToSpawn;
    }

    /// <summary>
    /// Sahnede işi biten objeyi silmek yerine kapatıp havuza geri koyar.
    /// </summary>
    public void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false); // Görünmez yap
        
        string poolKey = obj.name.Replace("(Clone)", "").Trim();
        poolKey = System.Uri.UnescapeDataString(poolKey);

        if (_poolDictionary.ContainsKey(poolKey))
        {
            _poolDictionary[poolKey].Enqueue(obj);
            Debug.Log($"<color=green>[ObjectPoolManager]</color> {obj.name} havuza eklendi. ({poolKey} havuzunda {_poolDictionary[poolKey].Count} obje var)");
        }
        else
        {
            // Eğer tam eşleşme bulunamadıysa (Model ismi / özel karakter farkları için) esnek eşleşme ara
            string matchedKey = null;
            foreach (var key in _poolDictionary.Keys)
            {
                if (poolKey.Contains(key) || key.Contains(poolKey))
                {
                    matchedKey = key;
                    break;
                }
            }

            if (matchedKey != null)
            {
                _poolDictionary[matchedKey].Enqueue(obj);
                Debug.Log($"<color=green>[ObjectPoolManager]</color> {obj.name} (Esnek Eşleşen: {matchedKey}) havuza eklendi. ({matchedKey} havuzunda {_poolDictionary[matchedKey].Count} obje var)");
            }
            else
            {
                Debug.Log($"<color=orange>[ObjectPoolManager]</color> {obj.name} için havuz bulunamadı, yok ediliyor.");
                Destroy(obj);
            }
        }
    }
}
