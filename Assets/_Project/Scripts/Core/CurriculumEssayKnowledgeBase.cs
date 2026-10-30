using System;
using System.Collections.Generic;
using UnityEngine;
using Math6Companion.Core;
using Math6Companion.Core.Contracts;

namespace Math6Companion.Core
{
    /// <summary>
    /// Cơ sở dữ liệu Chuẩn kiến thức Toán 6 cho 39 câu tự luận trong chương trình.
    /// Tự động bổ sung Rubric chuẩn và Đáp số cho các câu hỏi lấy từ Google Sheets
    /// khi sheet bị để trống cột correctAnswer hoặc rubric.
    /// </summary>
    public static class CurriculumEssayKnowledgeBase
    {
        public static void EnrichQuestionData(QuestionData q)
        {
            if (q == null) return;
            string prompt = q.prompt ?? "";
            string qid = q.questionId ?? "";

            string ans = GetAnswer(prompt, qid);
            if (!string.IsNullOrEmpty(ans))
            {
                q.correctAnswer = ans;
            }

            var rubric = GetRubric(prompt, qid, q.correctAnswer);
            if (rubric != null && rubric.Count > 0)
            {
                q.rubric = rubric;
            }

            if (string.IsNullOrEmpty(q.imageUrl))
            {
                string norm = NormalizePrompt(prompt);
                if (norm.Contains("taphopa") || norm.Contains("lonhon3") || qid.Contains("SET"))
                {
                    q.imageUrl = "https://picsum.photos/800/600.jpg";
                }
            }
        }

        public static void EnrichSubmissionPayload(EssaySubmissionPayload payload)
        {
            if (payload == null) return;
            string prompt = payload.prompt ?? "";
            string qid = payload.questionId ?? "";

            if (string.IsNullOrEmpty(payload.correctAnswer))
            {
                string ans = GetAnswer(prompt, qid);
                if (!string.IsNullOrEmpty(ans))
                {
                    payload.correctAnswer = ans;
                }
            }

            if (payload.rubric == null || payload.rubric.Count == 0 || 
                (payload.rubric.Count == 3 && payload.rubric[0].description.Contains("Biến đổi / tính toán bước 1")))
            {
                var rubric = GetRubric(prompt, qid, payload.correctAnswer);
                if (rubric != null && rubric.Count > 0)
                {
                    payload.rubric = rubric;
                }
            }
        }

        public static string NormalizePrompt(string p)
        {
            if (string.IsNullOrEmpty(p)) return "";
            return p.ToLowerInvariant()
                    .Replace(" ", "")
                    .Replace("×", "x")
                    .Replace("*", "x")
                    .Replace("·", "x")
                    .Replace("–", "-")
                    .Replace("—", "-")
                    .Replace(".", "")
                    .Replace(":", "/")
                    .Replace("\n", "")
                    .Replace("\r", "");
        }

