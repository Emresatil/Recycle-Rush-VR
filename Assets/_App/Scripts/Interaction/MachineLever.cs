using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Animasyonlu makine kolunun VR kontrolcüleri ile etkileşime girmesini sağlar.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class MachineLever : MonoBehaviour
{
    private Animator _animator;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable _interactable;

    [Header("Animasyon Ayarları")]
    [Tooltip("Animator penceresinde oluşturacağınız Trigger parametresinin tam adı.")]
    public string animationTriggerName = "Pull";

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        // VR Kontrolcüsü ile tıklandığında (Seçildiğinde) OnLeverPulled fonksiyonunu çalıştır
        _interactable.selectEntered.AddListener(OnLeverPulled);
    }

    private void OnDisable()
    {
        // Obje kapanırsa dinlemeyi bırak (Memory Leak önlemi)
        _interactable.selectEntered.RemoveListener(OnLeverPulled);
    }

    // Sabri'nin yazacağı AudioManager'ın bu kol çekildiğinde (Klik sesi için) dinleyeceği özel Event
    public static event System.Action OnLeverPulledAction;

    private void OnLeverPulled(SelectEnterEventArgs args)
    {
        // Animator'daki tetikleyiciyi (Trigger) ateşle
        if (_animator != null)
        {
            _animator.SetTrigger(animationTriggerName);
            Debug.Log($"<color=green>[MachineLever]</color> Kol çekildi! '{animationTriggerName}' animasyonu başlatılıyor.");
            
            // Kol her çekildiğinde mekanik klik sesi çalınması için anons (Broadcast) yapıyoruz.
            OnLeverPulledAction?.Invoke();

            // Eğer oyun henüz başlamadıysa (Örneğin MainMenu/Bekleme durumundaysa) oyunu BAŞLAT!
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                GameManager.Instance.StartGame();
                Debug.Log("<color=yellow>[MachineLever]</color> Vardiya başlatıldı! GameManager tetiklendi.");
            }
        }
    }
}
