using System;
using System.Text.RegularExpressions;

namespace Math6Companion.Core
{
    /// <summary>
    /// Bộ xử lý ký hiệu Toán Học và chuẩn hóa Unicode toàn diện cho Toán 6:
    /// - Hỗ trợ song song cả ký hiệu chuẩn (∈, ∉, ℕ, ℤ, ≤, ≥...) và chữ thuần Việt (thuộc, không thuộc, N, Z, <=, >=...).
    /// - Đảm bảo AI chấm điểm, đối chiếu đáp án trắc nghiệm & tự luận không bao giờ bị lệch do khác biệt font/ký tự.
    /// - Hiển thị 100% rõ nét trên mọi font TextMeshPro mà không bao giờ bị lỗi ô vuông □.
    /// </summary>
    public static class MathTextSanitizer
    {
        /// <summary>
        /// Làm sạch và chuyển đổi ký hiệu toán học an toàn để hiển thị trên UI.
        /// Chuyển các ký hiệu không có trong font TMP tiêu chuẩn sang dạng tiếng Việt thân thiện, rõ ràng.
        /// </summary>
        public static string SanitizeForDisplay(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            string s = text;

            // 1. Xử lý chuỗi Unicode escape nếu có từ JSON hoặc Google Sheets
            s = s.Replace("\\u2208", " thuộc ")
                 .Replace("\\u2209", " không thuộc ")
                 .Replace("\\u2115*", "N*")
                 .Replace("\\u2115", "N")
                 .Replace("\\u2124", "Z")
                 .Replace("\\u2264", "<=")
                 .Replace("\\u2265", ">=")
                 .Replace("\\u2248", "≈")
                 .Replace("\\u2260", "!=")
                 .Replace("\\u2282", " con ")
                 .Replace("\\u2286", " tập con ")
                 .Replace("\\u2229", " giao ")
                 .Replace("\\u222A", " hợp ")
                 .Replace("\\u22EE", " ⋮ ")
                 .Replace("\\u2B50", "*")
                 .Replace("\\u2728", "")
                 .Replace("\\u238B", "[ESC]")
                 .Replace("\\u2699", "")
                 .Replace("\\u2694", "")
                 .Replace("\\u2715", "[X]")
                 .Replace("\\u2713", "[OK]")
                 .Replace("\\u274C", "[!]");

            // 2. Chuyển đổi các ký hiệu toán học đặc biệt mà font TMP không hỗ trợ sang chữ tiếng Việt thân thiện
            // Giải quyết triệt để vấn đề "dấu thuộc" bị biến thành ô vuông □
            s = s.Replace("∈", " thuộc ")
                 .Replace("∉", " không thuộc ")
                 .Replace("ℕ*", "N*")
                 .Replace("ℕ", "N")
                 .Replace("ℤ", "Z")
                 .Replace("≤", "<=")
                 .Replace("≥", ">=")
                 .Replace("≠", "!=")
                 .Replace("⊂", " con ")
                 .Replace("⊆", " tập con ")
                 .Replace("⇒", " => ")
                 .Replace("⇔", " <=> ");

            // 3. Khắc phục triệt để trường hợp ký tự bị biến thành ô vuông □ từ trước (do dữ liệu bị lỗi hiển thị)
            // Ví dụ: "x □ N" -> "x thuộc N", "0 □ A" -> "0 thuộc A", "4 □ A" -> "4 thuộc A"
            s = Regex.Replace(s, @"\b([a-zA-Z0-9]+)\s*□\s*([a-zA-Z0-9]+)\b", "$1 thuộc $2");
            s = s.Replace("□", " thuộc ");

            // 4. Làm sạch các emoji giao diện nếu còn sót
            s = s.Replace("✨", "")
                 .Replace("⎋", "[ESC]")
                 .Replace("⚙️", "")
                 .Replace("⚙", "")
                 .Replace("⚔️", "")
                 .Replace("⚔", "")
                 .Replace("⏱️", "")
                 .Replace("⏱", "")
                 .Replace("⭐", "*")
                 .Replace("✕", "[X]")
                 .Replace("❌", "[!]")
                 .Replace("✓", "[OK]")
                 .Replace("🔍", "")
                 .Replace("🖼️", "")
                 .Replace("🖼", "")
                 .Replace("🏡", "[3D]")
                 .Replace("•", "-");

            // 5. Chuẩn hóa khoảng trắng để câu hiển thị gọn gàng, tự nhiên
            s = Regex.Replace(s, @"[ ]{2,}", " ");
            s = s.Replace(" thuộc A", " thuộc A")
                 .Replace("không thuộc A", "không thuộc A")
                 .Replace("{x thuộc N", "{x thuộc N");

            return s.Trim();
        }

        /// <summary>
        /// Chuẩn hóa biểu thức và câu trả lời để đối chiếu đáp án chính xác 100%:
        /// "4 thuộc A" == "4 ∈ A"
        /// "9 không thuộc A" == "9 ∉ A"
        /// "x <= 6" == "x ≤ 6"
        /// "x thuộc N" == "x ∈ ℕ"
        /// </summary>
        public static string NormalizeForComparison(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            string s = input.Trim().ToLowerInvariant();

            // Dấu thuộc và không thuộc
            s = s.Replace("không thuộc", "∉")
                 .Replace("khong thuoc", "∉")
                 .Replace("k.thuộc", "∉")
                 .Replace("k.thuoc", "∉")
                 .Replace("k thuộc", "∉")
                 .Replace("k thuoc", "∉")
                 .Replace("\\u2209", "∉")
                 .Replace("thuộc", "∈")
                 .Replace("thuoc", "∈")
                 .Replace("\\u2208", "∈");

            // Tập hợp số
            s = s.Replace("ℕ*", "n*")
                 .Replace("n*", "n*")
                 .Replace("ℕ", "n")
                 .Replace("\\u2115", "n")
                 .Replace("ℤ", "z")
                 .Replace("\\u2124", "z");

            // So sánh
            s = s.Replace("≤", "<=")
                 .Replace("\\u2264", "<=")
                 .Replace("=<", "<=")
                 .Replace("≥", ">=")
                 .Replace("\\u2265", ">=")
                 .Replace("=>", ">=")
                 .Replace("≠", "!=")
                 .Replace("\\u2260", "!=");

            // Phép tính
            s = s.Replace("×", "*")
                 .Replace(":", "/")
                 .Replace("÷", "/");

            // Loại bỏ khoảng trắng thừa
            s = Regex.Replace(s, @"\s+", " ").Trim();

            return s;
        }

        /// <summary>
        /// Kiểm tra hai câu trả lời toán học có tương đương nhau hay không.
        /// </summary>
        public static bool IsMathAnswerEquivalent(string studentAns, string targetAns)
        {
            if (string.IsNullOrEmpty(studentAns) && string.IsNullOrEmpty(targetAns)) return true;
            if (string.IsNullOrEmpty(studentAns) || string.IsNullOrEmpty(targetAns)) return false;

            if (string.Equals(studentAns.Trim(), targetAns.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            string n1 = NormalizeForComparison(studentAns);
            string n2 = NormalizeForComparison(targetAns);

            if (n1 == n2) return true;

            // Xóa toàn bộ khoảng trắng để so sánh cấu trúc
            string c1 = n1.Replace(" ", "");
            string c2 = n2.Replace(" ", "");
            return c1 == c2;
        }
    }
}
