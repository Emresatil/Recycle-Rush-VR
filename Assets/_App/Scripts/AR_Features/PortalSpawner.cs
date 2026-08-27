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

        [Header("Spawn Distance & Area Settings")]
        [Tooltip("Kutulardan oyuncuya doğru ne kadar önde doğsun? (Metre)")]
        public float spawnDistanceFromBins = 2.4f;
        [Tooltip("Yatay sağ-sol dağılım genişliği (Metre)")]
        public float spawnHorizontalSpread = 1.0f;
        
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
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
            EventManager.OnGameEventStarted += HandleGameEventStarted;
            EventManager.OnGameEventEnded += HandleGameEventEnded;
            DifficultyManager.OnDifficultyLevelChanged += UpdateSpawnSpeed;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
            EventManager.OnGameEventStarted -= HandleGameEventStarted;
            EventManager.OnGameEventEnded -= HandleGameEventEnded;
            DifficultyManager.OnDifficultyLevelChanged -= UpdateSpawnSpeed;
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.Playing)
            {
                StartSpawning();
            }
            else
            {
                StopSpawning();
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

        private void Start()
        {
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError("<color=red>[PortalSpawner]</color> Sahnede ObjectPoolManager bulunamadı! Lütfen Core sistemleri ekleyin.");
            }
            
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                StartSpawning();
            }
        }

        private bool HasWasteTag(GameObject obj)
        {
            if (obj == null) return false;
            return obj.CompareTag("Paper") || obj.CompareTag("Glass") || obj.CompareTag("Plastic") || 
                   obj.CompareTag("Metal") || obj.CompareTag("Hourglass") || obj.CompareTag("Magnet");
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

        /// <summary>
        /// Atık düşürme döngüsünü başlatır. GameManager tarafından çağrılır (Örn: Playing state'e geçildiğinde).
        /// </summary>
        public void StartSpawning()
        {
            _isSpawning = true;
            _spawnTimer = 0f;
            Debug.Log("<color=cyan>[PortalSpawner]</color> Atık üretimi başladı.");
        }

        /// <summary>
        /// Atık düşürme döngüsünü durdurur. GameManager tarafından çağrılır (Örn: Oyun bitince veya duraklatılınca).
        /// </summary>
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
            if (wastePrefabs == null || wastePrefabs.Length == 0)
            {
                Debug.LogWarning("<color=orange>[PortalSpawner]</color> Prefab listesi boş!");
                return;
            }

            // Rastgele bir konum hesapla (Kutulardan spawnDistanceFromBins kadar önde veya SpawnCenter etrafında)
            Vector3 randomSpawnPos;
            if (BinTrigger.TryGetBinsCenter(out Vector3 binsCenter))
            {
                Vector3 frontDir = Vector3.back;
                Vector3 camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
                Vector3 toCam = camPos - binsCenter;
                toCam.y = 0f;
                float totalDistance = toCam.magnitude;

                if (toCam.sqrMagnitude > 0.25f)
                {
                    frontDir = toCam / totalDistance;
                }

                Vector3 rightDir = Vector3.Cross(Vector3.up, frontDir).normalized;

                float actualDistance = totalDistance > 1.5f ? Mathf.Min(spawnDistanceFromBins, totalDistance - 0.7f) : spawnDistanceFromBins;
                float frontOffset = actualDistance + Random.Range(-0.2f, 0.2f);
                float rightOffset = Random.Range(-spawnHorizontalSpread * 0.5f, spawnHorizontalSpread * 0.5f);

                float spawnY = (Camera.main != null ? Camera.main.transform.position.y : binsCenter.y + 1.2f);
                spawnY = Mathf.Max(spawnY, binsCenter.y + 0.8f);

                randomSpawnPos = binsCenter + (frontDir * frontOffset) + (rightDir * rightOffset);
                randomSpawnPos.y = spawnY;
            }
            else
            {
                Vector3 centerPos = spawnCenter != null ? spawnCenter.position : (Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f : Vector3.zero);
                float randomAngle = Random.Range(0f, Mathf.PI * 2f);
                float effectiveRadius = Mathf.Min(spawnRadius, 0.6f);
                float randomDist = Random.Range(0f, effectiveRadius);

                float randomX = Mathf.Cos(randomAngle) * randomDist;
                float randomZ = Mathf.Sin(randomAngle) * randomDist;

                randomSpawnPos = new Vector3(
                    centerPos.x + randomX, 
                    centerPos.y + spawnHeight, 
                    centerPos.z + randomZ
                );
            }

            // --- ÇAKIŞMA ÖNLEYİCİ (Overlap Check) ---
            // Objeler spawn olurken birbirleriyle iç içe geçerse yatayda (havada) mermi gibi fırlarlar.
            // Sadece diğer çöp objelerini kontrol ediyoruz; AR mekansal ortam mesh'leri engel sayılmaz!
            Collider[] existingColliders = Physics.OverlapSphere(randomSpawnPos, 0.35f);
            foreach (var col in existingColliders)
            {
                if (!col.isTrigger && HasWasteTag(col.gameObject))
                {
                    Debug.LogWarning($"<color=orange>[PortalSpawner]</color> Spawn noktası dolu! ({col.gameObject.name}). Patlama (Ghosting) önlendi.");
                    return; 
                }
            }
            // ----------------------------------------

            // Golden Waste (Altın Çöp) İhtimal Hesaplama (RNG) Algoritması
            GameObject selectedPrefab = null;
            
            // Mevcut oyun aşamasını (Stage) LevelSelectionManager'dan al
            int currentStage = 1;
            if (LevelSelectionManager.Instance != null)
            {
                currentStage = LevelSelectionManager.Instance.CurrentPlayingLevelId;
            }
            else if (LevelManager.Instance != null)
            {
                currentStage = LevelManager.Instance.CurrentLevel; // Yedek sistem
            }

            // --- ALTIN ÇÖP MANTIĞI (Sabit İhtimal) ---
            float currentGoldenChance = 0.05f;

            // --- POWER-UP: KUM SAATİ SPAWN MANTIĞI (Level 20-30 Arası) ---
            bool isHourglassSpawned = false;
            // %10 ihtimalle kum saati fırlat (Zamanın daraldığı anlar için çok kritik)
            if (hourglassPrefab != null && currentStage >= 20 && currentStage <= 30)
            {
                if (Random.value <= 0.1f) 
                {
                    selectedPrefab = hourglassPrefab;
                    isHourglassSpawned = true;
                    Debug.Log($"<color=magenta>[PortalSpawner]</color> POWER-UP! Kum Saati Düştü! (Aşama: {currentStage})");
                }
            }

            // Eğer kum saati DÜŞMEDİYSE mıknatıs düşme ihtimaline bak (Level 20-30 arası)
            bool isMagnetSpawned = false;
            if (!isHourglassSpawned && magnetPrefab != null && currentStage >= 20 && currentStage <= 30)
            {
                if (Random.value <= 0.08f) // %8 İhtimal
                {
                    selectedPrefab = magnetPrefab;
                    isMagnetSpawned = true;
                    Debug.Log($"<color=magenta>[PortalSpawner]</color> POWER-UP! Mıknatıs Düştü! (Aşama: {currentStage})");
                }
            }

            // Eğer hiçbir power-up DÜŞMEDİYSE normal çöplere ve altın çöpe bak
            if (!isHourglassSpawned && !isMagnetSpawned)
            {
                // Rastgele zar at (0.0 ile 1.0 arası)
                if (goldenWastePrefab != null && Random.value <= currentGoldenChance)
                {
                    // Şans yaver gitti, Altın Çöp seçildi!
                    selectedPrefab = goldenWastePrefab;
                    Debug.Log($"<color=yellow>[PortalSpawner]</color> Jackpot! Altın Çöp düştü! (Mevcut İhtimal: %{currentGoldenChance * 100})");
                }
                else
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
                        Debug.LogWarning("<color=orange>[PortalSpawner]</color> Geçerli standart prefab bulunamadı!");
                        return;
                    }

                    // --- GÖREV ODAKLI SPAWN SİSTEMİ (Mission Biasing) ---
                    bool missionBiased = false;
                    if (RecycleRush.Managers.MissionManager.Instance != null && 
                        RecycleRush.Managers.MissionManager.Instance.ActiveMission != null &&
                        RecycleRush.Managers.MissionManager.Instance.ActiveMission.Type == RecycleRush.Managers.MissionType.CollectWaste &&
                        !RecycleRush.Managers.MissionManager.Instance.ActiveMission.IsCompleted)
                    {
                        if (Random.value <= 0.6f) // %60 şansla görev hedefini seç
                        {
                            string targetTag = RecycleRush.Managers.MissionManager.Instance.ActiveMission.TargetWaste.ToString();
                            List<GameObject> targetPrefabs = new List<GameObject>();
                            foreach (var p in validPrefabs)
                            {
                                if (p.CompareTag(targetTag)) targetPrefabs.Add(p);
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
                        // Şans tutmadı, rastgele standart atık (Kağıt/Cam/Plastik) seç
                        selectedPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
                    }
                }
            }

            // Objeyi havuzdan çek (Instantiate yerine bellek dostu havuzlama)
            GameObject spawnedWaste = ObjectPoolManager.Instance.SpawnFromPool(
                selectedPrefab.name, 
                selectedPrefab, 
                randomSpawnPos, 
                Random.rotation // Havada rastgele dönerek düşmesi için
            );

            if (spawnedWaste != null)
            {
                // AR ortamı için Fizik ayarlarını yerçekimine uygun hale getir (Serbest Düşüş)
                Rigidbody rb = spawnedWaste.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    // Aşağıya doğru çok hafif bir ilk hız (Force) uygulayarak portalın içinden fırlamasını sağla
                    rb.linearVelocity = Vector3.down * 1.5f; 
                }
            }
        }
        
        /// <summary>
        /// Seviye (Level) ilerledikçe hızın artması için dışarıdan (LevelManager) çağrılabilir.
        /// </summary>
        public void SetSpawnInterval(float newInterval)
        {
            spawnInterval = Mathf.Max(0.5f, newInterval);
            _originalSpawnInterval = spawnInterval; // Temel hızı güncelle
        }
    }
}
