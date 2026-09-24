using System;
using UnityEngine;
using Math6Companion.Core.Contracts;

namespace Math6Companion.Core
{
    /// <summary>
    /// Quản lý phiên làm việc của học sinh/giáo viên và cài đặt ứng dụng.
    /// Tồn tại xuyên suốt vòng đời ứng dụng (DontDestroyOnLoad).
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        private static SessionManager instance;
        public static SessionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<SessionManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("[SessionManager]");
                        instance = go.AddComponent<SessionManager>();
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("Current User Session")]
        [SerializeField] private UserData currentUser;

        [Header("Settings & Preferences")]
        [Range(0f, 1f)] public float masterVolume = 1.0f;
        [Range(0f, 1f)] public float bgmVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 1.0f;
        [Range(0.2f, 1f)] public float brightness = 1.0f;
        public bool isDarkMode = false;

        public UserData CurrentUser => currentUser;
        public bool IsLoggedIn => currentUser != null && !string.IsNullOrEmpty(currentUser.userId);

        public event Action<UserData> OnUserLoggedIn;
        public event Action OnUserLoggedOut;
        public event Action OnSettingsChanged;

        private UnityEngine.UI.Image dimmerImage;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSavedPreferences();
            InitScreenDimmer();
        }

        private void InitScreenDimmer()
        {
            if (dimmerImage != null) return;

            var dimmerGo = new GameObject("[GlobalScreenDimmer]", typeof(RectTransform), typeof(Canvas));
            dimmerGo.transform.SetParent(transform, false);
            var canvas = dimmerGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767; // Luôn nằm trên tất cả UI khác

            var imgGo = new GameObject("DimmerOverlay", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            imgGo.transform.SetParent(dimmerGo.transform, false);
            var rt = imgGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            dimmerImage = imgGo.GetComponent<UnityEngine.UI.Image>();
            dimmerImage.raycastTarget = false; // Chuột xuyên qua để vẫn click được UI bên dưới
            ApplyDimmerColor();
        }

        private void ApplyDimmerColor()
        {
            if (dimmerImage != null)
            {
                // Khi brightness = 1.0 => alpha = 0 (sáng tối đa)
                // Khi brightness = 0.2 => alpha = 0.56 (tối dịu mà vẫn giữ màu sắc tươi tắn)
                float alpha = Mathf.Clamp01((1.0f - brightness) * 0.70f);
                dimmerImage.color = new Color(0f, 0f, 0f, alpha);
            }
        }

        public void SetUserSession(UserData user)
        {
            currentUser = user;

            if (user != null)
            {
                PlayerPrefs.SetString("User_ID", user.userId ?? "");
                PlayerPrefs.SetString("User_Username", user.username ?? "");
                PlayerPrefs.SetString("User_FullName", user.fullName ?? "");
                PlayerPrefs.SetString("User_Role", user.role ?? "");
                PlayerPrefs.SetInt("User_Grade", user.grade);
                PlayerPrefs.SetString("User_ClassId", user.classId ?? "");
                PlayerPrefs.SetString("User_Nickname", user.nickname ?? user.fullName ?? "Học sinh");
                PlayerPrefs.Save();

                OnUserLoggedIn?.Invoke(user);
            }
        }

        public void UpdateNickname(string newNickname)
        {
            if (currentUser != null && !string.IsNullOrEmpty(newNickname))
            {
                currentUser.nickname = newNickname;
                PlayerPrefs.SetString("User_Nickname", newNickname);
                PlayerPrefs.Save();
                OnSettingsChanged?.Invoke();
            }
        }

        public void UpdateAudioSettings(float master, float bgm, float sfx)
        {
            masterVolume = Mathf.Clamp01(master);
            bgmVolume = Mathf.Clamp01(bgm);
            sfxVolume = Mathf.Clamp01(sfx);

            PlayerPrefs.SetFloat("Settings_MasterVol", masterVolume);
            PlayerPrefs.SetFloat("Settings_BgmVol", bgmVolume);
            PlayerPrefs.SetFloat("Settings_SfxVol", sfxVolume);
            PlayerPrefs.Save();

            OnSettingsChanged?.Invoke();
        }

        public void UpdateDisplaySettings(float newBrightness, bool darkMode)
        {
            brightness = Mathf.Clamp(newBrightness, 0.2f, 1.0f);
            isDarkMode = darkMode;

            PlayerPrefs.SetFloat("Settings_Brightness", brightness);
            PlayerPrefs.SetInt("Settings_DarkMode", isDarkMode ? 1 : 0);
            PlayerPrefs.Save();

            ApplyDimmerColor();
            OnSettingsChanged?.Invoke();
        }

        public void ClearSession()
        {
            currentUser = null;
            PlayerPrefs.DeleteKey("User_ID");
            PlayerPrefs.DeleteKey("User_Username");
            PlayerPrefs.DeleteKey("User_FullName");
            PlayerPrefs.DeleteKey("User_Role");
            PlayerPrefs.DeleteKey("User_Grade");
            PlayerPrefs.DeleteKey("User_ClassId");
            PlayerPrefs.Save();

            OnUserLoggedOut?.Invoke();
        }

        private void LoadSavedPreferences()
        {
            masterVolume = PlayerPrefs.GetFloat("Settings_MasterVol", 1.0f);
            bgmVolume = PlayerPrefs.GetFloat("Settings_BgmVol", 0.8f);
            sfxVolume = PlayerPrefs.GetFloat("Settings_SfxVol", 1.0f);
            brightness = PlayerPrefs.GetFloat("Settings_Brightness", 1.0f);
            isDarkMode = PlayerPrefs.GetInt("Settings_DarkMode", 0) == 1;

            // Kiểm tra nếu có session cũ đã lưu
            string savedUserId = PlayerPrefs.GetString("User_ID", "");
            if (!string.IsNullOrEmpty(savedUserId))
            {
                currentUser = new UserData
                {
                    userId = savedUserId,
                    username = PlayerPrefs.GetString("User_Username", ""),
                    fullName = PlayerPrefs.GetString("User_FullName", ""),
                    role = PlayerPrefs.GetString("User_Role", "STUDENT"),
                    grade = PlayerPrefs.GetInt("User_Grade", 6),
                    classId = PlayerPrefs.GetString("User_ClassId", "6A1"),
                    nickname = PlayerPrefs.GetString("User_Nickname", "Học sinh")
                };
            }
            else
            {
                // Mặc định cho kiểm thử
                currentUser = new UserData
                {
                    userId = "STU_DEV_01",
                    username = "an_nguyen",
                    fullName = "Nguyễn Văn An",
                    nickname = "Hiệp Sĩ Toán 6",
                    role = "STUDENT",
                    grade = 6,
                    classId = "6A1"
                };
            }
        }
    }
}
