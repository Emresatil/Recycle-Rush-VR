using System;
using UnityEngine;

// Atık türleri için Enum yapısı (Inspector'dan kolayca seçilebilmesi için)
public enum WasteType
{
    Paper = 0,
    Glass = 1,
    Plastic = 2,
    Metal = 3,
    Untagged = 4, // Atık olmayan objeler (oyuncunun eli vb.) için
    Hourglass = 5 // Power-up (Süre ekleyen Kum Saati)
}

// Event üzerinden diğer sistemlere (Manager'lara) aktarılacak paket veri yapısı
public struct SortResultData
{
    public bool IsCorrect;
    public WasteType ProcessedWasteType; // Gün 11: Görevler için atılan çöpün türü
    public int ScoreChange;
    public int CoinChange; // Gün 7: Kazanılacak Para
    public int XpChange; // Gün 7: Kazanılacak XP
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
    [SerializeField] private int _correctCoin = 5;
    [SerializeField] private int _correctXp = 20;
    [SerializeField] private float _correctHapticDuration = 0.2f;
    [SerializeField] private float _correctHapticAmplitude = 0.5f;

    [Header("Yanlış Eşleşme (Incorrect) Parametreleri")]
    [SerializeField] private int _incorrectScore = -5;
    [SerializeField] private int _incorrectCoin = 0;
    [SerializeField] private float _incorrectHapticDuration = 0.4f;
    [SerializeField] private float _incorrectHapticAmplitude = 0.8f;

    [Header("Görsel Efektler (VFX)")]
    [Tooltip("Doğru kutuya atıldığında çıkacak yeşil patlama efekti (Prefab)")]
    [SerializeField] private GameObject _successParticlePrefab;
    [Tooltip("Yanlış kutuya atıldığında çıkacak kırmızı duman efekti (Prefab)")]
    [SerializeField] private GameObject _failParticlePrefab;

    // Sistemler arası spagetti bağlantıları engelleyen (Loose Coupling) statik Action Event'imiz.
    // ScoreManager, AudioManager ve HapticManager sadece bu event'e Abone (Subscribe) olacaktır.
    public static event Action<SortResultData> OnWasteProcessed;
    public static event Action<int> OnComboChanged; // Gün 11: Kombo tetikleyicisi

    private static int _currentCombo = 0; // Tüm kutular için ortak kombo sayacı

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
        if (other == null) return;
        
        // Giren objenin kendisini veya Rigidbody'sini bul (Oyun objesinin en dış kapsayıcısı)
        GameObject wasteObj = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        
        // Zaten havuza gönderilmiş ve kapatılmış bir obje tekrar tetiklenmemeli (Aynı karede 2 collider çarpışması önlemi)
        if (!wasteObj.activeInHierarchy) return;

        // GÜVENLİK: Eğer oyun aktif veya öğretici modunda değilse puan hesaplama ve atığı yoksay!
        if (GameManager.Instance != null && 
            GameManager.Instance.CurrentState != GameState.Playing && 
            GameManager.Instance.CurrentState != GameState.Tutorial)
        {
            Debug.Log("<color=red>[BinTrigger]</color> Oyun aktif değil! Atık kutuya girse de puan kazandırmaz.");
            return;
        }

        Debug.Log($"<color=orange>[BinTrigger]</color> Kutunun içine bir şey girdi! Giren şeyin adı: {wasteObj.name}");

        // Giren objenin atık türünü alıyoruz.
        WasteType incomingType = GetWasteTypeFromCollider(other);
        
        Debug.Log($"<color=yellow>[BinTrigger]</color> {wasteObj.name} objesinin Tag kontrolü yapıldı. Bulunan Atık Türü: {incomingType}");

        // Eğer giren obje bir atık değilse işlemi iptal et.
        if (incomingType == WasteType.Untagged) 
        {
            Debug.Log("<color=red>[BinTrigger]</color> Bu obje Untagged (Etiketsiz) olduğu için puanlama yapılmadı ve silinmedi!");
            return;
        }

        // Doğruluk mantığı: Giren atığın türü, kutunun kabul ettiği türe eşit mi?
        // Power-Up (Kum Saati) her kutuda doğru kabul edilir.
        bool isCorrect = (incomingType == _acceptedWasteType) || (incomingType == WasteType.Hourglass);
        
