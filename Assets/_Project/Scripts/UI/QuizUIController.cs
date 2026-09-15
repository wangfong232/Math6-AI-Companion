using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Math6Companion.Core.Contracts;
using Math6Companion.Core.Mock;

namespace Math6Companion.UI
{
    public class QuizUIController : MonoBehaviour
    {
        [Header("Header Info")]
        [SerializeField] private TextMeshProUGUI topicTitleText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Question Body")]
        [SerializeField] private TextMeshProUGUI promptText;

        [Header("Option Buttons")]
        [SerializeField] private Button[] optionButtons; // 4 buttons: 0->A, 1->B, 2->C, 3->D
        [SerializeField] private TextMeshProUGUI[] optionTexts;

        [Header("Feedback & Controls")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private Button nextButton;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultScoreText;
        [SerializeField] private Button closeQuizButton;

        [Header("Quiz Settings")]
        [SerializeField] private float timePerQuestion = 30f;

        private List<QuestionData> questionList = new List<QuestionData>();
        private int currentQuestionIndex = 0;
        private int correctCount = 0;
        private float remainingTime;
        private bool isAnswering = false;
        private INetworkService networkService;

        private readonly string[] optionKeys = { "A", "B", "C", "D" };

        private void Awake()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }

            if (closeQuizButton != null)
            {
                closeQuizButton.onClick.AddListener(CloseQuiz);
            }
        }

        private void Start()
        {
            networkService = MockNetworkService.Instance ?? FindAnyObjectByType<MockNetworkService>();
            LoadQuizData("Integers", "6");
        }

        public void LoadQuizData(string topic, string grade)
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            if (feedbackText != null) feedbackText.text = string.Empty;
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            networkService.GetQuestions(topic, grade, (success, questions, message) =>
            {
                if (success && questions != null && questions.Count > 0)
                {
                    questionList = questions;
                    currentQuestionIndex = 0;
                    correctCount = 0;
                    DisplayQuestion(currentQuestionIndex);
                }
                else
                {
                    if (promptText != null) promptText.text = "Không thể tải danh sách câu hỏi!";
                }
            });
        }

        private void DisplayQuestion(int index)
        {
            if (index < 0 || index >= questionList.Count) return;

            var q = questionList[index];
            isAnswering = true;
            remainingTime = timePerQuestion;

            if (topicTitleText != null) topicTitleText.text = $"Chủ đề: {q.topic} ({q.difficulty})";
            if (progressText != null) progressText.text = $"Câu {index + 1}/{questionList.Count}";
            if (promptText != null) promptText.text = q.prompt;
            if (feedbackText != null) feedbackText.text = string.Empty;
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            // Gán dữ liệu 4 đáp án A, B, C, D
            string[] answers = { q.options.A, q.options.B, q.options.C, q.options.D };
            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i].interactable = true;
                optionButtons[i].image.color = Color.white;
                if (i < answers.Length && optionTexts[i] != null)
                {
                    optionTexts[i].text = $"{optionKeys[i]}. {answers[i]}";
                }
            }

            StopAllCoroutines();
            StartCoroutine(TimerCountdown());
        }

        private IEnumerator TimerCountdown()
        {
            while (remainingTime > 0 && isAnswering)
            {
                remainingTime -= Time.deltaTime;
                if (timerText != null)
                {
                    timerText.text = $"Thời gian: {Mathf.CeilToInt(remainingTime)}s";
                    timerText.color = remainingTime <= 5f ? Color.red : Color.white;
                }
                yield return null;
            }

            if (isAnswering)
            {
                OnTimeOut();
            }
        }

        private void OnTimeOut()
        {
            isAnswering = false;
            LockAllOptions();
            if (feedbackText != null)
            {
                feedbackText.text = $"Hết giờ! Đáp án đúng là {questionList[currentQuestionIndex].correctAnswer}.";
                feedbackText.color = Color.yellow;
            }
            if (nextButton != null) nextButton.gameObject.SetActive(true);
        }

        private void OnOptionSelected(int index)
        {
            if (!isAnswering) return;
            isAnswering = false;
            LockAllOptions();

            string chosen = optionKeys[index];
            string correct = questionList[currentQuestionIndex].correctAnswer;

            if (chosen == correct)
            {
                correctCount++;
                optionButtons[index].image.color = new Color(0.4f, 0.9f, 0.4f); // Xanh lá
                if (feedbackText != null)
                {
                    feedbackText.text = "Chính xác! " + questionList[currentQuestionIndex].explanation;
                    feedbackText.color = Color.green;
                }
            }
            else
            {
                optionButtons[index].image.color = new Color(1f, 0.4f, 0.4f); // Đỏ
                HighlightCorrectButton(correct);
                if (feedbackText != null)
                {
                    feedbackText.text = $"Sai rồi! {questionList[currentQuestionIndex].explanation}";
                    feedbackText.color = Color.red;
                }
            }

            if (nextButton != null) nextButton.gameObject.SetActive(true);
        }

        private void HighlightCorrectButton(string correctKey)
        {
            for (int i = 0; i < optionKeys.Length; i++)
            {
                if (optionKeys[i] == correctKey)
                {
                    optionButtons[i].image.color = new Color(0.4f, 0.9f, 0.4f);
                    break;
                }
            }
        }

        private void LockAllOptions()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i].interactable = false;
            }
        }

        private void OnNextButtonClicked()
        {
            currentQuestionIndex++;
            if (currentQuestionIndex < questionList.Count)
            {
                DisplayQuestion(currentQuestionIndex);
            }
            else
            {
                CompleteQuiz();
            }
        }

        private void CompleteQuiz()
        {
            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultScoreText != null)
            {
                resultScoreText.text = $"Hoàn thành bài tập!\nĐiểm số: {correctCount}/{questionList.Count}";
            }

            // Gửi kết quả qua MockNetworkService
            ResultSubmission submission = new ResultSubmission
            {
                userId = PlayerPrefs.GetString("User_ID", "STU_001"),
                lessonId = "LES_INT_01",
                activityType = "MCQ",
                score = correctCount * 10,
                maxScore = questionList.Count * 10,
                timeSpentSeconds = 60,
                mistakesCount = questionList.Count - correctCount
            };

            networkService.SaveResult(submission, (success, badges, msg) =>
            {
                Debug.Log("Kết quả Quiz đã được lưu vào hệ thống!");
            });
        }

        public void CloseQuiz()
        {
            gameObject.SetActive(false);
        }
    }
}