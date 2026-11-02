using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Math6Companion.Core.Contracts;
using Math6Companion.Core.Mock;

namespace Math6Companion.UI
{
    public class LoginUIController : MonoBehaviour
    {
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("Buttons & Indicators")]
        [SerializeField] private Button loginButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingIndicator;
        [Header("Quick Test Login Buttons")]
        [SerializeField] private Button quickStudentButton;
        [SerializeField] private Button quickTeacherButton;

        private INetworkService networkService;

        private void Start()
        {
            // Kết nối với Network Service (Google Sheets Web App qua NetworkServiceProvider)
            networkService = Math6Companion.Core.Services.NetworkServiceProvider.NetworkService;

            // Làm sạch input và tắt rich text để tránh IME/Vietkey chèn tag <u>
            if (usernameInput != null)
            {
                usernameInput.richText = false;
                if (usernameInput.textComponent != null) usernameInput.textComponent.richText = false;
                usernameInput.onValueChanged.AddListener(val =>
                {
                    if (statusText != null && statusText.color == Color.red) statusText.text = string.Empty;
                });
            }

            if (passwordInput != null)
            {
                passwordInput.richText = false;
                if (passwordInput.textComponent != null) passwordInput.textComponent.richText = false;
                passwordInput.contentType = TMP_InputField.ContentType.Password;
                passwordInput.onValueChanged.AddListener(val =>
                {
                    if (statusText != null && statusText.color == Color.red) statusText.text = string.Empty;
                });
            }

            if (statusText != null)
            {
                statusText.text = string.Empty;
            }

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
            }

            if (loginButton != null)
            {
                loginButton.onClick.AddListener(OnLoginButtonClicked);
            }

            // Tự động tìm và liên kết 2 nút đăng nhập nhanh
            if (quickStudentButton == null)
            {
                var btn = GameObject.Find("Btn_QuickStudent");
                if (btn != null) quickStudentButton = btn.GetComponent<Button>();
            }
            if (quickStudentButton != null)
            {
                quickStudentButton.onClick.RemoveAllListeners();
                quickStudentButton.onClick.AddListener(() => QuickFillAccount("student_01", "123"));
            }

            if (quickTeacherButton == null)
            {
                var btn = GameObject.Find("Btn_QuickTeacher");
                if (btn != null) quickTeacherButton = btn.GetComponent<Button>();
            }
            if (quickTeacherButton != null)
            {
                quickTeacherButton.onClick.RemoveAllListeners();
                quickTeacherButton.onClick.AddListener(() => QuickFillAccount("teacher_01", "123"));
            }
        }

        private void Update()
        {
            // Cho phép bấm Enter để đăng nhập nhanh
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (loginButton != null && loginButton.interactable)
                {
                    OnLoginButtonClicked();
                }
            }
        }

        public void QuickFillAccount(string username, string password)
        {
            if (usernameInput != null)
            {
                usernameInput.text = username;
            }
            if (passwordInput != null)
            {
                passwordInput.text = password;
            }
            if (statusText != null)
            {
                statusText.text = $"Đã điền tài khoản: {username}. Đang đăng nhập...";
                statusText.color = Color.green;
            }

            // Tự động đăng nhập ngay khi bấm nút nhanh
            OnLoginButtonClicked();
        }

        private void OnLoginButtonClicked()
        {
            string username = usernameInput != null ? usernameInput.text.Trim() : string.Empty;
            string password = passwordInput != null ? passwordInput.text : string.Empty;

            // Kiểm tra tính hợp lệ cơ bản
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowStatus("Vui lòng nhập đầy đủ tài khoản và mật khẩu!", Color.red);
                return;
            }

            SetLoadingState(true);
            ShowStatus("Đang đăng nhập...", Color.yellow);

            // Gọi hàm đăng nhập qua Interface INetworkService
            networkService.Login(username, password, (success, userData, message) =>
            {
                SetLoadingState(false);

                if (success && userData != null)
                {
                    ShowStatus("Đăng nhập thành công!", Color.green);

                    // Đồng bộ phiên làm việc vào SessionManager
                    if (Math6Companion.Core.SessionManager.Instance != null)
                    {
                        Math6Companion.Core.SessionManager.Instance.SetUserSession(userData);
                    }
                    else
                    {
                        // Dự phòng nếu mở trực tiếp Scene 01_Login mà không qua 00_Bootstrapper
                        PlayerPrefs.SetString("User_ID", userData.userId);
                        PlayerPrefs.SetString("User_Username", userData.username);
                        PlayerPrefs.SetString("User_FullName", userData.fullName);
                        PlayerPrefs.SetString("User_Role", userData.role);
                        PlayerPrefs.SetInt("User_Grade", userData.grade);
                        PlayerPrefs.SetString("User_ClassId", userData.classId);
                        PlayerPrefs.SetString("User_Nickname", userData.nickname ?? userData.fullName ?? "Học sinh");
                        PlayerPrefs.Save();
                    }

                    // Chuyển sang Thế giới 3D Làng Sen Tri Thức
                    SceneManager.LoadScene("02_VillageWorld");
                }
                else
                {
                    ShowStatus(string.IsNullOrEmpty(message) ? "Đăng nhập thất bại!" : message, Color.red);
                }
            });
        }

        private void SetLoadingState(bool isLoading)
        {
            if (loginButton != null) loginButton.interactable = !isLoading;
            if (usernameInput != null) usernameInput.interactable = !isLoading;
            if (passwordInput != null) passwordInput.interactable = !isLoading;
            if (loadingIndicator != null) loadingIndicator.SetActive(isLoading);
        }

        private void ShowStatus(string msg, Color color)
        {
            if (statusText != null)
            {
                statusText.text = msg;
                statusText.color = color;
            }
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}