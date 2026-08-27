using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using RecycleRush.Interaction;

namespace RecycleRush.Tutorial
{
    public enum TutorialStep
    {
        None = 0,
        Placement = 1,          // Adım 1: Kutuları ve İstasyonu Tanıma / Yerleştirme
        GrabAndDrop = 2,        // Adım 2: İlk Çöpü Tut ve Doğru Kutuya At
        GravityPull = 3,        // Adım 3: Uzaktaki Çöpü Ray / Yerçekimi ile Çek
        CompositeWaste = 4,     // Adım 4: Yapışık Kompozit Çöpleri İki Elle Ayır
        Completed = 5           // Adım 5: Eğitim Başarıyla Tamamlandı!
    }

    /// <summary>
    /// AR Passthrough ortamında oyuncuyu adım adım eğiten ve aksiyonları zorunlu kılan
    /// Holografik 3D Tutorial Durum Makinesi Yöneticisi.
    /// </summary>
    public class ARHoloTutorialManager : MonoBehaviour
    {
        public static ARHoloTutorialManager Instance { get; private set; }

        [Header("Örnek Atık Prefabları (Opsiyonel - Atanmazsa sahneden otomatik bulunur)")]
        [Tooltip("Adım 2 için kullanılacak basit atık (Örn: Kağıt Bardak / Gazete)")]
        public GameObject basicWastePrefab;
        [Tooltip("Adım 3 için kullanılacak uzaktan çekilecek atık (Örn: Şişe)")]
        public float distantSpawnDistance = 2.4f;
        [Tooltip("Adım 4 için kullanılacak kompozit/yapışık atık prefabı")]
        public GameObject compositeWastePrefab;

        [Header("Geri Dönüşüm Kutuları (Atanmazsa otomatik bulunur)")]
        public Transform paperBinTransform;
        public Transform plasticBinTransform;
        public Transform glassBinTransform;
        public Transform metalBinTransform;

        public TutorialStep CurrentStep { get; private set; } = TutorialStep.None;

        public static event Action<TutorialStep> OnTutorialStepStarted;
        public static event Action<TutorialStep> OnTutorialStepCompleted;

        private ARHoloPointer _wastePointer;
        private ARHoloPointer _binPointer;
        private ARHoloStepCard _stepCard;
        private ARHoloHandGuide _handGuide;

        private GameObject _activeTutorialWaste;
        private Coroutine _stepRoutine;
        private bool _isActionInProgress = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeHoloComponents();
        }

        private void Start()
        {
            AutoFindBins();

            // Eğer oyuncu eğitimi daha önce tamamlamışsa sistemi pasif yap
            if (PlayerPrefs.GetInt("TutorialDone", 0) == 1)
            {
                CurrentStep = TutorialStep.Completed;
                CleanupVisuals();
                return;
            }

            StartTutorial();
        }

        private void InitializeHoloComponents()
        {
            // 3D Pointer'lar
            GameObject wastePtrObj = new GameObject("HoloWastePointer");
            wastePtrObj.transform.SetParent(transform);
            _wastePointer = wastePtrObj.AddComponent<ARHoloPointer>();

            GameObject binPtrObj = new GameObject("HoloBinPointer");
            binPtrObj.transform.SetParent(transform);
            _binPointer = binPtrObj.AddComponent<ARHoloPointer>();

            // Floating HUD Card
            GameObject cardObj = new GameObject("HoloStepCard");
            cardObj.transform.SetParent(transform);
            _stepCard = cardObj.AddComponent<ARHoloStepCard>();

            // Gesture Guide
            GameObject guideObj = new GameObject("HoloHandGuide");
            guideObj.transform.SetParent(transform);
            _handGuide = guideObj.AddComponent<ARHoloHandGuide>();
        }

