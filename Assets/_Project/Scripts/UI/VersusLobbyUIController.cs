using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;
using Math6Companion.Core.Contracts;

namespace Math6Companion.UI
{
    /// <summary>
    /// Điều khiển Sảnh thi đấu đối kháng (09_VersusLobby.unity).
    /// Hỗ trợ ghép trận Realtime 2 học sinh thật qua Google Sheets REST Polling,
    /// đồng thời giữ fallback đấu với Bạn học ảo (Bot) khi thử nghiệm 1 mình.
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

        private INetworkService networkService;
        private string matchedOpponentNickname = "BảoToánHọc";
        private int matchedOpponentScore = 8;
        private Coroutine matchmakingCoroutine;
        private Coroutine acceptTimerCoroutine;
        private bool isSearching = false;
        private bool isRealMatch = false;
        private string currentUserId = "STU_001";
        private string currentNickname = "Học sinh";
        private VersusQueueResponse currentMatchInfo;

        private void Start()
        {
            networkService = Math6Companion.Core.Services.NetworkServiceProvider.NetworkService;

            // Lấy thông tin tài khoản hiện tại
            if (SessionManager.Instance != null && SessionManager.Instance.CurrentUser != null)
            {
                currentUserId = SessionManager.Instance.CurrentUser.userId ?? "STU_001";
                currentNickname = MathTextSanitizer.SanitizeForDisplay(SessionManager.Instance.CurrentNickname);
            }
            else
            {
                currentUserId = PlayerPrefs.GetString("User_ID", "STU_001");
                currentNickname = MathTextSanitizer.SanitizeForDisplay(PlayerPrefs.GetString("User_Nickname", "Học sinh"));
            }

            // Cập nhật tên học sinh lên thẻ người chơi (PlayerCard)
            var pNameGo = GameObject.Find("PName");
            if (pNameGo != null)
            {
                var pNameText = pNameGo.GetComponent<TextMeshProUGUI>();
                if (pNameText != null) pNameText.text = currentNickname;
            }

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

            if (statusText != null)
                statusText.text = "Sảnh trực tuyến sẵn sàng. Bấm [BẮT ĐẦU GHÉP CẶP NGẪU NHIÊN] để tìm bạn thi đấu!";

            if (scanningIndicatorText != null)
                scanningIndicatorText.text = "Sẵn sàng tìm đối thủ...";
        }

        private void OnDestroy()
        {
            CancelQueueIfSearching();
        }

        private void CancelQueueIfSearching()
        {
            if (isSearching)
            {
                isSearching = false;
                if (matchmakingCoroutine != null) StopCoroutine(matchmakingCoroutine);
                if (networkService != null)
                {
                    networkService.CancelVersusQueue(currentUserId, (ok, msg) => { });
                }
            }
        }

        private void OnRandomMatchClicked()
        {
            PlayClick();

            if (isSearching)
            {
                CancelQueueIfSearching();
                ResetSearchUI("Đã hủy tìm kiếm.");
                return;
            }

            isSearching = true;
            if (randomMatchButton != null)
            {
                var btnTxt = randomMatchButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnTxt != null) btnTxt.text = "  HỦY TÌM KIẾM TRẬN ĐẤU  ";
            }

