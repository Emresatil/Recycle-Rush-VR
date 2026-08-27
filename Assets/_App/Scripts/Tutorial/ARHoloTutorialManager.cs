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
    /// Tüm spawn mesafeleri, bileşenler ve süreler Inspector üzerinden ayarlanabilir.
    /// </summary>
    public class ARHoloTutorialManager : MonoBehaviour
    {
        public static ARHoloTutorialManager Instance { get; private set; }

        [Header("🎛️ Alt Bileşenler (Otomatik oluşturulur veya atanabilir)")]
        public ARHoloStepCard stepCard;
        public ARHoloPointer wastePointer;
        public ARHoloPointer binPointer;
        public ARHoloHandGuide handGuide;

        [Header("📦 Örnek Atık Prefabları (Opsiyonel)")]
        [Tooltip("Adım 2 için kullanılacak basit atık (Örn: Kağıt Bardak / Gazete)")]
        public GameObject basicWastePrefab;
        [Tooltip("Adım 4 için kullanılacak kompozit/yapışık atık prefabı")]
        public GameObject compositeWastePrefab;

        [Header("📍 Adım Spawn Konumları (Kameraya Göre Bağıl)")]
        [Tooltip("Adım 2: Önüne çöp düşme mesafesi (İleri, Yukarı, Sağa)")]
        public Vector3 step2SpawnOffset = new Vector3(0f, 0.1f, 0.9f);

        [Tooltip("Adım 3: Uzaktan çekilecek çöpün mesafesi")]
        public Vector3 step3SpawnOffset = new Vector3(0f, 0.1f, 2.3f);

        [Tooltip("Adım 4: Kompozit çöpün spawn mesafesi")]
        public Vector3 step4SpawnOffset = new Vector3(0f, 0.1f, 0.9f);

        [Header("🗑️ Geri Dönüşüm Kutuları (Atanmazsa otomatik bulunur)")]
        public Transform paperBinTransform;
        public Transform plasticBinTransform;
        public Transform glassBinTransform;
        public Transform metalBinTransform;

        public TutorialStep CurrentStep { get; private set; } = TutorialStep.None;

        public static event Action<TutorialStep> OnTutorialStepStarted;
        public static event Action<TutorialStep> OnTutorialStepCompleted;

        private GameObject _activeTutorialWaste;
        private Coroutine _stepRoutine;

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
            if (wastePointer == null)
            {
                GameObject wastePtrObj = new GameObject("HoloWastePointer");
                wastePtrObj.transform.SetParent(transform);
                wastePointer = wastePtrObj.AddComponent<ARHoloPointer>();
            }

            if (binPointer == null)
            {
                GameObject binPtrObj = new GameObject("HoloBinPointer");
                binPtrObj.transform.SetParent(transform);
                binPointer = binPtrObj.AddComponent<ARHoloPointer>();
            }

            if (stepCard == null)
            {
                GameObject cardObj = new GameObject("HoloStepCard");
                cardObj.transform.SetParent(transform);
                stepCard = cardObj.AddComponent<ARHoloStepCard>();
            }

            if (handGuide == null)
            {
                GameObject guideObj = new GameObject("HoloHandGuide");
                guideObj.transform.SetParent(transform);
                handGuide = guideObj.AddComponent<ARHoloHandGuide>();
            }
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

            // GameManager'ı Tutorial state'ine al
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeState(GameState.Tutorial);
            }

            // LevelSelectionBoard'u gizle
            var levelBoard = GameObject.Find("LevelSelectionBoard") ?? GameObject.Find("LevelBoard");
            if (levelBoard != null) levelBoard.SetActive(false);

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
            if (stepCard != null)
            {
                stepCard.DisplayStep(
                    "ADIM 1: OYUN ALANI",
                    "Geri dönüşüm kutularına ve istasyona yaklaşın.",
                    "[ ● ○ ○ ○ ]"
                );
            }

            Transform target = paperBinTransform != null ? paperBinTransform : transform;
            if (binPointer != null) binPointer.SetTarget(target, Color.cyan, "★", 0.4f);
            if (handGuide != null) handGuide.Hide();

            yield return new WaitForSeconds(3.5f);

            if (binPointer != null) binPointer.SetTarget(null);
            if (stepCard != null) stepCard.ShowSuccess("İSTASYON HAZIR ✔");

            yield return new WaitForSeconds(1.5f);
            SetStep(TutorialStep.GrabAndDrop);
        }

        #endregion

        #region Step 2: Grab and Drop / Sort

        private IEnumerator StepGrabAndDropRoutine()
        {
            if (stepCard != null)
            {
                stepCard.DisplayStep(
                    "ADIM 2: TUT VE AT",
                    "Önünüzdeki atığı tutup doğru geri dönüşüm kutusuna atın!",
                    "[ ● ● ○ ○ ]"
                );
            }

            yield return SpawnTutorialWaste(step2SpawnOffset);

            if (_activeTutorialWaste != null)
            {
                if (wastePointer != null) wastePointer.SetTarget(_activeTutorialWaste.transform, Color.yellow, "▼");
                if (handGuide != null) handGuide.ShowGesture(GuideGestureType.SingleGrab, _activeTutorialWaste.transform);

                var grab = _activeTutorialWaste.GetComponentInChildren<XRGrabInteractable>();
                if (grab != null)
                {
                    grab.selectEntered.AddListener(OnStep2WasteGrabbed);
                }
            }

            BinTrigger.OnWasteProcessed += OnStep2WasteSorted;
        }

        private Transform GetMatchingBinForWaste(GameObject waste)
        {
            if (waste == null) return paperBinTransform != null ? paperBinTransform : transform;
            string t = waste.tag.ToLower();
            string n = waste.name.ToLower();

            if (t.Contains("paper") || n.Contains("paper") || n.Contains("gazete") || n.Contains("bardak"))
                return paperBinTransform != null ? paperBinTransform : transform;

            if (t.Contains("plastic") || n.Contains("plastic") || n.Contains("pet") || n.Contains("sise"))
                return plasticBinTransform != null ? plasticBinTransform : transform;

            if (t.Contains("glass") || n.Contains("glass") || n.Contains("cam"))
                return glassBinTransform != null ? glassBinTransform : transform;

            if (t.Contains("metal") || n.Contains("metal") || n.Contains("teneke") || n.Contains("can"))
                return metalBinTransform != null ? metalBinTransform : transform;

            return paperBinTransform != null ? paperBinTransform : transform;
        }

        private void OnStep2WasteGrabbed(SelectEnterEventArgs args)
        {
            if (wastePointer != null) wastePointer.SetTarget(null);
            if (handGuide != null) handGuide.Hide();

            Transform targetBin = GetMatchingBinForWaste(_activeTutorialWaste);
            if (binPointer != null) binPointer.SetTarget(targetBin, Color.green, "⬇", 0.3f);
        }

        private void OnStep2WasteSorted(SortResultData data)
        {
            if (CurrentStep != TutorialStep.GrabAndDrop) return;

            if (data.IsCorrect)
            {
                BinTrigger.OnWasteProcessed -= OnStep2WasteSorted;
                if (binPointer != null) binPointer.SetTarget(null);
                if (stepCard != null) stepCard.ShowSuccess("MÜKEMMEL ATIM! ✔");

                StartCoroutine(AdvanceAfterDelay(TutorialStep.GravityPull));
            }
            else
            {
                if (stepCard != null) stepCard.DisplayStep("HATALI KUTU!", "Yanlış kutuya attınız. Tekrar deneyin!", "[ ● ● ○ ○ ]");
                StartCoroutine(RetryStep(StepGrabAndDropRoutine()));
            }
        }

        #endregion

        #region Step 3: Remote Gravity Pull

        private IEnumerator StepGravityPullRoutine()
        {
            if (stepCard != null)
            {
                stepCard.DisplayStep(
                    "ADIM 3: UZAKTAN ÇEKME (GRAVITY PULL)",
                    "Uzakta duran atığa elinizi doğrultup GRIP ile kendinize çekin!",
                    "[ ● ● ● ○ ]"
                );
            }

            yield return SpawnTutorialWaste(step3SpawnOffset, isComposite: false, floatInAir: true);

            if (_activeTutorialWaste != null)
            {
                if (wastePointer != null) wastePointer.SetTarget(_activeTutorialWaste.transform, Color.magenta, "⚡");
                if (handGuide != null) handGuide.ShowGesture(GuideGestureType.GravityPull, _activeTutorialWaste.transform);

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
            if (_activeTutorialWaste != null)
            {
                var rb = _activeTutorialWaste.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }

                var grab = _activeTutorialWaste.GetComponentInChildren<XRGrabInteractable>();
                if (grab != null)
                {
                    grab.selectEntered.RemoveListener(OnStep3WasteGrabbed);
                }
            }

            BinTrigger.OnWasteProcessed -= OnStep3WasteSorted;

            if (wastePointer != null) wastePointer.SetTarget(null);
            if (handGuide != null) handGuide.Hide();
            if (binPointer != null) binPointer.SetTarget(null);

            // Uzaktan çekme mekaniği başarıyla yapıldı! Doğrudan adımı tamamla
            if (stepCard != null) stepCard.ShowSuccess("HARİKA ÇEKİŞ! ✔");

            StartCoroutine(AdvanceAfterDelay(TutorialStep.CompositeWaste));
        }

        private void OnStep3WasteSorted(SortResultData data)
        {
            // Eğer oyuncu geçiş olmadan önce kutuya atarsa da güvenli tamamlama
            if (CurrentStep != TutorialStep.GravityPull) return;

            BinTrigger.OnWasteProcessed -= OnStep3WasteSorted;
            if (binPointer != null) binPointer.SetTarget(null);
            if (stepCard != null) stepCard.ShowSuccess("HARİKA ÇEKİŞ! ✔");

            StartCoroutine(AdvanceAfterDelay(TutorialStep.CompositeWaste));
        }

        #endregion

        #region Step 4: Composite Waste Separation

        private IEnumerator StepCompositeWasteRoutine()
        {
            if (stepCard != null)
            {
                stepCard.DisplayStep(
                    "ADIM 4: YAPIŞIK ÇÖPLERİ AYIRMA",
                    "İki elinizle iki parçayı tutup zıt yönlere çekerek ayırın!",
                    "[ ● ● ● ● ]"
                );
            }

            yield return SpawnTutorialWaste(step4SpawnOffset, isComposite: true, floatInAir: false);

            if (_activeTutorialWaste != null)
            {
                if (wastePointer != null) wastePointer.SetTarget(_activeTutorialWaste.transform, Color.red, "✂");
                if (handGuide != null) handGuide.ShowGesture(GuideGestureType.BimanualTear, _activeTutorialWaste.transform);
            }

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

            if (wastePointer != null) wastePointer.SetTarget(null);
            if (handGuide != null) handGuide.Hide();
            if (stepCard != null) stepCard.ShowSuccess("BAŞARIYLA AYRILDI! ✔");

            yield return new WaitForSeconds(2.5f);
            SetStep(TutorialStep.Completed);
        }

        #endregion

        #region Step 5: Completed & Transition

        private IEnumerator StepCompletedRoutine()
        {
            if (stepCard != null)
            {
                stepCard.DisplayStep(
                    "TEBRİKLER!",
                    "Tüm mekanikleri öğrendiniz! Ana menü açılıyor...",
                    "[ ✔ ✔ ✔ ✔ ]"
                );
            }

            CleanupVisuals();

            PlayerPrefs.SetInt("TutorialDone", 1);
            PlayerPrefs.Save();
            Debug.Log("<color=green>[ARHoloTutorial]</color> Öğretici Tamamlandı!");

            yield return new WaitForSeconds(3.5f);

            if (stepCard != null) stepCard.gameObject.SetActive(false);

            // LevelSelectionBoard'u yeniden aktif et
            var levelBoard = GameObject.Find("LevelSelectionBoard") ?? GameObject.Find("LevelBoard");
            if (levelBoard != null) levelBoard.SetActive(true);

            if (GameManager.Instance != null)
            {
                // Ana menüye güvenle geç
                GameManager.Instance.ChangeState(GameState.MainMenu);
            }
        }

        #endregion

        #region Helpers & Spawning

        private IEnumerator SpawnTutorialWaste(Vector3 relativeOffset, bool isComposite = false, bool floatInAir = false)
        {
            if (_activeTutorialWaste != null)
            {
                if (global::ObjectPoolManager.Instance != null && !string.IsNullOrEmpty(_activeTutorialWaste.tag) && _activeTutorialWaste.tag != "Untagged")
                {
                    global::ObjectPoolManager.Instance.ReturnToPool(_activeTutorialWaste);
                }
                else
                {
                    Destroy(_activeTutorialWaste);
                }
                _activeTutorialWaste = null;
            }

            Camera cam = Camera.main;
            Vector3 spawnPos = transform.position + relativeOffset;
            if (cam != null)
            {
                Vector3 forward = cam.transform.forward;
                forward.y = 0;
                if (forward.sqrMagnitude < 0.001f) forward = cam.transform.forward;
                forward.Normalize();

                Vector3 right = cam.transform.right;
                right.y = 0;
                right.Normalize();

                spawnPos = cam.transform.position + forward * relativeOffset.z + Vector3.up * relativeOffset.y + right * relativeOffset.x;
            }

            // 1) Prefab'ı belirle veya sahnedeki Spawner'dan otomatik al
            GameObject prefabToSpawn = isComposite ? compositeWastePrefab : basicWastePrefab;

            if (prefabToSpawn == null)
            {
                WasteSpawner spawner = FindFirstObjectByType<WasteSpawner>();
                if (spawner != null && spawner.wastePrefabs != null && spawner.wastePrefabs.Length > 0)
                {
                    prefabToSpawn = spawner.wastePrefabs[0];
                }
            }

            if (prefabToSpawn != null)
            {
                if (global::ObjectPoolManager.Instance != null)
                {
                    string tagToUse = !string.IsNullOrEmpty(prefabToSpawn.tag) && prefabToSpawn.tag != "Untagged" ? prefabToSpawn.tag : prefabToSpawn.name;
                    _activeTutorialWaste = global::ObjectPoolManager.Instance.SpawnFromPool(tagToUse, prefabToSpawn, spawnPos, Quaternion.identity);
                }

                if (_activeTutorialWaste == null)
                {
                    _activeTutorialWaste = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                }

                if (_activeTutorialWaste != null)
                {
                    _activeTutorialWaste.transform.position = spawnPos;
                    _activeTutorialWaste.SetActive(true);

                    var rb = _activeTutorialWaste.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.isKinematic = floatInAir;
                        rb.useGravity = !floatInAir;
                    }

                    // Composite Çöp Fallback: Eğer composite prefab atanmamışsa 2. parça üret ve bağla
                    if (isComposite && compositeWastePrefab == null)
                    {
                        SetupCompositeFallback(_activeTutorialWaste, spawnPos);
                    }

                    Debug.Log($"<color=cyan>[ARHoloTutorial]</color> Başarıyla Atık Üretildi: {_activeTutorialWaste.name} | Konum: {spawnPos} | Havada: {floatInAir}");
                }
            }
            else
            {
                Debug.LogWarning("<color=orange>[ARHoloTutorial]</color> Sahnede WasteSpawner veya atanmış atık prefabı bulunamadı!");
            }

            yield return new WaitForSeconds(0.3f);
        }

        private void SetupCompositeFallback(GameObject pieceA, Vector3 posA)
        {
            if (pieceA == null) return;

            // İkinci parçayı hemen yanına üret
            Vector3 posB = posA + Vector3.right * 0.35f;
            GameObject pieceB = Instantiate(pieceA, posB, pieceA.transform.rotation);
            if (pieceB == null) return;

            var grabA = pieceA.GetComponentInChildren<XRGrabInteractable>();
            var grabB = pieceB.GetComponentInChildren<XRGrabInteractable>();

            if (grabA != null && grabB != null)
            {
                var glue = pieceA.GetComponent<WasteGlue>() ?? pieceA.AddComponent<WasteGlue>();
                var controller = pieceA.GetComponent<CompositeWasteController>() ?? pieceA.AddComponent<CompositeWasteController>();
                glue.Bind(grabA, grabB, null);
            }
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
            if (wastePointer != null) wastePointer.gameObject.SetActive(false);
            if (binPointer != null) binPointer.gameObject.SetActive(false);
            if (handGuide != null) handGuide.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            BinTrigger.OnWasteProcessed -= OnStep2WasteSorted;
            BinTrigger.OnWasteProcessed -= OnStep3WasteSorted;
        }

        #endregion
    }
}
