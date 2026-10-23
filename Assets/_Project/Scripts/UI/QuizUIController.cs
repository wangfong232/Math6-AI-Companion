using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;
using Math6Companion.Core.Contracts;
using Math6Companion.Core.Mock;
using Math6Companion.Core.Services;

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
        [SerializeField] private Button backToMenuButton;

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

            EnsureBackButtonUI();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnBackToMenuClicked();
            }
        }

        private void EnsureBackButtonUI()
        {
            if (backToMenuButton == null)
            {
                var foundBtn = GameObject.Find("Btn_BackToMenu") ?? GameObject.Find("Btn_Back") ?? GameObject.Find("BackButton") ?? GameObject.Find("Btn_BackToList");
                if (foundBtn != null) backToMenuButton = foundBtn.GetComponent<Button>();
            }

            if (backToMenuButton == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    var btnGo = new GameObject("Btn_BackToMenu", typeof(RectTransform), typeof(Image), typeof(Button));
                    btnGo.transform.SetParent(canvas.transform, false);

                    var rt = btnGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-780, 480);
                    rt.sizeDelta = new Vector2(160, 50);

                    var img = btnGo.GetComponent<Image>();
                    img.color = new Color(0.3f, 0.35f, 0.45f);

                    var btn = btnGo.GetComponent<Button>();
                    var nav = btn.navigation;
                    nav.mode = Navigation.Mode.None;
                    btn.navigation = nav;

                    var colors = btn.colors;
                    colors.normalColor = new Color(0.3f, 0.35f, 0.45f);
                    colors.highlightedColor = new Color(0.38f, 0.45f, 0.58f);
                    colors.pressedColor = new Color(0.2f, 0.25f, 0.35f);
                    btn.colors = colors;

                    var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtGo.transform.SetParent(btnGo.transform, false);
                    var txtRt = txtGo.GetComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.offsetMin = Vector2.zero;
                    txtRt.offsetMax = Vector2.zero;

                    var tmp = txtGo.GetComponent<TextMeshProUGUI>();
                    tmp.text = "< Menu";
                    tmp.fontSize = 20;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = Color.white;
                    tmp.alignment = TextAlignmentOptions.Center;

                    backToMenuButton = btn;
                }
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveListener(OnBackToMenuClicked);
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            }
        }

        public void OnBackToMenuClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
            }
            StopAllCoroutines();
            PlayerPrefs.SetInt("Quiz_IsWrongReviewMode", 0);

            string targetScene = "02_MainMenu";
            if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.ReturnSceneName))
            {
                targetScene = SessionManager.Instance.ReturnSceneName;
            }
            else if (!string.IsNullOrEmpty(PlayerPrefs.GetString("Current_Lesson_ID", "")))
            {
                targetScene = "03_LessonList";
            }

            SceneManager.LoadScene(targetScene);
        }

        private void Start()
        {
            EnsureBackButtonUI();

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

            networkService = Math6Companion.Core.Services.NetworkServiceProvider.NetworkService;
            string selectedTopic = PlayerPrefs.GetString("Current_Lesson_Topic", "ALL");
            if (string.IsNullOrEmpty(selectedTopic)) selectedTopic = "ALL";
            string selectedDiff = PlayerPrefs.GetString("Quiz_Difficulty_Filter", "ALL");
            LoadQuizData(selectedTopic, "6", selectedDiff);
        }

        public void LoadQuizData(string topic, string grade, string difficulty = "ALL")
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            if (feedbackText != null) feedbackText.text = string.Empty;
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            currentQuestionIndex = 0;
            correctCount = 0;
            isAnswering = true; // Khóa tương tác tạm thời khi đang tải câu hỏi
            LockAllOptions();
            
            // Bảo toàn cờ Versus nếu đi từ sảnh đối kháng sang
            bool wasVersus = ScoreSessionContext.IsVersusMode;
            string oppNick = ScoreSessionContext.OpponentNickname;
            int oppScore = ScoreSessionContext.OpponentScore;

            ScoreSessionContext.Reset();
            ScoreSessionContext.ActivityType = "MCQ";
            if (wasVersus)
            {
                ScoreSessionContext.IsVersusMode = true;
                ScoreSessionContext.OpponentNickname = oppNick;
                ScoreSessionContext.OpponentScore = oppScore;
            }

            bool isWrongReviewMode = PlayerPrefs.GetInt("Quiz_IsWrongReviewMode", 0) == 1;
            if (isWrongReviewMode)
            {
                if (promptText != null) promptText.text = "Đang nạp danh sách các câu làm sai (Chế độ Ôn tập Spaced Repetition)...";
                var wrongQs = WrongQuestionBankManager.GetWrongMCQQuestions();
                if (wrongQs != null && wrongQs.Count > 0)
                {
                    questionList = wrongQs;
                    DisplayQuestion(0);
                    return;
                }
                else
                {
                    Debug.Log("[QuizUIController] Không có câu sai nào trong ngân hàng, chuyển về chế độ luyện tập thông thường.");
                    PlayerPrefs.SetInt("Quiz_IsWrongReviewMode", 0);
                }
            }

            if (promptText != null) promptText.text = "Đang chuẩn bị bộ câu hỏi Toán 6...";

            string currentLessonId = PlayerPrefs.GetString("Current_Lesson_ID", "");
            string currentLessonTitle = PlayerPrefs.GetString("Current_Lesson_Title", "");

            if (networkService != null)
            {
                // Luôn yêu cầu toàn bộ ngân hàng câu hỏi để có thể lọc chính xác theo LessonID, Tiêu đề hoặc Topic
                networkService.GetQuestions("ALL", grade, (success, questions, msg) =>
                {
                    if (this == null || gameObject == null) return;

                    if (success && questions != null && questions.Count > 0)
                    {
                        questionList = SelectAndShuffleQuizQuestions(questions, currentLessonId, currentLessonTitle, topic, difficulty, 10);
                    }
                    else
                    {
                        questionList = LoadTopicDefaultQuestions(topic, currentLessonTitle);
                    }
                    DisplayQuestion(0);
                });
            }
            else
            {
                questionList = LoadTopicDefaultQuestions(topic, currentLessonTitle);
                DisplayQuestion(0);
            }
        }

        private static bool IsLessonIdMatch(string qLessonId, string targetLessonId)
        {
            if (string.IsNullOrEmpty(qLessonId) || string.IsNullOrEmpty(targetLessonId))
                return false;

            if (qLessonId.Equals(targetLessonId, StringComparison.OrdinalIgnoreCase))
                return true;

            string cleanQ = qLessonId.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
            string cleanT = targetLessonId.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
            return cleanQ == cleanT;
        }

        private static bool IsQuestionBelongToLesson(QuestionData q, string lessonId, string lessonTitle, string topic)
        {
            if (q == null) return false;

            // 1. KIỂM TRA CHỦ ĐỀ (TOPIC): Nếu bài học có Topic (ví dụ TapHop) và câu hỏi có Topic khác (ví dụ LuyThua, ThuTuPhepTinh) -> LOẠI BỎ NGAY LẬP TỨC
            if (!string.IsNullOrEmpty(topic) && !topic.Equals("ALL", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(q.topic))
            {
                if (!GoogleSheetsNetworkService.IsTopicMatch(q.topic, topic))
                {
                    return false; // Tuyệt đối không lấy câu hỏi khác chủ đề
                }
            }

            // 2. Khớp chính xác theo LessonID (Ví dụ: LES_CH1_01)
            if (!string.IsNullOrEmpty(lessonId) && !string.IsNullOrEmpty(q.lessonId) && IsLessonIdMatch(q.lessonId, lessonId))
                return true;

            // 3. Khớp theo Topic chuẩn (Ví dụ: TapHop, LuyThua, SoNguyen...)
            if (!string.IsNullOrEmpty(topic) && !topic.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(q.topic) && GoogleSheetsNetworkService.IsTopicMatch(q.topic, topic))
                    return true;
            }

            // 4. Dự phòng trường hợp câu hỏi trên Sheet không điền LessonID và Topic: Khớp ngữ nghĩa chặt chẽ
            if (string.IsNullOrEmpty(q.lessonId) && string.IsNullOrEmpty(q.topic) && !string.IsNullOrEmpty(lessonTitle))
            {
                string lt = lessonTitle.ToLowerInvariant();
                string qp = (q.prompt ?? "").ToLowerInvariant();

                if ((lt.Contains("tập hợp") || lt.Contains("phần tử")) && (qp.Contains("tập hợp") || qp.Contains("phần tử của tập hợp")))
                    return true;
                if ((lt.Contains("ghi số") || lt.Contains("la mã")) && (qp.Contains("chữ số la mã") || qp.Contains("ghi số tự nhiên")))
                    return true;
                if ((lt.Contains("luỹ thừa") || lt.Contains("lũy thừa")) && (qp.Contains("luỹ thừa") || qp.Contains("lũy thừa") || qp.Contains("số mũ")))
                    return true;
                if ((lt.Contains("chia hết") || lt.Contains("ước") || lt.Contains("bội")) && (qp.Contains("chia hết") || qp.Contains("ước chung") || qp.Contains("bội chung") || qp.Contains("số nguyên tố")))
                    return true;
                if (lt.Contains("số nguyên") && (qp.Contains("số nguyên") || qp.Contains("nguyên âm") || qp.Contains("trục số")))
                    return true;
                if (lt.Contains("phân số") && qp.Contains("phân số"))
                    return true;
            }

            return false;
        }

        private static void ShuffleList<T>(List<T> list)
        {
            if (list == null || list.Count <= 1) return;
            var rnd = new System.Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        private List<QuestionData> SelectAndShuffleQuizQuestions(List<QuestionData> allQuestions, string targetLessonId, string targetLessonTitle, string targetTopic, string targetDifficulty, int maxCount)
        {
            if (allQuestions == null || allQuestions.Count == 0)
                return LoadTopicDefaultQuestions(targetTopic, targetLessonTitle);

            List<QuestionData> matchedLessonQuestions = new List<QuestionData>();

            bool isSpecificLesson = !string.IsNullOrEmpty(targetLessonId) || !string.IsNullOrEmpty(targetLessonTitle);
            bool isSpecificTopic = !string.IsNullOrEmpty(targetTopic) && !targetTopic.Equals("ALL", StringComparison.OrdinalIgnoreCase);

            // 1. Nếu có bài học cụ thể (từ danh sách bài học): Lọc theo LessonID, Tiêu đề hoặc Topic bài đó
            if (isSpecificLesson)
            {
                foreach (var q in allQuestions)
                {
                    if (q.type == "MCQ" || string.IsNullOrEmpty(q.type))
                    {
                        if (IsQuestionBelongToLesson(q, targetLessonId, targetLessonTitle, targetTopic))
                        {
                            matchedLessonQuestions.Add(q);
                        }
                    }
                }
            }
            // 2. Nếu có Topic cụ thể (nhưng không chọn bài cụ thể):
            else if (isSpecificTopic)
            {
                foreach (var q in allQuestions)
                {
                    if (q.type == "MCQ" || string.IsNullOrEmpty(q.type))
                    {
                        if (GoogleSheetsNetworkService.IsTopicMatch(q.topic, targetTopic))
                        {
                            matchedLessonQuestions.Add(q);
                        }
                    }
                }
            }
            // 3. Nếu vào từ Mục 2 / Luyện tập tổng hợp (Topic = ALL hoặc không chọn bài): Lấy toàn bộ câu hỏi từ sheet để trộn ngẫu nhiên!
            else
            {
                foreach (var q in allQuestions)
                {
                    if (q.type == "MCQ" || string.IsNullOrEmpty(q.type))
                    {
                        matchedLessonQuestions.Add(q);
                    }
                }
            }

            // 4. Lọc theo Mức độ khó (Difficulty) trên tập câu hỏi đã khớp
            List<QuestionData> finalPool = new List<QuestionData>();

            if (matchedLessonQuestions.Count > 0)
            {
                // Ưu tiên: Câu hỏi đúng độ khó yêu cầu
                foreach (var q in matchedLessonQuestions)
                {
                    if (GoogleSheetsNetworkService.IsDifficultyMatch(q.difficulty, targetDifficulty))
                    {
                        finalPool.Add(q);
                    }
                }

                // Nếu số câu đúng độ khó < 3 hoặc trống, bổ sung thêm các câu khác trong tập đã lọc
                if (finalPool.Count < 3)
                {
                    foreach (var q in matchedLessonQuestions)
                    {
                        if (!finalPool.Contains(q))
                        {
                            finalPool.Add(q);
                        }
                    }
                }
            }

            // 5. Nếu vẫn trống (ví dụ bài mới chưa nhập trên Google Sheet): Nạp ngân hàng mặc định
            if (finalPool.Count == 0)
            {
                finalPool = LoadTopicDefaultQuestions(targetTopic, targetLessonTitle);
            }

            ShuffleList(finalPool);
            return finalPool.GetRange(0, Mathf.Min(maxCount, finalPool.Count));
        }

        private List<QuestionData> LoadTopicDefaultQuestions(string topic, string lessonTitle = "")
        {
            string lt = (lessonTitle ?? "").ToLowerInvariant();

            // Luỹ thừa với số mũ tự nhiên
            if (lt.Contains("luỹ thừa") || lt.Contains("lũy thừa") || lt.Contains("số mũ") || GoogleSheetsNetworkService.IsTopicMatch(topic, "luythua"))
            {
                return new List<QuestionData>
                {
                    new QuestionData
                    {
                        questionId = "Q_LT_01",
                        topic = "LuyThua",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "Viết kết quả phép tính sau dưới dạng một luỹ thừa: 2^4 . 2^5 = ?",
                        options = new QuestionOptionMap { A = "2^9", B = "4^9", C = "2^20", D = "4^20" },
                        correctAnswer = "A",
                        explanation = "Khi nhân hai luỹ thừa cùng cơ số, ta giữ nguyên cơ số và cộng các số mũ: 4 + 5 = 9. Kết quả: 2^9."
                    },
                    new QuestionData
                    {
                        questionId = "Q_LT_02",
                        topic = "LuyThua",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "Viết kết quả phép tính sau dưới dạng một luỹ thừa: 7^6 : 7^4 = ?",
                        options = new QuestionOptionMap { A = "7^10", B = "7^2", C = "1^2", D = "14" },
                        correctAnswer = "B",
                        explanation = "Khi chia hai luỹ thừa cùng cơ số (khác 0), ta giữ nguyên cơ số và trừ các số mũ: 6 - 4 = 2. Kết quả: 7^2."
                    },
                    new QuestionData
                    {
                        questionId = "Q_LT_03",
                        topic = "LuyThua",
                        type = "MCQ",
                        difficulty = "HARD",
                        prompt = "Giá trị của biểu thức 3^3 + 2^3 là:",
                        options = new QuestionOptionMap { A = "35", B = "25", C = "125", D = "5^3" },
                        correctAnswer = "A",
                        explanation = "Ta tính giá trị từng luỹ thừa: 3^3 = 27 và 2^3 = 8. Sau đó cộng lại: 27 + 8 = 35."
                    },
                    new QuestionData
                    {
                        questionId = "Q_LT_04",
                        topic = "LuyThua",
                        type = "MCQ",
                        difficulty = "EASY",
                        prompt = "Trong luỹ thừa 5^3, cơ số và số mũ lần lượt là:",
                        options = new QuestionOptionMap { A = "Cơ số 5, số mũ 3", B = "Cơ số 3, số mũ 5", C = "Cơ số 15, số mũ 1", D = "Cơ số 5, số mũ 15" },
                        correctAnswer = "A",
                        explanation = "Số viết ở dưới (5) là cơ số, số viết ở trên (3) là số mũ."
                    }
                };
            }

            // Thứ tự thực hiện phép tính
            if (lt.Contains("thứ tự") || lt.Contains("phép tính") || GoogleSheetsNetworkService.IsTopicMatch(topic, "thutupheptinh"))
            {
                return new List<QuestionData>
                {
                    new QuestionData
                    {
                        questionId = "Q_TT_01",
                        topic = "ThuTuPhepTinh",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "Tính giá trị của biểu thức: 14 + 2 . 8^2 = ?",
                        options = new QuestionOptionMap { A = "142", B = "1024", C = "130", D = "140" },
                        correctAnswer = "A",
                        explanation = "Ưu tiên luỹ thừa trước: 8^2 = 64. Sau đó nhân: 2 . 64 = 128. Cuối cùng cộng: 14 + 128 = 142."
                    },
                    new QuestionData
                    {
                        questionId = "Q_TT_02",
                        topic = "ThuTuPhepTinh",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "Tính giá trị của biểu thức: 8 + 36 : 3 . 2 = ?",
                        options = new QuestionOptionMap { A = "32", B = "14", C = "44", D = "20" },
                        correctAnswer = "A",
                        explanation = "Thực hiện nhân chia từ trái sang phải: 36 : 3 = 12, tiếp tục 12 . 2 = 24. Cuối cùng cộng: 8 + 24 = 32."
                    }
                };
            }

            // Tính chia hết / Dấu hiệu chia hết
            if (lt.Contains("chia hết") || GoogleSheetsNetworkService.IsTopicMatch(topic, "tinhchiahet"))
            {
                return new List<QuestionData>
                {
                    new QuestionData
                    {
                        questionId = "Q_CH_01",
                        topic = "TinhChiaHet",
                        type = "MCQ",
                        difficulty = "EASY",
                        prompt = "Trong các số sau, số nào là ước của 15?",
                        options = new QuestionOptionMap { A = "2", B = "5", C = "6", D = "30" },
                        correctAnswer = "B",
                        explanation = "Ta có 15 : 5 = 3 nên 5 là ước của 15."
                    },
                    new QuestionData
                    {
                        questionId = "Q_CH_02",
                        topic = "TinhChiaHet",
                        type = "MCQ",
                        difficulty = "EASY",
                        prompt = "Số nào sau đây là bội của 4?",
                        options = new QuestionOptionMap { A = "14", B = "24", C = "34", D = "42" },
                        correctAnswer = "B",
                        explanation = "Ta có 24 chia hết cho 4 (24 : 4 = 6) nên 24 là bội của 4."
                    }
                };
            }

            // Tập hợp
            if (lt.Contains("tập hợp") || GoogleSheetsNetworkService.IsTopicMatch(topic, "TapHop"))
            {
                return new List<QuestionData>
                {
                    new QuestionData
                    {
                        questionId = "Q_TH_01",
                        topic = "TapHop",
                        type = "MCQ",
                        difficulty = "EASY",
                        prompt = "Cho tập hợp A = {1; 2; 3; 4; 5}. Khẳng định nào sau đây là ĐÚNG?",
                        options = new QuestionOptionMap { A = "3 thuộc A", B = "6 thuộc A", C = "0 thuộc A", D = "7 thuộc A" },
                        correctAnswer = "A",
                        explanation = "Phần tử 3 nằm trong tập hợp A nên 3 thuộc A."
                    },
                    new QuestionData
                    {
                        questionId = "Q_TH_02",
                        topic = "TapHop",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "Viết tập hợp B các số tự nhiên x sao cho 5 < x <= 9 bằng cách liệt kê phần tử:",
                        options = new QuestionOptionMap { A = "B = {6; 7; 8; 9}", B = "B = {5; 6; 7; 8; 9}", C = "B = {6; 7; 8}", D = "B = {5; 6; 7; 8}" },
                        correctAnswer = "A",
                        explanation = "Vì x lớn hơn 5 và nhỏ hơn hoặc bằng 9 nên các phần tử gồm: 6, 7, 8, 9."
                    }
                };
            }

            // Phân số
            if (lt.Contains("phân số") || GoogleSheetsNetworkService.IsTopicMatch(topic, "PhanSo"))
            {
                return new List<QuestionData>
                {
                    new QuestionData
                    {
                        questionId = "Q_PS_01",
                        topic = "PhanSo",
                        type = "MCQ",
                        difficulty = "EASY",
                        prompt = "Rút gọn phân số 18/24 về phân số tối giản:",
                        options = new QuestionOptionMap { A = "3/4", B = "6/8", C = "9/12", D = "2/3" },
                        correctAnswer = "A",
                        explanation = "Chia cả tử và mẫu cho ƯCLN(18, 24) = 6: 18:6 / 24:6 = 3/4."
                    },
                    new QuestionData
                    {
                        questionId = "Q_PS_02",
                        topic = "PhanSo",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "Tính giá trị của phép tính: 1/4 + 2/3 = ?",
                        options = new QuestionOptionMap { A = "11/12", B = "3/7", C = "3/12", D = "2/12" },
                        correctAnswer = "A",
                        explanation = "Quy đồng mẫu chung 12: 3/12 + 8/12 = 11/12."
                    }
                };
            }

            // Hình học
            if (lt.Contains("hình") || lt.Contains("đối xứng") || GoogleSheetsNetworkService.IsTopicMatch(topic, "HinhHoc"))
            {
                return new List<QuestionData>
                {
                    new QuestionData
                    {
                        questionId = "Q_HH_01",
                        topic = "HinhHoc",
                        type = "MCQ",
                        difficulty = "EASY",
                        prompt = "Hình vuông có tất cả bao nhiêu trục đối xứng?",
                        options = new QuestionOptionMap { A = "4", B = "2", C = "1", D = "Vô số" },
                        correctAnswer = "A",
                        explanation = "Hình vuông có 4 trục đối xứng: 2 đường chéo và 2 đường trung trực của các cạnh đối diện."
                    },
                    new QuestionData
                    {
                        questionId = "Q_HH_02",
                        topic = "HinhHoc",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "Hình tam giác đều có bao nhiêu trục đối xứng?",
                        options = new QuestionOptionMap { A = "3", B = "1", C = "2", D = "0" },
                        correctAnswer = "A",
                        explanation = "Hình tam giác đều có 3 trục đối xứng là 3 đường trung trực của 3 cạnh."
                    }
                };
            }

            // Mặc định Số nguyên (Integers)
            return new List<QuestionData>
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
                    explanation = "Cộng hai số nguyên cùng dấu âm: -(15 + 8) = -23."
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
                }
            };
        }

        private void LoadDefaultQuestions()
        {
            string topic = PlayerPrefs.GetString("Current_Lesson_Topic", "Integers");
            string title = PlayerPrefs.GetString("Current_Lesson_Title", "");
            questionList = LoadTopicDefaultQuestions(topic, title);
        }

        private void DisplayQuestion(int index)
        {
            if (questionList == null || questionList.Count == 0)
            {
                LoadDefaultQuestions();
            }

            if (index < 0 || index >= questionList.Count) return;

            isAnswering = false;
            currentQuestionIndex = index;
            QuestionData q = questionList[index];

            if (topicTitleText != null)
            {
                bool isWrongReviewMode = PlayerPrefs.GetInt("Quiz_IsWrongReviewMode", 0) == 1;
                if (isWrongReviewMode)
                {
                    topicTitleText.text = "<b>[🔥] SỔ TAY CÂU SAI • ANKI SPACED REPETITION</b>";
                }
                else
                {
                    string topicDisplay = "Toán Học Lớp 6";
                    if (!string.IsNullOrEmpty(q.topic))
                    {
                        if (GoogleSheetsNetworkService.IsTopicMatch(q.topic, "HinhHoc")) topicDisplay = "Hình học trực quan & Tính đối xứng";
                        else if (GoogleSheetsNetworkService.IsTopicMatch(q.topic, "PhanSo")) topicDisplay = "Phân số & Số thập phân";
                        else if (GoogleSheetsNetworkService.IsTopicMatch(q.topic, "SoNguyen")) topicDisplay = "Số nguyên & Trục số";
                        else if (GoogleSheetsNetworkService.IsTopicMatch(q.topic, "TapHop")) topicDisplay = "Tập hợp & Số tự nhiên";
                        else topicDisplay = q.topic;
                    }

                    string lessonTitle = PlayerPrefs.GetString("Current_Lesson_Title", "");
                    if (!string.IsNullOrEmpty(lessonTitle))
                    {
                        topicTitleText.text = $"Trắc nghiệm: {lessonTitle}";
                    }
                    else
                    {
                        topicTitleText.text = $"Chủ đề: {topicDisplay}";
                    }
                }
            }

            if (progressText != null)
                progressText.text = $"Câu {index + 1} / {questionList.Count}";

            if (promptText != null)
                promptText.text = FormatMathString(q.prompt);

            if (feedbackText != null)
                feedbackText.text = string.Empty;

            if (nextButton != null)
                nextButton.gameObject.SetActive(false);

            ResetOptionButtons();

            string[] answers = q.options != null
                ? new string[] { q.options.A, q.options.B, q.options.C, q.options.D }
                : new string[] { "", "", "", "" };

            if (optionButtons != null)
            {
                for (int i = 0; i < optionButtons.Length; i++)
                {
                    if (optionButtons[i] == null) continue;

                    if (i < answers.Length && !string.IsNullOrEmpty(answers[i]))
                    {
                        optionButtons[i].gameObject.SetActive(true);
                        if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                        {
                            optionTexts[i].text = $"{optionKeys[i]}. {FormatMathString(answers[i])}";
                        }
                        optionButtons[i].interactable = true;
                    }
                    else
                    {
                        optionButtons[i].gameObject.SetActive(false);
                    }
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
            if (questionList == null || questionList.Count == 0 || currentQuestionIndex < 0 || currentQuestionIndex >= questionList.Count) return;

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
            if (questionList == null || questionList.Count == 0 || currentQuestionIndex < 0 || currentQuestionIndex >= questionList.Count) return;
            if (index < 0 || index >= optionKeys.Length || index >= optionButtons.Length) return;

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

                // Ghi nhận ôn tập thành công nếu đang trong ngân hàng câu sai
                WrongQuestionBankManager.RecordCorrectReview(questionList[currentQuestionIndex].questionId);
            }
            else
            {
                optionButtons[index].image.color = new Color(1f, 0.4f, 0.4f); // Đỏ
                HighlightCorrectButton(correct);

                // Ghi nhận câu sai vào Sổ tay câu sai (Anki/Quizlet)
                WrongQuestionBankManager.RecordWrongMCQ(questionList[currentQuestionIndex], chosen);

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
            if (optionButtons == null || optionKeys == null) return;
            for (int i = 0; i < optionKeys.Length; i++)
            {
                if (i < optionButtons.Length && optionButtons[i] != null && optionKeys[i] == correctKey)
                {
                    if (optionButtons[i].image != null) optionButtons[i].image.color = new Color(0.4f, 0.9f, 0.4f);
                    break;
                }
            }
        }

        private void LockAllOptions()
        {
            if (optionButtons == null) return;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null) optionButtons[i].interactable = false;
            }
        }

        private void ResetOptionButtons()
        {
            if (optionButtons == null) return;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null && optionButtons[i].image != null)
                {
                    optionButtons[i].image.color = normalOptionColor;
                }
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
            PlayerPrefs.SetInt("Quiz_IsWrongReviewMode", 0);

            if (resultPanel != null) resultPanel.SetActive(true);
            if (resultScoreText != null)
            {
                resultScoreText.text = $"Hoàn thành bài tập!\nĐiểm số: {correctCount}/{questionList.Count}";
            }

            // Ghi dữ liệu vào Session Score dùng chung
            int totalQ = (questionList != null && questionList.Count > 0) ? questionList.Count : 3;
            int final10Scale = Mathf.RoundToInt((float)correctCount / Mathf.Max(1, totalQ) * 10f);

            ScoreSessionContext.ActivityType = "MCQ";
            ScoreSessionContext.Score = correctCount * 10;
            ScoreSessionContext.MaxScore = totalQ * 10;
            ScoreSessionContext.CorrectCount = correctCount;
            ScoreSessionContext.TotalQuestions = totalQ;
            ScoreSessionContext.TimeSpentSeconds = 60;

            if (ScoreSessionContext.IsVersusMode)
            {
                ScoreSessionContext.IsWinner = (ScoreSessionContext.Score >= ScoreSessionContext.OpponentScore);
            }

            // Gửi kết quả lên Google Sheets (chuẩn hóa thang điểm 10 & Múi giờ Việt Nam GMT+7)
            string curLesId = PlayerPrefs.GetString("Current_Lesson_ID", "");
            if (!string.IsNullOrEmpty(curLesId))
            {
                int currentHigh = PlayerPrefs.GetInt($"Lesson_HighestScore_{curLesId}", 0);
                if (final10Scale > currentHigh)
                {
                    PlayerPrefs.SetInt($"Lesson_HighestScore_{curLesId}", final10Scale);
                }
                int att = PlayerPrefs.GetInt($"Lesson_Attempts_{curLesId}", 0);
                PlayerPrefs.SetInt($"Lesson_Attempts_{curLesId}", att + 1);
                PlayerPrefs.SetInt($"Lesson_LastScore_{curLesId}", final10Scale);
                PlayerPrefs.SetString($"Lesson_LastDate_{curLesId}", System.DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                PlayerPrefs.Save();
            }

            ResultSubmission submission = new ResultSubmission
            {
                userId = PlayerPrefs.GetString("User_ID", "STU_001"),
                lessonId = !string.IsNullOrEmpty(curLesId) ? curLesId : "LES_INT_01",
                activityType = ScoreSessionContext.IsVersusMode ? "VERSUS" : "MCQ",
                score = final10Scale,
                maxScore = 10,
                timeSpentSeconds = 60,
                mistakesCount = totalQ - correctCount,
                timestamp = System.DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss")
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

        private string FormatMathString(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return GoogleSheetsNetworkService.SanitizeMathText(text)
                .Replace("\\u2208", " thuộc ")
                .Replace("\u2208", " thuộc ")
                .Replace("∈", " thuộc ")
                .Replace("\\u2209", " không thuộc ")
                .Replace("\u2209", " không thuộc ")
                .Replace("∉", " không thuộc ")
                .Replace("\\u2282", " con ")
                .Replace("\u2282", " con ")
                .Replace("⊂", " con ")
                .Replace("\\u2286", " con hoặc bằng ")
                .Replace("\u2286", " con hoặc bằng ")
                .Replace("⊆", " con hoặc bằng ")
                .Replace("\\u2229", " giao ")
                .Replace("\u2229", " giao ")
                .Replace("∩", " giao ")
                .Replace("\\u222A", " hợp ")
                .Replace("\u222A", " hợp ")
                .Replace("∪", " hợp ");
        }
    }
}