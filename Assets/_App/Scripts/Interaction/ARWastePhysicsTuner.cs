using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

namespace RecycleRush.Interaction
{
    /// <summary>
    /// AR ortamı için çöp objelerinin fizik (Rigidbody) ve fırlatma (Grab/Throw) 
    /// etkileşimlerini otomatik olarak optimize eden ve kalibre eden modüler sınıftır.
    /// Obje sahnede doğduğunda (Awake) ayarları zorla (override) uygular.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ARWastePhysicsTuner : MonoBehaviour
    {
        [Header("Golden Waste Settings")]
        [Tooltip("Eğer bu obje Altın Çöp ise işaretleyin. Özel titreşim ve puanlama sistemlerini tetikler.")]
        public bool isGoldenWaste = false;

        [Header("AR Physics Calibration")]
        [Tooltip("AR ortamında çöpün yere düştüğünde sonsuza kadar yuvarlanmasını önlemek için uygulanacak dönüş sürtünmesi (Unity 6 Damping).")]
        [SerializeField] private float targetAngularDamping = 2.0f;
        
        [Tooltip("Çöpün ağırlık hissi. Çok ağır olursa XR elinden yavaş çıkar.")]
        [SerializeField] private float targetMass = 0.5f;

        [Header("AR Throw Calibration")]
        [Tooltip("Gerçek dünyada elimizde fiziksel bir çöp ağırlığı olmadığı için fırlatma gücünü AR için yapay olarak artırır.")]
        [SerializeField] private float throwVelocityMultiplier = 1.6f;

        [Tooltip("Elden bırakırken oluşan mikro titremeleri (Jitter) yumuşatmak için gereken süre. Fırlatma kavisini kusursuzlaştırır.")]
        [SerializeField] private float smoothingDuration = 0.25f;

        private Rigidbody _rigidbody;
        private XRGrabInteractable _grabInteractable;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _grabInteractable = GetComponent<XRGrabInteractable>();

            ApplyARPhysicsCalibration();
            ApplyARGrabCalibration();
        }

        public float SpawnTime { get; private set; }

        private void OnEnable()
        {
            SpawnTime = Time.time; // Obje havuza geri dönüp tekrar çıkarsa süreyi sıfırlar
            if (_grabInteractable != null)
                _grabInteractable.selectEntered.AddListener(OnGrabbed);
        }

        private void OnDisable()
        {
            if (_grabInteractable != null)
                _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            // Eğer objeyi tutan el (interactor) standart bir XR input (sağ/sol kol) ise:
            if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor inputInteractor)
            {
                if (RecycleRush.Core.HapticManager.Instance != null)
                {
                    if (isGoldenWaste)
                    {
                        // Altın Çöp tutuldu! Kalp atışı titreşimi gönder.
                        RecycleRush.Core.HapticManager.Instance.TriggerGoldenWasteHaptic(inputInteractor);
                        Debug.Log("<color=yellow>[Golden Waste]</color> Efsanevi çöp elinize alındı! Ritmik titreşim tetikleniyor.");
                    }
                    else
                    {
                        // Normal çöp tutuldu. Standart 'Click' titreşimi gönder.
                        RecycleRush.Core.HapticManager.Instance.TriggerGrabHaptic(inputInteractor);
                    }
                }
            }
        }

        private void ApplyARPhysicsCalibration()
        {
            if (_rigidbody == null) return;

            // Kütle ve sürtünme ayarları
            _rigidbody.mass = targetMass;
            _rigidbody.angularDamping = targetAngularDamping;
            
            // --- AAA KALİTE FİZİK STANDARTLARI ---
            // 1. Tünelleme Önleyici: Hızlı fırlatılan çöpün kutu duvarının içinden geçip gitmesini (Ghosting) engeller.
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            // 2. Havada Yumuşatma: Çöp havada uçarken FPS takılmasını gizler, sinematik ve pürüzsüz bir uçuş sağlar.
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            
            Debug.Log($"<color=green>[AR Physics]</color> {gameObject.name} fiziği AR için optimize edildi. (Mass: {targetMass}, Damping: {targetAngularDamping}, ContinuousDynamic: Açık)");
        }

        private void ApplyARGrabCalibration()
        {
            if (_grabInteractable == null) return;

            // 1. Fırlatma gücünü (Throw Velocity) AR ortamına göre artırıyoruz
            _grabInteractable.throwVelocityScale = throwVelocityMultiplier;
            
            // 2. Fırlatma anındaki (Release) pürüzleri gideriyoruz (AAA Hissi)
            _grabInteractable.throwSmoothingDuration = smoothingDuration;

            // 3. Objeyi tutarken duvarların, masanın içinden geçmesini engellemek için (Fizik tabanlı tutuş)
            _grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            
            Debug.Log($"<color=green>[AR Grab]</color> {gameObject.name} fırlatma ve tutma ayarları kalibre edildi. (ThrowScale: {throwVelocityMultiplier})");
        }
    }
}
