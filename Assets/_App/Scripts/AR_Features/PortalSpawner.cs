using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RecycleRush.Managers;

namespace RecycleRush.AR_Features
{
    /// <summary>
    /// AR ortamında odanın tavanından (veya belirlenen portallardan) geri dönüşüm atıklarını
    /// ObjectPool kullanarak düşüren yönetici sınıf.
    /// </summary>
    public class PortalSpawner : MonoBehaviour
    {
        [Header("Spawn Ayarları")]
        [Tooltip("Atıkların etrafında rastgele doğacağı merkez nokta (Genelde Main Camera veya XR Origin verilir)")]
        public Transform spawnCenter;

        [Tooltip("Merkez noktanın etrafında atıkların düşebileceği maksimum yarıçap (Metre)")]
        public float spawnRadius = 2.0f;

        [Tooltip("Atıkların oyuncunun kafasından ne kadar yüksekten düşeceği (Metre)")]
        public float spawnHeight = 1.5f;
        
        [Tooltip("Üretilecek standart atık prefabları (Kağıt, Cam, Plastik vb.)")]
        public GameObject[] wastePrefabs;

        [Header("Golden Waste (Altın Çöp) Ayarları")]
        [Tooltip("Nadir altın çöp prefabı")]
        public GameObject goldenWastePrefab;

        [Tooltip("Mevcut seviyeye bağlı Altın Çöp çıkma ihtimali (%5 - %25)")]
        [Range(0.05f, 0.25f)]
        public float goldenWasteChance = 0.05f;
        
        [Header("Power-Up Ayarları")]
        [Tooltip("Zaman uzatan Kum Saati prefabı")]
        public GameObject hourglassPrefab;
        [Tooltip("Çöpleri otomatik toplayan Mıknatıs prefabı")]
        public GameObject magnetPrefab;

        [Header("Zamanlama (Timing)")]
        [Tooltip("Saniye cinsinden iki atık arası bekleme süresi")]
        public float spawnInterval = 1.2f;
        
        private float _spawnTimer = 0f;
        private bool _isSpawning = false;
        private float _originalSpawnInterval; // Etkinlik bitince geri dönmek için

        private void Awake()
        {
            _originalSpawnInterval = spawnInterval;
            
            if (spawnCenter == null && Camera.main != null)
            {
                spawnCenter = Camera.main.transform;
            }
        }

        private void OnEnable()
        {
            EventManager.OnGameEventStarted += HandleGameEventStarted;
            EventManager.OnGameEventEnded += HandleGameEventEnded;
            DifficultyManager.OnDifficultyLevelChanged += UpdateSpawnSpeed;
            GameManager.OnGameStateChanged += HandleGameStateChanged;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                StartSpawning();
            }
        }

        private void OnDisable()
        {
            EventManager.OnGameEventStarted -= HandleGameEventStarted;
            EventManager.OnGameEventEnded -= HandleGameEventEnded;
            DifficultyManager.OnDifficultyLevelChanged -= UpdateSpawnSpeed;
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Start()
        {
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogWarning("<color=red>[PortalSpawner]</color> Sahnede ObjectPoolManager bulunamadı!");
            }
            
            if (spawnCenter == null && Camera.main != null)
            {
                spawnCenter = Camera.main.transform;
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                StartSpawning();
            }
        }

