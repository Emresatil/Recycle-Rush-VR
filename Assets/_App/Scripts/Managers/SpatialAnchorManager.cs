using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace RecycleRush.Managers
{
    /// <summary>
    /// Recycle Rush MR projesinde nesneleri (Bant, çöp kutuları) 
    /// gerçek dünyaya sabitlemekten sorumlu yönetici sınıf.
    /// </summary>
    public class SpatialAnchorManager : MonoBehaviour
    {
        [Header("AR Foundation")]
        public ARAnchorManager anchorManager;

        // Hafızada tutulan çapalarımız
        private List<ARAnchor> activeAnchors = new List<ARAnchor>();

        private void Awake()
        {
            if (anchorManager == null)
            {
                anchorManager = FindFirstObjectByType<ARAnchorManager>();
                
                if (anchorManager == null)
                {
                    Debug.LogWarning("[SpatialAnchorManager] Sahnede ARAnchorManager bulunamadı! Lütfen XR Origin objesine eklediğinizden emin olun.");
                }
            }
        }

        /// <summary>
        /// Mevcut bir objeyi (örneğin taşıma bandı veya çöp kutusu) bulunduğu 
        /// koordinatta kalıcı olarak dünyaya çapalar (Spatial Anchor).
        /// </summary>
        public void AnchorObject(GameObject targetObject)
        {
            if (targetObject == null) return;

            // AR Foundation 5.0+ standardına göre bir objeye doğrudan ARAnchor bileşeni eklenebilir.
            ARAnchor anchor = targetObject.GetComponent<ARAnchor>();
            
            if (anchor == null)
            {
                anchor = targetObject.AddComponent<ARAnchor>();
            }

            if (!activeAnchors.Contains(anchor))
            {
                activeAnchors.Add(anchor);
            }

            Debug.Log($"[SpatialAnchorManager] {targetObject.name} gerçek dünyaya sabitlendi! (ID: {anchor.trackableId})");
        }

        /// <summary>
        /// Sahnedeki tüm çapaları (Anchors) temizler. 
        /// Oyunu sıfırlamak veya objelerin yerini değiştirmek için kullanılır.
        /// </summary>
        public void ClearAllAnchors()
        {
            foreach (var anchor in activeAnchors)
            {
                if (anchor != null)
                {
                    // ARAnchor bileşenini silmek, cihazın o çapayı takibi bırakmasını sağlar
                    Destroy(anchor); 
                }
            }
            activeAnchors.Clear();
            Debug.Log("[SpatialAnchorManager] Tüm uzamsal çapalar temizlendi.");
        }
    }
}
