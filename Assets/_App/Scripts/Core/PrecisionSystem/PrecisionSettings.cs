using UnityEngine;

namespace RecycleRush.Core.PrecisionSystem
{
    [CreateAssetMenu(fileName = "PrecisionSettings", menuName = "Recycle Rush/Precision Settings")]
    public class PrecisionSettings : ScriptableObject
    {
        [Header("Meta Data")]
        [Tooltip("Bu ayarların versiyonu (Dengeleme güncellemelerinde analytics verilerini ayırmak için)")]
        public string SettingsVersion = "1.0.0";

        [Header("Precision Score Curve")]
        [Tooltip("X: Normalized Distance (0=Merkez, 1=Kenar), Y: Precision Score Çarpanı (0=Düşük, 1=Yüksek)")]
        public AnimationCurve ScoreCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f)
        );

        [Header("Thresholds (Minimum Score for Tier)")]
        [Range(0, 100)] public float PerfectThreshold = 95f;
        [Range(0, 100)] public float GreatThreshold = 80f;
        [Range(0, 100)] public float GoodThreshold = 60f;

        [Header("Calibration Gizmo Rings (0.0 to 1.0)")]
        [Tooltip("Gizmo'da (Sahne ekranında) Perfect halkasının kutu çapına oranı")]
        public float PerfectRadiusPercent = 0.2f;
        public float GreatRadiusPercent = 0.5f;
        public float GoodRadiusPercent = 0.8f;

        [Header("Bonuses (Score, XP, Coin)")]
        [Tooltip("Perfect isabetinde verilecek ekstra bonuslar")]
        public Vector3Int PerfectBonus = new Vector3Int(100, 10, 5);
        [Tooltip("Great isabetinde verilecek ekstra bonuslar")]
        public Vector3Int GreatBonus = new Vector3Int(70, 5, 2);
        [Tooltip("Good isabetinde verilecek ekstra bonuslar")]
        public Vector3Int GoodBonus = new Vector3Int(50, 2, 0);
        [Tooltip("Normal isabetinde verilecek ekstra bonuslar")]
        public Vector3Int NormalBonus = new Vector3Int(0, 0, 0);

        [Header("UI Visuals")]
        public Color PerfectColor = new Color(1f, 0.84f, 0f); // Altın
        public Color GreatColor = new Color(0.13f, 0.59f, 0.95f); // Mavi
        public Color GoodColor = new Color(0.3f, 0.8f, 0.3f); // Yeşil
        public Color NormalColor = Color.white; // Beyaz
    }
}
