using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DestroyZone : MonoBehaviour
{
    [Header("Ceza Ayarları")]
    [Tooltip("Bu alana (bandın sonuna) düşen kaçırılmış atıklar için oyuncudan düşülecek ceza puanı")]
    [SerializeField] private int _penaltyScore = -5;

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

            // 3. Çöpü tamamen sahnede yok et (Alt objesi çarpmış olsa bile Root'unu bularak sil)
            // Bu kısım daha önce konuştuğumuz sonsuza kadar asılı kalan görünmez obje (Memory Leak) hatasını önler.
            Destroy(other.transform.root.gameObject);
        }
    }

    /// <summary>
    /// Çarpan nesnenin geri dönüşüm atığı olup olmadığını hızlıca kontrol eder (GC Dostu).
    /// </summary>
    private bool IsWaste(Collider col)
    {
        // 1. Önce doğrudan çarpan parçaya veya onun Rigidbody'sine bakalım
        GameObject directObj = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
        if (HasWasteTag(directObj)) return true;

        // 2. Eğer bulamadıysa, tag en dıştaki (Root) objede olabilir. Oraya bakalım:
        GameObject rootObj = col.transform.root.gameObject;
        if (HasWasteTag(rootObj)) return true;

        return false; // Hiçbiri değilse bu bir atık değildir
    }

    private bool HasWasteTag(GameObject obj)
    {
        // Unity'nin CompareTag fonksiyonu string karşılaştırmasından (obj.tag == "Paper") 
        // çok daha performanslıdır ve arkada çöp bellek (Garbage) üretmez.
        return obj.CompareTag("Paper") || 
               obj.CompareTag("Glass") || 
               obj.CompareTag("Plastic") || 
               obj.CompareTag("Metal");
    }
}
