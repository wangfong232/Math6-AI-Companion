// Assets/_Project/Scripts/Network/Services/GeminiAITutorService.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
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
        private string primaryModel = "models/gemini-3.5-flash-lite";

        [SerializeField]
        private string fallbackModel = "models/gemini-3.8-flash";

        [SerializeField] private float requestTimeoutSeconds = 15f;
        [SerializeField] private bool fallbackToLocalMockOnError = true;

        public string ApiKey
        {
            get => apiKey;
            set => apiKey = value;
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

        public void SendTutorTurn(TutorTurnPayload payload, Action<bool, AITutorStepResponse, string> callback)
        {
            string systemPrompt = @"Bạn là Thầy Minh - Gia sư AI dạy Toán lớp 6 tâm lý, ân cần và chuẩn sư phạm Việt Nam.
Nhiệm vụ: Hướng dẫn học sinh giải bài toán theo phương pháp Socratic từng bước.
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

            string systemPrompt = @"Bạn là Thầy Minh - Giám khảo & Gia sư AI Toán lớp 6 tâm lý, chuẩn mực sư phạm theo phương pháp SOCRATIC (Gợi Mở).
ĐỀ BÀI: " + payload.prompt + @"
RUBRIC / BƯỚC GIẢI TIÊU CHUẨN:
" + rubricText + @"
BÀI LÀM VĂN BẢN (nếu học sinh có nhập):
""" + payload.studentSteps + @"""

QUY TẮC CHẤM ĐIỂM & HƯỚNG DẪN SƯ PHẠM (SOCRATIC):
1. ĐỐI VỚI ẢNH ĐÍNH KÈM & VĂN BẢN:
   - Quan sát kỹ ảnh bài làm viết tay hoặc văn bản. Nhận diện từng dòng tính toán của học sinh.
   - Nếu ảnh đen, ảnh rỗng hoặc không liên quan đến đề bài: Chấm 0 điểm (score = 0, passed = false, detectedMistakeType = 'UNREADABLE_OR_IRRELEVANT_IMAGE').
2. CHẤM TỪNG BƯỚC (STEP-BY-STEP):
   - Phân tích bài làm đối chiếu với từng bước giải. Bước nào đúng ghi nhận đúng, bước nào sai hoặc thiếu ghi nhận rõ ràng.
3. NGUYÊN TẮC SƯ PHẠM GỢI MỞ (KHÔNG GIẢI HẾT TOÀN BỘ):
   - Tuyệt đối KHÔNG đưa ra toàn bộ lời giải hoàn chỉnh hay đáp số cuối cùng ngay lập tức khi học sinh làm sai.
   - Chỉ ra chính xác học sinh làm đúng đến bước nào, bị nhầm lẫn ở bước nào (ví dụ: nhầm dấu, quên ưu tiên ngoặc, quy đồng sai mẫu).
   - Đưa ra lời khuyên gợi mở, đặt câu hỏi định hướng (Hint) để học sinh tự suy nghĩ và làm lại câu đó thành công!

Trả về duy nhất định dạng JSON thuần túy (không kèm markdown ```json bọc ngoài):
{
  ""score"": 0,
  ""maxScore"": 10,
  ""passed"": false,
  ""detectedMistakeType"": ""NONE"" | ""ORDER_OF_OPERATIONS"" | ""SIGN_ERROR"" | ""FRACTION_DENOMINATOR"" | ""ARITHMETIC"" | ""UNREADABLE_OR_IRRELEVANT_IMAGE"",
  ""stepAnalysis"": [
    {""step"": 1, ""isCorrect"": false, ""comment"": ""Nhận xét ngắn gọn bước 1 (chỉ ra đúng/sai)""},
    {""step"": 2, ""isCorrect"": false, ""comment"": ""Nhận xét ngắn gọn bước 2 (gợi ý nếu sai)""},
    {""step"": 3, ""isCorrect"": false, ""comment"": ""Nhận xét ngắn gọn bước 3""}
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

        #endregion

        #region Gemini API Request Handler

        private IEnumerator CallGeminiGenerateContent(string promptText, Action<bool, string, string> onComplete)
        {
            return CallGeminiGenerateContent(promptText, null, onComplete);
        }

        private IEnumerator CallGeminiGenerateContent(string promptText, string imageBase64, Action<bool, string, string> onComplete)
        {
            // Thử model chính trước
            yield return CallSpecificModel(primaryModel, promptText, imageBase64, (success1, text1, err1) =>
            {
                if (success1)
                {
                    onComplete?.Invoke(true, text1, null);
                }
                else
                {
                    // Thử model dự phòng
                    StartCoroutine(CallSpecificModel(fallbackModel, promptText, imageBase64, (success2, text2, err2) =>
                    {
                        if (success2)
                        {
                            onComplete?.Invoke(true, text2, null);
                        }
                        else
                        {
                            onComplete?.Invoke(false, null, err2 ?? err1);
                        }
                    }));
                }
            });
        }

        private IEnumerator CallSpecificModel(string modelName, string promptText, string imageBase64, Action<bool, string, string> onComplete)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/{modelName}:generateContent?key={apiKey}";

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
