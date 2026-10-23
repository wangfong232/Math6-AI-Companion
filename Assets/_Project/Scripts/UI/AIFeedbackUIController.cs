using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;

namespace Math6Companion.UI
{
    /// <summary>
    /// Điều khiển Scene 08_AIFeedback.unity.
    /// AI Companion ("Thầy Minh") phân tích chi tiết lỗi sai từ trắc nghiệm hoặc tự luận,
    /// giải thích nguyên nhân và đưa ra bài tập tương tự.
    /// </summary>
    public class AIFeedbackUIController : MonoBehaviour
    {
        [Header("Tutor Character & Dialogue")]
        [SerializeField] private TextMeshProUGUI tutorGreetingText;
        [SerializeField] private TextMeshProUGUI questionTitleText;
        [SerializeField] private TextMeshProUGUI diagnosisBodyText;
        [SerializeField] private TextMeshProUGUI ruleReminderText;
        [SerializeField] private TextMeshProUGUI analogousProblemText;

        [Header("Navigation Buttons")]
        [SerializeField] private Button nextWrongQuestionButton;
        [SerializeField] private Button prevWrongQuestionButton;
        [SerializeField] private TextMeshProUGUI pageIndicatorText;

        [Header("System Buttons")]
        [SerializeField] private Button backToScoreButton;
        [SerializeField] private Button backToMenuButton;

        private int currentWrongIndex = 0;

        private void Start()
        {
            if (backToScoreButton != null)
                backToScoreButton.onClick.AddListener(() => NavigateScene("07_Score"));

            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(() => NavigateScene(SessionManager.Instance != null ? SessionManager.Instance.ReturnSceneName : "02_VillageWorld"));

            if (nextWrongQuestionButton != null)
                nextWrongQuestionButton.onClick.AddListener(OnNextClicked);

            if (prevWrongQuestionButton != null)
                prevWrongQuestionButton.onClick.AddListener(OnPrevClicked);

            LoadFeedbackData();
        }

        private void LoadFeedbackData()
        {
            if (ScoreSessionContext.ActivityType == "ESSAY" && !string.IsNullOrEmpty(ScoreSessionContext.EssayFeedback))
            {
                // Phản hồi bài tự luận theo Rubric
                if (tutorGreetingText != null)
                {
                    tutorGreetingText.text = "Thầy Minh đã phân tích bài tự luận của em:";
                }
                if (questionTitleText != null) questionTitleText.text = "Đánh giá theo Rubric từng bước";

                if (diagnosisBodyText != null)
                {
                    diagnosisBodyText.text = ScoreSessionContext.EssayFeedback;
                    var rt = diagnosisBodyText.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(0, 30);
                        rt.sizeDelta = new Vector2(1020, 260);
                    }
                }

                if (ruleReminderText != null)
                {
                    ruleReminderText.gameObject.SetActive(false);
                }

                if (analogousProblemText != null)
                {
                    var rtAnalog = analogousProblemText.GetComponent<RectTransform>();
                    if (rtAnalog != null)
                    {
                        rtAnalog.anchoredPosition = new Vector2(0, -170);
                        rtAnalog.sizeDelta = new Vector2(1020, 60);
                    }
                    analogousProblemText.text = "Bài tương tự: Thử rèn luyện thêm bài tập cùng dạng để nắm vững các bước nhé!";
                    analogousProblemText.gameObject.SetActive(true);
                }

                if (nextWrongQuestionButton != null) nextWrongQuestionButton.gameObject.SetActive(false);
                if (prevWrongQuestionButton != null) prevWrongQuestionButton.gameObject.SetActive(false);
                if (pageIndicatorText != null) pageIndicatorText.text = "Tự luận";
            }
            else if (ScoreSessionContext.WrongQuestionPrompts != null && ScoreSessionContext.WrongQuestionPrompts.Count > 0)
            {
                currentWrongIndex = 0;
                DisplayWrongQuestion(currentWrongIndex);
            }
            else if (!string.IsNullOrEmpty(ScoreSessionContext.EssayFeedback))
            {
                // Fallback nếu có feedback tự luận
                if (tutorGreetingText != null)
                {
                    tutorGreetingText.text = "Thầy Minh đã phân tích bài tự luận của em:";
                }
                if (questionTitleText != null) questionTitleText.text = "Đánh giá theo Rubric từng bước";

                if (diagnosisBodyText != null)
                {
                    diagnosisBodyText.text = ScoreSessionContext.EssayFeedback;
                    var rt = diagnosisBodyText.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(0, 30);
                        rt.sizeDelta = new Vector2(1020, 260);
                    }
                }