        // --- POWER-UP: KUM SAATİ MANTIĞI ---
        if (incomingType == WasteType.Hourglass && isCorrect)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddTime(10f); // 10 Saniye ekle
            }
        }

        // --- KOMBO SİSTEMİ (Gün 11) ---
        if (isCorrect)
        {
            _currentCombo++;
            if (_currentCombo > 1) 
            {
                OnComboChanged?.Invoke(_currentCombo);
            }
        }
        else
        {
            if (_currentCombo > 0)
            {
                _currentCombo = 0;
                OnComboChanged?.Invoke(0);
            }
        }

        // --- GÖRSEL EFEKT (PARTİCÜL) ÇAĞIRMA ---
        GameObject particleToSpawn = isCorrect ? _successParticlePrefab : _failParticlePrefab;
        if (particleToSpawn != null)
        {
            // Kutunun merkezinden yarım metre yukarıda oluştur
            Vector3 spawnPosition = transform.position + new Vector3(0, 0.5f, 0);
            GameObject spawnedParticle = Instantiate(particleToSpawn, spawnPosition, Quaternion.identity);
            
            Debug.Log($"<color=white>[VFX]</color> Partikül oluşturuldu: {spawnedParticle.name} at {spawnPosition}");
            
            // Sahne kirlenmesin diye efekti 3 saniye sonra otomatik sil
            Destroy(spawnedParticle, 3f);
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[VFX]</color> Kutuda {(isCorrect ? "Success" : "Fail")} particle prefab'i eksik!");
        }
        
        Debug.Log($"<color=cyan>[BinTrigger]</color> Kutu Türü: {_acceptedWasteType} | Gelen Çöp Türü: {incomingType} | Eşleşme: {isCorrect}");

        // Diğer Manager sınıflarına yayınlanacak veri paketi
        SortResultData resultData = new SortResultData
        {
            IsCorrect = isCorrect,
            ProcessedWasteType = incomingType, // Hangi atık türü olduğunu belirttik
            ScoreChange = isCorrect ? _correctScore : -_incorrectScore,
            CoinChange = isCorrect ? _correctCoin : -_incorrectCoin,
            XpChange = isCorrect ? _correctXp : 0, // Hatalı atışta XP silinmez
            ActionPosition = wasteObj.transform.position,
            HapticDuration = isCorrect ? _correctHapticDuration : _incorrectHapticDuration,
            HapticAmplitude = isCorrect ? _correctHapticAmplitude : _incorrectHapticAmplitude
        };

        Debug.Log($"<color=magenta>[BinTrigger]</color> OnWasteProcessed sinyali fırlatılıyor! Puan: {resultData.ScoreChange} | Coin: {resultData.CoinChange} | XP: {resultData.XpChange}");

        // Event'i fırlat.
        OnWasteProcessed?.Invoke(resultData);

        // İşlem tamamlandıktan sonra atık objesini sahneden yok et.
        Debug.Log($"<color=green>[BinTrigger]</color> {wasteObj.name} objesi havuza geri gönderildi.");
        ObjectPoolManager.Instance.ReturnToPool(wasteObj);
    }

    /// <summary>
    /// Objenin neresine (Root, Mesh, Collider) Tag konulduğunu bilemeyeceğimiz için,
    /// objenin tamamını (kendisini ve tüm alt çocuklarını) tarayıp Tag'i bulur. (Foolproof)
    /// </summary>
    private WasteType GetWasteTypeFromCollider(Collider col)
    {
        if (col == null) return WasteType.Untagged;
        
        // En dış (Root) objeyi bul (attachedRigidbody sayesinde güvenli bir şekilde ana prefab objesini buluruz)
        GameObject wasteObj = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;

        // Objenin kendisine ve BÜTÜN alt objelerine (çocuklarına) sırayla bak
        foreach (Transform child in wasteObj.GetComponentsInChildren<Transform>(true))
        {
            if (CheckTag(child.gameObject, out WasteType type)) 
            {
                return type;
            }
        }

        return WasteType.Untagged;
    }

    private bool CheckTag(GameObject obj, out WasteType type)
    {
        if (obj.CompareTag("Paper")) { type = WasteType.Paper; return true; }
        if (obj.CompareTag("Glass")) { type = WasteType.Glass; return true; }
        if (obj.CompareTag("Plastic")) { type = WasteType.Plastic; return true; }
        if (obj.CompareTag("Metal")) { type = WasteType.Metal; return true; }
        if (obj.CompareTag("Hourglass")) { type = WasteType.Hourglass; return true; }
        
        type = WasteType.Untagged;
        return false;
    }
}
