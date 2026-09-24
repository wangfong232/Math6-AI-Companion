using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;

namespace Math6Companion.UI
{
    /// <summary>
    /// Điều khiển giao diện Menu chính (02_MainMenu.unity).
    /// Hiển thị thông tin người chơi và điều hướng đến 6 phân hệ cốt lõi.
    /// </summary>
    public class MainMenuUIController : MonoBehaviour
    {
        [Header("Profile Header")]
        [SerializeField] private TextMeshProUGUI nicknameText;
        [SerializeField] private TextMeshProUGUI userSubInfoText;
        [SerializeField] private Image avatarImage;

        [Header("Core Feature Buttons")]
        [SerializeField] private Button lessonListButton;      // 1. Danh sách bài học
        [SerializeField] private Button mcqPracticeButton;     // 2. Luyện tập trắc nghiệm
        [SerializeField] private Button essayPracticeButton;   // 3. Luyện tập tự luận
        [SerializeField] private Button versusLobbyButton;     // 4. Thi đấu đối kháng
        [SerializeField] private Button practiceLogButton;     // 5. Nhật ký luyện tập
        [SerializeField] private Button settingsButton;        // 6. Cài đặt

        [Header("System Buttons")]
        [SerializeField] private Button logoutButton;

        private void Start()
        {
            UpdateUserProfileUI();

            // Đăng ký sự kiện các nút
            if (lessonListButton != null)
                lessonListButton.onClick.AddListener(() => NavigateToScene("03_LessonList"));

            if (mcqPracticeButton != null)
                mcqPracticeButton.onClick.AddListener(() => NavigateToScene("05_MCQPractice"));

            if (essayPracticeButton != null)
                essayPracticeButton.onClick.AddListener(() => NavigateToScene("06_EssayPractice"));

            if (versusLobbyButton != null)
                versusLobbyButton.onClick.AddListener(() => NavigateToScene("09_VersusLobby"));

            if (practiceLogButton != null)
                practiceLogButton.onClick.AddListener(() => NavigateToScene("10_PracticeLog"));

            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => NavigateToScene("11_Settings"));

            if (logoutButton != null)
                logoutButton.onClick.AddListener(OnLogoutClicked);
        }

        private void UpdateUserProfileUI()
        {
            if (SessionManager.Instance != null && SessionManager.Instance.CurrentUser != null)
            {
                var user = SessionManager.Instance.CurrentUser;
                if (nicknameText != null)
                {
                    nicknameText.text = !string.IsNullOrEmpty(user.nickname) ? user.nickname : user.fullName;
                }

                if (userSubInfoText != null)
                {
                    userSubInfoText.text = $"Lớp: {user.classId} | {user.fullName}";
                }
            }
            else
            {
                // Dữ liệu mặc định nếu chạy trực tiếp Scene Menu khi test trong Unity Editor
                if (nicknameText != null) nicknameText.text = "Học sinh Toán 6";
                if (userSubInfoText != null) userSubInfoText.text = "Lớp 6A1 | Chưa đăng nhập";
            }
        }

        private void NavigateToScene(string sceneName)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
            }

            SceneManager.LoadScene(sceneName);
        }

        private void OnLogoutClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
            }

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.ClearSession();
            }

            SceneManager.LoadScene("01_Login");
        }
    }
}
