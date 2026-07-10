using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class AntigravityItem : MonoBehaviour
{
    [Header("Antigravity Settings")]
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
        _grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
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
            _rb.useGravity = false; // Yerçekimini kapat (Antigravity başlangıcı)
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
        if (_rb == null) return;

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
    }
}
