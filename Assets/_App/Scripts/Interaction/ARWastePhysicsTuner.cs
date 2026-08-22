using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

namespace RecycleRush.Interaction
{
    public enum WasteMaterialType
    {
        Plastic,    // Dengeli (Orta ağırlık, orta sürtünme)
        Paper,      // Hafif (Süzülerek düşer)
        Glass,      // Ağır (Hızlı düşer, tok hissettirir)
        Metal       // En ağır (En hızlı düşer, stabil uçuş)
    }

    /// <summary>
    /// AR ortamı için çöp objelerinin fizik (Rigidbody) ve fırlatma (Grab/Throw) 
    /// etkileşimlerini otomatik olarak optimize eden ve kalibre eden modüler sınıftır.
    /// Obje sahnede doğduğunda (Awake) ayarları zorla (override) uygular.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ARWastePhysicsTuner : MonoBehaviour
    {
        [Header("Material Physics")]
        [Tooltip("Çöpün materyalini seçin. Fizik ayarları (Mass, Drag vb.) buna göre otomatik kalibre edilir.")]
        public WasteMaterialType materialType = WasteMaterialType.Plastic;

        [Header("Golden Waste Settings")]
        [Tooltip("Eğer bu obje Altın Çöp ise işaretleyin. Özel titreşim ve puanlama sistemlerini tetikler.")]
        public bool isGoldenWaste = false;

        [Header("AR Physics Calibration")]
        [Tooltip("Çöpün yere düştüğünde sonsuza kadar yuvarlanmasını önlemek için standart dönüş sürtünmesi (Unity 6 Damping).")]
        [SerializeField] private float defaultAngularDamping = 2.0f;

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

            // 1. Materyale Göre Kütle ve Sürtünme Ataması
            switch (materialType)
            {
                case WasteMaterialType.Paper:
                    _rigidbody.mass = 0.1f;          // Daha da hafiflettik
                    _rigidbody.linearDamping = 5.0f; // Paraşüt gibi yavaş inmesi için hava sürtünmesini çok artırdık (eski: 2)
                    _rigidbody.angularDamping = 4.0f; 
                    break;
                case WasteMaterialType.Plastic:
                    _rigidbody.mass = 0.5f;
                    _rigidbody.linearDamping = 0.5f;
                    _rigidbody.angularDamping = 1.0f;
                    break;
                case WasteMaterialType.Glass:
                    _rigidbody.mass = 1.2f;
                    _rigidbody.linearDamping = 0.1f; // Drag 0 olmasın ki aşırı savrulmasın
                    _rigidbody.angularDamping = defaultAngularDamping;
                    break;
                case WasteMaterialType.Metal:
                    _rigidbody.mass = 1.5f;
                    _rigidbody.linearDamping = 0.05f;
                    _rigidbody.angularDamping = defaultAngularDamping;
                    break;
            }
            
            // --- AAA KALİTE FİZİK STANDARTLARI ---
            // 1. Tünelleme Önleyici: Hızlı fırlatılan çöpün kutu duvarının içinden geçip gitmesini (Ghosting) engeller.
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            
            // 3. Havada Yumuşatma: Çöp havada uçarken FPS takılmasını gizler, sinematik ve pürüzsüz bir uçuş sağlar.
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            
            Debug.Log($"<color=green>[AR Physics]</color> {gameObject.name} ({materialType}) fiziği AR için optimize edildi.");
        }

        private void FixedUpdate()
        {
            // Sadece Kağıt objeleri salınım (yaprak düşüşü) yapar
            if (materialType != WasteMaterialType.Paper) return;
            if (_rigidbody == null || _grabInteractable == null) return;

            // Eğer obje Gravity Pull ile çekiliyorsa veya elde tutuluyorsa süzülme fiziğini DEVRE DIŞI bırak!
            if (_grabInteractable.isSelected) return;

            // Sadece aşağı doğru düşerken ve çok şiddetli fırlatılmamışken salınım yap (hedefi şaşırtmasın)
            if (_rigidbody.linearVelocity.y < -0.1f && _rigidbody.linearVelocity.magnitude < 5.0f)
            {
                // Kuvveti çok ciddi oranda artırdık ki VR'da gözle net görülebilsin
                // Frekansı (hızı) aynı tuttuk ama şiddetini (multiplier) 0.4'ten 4.0'a çıkardık
                float swayForceX = Mathf.Sin(Time.time * 3f) * 4.0f;
                float swayForceZ = Mathf.Cos(Time.time * 2f) * 3.0f;

                // ForceMode.Acceleration kullanarak kütleden (mass) bağımsız direkt itiş gücü uyguluyoruz
                _rigidbody.AddForce(new Vector3(swayForceX, 0, swayForceZ), ForceMode.Acceleration);
            }
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
