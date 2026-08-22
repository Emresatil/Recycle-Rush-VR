using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using RecycleRush.Core;

namespace RecycleRush.Environment
{
    /// <summary>
    /// Çöplerin elden düştükten sonra bir yüzeye çarpıp çarpmadığını algılar.
    /// 3 saniyelik kurtarma süresi (Recovery Window) tanır.
    /// Kurtarılmazsa RoomPollutionManager'a ceza yollar.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class FloorPollutionDetector : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Çöp yere düştükten sonra kurtarmak için verilen süre (saniye)")]
        [SerializeField] private float recoveryWindow = 3f;
        
        [Tooltip("Kurtarılamadığında eklenecek kirlilik miktarı")]
        [SerializeField] private float penaltyAmount = 5f;

        private XRGrabInteractable _interactable;
        private bool _isCurrentlyGrabbed = false;
        private bool _hasBeenGrabbedAtLeastOnce = false;
        private bool _isPenalized = false;
        
        private Coroutine _penaltyCoroutine;

        private void Awake()
        {
            _interactable = GetComponent<XRGrabInteractable>();
        }

        private void OnEnable()
        {
            _isCurrentlyGrabbed = false;
            _hasBeenGrabbedAtLeastOnce = false;
            _isPenalized = false;

            if (_interactable != null)
            {
                _interactable.selectEntered.AddListener(OnGrabbed);
                _interactable.selectExited.AddListener(OnReleased);
            }
        }

        private void OnDisable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.RemoveListener(OnGrabbed);
                _interactable.selectExited.RemoveListener(OnReleased);
            }

            // Kutuya girip havuza (Pool) döndüğünde veya silindiğinde sayacı iptal et
            CancelPenaltyTimer();
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            _isCurrentlyGrabbed = true;
            _hasBeenGrabbedAtLeastOnce = true;

            // Eğer Gravity Grab veya normal elle son saniyede kurtardıysa sayacı iptal et
            if (_penaltyCoroutine != null)
            {
                CancelPenaltyTimer();
                
                // İstatistikler için kurtarma kaydı yap
                if (RoomPollutionManager.Instance != null && !_isPenalized)
                {
                    RoomPollutionManager.Instance.RecordWasteRecovered();
                    Debug.Log("<color=green>[FloorPollutionDetector]</color> Çöp son anda kurtarıldı!");
                }
            }
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            _isCurrentlyGrabbed = false;
            // Bırakıldığında havada olacak. Herhangi bir yere (zemine/masaya) çarptığında OnCollisionEnter tetiklenecek.
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Sadece daha önce elde tutulmuş ve şu an havadan düşmüşse sayacı başlat (Spawn olduğu an cezalandırmamak için)
            if (_hasBeenGrabbedAtLeastOnce && !_isCurrentlyGrabbed && !_isPenalized)
            {
                // Çöp kutularının içine girdiğinde (Trigger) bu çalışmaz. 
                // Sadece katı objelere (zemin, duvar, kutunun dış yüzeyi) çarptığında sayaç başlar.
                if (_penaltyCoroutine == null)
                {
                    Debug.Log($"<color=orange>[FloorPollutionDetector]</color> Çöp bir yüzeye çarptı! {recoveryWindow} saniye kurtarma süresi başladı.");
                    _penaltyCoroutine = StartCoroutine(PenaltyTimerRoutine());
                }
            }
        }

        private IEnumerator PenaltyTimerRoutine()
        {
            yield return new WaitForSeconds(recoveryWindow);

            // Süre doldu ve hala alınmadıysa cezayı kes!
            if (!_isCurrentlyGrabbed && !_isPenalized)
            {
                _isPenalized = true;

                if (RoomPollutionManager.Instance != null)
                {
                    Debug.Log($"<color=red>[FloorPollutionDetector]</color> Süre doldu! Kirlilik +{penaltyAmount} arttı.");
                    RoomPollutionManager.Instance.AddPollution(penaltyAmount);
                }
                
                // Ceza kesildikten sonra çöpü yok edebilir/havuza geri gönderebiliriz.
                if (ObjectPoolManager.Instance != null)
                {
                    ObjectPoolManager.Instance.ReturnToPool(transform.root.gameObject);
                }
                else
                {
                    Destroy(transform.root.gameObject);
                }
            }

            _penaltyCoroutine = null;
        }

        private void CancelPenaltyTimer()
        {
            if (_penaltyCoroutine != null)
            {
                StopCoroutine(_penaltyCoroutine);
                _penaltyCoroutine = null;
            }
        }
    }
}
