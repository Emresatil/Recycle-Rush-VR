using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

namespace RecycleRush.Managers
{
    public enum TutorialStep
    {
        NotStarted,
        PullLever,
        GrabWaste,
        SortWaste,
        WaitingRetry,
        Completed
    }

    /// <summary>
    /// Oyuna ilk kez girenler için öğretici (Tutorial) akışını yönetir.
    /// Öğretici bitene kadar zamanlayıcı (Timer) çalışmaz ve rastgele çöp üretilmez.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Header("UI Bağlantıları")]
        [Tooltip("Öğretici metinlerinin yazılacağı TV Ekranı yazısı (UIManager'daki Status Text buraya sürüklenebilir)")]
        public TextMeshProUGUI tutorialText;

        [Header("Spawner Ayarları")]
        [Tooltip("Öğretici için kullanılacak örnek atık (Örn: Kağıt)")]
        public GameObject tutorialWastePrefab;
        [Tooltip("Atığın düşeceği başlangıç noktası (WasteSpawner ile aynı olabilir)")]
        public Transform spawnPoint;

        [Header("Görsel Yönlendirme (Pointer)")]
        [Tooltip("Sahnede oyuncunun çekmesi gereken makine kolu objesi")]
        public Transform leverTransform;
        [Tooltip("Öğretici atığının atılması gereken doğru kutu objesi")]
        public Transform targetBinTransform;

        private TutorialStep _currentStep = TutorialStep.NotStarted;
        private GameObject _spawnedWaste;

        private GameObject _pointerObj;
        private Transform _pointerTarget;

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
            
