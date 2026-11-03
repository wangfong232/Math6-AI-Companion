using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;

namespace Math6Companion.UI
{
    /// <summary>
    /// Điều khiển Nhật ký luyện tập (10_PracticeLog.unity).
    /// Mặc định hiển thị Màn hình 1: Tổng hợp thành tích.
    /// Có nút [Chi tiết theo bài] chuyển sang Màn hình 2: Chọn từng bài xem Số lần làm, Điểm cao nhất, Điểm lần cuối.
    /// </summary>
    public class PracticeLogUIController : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button toggleModeButton;
        [SerializeField] private TextMeshProUGUI toggleModeButtonText;

        [Header("Mode 1: Summary Panel (Mặc định)")]
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private TextMeshProUGUI totalLessonsCompletedText;
        [SerializeField] private TextMeshProUGUI averageScoreText;
        [SerializeField] private TextMeshProUGUI totalAttemptsText;
        [SerializeField] private TextMeshProUGUI totalTimeText;

        [Header("Mode 2: By-Lesson Detail Panel")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private TMP_Dropdown lessonDropdown;
        [SerializeField] private Button lessonSelectButton;
        [SerializeField] private Button prevLessonButton;
        [SerializeField] private Button nextLessonButton;
        [SerializeField] private TextMeshProUGUI selectedLessonTitleText;
        [SerializeField] private TextMeshProUGUI attemptsCountText;
        [SerializeField] private TextMeshProUGUI highestScoreText;
        [SerializeField] private TextMeshProUGUI lastScoreText;
        [SerializeField] private TextMeshProUGUI lastDateText;

        private bool isDetailMode = false;
        private int currentLessonIndex = 0;
        private TextMeshProUGUI fallbackDropdownLabel;

        private struct MockLessonLog
        {
            public string lessonId;
            public string title;
            public int attempts;
            public int highestScore;
            public int lastScore;
            public string lastDate;
        }

        private List<MockLessonLog> mockLogs = new List<MockLessonLog>();

        private void Awake()
        {
            if (lessonDropdown != null && lessonDropdown.template != null)
            {
                var templateScroll = lessonDropdown.template.GetComponentInChildren<ScrollRect>();
                if (templateScroll != null)
                {
                    templateScroll.scrollSensitivity = 48f;
                    templateScroll.inertia = true;
                    templateScroll.decelerationRate = 0.135f;
                }
            }

            if (lessonDropdown != null && lessonDropdown.template == null)
            {
                var dropGo = lessonDropdown.gameObject;
                DestroyImmediate(lessonDropdown);
                lessonDropdown = null;

                lessonSelectButton = dropGo.GetComponent<Button>() ?? dropGo.AddComponent<Button>();
            }

            if (lessonSelectButton == null && lessonDropdown == null)
            {
                var dropGo = GameObject.Find("LessonDropdown");
                if (dropGo != null)
                {
                    var tmpDrop = dropGo.GetComponent<TMP_Dropdown>();
                    if (tmpDrop != null && tmpDrop.template == null)
                    {
                        DestroyImmediate(tmpDrop);
                        lessonSelectButton = dropGo.GetComponent<Button>() ?? dropGo.AddComponent<Button>();
                    }
                    else if (tmpDrop == null)
                    {
                        lessonSelectButton = dropGo.GetComponent<Button>() ?? dropGo.AddComponent<Button>();
                    }
                }
            }

            if (lessonSelectButton != null)
            {
                lessonSelectButton.onClick.RemoveAllListeners();
                lessonSelectButton.onClick.AddListener(OnNextLessonClicked);

                var existingLabel = lessonSelectButton.GetComponentInChildren<TextMeshProUGUI>();
                if (existingLabel == null)
                {
                    var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    labelGo.transform.SetParent(lessonSelectButton.transform, false);
                    var lrt = labelGo.GetComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero;
                    lrt.anchorMax = Vector2.one;
                    lrt.sizeDelta = Vector2.zero;
                    lrt.anchoredPosition = Vector2.zero;

                    fallbackDropdownLabel = labelGo.GetComponent<TextMeshProUGUI>();
                    fallbackDropdownLabel.fontSize = 20;
                    fallbackDropdownLabel.fontStyle = FontStyles.Bold;
                    fallbackDropdownLabel.color = Color.white;
                    fallbackDropdownLabel.alignment = TextAlignmentOptions.Center;
                    fallbackDropdownLabel.raycastTarget = false;
                }
                else
                {
                    fallbackDropdownLabel = existingLabel;
                    fallbackDropdownLabel.raycastTarget = false;
                }
            }
        }

        private void Start()
        {
            if (backToMenuButton == null)
            {
                var b = GameObject.Find("Btn_BackToMenu") ?? GameObject.Find("BackToMenuButton");
                if (b != null) backToMenuButton = b.GetComponent<Button>();
            }
            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);

            if (toggleModeButton == null)
            {
                var tb = GameObject.Find("Btn_ToggleMode") ?? GameObject.Find("ToggleModeButton");
                if (tb != null) toggleModeButton = tb.GetComponent<Button>();
            }
            if (toggleModeButton != null)
                toggleModeButton.onClick.AddListener(OnToggleModeClicked);

            if (prevLessonButton == null)
            {
                var pb = GameObject.Find("Btn_PrevLesson") ?? GameObject.Find("PrevLessonButton");
                if (pb != null) prevLessonButton = pb.GetComponent<Button>();
            }
            if (prevLessonButton != null)
                prevLessonButton.onClick.AddListener(OnPrevLessonClicked);

            if (nextLessonButton == null)
            {
                var nb = GameObject.Find("Btn_NextLesson") ?? GameObject.Find("NextLessonButton");
                if (nb != null) nextLessonButton = nb.GetComponent<Button>();
            }
            if (nextLessonButton != null)
                nextLessonButton.onClick.AddListener(OnNextLessonClicked);

            if (lessonDropdown != null)
            {
                lessonDropdown.onValueChanged.AddListener(OnLessonDropdownChanged);
            }

            string target = PlayerPrefs.GetString("PracticeLog_TargetLesson", "");
            if (!string.IsNullOrEmpty(target))
            {
                isDetailMode = true;
            }

            LoadLivePracticeData();
            UpdateViewMode();
        }

