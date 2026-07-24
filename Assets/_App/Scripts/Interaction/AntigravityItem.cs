using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class AntigravityItem : MonoBehaviour
{
    [Header("Antigravity Settings")]
    [SerializeField, Tooltip("Havada süzülme aktif mi? (Bant üzerindeki atıklar için FALSE olmalıdır)")]
    private bool _enableFloating = false;

    [SerializeField, Tooltip("Havada süzülürken uygulanacak yukarı yönlü kuvvet.")]
    private float _floatForce = 0.5f;

    [SerializeField, Tooltip("Uzay boşluğunda dönüyormuş hissi veren dönüş torku.")]
    private float _spinTorque = 0.2f;

    private Rigidbody _rb;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grabInteractable;
    private bool _isGrabbed = false;
    private Vector3 _randomTorqueDirection;

    private void Awake()
    {
        // Önbellekleme (Caching) - Garbage Collection'ı yormamak için
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) _rb = GetComponentInChildren<Rigidbody>();
        
        // Prefab yapısına göre XRGrabInteractable root'ta, alt objede veya üst objede olabilir. Hepsini tara!
        _grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (_grabInteractable == null) _grabInteractable = GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (_grabInteractable == null) _grabInteractable = GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (_grabInteractable == null)
        {
            Debug.LogError($"[AntigravityItem] KRİTİK HATA! {gameObject.name} üzerinde XRGrabInteractable bulunamadı! Tutma sesleri ve titreşim çalışmayacak.");
        }
    }

    private void OnEnable()
    {
        if (_grabInteractable != null)
        {
            // XR Interaction Toolkit event'lerine abone ol (Observer Pattern)
            _grabInteractable.selectEntered.AddListener(OnGrabbed);
            _grabInteractable.selectExited.AddListener(OnReleased);
        }
        
        // Obje her havuzdan çekildiğinde (aktifleştiğinde) durumu sıfırla
        ResetAntigravityState();
    }

    private void OnDisable()
    {
        if (_grabInteractable != null)
        {
            // Bellek sızıntılarını (Memory Leak) önlemek için abonelikleri iptal et
            _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            _grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void FixedUpdate()
    {
        // Eğer oyuncu objeyi elinde tutmuyorsa süzülme fiziğini uygula
        if (!_isGrabbed)
        {
            ApplyAntigravity();
        }
    }

    /// <summary>
    /// Antigravity durumunu başlatır. Yerçekimini keser ve rastgele dönüş yönü belirler.
    /// </summary>
    private void ResetAntigravityState()
    {
        _isGrabbed = false;
        
        if (_rb != null)
        {
            if (_enableFloating)
            {
                _rb.useGravity = false; // Yerçekimini kapat (Antigravity başlangıcı)
            }
            else
            {
                _rb.useGravity = true; // Süzülme kapalıysa yerçekiminin AÇIK olduğundan emin ol!
            }
            _rb.linearVelocity = Vector3.zero; // Önceki hareketleri sıfırla
            _rb.angularVelocity = Vector3.zero;
        }

        // Objeye rastgele hafif bir dönüş (Spin) yönü tayin et
        _randomTorqueDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;
    }

    /// <summary>
    /// Fizik motoru üzerinden yukarı yönlü süzülme ve dönme kuvveti uygular.
    /// </summary>
    private void ApplyAntigravity()
    {
        if (_rb == null || !_enableFloating) return;

        // Yukarı doğru hafif süzülme kuvveti
        _rb.AddForce(Vector3.up * _floatForce, ForceMode.Force);
        
        // Kendi ekseni etrafında hafif dönüş torku
        _rb.AddTorque(_randomTorqueDirection * _spinTorque, ForceMode.Force);
    }

    /// <summary>
    /// Oyuncu objeyi XR kontrolcüsüyle tuttuğunda tetiklenir.
    /// </summary>
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        _isGrabbed = true;
        // Not: XRGrabInteractable zaten rigidbody kinematiğini veya hareket tipini
        // (Velocity Tracking) kendi yönettiği için burada extradan fizikle oynamıyoruz.
        
        // Ses efekti (SFX) çal
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGrabSound(transform.position);
        }

        // Dokunsal Geri Bildirim (Haptic Feedback)
        if (RecycleRush.Core.HapticManager.Instance != null && 
            args.interactorObject is XRBaseInputInteractor controllerInteractor)
        {
            Debug.Log($"<color=magenta>[Haptic/Grab]</color> {gameObject.name} tutuldu! Titreşim yollanıyor.");
            // Tutma anında kesin hissedilir bir 'Pop' titreşimi (0.5 şiddet, 0.1 saniye)
            RecycleRush.Core.HapticManager.Instance.TriggerHaptic(controllerInteractor, 0.5f, 0.1f);
        }
        else
        {
            Debug.LogWarning($"<color=red>[Haptic/Grab]</color> Titreşim tetiklenemedi! HapticManager={RecycleRush.Core.HapticManager.Instance != null}, InteractorCast={args.interactorObject is XRBaseInputInteractor}");
        }
    }

    /// <summary>
    /// Oyuncu objeyi fırlattığında veya bıraktığında tetiklenir.
    /// </summary>
    private void OnReleased(SelectExitEventArgs args)
    {
        _isGrabbed = false;
        
        // Oyuncu fırlattığı anda normal dünya yerçekimini geri ver 
        // ki obje parabolik (gerçekçi) bir fizikle kutuya düşsün.
        if (_rb != null)
        {
            _rb.useGravity = true;
        }

        // Ses efekti (SFX) çal
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayReleaseSound(transform.position);
        }
    }
}
