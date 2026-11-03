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
        [SerializeField] private float timePerQuestion = 90f;

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
            // Tá»± Ä‘á»™ng liÃªn káº¿t TextMeshProUGUI bÃªn trong nÃºt náº¿u chÆ°a kÃ©o tháº£
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

                    // Táº¯t Navigation trÃ¡nh EventSystem tá»± chá»n lÃ m tráº¯ng Ä‘Ã¡p Ã¡n Ä‘áº§u tiÃªn
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
            isAnswering = true; // KhÃ³a tÆ°Æ¡ng tÃ¡c táº¡m thá»i khi Ä‘ang táº£i cÃ¢u há»i
            LockAllOptions();
            
            // Báº£o toÃ n cá» Versus náº¿u Ä‘i tá»« sáº£nh Ä‘á»‘i khÃ¡ng sang
            bool wasVersus = ScoreSessionContext.IsVersusMode;
            string oppNick = ScoreSessionContext.OpponentNickname;
            int oppScore = ScoreSessionContext.OpponentScore;
            string vsRoomId = ScoreSessionContext.VersusRoomId;
            string oppUid = ScoreSessionContext.OpponentUserId;
            bool isRealMp = ScoreSessionContext.IsRealMultiplayer;
            List<string> vsQIds = new List<string>(ScoreSessionContext.VersusQuestionIds ?? new List<string>());

            ScoreSessionContext.Reset();
            ScoreSessionContext.ActivityType = "MCQ";
            if (wasVersus)
            {
                ScoreSessionContext.IsVersusMode = true;
                ScoreSessionContext.OpponentNickname = oppNick;
                ScoreSessionContext.OpponentScore = oppScore;
                ScoreSessionContext.VersusRoomId = vsRoomId;
                ScoreSessionContext.OpponentUserId = oppUid;
                ScoreSessionContext.IsRealMultiplayer = isRealMp;
                ScoreSessionContext.VersusQuestionIds = vsQIds;
            }

            bool isWrongReviewMode = PlayerPrefs.GetInt("Quiz_IsWrongReviewMode", 0) == 1;
            if (isWrongReviewMode)
            {
                if (promptText != null) promptText.text = "Äang náº¡p danh sÃ¡ch cÃ¡c cÃ¢u lÃ m sai (Cháº¿ Ä‘á»™ Ã”n táº­p Spaced Repetition)...";
                var wrongQs = WrongQuestionBankManager.GetWrongMCQQuestions();
                if (wrongQs != null && wrongQs.Count > 0)
                {
                    questionList = wrongQs;
                    DisplayQuestion(0);
                    return;
                }
                else
                {
                    Debug.Log("[QuizUIController] KhÃ´ng cÃ³ cÃ¢u sai nÃ o trong ngÃ¢n hÃ ng, chuyá»ƒn vá» cháº¿ Ä‘á»™ luyá»‡n táº­p thÃ´ng thÆ°á»ng.");
                    PlayerPrefs.SetInt("Quiz_IsWrongReviewMode", 0);
                }
            }

            if (promptText != null) promptText.text = "Äang chuáº©n bá»‹ bá»™ cÃ¢u há»i ToÃ¡n 6...";

            string currentLessonId = PlayerPrefs.GetString("Current_Lesson_ID", "");
            string currentLessonTitle = PlayerPrefs.GetString("Current_Lesson_Title", "");

            if (networkService != null)
            {
                // LuÃ´n yÃªu cáº§u toÃ n bá»™ ngÃ¢n hÃ ng cÃ¢u há»i Ä‘á»ƒ cÃ³ thá»ƒ lá»c chÃ­nh xÃ¡c theo LessonID, TiÃªu Ä‘á» hoáº·c Topic
                networkService.GetQuestions("ALL", grade, (success, questions, msg) =>
                {
                    if (this == null || gameObject == null) return;

                    if (success && questions != null && questions.Count > 0)
                    {
                        var allQ = new List<QuestionData>(questions);
                        if (TeacherConfigManager.Instance != null)
                        {
                            var customMCQs = TeacherConfigManager.Instance.GetCustomQuestionDataList(6, topic, "MCQ");
                            if (customMCQs != null && customMCQs.Count > 0)
                            {
                                allQ.InsertRange(0, customMCQs);
                            }
                        }
                        if (ScoreSessionContext.IsVersusMode && ScoreSessionContext.VersusQuestionIds != null && ScoreSessionContext.VersusQuestionIds.Count > 0)
                        {
                            questionList = FilterQuestionsBySpecificIds(allQ, ScoreSessionContext.VersusQuestionIds);
                        }
                        else
                        {
                            questionList = SelectAndShuffleQuizQuestions(allQ, currentLessonId, currentLessonTitle, topic, difficulty, 10);
                        }
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

            // 1. KIá»‚M TRA CHá»¦ Äá»€ (TOPIC): Náº¿u bÃ i há»c cÃ³ Topic (vÃ­ dá»¥ TapHop) vÃ  cÃ¢u há»i cÃ³ Topic khÃ¡c (vÃ­ dá»¥ LuyThua, ThuTuPhepTinh) -> LOáº I Bá»Ž NGAY Láº¬P Tá»¨C
            if (!string.IsNullOrEmpty(topic) && !topic.Equals("ALL", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(q.topic))
            {
                if (!GoogleSheetsNetworkService.IsTopicMatch(q.topic, topic))
                {
                    return false; // Tuyá»‡t Ä‘á»‘i khÃ´ng láº¥y cÃ¢u há»i khÃ¡c chá»§ Ä‘á»
                }
            }

            // 2. Khá»›p chÃ­nh xÃ¡c theo LessonID (VÃ­ dá»¥: LES_CH1_01)
            if (!string.IsNullOrEmpty(lessonId) && !string.IsNullOrEmpty(q.lessonId) && IsLessonIdMatch(q.lessonId, lessonId))
                return true;

            // 3. Khá»›p theo Topic chuáº©n (VÃ­ dá»¥: TapHop, LuyThua, SoNguyen...)
            if (!string.IsNullOrEmpty(topic) && !topic.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(q.topic) && GoogleSheetsNetworkService.IsTopicMatch(q.topic, topic))
                    return true;
            }

            // 4. Dá»± phÃ²ng trÆ°á»ng há»£p cÃ¢u há»i trÃªn Sheet khÃ´ng Ä‘iá»n LessonID vÃ  Topic: Khá»›p ngá»¯ nghÄ©a cháº·t cháº½
            if (string.IsNullOrEmpty(q.lessonId) && string.IsNullOrEmpty(q.topic) && !string.IsNullOrEmpty(lessonTitle))
            {
                string lt = lessonTitle.ToLowerInvariant();
                string qp = (q.prompt ?? "").ToLowerInvariant();

                if ((lt.Contains("táº­p há»£p") || lt.Contains("pháº§n tá»­")) && (qp.Contains("táº­p há»£p") || qp.Contains("pháº§n tá»­ cá»§a táº­p há»£p")))
                    return true;
                if ((lt.Contains("ghi sá»‘") || lt.Contains("la mÃ£")) && (qp.Contains("chá»¯ sá»‘ la mÃ£") || qp.Contains("ghi sá»‘ tá»± nhiÃªn")))
                    return true;
                if ((lt.Contains("luá»¹ thá»«a") || lt.Contains("lÅ©y thá»«a")) && (qp.Contains("luá»¹ thá»«a") || qp.Contains("lÅ©y thá»«a") || qp.Contains("sá»‘ mÅ©")))
                    return true;
                if ((lt.Contains("chia háº¿t") || lt.Contains("Æ°á»›c") || lt.Contains("bá»™i")) && (qp.Contains("chia háº¿t") || qp.Contains("Æ°á»›c chung") || qp.Contains("bá»™i chung") || qp.Contains("sá»‘ nguyÃªn tá»‘")))
                    return true;
                if (lt.Contains("sá»‘ nguyÃªn") && (qp.Contains("sá»‘ nguyÃªn") || qp.Contains("nguyÃªn Ã¢m") || qp.Contains("trá»¥c sá»‘")))
                    return true;
                if (lt.Contains("phÃ¢n sá»‘") && qp.Contains("phÃ¢n sá»‘"))
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

        private List<QuestionData> FilterQuestionsBySpecificIds(List<QuestionData> allQuestions, List<string> targetIds)
        {
            var result = new List<QuestionData>();
            if (allQuestions == null || targetIds == null || targetIds.Count == 0) return result;

            var dict = new Dictionary<string, QuestionData>(StringComparer.OrdinalIgnoreCase);
            foreach (var q in allQuestions)
            {
                if (q != null && !string.IsNullOrEmpty(q.questionId) && !dict.ContainsKey(q.questionId))
                {
                    dict[q.questionId] = q;
                }
            }

            foreach (var id in targetIds)
            {
                if (dict.TryGetValue(id.Trim(), out var matchedQ))
                {
                    result.Add(matchedQ);
                }
            }

            // Nếu câu hỏi trên Sheet không khớp ID hoặc thiếu, lấy thêm từ danh sách chung
            if (result.Count < 5)
            {
                var fallback = SelectAndShuffleQuizQuestions(allQuestions, "", "", "ALL", "MEDIUM", 10);
                foreach (var f in fallback)
                {
                    if (!result.Contains(f) && result.Count < 10) result.Add(f);
                }
            }
            return result;
        }

        private List<QuestionData> SelectAndShuffleQuizQuestions(List<QuestionData> allQuestions, string targetLessonId, string targetLessonTitle, string targetTopic, string targetDifficulty, int maxCount)
        {
            if (allQuestions == null || allQuestions.Count == 0)
                return LoadTopicDefaultQuestions(targetTopic, targetLessonTitle);

            List<QuestionData> matchedLessonQuestions = new List<QuestionData>();

            bool isSpecificLesson = !string.IsNullOrEmpty(targetLessonId) || !string.IsNullOrEmpty(targetLessonTitle);
            bool isSpecificTopic = !string.IsNullOrEmpty(targetTopic) && !targetTopic.Equals("ALL", StringComparison.OrdinalIgnoreCase);

            // 1. Náº¿u cÃ³ bÃ i há»c cá»¥ thá»ƒ (tá»« danh sÃ¡ch bÃ i há»c): Lá»c theo LessonID, TiÃªu Ä‘á» hoáº·c Topic bÃ i Ä‘Ã³
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
            // 2. Náº¿u cÃ³ Topic cá»¥ thá»ƒ (nhÆ°ng khÃ´ng chá»n bÃ i cá»¥ thá»ƒ):
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
            // 3. Náº¿u vÃ o tá»« Má»¥c 2 / Luyá»‡n táº­p tá»•ng há»£p (Topic = ALL hoáº·c khÃ´ng chá»n bÃ i): Láº¥y toÃ n bá»™ cÃ¢u há»i tá»« sheet Ä‘á»ƒ trá»™n ngáº«u nhiÃªn!
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

            // 4. Lá»c theo Má»©c Ä‘á»™ khÃ³ (Difficulty) trÃªn táº­p cÃ¢u há»i Ä‘Ã£ khá»›p
            List<QuestionData> finalPool = new List<QuestionData>();

            if (matchedLessonQuestions.Count > 0)
            {
                // Æ¯u tiÃªn: CÃ¢u há»i Ä‘Ãºng Ä‘á»™ khÃ³ yÃªu cáº§u
                foreach (var q in matchedLessonQuestions)
                {
                    if (GoogleSheetsNetworkService.IsDifficultyMatch(q.difficulty, targetDifficulty))
                    {
                        finalPool.Add(q);
                    }
                }

                // Náº¿u sá»‘ cÃ¢u Ä‘Ãºng Ä‘á»™ khÃ³ < 3 hoáº·c trá»‘ng, bá»• sung thÃªm cÃ¡c cÃ¢u khÃ¡c trong táº­p Ä‘Ã£ lá»c
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

            // 5. Náº¿u váº«n trá»‘ng (vÃ­ dá»¥ bÃ i má»›i chÆ°a nháº­p trÃªn Google Sheet): Náº¡p ngÃ¢n hÃ ng máº·c Ä‘á»‹nh
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

            // Luá»¹ thá»«a vá»›i sá»‘ mÅ© tá»± nhiÃªn
            if (lt.Contains("luá»¹ thá»«a") || lt.Contains("lÅ©y thá»«a") || lt.Contains("sá»‘ mÅ©") || GoogleSheetsNetworkService.IsTopicMatch(topic, "luythua"))
            {
                return new List<QuestionData>
                {
                    new QuestionData
                    {
                        questionId = "Q_LT_01",
                        topic = "LuyThua",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "Viáº¿t káº¿t quáº£ phÃ©p tÃ­nh sau dÆ°á»›i dáº¡ng má»™t luá»¹ thá»«a: 2^4 . 2^5 = ?",
                        options = new QuestionOptionMap { A = "2^9", B = "4^9", C = "2^20", D = "4^20" },
                        correctAnswer = "A",
                        explanation = "Khi nhÃ¢n hai luá»¹ thá»«a cÃ¹ng cÆ¡ sá»‘, ta giá»¯ nguyÃªn cÆ¡ sá»‘ vÃ  cá»™ng cÃ¡c sá»‘ mÅ©: 4 + 5 = 9. Káº¿t quáº£: 2^9."
                    },
                    new QuestionData
                    {
                        questionId = "Q_LT_02",
                        topic = "LuyThua",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "Viáº¿t káº¿t quáº£ phÃ©p tÃ­nh sau dÆ°á»›i dáº¡ng má»™t luá»¹ thá»«a: 7^6 : 7^4 = ?",
                        options = new QuestionOptionMap { A = "7^10", B = "7^2", C = "1^2", D = "14" },
                        correctAnswer = "B",
                        explanation = "Khi chia hai luá»¹ thá»«a cÃ¹ng cÆ¡ sá»‘ (khÃ¡c 0), ta giá»¯ nguyÃªn cÆ¡ sá»‘ vÃ  trá»« cÃ¡c sá»‘ mÅ©: 6 - 4 = 2. Káº¿t quáº£: 7^2."
                    },
                    new QuestionData
                    {
                        questionId = "Q_LT_03",
                        topic = "LuyThua",
                        type = "MCQ",
                        difficulty = "HARD",
                        prompt = "GiÃ¡ trá»‹ cá»§a biá»ƒu thá»©c 3^3 + 2^3 lÃ :",
                        options = new QuestionOptionMap { A = "35", B = "25", C = "125", D = "5^3" },
                        correctAnswer = "A",
                        explanation = "Ta tÃ­nh giÃ¡ trá»‹ tá»«ng luá»¹ thá»«a: 3^3 = 27 vÃ  2^3 = 8. Sau Ä‘Ã³ cá»™ng láº¡i: 27 + 8 = 35."
                    },
                    new QuestionData
                    {
                        questionId = "Q_LT_04",
                        topic = "LuyThua",
                        type = "MCQ",
                        difficulty = "EASY",
                        prompt = "Trong luá»¹ thá»«a 5^3, cÆ¡ sá»‘ vÃ  sá»‘ mÅ© láº§n lÆ°á»£t lÃ :",
                        options = new QuestionOptionMap { A = "CÆ¡ sá»‘ 5, sá»‘ mÅ© 3", B = "CÆ¡ sá»‘ 3, sá»‘ mÅ© 5", C = "CÆ¡ sá»‘ 15, sá»‘ mÅ© 1", D = "CÆ¡ sá»‘ 5, sá»‘ mÅ© 15" },
                        correctAnswer = "A",
                        explanation = "Sá»‘ viáº¿t á»Ÿ dÆ°á»›i (5) lÃ  cÆ¡ sá»‘, sá»‘ viáº¿t á»Ÿ trÃªn (3) lÃ  sá»‘ mÅ©."
                    }
                };
            }

            // Thá»© tá»± thá»±c hiá»‡n phÃ©p tÃ­nh
            if (lt.Contains("thá»© tá»±") || lt.Contains("phÃ©p tÃ­nh") || GoogleSheetsNetworkService.IsTopicMatch(topic, "thutupheptinh"))
            {
                return new List<QuestionData>
                {
                    new QuestionData
                    {
                        questionId = "Q_TT_01",
                        topic = "ThuTuPhepTinh",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "TÃ­nh giÃ¡ trá»‹ cá»§a biá»ƒu thá»©c: 14 + 2 . 8^2 = ?",
                        options = new QuestionOptionMap { A = "142", B = "1024", C = "130", D = "140" },
                        correctAnswer = "A",
                        explanation = "Æ¯u tiÃªn luá»¹ thá»«a trÆ°á»›c: 8^2 = 64. Sau Ä‘Ã³ nhÃ¢n: 2 . 64 = 128. Cuá»‘i cÃ¹ng cá»™ng: 14 + 128 = 142."
                    },
                    new QuestionData
                    {
                        questionId = "Q_TT_02",
                        topic = "ThuTuPhepTinh",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "TÃ­nh giÃ¡ trá»‹ cá»§a biá»ƒu thá»©c: 8 + 36 : 3 . 2 = ?",
                        options = new QuestionOptionMap { A = "32", B = "14", C = "44", D = "20" },
                        correctAnswer = "A",
                        explanation = "Thá»±c hiá»‡n nhÃ¢n chia tá»« trÃ¡i sang pháº£i: 36 : 3 = 12, tiáº¿p tá»¥c 12 . 2 = 24. Cuá»‘i cÃ¹ng cá»™ng: 8 + 24 = 32."
                    }
                };
            }

            // TÃ­nh chia háº¿t / Dáº¥u hiá»‡u chia háº¿t
            if (lt.Contains("chia háº¿t") || GoogleSheetsNetworkService.IsTopicMatch(topic, "tinhchiahet"))
            {
                return new List<QuestionData>
                {
                    new QuestionData
                    {
                        questionId = "Q_CH_01",
                        topic = "TinhChiaHet",
                        type = "MCQ",
                        difficulty = "EASY",
                        prompt = "Trong cÃ¡c sá»‘ sau, sá»‘ nÃ o lÃ  Æ°á»›c cá»§a 15?",
                        options = new QuestionOptionMap { A = "2", B = "5", C = "6", D = "30" },
                        correctAnswer = "B",
                        explanation = "Ta cÃ³ 15 : 5 = 3 nÃªn 5 lÃ  Æ°á»›c cá»§a 15."
                    },
                    new QuestionData
                    {
                        questionId = "Q_CH_02",
                        topic = "TinhChiaHet",
                        type = "MCQ",
                        difficulty = "EASY",
                        prompt = "Sá»‘ nÃ o sau Ä‘Ã¢y lÃ  bá»™i cá»§a 4?",
                        options = new QuestionOptionMap { A = "14", B = "24", C = "34", D = "42" },
                        correctAnswer = "B",
                        explanation = "Ta cÃ³ 24 chia háº¿t cho 4 (24 : 4 = 6) nÃªn 24 lÃ  bá»™i cá»§a 4."
                    }
                };
            }

            // Táº­p há»£p
            if (lt.Contains("táº­p há»£p") || GoogleSheetsNetworkService.IsTopicMatch(topic, "TapHop"))
            {
                return new List<QuestionData>
                {
                    new QuestionData
                    {
                        questionId = "Q_TH_01",
                        topic = "TapHop",
                        type = "MCQ",
                        difficulty = "EASY",
                        prompt = "Cho táº­p há»£p A = {1; 2; 3; 4; 5}. Kháº³ng Ä‘á»‹nh nÃ o sau Ä‘Ã¢y lÃ  ÄÃšNG?",
                        options = new QuestionOptionMap { A = "3 thuá»™c A", B = "6 thuá»™c A", C = "0 thuá»™c A", D = "7 thuá»™c A" },
                        correctAnswer = "A",
                        explanation = "Pháº§n tá»­ 3 náº±m trong táº­p há»£p A nÃªn 3 thuá»™c A."
                    },
                    new QuestionData
                    {
                        questionId = "Q_TH_02",
                        topic = "TapHop",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "Viáº¿t táº­p há»£p B cÃ¡c sá»‘ tá»± nhiÃªn x sao cho 5 < x <= 9 báº±ng cÃ¡ch liá»‡t kÃª pháº§n tá»­:",
                        options = new QuestionOptionMap { A = "B = {6; 7; 8; 9}", B = "B = {5; 6; 7; 8; 9}", C = "B = {6; 7; 8}", D = "B = {5; 6; 7; 8}" },
                        correctAnswer = "A",
                        explanation = "VÃ¬ x lá»›n hÆ¡n 5 vÃ  nhá» hÆ¡n hoáº·c báº±ng 9 nÃªn cÃ¡c pháº§n tá»­ gá»“m: 6, 7, 8, 9."
                    }
                };
            }

            // PhÃ¢n sá»‘
            if (lt.Contains("phÃ¢n sá»‘") || GoogleSheetsNetworkService.IsTopicMatch(topic, "PhanSo"))
            {
                return new List<QuestionData>
                {
                    new QuestionData
                    {
                        questionId = "Q_PS_01",
                        topic = "PhanSo",
                        type = "MCQ",
                        difficulty = "EASY",
                        prompt = "RÃºt gá»n phÃ¢n sá»‘ 18/24 vá» phÃ¢n sá»‘ tá»‘i giáº£n:",
                        options = new QuestionOptionMap { A = "3/4", B = "6/8", C = "9/12", D = "2/3" },
                        correctAnswer = "A",
                        explanation = "Chia cáº£ tá»­ vÃ  máº«u cho Æ¯CLN(18, 24) = 6: 18:6 / 24:6 = 3/4."
                    },
                    new QuestionData
                    {
                        questionId = "Q_PS_02",
                        topic = "PhanSo",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "TÃ­nh giÃ¡ trá»‹ cá»§a phÃ©p tÃ­nh: 1/4 + 2/3 = ?",
                        options = new QuestionOptionMap { A = "11/12", B = "3/7", C = "3/12", D = "2/12" },
                        correctAnswer = "A",
                        explanation = "Quy Ä‘á»“ng máº«u chung 12: 3/12 + 8/12 = 11/12."
                    }
                };
            }

            // HÃ¬nh há»c
            if (lt.Contains("hÃ¬nh") || lt.Contains("Ä‘á»‘i xá»©ng") || GoogleSheetsNetworkService.IsTopicMatch(topic, "HinhHoc"))
            {
                return new List<QuestionData>
                {
                    new QuestionData
                    {
                        questionId = "Q_HH_01",
                        topic = "HinhHoc",
                        type = "MCQ",
                        difficulty = "EASY",
                        prompt = "HÃ¬nh vuÃ´ng cÃ³ táº¥t cáº£ bao nhiÃªu trá»¥c Ä‘á»‘i xá»©ng?",
                        options = new QuestionOptionMap { A = "4", B = "2", C = "1", D = "VÃ´ sá»‘" },
                        correctAnswer = "A",
                        explanation = "HÃ¬nh vuÃ´ng cÃ³ 4 trá»¥c Ä‘á»‘i xá»©ng: 2 Ä‘Æ°á»ng chÃ©o vÃ  2 Ä‘Æ°á»ng trung trá»±c cá»§a cÃ¡c cáº¡nh Ä‘á»‘i diá»‡n."
                    },
                    new QuestionData
                    {
                        questionId = "Q_HH_02",
                        topic = "HinhHoc",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "HÃ¬nh tam giÃ¡c Ä‘á»u cÃ³ bao nhiÃªu trá»¥c Ä‘á»‘i xá»©ng?",
                        options = new QuestionOptionMap { A = "3", B = "1", C = "2", D = "0" },
                        correctAnswer = "A",
                        explanation = "HÃ¬nh tam giÃ¡c Ä‘á»u cÃ³ 3 trá»¥c Ä‘á»‘i xá»©ng lÃ  3 Ä‘Æ°á»ng trung trá»±c cá»§a 3 cáº¡nh."
                    }
                };
            }

            // Máº·c Ä‘á»‹nh Sá»‘ nguyÃªn (Integers)
            return new List<QuestionData>
            {
                new QuestionData
                {
                    questionId = "Q01",
                    topic = "Integers",
                    type = "MCQ",
                    difficulty = "EASY",
                    prompt = "TÃ­nh giÃ¡ trá»‹ cá»§a phÃ©p tÃ­nh: (-15) + (-8) = ?",
                    options = new QuestionOptionMap { A = "-23", B = "23", C = "-7", D = "7" },
                    correctAnswer = "A",
                    explanation = "Cá»™ng hai sá»‘ nguyÃªn cÃ¹ng dáº¥u Ã¢m: -(15 + 8) = -23."
                },
                new QuestionData
                {
                    questionId = "Q02",
                    topic = "Integers",
                    type = "MCQ",
                    difficulty = "MEDIUM",
                    prompt = "TÃ­nh giÃ¡ trá»‹ biá»ƒu thá»©c: 12 - (-5) = ?",
                    options = new QuestionOptionMap { A = "7", B = "-17", C = "17", D = "-7" },
                    correctAnswer = "C",
                    explanation = "Trá»« cho sá»‘ Ã¢m tÆ°Æ¡ng Ä‘Æ°Æ¡ng vá»›i cá»™ng sá»‘ Ä‘á»‘i: 12 - (-5) = 12 + 5 = 17."
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
                    topicTitleText.text = "<b>[SỔ TAY CÂU SAI] ANKI SPACED REPETITION</b>";
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
                        topicTitleText.text = MathTextSanitizer.SanitizeForDisplay($"Trắc nghiệm: {lessonTitle}");
                    }
                    else
                    {
                        topicTitleText.text = MathTextSanitizer.SanitizeForDisplay($"Chủ đề: {topicDisplay}");
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
                    timerText.text = $"{Mathf.CeilToInt(remainingTime)}s";
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
                feedbackText.text = "Háº¿t giá»! " + FormatMathString(questionList[currentQuestionIndex].explanation);
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
                optionButtons[index].image.color = new Color(0.4f, 0.9f, 0.4f); // Xanh lÃ¡
                if (feedbackText != null)
                {
                    feedbackText.text = "ChÃ­nh xÃ¡c! " + FormatMathString(questionList[currentQuestionIndex].explanation);
                    feedbackText.color = Color.green;
                }

                // Ghi nháº­n Ã´n táº­p thÃ nh cÃ´ng náº¿u Ä‘ang trong ngÃ¢n hÃ ng cÃ¢u sai
                WrongQuestionBankManager.RecordCorrectReview(questionList[currentQuestionIndex].questionId);
            }
            else
            {
                optionButtons[index].image.color = new Color(1f, 0.4f, 0.4f); // Äá»
                HighlightCorrectButton(correct);

                // Ghi nháº­n cÃ¢u sai vÃ o Sá»• tay cÃ¢u sai (Anki/Quizlet)
                WrongQuestionBankManager.RecordWrongMCQ(questionList[currentQuestionIndex], chosen);

                // Ghi nháº­n cÃ¢u sai vÃ o Session Ä‘á»ƒ AI Tháº§y Minh phÃ¢n tÃ­ch trong Scene 08_AIFeedback
                ScoreSessionContext.WrongQuestionPrompts.Add(questionList[currentQuestionIndex].prompt);
                ScoreSessionContext.WrongAnswers.Add(chosen);
                ScoreSessionContext.CorrectAnswers.Add(correct);
                ScoreSessionContext.Explanations.Add(questionList[currentQuestionIndex].explanation);

                if (feedbackText != null)
                {
                    feedbackText.text = $"Sai rá»“i! {FormatMathString(questionList[currentQuestionIndex].explanation)}";
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
                resultScoreText.text = $"HoÃ n thÃ nh bÃ i táº­p!\nÄiá»ƒm sá»‘: {correctCount}/{questionList.Count}";
            }

            // Ghi dá»¯ liá»‡u vÃ o Session Score dÃ¹ng chung
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
            string curUserId = (SessionManager.Instance != null && SessionManager.Instance.CurrentUser != null && !string.IsNullOrEmpty(SessionManager.Instance.CurrentUser.userId))
                ? SessionManager.Instance.CurrentUser.userId
                : PlayerPrefs.GetString("User_ID", "STU_001");

            string curLesId = PlayerPrefs.GetString("Current_Lesson_ID", "");
            if (!string.IsNullOrEmpty(curLesId))
            {
                int currentHigh = PlayerPrefs.GetInt($"Lesson_{curUserId}_HighestScore_{curLesId}", 0);
                if (final10Scale > currentHigh)
                {
                    PlayerPrefs.SetInt($"Lesson_{curUserId}_HighestScore_{curLesId}", final10Scale);
                }
                int att = PlayerPrefs.GetInt($"Lesson_{curUserId}_Attempts_{curLesId}", 0);
                PlayerPrefs.SetInt($"Lesson_{curUserId}_Attempts_{curLesId}", att + 1);
                PlayerPrefs.SetInt($"Lesson_{curUserId}_LastScore_{curLesId}", final10Scale);
                PlayerPrefs.SetString($"Lesson_{curUserId}_LastDate_{curLesId}", System.DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                PlayerPrefs.Save();
            }

            ResultSubmission submission = new ResultSubmission
            {
                userId = curUserId,
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
                Debug.Log("[QuizUIController] Ná»™p bÃ i thÃ nh cÃ´ng! Äang chuyá»ƒn sang Scene 07_Score...");
                SceneManager.LoadScene("07_Score");
            }

            // Fallback an toÃ n: DÃ¹ máº¡ng pháº£n há»“i hay khÃ´ng, tá»‘i Ä‘a 0.4s sáº½ chuyá»ƒn sang Scene Äiá»ƒm
            StartCoroutine(SafetyScoreTransition(NavigateToScore));

            if (networkService != null)
            {
                if (ScoreSessionContext.IsVersusMode && ScoreSessionContext.IsRealMultiplayer && !string.IsNullOrEmpty(ScoreSessionContext.VersusRoomId))
                {
                    try
                    {
                        networkService.SubmitVersusScore(ScoreSessionContext.VersusRoomId, submission.userId, ScoreSessionContext.Score, (ok, r, m) =>
                        {
                            Debug.Log($"[QuizUIController] SubmitVersusScore: {ok} -> {m}");
                        });
                    }
                    catch (System.Exception exVs)
                    {
                        Debug.LogWarning("[QuizUIController] SubmitVersusScore exception: " + exVs.Message);
                    }
                }

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
            // Ná»™p bÃ i sá»›m: Láº­p tá»©c dá»«ng lÃ m bÃ i, hiá»ƒn thá»‹ tráº¡ng thÃ¡i vÃ  chuyá»ƒn sang Scene 07_Score
            if (closeQuizButton != null)
            {
                closeQuizButton.interactable = false;
                var btnTxt = closeQuizButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnTxt != null) btnTxt.text = "Äang ná»™p...";
            }

            CompleteQuiz();
        }

        private string FormatMathString(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return Math6Companion.Core.MathTextSanitizer.SanitizeForDisplay(text);
        }
    }
}