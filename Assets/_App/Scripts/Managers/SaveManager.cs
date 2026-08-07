using UnityEngine;
using System.IO;

namespace RecycleRush.Managers
{
    /// <summary>
    /// Oyuncu verilerinin kaydedilip yüklendiği veri modeli (JSON'a çevrilecek kısım).
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        public int Level = 1;
        public int XP = 0;
        public int Coins = 0;
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

        private void Awake()
        {
            // Singleton Kurulumu
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Unity'nin cihazlarda (Android/Windows) veri kaydetmek için ayırdığı özel güvenli klasör
            _savePath = Path.Combine(Application.persistentDataPath, "RecycleRushSave.json");
            
            // Oyun açıldığında veriyi hemen yükle
            LoadGame();
        }

        /// <summary>
        /// Mevcut veriyi JSON formatına çevirip diske yazar.
        /// </summary>
        public void SaveGame()
        {
            string json = JsonUtility.ToJson(CurrentData, true); // true = Pretty Print (Okunabilir format)
            File.WriteAllText(_savePath, json);
            Debug.Log($"<color=green>[SaveManager]</color> Oyun başarıyla kaydedildi: {_savePath}");
        }

        /// <summary>
        /// Dosya varsa okur ve Deserialize eder, yoksa sıfırdan yeni veri oluşturur.
        /// </summary>
        public void LoadGame()
        {
            if (File.Exists(_savePath))
            {
                string json = File.ReadAllText(_savePath);
                CurrentData = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("<color=cyan>[SaveManager]</color> Kayıtlı veri başarıyla yüklendi.");
            }
            else
            {
                // Daha önce hiç oyun oynanmamışsa veya silinmişse sıfır veri oluştur.
                CurrentData = new SaveData();
                Debug.Log("<color=yellow>[SaveManager]</color> Kayıt dosyası bulunamadı, yeni (Sıfır) veri oluşturuldu.");
            }
        }

        /// <summary>
        /// (İsteğe Bağlı) Oyunu sıfırlamak için kullanılır.
        /// </summary>
        public void DeleteSaveData()
        {
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
                CurrentData = new SaveData(); // Belleği de sıfırla
                Debug.Log("<color=red>[SaveManager]</color> Kayıt dosyası silindi! İlerleme sıfırlandı.");
            }
        }
    }
}