        public static string GetAnswer(string prompt, string questionId)
        {
            string p = NormalizePrompt(prompt);
            string qid = (questionId ?? "").ToUpperInvariant();

            // CHƯƠNG 1: SỐ TỰ NHIÊN
            if (qid == "Q_CH1_028" || qid == "ESSAY_ORDER_01" || qid == "ESSAY_001" || p.Contains("15-(3+2)") || p.Contains("15-(3+2)x2"))
                return "5";
            if (qid == "Q_CH1_029" || p.Contains("23^2+5(2+3)") || (p.Contains("2") && p.Contains("3^2") && p.Contains("2+3")))
                return "43";
            if (qid == "Q_CH1_030" || p.Contains("120+[55-") || p.Contains("11-32") || p.Contains("11-3x2"))
                return "158";

            // CHƯƠNG 2: TÍNH CHIA HẾT
            if (qid == "Q_CH2_028" || p.Contains("máya") && p.Contains("6tháng") && p.Contains("9tháng"))
                return "18";
            if (qid == "Q_CH2_029" || p.Contains("ưc(24,28)") || p.Contains("ước(24,28)"))
                return "4";
            if (qid == "Q_CH2_030" || (p.Contains("hàng2") && p.Contains("hàng3") && p.Contains("hàng7")))
                return "42";

            // CHƯƠNG 3: SỐ NGUYÊN
            if (qid == "Q_CH3_012" || (p.Contains("mẫusơn") && p.Contains("giảmthêm5")))
                return "-8";
            if (qid == "Q_CH3_017" || p.Contains("(56-27)-(11+28-16)"))
                return "6";
            if (qid == "Q_CH3_018" || p.Contains("152+(-73)-(-18)-127") || p.Contains("152-73+18-127"))
                return "-30";
            if (qid == "Q_CH3_024" || p.Contains("(-8)72+8(-19)-(-8)") || (p.Contains("(-8)") && p.Contains("72") && p.Contains("(-19)")))
                return "-720";
            if (qid == "Q_CH3_029" || (p.Contains("ướcchung") && p.Contains("15") && p.Contains("-25")))
                return "5";
            if (qid == "Q_CH3_030" || (p.Contains("200bộquầnáonữ") && p.Contains("150bộquầnáonam")))
                return "-5";

            // CHƯƠNG 4: HÌNH HỌC TRỰC QUAN
            if (qid == "Q_CH4_028" || (p.Contains("8mx6m") && p.Contains("40cm")))
                return "300";
            if (qid == "Q_CH4_029" || (p.Contains("35cm") && p.Contains("30cm") && p.Contains("260m")))
                return "200";
            if (qid == "Q_CH4_030" || (p.Contains("chiềudài20m") && p.Contains("1/4")))
                return "100";

            // CHƯƠNG 5: TÍNH ĐỐI XỨNG
            if (qid == "Q_CH5_013" || p.Contains("tờgiấytrắngvàkéo"))
                return "trục đối xứng";
            if (qid == "Q_CH5_014" || (p.Contains("conbướm") && p.Contains("thápeiffel")))
                return "trục đối xứng";
            if (qid == "Q_CH5_015" || p.Contains("{a,b,g,h,e,f}"))
                return "trục đối xứng";
            if (qid == "Q_CH5_028" || p.Contains("tâmđốixứngcủamộtđoạnthẳng"))
                return "tâm đối xứng";
            if (qid == "Q_CH5_029" || p.Contains("hìnhbìnhhànhvàhìnhthangcân"))
                return "tâm đối xứng";
            if (qid == "Q_CH5_030" || p.Contains("cánhquạttrần"))
                return "tâm đối xứng";

            // CHƯƠNG 6: PHÂN SỐ
            if (qid == "Q_CH6_018" || (p.Contains("tuấn") && p.Contains("3giờ") && p.Contains("2/3") && p.Contains("5/3")))
                return "2/3";
            if (qid == "Q_CH6_024" || (p.Contains("7/2") && p.Contains("21/10")))
                return "3/5";
            if (qid == "Q_CH6_029" || (p.Contains("1/6quãngđường") && p.Contains("1/7") && p.Contains("1/9")))
                return "53/126";
            if (qid == "Q_CH6_030" || (p.Contains("thịtbachỉ") && p.Contains("3/2") && p.Contains("600")))
                return "400";

            // CHƯƠNG 7: SỐ THẬP PHÂN
            if (qid == "Q_CH7_015" || (p.Contains("bắccực") && p.Contains("-3,4") && p.Contains("-49,3")))
                return "45.9";
            if (qid == "Q_CH7_022" || (p.Contains("6,32") && p.Contains("bốnđoạn")))
                return "1.6";
            if (qid == "Q_CH7_029" || (p.Contains("625nghìn") && p.Contains("10%")))
                return "62.5";
            if (qid == "Q_CH7_030" || (p.Contains("tân") && p.Contains("120") && p.Contains("60%")))
                return "200";

            // CHƯƠNG 8: HÌNH HỌC PHẲNG CƠ BẢN
            if (qid == "Q_CH8_015" || (p.Contains("gãyphầnngọn") && p.Contains("1,75") && p.Contains("3m")))
                return "4.75";
            if (qid == "Q_CH8_020" || (p.Contains("oa=4") && p.Contains("ob=6")))
                return "5";
            if (qid == "Q_CH8_025" || p.Contains("batiachunggốc"))
                return "3";
            if (qid == "Q_CH8_030" || p.Contains("63độ") && p.Contains("135độ"))
                return "góc nhọn";

            // CHƯƠNG 9: THỐNG KÊ & XÁC SUẤT
            if (qid == "Q_CH9_005" || (p.Contains("dươngxỉ") && p.Contains("vikhuẩn")))
                return "vi khuẩn";
            if (qid == "Q_CH9_010" || (p.Contains("40chiếcáo") && p.Contains("10chiếc")))
                return "4";
            if (qid == "Q_CH9_015" || (p.Contains("phongbaolìxì") && p.Contains("6c")))
                return "6C";
            if (qid == "Q_CH9_020" || p.Contains("biểuđồcộtvàbiểuđồcộtkép"))
                return "so sánh";
            if (qid == "Q_CH9_025" || (p.Contains("xúcxắc") && p.Contains("sốlẻ")))
                return "1, 3, 5";
            if (qid == "Q_CH9_030" || (p.Contains("100lần") && p.Contains("43lần")))
                return "0.43";

            return "";
        }