            // Eğer abone kalmışsak temizle
            if (_spawnedWaste != null)
            {
                var grab = _spawnedWaste.GetComponentInChildren<XRGrabInteractable>();
                if (grab != null) grab.selectEntered.RemoveListener(OnWasteGrabbed);
            }
            BinTrigger.OnWasteProcessed -= OnWasteSorted;
        }

        private void Start()
        {
            // Eğer daha önce tamamlandıysa bu scripti komple kapat/yok et
            if (PlayerPrefs.GetInt("TutorialDone", 0) == 1)
            {
                Destroy(this.gameObject);
                return;
            }

            CreatePointer();
        }

        private void CreatePointer()
        {
            // Kod üzerinden dinamik olarak bir 3D Ok (Pointer) objesi oluşturuyoruz
            _pointerObj = new GameObject("TutorialPointer");
            TextMeshPro text = _pointerObj.AddComponent<TextMeshPro>();
            
            text.text = "▼";
            text.color = Color.yellow;
            text.fontSize = 30; // 3D uzayda daha belirgin olması için büyüttük
            text.fontStyle = FontStyles.Bold; // Kalınlaştırdık
            text.alignment = TextAlignmentOptions.Center;
            
            // Başlangıçta gizle
            _pointerObj.SetActive(false);
        }

        private void Update()
        {
            // Oyuncu çöpü bandın sonundan düşürürse veya 3 saniye yerde bırakıp FloorZone tarafından silinirse
            if (_currentStep == TutorialStep.GrabWaste || _currentStep == TutorialStep.SortWaste)
            {
                if (_spawnedWaste == null)
                {
                    _currentStep = TutorialStep.WaitingRetry;
                    BinTrigger.OnWasteProcessed -= OnWasteSorted;
                    
                    UpdateTutorialUI("<color=red>WASTE LOST!</color>\nTRY AGAIN...");
                    StartCoroutine(RetryGrabWaste());
                }
            }

            UpdatePointer();
        }

        private void UpdatePointer()
        {
            if (_pointerObj != null && _pointerTarget != null && _pointerObj.activeSelf)
            {
                // Hedefin pivot noktası altta veya yanda olabileceği için, 
                // Collider'larının boyutlarını (Bounds) hesaplayarak tam görsel merkezini ve tepe noktasını buluyoruz.
                Vector3 centerPos = _pointerTarget.position;
                float topY = centerPos.y + 0.5f;

                Collider[] colliders = _pointerTarget.GetComponentsInChildren<Collider>();
                if (colliders.Length > 0)
                {
                    Bounds combinedBounds = new Bounds();
                    bool hasValidBounds = false;
                    
                    foreach (var col in colliders)
                    {
                        // Trigger olmayan (katı) collider'ları baz al
                        if (!col.isTrigger)
                        {
                            if (!hasValidBounds)
                            {
                                combinedBounds = col.bounds;
                                hasValidBounds = true;
                            }
                            else
                            {
                                combinedBounds.Encapsulate(col.bounds);
                            }
                        }
                    }
                    
                    if (hasValidBounds)
                    {
                        centerPos = combinedBounds.center; // Görselin tam ortası
                        topY = combinedBounds.max.y; // Görselin en tepe noktası
                    }
                }
                
                // Tepenin 40cm üzerinde, daha belirgin zıplayan ok animasyonu
                float bounce = Mathf.Sin(Time.time * 5f) * 0.15f;
                Vector3 finalPos = new Vector3(centerPos.x, topY + 0.4f + bounce, centerPos.z);
                
                _pointerObj.transform.position = finalPos;
                
                // Ok her zaman ana kameraya (oyuncuya) baksın (Billboard)
                if (Camera.main != null)
                {
                    _pointerObj.transform.rotation = Quaternion.LookRotation(_pointerObj.transform.position - Camera.main.transform.position);
                }
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (PlayerPrefs.GetInt("TutorialDone", 0) == 1) return;

            if (state == GameState.MainMenu)
            {
                _currentStep = TutorialStep.PullLever;
                UpdateTutorialUI("<color=yellow>TUTORIAL</color>\nPULL THE LEVER TO START");
                
                SetPointerTarget(leverTransform);
            }
            else if (state == GameState.Tutorial)
            {
                // Kol çekildi ve GameManager bizi Tutorial state'ine soktu
                StartCoroutine(StepGrabWaste());
            }
        }

        private IEnumerator StepGrabWaste()
        {
            _currentStep = TutorialStep.GrabWaste;
            UpdateTutorialUI("<color=yellow>TUTORIAL</color>\nWAITING FOR WASTE...");
            
            // Kol çekildiği için işaretçiyi geçici olarak gizle
            SetPointerTarget(null);

            // Banttan çöpün düşmesi için kısa bir süre bekle
            yield return new WaitForSeconds(1.5f);

            // Çöpü yarat
            if (tutorialWastePrefab != null && spawnPoint != null)
            {
                _spawnedWaste = Instantiate(tutorialWastePrefab, spawnPoint.position, Quaternion.identity);
                
                // Oyuncunun bunu eline alıp almadığını dinle
                XRGrabInteractable grabInteractable = _spawnedWaste.GetComponentInChildren<XRGrabInteractable>();
                if (grabInteractable != null)
                {
                    grabInteractable.selectEntered.AddListener(OnWasteGrabbed);
                }
                
                UpdateTutorialUI("<color=yellow>TUTORIAL</color>\nGRAB THE WASTE FROM CONVEYOR");
                SetPointerTarget(_spawnedWaste.transform);
            }
            else
            {
                Debug.LogError("[TutorialManager] Prefab veya SpawnPoint atanmamış!");
            }
        }

        private void OnWasteGrabbed(SelectEnterEventArgs args)
        {
            // Olayı dinlemeyi bırak
            var grab = _spawnedWaste.GetComponentInChildren<XRGrabInteractable>();
            if (grab != null) grab.selectEntered.RemoveListener(OnWasteGrabbed);

            _currentStep = TutorialStep.SortWaste;
            UpdateTutorialUI("<color=yellow>TUTORIAL</color>\nTHROW IT INTO THE CORRECT BIN");
            SetPointerTarget(targetBinTransform);

            // Doğru kutuya atıp atmadığını dinle
            BinTrigger.OnWasteProcessed += OnWasteSorted;
        }

        private void OnWasteSorted(SortResultData data)
        {
            // Dinlemeyi bırak
            BinTrigger.OnWasteProcessed -= OnWasteSorted;

            if (data.IsCorrect)
            {
                StartCoroutine(StepCompleted());
            }
            else
            {
                // Yanlış kutuya attıysa tekrar baştan çöp ver
                _currentStep = TutorialStep.WaitingRetry;
                UpdateTutorialUI("<color=red>MISTAKE!</color>\nWRONG BIN. TRY AGAIN...");
                SetPointerTarget(null);
                StartCoroutine(RetryGrabWaste());
            }
        }

        private IEnumerator RetryGrabWaste()
        {
            yield return new WaitForSeconds(2.5f);
            StartCoroutine(StepGrabWaste());
        }

        private IEnumerator StepCompleted()
        {
            _currentStep = TutorialStep.Completed;
            UpdateTutorialUI("<color=green>TUTORIAL COMPLETE!</color>\nGET READY...");
            SetPointerTarget(null);

            if (_pointerObj != null) Destroy(_pointerObj);

            PlayerPrefs.SetInt("TutorialDone", 1);
            PlayerPrefs.Save();

            yield return new WaitForSeconds(3f);

            // Asıl oyunu başlat
            if (GameManager.Instance != null)
            {
                // Artık TutorialDone 1 olduğu için StartGame bizi Playing state'ine sokacak
                GameManager.Instance.StartGame();
            }
            
            // İşimiz bittiği için scripti yok edebiliriz
            Destroy(this.gameObject);
        }

        private void UpdateTutorialUI(string message)
        {
            if (tutorialText != null)
            {
                tutorialText.text = message;
            }
        }

        private void SetPointerTarget(Transform target)
        {
            _pointerTarget = target;
            
            if (_pointerObj != null)
            {
                if (target == null)
                {
                    _pointerObj.SetActive(false);
                }
                else
                {
                    _pointerObj.SetActive(true);
                }
            }
        }
    }
}
