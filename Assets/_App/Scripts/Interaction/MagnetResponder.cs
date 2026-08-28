using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace RecycleRush.Interaction
{
    [RequireComponent(typeof(Rigidbody))]
    public class MagnetResponder : MonoBehaviour
    {
        private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grabInteractable;
        private Rigidbody _rb;
        private Collider _col;
        private bool _isFlying = false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb == null) _rb = GetComponentInChildren<Rigidbody>();
            
            _col = GetComponent<Collider>();
            if (_col == null) _col = GetComponentInChildren<Collider>();
            
            // Eğer objenin üzerinde XRGrabInteractable yoksa alt objelerde (mesh) ara
            _grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (_grabInteractable == null)
            {
                _grabInteractable = GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            }
        }

        private void OnEnable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.AddListener(OnGrabbed);
            }
            _isFlying = false;
        }

        private void OnDisable()
        {
            if (_grabInteractable != null)
            {
                _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            }
            _isFlying = false;
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            Debug.Log($"<color=cyan>[MagnetResponder]</color> Obje tutuldu! Magnet Aktif mi?: {(GameManager.Instance != null ? GameManager.Instance.IsMagnetActive.ToString() : "GameManager NULL")}");
            
            // Mıknatıs gücü açık mı kontrol et
            if (GameManager.Instance != null && GameManager.Instance.IsMagnetActive && !_isFlying)
            {
                // Çöpün türünü bul
                WasteType myType = BinTrigger.GetWasteTypeFromCollider(_col);
                Debug.Log($"<color=cyan>[MagnetResponder]</color> Tutulan objenin türü: {myType}");
                
                // Untagged veya PowerUp değilse uçuşu başlat
                if (myType != WasteType.Untagged && myType != WasteType.Hourglass && myType != WasteType.Magnet)
                {
                    Transform targetBin = BinTrigger.GetBinTransform(myType);
                    if (targetBin != null)
                    {
                        Debug.Log($"<color=cyan>[MagnetResponder]</color> Hedef kutu bulundu: {targetBin.name}. Uçuş başlatılıyor!");
                        StartCoroutine(FlyToBinRoutine(args.interactorObject, targetBin));
                    }
                    else
                    {
                        Debug.LogWarning($"<color=red>[MagnetResponder]</color> {myType} türü için sahnede hedef kutu bulunamadı!");
                    }
                }
            }
        }

        private IEnumerator FlyToBinRoutine(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor, Transform targetBin)
        {
            _isFlying = true;

            // 1. Oyuncunun elinden objeyi YUMUŞAKÇA ve ZORLA al
            // Bu adım XR bug'larını (Phantom forces, el kilitlenmesi) önler.
            if (_grabInteractable != null && _grabInteractable.interactionManager != null)
            {
                _grabInteractable.interactionManager.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)_grabInteractable);
            }

            // 2. Havada uçarken başka çöpleri veya oyuncuyu fırlatmasın diye fizikleri kapat
            if (_rb != null)
            {
                if (!_rb.isKinematic)
                {
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }
                _rb.isKinematic = true;
            }
            if (_col != null) _col.enabled = false;

            // 3. Hedef kutunun tam üstüne (0.5m yukarısına) pürüzsüz uçuş
            Vector3 startPos = transform.position;
            Vector3 endPos = targetBin.position + targetBin.up * 0.5f;
            
            float flightDuration = 0.5f; // Yarım saniyede uçar
            float elapsed = 0f;

            while (elapsed < flightDuration)
            {
                elapsed += Time.deltaTime;
                // Daha estetik bir uçuş için Smootherstep (veya düz Lerp)
                float t = elapsed / flightDuration;
                t = t * t * (3f - 2f * t); // Smoothstep formulü
                
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            // 4. Hedefe ulaştı, kutunun içine düşmesi ve puan vermesi için fizikleri geri aç
            _rb.isKinematic = false;
            _col.enabled = true;
            
            // Yerçekimiyle hafifçe kutunun içine düşecek ve BinTrigger OnWasteProcessed tetiklenecek.
            _isFlying = false;
        }
    }
}