        public static List<RubricItem> GetRubric(string prompt, string questionId, string expectedAns)
        {
            string p = NormalizePrompt(prompt);
            string qid = (questionId ?? "").ToUpperInvariant();

            // 1. Q_CH1_028: 15 - (3+2)*2
            if (qid == "Q_CH1_028" || qid == "ESSAY_ORDER_01" || qid == "ESSAY_001" || p.Contains("15-(3+2)") || p.Contains("15-(3+2)x2"))
            {
                return new List<RubricItem>
                {
                    new RubricItem { step = 1, description = "Tính trong ngoặc trước: (3 + 2) = 5", points = 4 },
                    new RubricItem { step = 2, description = "Nhân trước: 5 x 2 = 10", points = 3 },
                    new RubricItem { step = 3, description = "Trừ sau: 15 - 10 = 5", points = 3 }
                };
            }

            // 2. Q_CH1_029: 2 . 3^2 + 5 . (2+3)
            if (qid == "Q_CH1_029" || p.Contains("23^2+5(2+3)"))
            {
                return new List<RubricItem>
                {
                    new RubricItem { step = 1, description = "Tính lũy thừa và trong ngoặc: 3^2 = 9 và (2 + 3) = 5", points = 4 },
                    new RubricItem { step = 2, description = "Nhân trước: 2 x 9 = 18 và 5 x 5 = 25", points = 3 },
                    new RubricItem { step = 3, description = "Cộng sau: 18 + 25 = 43", points = 3 }
                };
            }

            // 3. Q_CH1_030: 120 + [55 - (11 - 3.2)^2] + 2^3
            if (qid == "Q_CH1_030" || p.Contains("120+[55-"))
            {
                return new List<RubricItem>
                {
                    new RubricItem { step = 1, description = "Tính trong ngoặc tròn: 11 - 3.2 = 11 - 6 = 5", points = 4 },
                    new RubricItem { step = 2, description = "Tính trong ngoặc vuông và lũy thừa: 55 - 5^2 = 30 và 2^3 = 8", points = 3 },
                    new RubricItem { step = 3, description = "Cộng các kết quả: 120 + 30 + 8 = 158", points = 3 }
                };
            }

            // 4. Q_CH2_028: Bảo dưỡng máy A 6 tháng, máy B 9 tháng
            if (qid == "Q_CH2_028" || (p.Contains("máya") && p.Contains("6tháng") && p.Contains("9tháng")))
            {
                return new List<RubricItem>
                {
                    new RubricItem { step = 1, description = "Nhận diện số tháng là BCNN(6, 9)", points = 4 },
                    new RubricItem { step = 2, description = "Phân tích thừa số nguyên tố: 6 = 2 . 3; 9 = 3^2", points = 3 },
                    new RubricItem { step = 3, description = "Tính ra kết quả: BCNN(6, 9) = 2 . 3^2 = 18 (tháng)", points = 3 }
                };
            }

            // 5. Q_CH3_012: Đỉnh Mẫu Sơn
            if (qid == "Q_CH3_012" || p.Contains("mẫusơn"))
            {
                return new List<RubricItem>
                {
                    new RubricItem { step = 1, description = "Thiết lập phép tính trừ nhiệt độ: (-3) - 5", points = 4 },
                    new RubricItem { step = 2, description = "Chuyển về phép cộng với số đối: (-3) + (-5)", points = 3 },
                    new RubricItem { step = 3, description = "Tính ra kết quả: (-3) + (-5) = -8 độ C", points = 3 }
                };
            }

            // 6. Q_CH7_030: Tân nhận được 120 phiếu bầu
            if (qid == "Q_CH7_030" || (p.Contains("tân") && p.Contains("120") && p.Contains("60%")))
            {
                return new List<RubricItem>
                {
                    new RubricItem { step = 1, description = "Nhận diện bài toán: Tìm một số biết 60% của nó bằng 120", points = 4 },
                    new RubricItem { step = 2, description = "Lập phép tính chia: 120 : (60/100) = 120 : 0,6", points = 3 },
                    new RubricItem { step = 3, description = "Tính ra kết quả: 120 : 0,6 = 200 (người)", points = 3 }
                };
            }

            // 7. Q_CH7_022: Chia đều một thanh gỗ dài 6,32 m thành bốn đoạn bằng nhau
            if (qid == "Q_CH7_022" || (p.Contains("6,32") && (p.Contains("bốnđoạn") || p.Contains("thanhgỗ"))))
            {
                return new List<RubricItem>
                {
                    new RubricItem { step = 1, description = "Thiết lập phép tính chia: 6,32 : 4", points = 3 },
                    new RubricItem { step = 2, description = "Thực hiện phép tính chia: 6,32 : 4 = 1,58 (m)", points = 4 },
                    new RubricItem { step = 3, description = "Làm tròn đến hàng phần mười: 1,58 ≈ 1,6 (m)", points = 3 }
                };
            }

            // Mặc định tạo Rubric 3 bước chuẩn
            string step3Desc = !string.IsNullOrEmpty(expectedAns) ? $"Kết luận đáp số chính xác: {expectedAns}" : "Kết luận đáp số chính xác";
            return new List<RubricItem>
            {
                new RubricItem { step = 1, description = "Nhận diện bài toán và thiết lập biểu thức / phép tính", points = 4 },
                new RubricItem { step = 2, description = "Thực hiện các bước biến đổi & tính toán theo quy tắc", points = 3 },
                new RubricItem { step = 3, description = step3Desc, points = 3 }
            };
        }

