using System;
using UnityEngine;

// Atık türleri için Enum yapısı (Inspector'dan kolayca seçilebilmesi için)
public enum WasteType
{
    Paper,
    Glass,
    Plastic,
    Metal,
    Untagged // Atık olmayan objeler (oyuncunun eli vb.) için
}

// Event üzerinden diğer sistemlere (Manager'lara) aktarılacak paket veri yapısı
public struct SortResultData
{
    public bool IsCorrect;
    public int ScoreChange;
    public float HapticDuration;
    public float HapticAmplitude;
    public Vector3 ActionPosition; // Ses ve Partikül efektlerinin nerede çıkacağı
}

[RequireComponent(typeof(Collider))]
public class BinTrigger : MonoBehaviour
{
    [Header("Kutu Ayarları")]
    [Tooltip("Bu kutunun kabul ettiği doğru atık türü")]
    [SerializeField] private WasteType _acceptedWasteType;

    [Header("Doğru Eşleşme (Correct) Parametreleri")]
    [SerializeField] private int _correctScore = 10;
    [SerializeField] private float _correctHapticDuration = 0.2f;
    [SerializeField] private float _correctHapticAmplitude = 0.5f;

    [Header("Yanlış Eşleşme (Incorrect) Parametreleri")]
    [SerializeField] private int _incorrectScore = -5;
    [SerializeField] private float _incorrectHapticDuration = 0.4f;
    [SerializeField] private float _incorrectHapticAmplitude = 0.8f;

    // Sistemler arası spagetti bağlantıları engelleyen (Loose Coupling) statik Action Event'imiz.
    // ScoreManager, AudioManager ve HapticManager sadece bu event'e Abone (Subscribe) olacaktır.
    public static event Action<SortResultData> OnWasteProcessed;

    private Collider _binCollider;

    private void Awake()
    {
        // GC Optimizasyonu: GetComponent çağrısını Awake içinde Cache'liyoruz.
        _binCollider = GetComponent<Collider>();
        
        // Kutunun çarpışma sınırının mutlaka Trigger modunda olduğundan emin oluyoruz.
        if (_binCollider != null)
        {
            _binCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"<color=orange>[BinTrigger]</color> Kutunun içine bir şey girdi! Giren şeyin adı: {other.name}");

        // Giren objenin atık türünü alıyoruz.
        WasteType incomingType = GetWasteTypeFromCollider(other);
        
        Debug.Log($"<color=yellow>[BinTrigger]</color> {other.name} objesinin Tag kontrolü yapıldı. Bulunan Atık Türü: {incomingType}");

        // Eğer giren obje bir atık değilse işlemi iptal et.
        if (incomingType == WasteType.Untagged) 
        {
            Debug.Log("<color=red>[BinTrigger]</color> Bu obje Untagged (Etiketsiz) olduğu için puanlama yapılmadı ve silinmedi!");
            return;
        }

        // Doğruluk mantığı: Giren atığın türü, kutunun kabul ettiği türe eşit mi?
        bool isCorrect = (incomingType == _acceptedWasteType);
        
        Debug.Log($"<color=cyan>[BinTrigger]</color> Kutu Türü: {_acceptedWasteType} | Gelen Çöp Türü: {incomingType} | Eşleşme: {isCorrect}");

        // Diğer Manager sınıflarına yayınlanacak veri paketi
        SortResultData resultData = new SortResultData
        {
            IsCorrect = isCorrect,
            ActionPosition = transform.position,
            ScoreChange = isCorrect ? _correctScore : _incorrectScore,
            HapticDuration = isCorrect ? _correctHapticDuration : _incorrectHapticDuration,
            HapticAmplitude = isCorrect ? _correctHapticAmplitude : _incorrectHapticAmplitude
        };

        Debug.Log($"<color=magenta>[BinTrigger]</color> OnWasteProcessed sinyali fırlatılıyor! Puan değişimi: {resultData.ScoreChange}");

        // Event'i fırlat.
        OnWasteProcessed?.Invoke(resultData);

        // İşlem tamamlandıktan sonra atık objesini sahneden yok et.
        // Çöplerin içi içe geçmiş prefablar olma ihtimaline karşı her zaman en dıştaki (Root) objeyi siliyoruz.
        Debug.Log($"<color=green>[BinTrigger]</color> {other.transform.root.name} objesi tamamen yok edildi.");
        Destroy(other.transform.root.gameObject);
    }

    /// <summary>
    /// String karşılaştırmaları yerine Unity'nin GC üretmeyen CompareTag metodunu kullanır.
    /// Prefab'ların ana gövdesine (Root) konan Tag'leri okuyabilmek için yukarı doğru tarar.
    /// </summary>
    private WasteType GetWasteTypeFromCollider(Collider col)
    {
        // 1. Önce doğrudan çarpan parçaya veya onun Rigidbody'sine bakalım
        GameObject directObj = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
        if (CheckTag(directObj, out WasteType type)) return type;

        // 2. Eğer bulamadıysa, kesin Tag'i en dıştaki (Root) objeye koymuştur. Oraya bakalım:
        GameObject rootObj = col.transform.root.gameObject;
        if (CheckTag(rootObj, out WasteType rootType)) return rootType;

        return WasteType.Untagged;
    }

    private bool CheckTag(GameObject obj, out WasteType type)
    {
        if (obj.CompareTag("Paper")) { type = WasteType.Paper; return true; }
        if (obj.CompareTag("Glass")) { type = WasteType.Glass; return true; }
        if (obj.CompareTag("Plastic")) { type = WasteType.Plastic; return true; }
        if (obj.CompareTag("Metal")) { type = WasteType.Metal; return true; }
        
        type = WasteType.Untagged;
        return false;
    }
}
