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

        private INetworkService networkService;

        private void Start()
        {
            // Kết nối với MockNetworkService (hoặc GoogleSheetsClient sau này khi M2 ghép vào)
            networkService = MockNetworkService.Instance;
            if (networkService == null)
            {
                networkService = FindAnyObjectByType<MockNetworkService>();
            }

            if (passwordInput != null)
            {
                passwordInput.contentType = TMP_InputField.ContentType.Password;
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

            // Băm SHA-256 mật khẩu theo đúng yêu cầu bảo mật
            string passHash = ComputeSha256Hash(password);

            // Gọi hàm đăng nhập qua Interface INetworkService
            networkService.Login(username, passHash, (success, userData, message) =>
            {
                SetLoadingState(false);

                if (success && userData != null)
                {
                    ShowStatus("Đăng nhập thành công!", Color.green);

                    // Lưu session học sinh/giáo viên vào PlayerPrefs
                    PlayerPrefs.SetString("User_ID", userData.userId);
                    PlayerPrefs.SetString("User_Username", userData.username);
                    PlayerPrefs.SetString("User_FullName", userData.fullName);
                    PlayerPrefs.SetString("User_Role", userData.role);
                    PlayerPrefs.SetInt("User_Grade", userData.grade);
                    PlayerPrefs.SetString("User_ClassId", userData.classId);
                    PlayerPrefs.Save();

                    // Chuyển Scene sau khi đăng nhập
                    SceneManager.LoadScene("02_Classroom");
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