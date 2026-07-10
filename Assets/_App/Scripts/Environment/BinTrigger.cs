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
        // Giren objenin atık türünü, en optimize tag kontrol metodu ile alıyoruz.
        WasteType incomingType = GetWasteTypeFromCollider(other);
        
        // Eğer giren obje bir atık değilse (örn: oyuncu eli, zemin vb.), işlemi iptal et (Early Exit).
        if (incomingType == WasteType.Untagged) return;

        // Doğruluk mantığı: Giren atığın türü, kutunun kabul ettiği türe eşit mi?
        bool isCorrect = (incomingType == _acceptedWasteType);

        // Diğer Manager sınıflarına yayınlanacak (Broadcast) veri paketini hazırla
        SortResultData resultData = new SortResultData
        {
            IsCorrect = isCorrect,
            ActionPosition = transform.position,
            ScoreChange = isCorrect ? _correctScore : _incorrectScore,
            HapticDuration = isCorrect ? _correctHapticDuration : _incorrectHapticDuration,
            HapticAmplitude = isCorrect ? _correctHapticAmplitude : _incorrectHapticAmplitude
        };

        // Event'i fırlat. "?." operatörü, null check yaparak (eğer dinleyen sistem yoksa) hata vermesini engeller.
        OnWasteProcessed?.Invoke(resultData);

        // İşlem tamamlandıktan sonra atık objesini sahneden yok et.
        // Eğer Tag ve Collider alt objede (child) ise sadece o parçayı silmemesi için,
        // Rigidbody'nin bağlı olduğu ana (Root) prefab objesini bulup tamamını siliyoruz.
        if (other.attachedRigidbody != null)
        {
            Destroy(other.attachedRigidbody.gameObject);
        }
        else
        {
            Destroy(other.gameObject); // Yedek (Fallback) durum
        }
    }

    /// <summary>
    /// String karşılaştırmaları yerine Unity'nin GC (Garbage Collection) üretmeyen
    /// ve çok daha hızlı olan CompareTag metodunu kullanır.
    /// </summary>
    private WasteType GetWasteTypeFromCollider(Collider col)
    {
        if (col.CompareTag("Paper")) return WasteType.Paper;
        if (col.CompareTag("Glass")) return WasteType.Glass;
        if (col.CompareTag("Plastic")) return WasteType.Plastic;
        if (col.CompareTag("Metal")) return WasteType.Metal;
        
        return WasteType.Untagged;
    }
}
