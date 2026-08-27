using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;

namespace RecycleRush.Managers
{
    public class ARPlacementManager : MonoBehaviour
    {
        [Header("AR Systems")]
        public SpatialAnchorManager anchorManager;
        public ARRaycastManager raycastManager;

        [Header("Input & Tracking")]
        public Transform rightController; 
        public InputActionReference triggerAction; 

        [Header("Sahnede Zaten Var Olan Objeler")]
        [Tooltip("Sırayla: Bant, Kağıt, Plastik, Cam...")]
        public List<GameObject> objectsToPlace = new List<GameObject>();

        private int currentIndex = 0;
        private bool isSetupComplete = false;

        private void OnEnable()
        {
            if (triggerAction != null)
            {
                triggerAction.action.Enable();
                triggerAction.action.performed += OnTriggerPressed;
            }
        }

        private void OnDisable()
        {
            if (triggerAction != null)
            {
                triggerAction.action.performed -= OnTriggerPressed;
                triggerAction.action.Disable();
            }
        }

        private void Start()
        {
            if (objectsToPlace == null) objectsToPlace = new List<GameObject>();

            // Oyun başlarken Listedeki tüm objeleri görünmez yap (Sakla)
            foreach (var obj in objectsToPlace)
            {
                if (obj != null)
                    obj.SetActive(false);
            }

            // İlk GEÇERLİ objeye kadar ilerle
            while (currentIndex < objectsToPlace.Count && objectsToPlace[currentIndex] == null)
            {
                currentIndex++;
            }

            if (currentIndex < objectsToPlace.Count)
            {
                // Sadece sıradaki objeyi görünür yap
                objectsToPlace[currentIndex].SetActive(true);
            }
            else
            {
                isSetupComplete = true;
            }
        }

        private void Update()
        {
            if (isSetupComplete || rightController == null || raycastManager == null) return;
            if (objectsToPlace == null || currentIndex >= objectsToPlace.Count) return;

            GameObject currentObj = objectsToPlace[currentIndex];
            if (currentObj == null) return;

            // Sağ kumandadan lazer at
            Ray ray = new Ray(rightController.position, rightController.forward);
            List<ARRaycastHit> hits = new List<ARRaycastHit>();

            if (raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                currentObj.transform.position = hitPose.position;
                
                // Rotasyonu da yüzeye uydur
                currentObj.transform.rotation = hitPose.rotation;
            }
        }

        private void OnTriggerPressed(InputAction.CallbackContext context)
        {
            if (isSetupComplete) return;
            if (objectsToPlace == null || currentIndex >= objectsToPlace.Count) return;

            GameObject currentObj = objectsToPlace[currentIndex];
            if (currentObj == null) return;

            // 1. Objeyi olduğu yere sabitle (Anchor)
            if (anchorManager != null)
            {
                anchorManager.AnchorObject(currentObj);
            }

            // 2. Sıradaki objeye geç (Geçerli bir obje bulana kadar)
            do
            {
                currentIndex++;
            } while (currentIndex < objectsToPlace.Count && objectsToPlace[currentIndex] == null);

            if (currentIndex < objectsToPlace.Count)
            {
                // Sıradaki objeyi aktif et
                objectsToPlace[currentIndex].SetActive(true);
            }
            else
            {
                isSetupComplete = true;
                Debug.Log("[ARPlacementManager] Tüm kurulum tamamlandı. Oyun Başlayabilir!");
            }
        }
    }
}
