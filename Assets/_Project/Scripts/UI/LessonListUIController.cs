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

        // Lưu bài học đang chọn vào static variable hoặc PlayerPrefs để Scene khác đọc
        public static LessonData CurrentSelectedLesson { get; private set; }
        public static string PreferredContentTab { get; set; } = "TEXT";

        private void Start()
        {
            networkService = MockNetworkService.Instance ?? FindAnyObjectByType<MockNetworkService>();

            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);

            if (closeModalButton != null)
                closeModalButton.onClick.AddListener(CloseActionModal);

            if (actionModal != null)
                actionModal.SetActive(false);

            BindModalButtons();

            if (topicFilterDropdown != null)
            {
                topicFilterDropdown.onValueChanged.AddListener(OnFilterChanged);
            }

            LoadLessons("Integers");
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
                    SceneManager.LoadScene("05_MCQPractice");
                });
            }

            if (essayPracticeButton != null)
            {
                essayPracticeButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    PlayerPrefs.SetString("Current_Lesson_ID", selectedLesson != null ? selectedLesson.lessonId : "");
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
            networkService.GetLessons(topic, (success, lessons, message) =>
            {
                if (success && lessons != null)
                {
                    currentLessons = lessons;
                    PopulateLessonList();
                }
            });
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
                    statsTmp.text = $"Điểm cao: {lesson.userHighestScore}/10\nTop: {lesson.topStudentNickname} ({lesson.topStudentAttempts} lần)";
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
                    modalStatsText.text = $"Điểm cao nhất: {lesson.userHighestScore}/10\nChăm chỉ nhất lớp: {lesson.topStudentNickname} ({lesson.topStudentAttempts} lần làm bài)";
                }
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
            string topic = index == 0 ? "Integers" : "Fractions";
            LoadLessons(topic);
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
