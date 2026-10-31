using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;

namespace Math6Companion.UI
{
    /// <summary>
    /// Điều khiển Scene Cài đặt (11_Settings.unity).
    /// Chỉnh âm lượng (Master/BGM/SFX), chỉnh sáng tối (Brightness/Theme), đặt bí danh,
    /// và Quản lý phiên chơi & Giới hạn thời gian bảo vệ thị lực cho học sinh lớp 6 (Dành cho Phụ Huynh).
    /// </summary>
    public class SettingsUIController : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterValueText;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private TextMeshProUGUI bgmValueText;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI sfxValueText;

        [Header("Display Settings")]
        [SerializeField] private Slider brightnessSlider;
        [SerializeField] private TextMeshProUGUI brightnessValueText;
        [SerializeField] private Toggle darkModeToggle;
        [SerializeField] private Toggle fullScreenToggle;

        [Header("Profile / Nickname")]
        [SerializeField] private TMP_InputField nicknameInputField;

        [Header("Parental Controls & Play Session")]
        [SerializeField] private Toggle screenTimeLimitToggle;
        [SerializeField] private TMP_Dropdown continuousLimitDropdown;
        [SerializeField] private TMP_Dropdown dailyLimitDropdown;
        [SerializeField] private TextMeshProUGUI todayUsageText;
        [SerializeField] private TMP_InputField parentPinInputField;
        [SerializeField] private Button resetTodayUsageButton;

        [Header("Action Buttons")]
        [SerializeField] private Button saveSettingsButton;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button resetProgressButton;
        [SerializeField] private Button teacherPortalButton;
        [SerializeField] private TextMeshProUGUI notifyToastText;

        [Header("Visual Theme Elements")]
        [SerializeField] private Image settingsBackground;
        [SerializeField] private Image settingsCard;
        [SerializeField] private TextMeshProUGUI settingsTitleText;

        private static readonly int[] CONTINUOUS_OPTIONS = { 1, 15, 20, 30, 45, 60 };
        private static readonly int[] DAILY_OPTIONS = { 45, 60, 90, 120, 0 }; // 0 = Không giới hạn

        private void Start()
        {
            if (saveSettingsButton != null)
                saveSettingsButton.onClick.AddListener(OnSaveSettingsClicked);

            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);

            if (resetProgressButton != null)
                resetProgressButton.onClick.AddListener(OnResetProgressClicked);

            if (resetTodayUsageButton != null)
                resetTodayUsageButton.onClick.AddListener(OnResetTodayUsageClicked);

