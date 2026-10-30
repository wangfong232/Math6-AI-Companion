// Assets/_Project/Scripts/Network/Services/GeminiAITutorService.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Math6Companion.Core;
using Math6Companion.Core.Contracts;
using Math6Companion.Core.Mock;

namespace Math6Companion.Core.Services
{
    /// <summary>
    /// Module 2: Dịch vụ Gia Sư AI ("Thầy Minh") & Giám khảo Chấm Điểm Tự Luận sử dụng Google Gemini API.
    /// Hỗ trợ phản hồi Socratic từng bước, gợi ý thông minh, nhận diện & chấm điểm bài tập tự luận theo Rubric.
    /// </summary>
    public class GeminiAITutorService : MonoBehaviour, IAITutorService
    {
        private static bool isQuitting = false;
        private static GeminiAITutorService instance;
        public static GeminiAITutorService Instance
        {
            get
            {
                if (isQuitting) return null;
                if (instance == null)
                {
                    instance = FindAnyObjectByType<GeminiAITutorService>();
                    if (instance == null && !isQuitting)
                    {
                        var go = new GameObject("[GeminiAITutorService]");
                        instance = go.AddComponent<GeminiAITutorService>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("Gemini API Configuration")]
        [SerializeField]
        private string apiKey = "";

        [SerializeField]
        private string primaryModel = "models/gemini-3.1-flash-lite";
        public string PrimaryModel => primaryModel;

        [SerializeField]
        private string fallbackModel = "models/gemini-3.1-flash-lite-preview";
        public string FallbackModel => fallbackModel;

        private static readonly string[] modelCascade = new string[]
        {
            "models/gemini-3.1-flash-lite",
            "models/gemini-3-flash-preview",
            "models/gemini-3.1-flash-lite-preview"
        };

        [SerializeField] private float requestTimeoutSeconds = 20f;
        [SerializeField] private bool fallbackToLocalMockOnError = true;

        public string EffectiveApiKey
        {
            get
            {
                string saved = PlayerPrefs.GetString("GEMINI_API_KEY", "").Trim();
                if (!string.IsNullOrEmpty(saved)) return saved;

                if (!string.IsNullOrEmpty(apiKey) && !string.IsNullOrWhiteSpace(apiKey)) 
                    return apiKey.Trim();

                string env = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (!string.IsNullOrEmpty(env)) return env.Trim();

                // Tự động kiểm tra file key.txt hoặc gemini_key.txt
                try
                {
                    string rootPath = System.IO.Directory.GetCurrentDirectory();
                    string f1 = System.IO.Path.Combine(rootPath, "gemini_key.txt");
                    if (System.IO.File.Exists(f1))
                    {
                        string k1 = System.IO.File.ReadAllText(f1).Trim();
                        if (!string.IsNullOrEmpty(k1)) return k1;
                    }

                    string f2 = System.IO.Path.Combine(rootPath, "key.txt");
                    if (System.IO.File.Exists(f2))
                    {
                        string k2 = System.IO.File.ReadAllText(f2).Trim();
                        if (!string.IsNullOrEmpty(k2)) return k2;
                    }

                    string f3 = System.IO.Path.Combine(Application.persistentDataPath, "gemini_key.txt");
                    if (System.IO.File.Exists(f3))
                    {
                        string k3 = System.IO.File.ReadAllText(f3).Trim();
                        if (!string.IsNullOrEmpty(k3)) return k3;
                    }
                }
                catch {}

                return "";
            }
        }

        public string ApiKey
        {
            get => EffectiveApiKey;
            set
            {
                apiKey = (value ?? "").Trim();
                PlayerPrefs.SetString("GEMINI_API_KEY", apiKey);
                PlayerPrefs.Save();
                try
                {
                    string rootPath = System.IO.Directory.GetCurrentDirectory();
                    System.IO.File.WriteAllText(System.IO.Path.Combine(rootPath, "gemini_key.txt"), apiKey);
                }
                catch {}
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                if (string.IsNullOrEmpty(apiKey))
                {
                    apiKey = PlayerPrefs.GetString("GEMINI_API_KEY", Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "");
                }
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        #region IAITutorService Implementation

        private string GetPedagogyGuidance()
        {
            if (TeacherConfigManager.Instance != null)
            {
                string mode = TeacherConfigManager.Instance.AIPedagogyMode;
                if (mode == "ENCOURAGING")
                    return "PHONG CÁCH SƯ PHẠM: Ân cần động viên, khích lệ tâm lý, dùng lời văn ấm áp khen ngợi nỗ lực của học sinh.";
                if (mode == "STRICT_RUBRIC")
                    return "PHONG CÁCH SƯ PHẠM: Nghiêm khắc học thuật, đối chiếu chính xác theo chuẩn barem Bộ GD&ĐT, chỉ ra rõ từng lỗi sai ký hiệu và logic.";
            }
            return "PHONG CÁCH SƯ PHẠM: Socratic (Hỏi gợi mở từng bước, không đưa đáp án ngay, hướng dẫn tư duy tự giải).";
        }

        public void SendTutorTurn(TutorTurnPayload payload, Action<bool, AITutorStepResponse, string> callback)
        {
            string systemPrompt = @"Bạn là Thầy Minh - Gia sư AI dạy Toán lớp 6 tâm lý, ân cần và chuẩn sư phạm Việt Nam.
Nhiệm vụ: " + GetPedagogyGuidance() + @"
Hướng dẫn học sinh giải bài toán từng bước.
ĐẦU VÀO:
- Đề bài toán: " + payload.problemStatement + @"
- Bước hiện tại: " + payload.currentStepIndex + "/" + payload.totalSteps + @"
- Câu trả lời của học sinh: """ + payload.studentInput + @"""

QUY TẮC:
1. Đánh giá câu trả lời của học sinh là đúng (isCorrect = true) hay chưa đúng (isCorrect = false).
2. Viết lời thoại động viên, giải thích ngắn gọn, gợi mở tư duy (tutorDialogue).
3. Cung cấp gợi ý (hint) nếu học sinh làm sai hoặc cần trợ giúp.
4. Trả về đúng định dạng JSON thuần túy (không kèm markdown ```json bọc ngoài):
{
  ""isCorrect"": true,
  ""scoreDelta"": 2,
  ""state"": ""STEP_PASSED"",
  ""tutorDialogue"": ""Chính xác! Ta tính trong ngoặc trước."",
  ""hint"": ""Nhớ quy tắc nhân chia trước cộng trừ sau"",
  ""highlightFormula"": ""15 - 5 * 2"",
  ""nextStepIndex"": 2,
  ""shouldWaitForStudent"": true,
  ""isLessonCompleted"": false
}";

            StartCoroutine(CallGeminiGenerateContent(systemPrompt, (success, rawText, err) =>
            {
                if (success && !string.IsNullOrEmpty(rawText))
                {
                    var parsed = ParseJsonFromText<AITutorStepResponse>(rawText);
                    if (parsed != null)
                    {
                        callback?.Invoke(true, parsed, "Success");
                        return;
                    }
                }

                if (fallbackToLocalMockOnError)
                {
                    Debug.LogWarning("[GeminiAITutorService] Gemini API không phản hồi, dùng Fallback Mock cho Tutor Turn.");
                    MockNetworkService.Instance.SendTutorTurn(payload, callback);
                }
                else
                {
                    callback?.Invoke(false, null, err ?? "Không thể nhận phản hồi từ Gemini AI!");
                }
            }));
        }

        public void GradeEssay(EssaySubmissionPayload payload, Action<bool, AIGradingResponse, string> callback)
        {
            // 0. Tự động bổ sung Rubric chuẩn và Đáp số từ Ngân hàng Kiến thức nếu bị trống từ Google Sheets
            CurriculumEssayKnowledgeBase.EnrichSubmissionPayload(payload);

            string rubricText = "";
            if (payload.rubric != null && payload.rubric.Count > 0)
            {
                foreach (var r in payload.rubric)
                {
                    rubricText += $"- Bước {r.step}: {r.description} ({r.points} điểm)\n";
                }
            }
            else
            {
                rubricText = "(Đề bài không có Rubric cố định. Bạn hãy tự động phân tích bài toán thành 2-3 bước giải tiêu chuẩn của Toán lớp 6 Bộ GD&ĐT và chấm điểm theo từng bước đó)";
            }

            string targetAnsPrompt = !string.IsNullOrEmpty(payload.correctAnswer)
                ? $"ĐÁP SỐ CHUẨN XÁC CẦN ĐẠT CỦA ĐỀ BÀI: {payload.correctAnswer}\n(Nếu học sinh giải đúng các bước ra kết quả này, bắt buộc chấm score = 10, maxScore = 10, passed = true, detectedMistakeType = 'NONE', tất cả các bước stepAnalysis có isCorrect = true)"
                : "(Hãy tự giải toán chính xác để đối chiếu kết quả của học sinh)";

            string systemPrompt = @"Bạn là Thầy Minh - Giám khảo & Gia sư AI Toán lớp 6 tâm lý, chuẩn mực sư phạm theo phương pháp SOCRATIC (Gợi Mở).
ĐỀ BÀI: " + payload.prompt + @"
" + targetAnsPrompt + @"
RUBRIC / BƯỚC GIẢI TIÊU CHUẨN:
" + rubricText + @"
BÀI LÀM VĂN BẢN (nếu học sinh có nhập):
""" + payload.studentSteps + @"""

QUY TẮC CHẤM ĐIỂM & " + GetPedagogyGuidance().ToUpper() + @":
1. ĐỐI VỚI ẢNH ĐÍNH KÈM & VĂN BẢN (KIỂM TRA TÍNH HỢP LỆ CỦA ẢNH):
   - Bạn PHẢI quan sát kỹ nội dung của ảnh:
     + Nếu ảnh là động vật (ví dụ: con bò, chó, mèo...), đồ vật, phong cảnh, chân dung, ảnh rác, ảnh đen, hoặc bất kỳ hình ảnh nào KHÔNG PHẢI là bài giải toán:
       BẮT BUỘC: score = 0, passed = false, detectedMistakeType = 'UNREADABLE_OR_IRRELEVANT_IMAGE'.
       TẤT CẢ các bước trong stepAnalysis đều phải có isCorrect = false, comment: 'Hình ảnh không chứa lời giải toán của đề bài'.
       pedagogicalFeedback: 'Thầy Minh nhận xét: Thầy thấy hình ảnh em tải lên không liên quan đến bài toán (ví dụ ảnh động vật, phong cảnh...) hoặc không nhận diện được chữ viết bài làm. Em hãy chụp đúng trang vở bài tập hoặc gõ lời giải vào ô văn bản nhé!'
     + Nếu ảnh đúng là bài giải viết tay, nhận diện từng dòng chữ và phép tính của học sinh để đối chiếu với Rubric.
2. CHẤM TỪNG DÒNG (LINE-BY-LINE - KHÔNG GIỚI HẠN BƯỚC):
   - Đọc và đối chiếu TỪNG DÒNG của bài làm học sinh (Dòng 1, Dòng 2, Dòng 3, ...).
   - Số phần tử trong stepAnalysis phải tương ứng với số dòng/bước trong bài làm của học sinh (không giới hạn 3 bước).
   - Nếu học sinh giải đúng tất cả các dòng và đạt đúng đáp số: Chấm score = 10, passed = true, detectedMistakeType = 'NONE', tất cả các bước isCorrect = true, firstWrongStepIndex = -1 kèm lời khen ngợi chân thành.
   - Nếu học sinh làm sai hoặc nhầm lẫn:
     + Phân tích đối chiếu từng dòng: Dòng nào đúng ghi nhận đúng (isCorrect = true), dòng nào bắt đầu sai đánh dấu isCorrect = false và giải thích cụ thể lỗi sai.
     + firstWrongStepIndex: Chỉ số dòng đầu tiên bị sai (0 là dòng 1, 1 là dòng 2...).
     + correctStepsPrefix: Chuỗi chứa tất cả các dòng học sinh đã làm đúng trước dòng sai (để giao diện giữ lại cho học sinh giải tiếp từ dòng sai).
     + penaltyRuleToCopy: Câu quy tắc toán học cốt lõi ngắn gọn chuẩn mực mà học sinh đã vi phạm, yêu cầu học sinh chép phạt để ghi nhớ.
3. NGUYÊN TẮC SƯ PHẠM GỢI MỞ (KHÔNG GIẢI HẾT TOÀN BỘ):
   - Tuyệt đối KHÔNG đưa ra toàn bộ lời giải hoàn chỉnh hay đáp số cuối cùng ngay lập tức khi học sinh làm sai.
   - Chỉ ra chính xác học sinh làm đúng đến dòng nào, sai ở dòng nào.
   - Đưa ra lời khuyên gợi mở, đặt câu hỏi định hướng (Hint) để học sinh tự suy nghĩ và sửa tiếp từ dòng đó thành công!
4. ĐỊNH DẠNG COMMENT:
   - Trong mảng stepAnalysis, trường 'comment' chỉ ghi nội dung nhận xét thuần túy, TUYỆT ĐỐI KHÔNG thêm tiền tố [Đúng] hoặc [Chú ý] vào đầu comment (giao diện hệ thống sẽ tự động hiển thị biểu tượng này).

Trả về duy nhất định dạng JSON thuần túy (không kèm markdown ```json bọc ngoài):
{
  ""score"": 0,
  ""maxScore"": 10,
  ""passed"": false,
  ""detectedMistakeType"": ""NONE"" | ""ORDER_OF_OPERATIONS"" | ""SIGN_ERROR"" | ""FRACTION_DENOMINATOR"" | ""ARITHMETIC"" | ""UNREADABLE_OR_IRRELEVANT_IMAGE"",
  ""firstWrongStepIndex"": -1,
  ""correctStepsPrefix"": ""các dòng đúng trước bước sai"",
  ""penaltyRuleToCopy"": ""Câu quy tắc toán học chuẩn để học sinh chép phạt ghi nhớ"",
  ""stepAnalysis"": [
    {""step"": 1, ""isCorrect"": false, ""comment"": ""Nhận xét ngắn gọn dòng 1""},
    {""step"": 2, ""isCorrect"": false, ""comment"": ""Nhận xét ngắn gọn dòng 2""}
  ],
  ""pedagogicalFeedback"": ""Lời nhận xét gợi mở sư phạm của Thầy Minh, chỉ ra điểm nghẽn tư duy và gợi ý hướng sửa để học sinh tự giải quyết."",
  ""remedialExercise"": ""Một bài tập tương tự ngắn gọn cùng dạng để học sinh tự rèn luyện.""
}";

            StartCoroutine(CallGeminiGenerateContent(systemPrompt, payload.imageBase64, (success, rawText, err) =>
            {
                if (success && !string.IsNullOrEmpty(rawText))
                {
                    var parsed = ParseJsonFromText<AIGradingResponse>(rawText);
                    if (parsed != null)
                    {
                        if (string.IsNullOrEmpty(parsed.penaltyRuleToCopy) && !parsed.passed)
                        {
                            parsed.penaltyRuleToCopy = CurriculumEssayKnowledgeBase.GetPenaltyRule(parsed.detectedMistakeType, payload.prompt, payload.questionId);
                        }
                        callback?.Invoke(true, parsed, "Success");
                        return;
                    }
                }

                if (fallbackToLocalMockOnError)
                {
                    Debug.LogWarning("[GeminiAITutorService] Gemini API không phản hồi, dùng Fallback Mock cho GradeEssay.");
                    MockNetworkService.Instance.GradeEssay(payload, callback);
                }
                else
                {
                    callback?.Invoke(false, null, err ?? "Không thể kết nối Gemini AI Chấm Điểm!");
                }
            }));
        }

        public void ExtractTextFromImage(string imageBase64, Action<bool, string, string> callback)
        {
            if (string.IsNullOrEmpty(imageBase64))
            {
                callback?.Invoke(false, "", "Ảnh rỗng");
                return;
            }

            string currentKey = EffectiveApiKey;
            if (string.IsNullOrEmpty(currentKey))
            {
                if (fallbackToLocalMockOnError)
                {
                    Debug.LogWarning("[GeminiAITutor] Chưa có GEMINI_API_KEY, tự động chuyển sang Fallback Mock OCR.");
                    MockNetworkService.Instance.ExtractTextFromImage(imageBase64, callback);
                    return;
                }
                callback?.Invoke(false, "", "Chưa cấu hình GEMINI_API_KEY");
                return;
            }

            string ocrPrompt = @"Bạn là hệ thống AI OCR chuyên sâu về nhận diện chữ viết tay học sinh và bài giải toán học.
Nhiệm vụ: Phân tích hình ảnh bài làm toán học và trích xuất nguyên văn các bước giải toán học sinh đã viết.

QUY TẮC NHẬN DIỆN NGHIÊM NGẶT:
1. ĐA CHIỀU & GÓC CHỤP: Tự động phát hiện và đọc đúng chiều dù ảnh bị chụp ngược 180 độ, xoay nghiêng 90 độ, méo góc hoặc giấy nhăn gấp.
2. CHỮ VIẾT TAY & CHỮ XẤU: Nhận diện chính xác chữ viết tay học sinh lớp 6, kể cả nét chữ xấu, nguệch ngoạc, nét bút bi/bút chì mờ.
3. PHÂN ĐỊNH ẢNH HỢP LỆ (BẮT BUỘC):
   - Nếu ảnh là phong cảnh thiên nhiên, làng quê, động vật (chó, mèo, bò...), chân dung người, đồ vật, màn hình trống, hoặc KHÔNG CHỨA BÀI LÀM TOÁN:
     BẮT BUỘC chỉ trả về duy nhất chuỗi: [KHÔNG_CÓ_BÀI_LÀM_TOÁN]
4. ĐỊNH DẠNG ĐẦU RA:
   - Nếu có bài làm toán: Chỉ trả về nội dung toán học được trích xuất từ ảnh, mỗi bước/dòng phép tính trên một dòng riêng biệt theo đúng thứ tự giải từ trên xuống dưới.
   - Giữ nguyên các phép tính, ký hiệu toán học (=, +, -, ×, :, /, phân số a/b, luỹ thừa, ngoặc, số âm).
   - Tuyệt đối không thêm lời giải thích, không thêm nhận xét, không thêm markdown ```.";

            StartCoroutine(CallGeminiGenerateContent(ocrPrompt, imageBase64, (success, rawText, err) =>
            {
                if (success && !string.IsNullOrEmpty(rawText))
                {
                    string cleaned = rawText.Trim();
                    if (cleaned.StartsWith("```"))
                    {
                        int firstNl = cleaned.IndexOf('\n');
                        if (firstNl >= 0) cleaned = cleaned.Substring(firstNl + 1);
                        if (cleaned.EndsWith("```")) cleaned = cleaned.Substring(0, cleaned.Length - 3).Trim();
                    }

                    if (cleaned.Contains("[KHÔNG_CÓ_BÀI_LÀM_TOÁN]") || cleaned.Contains("KHÔNG_CÓ_BÀI_LÀM_TOÁN"))
                    {
                        callback?.Invoke(false, "", "Ảnh không chứa chữ viết tay hoặc bài làm toán học hợp lệ (Ảnh chụp phong cảnh/con vật hoặc không có phép tính)!");
                        return;
                    }

                    callback?.Invoke(true, cleaned, "Success");
                }
                else
                {
                    if (string.IsNullOrEmpty(EffectiveApiKey) && fallbackToLocalMockOnError)
                    {
                        Debug.LogWarning("[GeminiAITutor] Chưa có GEMINI_API_KEY, tự động chuyển sang Fallback Mock OCR.");
                        MockNetworkService.Instance.ExtractTextFromImage(imageBase64, callback);
                        return;
                    }
                    callback?.Invoke(false, "", err ?? "Không nhận diện được chữ từ ảnh");
                }
            }));
        }

        #endregion

        #region Gemini API Request Handler

        private IEnumerator CallGeminiGenerateContent(string promptText, Action<bool, string, string> onComplete)
        {
            return CallGeminiGenerateContent(promptText, null, onComplete);
        }

        private IEnumerator CallGeminiGenerateContent(string promptText, string imageBase64, Action<bool, string, string> onComplete)
        {
            string currentKey = EffectiveApiKey;
            if (string.IsNullOrEmpty(currentKey))
            {
                onComplete?.Invoke(false, null, "Chưa cấu hình GEMINI_API_KEY");
                yield break;
            }

            yield return TryModelsInSequence(0, promptText, imageBase64, currentKey, onComplete);
        }

        private IEnumerator TryModelsInSequence(int modelIndex, string promptText, string imageBase64, string currentKey, Action<bool, string, string> onComplete)
        {
            if (modelIndex >= modelCascade.Length)
            {
                onComplete?.Invoke(false, null, "Không có model Gemini nào phản hồi khả dụng.");
                yield break;
            }

            string modelName = modelCascade[modelIndex];
            yield return CallSpecificModel(modelName, promptText, imageBase64, currentKey, (success, result, err) =>
            {
                if (success)
                {
                    onComplete?.Invoke(true, result, null);
                }
                else
                {
                    Debug.LogWarning($"[GeminiAITutor] Model {modelName} không phản hồi ({err}). Thử model tiếp theo...");
                    StartCoroutine(TryModelsInSequence(modelIndex + 1, promptText, imageBase64, currentKey, onComplete));
                }
            });
        }

        private IEnumerator CallSpecificModel(string modelName, string promptText, string imageBase64, string currentKey, Action<bool, string, string> onComplete)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/{modelName}:generateContent?key={currentKey}";

            string jsonPayload;
            if (!string.IsNullOrEmpty(imageBase64))
            {
                // Escape text for valid JSON
                string escapedPrompt = promptText.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
                jsonPayload = "{\"contents\":[{\"parts\":[{\"text\":\"" + escapedPrompt + "\"},{\"inlineData\":{\"mimeType\":\"image/jpeg\",\"data\":\"" + imageBase64 + "\"}}]}]}";
            }
            else
            {
                var reqBody = new GeminiRequest
                {
                    contents = new List<GeminiContent>
                    {
                        new GeminiContent
                        {
                            parts = new List<GeminiPart>
                            {
                                new GeminiPart { text = promptText }
                            }
                        }
                    }
                };
                jsonPayload = JsonUtility.ToJson(reqBody);
            }

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = Mathf.RoundToInt(requestTimeoutSeconds);

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string resText = req.downloadHandler.text;
                    try
                    {
                        var geminiResp = JsonUtility.FromJson<GeminiResponse>(resText);
                        if (geminiResp != null && geminiResp.candidates != null && geminiResp.candidates.Count > 0)
                        {
                            var parts = geminiResp.candidates[0].content.parts;
                            if (parts != null && parts.Count > 0)
                            {
                                string output = parts[0].text;
                                onComplete?.Invoke(true, output, null);
                                yield break;
                            }
                        }
                        onComplete?.Invoke(false, null, "Không có nội dung trả về từ Gemini.");
                    }
                    catch (Exception ex)
                    {
                        onComplete?.Invoke(false, null, $"Lỗi parse phản hồi Gemini: {ex.Message}");
                    }
                }
                else
                {
                    onComplete?.Invoke(false, null, $"Gemini API Lỗi ({req.responseCode}): {req.error}");
                }
            }
        }

        private T ParseJsonFromText<T>(string raw) where T : class
        {
            try
            {
                // Làm sạch markdown nếu có ```json ... ```
                string cleaned = raw.Trim();
                if (cleaned.StartsWith("```json"))
                {
                    cleaned = cleaned.Substring(7);
                }
                else if (cleaned.StartsWith("```"))
                {
                    cleaned = cleaned.Substring(3);
                }

                if (cleaned.EndsWith("```"))
                {
                    cleaned = cleaned.Substring(0, cleaned.Length - 3);
                }

                cleaned = cleaned.Trim();
                return JsonUtility.FromJson<T>(cleaned);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GeminiAITutorService] Lỗi trích xuất JSON: {ex.Message} -> Raw: {raw}");
                return null;
            }
        }

        #endregion

        #region Gemini DTO Structures

        [Serializable]
        private class GeminiPart
        {
            public string text;
        }

        [Serializable]
        private class GeminiContent
        {
            public List<GeminiPart> parts;
        }

        [Serializable]
        private class GeminiRequest
        {
            public List<GeminiContent> contents;
        }

        [Serializable]
        private class GeminiCandidate
        {
            public GeminiContent content;
        }

        [Serializable]
        private class GeminiResponse
        {
            public List<GeminiCandidate> candidates;
        }

        #endregion
    }
}
