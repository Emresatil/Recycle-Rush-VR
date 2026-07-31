using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    // Her prefab (örneğin plastik, kağıt) için ayrı bir kuyruk (Queue) tutan sözlük.
    private Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();
    
    // Hangi tag'e sahip objenin hangi prefab'dan üretileceğini hatırlamak için sözlük.
    private Dictionary<string, GameObject> _prefabDictionary = new Dictionary<string, GameObject>();

    [Header("Güvenlik Sınırı (Kill-Z / Out of Bounds)")]
    [Tooltip("Objeler bu Y koordinatının altına düşerse otomatik havuza döner (Örn: -2.0)")]
    public float killZLevel = -2.0f;
    [Tooltip("Objeler sahnede başlangıç noktasından ne kadar uzağa fırlarsa havuza döner (Örn: 12.0)")]
    public float maxDistance = 12.0f;

    // Sahnede aktif olan havuz objelerini takip etmek için liste
    private List<GameObject> _activeObjects = new List<GameObject>();

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

        // ÇOK KRİTİK: Objeyi aktifleştirmeden ÖNCE pozisyonunu ayarla.
        // Çünkü SetActive(true) çağrıldığında BeltItem.OnEnable() çalışır ve AttachToBelt() metodu çalışır.
        // Eğer pozisyonu sonradan ayarlarsak, AttachToBelt'in yaptığı hizalama ezilir!
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        
        objToSpawn.SetActive(true);

        // Önceki hareketinden kalan Fiziksel etkileri ve kilitleri tam fabrika ayarlarına sıfırla
        Rigidbody[] rbs = objToSpawn.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in rbs)
        {
            // Unity 6 Uyarısını Çözümleme: Kinematic bir objenin hızını sıfırlamaya çalışmak uyarı verir.
            // Bu yüzden önce geçici olarak kinematic'i kapatıp hızları sıfırlıyoruz, sonra tekrar açıyoruz.
            bool wasKinematic = rb.isKinematic;
            if (wasKinematic) rb.isKinematic = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            rb.isKinematic = true; // Banta sorunsuz oturması için başta Kinematic olmalı
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.maxDepenetrationVelocity = 0.8f; 
        }

        // 1) BeltItem bileşeni yoksa otomatik ekle (Bu bileşen OnEnable'da banta kaydeder)
        if (objToSpawn.GetComponent<BeltItem>() == null)
        {
            objToSpawn.AddComponent<BeltItem>();
        }

        // 2) Tutma ve fırlatma seslerinin çalışması için WasteAudioFeedback bileşenini dinamik ekle
        if (objToSpawn.GetComponent<RecycleRush.Interaction.WasteAudioFeedback>() == null &&
            objToSpawn.GetComponentInChildren<RecycleRush.Interaction.WasteAudioFeedback>() == null)
        {
            objToSpawn.AddComponent<RecycleRush.Interaction.WasteAudioFeedback>();
        }

        // Aktif objeler listesine ekle (Kill-Z takibi için)
        if (!_activeObjects.Contains(objToSpawn))
        {
            _activeObjects.Add(objToSpawn);
        }

        return objToSpawn;
    }

    /// <summary>
    /// Sahnede işi biten objeyi silmek yerine kapatıp havuza geri koyar.
    /// </summary>
    public void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        // Kill-Z takibinden çıkar
        if (_activeObjects.Contains(obj))
        {
            _activeObjects.Remove(obj);
        }

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

    private void Update()
    {
        // Geriye doğru döngü kullanıyoruz çünkü döngü içinde ReturnToPool çağırırsak listeden eleman silinecek.
        for (int i = _activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = _activeObjects[i];
            
            // Eğer obje bir şekilde sahnede yok edildiyse veya kapatıldıysa listeden çıkar
            if (obj == null || !obj.activeInHierarchy)
            {
                _activeObjects.RemoveAt(i);
                continue;
            }

            Vector3 pos = obj.transform.position;
            
            // Sadece yükseklik sınırı kontrolü (Kill-Z). Uzaklık sınırını (maxDistance) kaldırdık çünkü spawn noktası merkeze çok uzak!
            if (pos.y < killZLevel)
            {
                Debug.Log($"<color=orange>[ObjectPoolManager - Kill Z]</color> {obj.name} yere düştü! (Konum: {pos}). Otomatik havuza çekiliyor.");
                ReturnToPool(obj); 
                // ReturnToPool metodu zaten '_activeObjects.Remove(obj)' yapacağı için listemiz temiz kalır.
            }
        }
    }
}