        /// <summary>
        /// Trả về lời giải từng bước hoàn chỉnh để điền vào ô nhập liệu khi học sinh chụp ảnh / tải vở mẫu (OCR).
        /// </summary>
        public static string GetSampleSolution(string prompt, string questionId)
        {
            string p = NormalizePrompt(prompt);
            string qid = (questionId ?? "").ToUpperInvariant();

            // CHƯƠNG 1
            if (qid == "Q_CH1_028" || qid == "ESSAY_ORDER_01" || qid == "ESSAY_001" || p.Contains("15-(3+2)"))
            {
                return "= 15 - 5 × 2\n= 15 - 10\n= 5";
            }
            if (qid == "Q_CH1_029" || p.Contains("23^2+5(2+3)"))
            {
                return "= 2 × 3² + 5 × (2 + 3)\n= 2 × 9 + 5 × 5\n= 18 + 25\n= 43";
            }
            if (qid == "Q_CH1_030" || p.Contains("120+[55-"))
            {
                return "= 120 + [55 - (11 - 3 × 2)²] + 2³\n= 120 + [55 - (11 - 6)²] + 8\n= 120 + [55 - 5²] + 8\n= 120 + [55 - 25] + 8\n= 120 + 30 + 8\n= 158";
            }

            // CHƯƠNG 2: BỘI & ƯỚC, BCNN, ƯCLN
            if (qid == "Q_CH2_028" || p.Contains("máya"))
            {
                return "Số tháng ít nhất để hai máy cùng bảo dưỡng là BCNN(6, 9).\n6 = 2 × 3; 9 = 3²\nBCNN(6, 9) = 2 × 3² = 18\nVậy sau 18 tháng hai máy lại cùng được bảo dưỡng.";
            }
            if (p.Contains("hàng2") || p.Contains("hàng3") || p.Contains("hàng7") || p.Contains("6a") || p.Contains("nhỏhơn45") || (p.Contains("xếphàng") && p.Contains("45")))
            {
                return "Gọi số học sinh của lớp 6A là x (học sinh, x ∈ ℕ*, x < 45).\n" +
                       "Vì khi xếp hàng 2, hàng 3, hàng 7 đều vừa đủ nên:\n" +
                       "x ⋮ 2 ; x ⋮ 3 ; x ⋮ 7\n" +
                       "⇒ x ∈ BC(2, 3, 7)\n" +
                       "Vì 2, 3, 7 là các số nguyên tố cùng nhau đôi một nên:\n" +
                       "BCNN(2, 3, 7) = 2 × 3 × 7 = 42\n" +
                       "⇒ BC(2, 3, 7) = {0; 42; 84; ...}\n" +
                       "Do x < 45 và x > 0 nên x = 42.\n" +
                       "Vậy số học sinh của lớp 6A là 42 học sinh.";
            }
            if (p.Contains("tậphợp") || p.Contains("phầntử"))
            {
                return "a) Liệt kê phần tử: A = {4; 5; 6; 7; 8}\n" +
                       "b) Tính chất đặc trưng: A = {x ∈ ℕ | 3 < x ≤ 8}\n" +
                       "c) Điền ký hiệu: 4 ∈ A ; 9 ∉ A ; 8 ∈ A";
            }
            // Q_CH7_022: Chia đều thanh gỗ 6,32m thành 4 đoạn
            if (qid == "Q_CH7_022" || (p.Contains("6,32") && (p.Contains("bốnđoạn") || p.Contains("thanhgỗ"))))
            {
                return "Độ dài mỗi đoạn gỗ là:\n" +
                       "6,32 : 4 = 1,58 (m)\n" +
                       "Làm tròn kết quả đến hàng phần mười:\n" +
                       "1,58 ≈ 1,6 (m)\n" +
                       "Đáp số: 1,6 m";
            }

            if ((p.Contains("chiađều") && (p.Contains("nhiềunhất") || p.Contains("mỗitổ") || p.Contains("mỗiphần"))) || p.Contains("ướclớnnhất") || p.Contains("ưcln"))
            {
                string uAns = GetAnswer(prompt, questionId);
                return $"Gọi số phần chia được nhiều nhất là a (a ∈ ℕ*).\n" +
                       $"Theo đề bài, a là ƯCLN của các số đã cho.\n" +
                       $"Phân tích ra thừa số nguyên tố và tìm ƯCLN.\n" +
                       $"⇒ a = {(string.IsNullOrEmpty(uAns) ? "12" : uAns)}\n" +
                       $"Đáp số: {(string.IsNullOrEmpty(uAns) ? "12" : uAns)} phần";
            }

            // CHƯƠNG 3: SỐ NGUYÊN
            if (qid == "Q_CH3_012" || p.Contains("mẫusơn"))
            {
                return "Nhiệt độ ban đêm tại đỉnh Mẫu Sơn là:\n(-3) - 5\n= (-3) + (-5)\n= -8 (°C)\nĐáp số: -8 °C";
            }
            if (qid == "Q_CH3_017" || p.Contains("(56-27)-(11+28-16)") || p.Contains("56-27") || (p.Contains("56") && p.Contains("27")))
            {
                return "= 56 - 27 - 11 - 28 + 16\n= (56 + 16) - (27 + 28 + 11)\n= 72 - 66\n= 6";
            }
            if (qid == "Q_CH3_018" || p.Contains("(-25)+48") || p.Contains("-25+48"))
            {
                return "= [(-25) + (-75)] + (48 + 52)\n= (-100) + 100\n= 0";
            }

            // CHƯƠNG 6
            if (qid == "Q_CH6_024" || p.Contains("7/2"))
            {
                return "= 7/2 : 21/10\n= 7/2 × 10/21\n= (7 × 10) / (2 × 21)\n= 70/42\n= 5/3";
            }

            // CHƯƠNG 7
            if (qid == "Q_CH7_030" || (p.Contains("tân") && p.Contains("120") && p.Contains("60%")))
            {
                return "Số người tham gia bỏ phiếu là:\n120 : 60%\n= 120 : (60/100)\n= 120 : 0,6\n= 200 (người)\nĐáp số: 200 người";
            }

            string ans = GetAnswer(prompt, questionId);
            if (!string.IsNullOrEmpty(ans))
            {
                return $"Theo yêu cầu của đề bài, ta có lời giải:\n" +
                       $"Thực hiện biến đổi và tính toán theo quy tắc Toán 6.\n" +
                       $"Kết quả: {ans}\n" +
                       $"Đáp số: {ans}";
            }
            return "";
        }

