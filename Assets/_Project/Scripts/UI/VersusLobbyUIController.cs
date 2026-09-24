using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;

namespace Math6Companion.UI
{
    /// <summary>
    /// Điều khiển Sảnh thi đấu đối kháng (09_VersusLobby.unity).
    /// Quét danh sách online, ghép cặp ngẫu nhiên, đợi Accept và chọn thể thức (TN/TL).
    /// </summary>
    public class VersusLobbyUIController : MonoBehaviour
    {
        [Header("Header & Navigation")]
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Online Players List")]
        [SerializeField] private Transform onlineListContent;
        [SerializeField] private TextMeshProUGUI scanningIndicatorText;
        [SerializeField] private Button randomMatchButton;

        [Header("Match Request Modal (Popup Chờ Accept)")]
        [SerializeField] private GameObject matchModal;
        [SerializeField] private TextMeshProUGUI matchFoundText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;
        [SerializeField] private TextMeshProUGUI countdownAcceptText;

        [Header("Game Mode Selection Modal")]
        [SerializeField] private GameObject modeSelectionModal;
        [SerializeField] private Button chooseMcqModeButton;
        [SerializeField] private Button chooseEssayModeButton;

        private string matchedOpponentNickname = "BảoToánHọc";
        private int matchedOpponentScore = 8;
        private Coroutine acceptTimerCoroutine;

        private void Start()
        {
            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);

            if (randomMatchButton != null)
                randomMatchButton.onClick.AddListener(OnRandomMatchClicked);

            if (acceptButton != null)
                acceptButton.onClick.AddListener(OnAcceptMatchClicked);

            if (declineButton != null)
                declineButton.onClick.AddListener(OnDeclineMatchClicked);

            if (chooseMcqModeButton != null)
                chooseMcqModeButton.onClick.AddListener(() => StartMatch("MCQ"));

            if (chooseEssayModeButton != null)
                chooseEssayModeButton.onClick.AddListener(() => StartMatch("ESSAY"));

            if (matchModal != null) matchModal.SetActive(false);
            if (modeSelectionModal != null) modeSelectionModal.SetActive(false);

            StartCoroutine(ScanOnlineUsersRoutine());
        }

        private IEnumerator ScanOnlineUsersRoutine()
        {
            if (scanningIndicatorText != null)
                scanningIndicatorText.text = "Đang quét danh sách tài khoản học sinh trực tuyến...";

            yield return new WaitForSeconds(1.0f);

            if (scanningIndicatorText != null)
                scanningIndicatorText.text = "[ONLINE] Tìm thấy 4 học sinh trực tuyến sẵn sàng thi đấu!";
        }

        private void OnRandomMatchClicked()
        {
            PlayClick();
            if (statusText != null) statusText.text = "Đang ghép cặp ngẫu nhiên với đối thủ...";
            if (randomMatchButton != null) randomMatchButton.interactable = false;

            StartCoroutine(MatchmakingRoutine());
        }

        private IEnumerator MatchmakingRoutine()
        {
            yield return new WaitForSeconds(1.5f);

            matchedOpponentNickname = "BảoToánHọc (Lớp 6A2)";
            matchedOpponentScore = Random.Range(6, 10);

            if (matchModal != null)
            {
                matchModal.SetActive(true);
                if (matchFoundText != null)
                {
                    matchFoundText.text = $"ĐÃ TÌM THẤY ĐỐI THỦ!\n\n<color=#f39c12>{matchedOpponentNickname}</color>\n\nBạn có chấp nhận thi đấu đối kháng?";
                }

                if (acceptTimerCoroutine != null) StopCoroutine(acceptTimerCoroutine);
                acceptTimerCoroutine = StartCoroutine(AcceptCountdownRoutine(10));
            }
        }

        private IEnumerator AcceptCountdownRoutine(int seconds)
        {
            int remaining = seconds;
            while (remaining > 0)
            {
                if (countdownAcceptText != null) countdownAcceptText.text = $"Tự động từ chối sau: {remaining}s";
                yield return new WaitForSeconds(1.0f);
                remaining--;
            }
            OnDeclineMatchClicked();
        }

        private void OnAcceptMatchClicked()
        {
            PlayClick();
            if (acceptTimerCoroutine != null) StopCoroutine(acceptTimerCoroutine);
            if (matchModal != null) matchModal.SetActive(false);

            // Mở popup chọn thể thức thi đấu
            if (modeSelectionModal != null)
            {
                modeSelectionModal.SetActive(true);
            }
        }

        private void OnDeclineMatchClicked()
        {
            PlayClick();
            if (acceptTimerCoroutine != null) StopCoroutine(acceptTimerCoroutine);
            if (matchModal != null) matchModal.SetActive(false);
            if (randomMatchButton != null) randomMatchButton.interactable = true;
            if (statusText != null) statusText.text = "Đã hủy ghép cặp. Bấm [Ghép cặp ngẫu nhiên] để tìm lại.";
        }

        private void StartMatch(string mode)
        {
            PlayClick();

            // Cấu hình cờ Đối kháng cho Scene Điểm
            ScoreSessionContext.IsVersusMode = true;
            ScoreSessionContext.OpponentNickname = matchedOpponentNickname;
            ScoreSessionContext.OpponentScore = matchedOpponentScore;
            ScoreSessionContext.TimeSpentSeconds = 180; // 3 phút đếm ngược

            if (mode == "MCQ")
            {
                SceneManager.LoadScene("05_MCQPractice");
            }
            else
            {
                SceneManager.LoadScene("06_EssayPractice");
            }
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
