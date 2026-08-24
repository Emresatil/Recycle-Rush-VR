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
        }

        private void OnEnable()
        {
            EventManager.OnGameEventStarted += HandleGameEventStarted;
            EventManager.OnGameEventEnded += HandleGameEventEnded;
            DifficultyManager.OnDifficultyLevelChanged += UpdateSpawnSpeed;
        }

        private void OnDisable()
        {
            EventManager.OnGameEventStarted -= HandleGameEventStarted;
            EventManager.OnGameEventEnded -= HandleGameEventEnded;
            DifficultyManager.OnDifficultyLevelChanged -= UpdateSpawnSpeed;
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
            
            // GameManager artık spawner'ı kontrol ettiği için otomatik başlamayı kaldırdık.
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

            // Rastgele bir konum hesapla (SpawnCenter'ın etrafında X ve Z ekseninde rastgele bir çember)
            Vector3 centerPos = spawnCenter != null ? spawnCenter.position : Vector3.zero;
            
            // Rastgele bir açı (0-360) ve yarıçap (0 - spawnRadius) belirle
            float randomAngle = Random.Range(0f, Mathf.PI * 2f);
            float randomDist = Random.Range(0f, spawnRadius);

            float randomX = Mathf.Cos(randomAngle) * randomDist;
            float randomZ = Mathf.Sin(randomAngle) * randomDist;

            // Düşme noktası: Merkezden X ve Z kadar uzaklaş, yüksekliği (Y) ise merkezden spawnHeight kadar yukarıda tut.
            Vector3 randomSpawnPos = new Vector3(
                centerPos.x + randomX, 
                centerPos.y + spawnHeight, 
                centerPos.z + randomZ
            );

            // --- ÇAKIŞMA ÖNLEYİCİ (Overlap Check) ---
            // Objeler spawn olurken tavanla veya birbirleriyle iç içe geçerse yatayda (havada) mermi gibi fırlarlar.
            // Bunu engellemek için spawn noktasının boş olup olmadığını kontrol ediyoruz.
            Collider[] existingColliders = Physics.OverlapSphere(randomSpawnPos, 0.4f);
            foreach (var col in existingColliders)
            {
                if (!col.isTrigger)
                {
                    // Çakışma var, bu spawn döngüsünü iptal et (Sonraki döngüde başka bir yerde dener)
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
            // Seviyeye göre altın çöp şansının artması, ileri seviyelerde oyunun dengesini bozduğu için kaldırıldı.
            // Ayrıca etkinliklerin (LuckyDrop) bu ihtimali değiştirmesi istendiği gibi iptal edildi.
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
                    // Konsola bilgi yazdır
                    Debug.Log($"<color=yellow>[PortalSpawner]</color> Jackpot! Altın Çöp düştü! (Mevcut İhtimal: %{currentGoldenChance * 100})");
                }
                else
                {
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
                            foreach (var p in wastePrefabs)
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
                        // Şans tutmadı, rastgele standart atık (Kağıt/Cam/Plastik) seç
                        selectedPrefab = wastePrefabs[Random.Range(0, wastePrefabs.Length)];
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
