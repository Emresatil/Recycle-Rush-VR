using UnityEngine;
using System.IO;
using System;
using RecycleRush.Core; // ScoreManager vb. için gerekli olabilir

namespace RecycleRush.Managers
{
    /// <summary>
    /// Huni (Funnel) Analizi için hangi state'e kaç kere girildiğini JSON'da güvenli tutacak yapı
    /// (Dictionary yerine Serialize olabilen List yapısı).
    /// </summary>
    [Serializable]
    public struct StateReachData
    {
        public GameState State;
        public int Count;
    }

    /// <summary>
    /// JSON olarak cihazda saklanacak Analiz (Davranış) Veri Modeli
    /// </summary>
    [Serializable]
    public class AnalyticsData
    {
        public int TotalGamesPlayed = 0;
        public int TotalGamesCompleted = 0; // Süre sonuna kadar (GameOver) hayatta kalabilme
        public float TotalPlayTime = 0f;
        
        // İsabet Oranı (Accuracy)
        public int TotalCorrectThrows = 0;
        public int TotalIncorrectThrows = 0;

        // Tepki Süresi (Reaction Time)
        public float TotalReactionTime = 0f;
        public float TotalGoldenWasteReactionTime = 0f;

        // Huni (Funnel) Analizi ve State Takibi
        public System.Collections.Generic.List<StateReachData> StateReaches = new System.Collections.Generic.List<StateReachData>();
        public GameState QuitDuringState;

        // Dalga (Wave) Analizi
        public int MaxWaveReached = 0;
        
        // Hangi kutuya kaç yanlış atış yapıldı?
        public int PaperBinErrors = 0;
        public int GlassBinErrors = 0;
        // Kirlilik ve Hayatta Kalma (Room Pollution)
        public int TotalPollutionGameOvers = 0;
        public float MaxPollutionEverReached = 0f;
        public float TotalPollutionAdded = 0f;
        public float TotalPollutionReduced = 0f;
        public int TotalWastesRecoveredFromFloor = 0;
        public int PlasticBinErrors = 0;
        public int MetalBinErrors = 0;

        // Golden Waste Oranı
        public int TotalGoldenWastesSpawned = 0;
        public int TotalGoldenWastesCaught = 0;

        // Kombo Başarısı Takibi
        public int TotalCombosReached = 0;
        public int MaxComboEverReached = 0;
        public int TotalGraceEarned = 0; // Kaç kere af kazanıldı
        public int TotalGraceUsed = 0; // Kazanılan afların kaçı kullanıldı

        // Başarım ve Ödül Takibi
        public int TotalUnlockedAchievements = 0;
        public int TotalMedalsEarned = 0;
        
        // YENİ: Precision Takibi
        public string PrecisionSettingsVersion = "Unknown";
        public int TotalPerfectThrows = 0;
        public int TotalGreatThrows = 0;
        public int TotalGoodThrows = 0;
        public float AveragePrecision = 0f;
        public float BestPrecision = 0f;
        
        // Harf Notu (Grade) Dağılımı
        public int TotalSGrades = 0;
        public int TotalAGrades = 0;
        public int TotalBGrades = 0;
        public int TotalCGrades = 0;
        public int TotalDGrades = 0;
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

            Managers.ComboManager.OnComboChanged += HandleComboChanged;
            Managers.ComboManager.OnComboGraceEarned += HandleGraceEarned;
            Managers.ComboManager.OnComboGraceUsed += HandleGraceUsed;

            Managers.AchievementManager.OnAchievementUnlocked += HandleAchievementUnlocked;
        }

        private void OnDisable()
        {
            // Hafıza sızıntısı (Memory Leak) önlemek için abonelikleri iptal et
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
            BinTrigger.OnWasteProcessed -= HandleWasteProcessed;
            WasteSpawner.OnGoldenWasteSpawned -= HandleGoldenWasteSpawned;
            
            Managers.ComboManager.OnComboChanged -= HandleComboChanged;
            Managers.ComboManager.OnComboGraceEarned -= HandleGraceEarned;
            Managers.ComboManager.OnComboGraceUsed -= HandleGraceUsed;

            Managers.AchievementManager.OnAchievementUnlocked -= HandleAchievementUnlocked;
        }

        private void Start()
        {
            // ComboManager genelde Awake'de kendini kurar, biz Start'ta güvenle abone olabiliriz
        }

        #region Event Handlers (Veri Toplama Noktaları)