        private void Update()
        {
            if (!_isSpawning) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= spawnInterval)
            {
                SpawnWaste();
                _spawnTimer = 0f;
            }
        }

        private void UpdateSpawnSpeed(float multiplier)
        {
            // Zorluk arttıkça bekleme süresi kısalır ama çakışmayı önlemek için minimum 0.5s sınır konur
            spawnInterval = Mathf.Max(0.5f, _originalSpawnInterval / multiplier);
        }

        private void HandleGameEventStarted(GameEventType eventType)
        {
            if (eventType == GameEventType.SpeedMode)
            {
                spawnInterval = _originalSpawnInterval * 0.5f; // 2x hız
                Debug.Log("<color=cyan>[PortalSpawner]</color> SpeedMode aktif! Spawn hızı iki katına çıktı.");
            }
            else if (eventType == GameEventType.FrenzyMode)
            {
                spawnInterval = _originalSpawnInterval * 0.2f; // 5x hız (çılgınlık)
                Debug.Log("<color=red>[PortalSpawner]</color> FRENZY MODE aktif! Çöpler yağıyor.");
            }
        }

        private void HandleGameEventEnded()
        {
            // Etkinlik bitince normal hıza dön
            spawnInterval = _originalSpawnInterval;
            Debug.Log("<color=cyan>[PortalSpawner]</color> Etkinlik bitti, normal spawn hızına dönüldü.");
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Playing)
            {
                StartSpawning();
            }
            else
            {
                StopSpawning();
            }
        }

        public void StartSpawning()
        {
            _isSpawning = true;
            _spawnTimer = 0f;
            Debug.Log("<color=cyan>[PortalSpawner]</color> Atık üretimi başladı.");
        }

        public void StopSpawning()
        {
            _isSpawning = false;
            Debug.Log("<color=cyan>[PortalSpawner]</color> Atık üretimi durduruldu.");
        }

        /// <summary>
        /// ObjectPoolManager üzerinden havuzdan obje çeker ve rastgele bir konumdan serbest düşüşe bırakır.
        /// </summary>
        private void SpawnWaste()
        {
            List<GameObject> validPrefabs = new List<GameObject>();
            if (wastePrefabs != null)
            {
                foreach (var p in wastePrefabs)
                {
                    if (p != null) validPrefabs.Add(p);
                }
            }

            if (validPrefabs.Count == 0)
            {
                Debug.LogWarning("<color=orange>[PortalSpawner]</color> wastePrefabs listesi boş veya tüm elemanlar None! Inspector'dan prefab ekleyin.");
                return;
            }

            // Rastgele bir konum hesapla
            Vector3 centerPos = spawnCenter != null ? spawnCenter.position : Vector3.zero;
            
            float randomAngle = Random.Range(0f, Mathf.PI * 2f);
            float randomDist = Random.Range(0.2f, spawnRadius);

            float randomX = Mathf.Cos(randomAngle) * randomDist;
            float randomZ = Mathf.Sin(randomAngle) * randomDist;

            Vector3 randomSpawnPos = new Vector3(
                centerPos.x + randomX, 
                centerPos.y + spawnHeight, 
                centerPos.z + randomZ
            );

            // --- ÇAKIŞMA ÖNLEYİCİ (Overlap Check) ---
            // Sadece diğer atıklarla iç içe girmeyi engelle (Oda veya zemin mesh'lerini yok say)
            Collider[] existingColliders = Physics.OverlapSphere(randomSpawnPos, 0.3f);
            foreach (var col in existingColliders)
            {
                if (!col.isTrigger && col.attachedRigidbody != null)
                {
                    if (IsWaste(col.gameObject))
                    {
                        Debug.LogWarning($"<color=orange>[PortalSpawner]</color> Spawn noktası dolu! ({col.gameObject.name}).");
                        return; 
                    }
                }
            }

            GameObject selectedPrefab = null;
            
            int currentStage = 1;
            if (LevelSelectionManager.Instance != null)
            {
                currentStage = LevelSelectionManager.Instance.CurrentPlayingLevelId;
            }
            else if (LevelManager.Instance != null)
            {
                currentStage = LevelManager.Instance.CurrentLevel;
            }

            float currentGoldenChance = 0.05f;

            // POWER-UP: Kum Saati (Level 20-30 Arası)
            bool isHourglassSpawned = false;
            if (hourglassPrefab != null && currentStage >= 20 && currentStage <= 30)
            {
                if (Random.value <= 0.1f) 
                {
                    selectedPrefab = hourglassPrefab;
                    isHourglassSpawned = true;
                    Debug.Log($"<color=magenta>[PortalSpawner]</color> POWER-UP! Kum Saati Düştü! (Aşama: {currentStage})");
                }
            }

            // POWER-UP: Mıknatıs (Level 20-30 Arası)
            bool isMagnetSpawned = false;
            if (!isHourglassSpawned && magnetPrefab != null && currentStage >= 20 && currentStage <= 30)
            {
                if (Random.value <= 0.08f)
                {
                    selectedPrefab = magnetPrefab;
                    isMagnetSpawned = true;
                    Debug.Log($"<color=magenta>[PortalSpawner]</color> POWER-UP! Mıknatıs Düştü! (Aşama: {currentStage})");
                }
            }

            if (!isHourglassSpawned && !isMagnetSpawned)
            {
                if (goldenWastePrefab != null && Random.value <= currentGoldenChance)
                {
                    selectedPrefab = goldenWastePrefab;
                    Debug.Log($"<color=yellow>[PortalSpawner]</color> Jackpot! Altın Çöp düştü!");
                }
                else
                {
                    // Görev odaklı spawn (Mission Biasing)
                    bool missionBiased = false;
                    if (MissionManager.Instance != null && 
                        MissionManager.Instance.ActiveMission != null &&
                        MissionManager.Instance.ActiveMission.Type == MissionType.CollectWaste &&
                        !MissionManager.Instance.ActiveMission.IsCompleted)
                    {
                        if (Random.value <= 0.6f)
                        {
                            string targetTag = MissionManager.Instance.ActiveMission.TargetWaste.ToString();
                            List<GameObject> targetPrefabs = new List<GameObject>();
                            foreach (var p in validPrefabs)
                            {
                                if (p != null && p.CompareTag(targetTag)) targetPrefabs.Add(p);
                            }
                            
                            if (targetPrefabs.Count > 0)
                            {
                                selectedPrefab = targetPrefabs[Random.Range(0, targetPrefabs.Count)];
                                missionBiased = true;
                            }
                        }
                    }
                    
                    if (!missionBiased)
                    {
                        selectedPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
                    }
                }
            }

            if (selectedPrefab == null) return;

            if (ObjectPoolManager.Instance != null)
            {
                GameObject spawnedWaste = ObjectPoolManager.Instance.SpawnFromPool(
                    selectedPrefab.name, 
                    selectedPrefab, 
                    randomSpawnPos, 
                    Random.rotation
                );

                if (spawnedWaste != null)
                {
                    Rigidbody rb = spawnedWaste.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        rb.linearVelocity = Vector3.down * 1.5f; 
                    }
                }
            }
            else
            {
                Instantiate(selectedPrefab, randomSpawnPos, Random.rotation);
            }
        }

        private bool IsWaste(GameObject obj)
        {
            return obj.CompareTag("Paper") || obj.CompareTag("Glass") || obj.CompareTag("Plastic") || 
                   obj.CompareTag("Metal") || obj.CompareTag("Organic") || obj.CompareTag("Hazardous") ||
                   obj.CompareTag("Untagged");
        }
        
        public void SetSpawnInterval(float newInterval)
        {
            spawnInterval = Mathf.Max(0.5f, newInterval);
            _originalSpawnInterval = spawnInterval;
        }
    }
}
