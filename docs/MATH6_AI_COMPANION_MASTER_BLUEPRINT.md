# DỰ ÁN MATH6-AI-COMPANION: HỆ THỐNG GAME HỌC TOÁN LỚP 6 TÍCH HỢP GIA SƯ AI
## Kế Hoạch Triển Khai Chi Tiết Cho Nhóm 3 Sinh Viên (8 - 9 Tuần)

---

## 1. ĐÁNH GIÁ PHẠM VI & QUẢN TRỊ RỦI RO (RISK AUDIT)

### 1.1. Các Yêu Cầu Rủi Ro Cao & Giải Pháp Thay Thế Thực Tế
1. **Gọi trực tiếp Google Sheets API v4 từ Unity C#**:
   * *Rủi ro*: Unity thiếu hỗ trợ ổn định cho các thư viện Google Auth/gRPC khi build sang IL2CPP hoặc WebGL; dễ lộ Secret Key trong client.
   * *Giải pháp*: Dùng **Google Apps Script Web App** làm cổng API REST trung gian. Unity chỉ cần dùng `UnityWebRequest` gửi nhận JSON thông qua HTTPS chuẩn.
2. **OCR / Nhận diện chữ viết tay qua hình ảnh**:
   * *Rủi ro*: Chụp ảnh từ camera trong Unity, xử lý độ phân giải, ánh sáng, góc nghiêng và giải mã công thức chữ viết tay Tiếng Việt của học sinh lớp 6 có tỷ lệ lỗi trên 60%.
   * *Giải pháp MVP*: Xây dựng **Bàn phím Toán ảo (Virtual Math Keypad)** trong game và giao diện nhập liệu nhiều bước. Tính năng upload ảnh làm tùy chọn bổ trợ ở giai đoạn sau.
3. **Thế giới 3D / RPG quá rộng lớn**:
   * *Rủi ro*: Thành viên M1 mất toàn bộ thời gian cho modeling, rigging, pathfinding và ánh sáng, không kịp làm UI trắc nghiệm, hộp thoại gia sư và dashboard giáo viên.
   * *Giải pháp MVP*: Giới hạn không gian trong **Một phòng học Toán 3D/2.5D tiêu chuẩn** với 3 điểm tương tác: Bàn học (Làm bài), Bục giảng (Gia sư AI), Bảng tin (Xem tiến độ & Bảng điều khiển giáo viên).
4. **Mất kiểm soát trạng thái Gia sư AI (AI Tutor State Drift)**:
   * *Rủi ro*: LLM dễ "quên" vai trò, trả lời toàn bộ bài toán ngay khi học sinh bảo "Em không biết" hoặc trả lời sai.
   * *Giải pháp*: Áp dụng **State Machine dạng JSON có đánh số thứ tự bước (`currentStepIndex`)**. Mỗi lượt chỉ gửi và nhận thông tin của bước hiện tại.

---

## 2. ĐỊNH NGHĨA MVP TINH GỌN (REVISED MVP)

* **Chủ đề kiến thức Toán 6 chuẩn SGK**:
  1. *Chuyên đề 1: Số nguyên* (Tập hợp Z, cộng trừ nhân chia số nguyên, thứ tự thực hiện phép tính, quy tắc dấu ngoặc).
  2. *Chuyên đề 2: Phân số* (Khái niệm phân số, quy đồng mẫu số, phép toán phân số).
* **Ngân hàng câu hỏi trong Google Sheets**: 30 câu trắc nghiệm (MCQ) có giải thích chi tiết, 10 bài toán tự luận nhiều bước có rubric chấm điểm.
* **Gia sư AI (Socratic AI Tutor)**: Đàm thoại từng bước, không đưa đáp án ngay, phản hồi theo lỗi sai của học sinh, có gợi ý nhớ nhanh.
* **Theo dõi tiến độ & Huy hiệu**: Lưu trữ kết quả, thời gian, số lần thử và tự động cấp huy hiệu theo điều kiện.
* **Bảng điều khiển giáo viên**: Xem danh sách lớp, tỷ lệ hoàn thành, điểm trung bình và phân tích lỗi sai phổ biến.

---

## 3. BẢNG PHÂN CHIA NHIỆM VỤ CHI TIẾT (TASK BREAKDOWN)

> **Quy ước mức độ phụ thuộc:**
> 🔵 **Độc lập (Independent)**: Làm được ngay từ Tuần 1 với Mock Data.
> 🟡 **Cần Hợp đồng Giao diện (Interface Contract)**: Cần chốt khung JSON / Interface C#.
> 🔴 **Cần Tích hợp (Integration)**: Cần ghép nối code của 2 hoặc 3 thành viên.