                if (ruleReminderText != null)
                {
                    ruleReminderText.gameObject.SetActive(false);
                }

                if (analogousProblemText != null)
                {
                    var rtAnalog = analogousProblemText.GetComponent<RectTransform>();
                    if (rtAnalog != null)
                    {
                        rtAnalog.anchoredPosition = new Vector2(0, -170);
                        rtAnalog.sizeDelta = new Vector2(1020, 60);
                    }
                    analogousProblemText.text = "Bài tương tự: Thử giải biểu thức: 20 - (4 + 3) * 2.";
                    analogousProblemText.gameObject.SetActive(true);
                }

                if (nextWrongQuestionButton != null) nextWrongQuestionButton.gameObject.SetActive(false);
                if (prevWrongQuestionButton != null) prevWrongQuestionButton.gameObject.SetActive(false);
                if (pageIndicatorText != null) pageIndicatorText.text = "Tự luận";
            }
            else
            {
                // Dữ liệu mẫu minh họa khi test trực tiếp trong Unity Editor
                if (tutorGreetingText != null)
                    tutorGreetingText.text = "Chào em! Thầy Minh ở đây để cùng em xem lại các câu cần lưu ý nhé:";

                if (questionTitleText != null)
                    questionTitleText.text = "Câu sai mẫu: Tính giá trị biểu thức: (-15) + 7";

                if (diagnosisBodyText != null)
                    diagnosisBodyText.text = "Nguyên nhân nhầm lẫn: Em đã chọn phương án A (8) vì lấy 15 - 7 nhưng quên đặt dấu '-' của số có giá trị tuyệt đối lớn hơn (-15).";

                if (ruleReminderText != null)
                    ruleReminderText.text = "Quy tắc cốt lõi: Khi cộng hai số nguyên khác dấu, ta lấy số lớn trừ số bé (theo giá trị tuyệt đối) và mang dấu của số lớn hơn: (-15) + 7 = -(15 - 7) = -8.";

                if (analogousProblemText != null)
                    analogousProblemText.text = "Thử làm lại câu tương tự: Tính (-20) + 12 = ?";

                if (pageIndicatorText != null) pageIndicatorText.text = "Câu 1/1";
            }
        }

        private void DisplayWrongQuestion(int index)
        {
            int total = ScoreSessionContext.WrongQuestionPrompts.Count;
            if (index < 0 || index >= total) return;

            string prompt = ScoreSessionContext.WrongQuestionPrompts[index];
            string wrongAns = ScoreSessionContext.WrongAnswers.Count > index ? ScoreSessionContext.WrongAnswers[index] : "";
            string correctAns = ScoreSessionContext.CorrectAnswers.Count > index ? ScoreSessionContext.CorrectAnswers[index] : "";
            string explanation = ScoreSessionContext.Explanations.Count > index ? ScoreSessionContext.Explanations[index] : "";

            if (tutorGreetingText != null)
                tutorGreetingText.text = "Chào em! Thầy Minh hướng dẫn em câu này nhé:";

            if (questionTitleText != null)
                questionTitleText.text = prompt;

            if (diagnosisBodyText != null)
                diagnosisBodyText.text = $"Em đã chọn: '{wrongAns}' (Chưa chính xác).\nĐáp án đúng là: '{correctAns}'.\n\nGiải thích chi tiết: {explanation}";

            if (ruleReminderText != null)
                ruleReminderText.text = "Lưu ý: Đọc kỹ đề bài và đối chiếu với lời giải chi tiết ở trên để nắm vững phương pháp giải nhé!";

            if (analogousProblemText != null)
                analogousProblemText.text = "Em hãy ghi nhớ quy tắc này để không bị nhầm lẫn trong lần làm tiếp theo nhé!";

            if (pageIndicatorText != null)
                pageIndicatorText.text = $"Câu sai {index + 1}/{total}";

            if (prevWrongQuestionButton != null) prevWrongQuestionButton.interactable = index > 0;
            if (nextWrongQuestionButton != null) nextWrongQuestionButton.interactable = index < total - 1;
        }

        private void OnNextClicked()
        {
            PlayClick();
            if (currentWrongIndex < ScoreSessionContext.WrongQuestionPrompts.Count - 1)
            {
                currentWrongIndex++;
                DisplayWrongQuestion(currentWrongIndex);
            }
        }

        private void OnPrevClicked()
        {
            PlayClick();
            if (currentWrongIndex > 0)
            {
                currentWrongIndex--;
                DisplayWrongQuestion(currentWrongIndex);
            }
        }

        private void NavigateScene(string sceneName)
        {
            PlayClick();
            SceneManager.LoadScene(sceneName);
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
