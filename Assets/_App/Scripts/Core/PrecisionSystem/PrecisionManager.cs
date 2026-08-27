using UnityEngine;
using System;

namespace RecycleRush.Core.PrecisionSystem
{
    /// <summary>
    /// Çöplerin kutuya ne kadar isabetli (merkeze yakın) girdiğini hesaplayan sistem.
    /// </summary>
    public class PrecisionManager : MonoBehaviour
    {
        public static PrecisionManager Instance { get; private set; }

        [SerializeField] private PrecisionSettings _settings;
        public PrecisionSettings Settings => _settings;

        // YENİ: Precision Streak Takibi
        public int CurrentPrecisionStreak { get; private set; }
        public int BestPrecisionStreak { get; private set; }

        // Her isabet hesaplandığında diğer sistemlere haber vermek için Event
        public static event Action<PrecisionResult> OnPrecisionCalculated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        /// <summary>
        /// Kutuya giren bir çöpün hassasiyet skorunu hesaplar.
        /// </summary>
        public PrecisionResult CalculatePrecision(Transform binTransform, Vector3 boundsCenter, float binRadius, Vector3 hitWorldPos, WasteType wasteType, WasteType targetBinType)
        {
            if (_settings == null)
            {
                Debug.LogError("[PrecisionManager] PrecisionSettings atanmamış!");
                return new PrecisionResult();
            }

            // 1. Dünya koordinatını kutunun lokal koordinatına çevir (Döndürme vb. hatalarını engeller)
            Vector3 localHit = binTransform.InverseTransformPoint(hitWorldPos);
            Vector3 localCenter = binTransform.InverseTransformPoint(boundsCenter);

            // 2. Y eksenini (derinliği) görmezden gelerek sadece yüzeydeki (XZ düzlemindeki) mesafeyi ölç
            Vector2 hit2D = new Vector2(localHit.x, localHit.z);
            Vector2 center2D = new Vector2(localCenter.x, localCenter.z);
            float distance = Vector2.Distance(hit2D, center2D);

            // 3. Mesafeyi 0.0 (tam merkez) ile 1.0 (kenar) arasına normalize et
            float normalizedDistance = Mathf.Clamp01(distance / binRadius);

            // 4. AnimationCurve kullanarak skoru hesapla (örneğin merkeze yaklaştıkça skor katlanarak artar)
            float scoreMultiplier = _settings.ScoreCurve.Evaluate(normalizedDistance);
            float finalScore = scoreMultiplier * 100f;

            // 5. Tier (Derece) belirleme
            PrecisionTier tier = PrecisionTier.Normal;
            Vector3Int bonus = _settings.NormalBonus;

            if (finalScore >= _settings.PerfectThreshold)
            {
                tier = PrecisionTier.Perfect;
                bonus = _settings.PerfectBonus;
            }
            else if (finalScore >= _settings.GreatThreshold)
            {
                tier = PrecisionTier.Great;
                bonus = _settings.GreatBonus;
            }
            else if (finalScore >= _settings.GoodThreshold)
            {
                tier = PrecisionTier.Good;
                bonus = _settings.GoodBonus;
            }

            // YENİ: Streak Mantığı
            if (tier == PrecisionTier.Perfect)
            {
                CurrentPrecisionStreak++;
                if (CurrentPrecisionStreak > BestPrecisionStreak)
                {
                    BestPrecisionStreak = CurrentPrecisionStreak;
                }
            }
            else
            {
                // Mükemmel olmayan herhangi bir atış Precision Streak'i sıfırlar
                CurrentPrecisionStreak = 0;
            }

            // 6. Sonuç paketini (Struct) oluştur
            PrecisionResult result = new PrecisionResult
            {
                WasteType = wasteType,
                TargetBinType = targetBinType,
                Tier = tier,
                Score = finalScore,
                Distance = distance,
                NormalizedDistance = normalizedDistance,
                BonusScore = bonus.x,
                BonusXP = bonus.y,
                BonusCoin = bonus.z,
                HitPoint = hitWorldPos
            };

            // 7. Hesaplanan veriyi yayınla (VFX, UI ve Score sistemleri dinleyecek)
            OnPrecisionCalculated?.Invoke(result);

            Debug.Log($"<color=cyan>[PrecisionManager]</color> Atış: {tier} | Skor: {finalScore:F1} | Mesafe: {distance:F2}m");

            return result;
        }
    }
}