### Thành viên 1: Gameplay & Giao diện Unity (M1)
| ID | Tên công việc | Ưu tiên | Ước lượng | Phụ thuộc | Độc lập? | Đầu vào | Đầu ra | Định nghĩa hoàn thành (DoD) |
|---|---|---|---|---|---|---|---|---|
| **U-01** | Tạo cảnh phòng học & Di chuyển nhân vật | 🔴 Cao | 3 ngày | Không | 🔵 Có | Asset Store | Player Controller | Học sinh di chuyển (WASD/phím mũi tên), camera bám theo mượt mà |
| **U-02** | Điểm tương tác (Interactable Zones) | 🔴 Cao | 2 ngày | U-01 | 🔵 Có | Collider | Triggers | Hiện nút nhắc [E] khi đến gần Bàn học, Bục giảng, Bảng tin |
| **U-03** | Màn hình Đăng nhập & Menu chính | 🟡 TB | 2 ngày | Không | 🟡 Có | Auth Contract | Login UI | Nhập user/pass, nút Đăng nhập gọi hàm `Login()` |
| **U-04** | Giao diện Làm bài Trắc nghiệm (MCQ) | 🔴 Cao | 3 ngày | Không | 🟡 Có | MockQuestionData | Quiz UI Prefab | Hiển thị câu hỏi, 4 đáp án, đồng hồ bấm giờ, thông báo đúng/sai |
| **U-05** | Bàn phím Toán ảo & Nhập tự luận | 🔴 Cao | 3 ngày | Không | 🔵 Có | Ký hiệu toán | Keypad Prefab | Gõ được phân số, số âm, lũy thừa, dấu ngoặc trên màn hình |
| **U-06** | Hộp thoại Gia sư AI từng bước | 🔴 Cao | 4 ngày | Không | 🟡 Có | MockAIResponse | Tutor Dialog UI | Avatar Giáo viên, bong bóng chat, thanh tiến trình từng bước |
| **U-07** | Màn hình Tiến độ & Huy hiệu | 🟡 TB | 2 ngày | Không | 🟡 Có | MockBadgeData | Badge UI | Danh sách huy hiệu đã mở/khóa, hiệu ứng chúc mừng khi đạt badge |
| **U-08** | Màn hình Bảng điều khiển Giáo viên | 🟡 TB | 3 ngày | Không | 🟡 Có | MockTeacherData | Teacher Screen | Bảng theo dõi điểm số, số lần thử và phân tích lỗi sai học sinh |
| **U-09** | Tích hợp toàn diện & Đóng gói bản build | 🔴 Cao | 5 ngày | U-01 - U-08 | 🔴 Không | API thực (M2, M3) | Bản build game | Thay thế toàn bộ Mock bằng API thật, test không lỗi, build WebGL/PC |

### Thành viên 2: Google Sheets, Dữ liệu & Xác thực (M2)
| ID | Tên công việc | Ưu tiên | Ước lượng | Phụ thuộc | Độc lập? | Đầu vào | Đầu ra | Định nghĩa hoàn thành (DoD) |
|---|---|---|---|---|---|---|---|---|
| **D-01** | Thiết lập Cơ sở dữ liệu Google Sheets | 🔴 Cao | 2 ngày | Không | 🔵 Có | Schema thiết kế | 8 Sheet Tabs | Đủ 8 bảng: Users, Lessons, Questions_Math6, Results, Progress, Badges,... |
| **D-02** | Xây dựng API Đăng nhập (Apps Script) | 🔴 Cao | 2 ngày | D-01 | 🟡 Có | Auth Contract | POST `/login` | Băm mật khẩu SHA-256, kiểm tra sheet `Users`, trả JSON role |
| **D-03** | Xây dựng API Tải câu hỏi | 🔴 Cao | 2 ngày | D-01 | 🟡 Có | Quiz Contract | GET `/questions` | Lọc câu hỏi PUBLISHED theo chủ đề, trả danh sách JSON chuẩn |
| **D-04** | Xây dựng API Lưu kết quả bài làm | 🔴 Cao | 3 ngày | D-01 | 🟡 Có | Result Contract | POST `/saveResult` | Ghi nhận lần thi, tính điểm cao nhất/trung bình, lưu vào `Results` |
| **D-05** | Bộ máy kích hoạt Huy hiệu (Badge Trigger) | 🟡 TB | 2 ngày | D-04 | 🟡 Có | Badge Rules | Trigger Function | Tự động quét điều kiện (ví dụ: điểm 10) và thêm vào `StudentBadges` |
| **D-06** | API Tổng hợp dữ liệu cho Giáo viên | 🟡 TB | 2 ngày | D-01 | 🟡 Có | Dash Contract | GET `/analytics` | Trả về tổng hợp kết quả của lớp và câu hỏi có tỷ lệ sai cao nhất |
| **D-07** | Viết Client C# `GoogleSheetsClient` trong Unity | 🔴 Cao | 3 ngày | D-02 - D-06 | 🔴 Không | `INetworkService` | C# Network Class | Gọi `UnityWebRequest` kết nối Google Apps Script mượt mà |
| **D-08** | Kiểm thử chịu tải & Dự phòng Offline Cache | 🟡 TB | 2 ngày | D-07 | 🔴 Không | Unity Scene | Báo cáo test | Xử lý tốt khi mất mạng đột ngột (lưu tạm kết quả vào `PlayerPrefs`) |

