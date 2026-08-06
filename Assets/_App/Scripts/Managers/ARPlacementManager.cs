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
        public List<GameObject> objectsToPlace;

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
            // Oyun başlarken Listedeki tüm objeleri görünmez yap (Sakla)
            foreach (var obj in objectsToPlace)
            {
                if (obj != null)
                    obj.SetActive(false);
            }

            if (objectsToPlace.Count > 0)
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

            GameObject currentObj = objectsToPlace[currentIndex];
            if (currentObj == null) return;

            // Sağ kumandadan lazer at
            Ray ray = new Ray(rightController.position, rightController.forward);
            List<ARRaycastHit> hits = new List<ARRaycastHit>();

            if (raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                currentObj.transform.position = hitPose.position;

                // Oyuncuya (Kameraya) baksın
                Vector3 lookPos = Camera.main.transform.position;
                lookPos.y = currentObj.transform.position.y;
                currentObj.transform.LookAt(lookPos);
            }
        }

        private void OnTriggerPressed(InputAction.CallbackContext context)
        {
            if (isSetupComplete) return;

            GameObject currentObj = objectsToPlace[currentIndex];

            // 1. Objeyi olduğu yere sabitle (Anchor)
            anchorManager.AnchorObject(currentObj);

            // 2. Sıradaki objeye geç
            currentIndex++;

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