        private void AutoFindBins()
        {
            if (paperBinTransform == null)
            {
                var paper = GameObject.Find("TrashbinBlue") ?? GameObject.Find("Paper_Bin") ?? GameObject.Find("Bin_Paper");
                if (paper != null) paperBinTransform = paper.transform;
            }
            if (plasticBinTransform == null)
            {
                var plastic = GameObject.Find("TrashbinYellow") ?? GameObject.Find("Plastic_Bin") ?? GameObject.Find("Bin_Plastic");
                if (plastic != null) plasticBinTransform = plastic.transform;
            }
            if (glassBinTransform == null)
            {
                var glass = GameObject.Find("TrashbinGreen") ?? GameObject.Find("Glass_Bin") ?? GameObject.Find("Bin_Glass");
                if (glass != null) glassBinTransform = glass.transform;
            }
            if (metalBinTransform == null)
            {
                var metal = GameObject.Find("TrashbinRed") ?? GameObject.Find("Metal_Bin") ?? GameObject.Find("Bin_Metal");
                if (metal != null) metalBinTransform = metal.transform;
            }
        }

        public void StartTutorial()
        {
            if (CurrentStep == TutorialStep.Completed) return;

            Debug.Log("<color=cyan>[ARHoloTutorial]</color> Öğretici Başlatılıyor...");
            SetStep(TutorialStep.Placement);
        }

        public void SetStep(TutorialStep step)
        {
            if (_stepRoutine != null)
            {
                StopCoroutine(_stepRoutine);
            }

            CurrentStep = step;
            OnTutorialStepStarted?.Invoke(CurrentStep);

            switch (CurrentStep)
            {
                case TutorialStep.Placement:
                    _stepRoutine = StartCoroutine(StepPlacementRoutine());
                    break;
                case TutorialStep.GrabAndDrop:
                    _stepRoutine = StartCoroutine(StepGrabAndDropRoutine());
                    break;
                case TutorialStep.GravityPull:
                    _stepRoutine = StartCoroutine(StepGravityPullRoutine());
                    break;
                case TutorialStep.CompositeWaste:
                    _stepRoutine = StartCoroutine(StepCompositeWasteRoutine());
                    break;
                case TutorialStep.Completed:
                    _stepRoutine = StartCoroutine(StepCompletedRoutine());
                    break;
            }
        }

        #region Step 1: Placement & Station Setup

        private IEnumerator StepPlacementRoutine()
        {
            _stepCard.DisplayStep(
                "ADIM 1: OYUN ALANI",
                "Geri dönüşüm kutularına ve istasyona yaklaşın.",
                "[ ● ○ ○ ○ ]"
            );

            // İstasyon veya kutuların üzerinde parıldayan hologram ok göster
            Transform target = paperBinTransform != null ? paperBinTransform : transform;
            _binPointer.SetTarget(target, Color.cyan, "★", 0.4f);
            _handGuide.Hide();

            yield return new WaitForSeconds(3.5f);

            _binPointer.SetTarget(null);
            _stepCard.ShowSuccess("İSTASYON HAZIR ✔");

            yield return new WaitForSeconds(1.5f);
            SetStep(TutorialStep.GrabAndDrop);
        }

        #endregion

        #region Step 2: Grab and Drop / Sort

        private IEnumerator StepGrabAndDropRoutine()
        {
            _stepCard.DisplayStep(
                "ADIM 2: TUT VE AT",
                "Önünüzdeki atığı tutup doğru geri dönüşüm kutusuna atın!",
                "[ ● ● ○ ○ ]"
            );

            yield return SpawnTutorialWaste(Vector3.forward * 0.8f + Vector3.up * 0.2f);

            if (_activeTutorialWaste != null)
            {
                _wastePointer.SetTarget(_activeTutorialWaste.transform, Color.yellow, "▼");
                _handGuide.ShowGesture(GuideGestureType.SingleGrab, _activeTutorialWaste.transform);

                // Tutulma anını dinle
                var grab = _activeTutorialWaste.GetComponentInChildren<XRGrabInteractable>();
                if (grab != null)
                {
                    grab.selectEntered.AddListener(OnStep2WasteGrabbed);
                }
            }

            // Kutuya atılmasını bekle
            BinTrigger.OnWasteProcessed += OnStep2WasteSorted;
        }