        /// <summary>
        /// Trả về câu quy tắc toán học cốt lõi để học sinh thực hiện 'Chép Phạt' khi làm sai bước nào.
        /// </summary>
        public static string GetPenaltyRule(string mistakeType, string prompt, string questionId)
        {
            string p = NormalizePrompt(prompt);
            if (mistakeType == "ORDER_OF_OPERATIONS" || p.Contains("15-(3+2)") || p.Contains("3^2") || p.Contains("120+[55-"))
            {
                return "Quy tắc: Thứ tự thực hiện phép tính: Trong ngoặc trước, ngoài ngoặc sau; Nhân chia trước, cộng trừ sau. Với biểu thức 15 - 5 x 2, phải nhân 5 x 2 = 10 trước rồi mới lấy 15 - 10 = 5.";
            }
            if (mistakeType == "SIGN_ERROR" || p.Contains("mẫusơn") || p.Contains("(-25)") || p.Contains("nguyên"))
            {
                return "Quy tắc: Khi trừ hai số nguyên ta cộng với số đối: a - b = a + (-b). Khi cộng hai số nguyên cùng dấu âm, ta cộng hai phần tự nhiên rồi đặt dấu trừ trước kết quả: (-3) + (-5) = -8.";
            }
            if (mistakeType == "FRACTION_DENOMINATOR" || p.Contains("phânsố") || p.Contains("7/2") || p.Contains("quãngđường"))
            {
                return "Quy tắc: Muốn chia hai phân số, ta lấy phân số thứ nhất nhân với phân số đảo ngược của phân số thứ hai: (a/b) : (c/d) = (a/b) x (d/c).";
            }
            if (p.Contains("tân") || p.Contains("60%"))
            {
                return "Quy tắc: Muốn tìm một số biết m/n của nó bằng a, ta tính a : (m/n). Ở đây ta phải lấy 120 : 60% = 120 : 0,6 = 200 người.";
            }
            if (p.Contains("6,32") || p.Contains("làmtròn") || p.Contains("phầnmười"))
            {
                return "Quy tắc làm tròn số thập phân: Để làm tròn số thập phân đến hàng phần mười, xét chữ số ngay sau nó (hàng phần trăm). Nếu chữ số đó ≥ 5 thì cộng thêm 1 vào hàng phần mười. Vì 1,58 có 8 ≥ 5 nên làm tròn thành 1,6.";
            }
            return "Quy tắc: Đọc kỹ đề bài, tính toán cẩn thận từng bước theo đúng thứ tự ưu tiên và kiểm tra lại phép tính trước khi kết luận đáp số.";
        }
    }
}
