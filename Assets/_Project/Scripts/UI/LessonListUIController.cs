using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;
using Math6Companion.Core.Contracts;
using Math6Companion.Core.Mock;

namespace Math6Companion.UI
{
    /// <summary>
    /// Điều khiển giao diện Danh sách bài học (03_LessonList.unity).
    /// Hiển thị danh mục chuyên đề, điểm cao nhất, vinh danh học sinh chăm chỉ nhất
    /// và mở Action Popup để chọn phương thức học tập.
    /// </summary>
    public class LessonListUIController : MonoBehaviour
    {
        [Header("Header & Navigation")]
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private TMP_Dropdown topicFilterDropdown;

        [Header("Lesson List Container")]
        [SerializeField] private Transform lessonListContent;
        [SerializeField] private GameObject lessonCardPrefab;

        [Header("Action Popup Modal")]
        [SerializeField] private GameObject actionModal;
        [SerializeField] private TextMeshProUGUI modalLessonTitle;
        [SerializeField] private TextMeshProUGUI modalStatsText;
        [SerializeField] private Button textLessonButton;
        [SerializeField] private Button videoLessonButton;
        [SerializeField] private Button mcqPracticeButton;
        [SerializeField] private Button essayPracticeButton;
        [SerializeField] private Button practiceLogButton;
        [SerializeField] private Button closeModalButton;

        private INetworkService networkService;
        private List<LessonData> currentLessons = new List<LessonData>();
        private LessonData selectedLesson;

        // Anki / Spaced Repetition Tab Components
        private Button lessonsTabButton;
        private Button ankiTabButton;
        private Image lessonsTabImage;
        private Image ankiTabImage;
        private TextMeshProUGUI ankiTabBadgeText;
        private bool isAnkiTabActive = false;

        // Lưu bài học đang chọn vào static variable hoặc PlayerPrefs để Scene khác đọc
        public static LessonData CurrentSelectedLesson { get; set; }
        public static string PreferredContentTab { get; set; } = "TEXT";

        private void Awake()
        {
            // Tối ưu tốc độ cuộn danh sách bài học siêu mượt
            var scroll = GetComponentInChildren<ScrollRect>() ?? FindAnyObjectByType<ScrollRect>();
            if (scroll != null)
            {
                scroll.scrollSensitivity = 38f;
                scroll.inertia = true;
                scroll.decelerationRate = 0.135f;
                scroll.movementType = ScrollRect.MovementType.Elastic;
            }
        }

        private void Start()
        {
            networkService = Math6Companion.Core.Services.NetworkServiceProvider.NetworkService;

            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);

            if (closeModalButton != null)
                closeModalButton.onClick.AddListener(CloseActionModal);

            if (actionModal != null)
                actionModal.SetActive(false);

            BindModalButtons();
            SetupHeaderNavigationTabs();

            if (topicFilterDropdown != null)
            {
                topicFilterDropdown.ClearOptions();
                topicFilterDropdown.AddOptions(new List<string>
                {
                    "Tất cả bài học (Toán 6)",
                    "1. Tập hợp & Số tự nhiên",
                    "2. Số nguyên",
                    "3. Phân số",
                    "4. Số thập phân",
                    "5. Hình học trực quan"
                });
                topicFilterDropdown.onValueChanged.RemoveAllListeners();
                topicFilterDropdown.onValueChanged.AddListener(OnFilterChanged);
            }

            LoadLessons("ALL");
        }

