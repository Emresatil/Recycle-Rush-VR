using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DestroyZone : MonoBehaviour
{
    [Header("Ceza Ayarları")]
    [Tooltip("Bu alana (bandın sonuna) düşen kaçırılmış atıklar için oyuncudan düşülecek ceza puanı")]
    [SerializeField] private int _penaltyScore = -5;

    [Header("Öğütücü (Crusher) Görsel Efekti")]
    [Tooltip("Çöp aşağı düştüğünde patlayacak olan ateş/elektrik/parçalanma efekti (Prefab)")]
    [SerializeField] private GameObject _crusherVFXPrefab;

    // ScoreManager'ın dinleyebilmesi için statik Action Event (Gevşek Bağlılık / Loose Coupling)
    // Bu sayede DestroyZone, ScoreManager'ın varlığından haberdar olmadan işini yapar.
    public static event Action<int> OnWasteMissed;

    private Collider _zoneCollider;

    private void Awake()
    {
        // Alanın bir Trigger olduğundan kodsal olarak emin oluyoruz
        _zoneCollider = GetComponent<Collider>();
        if (_zoneCollider != null)
        {
            _zoneCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Önce nesnenin gerçekten bir Atık (Waste) olup olmadığını Tag ile anlamalıyız.
        // Eğer oyuncunun eli, ortam objesi veya başka bir şeyse silinmemeli.
        if (IsWaste(other))
        {
            // 2. ScoreManager'a puan düşmesi için event fırlat (-5 puan gönder)
            OnWasteMissed?.Invoke(_penaltyScore);

            // 3. Görsel Efekti (Crusher VFX) Çöplerin düştüğü noktada patlat
            if (_crusherVFXPrefab != null)
            {
                // Efekti bandın hizasında, çöpün hemen üstünde oluştur (Yükseklik normale döndürüldü)
                Vector3 spawnPosition = transform.position + new Vector3(0, 0.5f, 0);
                GameObject crusherVfx = Instantiate(_crusherVFXPrefab, spawnPosition, Quaternion.identity);
                
                // Efektin boyutunu zorla orijinal haline getir
                crusherVfx.transform.localScale = Vector3.one;
                
                // Sahneyi kirletmemesi için efekt bittikten 3 saniye sonra otomatik sil
                Destroy(crusherVfx, 3f);
                
                Debug.Log($"<color=orange>[Crusher]</color> Atık parçalandı! Patlayan efekt: {crusherVfx.name} | Konum: {spawnPosition}");
            }

            // 4. Çöpü tamamen silme, havuza geri gönder (Object Pooling)
            ObjectPoolManager.Instance.ReturnToPool(other.transform.root.gameObject);
        }
    }

    /// <summary>
    /// Çarpan nesnenin geri dönüşüm atığı olup olmadığını tüm alt objelerini (çocuklarını) tarayarak kontrol eder.
    /// (BinTrigger'daki Foolproof mantığıyla aynı)
    /// </summary>
    private bool IsWaste(Collider col)
    {
        // En dış (Root) objeyi bul (Bu sayede prefab'ın en tepesine ulaşırız)
        Transform rootTransform = col.transform.root;

        // Root objenin kendisine ve BÜTÜN alt objelerine (çocuklarına) sırayla bak
        foreach (Transform child in rootTransform.GetComponentsInChildren<Transform>(true))
        {
            if (HasWasteTag(child.gameObject)) 
            {
                return true;
            }
        }

        return false; // Hiçbiri değilse bu bir atık değildir
    }

    private bool HasWasteTag(GameObject obj)
    {
        return obj.CompareTag("Paper") || 
               obj.CompareTag("Glass") || 
               obj.CompareTag("Plastic") || 
               obj.CompareTag("Metal");
    }
}
