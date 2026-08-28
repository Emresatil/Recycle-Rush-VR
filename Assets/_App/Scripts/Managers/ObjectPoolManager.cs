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
    public int ActiveWasteCount => _activeObjects.Count;
    public IReadOnlyList<GameObject> ActiveObjects => _activeObjects;
    public static event System.Action<int> OnActiveWasteCountChanged;

    private Transform _poolContainer;
    public Transform PoolContainer => _poolContainer;

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
            
            // Havuz objelerini saklamak için Core_Managers'tan bağımsız güvenli bir depo oluştur
            _poolContainer = new GameObject("ObjectPool_Container").transform;
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

        // ÇOK KRİTİK: Objenin kök (Root) transformunu her ihtimale karşı ayarla
        objToSpawn.transform.SetParent(null); // Objeyi havuz deposundan çıkar ki kendi Root'u olsun
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        objToSpawn.transform.localScale = prefab.transform.localScale; // Ölçek bozulmasını (0 veya devasa) %100 engeller

        // Önceki hareketinden kalan Fiziksel etkileri ve kilitleri tam fabrika ayarlarına sıfırla
        Rigidbody[] rbs = objToSpawn.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in rbs)
        {
            // 1. Unity 6 uyarısını önlemek için Kinematic kapalıyken hızları sıfırla
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // 2. FİZİK MOTORU BUG ÇÖZÜMÜ (Phantom Forces):
            // Transform'u taşırken eski interpolasyondan doğan hayalet (phantom) fırlatma gücünü
            // engellemek için objeyi taşıma anında Kinematic (fiziksiz) duruma alıyoruz.
            rb.isKinematic = true; 
            
            // 3. Child Rigidbody'lerin (varsa) kendi lokal pozisyonlarını bozmamak için direkt taşıma (position/rotation ataması) YAPMIYORUZ.
            
            // 4. Taşıma bitti, yerçekimine geri bırak
            rb.isKinematic = false; // Bant olmadığı için doğrudan yerçekimiyle düşmeli
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.maxDepenetrationVelocity = 2.0f; 
        }

        // Hızlar ve konum sıfırlandıktan SONRA objeyi aktif et (Böylece Physics motoru fırlatmaz)
        objToSpawn.SetActive(true);

        // 2) Tutma ve fırlatma seslerinin çalışması için WasteAudioFeedback bileşenini dinamik ekle
        if (objToSpawn.GetComponent<RecycleRush.Interaction.WasteAudioFeedback>() == null &&
            objToSpawn.GetComponentInChildren<RecycleRush.Interaction.WasteAudioFeedback>() == null)
        {
            objToSpawn.AddComponent<RecycleRush.Interaction.WasteAudioFeedback>();
        }

        // 3) Mıknatıs mekaniği için MagnetResponder ekle
        if (objToSpawn.GetComponent<RecycleRush.Interaction.MagnetResponder>() == null &&
            objToSpawn.GetComponentInChildren<RecycleRush.Interaction.MagnetResponder>() == null)
        {
            objToSpawn.AddComponent<RecycleRush.Interaction.MagnetResponder>();
        }

        // 4) Kill-Z sistemi için aktif objeler listesine ekle
        if (!_activeObjects.Contains(objToSpawn))
        {
            _activeObjects.Add(objToSpawn);
            OnActiveWasteCountChanged?.Invoke(_activeObjects.Count);
        }

        return objToSpawn;
    }

    /// <summary>
    /// Sahnede işi biten objeyi silmek yerine kapatıp havuza geri koyar.
    /// </summary>
    public void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        // Failsafe: Havuz yöneticisini veya depo objesini havuza atmaya çalışırsak işlemi reddet!
        if (obj == gameObject || (_poolContainer != null && obj == _poolContainer.gameObject))
        {
            Debug.LogError($"<color=red>[CRITICAL BUG PREVENTED]</color> Sistemi havuza atmaya çalıştınız! {obj.name} reddedildi.");
            return;
        }

        // Zaten kapalıysa işlem yapma (Aynı objenin iki kez havuza dönmesini engeller)
        if (!obj.activeInHierarchy) return;

        // Kill-Z takibinden çıkar
        if (_activeObjects.Contains(obj))
        {
            _activeObjects.Remove(obj);
            OnActiveWasteCountChanged?.Invoke(_activeObjects.Count);
        }

        // 1) EĞER OYUNCU OBJEYİ ELİNDE TUTARKEN HAVUZA GİDERSE (Süre bitimi, yere düşme vs.)
        // XR sistemi bug'a girer ve obje tekrar doğduğunda oyuncunun eline veya uzaya mermi gibi fırlar!
        // Bunu önlemek için grab bağlantısını XR Interaction Manager üzerinden iptal ediyoruz.
        var grab = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null) 
            grab = obj.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            
        if (grab != null && grab.isSelected)
        {
            if (grab.interactionManager != null)
            {
                grab.interactionManager.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
            }
            grab.enabled = false;
            grab.enabled = true; // Kapat-aç yapmak grab bağlantısını kesin olarak koparır.
        }

        // Havuza dönerken tüm fiziksel kuvvetleri temizle ve kinematic yap (Unity 6 PhysX uyumlu sıralama)
        Rigidbody[] returnRbs = obj.GetComponentsInChildren<Rigidbody>(true);
        foreach (var r in returnRbs)
        {
            if (r != null)
            {
                if (!r.isKinematic)
                {
                    r.linearVelocity = Vector3.zero;
                    r.angularVelocity = Vector3.zero;
                }
                r.isKinematic = true;
            }
        }

        // 2) EBEVEYN (PARENT) RESETLEME
        // Objeleri ObjectPool_Container içine saklıyoruz ki root kirlenmesin.
        if (_poolContainer != null)
        {
            obj.transform.SetParent(_poolContainer);
        }
        else
        {
            obj.transform.SetParent(null);
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

    /// <summary>
    /// Sahnede aktif olan tüm çöpleri (havuz objelerini) temizler ve havuza geri gönderir.
    /// Genelde oyun yeniden başlatıldığında (Restart) kullanılır.
    /// </summary>
    public void ReturnAllToPool()
    {
        for (int i = _activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = _activeObjects[i];
            if (obj != null && obj.activeInHierarchy)
            {
                ReturnToPool(obj);
            }
        }
        _activeObjects.Clear();
        OnActiveWasteCountChanged?.Invoke(0);
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
