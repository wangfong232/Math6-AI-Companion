using UnityEngine;
using TMPro;

namespace Math6Companion.Core
{
    /// <summary>
    /// TMP Font Fallback Enabler - Đảm bảo cấu hình TMP an toàn không bị crash.
    /// Ký hiệu toán học được xử lý chuẩn hóa an toàn thông qua MathTextSanitizer.
    /// </summary>
    public static class TMPMathFontFallbackEnabler
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeMathFontFallback()
        {
            // Tắt cảnh báo missing glyph để tránh spam console khi gặp ký tự lạ
            var prop = typeof(TMP_Settings).GetProperty("warningsDisabled");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(null, true);
            }
        }
    }
}
