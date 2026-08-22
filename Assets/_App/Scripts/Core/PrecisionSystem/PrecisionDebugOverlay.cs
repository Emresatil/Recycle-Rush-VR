using UnityEngine;

namespace RecycleRush.Core.PrecisionSystem
{
    /// <summary>
    /// Geliştirici modu açıkken oyun ekranında Precision (Hassasiyet) verilerini canlı gösteren hafif bir araç.
    /// Scene'de herhangi bir objeye eklenerek kullanılabilir.
    /// </summary>
    public class PrecisionDebugOverlay : MonoBehaviour
    {
        [Tooltip("Oyun içi geliştirici modu kapalıysa overlay gizlenir.")]
        public bool ShowOverlay = true;

        private PrecisionResult _lastResult;
        private int _totalThrows;
        private float _averagePrecision;
        private float _consistency;

        private void OnEnable()
        {
            PrecisionManager.OnPrecisionCalculated += HandlePrecision;
        }

        private void OnDisable()
        {
            PrecisionManager.OnPrecisionCalculated -= HandlePrecision;
        }

        private void Update()
        {
            // YENİ: Yeni Input System ile F3 tuşu kontrolü
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f3Key.wasPressedThisFrame)
            {
                ShowOverlay = !ShowOverlay;
            }
        }

        private void HandlePrecision(PrecisionResult result)
        {
            _lastResult = result;
            
            // Eğer GameManager ve ScoreManager hayattaysa canlı istatistikleri çek
            if (global::GameManager.Instance != null)
            {
                var session = global::GameManager.Instance.CurrentSession;
                _totalThrows = session.TotalCorrectThrows;
                _averagePrecision = session.AveragePrecision;
                _consistency = session.PrecisionConsistency;
            }
        }

        private void OnGUI()
        {
            if (!ShowOverlay) return;
            if (PrecisionManager.Instance == null) return;

            // Arka plan paneli (Taşmayı önlemek için yükseklik 160'dan 200'e çıkarıldı)
            GUI.Box(new Rect(10, 10, 250, 200), "Precision Debug (F3 to Toggle)");

            // İçerik alanı genişletildi
            GUILayout.BeginArea(new Rect(20, 40, 230, 180));
            
            // Son atış bilgileri
            GUILayout.Label($"Last Tier: {_lastResult.Tier}");
            GUILayout.Label($"Last Score: {_lastResult.Score:F1}");
            GUILayout.Label($"Distance: {_lastResult.Distance:F2}m (Norm: {_lastResult.NormalizedDistance:F2})");
            GUILayout.Label($"Target Bin: {_lastResult.TargetBinType}");

            GUILayout.Space(10);

            // Genel oturum bilgileri
            GUILayout.Label($"Avg Precision: {_averagePrecision:F1}%");
            GUILayout.Label($"Consistency: {_consistency:F1}%");
            GUILayout.Label($"Streak: {PrecisionManager.Instance.CurrentPrecisionStreak}");

            GUILayout.EndArea();
        }
    }
}
