using UnityEngine;

namespace RecycleRush.AI
{
    /// <summary>
    /// FSM durumlarını başlatan ve dronun renk/baktığı yön gibi görsel işlerini yürüten ana sınıf.
    /// Dron uzaya fırlamasın diye pozisyon değiştirmesi yasaklanmıştır (Sadece olduğu yerde döner).
    /// </summary>
    public class QCDroneController : MonoBehaviour
    {
        [Header("Görsel Ayarlar")]
        [Tooltip("Dronun dış yüzeyindeki meshler (Göz, Kasa vb.). Renk değiştirmek için kullanılır.")]
        public MeshRenderer[] droneMeshes;
        
        [Tooltip("Dronun yüzü sana bakmıyorsa (ters dönüyorsa) bu değeri 0, 90, 180, -90 gibi ayarlayabilirsin.")]
        public float modelRotationOffset = 180f;

        [Header("Renk Durumları")]
        public Color normalColor = Color.white;
        public Color happyColor = Color.green;
        public Color angryColor = Color.red;
        public Color sadColor = new Color(1f, 0.5f, 0f); // Turuncu

        // FSM Çekirdeği
        public StateMachine StateMachine { get; private set; }
        
        // Durumlar
        public DroneIdleState IdleState { get; private set; }
        public DroneHappyState HappyState { get; private set; }
        public DroneAngryState AngryState { get; private set; }
        public DroneSadState SadState { get; private set; }

        private void Awake()
        {
            StateMachine = new StateMachine();
            
            IdleState = new DroneIdleState(this, StateMachine);
            HappyState = new DroneHappyState(this, StateMachine);
            AngryState = new DroneAngryState(this, StateMachine);
            SadState = new DroneSadState(this, StateMachine);
        }

        private void Start()
        {
            // Dronu artık kilitli bir yere ışınlamıyoruz!
            // Editörde nereye koyarsan orada kalacak.
            
            StateMachine.Initialize(IdleState);
        }

        private void OnEnable()
        {
            // Olayları (Events) dinlemeye başla
            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
            DestroyZone.OnWasteMissed += HandleWasteMissed;
            RecycleRush.Environment.FloorZone.OnWasteMissedFloor += HandleWasteMissed;
        }

        private void OnDisable()
        {
            // Olayları (Events) dinlemeyi bırak (Memory Leak önlemek için)
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
            DestroyZone.OnWasteMissed -= HandleWasteMissed;
            RecycleRush.Environment.FloorZone.OnWasteMissedFloor -= HandleWasteMissed;
        }

        private void HandleWasteProcessed(SortResultData data)
        {
            if (data.IsCorrect)
                StateMachine.ChangeState(HappyState);
            else
                StateMachine.ChangeState(AngryState);
        }

        private void HandleWasteMissed(int penalty)
        {
            StateMachine.ChangeState(SadState);
        }

        private void Update()
        {
            StateMachine.Update();
        }

        // --- GÖRSEL YARDIMCI FONKSİYONLAR ---

        /// <summary>
        /// Dronun tüm mesh parçalarının rengini değiştirir.
        /// </summary>
        public void ChangeColor(Color newColor)
        {
            if (droneMeshes == null) return;
            foreach (var mesh in droneMeshes)
            {
                if (mesh != null)
                {
                    mesh.material.color = newColor;
                }
            }
        }

        /// <summary>
        /// Sadece rotasyon yapar (Dönerek oyuncuya bakar). Dronun pozisyonunu ELLEMEZ!
        /// </summary>
        public void FacePlayer(Vector3 extraRotationOffset = default)
        {
            // OYUNCUYU BUL: Önce ana kamerayı, yoksa sahnede herhangi bir kamerayı, yoksa XR Rig'i dene
            Transform target = null;
            if (Camera.main != null) target = Camera.main.transform;
            else if (Object.FindFirstObjectByType<Camera>() != null) target = Object.FindFirstObjectByType<Camera>().transform;
            else if (GameObject.Find("XR Origin (XR Rig)") != null) target = GameObject.Find("XR Origin (XR Rig)").transform;

            Quaternion baseRot = transform.rotation; // Hedef yoksa şu anki yönüne bakmaya devam et

            if (target != null)
            {
                Vector3 lookDir = target.position - transform.position;
                lookDir.y = 0; // Ufka sabit kalsın
                
                if (lookDir != Vector3.zero)
                {
                    baseRot = Quaternion.LookRotation(lookDir);
                }
            }

            // Animasyon (extraRot) ve Model Ofsetini (offsetRot) her halükarda uygula!
            Quaternion offsetRot = Quaternion.Euler(0, modelRotationOffset, 0);
            Quaternion extraRot = Quaternion.Euler(extraRotationOffset);
            
            Quaternion targetRotation = baseRot * offsetRot * extraRot;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }
}