        public void LoadLivePracticeData()
        {
            var networkService = Math6Companion.Core.Services.NetworkServiceProvider.NetworkService;
            string userId = (SessionManager.Instance != null && SessionManager.Instance.CurrentUser != null && !string.IsNullOrEmpty(SessionManager.Instance.CurrentUser.userId))
                ? SessionManager.Instance.CurrentUser.userId
                : PlayerPrefs.GetString("User_ID", "STU_001");

            // Khởi tạo trước dữ liệu dự phòng rỗng cho người dùng hiện tại
            InitDefaultLessonLogs();

            if (networkService != null)
            {
                networkService.GetLessons("ALL", (lOk, lessons, lMsg) =>
                {
                    if (this == null || gameObject == null) return;

                    networkService.GetStudentAnalytics(userId, (anOk, analytics, anMsg) =>
                    {
                        if (this == null || gameObject == null) return;

                        if (anOk && analytics != null)
                        {
                            // 2. Cập nhật chi tiết từng bài học từ Sheet kết hợp bộ nhớ cục bộ theo đúng userId
                            if (lessons != null && lessons.Count > 0)
                            {
                                mockLogs.Clear();
                                int completedCount = 0;
                                float totalScoreSum = 0;
                                int totalAttemptsSum = 0;

                                foreach (var les in lessons)
                                {
                                    var prog = analytics.progressList?.Find(p => p.lessonId == les.lessonId);
                                    int localHigh = PlayerPrefs.GetInt($"Lesson_{userId}_HighestScore_{les.lessonId}", -1);
                                    int localAtt = PlayerPrefs.GetInt($"Lesson_{userId}_Attempts_{les.lessonId}", 0);
                                    string localDate = PlayerPrefs.GetString($"Lesson_{userId}_LastDate_{les.lessonId}", "");

                                    int att = (prog != null && prog.attempts > 0) ? prog.attempts : Mathf.Max(localAtt, 0);
                                    int high = (prog != null && prog.highestScore > 0) ? prog.highestScore : Mathf.Max(localHigh, 0);
                                    int last = (prog != null && prog.lastScore > 0) ? prog.lastScore : high;
                                    string dateStr = !string.IsNullOrEmpty(localDate) ? localDate : (att > 0 ? "Gần đây" : "Chưa làm");

                                    if (att > 0 || high > 0)
                                    {
                                        completedCount++;
                                        totalScoreSum += high;
                                        totalAttemptsSum += Mathf.Max(att, 1);
                                    }

                                    mockLogs.Add(new MockLessonLog
                                    {
                                        lessonId = les.lessonId,
                                        title = MathTextSanitizer.SanitizeForDisplay(les.title),
                                        attempts = att,
                                        highestScore = high,
                                        lastScore = last,
                                        lastDate = dateStr
                                    });
                                }

                                // 1. Cập nhật thống kê tổng quan chính xác theo số bài của chính học sinh này
                                int finalCompleted = completedCount;
                                float finalAvg = completedCount > 0 ? (totalScoreSum / completedCount) : 0f;
                                int finalAttempts = totalAttemptsSum;

                                if (totalLessonsCompletedText != null) 
                                    totalLessonsCompletedText.text = $"{finalCompleted} / {lessons.Count} Bài";
                                
                                if (averageScoreText != null) 
                                    averageScoreText.text = finalCompleted > 0 ? $"{finalAvg:F1} / 10" : "0.0 / 10";

                                if (totalAttemptsText != null) 
                                    totalAttemptsText.text = $"{finalAttempts} lần";

                                if (totalTimeText != null) 
                                    totalTimeText.text = $"{finalAttempts * 5} phút";

                                string targetLesson = PlayerPrefs.GetString("PracticeLog_TargetLesson", "");
                                if (!string.IsNullOrEmpty(targetLesson))
                                {
                                    int matchIdx = mockLogs.FindIndex(m => m.lessonId == targetLesson || m.title.Contains(targetLesson));
                                    if (matchIdx >= 0)
                                    {
                                        currentLessonIndex = matchIdx;
                                    }
                                    PlayerPrefs.DeleteKey("PracticeLog_TargetLesson");
                                }

                                RefreshDropdownOptions();
                            }
                        }
                    });
                });
            }
        }