### Thành viên 3: AI Engine, Prompts & Sư Phạm Toán (M3)
| ID | Tên công việc | Ưu tiên | Ước lượng | Phụ thuộc | Độc lập? | Đầu vào | Đầu ra | Định nghĩa hoàn thành (DoD) |
|---|---|---|---|---|---|---|---|---|
| **AI-01** | Thiết kế Prompt Toán 6 chuẩn sư phạm | 🔴 Cao | 3 ngày | Không | 🔵 Có | SGK Toán 6 | Bộ Prompt mẫu | Định hình phong cách đàm thoại Thầy Minh, tuân thủ thứ tự tính toán |
| **AI-02** | Prompt Gia sư AI Socratic từng bước | 🔴 Cao | 3 ngày | AI-01 | 🟡 Có | Tutor Contract | Enforced JSON | AI chỉ hỏi 1 câu/lượt, đợi học sinh, gợi ý khi sai, không đưa đáp án |
| **AI-03** | Script sinh ngân hàng câu hỏi tự động | 🟡 TB | 2 ngày | AI-01 | 🔵 Có | Chuẩn đầu ra | Script Python/Node | Sinh 30 câu trắc nghiệm + 10 tự luận kèm lời giải & biểu điểm chi tiết |
| **AI-04** | Prompt Chấm điểm Tự luận theo Rubric | 🔴 Cao | 3 ngày | AI-01 | 🟡 Có | Grading Contract | Scoring prompt | Nhận diện đúng lỗi tính toán, phân loại mã lỗi và cho điểm từng bước |
| **AI-05** | Viết Client C# `GeminiClient` trong Unity | 🔴 Cao | 3 ngày | AI-02, AI-04 | 🟡 Có | C# Interface | `GeminiClient.cs` | Gửi request đến Gemini API, parse JSON trực tiếp vào Struct C# |
| **AI-06** | Hệ thống phục hồi khi học sinh sai liên tiếp | 🟡 TB | 2 ngày | AI-05 | 🔴 Không | Lịch sử chat | Dynamic Hints | Tự động hạ độ khó hoặc đưa ví dụ số nhỏ hơn khi học sinh bí 2 lần |
| **AI-07** | Thẩm định & Đóng gói ngân hàng câu hỏi | 🔴 Cao | 2 ngày | D-01 | 🔴 Không | Google Sheets | 40 câu PUBLISHED | Kiểm tra độ chuẩn xác 100% của đáp án trước khi nhập vào Sheet chính |

---

## 4. KẾ HOẠCH PHÁT TRIỂN 9 TUẦN (9-WEEK TIMELINE)

* **Tuần 1 - Nền tảng**:
  * M1: Dựng phòng học, di chuyển nhân vật, camera bám.
  * M2: Tạo Google Sheets (8 bảng), đóng băng Hợp đồng Dữ liệu (Data Contract).
  * M3: Thiết kế System Prompt Gia sư Toán 6, đóng băng Hợp đồng JSON AI.
* **Tuần 2 - Giao diện & Kết nối cơ sở**:
  * M1: Tương tác NPC, Menu chính, Đăng nhập (dùng Mock).
  * M2: Code Apps Script API Đăng nhập và Tải câu hỏi.
  * M3: Thử nghiệm Prompt Socratic trên Google AI Studio, viết script sinh câu hỏi.
