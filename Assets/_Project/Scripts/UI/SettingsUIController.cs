using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;

namespace Math6Companion.UI
{
    /// <summary>
    /// Điều khiển Scene Cài đặt (11_Settings.unity).
    /// Chỉnh âm lượng (Master/BGM/SFX), chỉnh sáng tối (Brightness/Theme), và đặt bí danh.
    /// </summary>
    /// <summary>
    /// Điều khiển Scene Cài đặt (11_Settings.unity).
    /// Chỉnh âm lượng (Master/BGM/SFX), chỉnh sáng tối (Brightness/Theme), và đặt bí danh.
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

        [Header("Profile / Nickname")]
        [SerializeField] private TMP_InputField nicknameInputField;

        [Header("Action Buttons")]
        [SerializeField] private Button saveSettingsButton;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private TextMeshProUGUI notifyToastText;

        [Header("Visual Theme Elements")]
        [SerializeField] private Image settingsBackground;
        [SerializeField] private Image settingsCard;
        [SerializeField] private TextMeshProUGUI settingsTitleText;

        private void Start()
        {
            if (saveSettingsButton != null)
                saveSettingsButton.onClick.AddListener(OnSaveSettingsClicked);

            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);

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

            LoadCurrentSettings();
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
            if (nicknameInputField != null) nicknameInputField.text = nick;

            UpdateValueTexts(master, bgm, sfx, bright);
            ApplyTheme(isDark);

            if (notifyToastText != null) notifyToastText.text = string.Empty;
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

        private void ApplyTheme(bool isDark)
        {
            if (isDark)
            {
                // Dark Mode: Tông xanh đen vũ trụ huyền bí
                if (settingsBackground != null) settingsBackground.color = new Color(0.06f, 0.09f, 0.14f);
                if (settingsCard != null) settingsCard.color = new Color(0.12f, 0.17f, 0.25f);
                if (settingsTitleText != null) settingsTitleText.color = new Color(0.95f, 0.75f, 0.2f); // Gold

                if (settingsCard != null)
                {
                    foreach (var tmp in settingsCard.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        if (tmp == notifyToastText) continue;

                        if (tmp.name.Contains("Header"))
                            tmp.color = new Color(0.95f, 0.75f, 0.2f); // Vàng gold
                        else if (tmp.name.Contains("Val"))
                            tmp.color = new Color(0.95f, 0.85f, 0.2f); // Vàng tươi
                        else
                            tmp.color = Color.white; // Chữ nhãn trắng sáng rõ nét
                    }
                }

                SetSlidersTrackColor(new Color(0.18f, 0.22f, 0.30f));
                SetInputFieldTheme(new Color(0.15f, 0.2f, 0.25f), Color.white);
            }
            else
            {
                // Light Mode: Tông màu sáng học đường hiện đại với độ tương phản cao
                if (settingsBackground != null) settingsBackground.color = new Color(0.90f, 0.93f, 0.97f);
                if (settingsCard != null) settingsCard.color = new Color(1.0f, 1.0f, 1.0f); // Nền thẻ trắng tinh
                if (settingsTitleText != null) settingsTitleText.color = new Color(0.08f, 0.16f, 0.32f); // Xanh navy đậm

                if (settingsCard != null)
                {
                    foreach (var tmp in settingsCard.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        if (tmp == notifyToastText) continue;

                        if (tmp.name.Contains("Header"))
                            tmp.color = new Color(0.10f, 0.25f, 0.65f); // Xanh dương hoàng gia
                        else if (tmp.name.Contains("Val"))
                            tmp.color = new Color(0.05f, 0.50f, 0.35f); // Xanh lục bảo đậm
                        else
                            tmp.color = new Color(0.08f, 0.12f, 0.20f); // Xanh đen tương phản cực cao trên nền trắng!
                    }
                }

                SetSlidersTrackColor(new Color(0.85f, 0.88f, 0.93f)); // Rãnh trượt màu xám sáng
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
            if (nicknameInputField == null) return;
            var img = nicknameInputField.GetComponent<Image>();
            if (img != null) img.color = bgCol;
            if (nicknameInputField.textComponent != null) nicknameInputField.textComponent.color = textCol;
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

            if (notifyToastText != null)
            {
                notifyToastText.text = $"<color=#2ecc71>[OK] Đã lưu cài đặt! Bí danh hiển thị: \"{newNick}\"</color>";
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