        private void OnStep2WasteGrabbed(SelectEnterEventArgs args)
        {
            _wastePointer.SetTarget(null);
            _handGuide.Hide();

            // Hedef kutu üzerine hologram oku çevir
            Transform targetBin = paperBinTransform != null ? paperBinTransform : transform;
            _binPointer.SetTarget(targetBin, Color.green, "⬇", 0.3f);
        }

        private void OnStep2WasteSorted(SortResultData data)
        {
            if (CurrentStep != TutorialStep.GrabAndDrop) return;

            if (data.IsCorrect)
            {
                BinTrigger.OnWasteProcessed -= OnStep2WasteSorted;
                _binPointer.SetTarget(null);
                _stepCard.ShowSuccess("MÜKEMMEL ATTIM! ✔");

                StartCoroutine(AdvanceAfterDelay(TutorialStep.GravityPull));
            }
            else
            {
                // Yanlış kutuya attıysa tekrar dene
                _stepCard.DisplayStep("HATALI KUTU!", "Yanlış kutuya attınız. Tekrar deneyin!", "[ ● ● ○ ○ ]");
                StartCoroutine(RetryStep(StepGrabAndDropRoutine()));
            }
        }

        #endregion

        #region Step 3: Remote Gravity Pull

        private IEnumerator StepGravityPullRoutine()
        {
            _stepCard.DisplayStep(
                "ADIM 3: UZAKTAN ÇEKME (GRAVITY PULL)",
                "Uzakta duran atığa elinizi doğrultup GRIP ile kendinize çekin!",
                "[ ● ● ● ○ ]"
            );

            yield return SpawnTutorialWaste(Vector3.forward * distantSpawnDistance + Vector3.up * 0.1f);

            if (_activeTutorialWaste != null)
            {
                _wastePointer.SetTarget(_activeTutorialWaste.transform, Color.magenta, "⚡");
                _handGuide.ShowGesture(GuideGestureType.GravityPull, _activeTutorialWaste.transform);

                var grab = _activeTutorialWaste.GetComponentInChildren<XRGrabInteractable>();
                if (grab != null)
                {
                    grab.selectEntered.AddListener(OnStep3WasteGrabbed);
                }
            }

            BinTrigger.OnWasteProcessed += OnStep3WasteSorted;
        }

        private void OnStep3WasteGrabbed(SelectEnterEventArgs args)
        {
            _wastePointer.SetTarget(null);
            _handGuide.Hide();

            Transform targetBin = plasticBinTransform != null ? plasticBinTransform : paperBinTransform;
            _binPointer.SetTarget(targetBin, Color.yellow, "⬇", 0.3f);
        }

        private void OnStep3WasteSorted(SortResultData data)
        {
            if (CurrentStep != TutorialStep.GravityPull) return;

            if (data.IsCorrect)
            {
                BinTrigger.OnWasteProcessed -= OnStep3WasteSorted;
                _binPointer.SetTarget(null);
                _stepCard.ShowSuccess("HARİKA ÇEKİŞ! ✔");

                StartCoroutine(AdvanceAfterDelay(TutorialStep.CompositeWaste));
            }
            else
            {
                _stepCard.DisplayStep("TEKRAR DENE!", "Doğru kutuya atın!", "[ ● ● ● ○ ]");
                StartCoroutine(RetryStep(StepGravityPullRoutine()));
            }
        }

        #endregion

        #region Step 4: Composite Waste Separation

