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
        // Havuzda bu tag için bir kuyruk yoksa ilk defa oluştur
        if (!_poolDictionary.ContainsKey(tag))
        {
            _poolDictionary[tag] = new Queue<GameObject>();
            _prefabDictionary[tag] = prefab;
        }

        GameObject objToSpawn;

        // Havuzda kullanılmayan obje varsa onu al
        if (_poolDictionary[tag].Count > 0)
        {
            objToSpawn = _poolDictionary[tag].Dequeue();
        }
        else
        {
            // Havuz boşsa (hepsi sahnede aktifse), mecbur 1 tane yeni obje yarat ve kapasiteyi artır
            // NOT: SetParent KULLANMIYORUZ! Çünkü BinTrigger gibi sistemler transform.root ile objeyi arıyor.
            // Eğer bunu ObjectPoolManager'ın altına atarsak, root ObjectPoolManager olur ve sistem kendi kendini imha eder!
            objToSpawn = Instantiate(prefab);
        }

        // Objenin pozisyonunu ve açısını ayarla
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        objToSpawn.SetActive(true);

        // Önceki hareketinden kalan Fiziksel (Hız) etkilerini sıfırla ki havada uçmasın!
        Rigidbody rb = objToSpawn.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        return objToSpawn;
    }

    /// <summary>
    /// Sahnede işi biten objeyi silmek yerine kapatıp havuza geri koyar.
    /// </summary>
    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false); // Görünmez yap
        
        // Objenin tag'ine göre doğru havuza geri ekle
        if (_poolDictionary.ContainsKey(obj.tag))
        {
            _poolDictionary[obj.tag].Enqueue(obj);
            Debug.Log($"<color=green>[ObjectPoolManager]</color> {obj.name} objesi (Tag: {obj.tag}) havuza başarıyla eklendi. Kuyrukta {_poolDictionary[obj.tag].Count} obje var.");
        }
        else
        {
            // Eğer objenin tag'i havuzda yoksa (örneğin havuzdan çıkmamış harici bir objeyse) tamamen sil
            Debug.Log($"<color=red>[ObjectPoolManager]</color> {obj.name} objesi (Tag: {obj.tag}) havuzda bulunamadığı için SİLİNİYOR! Mevcut Havuz Tagleri: {string.Join(", ", _poolDictionary.Keys)}");
            Destroy(obj);
        }
    }
}
