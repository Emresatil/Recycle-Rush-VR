using UnityEngine;
using TMPro;
using RecycleRush.Managers;

namespace RecycleRush.UI
{
    public class EndgamePanelController : MonoBehaviour
    {
        [Header("Panel Konteyneri")]
        [Tooltip("Paneli açıp kapatmak için ana obje")]
        [SerializeField] private GameObject _panelContainer;

        [Header("Metin Referansları (TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI _highlightText;
        [SerializeField] private TextMeshProUGUI _gradeText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _accuracyText;
        [SerializeField] private TextMeshProUGUI _medalsText;
        [SerializeField] private TextMeshProUGUI _economyText;
        [SerializeField] private TextMeshProUGUI _nextGoalText;

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }
        
        private void Start()
        {
            // Başlangıçta paneli gizle
            if (_panelContainer != null) _panelContainer.SetActive(false);
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.GameOver)
            {
                ShowEndgameReport();
            }
            else if (newState == GameState.Playing)
            {
                // Yeni oyun başladığında paneli kapat
                if (_panelContainer != null) _panelContainer.SetActive(false);
            }
        }

        private void ShowEndgameReport()
        {
            if (GameManager.Instance == null) return;

            // Arka planda dondurduğumuz o mükemmel snapshot raporunu alıyoruz
            var report = GameManager.Instance.FinalSessionReport;

            // Paneli Aktif Et
            if (_panelContainer != null) _panelContainer.SetActive(true);

            // Verileri UI'a Basma
            if (_highlightText != null) 
                _highlightText.text = string.IsNullOrEmpty(report.SessionHighlight) ? "" : report.SessionHighlight;

            if (_gradeText != null) 
                _gradeText.text = string.IsNullOrEmpty(report.PerformanceGrade) ? "-" : report.PerformanceGrade;

            if (_scoreText != null)
            {
                // Toplam skoru kırılımlardan hesaplayıp (veya BaseXP * 10 diyerek) ekrana basıyoruz
                int totalScore = report.BaseScore + report.ComboBonusScore + report.GoldenWasteBonusScore - report.PenaltyScore;
                string deltaStr = report.ScoreDelta > 0 ? $"<color=green>(+{report.ScoreDelta})</color>" : $"<color=red>({report.ScoreDelta})</color>";
                _scoreText.text = $"Score: {totalScore} {deltaStr}";
            }

            if (_accuracyText != null)
            {
                _accuracyText.text = $"Accuracy: %{report.AccuracyPercentage:F1}";
            }

            if (_medalsText != null)
            {
                if (report.EarnedMedals != null && report.EarnedMedals.Count > 0)
                {
                    _medalsText.text = "Medals:\n" + string.Join("\n", report.EarnedMedals);
                }
                else
                {
                    _medalsText.text = "No Medals Earned";
                }
            }

            if (_economyText != null)
            {
                _economyText.text = $"+{report.EarnedXP} XP  |  +{report.EarnedCoins} Coin";
            }

            if (_nextGoalText != null)
            {
                _nextGoalText.text = $"Next Goal: {report.SuggestedNextGoal}";
            }
        }

        // Bu fonksiyonu "Tekrar Oyna" (Restart) butonunun OnClick eventine bağlayabilirsin
        public void OnClickRestart()
        {
            if (_panelContainer != null) _panelContainer.SetActive(false);
            // Burada sahneyi yeniden yükleyebilir veya GameManager'a restart komutu gönderebilirsin.
            // Örn: GameManager.Instance.StartGame();
        }
    }
}
