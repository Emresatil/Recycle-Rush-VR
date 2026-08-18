using System;
using UnityEngine;


namespace RecycleRush.Interaction
{
    /// <summary>
    /// Saf fizik ve hareket işçisi (Motion Handler). Sadece kavisli çekimi yapar ve sıfır GC ile çalışır.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GravityPullMotion : MonoBehaviour
    {
        [Header("Motion Settings")]
        [SerializeField, Tooltip("Hareketin tamamlanma süresi (sn)")]
        private float _pullDuration = 0.3f;
        
        [SerializeField, Tooltip("Kavisin tepe yüksekliği")]
        private float _arcHeight = 0.5f;

        [SerializeField, Tooltip("Hız eğrisi (Yumuşak başlangıç ve bitiş)")]
        private AnimationCurve _speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Rigidbody _rigidbody;
        private Transform _targetHand;
        private Action<GravityPullMotion> _onPullCompleted;
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grabInteractable;

        private Vector3 _startPosition;
        private float _timeElapsed;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            this.enabled = false; // Başlangıçta inaktif
        }

        public void StartPull(Transform targetHand, UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable, Action<GravityPullMotion> onCompleted)
        {
            if (targetHand == null || grabInteractable == null) return;

            _targetHand = targetHand;
            _grabInteractable = grabInteractable;
            _onPullCompleted = onCompleted;
            _startPosition = transform.position;
            _timeElapsed = 0f;

            // XR Toolkit'in pozisyon kontrolünü uçuş boyunca devre dışı bırak.
            // Bu sayede obje "Tutulmuş" (Selected) statüsünü korur ama biz onu hareket ettirebiliriz.
            _grabInteractable.trackPosition = false;

            this.enabled = true; // Componenti aktifleştir ve uçuşu başlat
        }

        private void Update()
        {
            // Null-Check: Hedef kaybolduysa güvenli iptal et
            if (_targetHand == null || _grabInteractable == null || !_grabInteractable.isSelected)
            {
                CancelPull();
                return;
            }

            _timeElapsed += Time.deltaTime;
            float rawT = _timeElapsed / _pullDuration;
            
            if (rawT >= 1f)
            {
                CompletePull();
                return;
            }

            // Optimize edilmiş kavis hesaplaması (GC yaratmaz)
            float curvedT = _speedCurve.Evaluate(rawT);
            
            Vector3 targetPos = _targetHand.position;
            Vector3 currentPos = Vector3.Lerp(_startPosition, targetPos, curvedT);
            
            float arc = Mathf.Sin(rawT * Mathf.PI) * _arcHeight;
            currentPos.y += arc;

            _rigidbody.MovePosition(currentPos);
        }

        private void CompletePull()
        {
            if (_targetHand != null)
            {
                transform.position = _targetHand.position;
            }

            if (_grabInteractable != null)
            {
                _grabInteractable.trackPosition = true;
            }
            
            // Delegate ile haberdar et ve uyu
            _onPullCompleted?.Invoke(this);
            this.enabled = false;
        }

        public void CancelPull()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.trackPosition = true;
            }
            this.enabled = false;
        }
    }
}
