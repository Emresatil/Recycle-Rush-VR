using UnityEngine;
using System;
using RecycleRush.Core; // ScoreManager için gerekli namespace

public class DifficultyManager : MonoBehaviour
{
    // Singleton Kurulumu
    public static DifficultyManager Instance { get; private set; }

    [Header("Zorluk Çarpanları")]
    [Tooltip("Sırasıyla Seviye 0, 1, 2 ve 3 (Arcade) hız/süre çarpanları.")]
    [SerializeField] private float[] _difficultyMultipliers = new float[] { 1.0f, 1.2f, 1.5f, 2.0f };

    // Sadece okunabilir mevcut seviye değişkeni
    public int CurrentLevel { get; private set; } = 0;

    // Sistemlere (Bant ve Spawner) fırlatılacak Event
    // Float değeri, o anki zorluğun Hız çarpanıdır (Örn: 1.5)
    public static event Action<float> OnDifficultyLevelChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // ScoreManager sahnede hazır olduğunda Event'ine abone ol (Gevşek Bağlılık)
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
        }
        else
        {
            Debug.LogWarning("[DifficultyManager] Sahnedeki ScoreManager bulunamadı! Zorluk sistemi puanı takip edemeyecek.");
        }
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
        }
    }

    /// <summary>
    /// Puan her değiştiğinde bu fonksiyon tetiklenir ve seviye kontrolü yapar.
    /// </summary>
    private void HandleScoreChanged(int newScore)
    {
        int newLevel = 0;

        // Planda belirtilen puan eşikleri:
        if (newScore >= 200) newLevel = 3;
        else if (newScore >= 100) newLevel = 2;
        else if (newScore >= 50) newLevel = 1;

        // Eğer seviye gerçekten değiştiyse sistemi uyar
        if (newLevel != CurrentLevel)
        {
            CurrentLevel = newLevel;
            
            // Dizinin (Array) dışına çıkmayı engellemek için Clamp ile sınırlandırıyoruz
            float multiplier = _difficultyMultipliers[Mathf.Clamp(CurrentLevel, 0, _difficultyMultipliers.Length - 1)];
            
            Debug.Log($"<color=orange>[DifficultyManager]</color> Seviye Atlandı! Yeni Seviye: {CurrentLevel} | Yeni Hız Çarpanı: {multiplier}x");
            
            // Yeni çarpanı bandı ve spawner'ı uyarmak için fırlatıyoruz
            OnDifficultyLevelChanged?.Invoke(multiplier);
        }
    }
}