            matchmakingCoroutine = StartCoroutine(MatchmakingRoutine());
        }

        private IEnumerator MatchmakingRoutine()
        {
            if (statusText != null) statusText.text = "Đang kết nối đến máy chủ Google Sheets để tìm bạn học...";
            if (scanningIndicatorText != null) scanningIndicatorText.text = "Đang gửi yêu cầu vào hàng đợi...";

            bool joinCompleted = false;
            VersusQueueResponse joinResult = null;

            networkService.JoinVersusQueue(currentUserId, currentNickname, "MCQ", (success, resp, msg) =>
            {
                joinCompleted = true;
                joinResult = resp;
            });

            while (!joinCompleted) yield return null;

            if (joinResult != null && joinResult.status == "MATCHED")
            {
                // Tìm thấy đối thủ ngay lập tức (đã có bạn học đợi sẵn)
                HandleMatchSuccess(joinResult);
                yield break;
            }

            // Nếu đang WAITING: Bắt đầu vòng lặp Polling mỗi 2s
            int waitSeconds = 0;
            int maxWaitSeconds = 25; // 25s trước khi gợi ý đấu Bot ảo

            while (isSearching && waitSeconds < maxWaitSeconds)
            {
                waitSeconds += 2;
                yield return new WaitForSeconds(2.0f);

                if (!isSearching) yield break;

                if (statusText != null)
                    statusText.text = $"Đang tìm bạn học trên máy chủ Google Sheets... ({waitSeconds}s)";
                if (scanningIndicatorText != null)
                    scanningIndicatorText.text = $"Đang chờ học sinh khác bấm ghép trận... ({waitSeconds}s)";

                bool pollCompleted = false;
                VersusQueueResponse pollResult = null;

                networkService.PollVersusQueue(currentUserId, (success, resp, msg) =>
                {
                    pollCompleted = true;
                    pollResult = resp;
                });

                while (!pollCompleted) yield return null;

                if (pollResult != null && pollResult.status == "MATCHED")
                {
                    HandleMatchSuccess(pollResult);
                    yield break;
                }
            }

            // Quá 25s chưa có người chơi khác -> Đề xuất đấu với Bot hoặc tiếp tục chờ
            if (isSearching)
            {
                if (statusText != null)
                    statusText.text = "Chưa có học sinh khác online. Hệ thống kết nối với Bạn Học Ảo (BảoToánHọc)...";

                yield return new WaitForSeconds(1.5f);
                HandleBotMatch();
            }
        }

        private void HandleMatchSuccess(VersusQueueResponse matchResp)
        {
            isSearching = false;
            isRealMatch = true;
            currentMatchInfo = matchResp;

            matchedOpponentNickname = MathTextSanitizer.SanitizeForDisplay(!string.IsNullOrEmpty(matchResp.opponentNickname) ? matchResp.opponentNickname : "Học sinh đối thủ");
            matchedOpponentScore = 8; // Sẽ được cập nhật sau trận khi đối thủ nộp bài

            if (statusText != null)
                statusText.text = $"<color=#2ecc71>ĐÃ GHÉP CẶP VỚI: {matchedOpponentNickname}!</color>";

            if (scanningIndicatorText != null)
                scanningIndicatorText.text = $"Sẵn sàng: {matchedOpponentNickname}";

            ShowMatchFoundModal(true);
        }

        private void HandleBotMatch()
        {
            isSearching = false;
            isRealMatch = false;
            currentMatchInfo = null;

            matchedOpponentNickname = "BảoToánHọc (Lớp 6A2)";
            matchedOpponentScore = Random.Range(6, 10);

            if (statusText != null)
                statusText.text = $"Ghép cặp với Bạn Học Ảo: {matchedOpponentNickname}";

            if (scanningIndicatorText != null)
                scanningIndicatorText.text = $"Đối thủ: {matchedOpponentNickname}";

            ShowMatchFoundModal(false);
        }

        private void ShowMatchFoundModal(bool isReal)
        {
            if (matchModal != null)
            {
                matchModal.SetActive(true);
                if (matchFoundText != null)
                {
                    string tag = isReal 
                        ? "<color=#2ecc71><b>[NGƯỜI CHƠI THẬT]</b></color>" 
                        : "<color=#f39c12><b>[BẠN HỌC ẢO]</b></color>";
                    matchFoundText.text = $"{tag}\n<size=26><color=#f1c40f><b>{matchedOpponentNickname}</b></color></size>\n<size=16><color=#ecf0f1>Bạn có chấp nhận thi đấu đối kháng?</color></size>";
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
                if (countdownAcceptText != null) countdownAcceptText.text = $"Tự động bắt đầu sau: {remaining}s";
                yield return new WaitForSeconds(1.0f);
                remaining--;
            }
            OnAcceptMatchClicked();
        }

        private void OnAcceptMatchClicked()
        {
            PlayClick();
            if (acceptTimerCoroutine != null) StopCoroutine(acceptTimerCoroutine);
            if (matchModal != null) matchModal.SetActive(false);

            // Bắt đầu ngay thể thức Trắc nghiệm tốc độ (MCQ)
            StartMatch("MCQ");
        }

        private void OnDeclineMatchClicked()
        {
            PlayClick();
            if (acceptTimerCoroutine != null) StopCoroutine(acceptTimerCoroutine);
            if (matchModal != null) matchModal.SetActive(false);

            // Luôn gửi CancelVersusQueue lên server để gỡ trạng thái MATCHED/WAITING
            if (networkService != null && !string.IsNullOrEmpty(currentUserId))
            {
                networkService.CancelVersusQueue(currentUserId, (ok, msg) =>
                {
                    Debug.Log($"[VersusLobby] CancelVersusQueue: ok={ok}, msg={msg}");
                });
            }

            currentMatchInfo = null;
            isRealMatch = false;
            ResetSearchUI("Đã từ chối trận đấu.");
        }

        private void ResetSearchUI(string msg)
        {
            isSearching = false;
            if (randomMatchButton != null)
            {
                randomMatchButton.interactable = true;
                var btnTxt = randomMatchButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnTxt != null) btnTxt.text = "  BẮT ĐẦU GHÉP CẶP NGẪU NHIÊN  >";
            }
            if (statusText != null) statusText.text = msg;
            if (scanningIndicatorText != null) scanningIndicatorText.text = "Sẵn sàng tìm đối thủ...";
        }

        private void StartMatch(string mode)
        {
            PlayClick();

            // Cấu hình cờ Đối kháng cho Session chung
            ScoreSessionContext.IsVersusMode = true;
            ScoreSessionContext.IsRealMultiplayer = isRealMatch;
            ScoreSessionContext.OpponentNickname = matchedOpponentNickname;
            ScoreSessionContext.OpponentScore = matchedOpponentScore;
            ScoreSessionContext.TimeSpentSeconds = (TeacherConfigManager.Instance != null) ? TeacherConfigManager.Instance.PvPDurationSeconds : 180;

            if (isRealMatch && currentMatchInfo != null)
            {
                ScoreSessionContext.VersusRoomId = currentMatchInfo.roomId;
                ScoreSessionContext.OpponentUserId = currentMatchInfo.opponentId;
                ScoreSessionContext.VersusQuestionIds = currentMatchInfo.questionIds ?? new List<string>();
            }
            else
            {
                ScoreSessionContext.VersusRoomId = "";
                ScoreSessionContext.OpponentUserId = "";
                ScoreSessionContext.VersusQuestionIds.Clear();
            }

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
            CancelQueueIfSearching();
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
