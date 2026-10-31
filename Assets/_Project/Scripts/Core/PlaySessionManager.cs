using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Math6Companion.Core.Contracts;

namespace Math6Companion.Core
{
    /// <summary>
    /// =========================================================================
    ///               HỆ THỐNG QUẢN LÝ PHIÊN CHƠI & BẢO VỆ MẮT HỌC SINH LỚP 6
    ///                            (PLAY SESSION LIMITER)
    /// =========================================================================
    /// Mục tiêu giáo dục & sức khỏe:
    /// 1. Giới hạn thời lượng mỗi phiên chơi liên tục (mặc định 30 phút - chuẩn học tập Toán 6).
    /// 2. Bắt buộc nghỉ giải lao (mặc định 5 phút) để bảo vệ thị lực (quy tắc 20-20-20),
    ///    vận động giãn cơ và uống nước sau mỗi phiên.
    /// 3. Giới hạn tổng thời gian chơi tích lũy trong ngày (mặc định 90 phút).
    /// 4. Cảnh báo sớm thân thiện (trước khi hết giờ 5 phút).
    /// 5. Chế độ Phụ huynh (Parental Lock & PIN) để gia hạn thêm 15-30 phút khi cần hoàn thành bài.
    /// 6. Tồn tại xuyên suốt ứng dụng (DontDestroyOnLoad) với giao diện hiện đại độc lập.
    /// =========================================================================
    /// </summary>
    public class PlaySessionManager : MonoBehaviour
    {
        private static bool isQuitting = false;
        private static PlaySessionManager instance;

