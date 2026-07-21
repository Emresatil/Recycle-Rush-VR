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
                _zoneCollider.isTrigger = true; // Zemin bir tetikleyici olmalı
            }
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
