using UnityEngine;

namespace Math6Companion.Core
{
    /// <summary>
    /// =========================================================================
    ///               TRUNG TÂM CẤU HÌNH DUY NHẤT CỦA DỰ ÁN (APP CONFIG)
    /// =========================================================================
    /// MỖI KHI CẬP NHẬT LINK GOOGLE APPS SCRIPT WEB APP MỚI:
    /// CHỈ CẦN DÁN LINK VÀO BIẾN "APPS_SCRIPT_URL" BÊN DƯỚI LÀ XONG!
    /// TẤT CẢ CÁC MODULE (LOGIN, LESSONS, QUESTIONS, RESULTS, AI LOGS, MINIGAMES)
    /// ĐỀU SẼ TỰ ĐỘNG NHẬN LINK MỚI NÀY NGAY LẬP TỨC.
    /// =========================================================================
    /// </summary>
    public static class AppConfig
    {
        // >>>>> [ĐỔI LINK WEB APP TẠI ĐÂY] <<<<<
        public const string APPS_SCRIPT_URL = "https://script.google.com/macros/s/AKfycbx4nIX1jBnTk9zpTql8lg784yImNF4e4sPKcBhK8EZWvr2HLBsdQppj-sPo5ecjkxc/exec";

        // Khóa lưu tạm nếu muốn ghi đè qua Editor/Runtime
        private const string PREFS_BACKEND_URL_KEY = "Math6_Custom_Backend_URL";

        /// <summary>
        /// Lấy URL Web App đang hoạt động (Ưu tiên URL tùy chỉnh nếu có, mặc định lấy APPS_SCRIPT_URL ở trên)
        /// </summary>
        public static string GetActiveBackendUrl()
        {
            string custom = PlayerPrefs.GetString(PREFS_BACKEND_URL_KEY, "");
            if (!string.IsNullOrEmpty(custom) && custom.StartsWith("https://script.google.com/macros/s/"))
            {
                return custom.Trim();
            }
            return APPS_SCRIPT_URL.Trim();
        }

        /// <summary>
        /// Cập nhật nhanh URL Web App trực tiếp trong khi chạy game / Editor
        /// </summary>
        public static void SetCustomBackendUrl(string newUrl)
        {
            if (!string.IsNullOrEmpty(newUrl))
            {
                PlayerPrefs.SetString(PREFS_BACKEND_URL_KEY, newUrl.Trim());
                PlayerPrefs.Save();
                Debug.Log("[AppConfig] Đã cập nhật Backend URL mới: " + newUrl);
            }
        }

        /// <summary>
        /// Khôi phục về link mặc định trong code
        /// </summary>
        public static void ResetToDefaultUrl()
        {
            PlayerPrefs.DeleteKey(PREFS_BACKEND_URL_KEY);
            PlayerPrefs.Save();
            Debug.Log("[AppConfig] Đã reset về link mặc định: " + APPS_SCRIPT_URL);
        }
    }
}
