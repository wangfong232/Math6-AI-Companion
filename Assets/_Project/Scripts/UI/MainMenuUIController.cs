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
        [SerializeField] private Button enterVillage3DButton;  // 0. Bước vào Thế giới 3D Làng Sen
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
            if (enterVillage3DButton != null)
                enterVillage3DButton.onClick.AddListener(() => NavigateToScene("02_VillageWorld"));

            if (lessonListButton != null)
                lessonListButton.onClick.AddListener(() => NavigateToScene("03_LessonList"));

            if (mcqPracticeButton != null)
                mcqPracticeButton.onClick.AddListener(() =>
                {
                    PlayerPrefs.SetString("Current_Lesson_ID", "");
                    PlayerPrefs.SetString("Current_Lesson_Title", "");
                    PlayerPrefs.SetString("Current_Lesson_Topic", "ALL");
                    PlayerPrefs.SetInt("Quiz_IsWrongReviewMode", 0);
                    LessonListUIController.CurrentSelectedLesson = null;
                    NavigateToScene("05_MCQPractice");
                });

            if (essayPracticeButton != null)
                essayPracticeButton.onClick.AddListener(() =>
                {
                    PlayerPrefs.SetString("Current_Lesson_ID", "");
                    PlayerPrefs.SetString("Current_Lesson_Title", "");
                    PlayerPrefs.SetString("Current_Lesson_Topic", "ALL");
                    LessonListUIController.CurrentSelectedLesson = null;
                    NavigateToScene("06_EssayPractice");
                });

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
            string nick = "Hiệp Sĩ Toán 6";
            string cls = "6A1";
            string name = "Nguyễn Văn An";

            if (SessionManager.Instance != null && SessionManager.Instance.CurrentUser != null)
            {
                var user = SessionManager.Instance.CurrentUser;
                if (!string.IsNullOrEmpty(user.nickname)) nick = user.nickname;
                else if (!string.IsNullOrEmpty(user.fullName)) nick = user.fullName;

                if (!string.IsNullOrEmpty(user.classId)) cls = user.classId;
                if (!string.IsNullOrEmpty(user.fullName)) name = user.fullName;
            }
            else
            {
                nick = PlayerPrefs.GetString("User_Nickname", PlayerPrefs.GetString("User_FullName", "Hiệp Sĩ Toán 6"));
                cls = PlayerPrefs.GetString("User_ClassId", "6A1");
                name = PlayerPrefs.GetString("User_FullName", "Nguyễn Văn An");
            }

            if (nicknameText != null) nicknameText.text = nick;
            if (userSubInfoText != null) userSubInfoText.text = $"Lớp: {cls} | {name}";
        }

        private void NavigateToScene(string sceneName)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
            }

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.ReturnSceneName = sceneName == "02_VillageWorld" ? "02_VillageWorld" : "02_MainMenu";
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
