using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;

namespace Math6Companion.UI
{
    /// <summary>
    /// Dữ liệu truyền sang Scene Điểm (07_Score.unity)
    /// </summary>
    public static class ScoreSessionContext
    {
        public static string ActivityType = "MCQ"; // "MCQ", "ESSAY", "VERSUS"
        public static int Score = 8;
        public static int MaxScore = 10;
        public static int TimeSpentSeconds = 45;
        public static int CorrectCount = 8;
        public static int TotalQuestions = 10;
        public static List<string> WrongQuestionPrompts = new List<string>();
        public static List<string> WrongAnswers = new List<string>();
        public static List<string> CorrectAnswers = new List<string>();
        public static List<string> Explanations = new List<string>();
        public static bool IsVersusMode = false;
        public static bool IsWinner = true;
        public static string OpponentNickname = "BảoToánHọc";
        public static int OpponentScore = 6;
        public static string EssayFeedback = "";

        public static void Reset()
        {
            ActivityType = "MCQ";
            Score = 0;
            MaxScore = 10;
            TimeSpentSeconds = 0;
            CorrectCount = 0;
            TotalQuestions = 0;
            WrongQuestionPrompts.Clear();
            WrongAnswers.Clear();
            CorrectAnswers.Clear();
            Explanations.Clear();
            IsVersusMode = false;
            IsWinner = false;
            OpponentNickname = "";
            OpponentScore = 0;
            EssayFeedback = "";
        }
    }

    /// <summary>
    /// Điều khiển Scene Điểm dùng chung (07_Score.unity).
    /// Tiếp nhận và hiển thị kết quả từ Trắc nghiệm, Tự luận và Đối kháng.
    /// </summary>
    public class ScoreUIController : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreBigText;
        [SerializeField] private TextMeshProUGUI scoreDetailText;
        [SerializeField] private TextMeshProUGUI timeSpentText;
        [SerializeField] private Slider scoreCircleSlider;

        [Header("Versus Mode Banner")]
        [SerializeField] private GameObject versusBannerPanel;
        [SerializeField] private TextMeshProUGUI versusResultText;
        [SerializeField] private TextMeshProUGUI versusCompareText;

        [Header("Action Buttons")]
        [SerializeField] private Button aiFeedbackButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button backToMenuButton;

        private void Start()
        {
            if (aiFeedbackButton != null)
                aiFeedbackButton.onClick.AddListener(OnAIFeedbackClicked);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);

            DisplayScoreData();
        }

        private void DisplayScoreData()
        {
            // Hiển thị điểm số
            if (scoreBigText != null)
            {
                scoreBigText.text = $"{ScoreSessionContext.Score}/{ScoreSessionContext.MaxScore}";
            }

            if (scoreCircleSlider != null)
            {
                float ratio = (float)ScoreSessionContext.Score / Mathf.Max(1, ScoreSessionContext.MaxScore);
                scoreCircleSlider.value = ratio;
            }

            if (scoreDetailText != null)
            {
                if (ScoreSessionContext.ActivityType == "MCQ")
                {
                    scoreDetailText.text = $"Số câu đúng: {ScoreSessionContext.CorrectCount}/{ScoreSessionContext.TotalQuestions} câu";
                }
                else if (ScoreSessionContext.ActivityType == "ESSAY")
                {
                    scoreDetailText.text = "Bài làm tự luận đã được AI chấm theo Rubric";
                }
                else
                {
                    scoreDetailText.text = "Thi đấu đối kháng 3 phút";
                }
            }

            if (timeSpentText != null)
            {
                int minutes = ScoreSessionContext.TimeSpentSeconds / 60;
                int seconds = ScoreSessionContext.TimeSpentSeconds % 60;
                timeSpentText.text = $"Thời gian hoàn thành: {minutes:D2}:{seconds:D2}";
            }

            // Xử lý hiển thị chế độ Đối kháng (Versus)
            if (versusBannerPanel != null)
            {
                versusBannerPanel.SetActive(ScoreSessionContext.IsVersusMode);
                if (ScoreSessionContext.IsVersusMode)
                {
                    if (versusResultText != null)
                    {
                        versusResultText.text = ScoreSessionContext.IsWinner ? "CHIẾN THẮNG (WINNER)!" : "THẤT BẠI (DEFEAT)";
                        versusResultText.color = ScoreSessionContext.IsWinner ? Color.green : Color.red;
                    }

                    if (versusCompareText != null)
                    {
                        versusCompareText.text = $"Bạn: {ScoreSessionContext.Score} điểm  VS  {ScoreSessionContext.OpponentNickname}: {ScoreSessionContext.OpponentScore} điểm";
                    }
                }
            }

            // Nút AI gợi ý chỉ hiện hoặc sáng lên khi có câu sai hoặc bài tự luận
            if (aiFeedbackButton != null)
            {
                bool hasFeedback = ScoreSessionContext.WrongQuestionPrompts.Count > 0 || !string.IsNullOrEmpty(ScoreSessionContext.EssayFeedback);
                aiFeedbackButton.interactable = hasFeedback;
            }

            // Âm thanh chúc mừng
            if (AudioManager.Instance != null)
            {
                if (ScoreSessionContext.Score >= ScoreSessionContext.MaxScore * 0.8f)
                {
                    AudioManager.Instance.PlayCorrectSound();
                }
            }
        }

        private void OnAIFeedbackClicked()
        {
            PlayClick();
            SceneManager.LoadScene("08_AIFeedback");
        }

        private void OnRetryClicked()
        {
            PlayClick();
            if (ScoreSessionContext.ActivityType == "ESSAY")
            {
                SceneManager.LoadScene("06_EssayPractice");
            }
            else
            {
                SceneManager.LoadScene("05_MCQPractice");
            }
        }

        private void OnBackToMenuClicked()
        {
            PlayClick();
            SceneManager.LoadScene("02_MainMenu");
        }

        private void PlayClick()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }
    }
}
