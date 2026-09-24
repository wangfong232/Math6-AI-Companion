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
            public string title;
            public int attempts;
            public int highestScore;
            public int lastScore;
            public string lastDate;
        }

        private List<MockLessonLog> mockLogs = new List<MockLessonLog>();

        private void Awake()
        {
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

            InitMockData();
            UpdateViewMode();
        }

        private void InitMockData()
        {
            mockLogs = new List<MockLessonLog>
            {
                new MockLessonLog { title = "Bài 1: Phép cộng và trừ số nguyên", attempts = 8, highestScore = 10, lastScore = 9, lastDate = "Hôm qua lúc 19:30" },
                new MockLessonLog { title = "Bài 2: Phép nhân và chia số nguyên", attempts = 5, highestScore = 8, lastScore = 8, lastDate = "2 ngày trước lúc 14:15" },
                new MockLessonLog { title = "Bài 3: Khái niệm & Rút gọn phân số", attempts = 6, highestScore = 9, lastScore = 7, lastDate = "3 ngày trước lúc 20:00" }
            };

            // Điền dữ liệu vào Dropdown nếu hợp lệ
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

            // Điền dữ liệu tổng hợp
            if (totalLessonsCompletedText != null) totalLessonsCompletedText.text = "3 / 3 Bài";
            if (averageScoreText != null) averageScoreText.text = "8.8 / 10";
            if (totalAttemptsText != null) totalAttemptsText.text = "19 lần";
            if (totalTimeText != null) totalTimeText.text = "1 giờ 45 phút";
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
                fallbackDropdownLabel.text = $"[Bài {index + 1}/3] {log.title}  (Bấm để đổi bài >)";
            }

            if (selectedLessonTitleText != null) selectedLessonTitleText.text = log.title;
            if (attemptsCountText != null) attemptsCountText.text = $"{log.attempts} lần làm bài";
            if (highestScoreText != null) highestScoreText.text = $"{log.highestScore} / 10 điểm";
            if (lastScoreText != null) lastScoreText.text = $"{log.lastScore} / 10 điểm";
            if (lastDateText != null) lastDateText.text = $"Lần cuối: {log.lastDate}";
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