            if (teacherPortalButton == null)
            {
                var btn = GameObject.Find("Btn_TeacherPortal");
                if (btn != null) teacherPortalButton = btn.GetComponent<Button>();
            }
            if (teacherPortalButton != null)
            {
                bool isTeacher = SessionManager.Instance != null && SessionManager.Instance.IsTeacher;
                teacherPortalButton.gameObject.SetActive(isTeacher);
                teacherPortalButton.onClick.AddListener(() =>
                {
                    TeacherPortalUIController.Open();
                });
            }

            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterSliderChanged);

            if (bgmVolumeSlider != null)
                bgmVolumeSlider.onValueChanged.AddListener(OnBgmSliderChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxSliderChanged);

            if (brightnessSlider != null)
                brightnessSlider.onValueChanged.AddListener(OnBrightnessSliderChanged);

            if (darkModeToggle != null)
                darkModeToggle.onValueChanged.AddListener(OnDarkModeToggled);

            if (fullScreenToggle != null)
                fullScreenToggle.onValueChanged.AddListener(OnFullScreenToggled);

            InitDropdownOptions();
            LoadCurrentSettings();
        }

        private void InitDropdownOptions()
        {
            if (continuousLimitDropdown != null)
            {
                continuousLimitDropdown.ClearOptions();
                var list = new System.Collections.Generic.List<string>
                {
                    "1 phút / phiên (Kiểm thử TEST 60s)",
                    "15 phút / phiên (Nhẹ nhàng)",
                    "20 phút / phiên",
                    "30 phút / phiên (Khuyến nghị Lớp 6)",
                    "45 phút / phiên (1 tiết học)",
                    "60 phút / phiên"
                };
                continuousLimitDropdown.AddOptions(list);
            }

            if (dailyLimitDropdown != null)
            {
                dailyLimitDropdown.ClearOptions();
                var list = new System.Collections.Generic.List<string>
                {
                    "45 phút / ngày",
                    "60 phút / ngày (1 giờ)",
                    "90 phút / ngày (Khuyến nghị 1.5 giờ)",
                    "120 phút / ngày (2 giờ)",
                    "Không giới hạn ngày"
                };
                dailyLimitDropdown.AddOptions(list);
            }
        }

        private void LoadCurrentSettings()
        {
            float master = 1.0f;
            float bgm = 0.8f;
            float sfx = 1.0f;
            float bright = 1.0f;
            bool isDark = false;
            string nick = "Học sinh";

            if (SessionManager.Instance != null)
            {
                master = SessionManager.Instance.masterVolume;
                bgm = SessionManager.Instance.bgmVolume;
                sfx = SessionManager.Instance.sfxVolume;
                bright = SessionManager.Instance.brightness;
                isDark = SessionManager.Instance.isDarkMode;

                if (SessionManager.Instance.CurrentUser != null)
                {
                    nick = SessionManager.Instance.CurrentUser.nickname;
                }
            }

            if (masterVolumeSlider != null) masterVolumeSlider.value = master;
            if (bgmVolumeSlider != null) bgmVolumeSlider.value = bgm;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfx;
            if (brightnessSlider != null) brightnessSlider.value = bright;
            if (darkModeToggle != null) darkModeToggle.isOn = isDark;
            if (fullScreenToggle != null) fullScreenToggle.isOn = Screen.fullScreen;
            if (nicknameInputField != null) nicknameInputField.text = nick;

            // Load PlaySessionManager settings
            if (PlaySessionManager.Instance != null)
            {
                var ps = PlaySessionManager.Instance;
                if (screenTimeLimitToggle != null)
                {
                    screenTimeLimitToggle.isOn = ps.IsEnabled;
                }

                if (continuousLimitDropdown != null)
                {
                    int idx = System.Array.IndexOf(CONTINUOUS_OPTIONS, ps.MaxContinuousMinutes);
                    continuousLimitDropdown.value = idx >= 0 ? idx : 3; // Mặc định 30 phút (index 3)
                }

                if (dailyLimitDropdown != null)
                {
                    int idx = System.Array.IndexOf(DAILY_OPTIONS, ps.MaxDailyMinutes);
                    dailyLimitDropdown.value = idx >= 0 ? idx : 2; // Mặc định 90 phút (index 2)
                }

                if (parentPinInputField != null)
                {
                    parentPinInputField.text = ps.ParentPin;
                }

                UpdateTodayUsageDisplay();
            }

            UpdateValueTexts(master, bgm, sfx, bright);
            ApplyTheme(isDark);

            if (notifyToastText != null) notifyToastText.text = string.Empty;
        }

        private void UpdateTodayUsageDisplay()
        {
            if (todayUsageText != null && PlaySessionManager.Instance != null)
            {
                var ps = PlaySessionManager.Instance;
                int usedMins = Mathf.FloorToInt(ps.DailyAccumulatedSeconds / 60f);
                string maxStr = ps.MaxDailyMinutes > 0 ? $"{ps.MaxDailyMinutes} phút" : "Không giới hạn";
                todayUsageText.text = $"Hôm nay em đã học: <b><color=#14B875>{usedMins} phút</color></b> / Giới hạn: <b>{maxStr}</b> (Phiên: <b>{ps.MaxContinuousMinutes}p</b>)";
            }
        }

        private void UpdateValueTexts(float master, float bgm, float sfx, float bright)
        {
            if (masterValueText != null) masterValueText.text = $"{Mathf.RoundToInt(master * 100)}%";
            if (bgmValueText != null) bgmValueText.text = $"{Mathf.RoundToInt(bgm * 100)}%";
            if (sfxValueText != null) sfxValueText.text = $"{Mathf.RoundToInt(sfx * 100)}%";
            if (brightnessValueText != null) brightnessValueText.text = $"{Mathf.RoundToInt(bright * 100)}%";
        }

        private void OnMasterSliderChanged(float val)
        {
            if (masterValueText != null) masterValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
            ApplyAudioSettings();
        }

        private void OnBgmSliderChanged(float val)
        {
            if (bgmValueText != null) bgmValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
            ApplyAudioSettings();
        }

        private void OnSfxSliderChanged(float val)
        {
            if (sfxValueText != null) sfxValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
            ApplyAudioSettings();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPreviewTone();
            }
        }

        private void ApplyAudioSettings()
        {
            if (SessionManager.Instance != null)
            {
                float master = masterVolumeSlider != null ? masterVolumeSlider.value : 1.0f;
                float bgm = bgmVolumeSlider != null ? bgmVolumeSlider.value : 0.8f;
                float sfx = sfxVolumeSlider != null ? sfxVolumeSlider.value : 1.0f;

                SessionManager.Instance.UpdateAudioSettings(master, bgm, sfx);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.ApplyVolumeSettings();
                }
            }
        }

        private void OnBrightnessSliderChanged(float val)
        {
            if (brightnessValueText != null) brightnessValueText.text = $"{Mathf.RoundToInt(val * 100)}%";
            if (SessionManager.Instance != null)
            {
                bool isDark = darkModeToggle != null && darkModeToggle.isOn;
                SessionManager.Instance.UpdateDisplaySettings(val, isDark);
            }
        }

        private void OnDarkModeToggled(bool isDark)
        {
            ApplyTheme(isDark);
            if (SessionManager.Instance != null)
            {
                float bright = brightnessSlider != null ? brightnessSlider.value : 1.0f;
                SessionManager.Instance.UpdateDisplaySettings(bright, isDark);
            }
        }

        private void OnFullScreenToggled(bool isFull)
        {
            Screen.fullScreen = isFull;
        }

        private void OnResetTodayUsageClicked()
        {
            PlayClick();
            if (PlaySessionManager.Instance != null)
            {
                PlaySessionManager.Instance.ResetDailyUsage();
                UpdateTodayUsageDisplay();
                if (notifyToastText != null)
                {
                    notifyToastText.text = "<color=#2ecc71>✅ Đã đặt lại thời gian học tập hôm nay về 0 phút!</color>";
                }
            }
        }

        private void OnResetProgressClicked()
        {
            PlayClick();
            if (notifyToastText != null)
            {
                notifyToastText.text = "<color=#f39c12>ℹ️ Tiến độ học tập và câu hỏi sai đã được đồng bộ.</color>";
            }
        }

        private void ApplyTheme(bool isDark)
        {
            if (isDark)
            {
                if (settingsBackground != null) settingsBackground.color = new Color(0.06f, 0.09f, 0.14f);
                if (settingsCard != null) settingsCard.color = new Color(0.12f, 0.17f, 0.25f);
                if (settingsTitleText != null) settingsTitleText.color = new Color(0.95f, 0.75f, 0.2f);

                if (settingsCard != null)
                {
                    foreach (var tmp in settingsCard.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        if (tmp == notifyToastText) continue;

                        if (tmp.name.Contains("Header"))
                            tmp.color = new Color(0.95f, 0.75f, 0.2f);
                        else if (tmp.name.Contains("Val"))
                            tmp.color = new Color(0.95f, 0.85f, 0.2f);
                        else
                            tmp.color = Color.white;
                    }
                }

                SetSlidersTrackColor(new Color(0.18f, 0.22f, 0.30f));
                SetInputFieldTheme(new Color(0.15f, 0.2f, 0.25f), Color.white);
            }
            else
            {
                if (settingsBackground != null) settingsBackground.color = new Color(0.90f, 0.93f, 0.97f);
                if (settingsCard != null) settingsCard.color = new Color(1.0f, 1.0f, 1.0f);
                if (settingsTitleText != null) settingsTitleText.color = new Color(0.08f, 0.16f, 0.32f);

                if (settingsCard != null)
                {
                    foreach (var tmp in settingsCard.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        if (tmp == notifyToastText) continue;

                        if (tmp.name.Contains("Header"))
                            tmp.color = new Color(0.10f, 0.25f, 0.65f);
                        else if (tmp.name.Contains("Val"))
                            tmp.color = new Color(0.05f, 0.50f, 0.35f);
                        else
                            tmp.color = new Color(0.08f, 0.12f, 0.20f);
                    }
                }

                SetSlidersTrackColor(new Color(0.85f, 0.88f, 0.93f));
                SetInputFieldTheme(new Color(0.93f, 0.95f, 0.98f), new Color(0.08f, 0.12f, 0.20f));
            }
        }

        private void SetSlidersTrackColor(Color trackCol)
        {
            Slider[] sliders = { masterVolumeSlider, bgmVolumeSlider, sfxVolumeSlider, brightnessSlider };
            foreach (var s in sliders)
            {
                if (s == null) continue;
                var bg = s.transform.Find("Background")?.GetComponent<Image>();
                if (bg != null) bg.color = trackCol;
            }
        }

        private void SetInputFieldTheme(Color bgCol, Color textCol)
        {
            TMP_InputField[] fields = { nicknameInputField, parentPinInputField };
            foreach (var f in fields)
            {
                if (f == null) continue;
                var img = f.GetComponent<Image>();
                if (img != null) img.color = bgCol;
                if (f.textComponent != null) f.textComponent.color = textCol;
            }
        }

        private void OnSaveSettingsClicked()
        {
            PlayClick();

            float master = masterVolumeSlider != null ? masterVolumeSlider.value : 1.0f;
            float bgm = bgmVolumeSlider != null ? bgmVolumeSlider.value : 0.8f;
            float sfx = sfxVolumeSlider != null ? sfxVolumeSlider.value : 1.0f;
            float brightness = brightnessSlider != null ? brightnessSlider.value : 1.0f;
            bool isDark = darkModeToggle != null && darkModeToggle.isOn;
            string newNick = nicknameInputField != null ? nicknameInputField.text.Trim() : "Học sinh";

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.UpdateAudioSettings(master, bgm, sfx);
                SessionManager.Instance.UpdateDisplaySettings(brightness, isDark);

                if (!string.IsNullOrEmpty(newNick))
                {
                    SessionManager.Instance.UpdateNickname(newNick);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.ApplyVolumeSettings();
                }
            }

            // Lưu cài đặt quản lý phiên chơi
            if (PlaySessionManager.Instance != null)
            {
                bool limitEnabled = screenTimeLimitToggle == null || screenTimeLimitToggle.isOn;

                int contIdx = continuousLimitDropdown != null ? continuousLimitDropdown.value : 2;
                int continuousMins = contIdx >= 0 && contIdx < CONTINUOUS_OPTIONS.Length ? CONTINUOUS_OPTIONS[contIdx] : 30;

                int dailyIdx = dailyLimitDropdown != null ? dailyLimitDropdown.value : 2;
                int dailyMins = dailyIdx >= 0 && dailyIdx < DAILY_OPTIONS.Length ? DAILY_OPTIONS[dailyIdx] : 90;

                string newPin = parentPinInputField != null ? parentPinInputField.text.Trim() : "2026";
                if (string.IsNullOrEmpty(newPin) || newPin.Length < 4)
                {
                    newPin = "2026";
                }

                PlaySessionManager.Instance.UpdateSettings(limitEnabled, continuousMins, dailyMins, 5, newPin);
                UpdateTodayUsageDisplay();
            }

            if (notifyToastText != null)
            {
                notifyToastText.text = $"<color=#2ecc71>[OK] Đã lưu thành công! Giới hạn phiên: {(screenTimeLimitToggle != null && screenTimeLimitToggle.isOn ? "BẬT (30p/phiên)" : "TẮT")}</color>";
            }
        }

        private void OnBackToMenuClicked()
        {
            PlayClick();
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