        public static PlaySessionManager Instance
        {
            get
            {
                if (isQuitting) return null;
                if (instance == null)
                {
                    instance = FindAnyObjectByType<PlaySessionManager>();
                    if (instance == null && !isQuitting)
                    {
                        var go = new GameObject("[PlaySessionManager]");
                        instance = go.AddComponent<PlaySessionManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        #region Constants & PlayerPrefs Keys
        private const string PREF_ENABLED = "ScreenTime_Enabled";
        private const string PREF_CONTINUOUS_MINS = "ScreenTime_ContinuousMins";
        private const string PREF_DAILY_MAX_MINS = "ScreenTime_DailyMaxMins";
        private const string PREF_BREAK_MINS = "ScreenTime_BreakMins";
        private const string PREF_PARENT_PIN = "ScreenTime_ParentPin";
        private const string PREF_SHOW_WIDGET = "ScreenTime_ShowWidget";
        private const string PREF_ACTIVE_DATE = "ScreenTime_ActiveDate";
        private const string PREF_DAILY_SECONDS = "ScreenTime_DailySeconds";

        public const string DEFAULT_PARENT_PIN = "2026";
        public const int DEFAULT_CONTINUOUS_MINS = 30; // 30 phút mỗi phiên
        public const int DEFAULT_DAILY_MAX_MINS = 90;  // 90 phút mỗi ngày
        public const int DEFAULT_BREAK_MINS = 5;      // 5 phút nghỉ ngơi bắt buộc
        #endregion

        #region Configuration Settings
        [Header("Settings & Limits")]
        [SerializeField] private bool isEnabled = true;
        [SerializeField] private int maxContinuousMinutes = DEFAULT_CONTINUOUS_MINS;
        [SerializeField] private int maxDailyMinutes = DEFAULT_DAILY_MAX_MINS;
        [SerializeField] private int breakDurationMinutes = DEFAULT_BREAK_MINS;
        [SerializeField] private string parentPin = DEFAULT_PARENT_PIN;
        [SerializeField] private bool showTimerWidget = true;

        public bool IsEnabled => isEnabled;
        public int MaxContinuousMinutes => maxContinuousMinutes;
        public int MaxDailyMinutes => maxDailyMinutes;
        public int BreakDurationMinutes => breakDurationMinutes;
        public string ParentPin => parentPin;
        public bool ShowTimerWidget => showTimerWidget;
        #endregion

        #region Realtime State
        [Header("Realtime State")]
        [SerializeField] private float currentContinuousSeconds = 0f;
        [SerializeField] private float dailyAccumulatedSeconds = 0f;
        [SerializeField] private bool isInBreak = false;
        [SerializeField] private float breakRemainingSeconds = 0f;
        [SerializeField] private bool warningToastShownThisSession = false;

        public float CurrentContinuousSeconds => currentContinuousSeconds;
        public float DailyAccumulatedSeconds => dailyAccumulatedSeconds;
        public bool IsInBreak => isInBreak;
        public float BreakRemainingSeconds => breakRemainingSeconds;
        public float RemainingSessionSeconds
        {
            get
            {
                if (!isEnabled) return 99999f;
                float sessionLimit = maxContinuousMinutes * 60f;
                float sessionLeft = Mathf.Max(0f, sessionLimit - currentContinuousSeconds);

                if (maxDailyMinutes > 0)
                {
                    float dailyLimit = maxDailyMinutes * 60f;
                    float dailyLeft = Mathf.Max(0f, dailyLimit - dailyAccumulatedSeconds);
                    return Mathf.Min(sessionLeft, dailyLeft);
                }

                return sessionLeft;
            }
        }
        #endregion

        #region Events
#pragma warning disable CS0067
        public event Action<float, float> OnTimeUpdated; // (continuousSecs, dailySecs)
        public event Action<string> OnWarningNotice;     // Message
        public event Action<bool> OnBreakStateChanged;   // isInBreak
        public event Action OnLimitsConfigChanged;
#pragma warning restore CS0067
        #endregion

        #region UI Elements (Global Overlay)
        private Canvas globalCanvas;
        private GameObject timerPillGo;
        private TextMeshProUGUI timerPillText;
        private Image timerPillBg;
        private Button timerPillButton;

        private GameObject toastBannerGo;
        private TextMeshProUGUI toastBannerText;
        private Coroutine toastCoroutine;

        private GameObject breakOverlayGo;
        private TextMeshProUGUI breakCountdownText;
        private Slider breakProgressBar;
        private Button btnStartNewSession;
        private Button btnParentOverride;
        private Button btnQuitApp;
        private TextMeshProUGUI breakEncourageText;

        private GameObject parentPinModalGo;
        private TMP_InputField inputPinField;
        private TextMeshProUGUI parentChallengeText;
        private TextMeshProUGUI pinStatusText;
        private int mathAnsCorrect = 0;
        #endregion

        private float saveIntervalTimer = 0f;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettingsAndState();
            BuildGlobalUI();
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
            SaveState();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveState();
            }
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            CleanupGlobalCanvasStrayChildren();
        }

        private void CleanupGlobalCanvasStrayChildren()
        {
            if (globalCanvas == null) return;
            for (int ci = globalCanvas.transform.childCount - 1; ci >= 0; ci--)
            {
                var child = globalCanvas.transform.GetChild(ci);
                if (child.gameObject != timerPillGo &&
                    child.gameObject != toastBannerGo &&
                    child.gameObject != breakOverlayGo &&
                    child.gameObject != parentPinModalGo)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void Start()
        {
            CleanupGlobalCanvasStrayChildren();
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnUserLoggedIn += HandleUserLoggedIn;
                SessionManager.Instance.OnUserLoggedOut += HandleUserLoggedOut;
            }
            UpdateUIWidgetVisibility();
        }

        private void OnDestroy()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnUserLoggedIn -= HandleUserLoggedIn;
                SessionManager.Instance.OnUserLoggedOut -= HandleUserLoggedOut;
            }
        }

        private void HandleUserLoggedIn(UserData user)
        {
            ResetContinuousSession();
            UpdateUIWidgetVisibility();
        }

        private void HandleUserLoggedOut()
        {
            ResetContinuousSession();
            UpdateUIWidgetVisibility();
        }

        public void ResetContinuousSession()
        {
            currentContinuousSeconds = 0f;
            isInBreak = false;
            breakRemainingSeconds = 0f;
            warningToastShownThisSession = false;
            if (breakOverlayGo != null && breakOverlayGo.activeSelf)
            {
                breakOverlayGo.SetActive(false);
            }
        }

        private void Update()
        {
            // Tự động dọn dẹp các UI rò rỉ nếu bị gán nhầm vào globalCanvas
            if (globalCanvas != null && globalCanvas.transform.childCount > 4)
            {
                for (int ci = globalCanvas.transform.childCount - 1; ci >= 0; ci--)
                {
                    var child = globalCanvas.transform.GetChild(ci);
                    if (child.name.StartsWith("LeftPhotoCard") || child.name.StartsWith("RightTextCard") || child.name.StartsWith("Btn_Capture") || child.name.StartsWith("Btn_Pick") || child.name.StartsWith("Btn_Sample") || child.name.StartsWith("PreviewContainer"))
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            if (!isEnabled)
            {
                if (timerPillGo != null && timerPillGo.activeSelf) timerPillGo.SetActive(false);
                return;
            }

            // Giáo viên không bị đếm giờ và không bị giới hạn phiên chơi của học sinh
            if (SessionManager.Instance != null && SessionManager.Instance.IsTeacher)
            {
                if (timerPillGo != null && timerPillGo.activeSelf) timerPillGo.SetActive(false);
                if (breakOverlayGo != null && breakOverlayGo.activeSelf) breakOverlayGo.SetActive(false);
                return;
            }

            float dt = Time.unscaledDeltaTime;

            if (isInBreak)
            {
                // Đảm bảo chuột luôn mở khóa để click các nút giải lao / phụ huynh trong mọi cảnh (kể cả 3D)
                if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                // Đang trong thời gian nghỉ giải lao
                breakRemainingSeconds -= dt;
                if (breakRemainingSeconds <= 0f)
                {
                    breakRemainingSeconds = 0f;
                    OnBreakFinished();
                }
                UpdateBreakUI();
            }
            else
            {
                // Đang trong phiên học / chơi
                currentContinuousSeconds += dt;
                dailyAccumulatedSeconds += dt;

                // Cảnh báo trước khi hết phiên (20s nếu phiên 1 phút TEST, 5 phút nếu phiên chuẩn)
                float remaining = RemainingSessionSeconds;
                float warnThreshold = (maxContinuousMinutes <= 1) ? 20f : 300f;
                if (!warningToastShownThisSession && remaining <= warnThreshold && remaining > 0f)
                {
                    warningToastShownThisSession = true;
                    if (maxContinuousMinutes <= 1)
                    {
                        ShowWarningToast("<b>[Kiểm thử TEST]</b> Phiên 1 phút chỉ còn <b>20 giây</b> nữa là đến giờ giải lao!");
                    }
                    else
                    {
                        ShowWarningToast("<b>Nhắc nhở học tập:</b> Em đã học chăm chỉ rồi! Còn <b>5 phút</b> nữa là đến giờ giải lao cho mắt nhé.");
                    }
                }

                // Hết giờ phiên hoặc hết giới hạn ngày
                if (remaining <= 0f)
                {
                    StartBreak();
                }

                UpdateTimerWidgetUI();
                OnTimeUpdated?.Invoke(currentContinuousSeconds, dailyAccumulatedSeconds);
            }

            // Tự động lưu tiến độ mỗi 30 giây
            saveIntervalTimer += dt;
            if (saveIntervalTimer >= 30f)
            {
                saveIntervalTimer = 0f;
                SaveState();
            }
        }

        #region Break State Logic
        public void StartBreak()
        {
            isInBreak = true;
            // Nếu phiên đặt là 1 phút (chế độ test), thời gian nghỉ giải lao chỉ 15 giây để kiểm thử nhanh chóng
            breakRemainingSeconds = (maxContinuousMinutes <= 1) ? 15f : Mathf.Max(60f, breakDurationMinutes * 60f);

            if (breakOverlayGo != null)
            {
                breakOverlayGo.SetActive(true);
            }

            if (btnStartNewSession != null)
            {
                btnStartNewSession.interactable = false;
                var lbl = btnStartNewSession.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.text = " ĐANG TRONG GIỜ NGHỈ GIẢI LAO...";
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPreviewTone();
            }

            OnBreakStateChanged?.Invoke(true);
        }

        private void OnBreakFinished()
        {
            if (btnStartNewSession != null)
            {
                btnStartNewSession.interactable = true;
                var lbl = btnStartNewSession.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.text = "BẮT ĐẦU PHIÊN HỌC MỚI >>";
            }

            if (breakEncourageText != null)
            {
                breakEncourageText.text = "<color=#2ecc71><b>XUẤT SẮC!</b></color> Đôi mắt và tinh thần của em đã được nạp đầy năng lượng! Nhấn nút bên dưới để tiếp tục học tập.";
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCorrectSound();
            }
        }

        public void EndBreakAndResume()
        {
            isInBreak = false;
            currentContinuousSeconds = 0f;
            warningToastShownThisSession = false;

            if (breakOverlayGo != null)
            {
                breakOverlayGo.SetActive(false);
            }

            SaveState();
            OnBreakStateChanged?.Invoke(false);
            ShowWarningToast("<b>Phiên học mới bắt đầu!</b> Chúc em học tập thật tốt và đạt nhiều điểm 10!");
        }

        public void ExtendSessionMinutes(int additionalMinutes)
        {
            float additionalSeconds = additionalMinutes * 60f;
            // Giảm bớt số giây liên tục đã tính để nới rộng thời gian còn lại
            currentContinuousSeconds = Mathf.Max(0f, currentContinuousSeconds - additionalSeconds);
            warningToastShownThisSession = false;

            if (isInBreak)
            {
                EndBreakAndResume();
            }
            else
            {
                ShowWarningToast($"<b>Phụ huynh đã gia hạn thêm: +{additionalMinutes} phút</b> học tập.");
            }

            SaveState();
        }
        #endregion

        #region Settings API
        public void UpdateSettings(bool enabled, int continuousMins, int dailyMins, int breakMins, string newPin = null)
        {
            isEnabled = enabled;
            maxContinuousMinutes = Mathf.Clamp(continuousMins, 1, 120);
            maxDailyMinutes = Mathf.Max(0, dailyMins);
            breakDurationMinutes = Mathf.Clamp(breakMins, 1, 30);

            if (!string.IsNullOrEmpty(newPin) && newPin.Length >= 4)
            {
                parentPin = newPin;
            }

            SaveSettings();
            UpdateUIWidgetVisibility();
            OnLimitsConfigChanged?.Invoke();
        }

        public void SetWidgetVisible(bool visible)
        {
            showTimerWidget = visible;
            PlayerPrefs.SetInt(PREF_SHOW_WIDGET, showTimerWidget ? 1 : 0);
            PlayerPrefs.Save();
            UpdateUIWidgetVisibility();
        }

        public void ResetDailyUsage()
        {
            dailyAccumulatedSeconds = 0f;
            currentContinuousSeconds = 0f;
            warningToastShownThisSession = false;
            SaveState();
            ShowWarningToast("[OK] Đã đặt lại thời gian học tập hôm nay về 0.");
        }

        public bool VerifyParentPin(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            return input.Trim() == parentPin.Trim();
        }
        #endregion

        #region State Persistence
        private void LoadSettingsAndState()
        {
            isEnabled = PlayerPrefs.GetInt(PREF_ENABLED, 1) == 1;
            maxContinuousMinutes = PlayerPrefs.GetInt(PREF_CONTINUOUS_MINS, DEFAULT_CONTINUOUS_MINS);
            maxDailyMinutes = PlayerPrefs.GetInt(PREF_DAILY_MAX_MINS, DEFAULT_DAILY_MAX_MINS);
            breakDurationMinutes = PlayerPrefs.GetInt(PREF_BREAK_MINS, DEFAULT_BREAK_MINS);
            parentPin = PlayerPrefs.GetString(PREF_PARENT_PIN, DEFAULT_PARENT_PIN);
            showTimerWidget = PlayerPrefs.GetInt(PREF_SHOW_WIDGET, 1) == 1;

            string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
            string savedDate = PlayerPrefs.GetString(PREF_ACTIVE_DATE, "");

            if (savedDate == todayStr)
            {
                dailyAccumulatedSeconds = PlayerPrefs.GetFloat(PREF_DAILY_SECONDS, 0f);
            }
            else
            {
                // Sang ngày mới -> Tự động đặt lại bộ đếm ngày
                dailyAccumulatedSeconds = 0f;
                PlayerPrefs.SetString(PREF_ACTIVE_DATE, todayStr);
                PlayerPrefs.SetFloat(PREF_DAILY_SECONDS, 0f);
                PlayerPrefs.Save();
            }

            currentContinuousSeconds = 0f;
            isInBreak = false;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt(PREF_ENABLED, isEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PREF_CONTINUOUS_MINS, maxContinuousMinutes);
            PlayerPrefs.SetInt(PREF_DAILY_MAX_MINS, maxDailyMinutes);
            PlayerPrefs.SetInt(PREF_BREAK_MINS, breakDurationMinutes);
            PlayerPrefs.SetString(PREF_PARENT_PIN, parentPin);
            PlayerPrefs.SetInt(PREF_SHOW_WIDGET, showTimerWidget ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void SaveState()
        {
            string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
            PlayerPrefs.SetString(PREF_ACTIVE_DATE, todayStr);
            PlayerPrefs.SetFloat(PREF_DAILY_SECONDS, dailyAccumulatedSeconds);
            PlayerPrefs.Save();
        }
        #endregion

        #region Global UI Creation
        private void BuildGlobalUI()
        {
            if (globalCanvas != null) return;

            // 1. Root Canvas
            var canvasGo = new GameObject("[GlobalPlaySessionCanvas]", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            globalCanvas = canvasGo.GetComponent<Canvas>();
            globalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            globalCanvas.sortingOrder = 32766; // Nằm ngay dưới Dimmer (32767)

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 2. Timer Pill Widget (Góc trên)
            BuildTimerPillWidget(canvasGo);

            // 3. Warning Toast Banner (Đỉnh màn hình)
            BuildToastBanner(canvasGo);

            // 4. Mandatory Break Fullscreen Overlay (Khóa nghỉ ngơi)
            BuildBreakOverlay(canvasGo);

            // 5. Parent Unlock PIN Modal
            BuildParentPinModal(canvasGo);
        }

        private void BuildTimerPillWidget(GameObject parent)
        {
            timerPillGo = new GameObject("SessionTimerPill", typeof(RectTransform), typeof(Image), typeof(Button));
            timerPillGo.transform.SetParent(parent.transform, false);
            var rt = timerPillGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(250f, -8f); // Đặt cân đối ở góc trên bên phải header
            rt.sizeDelta = new Vector2(165f, 32f);

            timerPillBg = timerPillGo.GetComponent<Image>();
            timerPillBg.color = new Color(0.08f, 0.12f, 0.20f, 0.90f);

            timerPillButton = timerPillGo.GetComponent<Button>();
            timerPillButton.onClick.AddListener(OnTimerPillClicked);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(timerPillGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8, 0);
            textRt.offsetMax = new Vector2(-8, 0);

            timerPillText = textGo.GetComponent<TextMeshProUGUI>();
            timerPillText.fontSize = 15;
            timerPillText.fontStyle = FontStyles.Bold;
            timerPillText.alignment = TextAlignmentOptions.Center;
            timerPillText.color = new Color(0.2f, 0.85f, 0.6f);
            timerPillText.text = "30:00";
        }

        private void BuildToastBanner(GameObject parent)
        {
            toastBannerGo = new GameObject("WarningToastBanner", typeof(RectTransform), typeof(Image));
            toastBannerGo.transform.SetParent(parent.transform, false);
            var rt = toastBannerGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -50f);
            rt.sizeDelta = new Vector2(760f, 64f);

            var bg = toastBannerGo.GetComponent<Image>();
            bg.color = new Color(0.10f, 0.15f, 0.26f, 0.98f);

            var textGo = new GameObject("ToastText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(toastBannerGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(25, 0);
            textRt.offsetMax = new Vector2(-110, 0);

            toastBannerText = textGo.GetComponent<TextMeshProUGUI>();
            toastBannerText.fontSize = 17;
            toastBannerText.alignment = TextAlignmentOptions.MidlineLeft;
            toastBannerText.color = new Color(0.95f, 0.85f, 0.35f);
            toastBannerText.text = "Nhắc nhở học tập lành mạnh...";

            // Nút Đã hiểu
            var btnCloseGo = new GameObject("Btn_GotIt", typeof(RectTransform), typeof(Image), typeof(Button));
            btnCloseGo.transform.SetParent(toastBannerGo.transform, false);
            var btnRt = btnCloseGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.anchoredPosition = new Vector2(-15f, 0f);
            btnRt.sizeDelta = new Vector2(90f, 38f);

            var btnImg = btnCloseGo.GetComponent<Image>();
            btnImg.color = new Color(0.14f, 0.72f, 0.46f);

            var btnLblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnLblGo.transform.SetParent(btnCloseGo.transform, false);
            var btnLblRt = btnLblGo.GetComponent<RectTransform>();
            btnLblRt.anchorMin = Vector2.zero;
            btnLblRt.anchorMax = Vector2.one;
            var btnLbl = btnLblGo.GetComponent<TextMeshProUGUI>();
            btnLbl.fontSize = 15;
            btnLbl.fontStyle = FontStyles.Bold;
            btnLbl.alignment = TextAlignmentOptions.Center;
            btnLbl.color = Color.white;
            btnLbl.text = "ĐÃ RÕ [OK]";

            var btn = btnCloseGo.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (toastCoroutine != null) StopCoroutine(toastCoroutine);
                toastBannerGo.SetActive(false);
            });

            toastBannerGo.SetActive(false);
        }

        private void BuildBreakOverlay(GameObject parent)
        {
            breakOverlayGo = new GameObject("MandatoryBreakOverlay", typeof(RectTransform), typeof(Image));
            breakOverlayGo.transform.SetParent(parent.transform, false);
            var rt = breakOverlayGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = breakOverlayGo.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.14f, 0.98f);
            bg.raycastTarget = true; // Chặn tương tác với UI và gameplay bên dưới!

            // Center Card
            var cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(breakOverlayGo.transform, false);
            var cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(860f, 660f);

            var cardImg = cardGo.GetComponent<Image>();
            cardImg.color = new Color(0.10f, 0.15f, 0.25f, 0.98f);

            // Badge
            CreateTextElement(cardGo, "Badge", "[+] BẢO VỆ MẮT & SỨC KHỎE HỌC SINH LỚP 6", 14, new Vector2(0, 280), new Vector2(400, 32), new Color(0.14f, 0.72f, 0.46f), FontStyles.Bold);

            // Title
            CreateTextElement(cardGo, "Title", "ĐẾN GIỜ NGHỈ GIẢI LAO RỒI BẠN ƠI!", 32, new Vector2(0, 230), new Vector2(760, 48), new Color(0.96f, 0.65f, 0.15f), FontStyles.Bold);

            // Subtitle
            breakEncourageText = CreateTextElement(cardGo, "SubTitle", "Em đã học tập rất chăm chỉ suốt phiên học! Hãy tạm rời màn hình để thư giãn mắt và giãn cơ nhé.", 18, new Vector2(0, 180), new Vector2(760, 40), new Color(0.90f, 0.93f, 0.98f));

            // Countdown Container
            var timerBox = new GameObject("TimerBox", typeof(RectTransform), typeof(Image));
            timerBox.transform.SetParent(cardGo.transform, false);
            var tRt = timerBox.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0, 105);
            tRt.sizeDelta = new Vector2(300, 75);
            timerBox.GetComponent<Image>().color = new Color(0.07f, 0.10f, 0.18f);

            breakCountdownText = CreateTextElement(timerBox, "Countdown", "05:00", 42, Vector2.zero, new Vector2(280, 60), new Color(0.2f, 0.85f, 0.6f), FontStyles.Bold);

            // 4 Healthy Advice Cards Container
            var tipsContainer = new GameObject("TipsContainer", typeof(RectTransform));
            tipsContainer.transform.SetParent(cardGo.transform, false);
            var tipsRt = tipsContainer.GetComponent<RectTransform>();
            tipsRt.anchoredPosition = new Vector2(0, -25);
            tipsRt.sizeDelta = new Vector2(760, 140);

            CreateTipCard(tipsContainer, "Tip1", "[1] QUY TẮC 20-20-20", "Nhìn xa 6m ra ngoài cửa sổ trong 20 giây để mắt thư giãn.", new Vector2(-195, 38));
            CreateTipCard(tipsContainer, "Tip2", "[2] UỐNG NƯỚC", "Uống 1 cốc nước lọc mát giúp não bộ tỉnh táo.", new Vector2(195, 38));
            CreateTipCard(tipsContainer, "Tip3", "[3] VƯƠN VAI", "Đứng dậy vươn vai, xoay cổ và hít thở sâu 5 lần.", new Vector2(-195, -38));
            CreateTipCard(tipsContainer, "Tip4", "[4] VẬN ĐỘNG NHẸ", "Đi lại vài bước trong phòng hoặc trò chuyện cùng bố mẹ.", new Vector2(195, -38));

            // Action Buttons
            btnStartNewSession = CreateButtonElement(cardGo, "Btn_StartNewSession", "BẮT ĐẦU PHIÊN HỌC MỚI", new Vector2(0, -145), new Vector2(480, 52), new Color(0.14f, 0.72f, 0.46f), 18);
            btnStartNewSession.onClick.AddListener(EndBreakAndResume);

            btnParentOverride = CreateButtonElement(cardGo, "Btn_ParentOverride", "[PIN] Dành cho Phụ huynh / Thầy cô (Gia hạn thời gian)", new Vector2(0, -210), new Vector2(480, 44), new Color(0.20f, 0.26f, 0.36f), 15);
            btnParentOverride.onClick.AddListener(OpenParentPinModal);

            btnQuitApp = CreateButtonElement(cardGo, "Btn_QuitApp", "[X] Thoát trò chơi để nghỉ ngơi", new Vector2(0, -265), new Vector2(300, 40), new Color(0.85f, 0.25f, 0.25f), 15);
            btnQuitApp.onClick.AddListener(() => Application.Quit());

            breakOverlayGo.SetActive(false);
        }

        private void CreateTipCard(GameObject parent, string name, string header, string desc, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(360, 68);
            go.GetComponent<Image>().color = new Color(0.14f, 0.19f, 0.30f);

            CreateTextElement(go, "Header", header, 15, new Vector2(0, 16), new Vector2(340, 24), new Color(0.96f, 0.65f, 0.15f), FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            CreateTextElement(go, "Desc", desc, 13, new Vector2(0, -14), new Vector2(340, 32), new Color(0.80f, 0.85f, 0.95f), FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        }

        private void BuildParentPinModal(GameObject parent)
        {
            parentPinModalGo = new GameObject("ParentPinModal", typeof(RectTransform), typeof(Image));
            parentPinModalGo.transform.SetParent(parent.transform, false);
            var rt = parentPinModalGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = parentPinModalGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.88f);
            bg.raycastTarget = true;

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parentPinModalGo.transform, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(560, 480);
            card.GetComponent<Image>().color = new Color(0.10f, 0.15f, 0.26f);

            CreateTextElement(card, "Title", "XÁC THỰC PHỤ HUYNH / THẦY CÔ", 24, new Vector2(0, 195), new Vector2(500, 40), new Color(0.96f, 0.65f, 0.15f), FontStyles.Bold);
            CreateTextElement(card, "Desc", "Nhập mã PIN phụ huynh (Mặc định: <b>2026</b>) hoặc giải câu đố bên dưới để gia hạn thời gian học cho con:", 15, new Vector2(0, 140), new Vector2(500, 50), new Color(0.90f, 0.93f, 0.98f));

            // Math Challenge Text
            parentChallengeText = CreateTextElement(card, "MathChallenge", "Câu đố người lớn: 13 x 17 = ?", 18, new Vector2(0, 80), new Vector2(500, 30), new Color(0.14f, 0.72f, 0.46f), FontStyles.Bold);

            // Input PIN
            var inputGo = new GameObject("PinInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(card.transform, false);
            var inRt = inputGo.GetComponent<RectTransform>();
            inRt.anchoredPosition = new Vector2(0, 25);
            inRt.sizeDelta = new Vector2(360, 48);
            inputGo.GetComponent<Image>().color = new Color(0.07f, 0.10f, 0.18f);

            var textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputGo.transform, false);
            var aRt = textArea.GetComponent<RectTransform>();
            aRt.anchorMin = Vector2.zero; aRt.anchorMax = Vector2.one;
            aRt.offsetMin = new Vector2(15, 0); aRt.offsetMax = new Vector2(-15, 0);

            var textC = CreateTextElement(textArea, "Text", "", 20, Vector2.zero, Vector2.zero, Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
            var phC = CreateTextElement(textArea, "Placeholder", "Nhập mã PIN hoặc đáp án...", 16, Vector2.zero, Vector2.zero, new Color(0.6f, 0.7f, 0.8f, 0.6f), FontStyles.Italic, TextAlignmentOptions.Center);

            inputPinField = inputGo.GetComponent<TMP_InputField>();
            inputPinField.textViewport = aRt;
            inputPinField.textComponent = textC;
            inputPinField.placeholder = phC;
            inputPinField.contentType = TMP_InputField.ContentType.Password;

            pinStatusText = CreateTextElement(card, "Status", "", 14, new Vector2(0, -18), new Vector2(500, 25), new Color(0.9f, 0.3f, 0.3f));

            // Extension Buttons
            var btn15 = CreateButtonElement(card, "Btn_+15m", "Gia Hạn Thêm +15 Phút", new Vector2(0, -65), new Vector2(400, 44), new Color(0.15f, 0.45f, 0.90f), 16);
            btn15.onClick.AddListener(() => TryUnlockWithExtension(15));

            var btn30 = CreateButtonElement(card, "Btn_+30m", "Gia Hạn Thêm +30 Phút", new Vector2(0, -120), new Vector2(400, 44), new Color(0.14f, 0.72f, 0.46f), 16);
            btn30.onClick.AddListener(() => TryUnlockWithExtension(30));

            var btnCancel = CreateButtonElement(card, "Btn_Cancel", "Đóng Lại", new Vector2(0, -175), new Vector2(250, 38), new Color(0.20f, 0.26f, 0.36f), 15);
            btnCancel.onClick.AddListener(() => parentPinModalGo.SetActive(false));

            parentPinModalGo.SetActive(false);
        }

        private TextMeshProUGUI CreateTextElement(GameObject parent, string name, string content, float size, Vector2 pos, Vector2 delta, Color col, FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            if (delta != Vector2.zero) rt.sizeDelta = delta;
            else
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
            }

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = col;
            tmp.alignment = align;
            return tmp;
        }

        private Button CreateButtonElement(GameObject parent, string name, string label, Vector2 pos, Vector2 delta, Color col, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = delta;

            var img = go.GetComponent<Image>();
            img.color = col;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return go.GetComponent<Button>();
        }
        #endregion

        #region UI Updates
        private void UpdateTimerWidgetUI()
        {
            if (timerPillText == null) return;

            float remaining = RemainingSessionSeconds;
            int mins = Mathf.FloorToInt(remaining / 60f);
            int secs = Mathf.FloorToInt(remaining % 60f);

            timerPillText.text = $"{mins:00}:{secs:00}";

            if (timerPillBg != null)
            {
                if (remaining <= 60f)
                {
                    // Dưới 1 phút: Đỏ cảnh báo
                    timerPillBg.color = new Color(0.85f, 0.25f, 0.25f, 0.95f);
                }
                else if (remaining <= 300f)
                {
                    // Dưới 5 phút: Vàng cam
                    timerPillBg.color = new Color(0.85f, 0.55f, 0.15f, 0.95f);
                }
                else
                {
                    // Còn nhiều thời gian: Xanh đen hiện đại
                    timerPillBg.color = new Color(0.08f, 0.12f, 0.20f, 0.90f);
                }
            }
        }

        private void UpdateBreakUI()
        {
            if (breakCountdownText != null)
            {
                int mins = Mathf.FloorToInt(breakRemainingSeconds / 60f);
                int secs = Mathf.FloorToInt(breakRemainingSeconds % 60f);
                breakCountdownText.text = $"{mins:00}:{secs:00}";
            }
        }

        private void UpdateUIWidgetVisibility()
        {
            bool isTeacher = SessionManager.Instance != null && SessionManager.Instance.IsTeacher;
            if (timerPillGo != null)
            {
                timerPillGo.SetActive(isEnabled && showTimerWidget && !isTeacher);
            }
            if (isTeacher && breakOverlayGo != null && breakOverlayGo.activeSelf)
            {
                breakOverlayGo.SetActive(false);
                isInBreak = false;
            }
        }

        private void OnTimerPillClicked()
        {
            int usedMins = Mathf.FloorToInt(currentContinuousSeconds / 60f);
            int dailyMins = Mathf.FloorToInt(dailyAccumulatedSeconds / 60f);
            string msg = $"<b>Thông tin phiên học hiện tại:</b>\n• Đã học liên tục: <b>{usedMins} / {maxContinuousMinutes} phút</b>\n• Tổng thời gian hôm nay: <b>{dailyMins} / {(maxDailyMinutes > 0 ? maxDailyMinutes + " phút" : "Không giới hạn")}</b>";
            ShowWarningToast(msg, 6f);
        }

        public void ShowWarningToast(string message, float duration = 7f)
        {
            if (toastBannerGo == null || toastBannerText == null) return;

            toastBannerText.text = message;
            toastBannerGo.SetActive(true);

            if (toastCoroutine != null) StopCoroutine(toastCoroutine);
            toastCoroutine = StartCoroutine(DismissToastRoutine(duration));
        }

        private IEnumerator DismissToastRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (toastBannerGo != null)
            {
                toastBannerGo.SetActive(false);
            }
        }

        private void OpenParentPinModal()
        {
            if (parentPinModalGo == null) return;

            // Tạo câu đố nhân hai số ngẫu nhiên cho phụ huynh
            int a = UnityEngine.Random.Range(12, 19);
            int b = UnityEngine.Random.Range(11, 16);
            mathAnsCorrect = a * b;

            if (parentChallengeText != null)
            {
                parentChallengeText.text = $"Hoặc giải nhanh: <b>{a} × {b} = ?</b>";
            }

            if (inputPinField != null) inputPinField.text = string.Empty;
            if (pinStatusText != null) pinStatusText.text = string.Empty;

            parentPinModalGo.SetActive(true);
        }

        private void TryUnlockWithExtension(int extraMinutes)
        {
            string entered = inputPinField != null ? inputPinField.text.Trim() : "";

            bool isPinCorrect = VerifyParentPin(entered);
            bool isMathCorrect = int.TryParse(entered, out int ans) && ans == mathAnsCorrect;

            if (isPinCorrect || isMathCorrect)
            {
                parentPinModalGo.SetActive(false);
                ExtendSessionMinutes(extraMinutes);
            }
            else
            {
                if (pinStatusText != null)
                {
                    pinStatusText.text = "<color=#e74c3c>Mã PIN hoặc kết quả câu đố chưa chính xác. Vui lòng thử lại!</color>";
                }
            }
        }
        #endregion
    }
}
