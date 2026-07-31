using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RecycleRush.Environment
{
    /// <summary>
    /// Yere düşen atıkları algılar. Atık 3 saniye içinde yerden alınmazsa silinir ve ceza puanı uygulatır.
    /// Oyuncuların çöpleri yere atarak zaman kazanmasını (Exploit/Hile) engeller.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FloorZone : MonoBehaviour
    {
        [Header("Ceza Ayarları")]
        [Tooltip("Yerde kalan atık için kesilecek ceza puanı")]
        [SerializeField] private int _penaltyScore = -5;

        [Tooltip("Atığın yerde kalmasına ne kadar süre tahammül edilecek? (Saniye)")]
        [SerializeField] private float _gracePeriod = 3f;

        // ScoreManager'ın dinleyeceği özel Event (DestroyZone ile benzer mantıkta çalışır)
        public static event Action<int> OnWasteMissedFloor;

        // Yere düşen objelerin düşme anını tuttuğumuz liste (Key: Collider, Value: Düşme Zamanı ve Root Obje)
        private class FloorItem
        {
            public float DropTime;
            public GameObject RootObject;
            public XRGrabInteractable Interactable; // Oyuncunun tutup tutmadığını anlamak için
        }

        private Dictionary<Collider, FloorItem> _itemsOnFloor = new Dictionary<Collider, FloorItem>();
        private Collider _zoneCollider;

        private void Awake()
        {
            _zoneCollider = GetComponent<Collider>();
            if (_zoneCollider != null)
            {
                _zoneCollider.isTrigger = true;
                
                // 1) TETİKLEYİCİ KALINLAŞTIRMA (Görünmez alan tespiti)
                if (_zoneCollider is BoxCollider box)
                {
                    box.size = new Vector3(box.size.x, 3f, box.size.z);
                    box.center = new Vector3(box.center.x, 0.5f, box.center.z); // Alt sınır -1.0, Üst sınır +2.0
                }
                else
                {
                    // Eğer zemin bir MeshCollider ise onu kalınlaştıramayız! (Bu yüzden 3 saniye sayacı iptal oluyordu)
                    // İncecik MeshCollider'ı kapatıp yerine bizim devasa BoxCollider sensörümüzü ekliyoruz.
                    _zoneCollider.enabled = false; 
                    BoxCollider newTrigger = gameObject.AddComponent<BoxCollider>();
                    newTrigger.isTrigger = true;
                    // Y ekseninde -1'den +2'ye uzanan devasa bir alan (Merkez 0.5, Boy 3)
                    newTrigger.size = new Vector3(100f, 3f, 100f); 
                    newTrigger.center = new Vector3(0, 0.5f, 0);
                }
            }

            // 2) FİZİKSEL KALINLAŞTIRMA (Tunneling engelleyici devasa güvenlik ağı):
            // İnce objelerin zemini mermi gibi delip -50'lere düşmesini engellemek için
            // tam zemin hizasına (Üst yüzeyi Y=0) 1 metre kalınlığında katı bir beton blok ekliyoruz.
            GameObject safetyFloor = new GameObject("SafetyFloor_PhysicsFix");
            safetyFloor.transform.position = new Vector3(0, -0.5f, 0); 
            BoxCollider sc = safetyFloor.AddComponent<BoxCollider>();
            sc.size = new Vector3(100f, 1f, 100f); 
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsWaste(other))
            {
                // Objenin en tepesindeki Root'u ve varsa tutma (Grab) bileşenini bul
                GameObject rootObj = other.transform.root.gameObject;
                XRGrabInteractable grabInteractable = rootObj.GetComponentInChildren<XRGrabInteractable>();

                _itemsOnFloor[other] = new FloorItem
                {
                    DropTime = Time.time,
                    RootObject = rootObj,
                    Interactable = grabInteractable
                };
            }
        }

        private void OnTriggerExit(Collider other)
        {
            // Atık zemin alanından çıkarsa (Örn: Oyuncu eline alıp kaldırdıysa) listeden çıkar
            if (_itemsOnFloor.ContainsKey(other))
            {
                _itemsOnFloor.Remove(other);
            }
        }

        private void Update()
        {
            // Listede eleman yoksa hiç işlem yapma (Optimizasyon)
            if (_itemsOnFloor.Count == 0) return;

            List<Collider> toDestroy = new List<Collider>();

            foreach (var kvp in _itemsOnFloor)
            {
                FloorItem item = kvp.Value;

                // Eğer obje sahneden başka bir yolla silindiyse listeyi temizle
                if (item.RootObject == null)
                {
                    toDestroy.Add(kvp.Key);
                    continue;
                }

                // 1. KORUMA: Eğer oyuncu objeyi şu an elinde tutuyorsa süreyi sıfırla (Ceza verme!)
                if (item.Interactable != null && item.Interactable.isSelected)
                {
                    item.DropTime = Time.time; // Süreyi sıfırlıyoruz ki elinde tuttuğu sürece patlamasın
                    continue;
                }

                // 2. CEZA KONTROLÜ: Obje yerdeyse ve belirlenen süre (Grace Period) geçtiyse
                if (Time.time - item.DropTime >= _gracePeriod)
                {
                    toDestroy.Add(kvp.Key);
                }
            }

            // Süresi dolan objeleri imha et ve ceza Event'ini ateşle
            foreach (var col in toDestroy)
            {
                if (_itemsOnFloor.TryGetValue(col, out FloorItem item))
                {
                    if (item.RootObject != null)
                    {
                        // ScoreManager'a -5 puan sinyali gönder
                        OnWasteMissedFloor?.Invoke(_penaltyScore);

                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlayFloorPenaltySound();
                        }

                        Debug.Log($"<color=red>[FloorZone]</color> {item.RootObject.name} çok uzun süre yerde kaldı! İmha ediliyor ve Ceza verildi.");

                        // Obje imha edilmek yerine havuza gönderilir (Object Pooling)
                        ObjectPoolManager.Instance.ReturnToPool(item.RootObject);
                    }
                    _itemsOnFloor.Remove(col);
                }
            }
        }

        /// <summary>
        /// Nesnenin bir atık olup olmadığını kontrol eder.
        /// </summary>
        private bool IsWaste(Collider col)
        {
            GameObject directObj = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
            if (HasWasteTag(directObj)) return true;

            GameObject rootObj = col.transform.root.gameObject;
            if (HasWasteTag(rootObj)) return true;

            return false;
        }

        private bool HasWasteTag(GameObject obj)
        {
            return obj.CompareTag("Paper") ||
                   obj.CompareTag("Glass") ||
                   obj.CompareTag("Plastic") ||
                   obj.CompareTag("Metal");
        }
    }
}