        private void RecordStateReach(GameState state)
        {
            // Dictionary yerine kullanılan List yapısında arama yapıyoruz (JSON uyumlu)
            bool found = false;
            for (int i = 0; i < CurrentData.StateReaches.Count; i++)
            {
                if (CurrentData.StateReaches[i].State == state)
                {
                    var data = CurrentData.StateReaches[i];
                    data.Count++;
                    CurrentData.StateReaches[i] = data; // Struct olduğu için üstüne yazmalıyız
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                CurrentData.StateReaches.Add(new StateReachData { State = state, Count = 1 });
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            RecordStateReach(state); // Huni (Funnel) Analizi için kaydet

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
                        
                        // Oda Kirlilik (Room Pollution) Analizlerini Kaydet
                        if (RoomPollutionManager.Instance != null)
                        {
                            var pStats = RoomPollutionManager.Instance.Stats;
                            
                            // Rekorları güncelle
                            if (pStats.PeakPollution > CurrentData.MaxPollutionEverReached)
                                CurrentData.MaxPollutionEverReached = pStats.PeakPollution;
                                
                            CurrentData.TotalPollutionAdded += pStats.TotalPollutionAdded;
                            CurrentData.TotalPollutionReduced += pStats.TotalPollutionReduced;
                            CurrentData.TotalWastesRecoveredFromFloor += pStats.WasteRecoveredBeforePenalty;

                            // Kirlilikten dolayı mı kaybedildi?
                            if (RoomPollutionManager.Instance.CurrentPollution >= 100f)
                            {
                                CurrentData.TotalPollutionGameOvers++;
                            }
                        }
                        if (GameManager.Instance != null)
                        {
// Yeni Snapshot'tan gelen AAA verilerini Analytics'e kaydet
                            var finalReport = GameManager.Instance.FinalSessionReport;
                            
                            // YENİ: Precision Verilerini Kaydet
                            if (RecycleRush.Core.PrecisionSystem.PrecisionManager.Instance != null && RecycleRush.Core.PrecisionSystem.PrecisionManager.Instance.Settings != null)
                            {
                                CurrentData.PrecisionSettingsVersion = RecycleRush.Core.PrecisionSystem.PrecisionManager.Instance.Settings.SettingsVersion;
                            }
                            
                            CurrentData.TotalPerfectThrows += finalReport.TotalPerfectThrows;
                            CurrentData.TotalGreatThrows += finalReport.TotalGreatThrows;
                            CurrentData.TotalGoodThrows += finalReport.TotalGoodThrows;
                            
                            // Average Precision Güncellemesi (Tüm oyunların ağırlıklı ortalaması)
                            int previousTotalCorrect = CurrentData.TotalCorrectThrows - finalReport.TotalCorrectThrows; // Toplam doğru atışlar az önce HandleWasteProcessed içinde arttığı için
                            if (CurrentData.TotalCorrectThrows > 0)
                            {
                                float oldSum = CurrentData.AveragePrecision * Mathf.Max(0, previousTotalCorrect);
                                float newSum = oldSum + (finalReport.AveragePrecision * finalReport.TotalCorrectThrows);
                                CurrentData.AveragePrecision = newSum / CurrentData.TotalCorrectThrows;
                            }
                            
                            if (finalReport.BestPrecision > CurrentData.BestPrecision)
                            {
                                CurrentData.BestPrecision = finalReport.BestPrecision;
                            }

                            if (finalReport.EarnedMedals != null)
                                CurrentData.TotalMedalsEarned += finalReport.EarnedMedals.Count;
                                
                            switch (finalReport.PerformanceGrade)
                            {
                                case "S": CurrentData.TotalSGrades++; break;
                                case "A": CurrentData.TotalAGrades++; break;
                                case "B": CurrentData.TotalBGrades++; break;
                                case "C": CurrentData.TotalCGrades++; break;
                                case "D": CurrentData.TotalDGrades++; break;
                            }
                        }
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
            if (data.IsCorrect)
            {
                CurrentData.TotalCorrectThrows++;
                
                if (data.WasGoldenWaste)
                {
                    CurrentData.TotalGoldenWastesCaught++; // Altın çöp kutuya başarıyla sokuldu
                    CurrentData.TotalGoldenWasteReactionTime += data.ReactionTime;
                }
                else
                {
                    CurrentData.TotalReactionTime += data.ReactionTime;
                }
            }
            else
            {
                CurrentData.TotalIncorrectThrows++;
                CurrentData.TotalReactionTime += data.ReactionTime;

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

        private void HandleComboChanged(int comboCount, int multiplier, bool isRankUp)
        {
            // En yüksek rekoru güncelle
            if (comboCount > CurrentData.MaxComboEverReached)
            {
                CurrentData.MaxComboEverReached = comboCount;
            }

            // Eğer oyuncu kademe atladıysa (isRankUp) bunu bir "Kombo Başarısı" say
            if (isRankUp)
            {
                CurrentData.TotalCombosReached++;
            }
        }

        private void HandleGraceEarned()
        {
            CurrentData.TotalGraceEarned++;
        }

        private void HandleGraceUsed()
        {
            CurrentData.TotalGraceUsed++;
            SaveAnalytics();
        }

        private void HandleAchievementUnlocked(AchievementData data)
        {
            CurrentData.TotalUnlockedAchievements++;
            SaveAnalytics();
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
            if (GameManager.Instance != null)
            {
                CurrentData.QuitDuringState = GameManager.Instance.CurrentState;
            }

            if (_isSessionActive)
            {
                float duration = Time.time - _sessionStartTime;
                CurrentData.TotalPlayTime += duration;
            }
            SaveAnalytics();
        }
    }
}