        private void SetupHeaderNavigationTabs()
        {
            // Tạo thanh Tab Chuyển đổi: [ 📖 DANH SÁCH BÀI HỌC ] và [ 🔥 SỔ TAY CÂU SAI (ANKI / QUIZLET) ]
            var topBar = GameObject.Find("TopBar") ?? GameObject.Find("Header") ?? transform.Find("TopBar")?.gameObject;
            if (topBar == null && transform.childCount > 0) topBar = transform.GetChild(0).gameObject;
            if (topBar == null) return;

            var tabContainer = topBar.transform.Find("NavTabsContainer")?.gameObject;
            if (tabContainer == null)
            {
                tabContainer = new GameObject("NavTabsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                tabContainer.transform.SetParent(topBar.transform, false);
                var rt = tabContainer.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(100, 0);
                rt.sizeDelta = new Vector2(460, 42);

                var hlg = tabContainer.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 12;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
            }

            // 1. Tab Bài Học
            var btn1Go = tabContainer.transform.Find("Tab_Lessons")?.gameObject;
            if (btn1Go == null)
            {
                btn1Go = new GameObject("Tab_Lessons", typeof(RectTransform), typeof(Image), typeof(Button));
                btn1Go.transform.SetParent(tabContainer.transform, false);
                var rt1 = btn1Go.GetComponent<RectTransform>();
                rt1.sizeDelta = new Vector2(200, 40);

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(btn1Go.transform, false);
                var txtRt = txtGo.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
                var tmp = txtGo.GetComponent<TextMeshProUGUI>();
                tmp.text = "<b>[📖] DANH SÁCH BÀI</b>";
                tmp.fontSize = 14;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }
            lessonsTabButton = btn1Go.GetComponent<Button>();
            lessonsTabImage = btn1Go.GetComponent<Image>();
            lessonsTabButton.onClick.RemoveAllListeners();
            lessonsTabButton.onClick.AddListener(() => SwitchNavTab(false));

            // 2. Tab Sổ Tay Câu Sai (Anki / Quizlet)
            var btn2Go = tabContainer.transform.Find("Tab_AnkiWrongBank")?.gameObject;
            if (btn2Go == null)
            {
                btn2Go = new GameObject("Tab_AnkiWrongBank", typeof(RectTransform), typeof(Image), typeof(Button));
                btn2Go.transform.SetParent(tabContainer.transform, false);
                var rt2 = btn2Go.GetComponent<RectTransform>();
                rt2.sizeDelta = new Vector2(240, 40);

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(btn2Go.transform, false);
                var txtRt = txtGo.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
                ankiTabBadgeText = txtGo.GetComponent<TextMeshProUGUI>();
                ankiTabBadgeText.fontSize = 14;
                ankiTabBadgeText.alignment = TextAlignmentOptions.Center;
                ankiTabBadgeText.color = Color.white;
            }
            ankiTabButton = btn2Go.GetComponent<Button>();
            ankiTabImage = btn2Go.GetComponent<Image>();
            ankiTabButton.onClick.RemoveAllListeners();
            ankiTabButton.onClick.AddListener(() => SwitchNavTab(true));

            UpdateNavTabVisuals();
        }

        private void SwitchNavTab(bool showAnki)
        {
            PlayClick();
            isAnkiTabActive = showAnki;
            UpdateNavTabVisuals();

            if (topicFilterDropdown != null)
                topicFilterDropdown.gameObject.SetActive(!isAnkiTabActive);

            if (isAnkiTabActive)
            {
                RenderAnkiWrongQuestionBankView();
            }
            else
            {
                PopulateLessonList();
            }
        }

        private void UpdateNavTabVisuals()
        {
            int wrongCount = WrongQuestionBankManager.GetTotalWrongCount();

            if (lessonsTabImage != null)
            {
                lessonsTabImage.color = !isAnkiTabActive ? new Color(0.18f, 0.75f, 0.45f) : new Color(0.15f, 0.22f, 0.32f);
            }

            if (ankiTabImage != null)
            {
                ankiTabImage.color = isAnkiTabActive ? new Color(0.85f, 0.25f, 0.25f) : new Color(0.15f, 0.22f, 0.32f);
            }

            if (ankiTabBadgeText != null)
            {
                ankiTabBadgeText.text = wrongCount > 0 
                    ? $"<b>[🔥] ÔN CÂU SAI <color=#f1c40f>({wrongCount})</color></b>"
                    : "<b>[🔥] SỔ TAY CÂU SAI</b>";
            }
        }

        private void BindModalButtons()
        {
            if (textLessonButton != null)
            {
                textLessonButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    PreferredContentTab = "TEXT";
                    SceneManager.LoadScene("04_LessonContent");
                });
            }

