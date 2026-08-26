using UnityEngine;
using System.IO;
using System;

namespace RecycleRush.Managers
{
    [System.Serializable]
    public class MatchHistoryRecord
    {
        public string Timestamp;
        public int Score;
        public string Grade;
        public float Accuracy;
        public int MaxCombo;
        public int GoldenWastes;
    }

    /// <summary>
    /// Oyuncu verilerinin kaydedilip yüklendiği veri modeli (JSON'a çevrilecek kısım).
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        public int Level = 1;
        public int XP = 0;
        public int Coins = 0;
        
        // YENİ: Oyuncu Profili & Rekorlar
        public int HighestScore = 0;
        public float BestAccuracy = 0f;
        public int BestCombo = 0;
        public int MostGoldenWaste = 0;
        
        // YENİ: Son 10 Maç Geçmişi
        public System.Collections.Generic.List<MatchHistoryRecord> MatchHistory = new System.Collections.Generic.List<MatchHistoryRecord>();
    }

    /// <summary>
    /// Oyundaki ilerlemeyi cihazın yerel hafızasına güvenli bir şekilde JSON olarak kaydeder.
    /// Singleton yapısı sayesinde her yerden kolayca erişilebilir (Örn: SaveManager.Instance.SaveGame()).
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        public SaveData CurrentData { get; private set; }

        private string _savePath;
        private string _backupPath;
        private string _tempPath;

        private void Awake()
        {
            // Singleton Kurulumu
            if (Instance != null && Instance != this)
            {
                Destroy(this); // Core objesini toptan silmemesi icin sadece scripti siliyoruz;
                return;
            }
            Instance = this;
            
            // Yolların belirlenmesi (Asıl, Yedek ve Geçici)
            _savePath = Path.Combine(Application.persistentDataPath, "RecycleRushSave.json");
            _backupPath = Path.Combine(Application.persistentDataPath, "RecycleRushSave.bak");
            _tempPath = Path.Combine(Application.persistentDataPath, "RecycleRushSave.tmp");
            
            // Oyun açıldığında veriyi hemen yükle
            LoadGame();
        }

        /// <summary>
        /// Mevcut veriyi JSON formatına çevirip geçici dosya üzerinden güvenle diske yazar.
        /// </summary>
        
        private void Start()
        {
            // Yoneticiler hazir oldugunda onlara verileri gonder
            if (EconomyManager.Instance != null && CurrentData != null)
            {
                var ecoData = new EconomyManager.EconomySaveData { Coins = CurrentData.Coins };
                EconomyManager.Instance.LoadSaveData(ecoData);
            }

            if (LevelManager.Instance != null && CurrentData != null)
            {
                var lvlData = new LevelManager.LevelSaveData { Level = CurrentData.Level, CurrentXp = CurrentData.XP };
                LevelManager.Instance.LoadSaveData(lvlData);
            }
        }

        public void SaveGame()
        {
            try 
            {
                
            if (EconomyManager.Instance != null)
            {
                CurrentData.Coins = EconomyManager.Instance.GetSaveData().Coins;
            }
            if (LevelManager.Instance != null)
            {
                CurrentData.Level = LevelManager.Instance.GetSaveData().Level;
                CurrentData.XP = LevelManager.Instance.GetSaveData().CurrentXp;
            }

                string json = JsonUtility.ToJson(CurrentData, true); // true = Pretty Print
                
                // 1. Önce geçici (TMP) dosyaya yaz. (Şarj biterse asıl dosya bozulmasın diye)
                File.WriteAllText(_tempPath, json);

                // 2. Asıl dosya zaten varsa, onun sağlam bir yedeğini (BAK) al.
                if (File.Exists(_savePath))
                {
                    File.Copy(_savePath, _backupPath, true);
                }

                // 3. Geçici dosyayı asıl dosya olarak kopyala (Güvenli yazma adımı)
                File.Copy(_tempPath, _savePath, true);
                
                // 4. İşi biten geçici dosyayı sil
                File.Delete(_tempPath);

                Debug.Log($"<color=green>[SaveManager]</color> Oyun başarıyla (GÜVENLİ) kaydedildi.");
            }
            catch (Exception e)
            {
                Debug.LogError($"<color=red>[SaveManager]</color> Oyun kaydedilirken KRİTİK HATA oluştu: {e.Message}");
            }
        }

        /// <summary>
        /// Dosya varsa okur, yoksa yedeğe bakar, o da yoksa sıfır veri oluşturur.
        /// </summary>
        public void LoadGame()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    string json = File.ReadAllText(_savePath);
                    CurrentData = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log("<color=cyan>[SaveManager]</color> Kayıtlı veri başarıyla yüklendi.");
                }
                else if (File.Exists(_backupPath))
                {
                    // Asıl dosya yok veya bozulmuşsa, YEDEKTEN kurtar!
                    string json = File.ReadAllText(_backupPath);
                    CurrentData = JsonUtility.FromJson<SaveData>(json);
                    Debug.LogWarning("<color=orange>[SaveManager]</color> Asıl kayıt dosyası bulunamadı, YEDEK (.bak) dosyadan kurtarıldı.");
                }
                else
                {
                    // Daha önce hiç oyun oynanmamışsa
                    CurrentData = new SaveData();
                    Debug.Log("<color=yellow>[SaveManager]</color> Kayıt dosyası bulunamadı, yeni (Sıfır) veri oluşturuldu.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"<color=red>[SaveManager]</color> Kayıt yüklenirken HATA oluştu, veriler sıfırlanıyor: {e.Message}");
                CurrentData = new SaveData();
            }
        }

        /// <summary>
        /// Tüm kayıtları ve yedekleri tamamen siler.
        /// </summary>
        
        private void OnApplicationQuit()
        {
            SaveGame();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGame();
            }
        }

        public void DeleteSaveData()
        {
            try
            {
                if (File.Exists(_savePath)) File.Delete(_savePath);
                if (File.Exists(_backupPath)) File.Delete(_backupPath);
                if (File.Exists(_tempPath)) File.Delete(_tempPath);
                
                CurrentData = new SaveData(); // Belleği de sıfırla
                Debug.Log("<color=red>[SaveManager]</color> Tüm kayıt ve yedek dosyaları SİLİNDİ! İlerleme sıfırlandı.");
            }
            catch (Exception e)
            {
                Debug.LogError($"<color=red>[SaveManager]</color> Kayıtlar silinirken HATA: {e.Message}");
            }
        }
    }
}
