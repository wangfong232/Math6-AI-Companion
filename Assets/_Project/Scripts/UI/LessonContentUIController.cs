using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;

namespace Math6Companion.UI
{
    /// <summary>
    /// Điều khiển giao diện Nội dung bài học (04_LessonContent.unity).
    /// Hỗ trợ chuyển đổi 2 Tab: Lý thuyết Text & Video bài giảng minh họa.
    /// </summary>
    public class LessonContentUIController : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI lessonTitleText;
        [SerializeField] private Button backToListButton;

        [Header("Tab Buttons")]
        [SerializeField] private Button textTabButton;
        [SerializeField] private Button videoTabButton;
        [SerializeField] private Image textTabHighlight;
        [SerializeField] private Image videoTabHighlight;

        [Header("Panels")]
        [SerializeField] private GameObject textContentPanel;
        [SerializeField] private GameObject videoContentPanel;

        [Header("Text Tab Content")]
        [SerializeField] private TextMeshProUGUI theoryBodyText;

        [Header("Video Tab Content")]
        [SerializeField] private TextMeshProUGUI videoInfoText;
        [SerializeField] private Button playExternalVideoButton;

        [Header("Quick Practice Actions")]
        [SerializeField] private Button startMcqPracticeButton;
        [SerializeField] private Button startEssayPracticeButton;

        private string currentVideoUrl = "";

        private void Start()
        {
            if (backToListButton != null)
                backToListButton.onClick.AddListener(OnBackToListClicked);

            if (textTabButton != null)
                textTabButton.onClick.AddListener(() => SwitchTab("TEXT"));

            if (videoTabButton != null)
                videoTabButton.onClick.AddListener(() => SwitchTab("VIDEO"));

            if (playExternalVideoButton != null)
                playExternalVideoButton.onClick.AddListener(OnOpenVideoUrl);

            if (startMcqPracticeButton != null)
            {
                startMcqPracticeButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    SceneManager.LoadScene("05_MCQPractice");
                });
            }

            if (startEssayPracticeButton != null)
            {
                startEssayPracticeButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    SceneManager.LoadScene("06_EssayPractice");
                });
            }

            LoadLessonDetails();

            // Mở tab ưu tiên đã chọn từ LessonList
            SwitchTab(LessonListUIController.PreferredContentTab);
        }

        private void LoadLessonDetails()
        {
            var lesson = LessonListUIController.CurrentSelectedLesson;
            if (lesson != null)
            {
                if (lessonTitleText != null) lessonTitleText.text = lesson.title;
                if (theoryBodyText != null) theoryBodyText.text = lesson.summaryText;
                currentVideoUrl = lesson.videoUrl;

                if (videoInfoText != null)
                {
                    videoInfoText.text = $"Video bài giảng: {lesson.title}\nNguồn: Bộ GD&ĐT Toán 6\nĐường dẫn: {currentVideoUrl}";
                }
            }
            else
            {
                // Dữ liệu mẫu khi test trực tiếp Scene trong Editor
                if (lessonTitleText != null) lessonTitleText.text = "Bài 1: Phép cộng và trừ số nguyên";
                if (theoryBodyText != null)
                {
                    theoryBodyText.text = "1. Cộng hai số nguyên cùng dấu:\n- Muốn cộng hai số nguyên âm, ta cộng hai phần tự nhiên rồi đặt dấu '-' trước kết quả.\nVí dụ: (-5) + (-3) = -(5 + 3) = -8.\n\n2. Cộng hai số nguyên khác dấu:\n- Lấy số có phần tự nhiên lớn hơn trừ số có phần tự nhiên bé hơn, đặt dấu của số có phần tự nhiên lớn hơn trước hiệu.\nVí dụ: (-15) + 7 = -(15 - 7) = -8.";
                }
                currentVideoUrl = "https://www.youtube.com";
                if (videoInfoText != null) videoInfoText.text = "Video bài giảng minh họa Toán 6";
            }
        }

        public void SwitchTab(string tabName)
        {
            PlayClick();
            bool isText = tabName == "TEXT";

            if (textContentPanel != null) textContentPanel.SetActive(isText);
            if (videoContentPanel != null) videoContentPanel.SetActive(!isText);

            if (textTabHighlight != null) textTabHighlight.gameObject.SetActive(isText);
            if (videoTabHighlight != null) videoTabHighlight.gameObject.SetActive(!isText);
        }

        private void OnOpenVideoUrl()
        {
            PlayClick();
            if (!string.IsNullOrEmpty(currentVideoUrl))
            {
                Application.OpenURL(currentVideoUrl);
            }
        }

        private void OnBackToListClicked()
        {
            PlayClick();
            SceneManager.LoadScene("03_LessonList");
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