            if (videoLessonButton != null)
            {
                videoLessonButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    PreferredContentTab = "VIDEO";
                    SceneManager.LoadScene("04_LessonContent");
                });
            }

            if (mcqPracticeButton != null)
            {
                mcqPracticeButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    PlayerPrefs.SetString("Current_Lesson_ID", selectedLesson != null ? selectedLesson.lessonId : "");
                    PlayerPrefs.SetString("Current_Lesson_Title", selectedLesson != null ? selectedLesson.title : "");
                    PlayerPrefs.SetString("Current_Lesson_Topic", selectedLesson != null ? selectedLesson.topic : "");
                    PlayerPrefs.SetInt("Quiz_IsWrongReviewMode", 0);
                    SceneManager.LoadScene("05_MCQPractice");
                });
            }

            if (essayPracticeButton != null)
            {
                essayPracticeButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    PlayerPrefs.SetString("Current_Lesson_ID", selectedLesson != null ? selectedLesson.lessonId : "");
                    PlayerPrefs.SetString("Current_Lesson_Title", selectedLesson != null ? selectedLesson.title : "");
                    PlayerPrefs.SetString("Current_Lesson_Topic", selectedLesson != null ? selectedLesson.topic : "");
                    SceneManager.LoadScene("06_EssayPractice");
                });
            }

            if (practiceLogButton != null)
            {
                practiceLogButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    PlayerPrefs.SetString("PracticeLog_TargetLesson", selectedLesson != null ? selectedLesson.lessonId : "");
                    SceneManager.LoadScene("10_PracticeLog");
                });
            }
        }

        public void LoadLessons(string topic)
        {
            if (networkService == null)
            {
                networkService = Math6Companion.Core.Services.NetworkServiceProvider.NetworkService;
            }

            if (networkService != null)
            {
                networkService.GetLessons(topic, (success, lessons, message) =>
                {
                    if (this == null || gameObject == null) return;

                    if (success && lessons != null && lessons.Count > 0)
                    {
                        currentLessons = lessons;
                    }
                    else
                    {
                        currentLessons = GetFallbackLessons(topic);
                    }

                    // Hiển thị danh sách bài học NGAY LẬP TỨC, không để người dùng phải chờ đợi
                    if (!isAnkiTabActive)
                    {
                        PopulateLessonList();
                    }

                    // Đồng bộ điểm cao nhất của chính học sinh từ Google Sheet Analytics (bất đồng bộ trong nền)
                    string currentUserId = PlayerPrefs.GetString("User_ID", "STU_001");
                    networkService.GetStudentAnalytics(currentUserId, (anOk, analytics, anMsg) =>
                    {
                        if (this == null || gameObject == null) return;

                        if (anOk && analytics != null && analytics.progressList != null)
                        {
                            bool hasScoreUpdate = false;
                            foreach (var p in analytics.progressList)
                            {
                                var matchLesson = currentLessons.Find(l => l.lessonId == p.lessonId);
                                if (matchLesson != null && p.highestScore > matchLesson.userHighestScore)
                                {
                                    matchLesson.userHighestScore = p.highestScore;
                                    hasScoreUpdate = true;
                                }
                            }

                            if (hasScoreUpdate && !isAnkiTabActive)
                            {
                                PopulateLessonList();
                            }
                        }
                    });
                });
            }
            else
            {
                currentLessons = GetFallbackLessons(topic);
                if (!isAnkiTabActive)
                {
                    PopulateLessonList();
                }
            }
        }

        private List<LessonData> GetFallbackLessons(string topic)
        {
            if (topic == "Fractions")
            {
                return new List<LessonData>
                {
                    new LessonData
                    {
                        lessonId = "LES_FRAC_01",
                        topic = "Fractions",
                        title = "Bài 1: Khái niệm phân số và phân số bằng nhau",
                        description = "Tìm hiểu phân số, tính chất cơ bản và rút gọn phân số",
                        grade = 6,
                        summaryText = "Phân số có dạng a/b với a, b là số nguyên, b khác 0.",
                        userHighestScore = 9,
                        topStudentNickname = "AnThầnĐồng",
                        topStudentAttempts = 12
                    },
                    new LessonData
                    {
                        lessonId = "LES_FRAC_02",
                        topic = "Fractions",
                        title = "Bài 2: Các phép tính với phân số",
                        description = "Cộng, trừ, nhân, chia phân số cùng mẫu và khác mẫu",
                        grade = 6,
                        summaryText = "Quy đồng mẫu số khi cộng trừ hai phân số khác mẫu.",
                        userHighestScore = 10,
                        topStudentNickname = "Trạng Tí",
                        topStudentAttempts = 20
                    }
                };
            }

            return new List<LessonData>
            {
                new LessonData
                {
                    lessonId = "LES_INT_01",
                    topic = "Integers",
                    title = "Bài 1: Phép cộng và trừ số nguyên",
                    description = "Quy tắc cộng trừ số nguyên cùng dấu và khác dấu",
                    grade = 6,
                    summaryText = "Cộng cùng dấu: giữ dấu cộng trị tuyệt đối. Khác dấu: lấy lớn trừ bé giữ dấu số lớn.",
                    userHighestScore = 10,
                    topStudentNickname = "BìnhToánHọc",
                    topStudentAttempts = 16
                },
                new LessonData
                {
                    lessonId = "LES_INT_02",
                    topic = "Integers",
                    title = "Bài 2: Phép nhân và chia hai số nguyên",
                    description = "Quy tắc nhân chia số nguyên, cùng dấu dương khác dấu âm",
                    grade = 6,
                    summaryText = "(+) * (+) = (+), (-) * (-) = (+), (+) * (-) = (-), (-) * (+) = (-)",
                    userHighestScore = 8,
                    topStudentNickname = "ChiToánTin",
                    topStudentAttempts = 10
                },
                new LessonData
                {
                    lessonId = "LES_INT_03",
                    topic = "Integers",
                    title = "Bài 3: Trục số nguyên và Thứ tự trong tập hợp Z",
                    description = "Biểu diễn số nguyên trên trục số, so sánh hai số nguyên",
                    grade = 6,
                    summaryText = "Trên trục số nằm ngang: điểm biểu diễn số bé nằm bên trái điểm biểu diễn số lớn.",
                    userHighestScore = 10,
                    topStudentNickname = "DũngThôngMinh",
                    topStudentAttempts = 14
                }
            };
        }

        private void PopulateLessonList()
        {
            if (lessonListContent == null) return;

            // Xóa danh sách cũ
            foreach (Transform child in lessonListContent)
            {
                Destroy(child.gameObject);
            }

            foreach (var lesson in currentLessons)
            {
                GameObject cardObj = null;
                if (lessonCardPrefab != null)
                {
                    cardObj = Instantiate(lessonCardPrefab, lessonListContent);
                    var texts = cardObj.GetComponentsInChildren<TextMeshProUGUI>();
                    if (texts.Length >= 1) texts[0].text = lesson.title;
                    if (texts.Length >= 2)
                    {
                        texts[1].text = $"Diem cao: {lesson.userHighestScore}/10 | Top: {lesson.topStudentNickname} ({lesson.topStudentAttempts} lan)";
                    }
                }
                else
                {
                    // Tự động tạo thẻ bài học hoàn chỉnh với đầy đủ Text, Layout và Button
                    cardObj = new GameObject($"Card_{lesson.lessonId}", typeof(RectTransform), typeof(Button), typeof(Image), typeof(LayoutElement));
                    cardObj.transform.SetParent(lessonListContent, false);

                    var le = cardObj.GetComponent<LayoutElement>();
                    le.preferredHeight = 110;
                    le.minHeight = 110;

                    var img = cardObj.GetComponent<Image>();
                    img.color = new Color(0.13f, 0.18f, 0.26f);

                    var btn = cardObj.GetComponent<Button>();
                    var nav = btn.navigation;
                    nav.mode = Navigation.Mode.None;
                    btn.navigation = nav;

                    var colors = btn.colors;
                    colors.normalColor = new Color(0.13f, 0.18f, 0.26f);
                    colors.highlightedColor = new Color(0.18f, 0.26f, 0.38f);
                    colors.pressedColor = new Color(0.10f, 0.14f, 0.20f);
                    btn.colors = colors;

                    // 1. Tiêu đề bài học
                    var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    titleGo.transform.SetParent(cardObj.transform, false);
                    var titleRt = titleGo.GetComponent<RectTransform>();
                    titleRt.anchorMin = new Vector2(0, 0.5f);
                    titleRt.anchorMax = new Vector2(0.68f, 1f);
                    titleRt.offsetMin = new Vector2(25, 0);
                    titleRt.offsetMax = new Vector2(-10, -12);

                    var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
                    titleTmp.text = lesson.title;
                    titleTmp.fontSize = 23;
                    titleTmp.fontStyle = FontStyles.Bold;
                    titleTmp.color = new Color(0.95f, 0.75f, 0.2f); // Vàng gold sang trọng
                    titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

                    // 2. Mô tả / Thời lượng
                    var subGo = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    subGo.transform.SetParent(cardObj.transform, false);
                    var subRt = subGo.GetComponent<RectTransform>();
                    subRt.anchorMin = new Vector2(0, 0);
                    subRt.anchorMax = new Vector2(0.68f, 0.5f);
                    subRt.offsetMin = new Vector2(25, 12);
                    subRt.offsetMax = new Vector2(-10, 0);

                    var subTmp = subGo.GetComponent<TextMeshProUGUI>();
                    subTmp.text = !string.IsNullOrEmpty(lesson.description) ? lesson.description : "Chuyên đề Toán 6 • Lý thuyết & Luyện tập";
                    subTmp.fontSize = 17;
                    subTmp.color = new Color(0.7f, 0.8f, 0.9f);
                    subTmp.alignment = TextAlignmentOptions.MidlineLeft;

                    // 3. Thông tin thành tích & Top
                    var statsGo = new GameObject("StatsBadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    statsGo.transform.SetParent(cardObj.transform, false);
                    var statsRt = statsGo.GetComponent<RectTransform>();
                    statsRt.anchorMin = new Vector2(0.68f, 0);
                    statsRt.anchorMax = new Vector2(1f, 1f);
                    statsRt.offsetMin = new Vector2(0, 10);
                    statsRt.offsetMax = new Vector2(-25, -10);

                    var statsTmp = statsGo.GetComponent<TextMeshProUGUI>();
                    int localHigh = PlayerPrefs.GetInt($"Lesson_HighestScore_{lesson.lessonId}", -1);
                    int localAtt = PlayerPrefs.GetInt($"Lesson_Attempts_{lesson.lessonId}", 0);
                    int normalizedScore = localHigh >= 0 ? localHigh : GetNormalizedScore(lesson.userHighestScore);
                    int attCount = localAtt > 0 ? localAtt : lesson.topStudentAttempts;
                    statsTmp.text = $"Điểm bạn: <color=#f1c40f><b>{normalizedScore}/10</b></color>\n<size=15><color=#95a5a6>Đã làm: {attCount} lần</color></size>";
                    statsTmp.fontSize = 18;
                    statsTmp.fontStyle = FontStyles.Bold;
                    statsTmp.color = new Color(0.18f, 0.75f, 0.45f); // Xanh ngọc
                    statsTmp.alignment = TextAlignmentOptions.MidlineRight;
                }

                var cardBtn = cardObj.GetComponent<Button>();
                if (cardBtn != null)
                {
                    var capturedLesson = lesson;
                    cardBtn.onClick.AddListener(() => OnLessonCardClicked(capturedLesson));
                }
            }
        }

        private void RenderAnkiWrongQuestionBankView()
        {
            if (lessonListContent == null) return;

            // Xóa danh sách cũ
            foreach (Transform child in lessonListContent)
            {
                Destroy(child.gameObject);
            }

            var wrongItems = WrongQuestionBankManager.GetAllWrongItems();
            int total = WrongQuestionBankManager.GetTotalWrongCount();
            int mastered = WrongQuestionBankManager.GetMasteredCount();
            int needReview = WrongQuestionBankManager.GetNeedReviewCount();

            // 1. Hero Summary Card (Bảng tổng quan Spaced Repetition)
            var heroCard = new GameObject("Hero_AnkiSummary", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            heroCard.transform.SetParent(lessonListContent, false);
            var leH = heroCard.GetComponent<LayoutElement>();
            leH.preferredHeight = 180;
            leH.minHeight = 180;
            heroCard.GetComponent<Image>().color = new Color(0.16f, 0.12f, 0.24f);

            var heroTxtGo = new GameObject("HeroText", typeof(RectTransform), typeof(TextMeshProUGUI));
            heroTxtGo.transform.SetParent(heroCard.transform, false);
            var rtHT = heroTxtGo.GetComponent<RectTransform>();
            rtHT.anchorMin = new Vector2(0, 0.4f);
            rtHT.anchorMax = new Vector2(1, 1);
            rtHT.offsetMin = new Vector2(20, 0);
            rtHT.offsetMax = new Vector2(-20, -10);
            var tmpHT = heroTxtGo.GetComponent<TextMeshProUGUI>();
            tmpHT.text = "<b>[🔥] SỔ TAY CÂU SAI • ANKI / QUIZLET (SPACED REPETITION)</b>\n" +
                         $"<size=16><color=#ecf0f1>Tự động lặp lại ngắt quãng các câu bạn từng làm sai để khắc phục điểm yếu.</color></size>\n" +
                         $"<size=17>Tổng câu sai: <color=#e74c3c><b>{total}</b></color>  |  Đã thuộc: <color=#2ecc71><b>{mastered}</b></color>  |  Cần ôn tập ngay: <color=#f1c40f><b>{needReview} câu</b></color></size>";
            tmpHT.fontSize = 20;
            tmpHT.color = new Color(0.95f, 0.77f, 0.06f);
            tmpHT.alignment = TextAlignmentOptions.TopLeft;

            // Nút bấm hành động trên Hero Card: [🚀 Ôn Tập Ngay] [🗑 Xóa Đã Thuộc] [➕ Thêm Câu Mẫu]
            var btnBar = new GameObject("HeroButtonBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            btnBar.transform.SetParent(heroCard.transform, false);
            var rtBB = btnBar.GetComponent<RectTransform>();
            rtBB.anchorMin = new Vector2(0, 0);
            rtBB.anchorMax = new Vector2(1, 0.4f);
            rtBB.offsetMin = new Vector2(20, 10);
            rtBB.offsetMax = new Vector2(-20, -5);
            var hlgB = btnBar.GetComponent<HorizontalLayoutGroup>();
            hlgB.spacing = 15;
            hlgB.childAlignment = TextAnchor.MiddleLeft;
            hlgB.childControlWidth = false;
            hlgB.childControlHeight = false;

            // Nút 1: Bắt đầu ôn tập Anki
            var startAnkiBtn = CreateActionButton(btnBar, "Btn_StartAnki", "<b>[🚀] BẮT ĐẦU ÔN TẬP ANKI</b>", new Vector2(260, 42), new Color(0.18f, 0.75f, 0.45f));
            startAnkiBtn.onClick.AddListener(() =>
            {
                PlayClick();
                PlayerPrefs.SetInt("Quiz_IsWrongReviewMode", 1);
                PlayerPrefs.SetString("Current_Lesson_ID", "");
                PlayerPrefs.SetString("Current_Lesson_Title", "SỔ TAY CÂU SAI (ANKI SPACED REPETITION)");
                SceneManager.LoadScene("05_MCQPractice");
            });

            // Nút 2: Xóa câu đã thuộc
            var cleanBtn = CreateActionButton(btnBar, "Btn_CleanMastered", "Xóa câu đã thuộc", new Vector2(170, 42), new Color(0.25f, 0.35f, 0.48f));
            cleanBtn.onClick.AddListener(() =>
            {
                PlayClick();
                WrongQuestionBankManager.RemoveMasteredItems();
                UpdateNavTabVisuals();
                RenderAnkiWrongQuestionBankView();
            });

            // Nút 3: Thêm câu mẫu (khi chưa có câu sai nào để test ngay)
            if (total == 0)
            {
                var sampleBtn = CreateActionButton(btnBar, "Btn_AddSample", "+ Thêm 3 câu mẫu để trải nghiệm", new Vector2(270, 42), new Color(0.85f, 0.45f, 0.15f));
                sampleBtn.onClick.AddListener(() =>
                {
                    PlayClick();
                    WrongQuestionBankManager.AddSampleWrongQuestions();
                    UpdateNavTabVisuals();
                    RenderAnkiWrongQuestionBankView();
                });
            }

            // 2. Danh sách các Flashcard câu hỏi sai
            if (wrongItems == null || wrongItems.Count == 0)
            {
                var emptyCard = new GameObject("EmptyCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                emptyCard.transform.SetParent(lessonListContent, false);
                var leE = emptyCard.GetComponent<LayoutElement>();
                leE.preferredHeight = 120;
                leE.minHeight = 120;
                emptyCard.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.20f);

                var emptyTxtGo = new GameObject("EmptyText", typeof(RectTransform), typeof(TextMeshProUGUI));
                emptyTxtGo.transform.SetParent(emptyCard.transform, false);
                var rtET = emptyTxtGo.GetComponent<RectTransform>();
                rtET.anchorMin = Vector2.zero; rtET.anchorMax = Vector2.one;
                rtET.offsetMin = new Vector2(20, 0); rtET.offsetMax = new Vector2(-20, 0);
                var tmpE = emptyTxtGo.GetComponent<TextMeshProUGUI>();
                tmpE.text = "🎉 <b>Tuyệt vời! Hiện bạn không có câu hỏi nào bị sai cần ôn tập.</b>\n<size=16><color=#7f8c8d>Hãy tiếp tục luyện tập các bài học hoặc bấm '+ Thêm 3 câu mẫu' ở trên để trải nghiệm chế độ ôn tập lặp lại ngắt quãng.</color></size>";
                tmpE.fontSize = 18;
                tmpE.alignment = TextAlignmentOptions.Center;
                tmpE.color = new Color(0.18f, 0.75f, 0.45f);
                return;
            }

            for (int i = 0; i < wrongItems.Count; i++)
            {
                var item = wrongItems[i];
                var card = new GameObject($"WrongCard_{i}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                card.transform.SetParent(lessonListContent, false);
                var le = card.GetComponent<LayoutElement>();
                le.preferredHeight = 135;
                le.minHeight = 135;
                card.GetComponent<Image>().color = new Color(0.11f, 0.16f, 0.24f);

                // Nội dung Flashcard câu hỏi
                var txtGo = new GameObject("CardText", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(card.transform, false);
                var rtT = txtGo.GetComponent<RectTransform>();
                rtT.anchorMin = new Vector2(0, 0);
                rtT.anchorMax = new Vector2(0.78f, 1);
                rtT.offsetMin = new Vector2(20, 10);
                rtT.offsetMax = new Vector2(-10, -10);
                var tmpT = txtGo.GetComponent<TextMeshProUGUI>();

                string statusBadge = item.reviewCorrectCount >= 2 
                    ? "<color=#2ecc71><b>[⭐ ĐÃ THUỘC - MASTERED]</b></color>"
                    : $"<color=#f39c12><b>[🔥 CẦN ÔN: {item.reviewCorrectCount}/2 lần đúng]</b></color>";

                tmpT.text = $"<b>Câu {i + 1}:</b> {item.prompt}  {statusBadge}\n" +
                            $"<size=15><color=#e74c3c>❌ Bạn đã chọn: {item.studentAnswer}</color>   |   <color=#2ecc71>✅ Đáp án đúng: {item.correctAnswer}</color></size>\n" +
                            $"<size=14><color=#bdc3c7>💡 {item.explanation}</color></size>";
                tmpT.fontSize = 17;
                tmpT.lineSpacing = 6;
                tmpT.alignment = TextAlignmentOptions.TopLeft;

                // Nút Ôn câu này bên phải card
                var reviewBtn = CreateActionButton(card, $"Btn_Review_{i}", "<b>[⚡] ÔN CÂU NÀY</b>", new Vector2(145, 45), new Color(0.85f, 0.25f, 0.25f));
                var rtRB = reviewBtn.GetComponent<RectTransform>();
                rtRB.anchorMin = new Vector2(1, 0.5f);
                rtRB.anchorMax = new Vector2(1, 0.5f);
                rtRB.pivot = new Vector2(1, 0.5f);
                rtRB.anchoredPosition = new Vector2(-20, 0);

                reviewBtn.onClick.AddListener(() =>
                {
                    PlayClick();
                    PlayerPrefs.SetInt("Quiz_IsWrongReviewMode", 1);
                    PlayerPrefs.SetString("Current_Lesson_ID", "");
                    PlayerPrefs.SetString("Current_Lesson_Title", "ÔN TẬP CÂU SAI");
                    SceneManager.LoadScene("05_MCQPractice");
                });
            }
        }

        private Button CreateActionButton(GameObject parent, string name, string label, Vector2 size, Color bgColor)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent.transform, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            btnGo.GetComponent<Image>().color = bgColor;

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            var rtT = txtGo.GetComponent<RectTransform>();
            rtT.anchorMin = Vector2.zero; rtT.anchorMax = Vector2.one;
            rtT.offsetMin = Vector2.zero; rtT.offsetMax = Vector2.zero;
            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnGo.GetComponent<Button>();
        }

        public static int GetNormalizedScore(int rawScore)
        {
            if (rawScore <= 0) return 0;
            if (rawScore <= 10) return rawScore;
            if (rawScore >= 50 && rawScore <= 60) return 10;
            if (rawScore % 10 == 0 && rawScore <= 100) return Mathf.Clamp(rawScore / 10, 1, 10);
            return Mathf.Clamp(Mathf.RoundToInt(rawScore / 10f), 0, 10);
        }

        private GameObject diffSelectorContainer;
        private Image[] diffTabImages;
        private string currentSelectedDifficulty = "ALL";

        private void EnsureDifficultySelectorUI()
        {
            if (actionModal == null || diffSelectorContainer != null) return;

            var card = actionModal.transform.Find("ModalCard") ?? actionModal.transform.Find("Card") ?? actionModal.transform;
            var cardRt = card.GetComponent<RectTransform>();
            if (cardRt != null)
            {
                cardRt.sizeDelta = new Vector2(650, 620);
            }

            // Tái cấu trúc tọa độ các thành phần trong Action Modal để không bị đè chữ
            if (modalLessonTitle != null)
            {
                var rt = modalLessonTitle.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, 255);
                rt.sizeDelta = new Vector2(550, 45);
            }
            if (modalStatsText != null)
            {
                var rt = modalStatsText.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, 205);
                rt.sizeDelta = new Vector2(550, 40);
            }

            if (textLessonButton != null)
            {
                var rt = textLessonButton.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, 80);
                rt.sizeDelta = new Vector2(520, 48);
            }
            if (videoLessonButton != null)
            {
                var rt = videoLessonButton.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, 22);
                rt.sizeDelta = new Vector2(520, 48);
            }
            if (mcqPracticeButton != null)
            {
                var rt = mcqPracticeButton.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, -36);
                rt.sizeDelta = new Vector2(520, 48);
            }
            if (essayPracticeButton != null)
            {
                var rt = essayPracticeButton.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, -94);
                rt.sizeDelta = new Vector2(520, 48);
            }
            if (practiceLogButton != null)
            {
                var rt = practiceLogButton.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, -152);
                rt.sizeDelta = new Vector2(520, 48);
            }
            if (closeModalButton != null)
            {
                var rt = closeModalButton.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(285, 275);
            }

            diffSelectorContainer = new GameObject("DifficultySelectorContainer", typeof(RectTransform));
            diffSelectorContainer.transform.SetParent(card, false);

            var diffRt = diffSelectorContainer.GetComponent<RectTransform>();
            diffRt.anchorMin = new Vector2(0.5f, 0.5f);
            diffRt.anchorMax = new Vector2(0.5f, 0.5f);
            diffRt.anchoredPosition = new Vector2(0, 142);
            diffRt.sizeDelta = new Vector2(520, 40);

            string[] diffLabels = { "Tất Cả", "Dễ (EASY)", "Vừa (MEDIUM)", "Khó (HARD)" };
            string[] diffValues = { "ALL", "EASY", "MEDIUM", "HARD" };
            diffTabImages = new Image[4];

            for (int i = 0; i < 4; i++)
            {
                int capturedIndex = i;
                string dVal = diffValues[i];

                var btnGo = new GameObject("Btn_Diff_" + dVal, typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(diffSelectorContainer.transform, false);

                var brt = btnGo.GetComponent<RectTransform>();
                brt.anchoredPosition = new Vector2(-195 + capturedIndex * 130, 0);
                brt.sizeDelta = new Vector2(120, 36);

                var img = btnGo.GetComponent<Image>();
                diffTabImages[i] = img;
                var btn = btnGo.GetComponent<Button>();

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(btnGo.transform, false);
                var txt = txtGo.GetComponent<TextMeshProUGUI>();
                txt.text = diffLabels[i];
                txt.fontSize = 14;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;

                btn.onClick.AddListener(() =>
                {
                    PlayClick();
                    SelectDifficulty(dVal);
                });
            }

            string savedDiff = PlayerPrefs.GetString("Quiz_Difficulty_Filter", "ALL");
            SelectDifficulty(savedDiff);
        }

        private void SelectDifficulty(string diff)
        {
            currentSelectedDifficulty = diff;
            PlayerPrefs.SetString("Quiz_Difficulty_Filter", diff);
            PlayerPrefs.Save();

            string[] diffValues = { "ALL", "EASY", "MEDIUM", "HARD" };
            if (diffTabImages != null)
            {
                for (int i = 0; i < diffTabImages.Length; i++)
                {
                    if (diffTabImages[i] != null)
                    {
                        bool isSelected = diffValues[i].Equals(diff, System.StringComparison.OrdinalIgnoreCase);
                        diffTabImages[i].color = isSelected ? new Color(0.18f, 0.75f, 0.45f) : new Color(0.2f, 0.28f, 0.4f);
                    }
                }
            }
        }

        private void OnLessonCardClicked(LessonData lesson)
        {
            PlayClick();
            selectedLesson = lesson;
            CurrentSelectedLesson = lesson;

            if (actionModal != null)
            {
                if (modalLessonTitle != null) modalLessonTitle.text = lesson.title;
                if (modalStatsText != null)
                {
                    int localHigh = PlayerPrefs.GetInt($"Lesson_HighestScore_{lesson.lessonId}", -1);
                    int localAtt = PlayerPrefs.GetInt($"Lesson_Attempts_{lesson.lessonId}", 0);
                    int normalizedScore = localHigh >= 0 ? localHigh : GetNormalizedScore(lesson.userHighestScore);
                    int attCount = localAtt > 0 ? localAtt : lesson.topStudentAttempts;
                    string lastDate = PlayerPrefs.GetString($"Lesson_LastDate_{lesson.lessonId}", "");

                    string stats = $"<b>Điểm cao nhất của bạn:</b> <color=#f1c40f><b>{normalizedScore}/10</b></color> (Đã luyện tập: <b>{attCount}</b> lần)";
                    if (!string.IsNullOrEmpty(lastDate))
                    {
                        stats += $"\n<size=15><color=#bdc3c7>Lần luyện gần nhất: {lastDate}</color></size>";
                    }
                    else if (!string.IsNullOrEmpty(lesson.topStudentNickname))
                    {
                        stats += $"\n<size=15><color=#bdc3c7>Top chăm chỉ: {lesson.topStudentNickname}</color></size>";
                    }
                    modalStatsText.text = stats;
                }
                EnsureDifficultySelectorUI();
                actionModal.SetActive(true);
            }
        }

        private void CloseActionModal()
        {
            PlayClick();
            if (actionModal != null) actionModal.SetActive(false);
        }

        private void OnFilterChanged(int index)
        {
            PlayClick();
            string topic = "ALL";
            switch (index)
            {
                case 0: topic = "ALL"; break;
                case 1: topic = "TapHop"; break;
                case 2: topic = "SoNguyen"; break;
                case 3: topic = "PhanSo"; break;
                case 4: topic = "SoThapPhan"; break;
                case 5: topic = "HinhHoc"; break;
                default: topic = "ALL"; break;
            }
            LoadLessons(topic);
        }

        private void OnBackToMenuClicked()
        {
            PlayClick();
            string target = SessionManager.Instance != null ? SessionManager.Instance.ReturnSceneName : "02_VillageWorld";
            SceneManager.LoadScene(target);
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
