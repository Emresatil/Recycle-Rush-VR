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
            {
                string grade = string.IsNullOrEmpty(report.PerformanceGrade) ? "-" : report.PerformanceGrade;
                string subtitle = "";
                
                // Translated subtitles
                if (grade == "Eco Legend") subtitle = "\n<size=40%>Earned with 90+ Performance Score!</size>";
                else if (grade == "Master Recycler") subtitle = "\n<size=40%>Earned with 80+ Performance Score.</size>";
                else if (grade == "Green Worker") subtitle = "\n<size=40%>Earned with 70+ Performance Score.</size>";
                else if (grade == "Clean Rookie") subtitle = "\n<size=40%>Earned with 50+ Performance Score.</size>";
                else if (grade == "Beginner Collector") subtitle = "\n<size=40%>Welcome to the game!</size>";
                
                _gradeText.text = $"{grade}{subtitle}";
            }

            if (_scoreText != null)
            {
                int totalScore = report.BaseScore + report.ComboBonusScore + report.GoldenWasteBonusScore - report.PenaltyScore;
                string deltaStr = report.ScoreDelta > 0 ? $"<color=green>(+{report.ScoreDelta})</color>" : $"<color=red>({report.ScoreDelta})</color>";
                _scoreText.text = $"Score: {totalScore} {deltaStr}";
            }

            if (_accuracyText != null)
            {
                _accuracyText.text = $"Accuracy: {report.AccuracyPercentage:F1}%";
            }

            if (_medalsText != null)
            {
                if (report.EarnedMedals != null && report.EarnedMedals.Count > 0)
                {
                    _medalsText.text = "<color=#FACC15>Medals earned</color>\n\n• " + string.Join("\n• ", report.EarnedMedals);
                }
                else
                {
                    _medalsText.text = "<color=#FACC15>Medals earned</color>\n\n• None";
                }
            }

            if (_economyText != null)
            {
                _economyText.text = $"XP gained: <color=green>+{report.EarnedXP}</color>\nCoins earned: <color=#FACC15>+{report.EarnedCoins}</color>";
            }

            if (_nextGoalText != null)
            {
                _nextGoalText.text = $"Next goal: {report.SuggestedNextGoal}";
            }

            // --- AUDIO HOOKS ---
            if (AudioManager.Instance != null)
            {
                // Zafer/Rütbe sesi
                AudioManager.Instance.PlayLevelUpFanfare();
                
                // Para Sesi (Oyuncu menüdeyken merkezde çalsın)
                if (report.EarnedCoins > 0)
                {
                    AudioManager.Instance.PlayCoinCollectSound(Vector3.zero);
                }
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
