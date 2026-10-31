using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Math6Companion.Core
{
    /// <summary>
    /// Script gắn vào Scene 00_Bootstrapper để khởi tạo các Singleton Managers,
    /// tải dữ liệu thiết lập ban đầu và chuyển cảnh mượt mà sang 01_Login.
    /// </summary>
    public class AppBootstrapper : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Configuration")]
        [SerializeField] private float minimumBootTime = 1.5f;
        [SerializeField] private string nextSceneName = "01_Login";

        [Header("Manager Prefabs (Tùy chọn tạo sẵn)")]
        [SerializeField] private GameObject sessionManagerPrefab;
        [SerializeField] private GameObject audioManagerPrefab;
        [SerializeField] private GameObject mockNetworkPrefab;

        private void Start()
        {
            StartCoroutine(PerformBootSequence());
        }

        private IEnumerator PerformBootSequence()
        {
            UpdateProgress(0.1f, "Đang khởi tạo hệ thống...");

            // 1. Đảm bảo SessionManager tồn tại
            if (SessionManager.Instance == null)
            {
                if (sessionManagerPrefab != null)
                {
                    Instantiate(sessionManagerPrefab);
                }
                else
                {
                    var go = new GameObject("SessionManager");
                    go.AddComponent<SessionManager>();
                }
            }

            // Đảm bảo PlaySessionManager tồn tại
            _ = PlaySessionManager.Instance;

            // Đảm bảo TeacherConfigManager & TeacherPortalUIController sẵn sàng
            _ = TeacherConfigManager.Instance;
            _ = Math6Companion.UI.TeacherPortalUIController.Instance;
            yield return new WaitForSeconds(0.3f);

            // 2. Đảm bảo AudioManager tồn tại
            UpdateProgress(0.4f, "Cấu hình âm thanh & giao diện...");
            if (AudioManager.Instance == null)
            {
                if (audioManagerPrefab != null)
                {
                    Instantiate(audioManagerPrefab);
                }
                else
                {
                    var go = new GameObject("AudioManager");
                    go.AddComponent<AudioManager>();
                }
            }
            yield return new WaitForSeconds(0.3f);

            // 3. Khởi tạo Service Layer (Google Sheets API & Gemini AI Service)
            UpdateProgress(0.7f, "Kết nối dịch vụ Google Sheets & Gemini AI...");
            if (Math6Companion.Core.Services.GoogleSheetsNetworkService.Instance == null)
            {
                var goSheets = new GameObject("[GoogleSheetsNetworkService]");
                goSheets.AddComponent<Math6Companion.Core.Services.GoogleSheetsNetworkService>();
            }

            if (Math6Companion.Core.Services.GeminiAITutorService.Instance == null)
            {
                var goGemini = new GameObject("[GeminiAITutorService]");
                goGemini.AddComponent<Math6Companion.Core.Services.GeminiAITutorService>();
            }

            if (Math6Companion.Core.Mock.MockNetworkService.Instance == null)
            {
                if (mockNetworkPrefab != null)
                {
                    Instantiate(mockNetworkPrefab);
                }
                else
                {
                    var go = new GameObject("MockNetworkService");
                    go.AddComponent<Math6Companion.Core.Mock.MockNetworkService>();
                }
            }
            yield return new WaitForSeconds(0.4f);

            UpdateProgress(1.0f, "Hệ thống sẵn sàng! Đang vào trò chơi...");
            yield return new WaitForSeconds(minimumBootTime * 0.3f);

            // 4. Chuyển sang Scene 01_Login
            SceneManager.LoadScene(nextSceneName);
        }

        private void UpdateProgress(float value, string message)
        {
            if (progressBar != null) progressBar.value = value;
            if (statusText != null) statusText.text = message;
        }
    }
}
