using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using RecycleRush.Core.Packages;
using System.Collections.Generic;
using RecycleRush.Core;
using RecycleRush.Managers;

namespace RecycleRush.Interaction
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class WastePackageController : MonoBehaviour
    {
        [Header("Paket İçeriği")]
        [SerializeField, Tooltip("Paket açıldığında çıkacak 3 farklı çöp prefabbı (ObjectPool üzerinden çekilecek).")] 
        private GameObject[] _containedWastePrefabs;

        [Header("Görsel ve Ses")]
        [SerializeField] private GameObject _explosionVfxPrefab;
        [SerializeField] private AudioClip _openSound;
        
        [Header("Paket Modeli (Kapatılacak)")]
        [SerializeField] private MeshRenderer _packageRenderer;
        [SerializeField] private Collider _packageCollider;

        private XRGrabInteractable _grabInteractable;
        private bool _isOpened = false;

        private void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();
            
            if (_packageRenderer == null) _packageRenderer = GetComponentInChildren<MeshRenderer>();
            if (_packageCollider == null) _packageCollider = GetComponentInChildren<Collider>();
        }

        private void OnEnable()
        {
            _isOpened = false;
            if (_packageRenderer != null) _packageRenderer.enabled = true;
            if (_packageCollider != null) _packageCollider.enabled = true;
            
            _grabInteractable.selectEntered.AddListener(OnPackageGrabbed);
        }

        private void OnDisable()
        {
            _grabInteractable.selectEntered.RemoveListener(OnPackageGrabbed);
        }

        private void OnPackageGrabbed(SelectEnterEventArgs args)
        {
            if (_isOpened) return;
            _isOpened = true;

            // Haptic (Titreşim) yolla
            var interactor = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor;
            if (interactor == null) 
            {
                var baseInteractor = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;
                // Eğer eski XRI sürümü kullanılıyorsa SendHapticImpulse destekleyen interactor bulunmaya çalışılır
            }
            else
            {
                interactor.SendHapticImpulse(0.8f, 0.2f);
            }

            // Patlama ve Ses
            if (_explosionVfxPrefab != null)
            {
                Destroy(Instantiate(_explosionVfxPrefab, transform.position, Quaternion.identity), 3f);
            }
            if (_openSound != null)
            {
                AudioSource.PlayClipAtPoint(_openSound, transform.position);
            }

            // Kendi modelini gizle ve fırlat
            if (_packageRenderer != null) _packageRenderer.enabled = false;
            if (_packageCollider != null) _packageCollider.enabled = false;
            
            // XR'dan bırak (Elinde görünmez obje kalmasın)
            if (_grabInteractable.interactionManager != null)
            {
                _grabInteractable.interactionManager.SelectCancel(args.interactorObject, (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)_grabInteractable);
            }

            // Çöpleri Spawn Et
            SpawnContents();

            // Kutu Objesini yok et / havuza yolla
            Destroy(gameObject, 0.5f); 
        }

        private void SpawnContents()
        {
            if (_containedWastePrefabs == null || _containedWastePrefabs.Length == 0) return;

            List<GameObject> spawnedList = new List<GameObject>();

            for (int i = 0; i < 3; i++)
            {
                GameObject prefabToSpawn = _containedWastePrefabs[i % _containedWastePrefabs.Length];
                
                if (ObjectPoolManager.Instance != null)
                {
                    GameObject spawnedWaste = ObjectPoolManager.Instance.SpawnFromPool(prefabToSpawn.tag, prefabToSpawn, transform.position + new Vector3(0, 0.2f, 0), Quaternion.identity);
                    if (spawnedWaste != null)
                    {
                        Rigidbody rb = spawnedWaste.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            // Çöpleri hafifçe yukarı ve etrafa doğru patlat
                            Vector3 randomDir = new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(-0.5f, 0.5f)).normalized;
                            rb.AddForce(randomDir * 2.5f, ForceMode.Impulse);
                            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
                        }
                        spawnedList.Add(spawnedWaste);
                    }
                }
            }

            // Hakeme (Tracker) bildir
            if (PackageChallengeTracker.Instance != null)
            {
                PackageChallengeTracker.Instance.RegisterNewPackage(spawnedList);
            }
        }
    }
}