* **Tuần 3 - Hệ thống Học tập & Luyện tập**:
  * M1: UI Trắc nghiệm (MCQ), chuyển câu hỏi, tính điểm.
  * M2: Hoàn thiện API Lưu kết quả (`saveResult`), logic cộng dồn tiến độ.
  * M3: Thiết kế Prompt chấm tự luận, chuẩn hóa 20 câu hỏi đầu tiên.
* **Tuần 4 - CỘT MỐC QUAN TRỌNG: LAB 1 CHECKPOINT**:
  * *Kiểm tra*: Nhân vật đi lại tương tác trong phòng học. Ngân hàng câu hỏi trên Google Sheets hoạt động, Unity load được câu hỏi từ Sheets hoặc Mock.
* **Tuần 5 - Nhập Tự Luận & Tích Hợp AI**:
  * M1: Bàn phím toán ảo, UI nộp bài tự luận nhiều bước.
  * M2: API Bảng điều khiển giáo viên, tối ưu tốc độ đọc Sheets.
  * M3: Viết `GeminiClient.cs` trong Unity, test gọi API chấm tự luận trực tiếp.
* **Tuần 6 - Gia Sư AI Tương Tác Socratic**:
  * M1: UI Chat đàm thoại gia sư, hiệu ứng gõ chữ, hiển thị gợi ý.
  * M2: Lưu lịch sử phiên học của học sinh vào Google Sheets.
  * M3: Hoàn thiện cỗ máy Gia sư AI từng bước: kiểm tra đáp án -> khen/gợi ý -> bước kế tiếp.
* **Tuần 7 - Huy Hiệu & Quản Lý Giáo Viên**:
  * M1: UI Bảng huy hiệu (Badges), Bảng điều khiển của Giáo viên.
  * M2: Xử lý kích hoạt tự động mở khóa huy hiệu trong Apps Script.
  * M3: Nhập đủ 40 câu hỏi hoàn chỉnh vào Sheet `Questions_Math6` (status PUBLISHED).
* **Tuần 8 - CỘT MỐC QUAN TRỌNG: LAB 2 CHECKPOINT**:
  * *Kiểm tra*: Luồng đầy đủ Học sinh (Học -> Trắc nghiệm -> Tự luận -> Gia sư AI hướng dẫn -> Nhận Huy hiệu). Luồng Giáo viên (Xem thống kê lớp, kết quả từng em).
* **Tuần 9 - Đóng Gói, Tinh Chỉnh & Báo Cáo Cuối Kỳ**:
  * Đồng bộ toàn team: Sửa lỗi giao diện, thêm âm thanh, tối ưu độ trễ AI, chuẩn bị kịch bản demo 5 phút.

---

## 5. KỊCH BẢN DEMO TỔNG KẾT (5 PHÚT TỎA SÁNG)

1. **Phút 0:00 - 0:45**: Học sinh "An" đăng nhập. Cảnh lớp học 3D mở ra, An di chuyển đến Bàn học và chọn bài "Số nguyên".
2. **Phút 0:45 - 2:00**: An hoàn thành nhanh 3 câu trắc nghiệm. Đạt 10/10 điểm tuyệt đối, huy hiệu *"Nhà Tính Toán Hoàn Hảo"* bung ra trên màn hình. Màn hình bên cạnh mở Google Sheet chứng minh dữ liệu vừa ghi nhận tức thì.
3. **Phút 2:00 - 3:45 (Trọng tâm)**: An đến Bục giảng gặp Thầy Minh (Gia sư AI) để giải bài toán: `15 - (3 + 2) * 2`:
   * Bước 1: AI hỏi tính gì trước. An trả lời "Trong ngoặc". AI khen ngợi.
   * Bước 2: AI hỏi kết quả (3 + 2). An cố tình gõ sai "6".
   * Bước 3: AI KHÔNG nói đáp án! AI gợi ý nhẹ nhàng: "Bình tĩnh nào, 3 cộng 2 bằng mấy em?". An gõ lại "5".
   * Bước 4: AI dẫn tiếp sang phép nhân `5 * 2 = 10`, và cuối cùng ra đáp án 5. Hoàn thành bài học!
4. **Phút 3:45 - 4:30**: An nộp bài tập tự luận có lỗi sai thứ tự phép tính. Hệ thống AI chấm điểm, chỉ ra đúng dòng bị sai và giải thích nguyên nhân.
5. **Phút 4:30 - 5:00**: Đăng xuất, đăng nhập tài khoản Cô Mai (Giáo viên). Mở Dashboard xem được ngay kết quả của An, các huy hiệu đạt được và lịch sử lỗi sai cần chú ý.
