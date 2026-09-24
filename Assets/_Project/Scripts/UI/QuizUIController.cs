using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
        private readonly Color normalOptionColor = new Color(0.2f, 0.25f, 0.35f);

        private void Awake()
        {
            // Tự động liên kết TextMeshProUGUI bên trong nút nếu chưa kéo thả
            if (optionTexts == null || optionTexts.Length != optionButtons.Length)
            {
                optionTexts = new TextMeshProUGUI[optionButtons.Length];
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                if (optionButtons[i] != null)
                {
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));

                    // Tắt Navigation tránh EventSystem tự chọn làm trắng đáp án đầu tiên
                    var nav = optionButtons[i].navigation;
                    nav.mode = Navigation.Mode.None;
                    optionButtons[i].navigation = nav;

                    if (optionTexts[i] == null)
                    {
                        optionTexts[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    }
                }
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
            if (closeQuizButton == null)
            {
                var foundBtn = GameObject.Find("CloseQuizButton") ?? GameObject.Find("Btn_CloseQuiz");
                if (foundBtn != null) closeQuizButton = foundBtn.GetComponent<Button>();
            }

            if (closeQuizButton != null)
            {
                closeQuizButton.onClick.RemoveListener(CloseQuiz);
                closeQuizButton.onClick.AddListener(CloseQuiz);
            }

            networkService = MockNetworkService.Instance ?? FindAnyObjectByType<MockNetworkService>();
            LoadQuizData("Integers", "6");
        }

        public void LoadQuizData(string topic, string grade)
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            if (feedbackText != null) feedbackText.text = string.Empty;
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            currentQuestionIndex = 0;
            correctCount = 0;
            ScoreSessionContext.Reset();

            if (networkService != null)
            {
                networkService.GetQuestions(topic, grade, (success, questions, msg) =>
                {
                    if (success && questions != null && questions.Count > 0)
                    {
                        questionList = questions;
                    }
                    else
                    {
                        LoadDefaultQuestions();
                    }
                    DisplayQuestion(0);
                });
            }
            else
            {
                LoadDefaultQuestions();
                DisplayQuestion(0);
            }
        }

        private void LoadDefaultQuestions()
        {
            questionList = new List<QuestionData>
            {
                new QuestionData
                {
                    questionId = "Q01",
                    topic = "Integers",
                    type = "MCQ",
                    difficulty = "EASY",
                    prompt = "Tính giá trị của phép tính: (-15) + (-8) = ?",
                    options = new QuestionOptionMap { A = "-23", B = "23", C = "-7", D = "7" },
                    correctAnswer = "A",
                    explanation = "Cộng hai số nguyên cùng dấu âm: ta cộng hai giá trị tuyệt đối rồi đặt dấu '-' phía trước: -(15 + 8) = -23."
                },
                new QuestionData
                {
                    questionId = "Q02",
                    topic = "Integers",
                    type = "MCQ",
                    difficulty = "MEDIUM",
                    prompt = "Tính giá trị biểu thức: 12 - (-5) = ?",
                    options = new QuestionOptionMap { A = "7", B = "-17", C = "17", D = "-7" },
                    correctAnswer = "C",
                    explanation = "Trừ cho số âm tương đương với cộng số đối: 12 - (-5) = 12 + 5 = 17."
                },
                new QuestionData
                {
                    questionId = "Q03",
                    topic = "Integers",
                    type = "MCQ",
                    difficulty = "HARD",
                    prompt = "Nhiệt độ buổi sáng ở Sa Pa là -2°C, đến trưa tăng thêm 5°C. Hỏi nhiệt độ buổi trưa là bao nhiêu?",
                    options = new QuestionOptionMap { A = "-7°C", B = "3°C", C = "-3°C", D = "7°C" },
                    correctAnswer = "B",
                    explanation = "Nhiệt độ buổi trưa = (-2) + 5 = 3°C."
                }
            };
        }

        private void DisplayQuestion(int index)
        {
            if (index < 0 || index >= questionList.Count) return;

            isAnswering = false;
            QuestionData q = questionList[index];

            if (topicTitleText != null)
                topicTitleText.text = "Chủ đề: Số Nguyên";

            if (progressText != null)
                progressText.text = $"Câu {index + 1} / {questionList.Count}";

            if (promptText != null)
                promptText.text = q.prompt;

            if (feedbackText != null)
                feedbackText.text = string.Empty;

            if (nextButton != null)
                nextButton.gameObject.SetActive(false);

            ResetOptionButtons();

            string[] answers = q.options != null
                ? new string[] { q.options.A, q.options.B, q.options.C, q.options.D }
                : new string[] { "", "", "", "" };

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < answers.Length && !string.IsNullOrEmpty(answers[i]))
                {
                    optionButtons[i].gameObject.SetActive(true);
                    if (optionTexts[i] != null)
                    {
                        optionTexts[i].text = $"{optionKeys[i]}. {answers[i]}";
                    }
                    optionButtons[i].interactable = true;
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            StartCoroutine(QuestionTimerRoutine());
        }

        private IEnumerator QuestionTimerRoutine()
        {
            remainingTime = timePerQuestion;

            while (remainingTime > 0)
            {
                if (isAnswering) yield break;

                remainingTime -= Time.deltaTime;
                if (timerText != null)
                {
                    timerText.text = $"Thời gian: {Mathf.CeilToInt(remainingTime)}s";
                }
                yield return null;
            }

            OnTimeOut();
        }

        private void OnTimeOut()
        {
            if (isAnswering) return;
            isAnswering = true;

            LockAllOptions();

            if (feedbackText != null)
            {
                feedbackText.text = "Hết giờ! " + questionList[currentQuestionIndex].explanation;
                feedbackText.color = Color.yellow;
            }

            HighlightCorrectButton(questionList[currentQuestionIndex].correctAnswer);

            if (nextButton != null)
                nextButton.gameObject.SetActive(true);
        }

        private void OnOptionSelected(int index)
        {
            if (isAnswering) return;
            isAnswering = true;

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

                // Ghi nhận câu sai vào Session để AI Thầy Minh phân tích trong Scene 08_AIFeedback
                ScoreSessionContext.WrongQuestionPrompts.Add(questionList[currentQuestionIndex].prompt);
                ScoreSessionContext.WrongAnswers.Add(chosen);
                ScoreSessionContext.CorrectAnswers.Add(correct);
                ScoreSessionContext.Explanations.Add(questionList[currentQuestionIndex].explanation);

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

        private void ResetOptionButtons()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i].image.color = normalOptionColor;
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

        private bool isQuizCompleted = false;

        private void CompleteQuiz()
        {
            if (isQuizCompleted) return;
            isQuizCompleted = true;
            StopAllCoroutines();

            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultScoreText != null)
            {
                resultScoreText.text = $"Hoàn thành bài tập!\nĐiểm số: {correctCount}/{questionList.Count}";
            }

            // Ghi dữ liệu vào Session Score dùng chung
            int totalQ = (questionList != null && questionList.Count > 0) ? questionList.Count : 3;
            ScoreSessionContext.ActivityType = "MCQ";
            ScoreSessionContext.Score = correctCount * 10;
            ScoreSessionContext.MaxScore = totalQ * 10;
            ScoreSessionContext.CorrectCount = correctCount;
            ScoreSessionContext.TotalQuestions = totalQ;
            ScoreSessionContext.TimeSpentSeconds = 60;
            ScoreSessionContext.IsVersusMode = false;

            // Gửi kết quả qua MockNetworkService
            ResultSubmission submission = new ResultSubmission
            {
                userId = PlayerPrefs.GetString("User_ID", "STU_001"),
                lessonId = PlayerPrefs.GetString("Current_Lesson_ID", "LES_INT_01"),
                activityType = "MCQ",
                score = ScoreSessionContext.Score,
                maxScore = ScoreSessionContext.MaxScore,
                timeSpentSeconds = 60,
                mistakesCount = totalQ - correctCount
            };

            bool navigated = false;
            void NavigateToScore()
            {
                if (navigated) return;
                navigated = true;
                Debug.Log("[QuizUIController] Nộp bài thành công! Đang chuyển sang Scene 07_Score...");
                SceneManager.LoadScene("07_Score");
            }

            // Fallback an toàn: Dù mạng phản hồi hay không, tối đa 0.4s sẽ chuyển sang Scene Điểm
            StartCoroutine(SafetyScoreTransition(NavigateToScore));

            if (networkService != null)
            {
                try
                {
                    networkService.SaveResult(submission, (success, badges, msg) =>
                    {
                        NavigateToScore();
                    });
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[QuizUIController] SaveResult exception: " + ex.Message);
                    NavigateToScore();
                }
            }
            else
            {
                NavigateToScore();
            }
        }

        private IEnumerator SafetyScoreTransition(System.Action onNavigate)
        {
            yield return new WaitForSeconds(0.4f);
            onNavigate?.Invoke();
        }

        public void CloseQuiz()
        {
            // Nộp bài sớm: Lập tức dừng làm bài, hiển thị trạng thái và chuyển sang Scene 07_Score
            if (closeQuizButton != null)
            {
                closeQuizButton.interactable = false;
                var btnTxt = closeQuizButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnTxt != null) btnTxt.text = "Đang nộp...";
            }

            CompleteQuiz();
        }
    }
}