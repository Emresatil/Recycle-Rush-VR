using UnityEngine;
using System.IO;
using System;
using RecycleRush.Core; // ScoreManager vb. için gerekli olabilir

namespace RecycleRush.Managers
{
    /// <summary>
    /// JSON olarak cihazda saklanacak Analiz (Davranış) Veri Modeli
    /// </summary>
    [Serializable]
    public class AnalyticsData
    {
        public int TotalGamesPlayed = 0;
        public int TotalGamesCompleted = 0; // Süre sonuna kadar (GameOver) hayatta kalabilme
        public float TotalPlayTime = 0f;
        
        // Hangi kutuya kaç yanlış atış yapıldı?
        public int PaperBinErrors = 0;
        public int GlassBinErrors = 0;
        public int PlasticBinErrors = 0;
        public int MetalBinErrors = 0;

        // Golden Waste Oranı
        public int TotalGoldenWastesSpawned = 0;
        public int TotalGoldenWastesCaught = 0;

        // Kombo İstatistikleri
        public int TotalCombosReached = 0; // Kaç kere komboya girdi
        public int MaxComboEverReached = 0; // Tüm zamanların en yüksek kombosu
    }

    /// <summary>
    /// Oyuncu davranışlarını (hatalarını, oyun süresini vb.) arka planda gözlemleyen 
    /// ve JSON olarak kaydeden Singleton Veri Madenciliği sistemi.
    /// (Single Responsibility: Sadece veriyi dinler ve kaydeder, oyuna karışmaz).
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        public AnalyticsData CurrentData { get; private set; }
        
        private string _analyticsFilePath;

        // Anlık oyun oturumu verileri (Süre ölçümü için)
        private float _sessionStartTime;
        private bool _isSessionActive = false;

        private void Awake()
        {
            // Singleton (Thread-safe)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Unity Uyarısı Çözümü: DontDestroyOnLoad sadece Root (En üst düzey) objelerde çalışır.
            // Eğer objeyi yanlışlıkla başka bir objenin içine koyduysan, kod onu otomatik olarak en dışa çıkarır.
            transform.SetParent(null); 
            DontDestroyOnLoad(gameObject);

            // Verinin kaydedileceği güvenli dizin (Persistent Data Path)
            _analyticsFilePath = Path.Combine(Application.persistentDataPath, "AnalyticsData.json");

            LoadAnalytics();
        }

        /// <summary>
        /// Analiz verilerini JSON formatında yerel cihaza kaydeder.
        /// </summary>
        public void SaveAnalytics()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentData, true); // true: Okunabilir (PrettyPrint) format
                File.WriteAllText(_analyticsFilePath, json);
                Debug.Log($"<color=cyan>[AnalyticsManager]</color> Veriler kaydedildi! Yol: {_analyticsFilePath}\nKayıt İçeriği:\n{json}");
            }
            catch (Exception e)
            {
                Debug.LogError($"<color=red>[AnalyticsManager]</color> Analiz verisi kaydedilirken hata: {e.Message}");
            }
        }

        /// <summary>
        /// Mevcut analiz verisini yükler, yoksa sıfırdan oluşturur.
        /// </summary>
        private void LoadAnalytics()
        {
            try
            {
                if (File.Exists(_analyticsFilePath))
                {
                    string json = File.ReadAllText(_analyticsFilePath);
                    CurrentData = JsonUtility.FromJson<AnalyticsData>(json);
                    Debug.Log("<color=cyan>[AnalyticsManager]</color> Önceki analiz verileri başarıyla yüklendi.");
                }
                else
                {
                    CurrentData = new AnalyticsData();
                    Debug.Log("<color=yellow>[AnalyticsManager]</color> İlk defa açılıyor, yepyeni bir Analiz dosyası oluşturuldu.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"<color=red>[AnalyticsManager]</color> Analiz dosyası okunurken hata (Veri sıfırlanıyor): {e.Message}");
                CurrentData = new AnalyticsData();
            }
        }

        private void OnEnable()
        {
            // Sinyal abonelikleri (Oyunun gidişatını sessizce dinliyoruz)
            GameManager.OnGameStateChanged += HandleGameStateChanged;
            BinTrigger.OnWasteProcessed += HandleWasteProcessed;
            WasteSpawner.OnGoldenWasteSpawned += HandleGoldenWasteSpawned;
        }

        private void OnDisable()
        {
            // Hafıza sızıntısı (Memory Leak) önlemek için abonelikleri iptal et
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
            WasteSpawner.OnGoldenWasteSpawned -= HandleGoldenWasteSpawned;
            
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnComboChanged -= HandleComboChanged;
            }
        }

        private void Start()
        {
            // ScoreManager genelde Awake'de kendini kurar, biz Start'ta güvenle abone olabiliriz
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnComboChanged += HandleComboChanged;
            }
        }

        #region Event Handlers (Veri Toplama Noktaları)

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Playing)
            {
                CurrentData.TotalGamesPlayed++;
                _sessionStartTime = Time.time; // Süreyi başlat
                _isSessionActive = true;
                SaveAnalytics(); // Oyuna başlama anını anında kaydet (Crash durumuna karşı)
            }
            else if (state == GameState.GameOver || state == GameState.MainMenu)
            {
                if (_isSessionActive)
                {
                    if (state == GameState.GameOver) 
                    {
                        CurrentData.TotalGamesCompleted++; // Süreyi sağ salim bitirebilme başarısı
                    }

                    float duration = Time.time - _sessionStartTime;
                    CurrentData.TotalPlayTime += duration;
                    _isSessionActive = false;
                    SaveAnalytics();
                }
            }
        }

        private void HandleWasteProcessed(SortResultData data)
        {
            if (data.WasGoldenWaste && data.IsCorrect)
            {
                CurrentData.TotalGoldenWastesCaught++; // Altın çöp kutuya başarıyla sokuldu
            }

            if (!data.IsCorrect)
            {
                // Oyuncu HATA yaptı! Hangi kutuda zorlandığını kaydet:
                switch (data.TargetBinType)
                {
                    case WasteType.Paper: CurrentData.PaperBinErrors++; break;
                    case WasteType.Glass: CurrentData.GlassBinErrors++; break;
                    case WasteType.Plastic: CurrentData.PlasticBinErrors++; break;
                    case WasteType.Metal: CurrentData.MetalBinErrors++; break;
                }
            }
            // Çok fazla File I/O olmaması için çöp atışlarında anında Save atmıyoruz (Oyun bitiminde atılacak).
        }

        private void HandleComboChanged(int comboCount, int multiplier)
        {
            // En yüksek rekoru güncelle
            if (comboCount > CurrentData.MaxComboEverReached)
            {
                CurrentData.MaxComboEverReached = comboCount;
            }

            // Eğer oyuncu x3 veya x2 katlayıcı eşiğine ulaşabildiyse bunu bir "Kombo Başarısı" say
            if (comboCount == 3 || comboCount == 5)
            {
                CurrentData.TotalCombosReached++;
            }
        }

        private void HandleGoldenWasteSpawned()
        {
            CurrentData.TotalGoldenWastesSpawned++;
            // Çok kritik bir nadir olay olduğu için anında save alınabilir (isteğe bağlı)
        }

        #endregion

        private void OnApplicationQuit()
        {
            // Oyun aniden kapatılırsa bile (Exit butonu veya pencere kapama) son verileri disk'e yaz
            if (_isSessionActive)
            {
                float duration = Time.time - _sessionStartTime;
                CurrentData.TotalPlayTime += duration;
            }
            SaveAnalytics();
        }
    }
}