        private void InitDefaultLessonLogs()
        {
            mockLogs = new List<MockLessonLog>();
            RefreshDropdownOptions();

            // Điền trạng thái khởi tạo an toàn
            if (totalLessonsCompletedText != null) totalLessonsCompletedText.text = "0 / 0 Bài";
            if (averageScoreText != null) averageScoreText.text = "0.0 / 10";
            if (totalAttemptsText != null) totalAttemptsText.text = "0 lần";
            if (totalTimeText != null) totalTimeText.text = "0 phút";
        }

        private void RefreshDropdownOptions()
        {
            if (lessonDropdown != null && lessonDropdown.enabled && lessonDropdown.template != null)
            {
                lessonDropdown.ClearOptions();
                var options = new List<string>();
                foreach (var item in mockLogs)
                {
                    options.Add(item.title);
                }
                lessonDropdown.AddOptions(options);
            }

            if (isDetailMode)
            {
                OnLessonDropdownChanged(currentLessonIndex);
            }
        }

        private void OnToggleModeClicked()
        {
            PlayClick();
            isDetailMode = !isDetailMode;
            UpdateViewMode();
        }

        private void OnPrevLessonClicked()
        {
            PlayClick();
            if (mockLogs.Count == 0) return;
            currentLessonIndex--;
            if (currentLessonIndex < 0) currentLessonIndex = mockLogs.Count - 1;

            if (lessonDropdown != null && lessonDropdown.enabled) lessonDropdown.value = currentLessonIndex;
            OnLessonDropdownChanged(currentLessonIndex);
        }

        private void OnNextLessonClicked()
        {
            PlayClick();
            if (mockLogs.Count == 0) return;
            currentLessonIndex++;
            if (currentLessonIndex >= mockLogs.Count) currentLessonIndex = 0;

            if (lessonDropdown != null && lessonDropdown.enabled) lessonDropdown.value = currentLessonIndex;
            OnLessonDropdownChanged(currentLessonIndex);
        }

        private void UpdateViewMode()
        {
            if (summaryPanel != null) summaryPanel.SetActive(!isDetailMode);
            if (detailPanel != null) detailPanel.SetActive(isDetailMode);

            if (toggleModeButtonText != null)
            {
                toggleModeButtonText.text = isDetailMode ? "< Xem Tổng Hợp" : "Xem Chi Tiết Theo Bài >";
            }

            if (isDetailMode)
            {
                OnLessonDropdownChanged(currentLessonIndex);
            }
        }

        private void OnLessonDropdownChanged(int index)
        {
            if (index < 0 || index >= mockLogs.Count) return;
            currentLessonIndex = index;
            var log = mockLogs[index];

            if (fallbackDropdownLabel != null)
            {
                fallbackDropdownLabel.text = $"[Bài {index + 1}/{mockLogs.Count}] {log.title}  (Bấm để đổi bài >)";
            }

            int normHighest = LessonListUIController.GetNormalizedScore(log.highestScore);
            int normLast = LessonListUIController.GetNormalizedScore(log.lastScore);

            if (selectedLessonTitleText != null) selectedLessonTitleText.text = log.title;
            if (attemptsCountText != null) attemptsCountText.text = $"{log.attempts} lần làm bài";
            if (highestScoreText != null) highestScoreText.text = $"{normHighest} / 10 điểm";
            if (lastScoreText != null) lastScoreText.text = $"{normLast} / 10 điểm";
            if (lastDateText != null) lastDateText.text = $"Lần cuối: {log.lastDate}";
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