        private IEnumerator StepCompositeWasteRoutine()
        {
            _stepCard.DisplayStep(
                "ADIM 4: YAPIŞIK ÇÖPLERİ AYIRMA",
                "İki elinizle iki parçayı tutup zıt yönlere çekerek ayırın!",
                "[ ● ● ● ● ]"
            );

            yield return SpawnTutorialWaste(Vector3.forward * 0.9f + Vector3.up * 0.2f, isComposite: true);

            if (_activeTutorialWaste != null)
            {
                _wastePointer.SetTarget(_activeTutorialWaste.transform, Color.red, "✂");
                _handGuide.ShowGesture(GuideGestureType.BimanualTear, _activeTutorialWaste.transform);
            }

            // Ayrılmayı dinle (WasteGlue veya parçaların ayrılması)
            float timeout = 25f;
            float timer = 0f;
            bool separated = false;

            while (timer < timeout && !separated)
            {
                timer += Time.deltaTime;
                if (_activeTutorialWaste == null) break;

                var glue = _activeTutorialWaste.GetComponentInChildren<WasteGlue>();
                if (glue == null || !glue.IsActive)
                {
                    separated = true;
                }
                yield return null;
            }

            _wastePointer.SetTarget(null);
            _handGuide.Hide();
            _stepCard.ShowSuccess("BAŞARIYLA AYRILDI! ✔");

            yield return new WaitForSeconds(2.5f);
            SetStep(TutorialStep.Completed);
        }

        #endregion

        #region Step 5: Completed & Transition

        private IEnumerator StepCompletedRoutine()
        {
            _stepCard.DisplayStep(
                "TEBRİKLER!",
                "Tüm mekanikleri öğrendiniz! Geri dönüşüm başlıyor...",
                "[ ✔ ✔ ✔ ✔ ]"
            );

            CleanupVisuals();

            // Kalıcı olarak tamamlandı işaretle
            PlayerPrefs.SetInt("TutorialDone", 1);
            PlayerPrefs.Save();
            Debug.Log("<color=green>[ARHoloTutorial]</color> Öğretici Tamamlandı!");

            yield return new WaitForSeconds(3.5f);

            if (_stepCard != null) _stepCard.gameObject.SetActive(false);

            // Ana oyunu başlat
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }

        #endregion

        #region Helpers & Spawning

        private IEnumerator SpawnTutorialWaste(Vector3 relativeOffset, bool isComposite = false)
        {
            if (_activeTutorialWaste != null)
            {
                Destroy(_activeTutorialWaste);
            }

            Camera cam = Camera.main;
            Vector3 spawnPos = transform.position + relativeOffset;
            if (cam != null)
            {
                Vector3 forward = cam.transform.forward;
                forward.y = 0;
                forward.Normalize();
                spawnPos = cam.transform.position + forward * relativeOffset.z + Vector3.up * relativeOffset.y;
            }

            GameObject prefabToSpawn = isComposite ? compositeWastePrefab : basicWastePrefab;

            // Prefab atanmadıysa ObjectPool veya dinamik fallback
            if (prefabToSpawn == null && global::ObjectPoolManager.Instance != null)
            {
                _activeTutorialWaste = global::ObjectPoolManager.Instance.SpawnFromPool("TutorialWaste", null, spawnPos, Quaternion.identity);
            }
            else if (prefabToSpawn != null)
            {
                _activeTutorialWaste = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            }

            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator RetryStep(IEnumerator routine)
        {
            yield return new WaitForSeconds(2f);
            if (_stepRoutine != null) StopCoroutine(_stepRoutine);
            _stepRoutine = StartCoroutine(routine);
        }

        private IEnumerator AdvanceAfterDelay(TutorialStep nextStep)
        {
            yield return new WaitForSeconds(2f);
            SetStep(nextStep);
        }

        private void CleanupVisuals()
        {
            if (_wastePointer != null) _wastePointer.gameObject.SetActive(false);
            if (_binPointer != null) _binPointer.gameObject.SetActive(false);
            if (_handGuide != null) _handGuide.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            BinTrigger.OnWasteProcessed -= OnStep2WasteSorted;
            BinTrigger.OnWasteProcessed -= OnStep3WasteSorted;
        }

        #endregion
    }
}
