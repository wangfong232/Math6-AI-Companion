// Assets/_Project/Scripts/Network/Mock/MockNetworkService.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Math6Companion.Core.Contracts;
using Math6Companion.UI;

namespace Math6Companion.Core.Mock
{
    public class MockNetworkService : MonoBehaviour, INetworkService, IAITutorService
    {
        private static bool isQuitting = false;
        private static MockNetworkService instance;
        public static MockNetworkService Instance
        {
            get
            {
                if (isQuitting) return null;
                if (instance == null)
                {
                    instance = FindAnyObjectByType<MockNetworkService>();
                    if (instance == null && !isQuitting)
                    {
                        var go = new GameObject("[MockNetworkService]");
                        instance = go.AddComponent<MockNetworkService>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        private void Awake()
        {
            if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
            else if (instance != this) { Destroy(gameObject); }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        #region INetworkService Mock
        public void Login(string username, string passwordHash, Action<bool, UserData, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                if (username.ToLower().StartsWith("teacher") || username == "teacher01" || username == "teacher_01")
                {
                    callback(true, new UserData
                    {
                        userId = "TEA_001",
                        username = username,
                        fullName = "Cô Mai Toán",
                        role = "TEACHER",
                        grade = 6,
                        classId = "6A1",
                        nickname = "Cô Mai"
                    }, "Success");
                }
                else
                {
                    callback(true, new UserData
                    {
                        userId = "STU_001",
                        username = username,
                        fullName = "Em An",
                        role = "STUDENT",
                        grade = 6,
                        classId = "6A1",
                        nickname = "AnThầnĐồng"
                    }, "Success");
                }
            }));
        }

        public void GetLessons(string topic, Action<bool, List<LessonData>, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                var lessons = new List<LessonData>
                {
                    new LessonData
                    {
                        lessonId = "LES_INT_01",
                        topic = "Integers",
                        title = "Bài 1: Phép cộng và trừ số nguyên",
                        description = "Quy tắc cộng trừ số nguyên cùng dấu và khác dấu",
                        grade = 6,
                        summaryText = "1. Cộng hai số nguyên cùng dấu:\n- Cộng hai phần tự nhiên rồi đặt dấu chung trước kết quả.\n\n2. Cộng hai số nguyên khác dấu:\n- Lấy số có phần tự nhiên lớn hơn trừ số có phần tự nhiên bé hơn, đặt dấu của số có phần tự nhiên lớn hơn trước hiệu tìm được.",
                        videoUrl = "https://www.youtube.com/watch?v=vcR0jey6xcI",
                        userHighestScore = 10,
                        topStudentNickname = "BìnhToánHọc",
                        topStudentAttempts = 16
                    },
                    new LessonData
                    {
                        lessonId = "LES_INT_02",
                        topic = "Integers",
                        title = "Bài 2: Phép nhân và chia hai số nguyên",
                        description = "Quy tắc nhân chia số nguyên, nhân khác dấu và cùng dấu",
                        grade = 6,
                        summaryText = "1. Nhân hai số nguyên cùng dấu: Kết quả luôn là số nguyên DƯƠNG (+).\n2. Nhân hai số nguyên khác dấu: Kết quả luôn là số nguyên ÂM (-).\nQuy tắc dấu: (+) * (+) = (+), (-) * (-) = (+), (+) * (-) = (-), (-) * (+) = (-).",
                        videoUrl = "https://www.youtube.com/watch?v=mYyUEaSUcu4",
                        userHighestScore = 8,
                        topStudentNickname = "ChiThầnĐồng",
                        topStudentAttempts = 12
                    },
                    new LessonData
                    {
                        lessonId = "LES_FRAC_01",
                        topic = "Fractions",
                        title = "Bài 3: Khái niệm phân số & Rút gọn phân số",
                        description = "Định nghĩa phân số, tử và mẫu, quy tắc rút gọn tối giản",
                        grade = 6,
                        summaryText = "Phân số có dạng a/b với a, b thuộc Z, b khác 0. a là tử số, b là mẫu số.\nRút gọn phân số: Chia cả tử và mẫu cho ước chung lớn hơn 1 để được phân số tối giản.",
                        videoUrl = "https://www.youtube.com/watch?v=ZKNbig3XpM8",
                        userHighestScore = 9,
                        topStudentNickname = "AnThầnĐồng",
                        topStudentAttempts = 14
                    }
                };
                callback(true, lessons, "Success");
            }));
        }

        public void GetQuestions(string topic, string grade, Action<bool, List<QuestionData>, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                var list = new List<QuestionData>
                {
                    new QuestionData
                    {
                        questionId = "Q_INT_001",
                        lessonId = "LES_INT_01",
                        topic = "Integers",
                        type = "MCQ",
                        difficulty = "EASY",
                        prompt = "Tính giá trị biểu thức: (-15) + 7",
                        options = new QuestionOptionMap { A = "8", B = "-8", C = "22", D = "-22" },
                        correctAnswer = "B",
                        explanation = "Cộng hai số khác dấu: 15 - 7 = 8, mang dấu của -15 nên kết quả là -8."
                    },
                    new QuestionData
                    {
                        questionId = "Q_INT_002",
                        lessonId = "LES_INT_01",
                        topic = "Integers",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "Kết quả của phép tính: (-4) * (-5) là:",
                        options = new QuestionOptionMap { A = "-20", B = "20", C = "-9", D = "9" },
                        correctAnswer = "B",
                        explanation = "Tích của hai số nguyên cùng dấu là một số nguyên dương: (-4) * (-5) = 20."
                    }
                };
                callback(true, list, "Success");
            }));
        }

        public void SaveResult(ResultSubmission submission, Action<bool, List<BadgeData>, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                var badges = new List<BadgeData>();
                if (submission.score == submission.maxScore)
                {
                    badges.Add(new BadgeData
                    {
                        badgeId = "BADGE_PERFECT",
                        badgeCode = "PERFECT_SCORE",
                        name = "Nhà Tính Toán Hoàn Hảo",
                        description = "Đạt điểm tối đa trong bài kiểm tra",
                        iconUrl = "icon_badge_perfect"
                    });
                }
                callback(true, badges, "Success");
            }));
        }

        public void LogAITutor(AITutorLogSubmission logSubmission, Action<bool, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                Debug.Log($"[MockNetworkService] AI Tutor Log saved: {logSubmission?.sessionType} | Student: {logSubmission?.studentAnswer} | Feedback: {logSubmission?.aiFeedback}");
                callback?.Invoke(true, "Ghi nhận AI Tutor log (Mock) thành công!");
            }));
        }

        public void GetStudentAnalytics(string studentId, Action<bool, StudentAnalyticsData, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                callback(true, new StudentAnalyticsData
                {
                    userId = studentId,
                    fullName = "Em An",
                    completedLessons = 4,
                    averageScore = 9.5f,
                    badges = new List<BadgeData>
                    {
                        new BadgeData { badgeCode = "FIRST_LESSON", name = "Khởi Đầu Nan" }
                    }
                }, "Success");
            }));
        }
        #endregion

        #region IAITutorService Mock
        public void SendTutorTurn(TutorTurnPayload payload, Action<bool, AITutorStepResponse, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                // Simple deterministic mock responses for 15 - (3 + 2) * 2
                var input = payload.studentInput.Trim().ToLower();
                if (payload.currentStepIndex == 1)
                {
                    if (input.Contains("ngoặc") || input.Contains("parentheses") || input.Contains("3+2") || input.Contains("5"))
                    {
                        callback(true, new AITutorStepResponse
                        {
                            isCorrect = true,
                            state = "STEP_PASSED",
                            tutorDialogue = "Chính xác! Ta ưu tiên tính trong ngoặc (3 + 2) trước. Kết quả (3 + 2) bằng mấy?",
                            hint = "Phép cộng đơn giản 3 + 2",
                            highlightFormula = "(3 + 2) = ?",
                            nextStepIndex = 2,
                            shouldWaitForStudent = true
                        }, "Success");
                    }
                    else
                    {
                        callback(true, new AITutorStepResponse
                        {
                            isCorrect = false,
                            state = "WAITING_INPUT",
                            tutorDialogue = "Chưa đúng rồi! Hãy nhớ quy tắc thứ tự phép tính: trong ngoặc hay ngoài ngoặc làm trước?",
                            hint = "Nhìn vào dấu ngoặc tròn nhé.",
                            nextStepIndex = 1,
                            shouldWaitForStudent = true
                        }, "Success");
                    }
                }
                else if (payload.currentStepIndex == 2)
                {
                    if (input == "5")
                    {
                        callback(true, new AITutorStepResponse
                        {
                            isCorrect = true,
                            state = "STEP_PASSED",
                            tutorDialogue = "Rất tốt! Biểu thức trở thành 15 - 5 * 2. Bây giờ ta thực hiện phép trừ hay phép nhân trước?",
                            hint = "Nhân chia trước, cộng trừ sau.",
                            highlightFormula = "15 - 5 * 2",
                            nextStepIndex = 3,
                            shouldWaitForStudent = true
                        }, "Success");
                    }
                    else
                    {
                        callback(true, new AITutorStepResponse
                        {
                            isCorrect = false,
                            state = "WAITING_INPUT",
                            tutorDialogue = "Tính lại giúp thầy: 3 + 2 bằng mấy nào?",
                            hint = "3 cộng thêm 2 đơn vị.",
                            nextStepIndex = 2,
                            shouldWaitForStudent = true
                        }, "Success");
                    }
                }
                else
                {
                    callback(true, new AITutorStepResponse
                    {
                        isCorrect = true,
                        state = "TUTOR_COMPLETED",
                        tutorDialogue = "Xuất sắc! Em đã hoàn thành bài toán từng bước một cách độc lập!",
                        isLessonCompleted = true
                    }, "Success");
                }
            }));
        }

        public void GradeEssay(EssaySubmissionPayload payload, Action<bool, AIGradingResponse, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                bool hasImage = !string.IsNullOrEmpty(payload.imageBase64);
                string text = (payload.studentSteps ?? "").Trim();
                bool hasText = !string.IsNullOrEmpty(text) && !text.Contains("(Không có bài làm)") && !text.Contains("(Học sinh không nhập văn bản");

                if (!hasImage && !hasText)
                {
                    callback(true, new AIGradingResponse
                    {
                        score = 0,
                        maxScore = 10,
                        passed = false,
                        detectedMistakeType = "UNREADABLE_OR_IRRELEVANT_IMAGE",
                        pedagogicalFeedback = "Học sinh chưa cung cấp bài giải hoặc ảnh bài làm không hợp lệ.",
                        remedialExercise = "Em hãy nhập lời giải hoặc tải ảnh bài làm lên để Thầy Minh nhận xét nhé.",
                        stepAnalysis = new List<StepAnalysisItem>
                        {
                            new StepAnalysisItem { step = 1, isCorrect = false, comment = "Chưa có lời giải cho câu hỏi này" }
                        }
                    }, "Success");
                    return;
                }

                AIGradingResponse result = EvaluateEssayLocally(payload, hasImage, text);
                callback(true, result, "Success");
            }));
        }

        public void ExtractTextFromImage(string imageBase64, Action<bool, string, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                if (string.IsNullOrEmpty(imageBase64))
                {
                    callback?.Invoke(false, "", "Ảnh rỗng");
                    return;
                }

                // Phân tích hình ảnh cục bộ: Phân biệt ảnh tài liệu/trang vở vs ảnh phong cảnh, con vật, đồ vật
                bool isMathDocument = false;
                bool isSampleExponentPhoto = false;

                try
                {
                    byte[] bytes = System.Convert.FromBase64String(imageBase64);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(bytes))
                    {
                        // Kiểm tra nếu là file sample_math_essay.png (bài nháp luỹ thừa bị chụp ngược 180 độ)
                        string samplePath = System.IO.Path.Combine(Application.dataPath, "_Project/Textures/sample_math_essay.png");
                        if (System.IO.File.Exists(samplePath))
                        {
                            byte[] sampleBytes = System.IO.File.ReadAllBytes(samplePath);
                            if (Math.Abs(bytes.Length - sampleBytes.Length) < 150)
                            {
                                isSampleExponentPhoto = true;
                                isMathDocument = true;
                            }
                        }

                        if (!isSampleExponentPhoto)
                        {
                            // Lấy mẫu các điểm ảnh để đo độ sáng trung bình và độ bão hòa màu sắc
                            int w = tex.width;
                            int h = tex.height;
                            int sampleCount = 50;
                            float totalLum = 0f;
                            float totalSat = 0f;

                            for (int i = 0; i < sampleCount; i++)
                            {
                                int rx = Mathf.RoundToInt((float)i / sampleCount * (w - 1));
                                int ry = Mathf.RoundToInt((float)i / sampleCount * (h - 1));
                                Color clr = tex.GetPixel(rx, ry);

                                float max = Mathf.Max(clr.r, Mathf.Max(clr.g, clr.b));
                                float min = Mathf.Min(clr.r, Mathf.Min(clr.g, clr.b));
                                float sat = max > 0.001f ? (max - min) / max : 0f;
                                float lum = 0.299f * clr.r + 0.587f * clr.g + 0.114f * clr.b;

                                totalLum += lum;
                                totalSat += sat;
                            }

                            float avgLum = totalLum / sampleCount;
                            float avgSat = totalSat / sampleCount;

                            // Giấy tập học sinh luôn có nền sáng (lum >= 0.40) và độ bão hòa thấp (sat <= 0.35)
                            // Ảnh phong cảnh làng quê ban đêm, con vật, cỏ cây, màn hình game có lum thấp hoặc sat rất cao
                            if (avgLum >= 0.38f && avgSat <= 0.35f)
                            {
                                isMathDocument = true;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[MockOCR] Lỗi phân tích ảnh: " + ex.Message);
                }

                // TRƯỜNG HỢP 1: Ảnh không phải là tài liệu vở bài tập (ảnh phong cảnh, con vật, đồ vật...)
                if (!isMathDocument)
                {
                    callback?.Invoke(false, "", "Ảnh không chứa chữ viết tay hoặc bài làm toán học hợp lệ (Hình ảnh phong cảnh / con vật / đồ vật hoặc không có phép tính)!");
                    return;
                }

                // TRƯỜNG HỢP 2: Là ảnh chụp bài làm nháp sample_math_essay.png (bị chụp ngược 180 độ)
                // OCR tự động xoay đúng chiều và trích xuất nguyên văn phép tính ghi trên ảnh đó
                if (isSampleExponentPhoto)
                {
                    string sampleExp = "= 2 × 3² + 5 × (2 + 3)\n= 2 × 9 + 5 × 5\n= 18 + 25\n= 43";
                    callback?.Invoke(true, sampleExp, "Success");
                    return;
                }

                // TRƯỜNG HỢP 3: Ảnh giấy tập nhưng đang ở chế độ Offline (chưa có Gemini API Key)
                callback?.Invoke(false, "", "Chưa cấu hình GEMINI_API_KEY để OCR ảnh chữ viết tay tự do. Vui lòng bấm [🔑 Cấu hình Gemini Key] hoặc tự gõ bài giải vào ô văn bản!");
            }));
        }

        private AIGradingResponse EvaluateEssayLocally(EssaySubmissionPayload payload, bool hasImage, string text)
        {
            // 0. Tu dong bo sung Rubric chuan va Dap so tu Ngan hang Kien thuc neu Google Sheet bi trong
            CurriculumEssayKnowledgeBase.EnrichSubmissionPayload(payload);

            string prompt = payload.prompt ?? "";
            string promptLower = prompt.ToLower();
            string textLower = (text ?? "").ToLower();

            // 1. Xac dinh dap so ky vong — kiem tra ca correctAnswer va rubric buoc cuoi
            string rubricTarget = "";
            if (payload.rubric != null && payload.rubric.Count > 0)
            {
                var lastStep = payload.rubric[payload.rubric.Count - 1];
                rubricTarget = ExtractFinalAnswer(lastStep.description);
            }

            string expectedTarget = "";
            if (!string.IsNullOrEmpty(payload.correctAnswer))
            {
                string cAns = payload.correctAnswer.Trim();
                // Neu rubric co phep tinh ro rang (vd: "120 : 0,6 = 200") ma correctAnswer lai ghi "120" (toan hang ve trai)
                // thi tu dong sua expectedTarget ve "200" de khong cham sai cau hoi!
                if (!string.IsNullOrEmpty(rubricTarget) && rubricTarget != cAns)
                {
                    var lastStep = payload.rubric[payload.rubric.Count - 1];
                    int eqIndex = lastStep.description.LastIndexOf('=');
                    if (eqIndex > 0 && lastStep.description.Substring(0, eqIndex).Contains(cAns))
                    {
                        Debug.LogWarning($"[MockGrading] Phat hien correctAnswer='{cAns}' la toan hang truoc dau '=' trong rubric '{lastStep.description}'. Uu tien dap so thuc su='{rubricTarget}'!");
                        expectedTarget = rubricTarget;
                    }
                    else
                    {
                        expectedTarget = cAns;
                    }
                }
                else
                {
                    expectedTarget = cAns;
                }
            }
            else if (!string.IsNullOrEmpty(rubricTarget))
            {
                expectedTarget = rubricTarget;
            }

            // Fallback cuoi cung: tra cuu tu Ngan hang Kien thuc chuan Toan 6
            if (string.IsNullOrEmpty(expectedTarget))
            {
                expectedTarget = CurriculumEssayKnowledgeBase.GetAnswer(prompt, payload.questionId);
            }

            Debug.Log($"[MockGrading] Prompt='{GetShortPrompt(prompt)}' | correctAnswer='{payload.correctAnswer}' | rubricTarget='{rubricTarget}' | expectedTarget='{expectedTarget}' | studentText='{(text != null && text.Length > 80 ? text.Substring(0, 80) : text)}'");

            bool isSampleNotebook = text != null && (text.Contains("Vở bài tập mẫu") || text.Contains("Vo bai tap mau"));

            bool hasRealText = !string.IsNullOrEmpty(text) && 
                               !text.Contains("(Khong co bai lam)") && 
                               !text.Contains("(Hoc sinh khong nhap van ban") &&
                               !text.Contains("(Học sinh không nhập văn bản") &&
                               !text.Contains("(Không có bài làm)") &&
                               !isSampleNotebook;

            var resp = new AIGradingResponse();
            resp.maxScore = 10;
            resp.stepAnalysis = new List<StepAnalysisItem>();

            List<RubricItem> rubrics = payload.rubric != null && payload.rubric.Count > 0 ? payload.rubric : new List<RubricItem>
            {
                new RubricItem { step = 1, description = "Nhan dien bai toan va thiet lap phep tinh", points = 4 },
                new RubricItem { step = 2, description = "Thuc hien cac buoc tinh toan theo quy tac", points = 3 },
                new RubricItem { step = 3, description = string.IsNullOrEmpty(expectedTarget) ? "Ket luan dap so" : $"Ket luan dap so chinh xac ({expectedTarget})", points = 3 }
            };

            // TRUONG HOP 1: Hoc sinh nop anh (vi du anh con bo, phong canh...) nhung KHONG co van ban giai toan va KHONG phai vo mau:
            // O che do Mo phong Mock (chua ket noi Gemini Vision truc tiep):
            if (hasImage && !hasRealText && !isSampleNotebook)
            {
                // Neu anh khong chua du lieu van ban toan hoc hop le, cham 0 diem va bao ro rang:
                resp.score = 0;
                resp.passed = false;
                resp.detectedMistakeType = "UNREADABLE_OR_IRRELEVANT_IMAGE";
                resp.pedagogicalFeedback = "Thay Minh nhan xet: Hinh anh em tai len khong nhan dien duoc cac buoc giai toan hop le (hinh anh khong ro net hoac khong lien quan den bai toan). Em hay chup dung vo bai lam hoac go loi giai vao o van ban nhe!\n\n(Luu y: De AI truc tiep 'nhin' va doc hinh anh bai lam cua em qua Gemini Vision, vui long cau hinh GEMINI_API_KEY trong Bang dieu khien Giao vien).";
                resp.remedialExercise = "Em hay bam 'GIAI LAI CAU NAY' va go tung buoc giai hoac chup anh ro net bai lam.";

                for (int i = 0; i < rubrics.Count; i++)
                {
                    var r = rubrics[i];
                    resp.stepAnalysis.Add(new StepAnalysisItem
                    {
                        step = r.step > 0 ? r.step : (i + 1),
                        isCorrect = false,
                        comment = $"Khong tim thay buoc giai toan hop le tren anh: {r.description}"
                    });
                }
                return resp;
            }

            // 2. Kiem tra xem hoc sinh co giai dung hay khong qua van ban (hoac qua vo mau test nhanh)
            bool isCorrect = false;
            if (isSampleNotebook)
            {
                // Vở mẫu Toán 6 chuẩn luôn giải đúng bài toán
                isCorrect = true;
            }
            else if (hasRealText)
            {
                if (!string.IsNullOrEmpty(expectedTarget) && CheckStudentAnswer(textLower, expectedTarget))
                {
                    isCorrect = true;
                }
                else if (!string.IsNullOrEmpty(rubricTarget) && CheckStudentAnswer(textLower, rubricTarget))
                {
                    isCorrect = true;
                    expectedTarget = rubricTarget;
                }
            }

            Debug.Log($"[MockGrading] isCorrect={isCorrect}");

            if (isCorrect)
            {
                resp.score = 10;
                resp.passed = true;
                resp.detectedMistakeType = "NONE";
                resp.pedagogicalFeedback = $"Thầy Minh khen ngợi: Bài giải của em cho câu '{GetShortPrompt(prompt)}' rất chuẩn xác và rõ ràng. Em đã thực hiện đúng quy tắc và đạt đáp số chính xác!";
                resp.remedialExercise = "Em đã làm chủ dạng toán này! Hãy tiếp tục rèn luyện thêm các câu tiếp theo nhé.";
                resp.firstWrongStepIndex = -1;
                resp.penaltyRuleToCopy = "";
                resp.correctStepsPrefix = text;

                string[] studentLines = (text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (studentLines.Length > 1)
                {
                    for (int i = 0; i < studentLines.Length; i++)
                    {
                        resp.stepAnalysis.Add(new StepAnalysisItem
                        {
                            step = i + 1,
                            isCorrect = true,
                            comment = $"Dòng {i + 1}: {studentLines[i].Trim()} (Chính xác)"
                        });
                    }
                }
                else
                {
                    for (int i = 0; i < rubrics.Count; i++)
                    {
                        var r = rubrics[i];
                        resp.stepAnalysis.Add(new StepAnalysisItem
                        {
                            step = r.step > 0 ? r.step : (i + 1),
                            isCorrect = true,
                            comment = r.description
                        });
                    }
                }
            }
            else
            {
                // Phan tich chi tiet tung buoc theo tinh su pham (Socratic), khong lo dap so va khong danh dong sai tat ca cac buoc
                EvaluateStepsDetailed(promptLower, textLower, text, rubrics, expectedTarget, resp);
            }

            return resp;
        }

        /// <summary>
        /// Cham diem va phan tich tung buoc theo Rubric khi hoc sinh chua dat dap so cuoi cung.
        /// Nhan dien chinh xac buoc nao hoc sinh lam dung de cong diem barem, buoc nao bi nham lan de chi ro quy tac.
        /// </summary>
        private void EvaluateStepsDetailed(string promptLower, string textLower, string rawText, List<RubricItem> rubrics, string expectedTarget, AIGradingResponse resp)
        {
            string cleanText = (textLower ?? "").Replace(" ", "").Replace("×", "x").Replace("*", "x").Replace("·", "x").Replace(":", "/");
            string normPrompt = (promptLower ?? "").Replace(" ", "").Replace("×", "x").Replace("*", "x").Replace("·", "x").Replace(":", "/");

            bool[] stepOk = new bool[rubrics.Count];
            string[] stepComments = new string[rubrics.Count];
            string detectedMistake = "ARITHMETIC";
            string specificFeedback = "";
            string remedialAdvice = "";
            bool orderError = false;

            // TRUONG HOP: Bai toan chia thanh go 6,32 m thanh 4 doan va lam tron den hang phan muoi
            if (normPrompt.Contains("6,32") && (normPrompt.Contains("bon") || normPrompt.Contains("thanhgo")))
            {
                bool hasStep1 = cleanText.Contains("6,32:4") || cleanText.Contains("6.32/4") || cleanText.Contains("6,32/4") || cleanText.Contains("6.32:4");
                stepOk[0] = hasStep1;
                stepComments[0] = hasStep1 ? "Thiết lập phép tính chia: 6,32 : 4 (Chính xác)" : "Thiết lập phép tính chia: 6,32 : 4 (Chưa thiết lập phép tính)";

                bool hasStep2 = cleanText.Contains("1,58") || cleanText.Contains("1.58");
                if (rubrics.Count > 1)
                {
                    stepOk[1] = hasStep2;
                    stepComments[1] = hasStep2 ? "Thực hiện phép tính chia: 6,32 : 4 = 1,58 (m) (Chính xác)" : "Thực hiện phép tính chia: 6,32 : 4 = 1,58 (m) (Chưa tính ra 1,58)";
                }

                bool hasStep3 = CheckStudentAnswer(cleanText, "1.6") || CheckStudentAnswer(cleanText, "1,6") || cleanText.Contains("1,6") || cleanText.Contains("1.6");
                if (rubrics.Count > 2)
                {
                    stepOk[2] = hasStep3;
                    stepComments[2] = hasStep3 ? "Làm tròn đến hàng phần mười: 1,58 ≈ 1,6 (m) (Chính xác)" : "Làm tròn đến hàng phần mười: 1,58 ≈ 1,6 (m) (Chưa làm tròn đúng 1,6)";
                }

                if (stepOk[0] && (rubrics.Count < 2 || stepOk[1]) && (rubrics.Count < 3 || stepOk[2]))
                {
                    resp.score = 10;
                    resp.passed = true;
                    resp.detectedMistakeType = "NONE";
                    resp.pedagogicalFeedback = "Thầy Minh khen ngợi: Bài giải của em rất xuất sắc! Em đã thực hiện phép chia số thập phân chuẩn xác và làm tròn đúng quy tắc đến hàng phần mười.";
                    resp.remedialExercise = "Em đã làm chủ dạng toán số thập phân!";
                    for (int i = 0; i < rubrics.Count; i++)
                    {
                        resp.stepAnalysis.Add(new StepAnalysisItem
                        {
                            step = rubrics[i].step > 0 ? rubrics[i].step : (i + 1),
                            isCorrect = true,
                            comment = rubrics[i].description
                        });
                    }
                    return;
                }
            }
            // TRUONG HOP A: Bai toan thu tu thuc hien phep tinh A = 15 - (3 + 2) x 2
            if (normPrompt.Contains("15-(3+2)"))
            {
                // Buoc 1: Tinh trong ngoac (3 + 2) = 5
                // Hoc sinh dat neu co 15 - 5 hoac 5 x 2 hoac 3 + 2 = 5 hoac 15-5x2
                bool hasStep1 = cleanText.Contains("15-5") || cleanText.Contains("5x2") || cleanText.Contains("3+2=5") || (cleanText.Contains("=5") && cleanText.Length < 8);
                stepOk[0] = hasStep1;
                stepComments[0] = hasStep1 
                    ? "Tính trong ngoặc: (3 + 2) = 5 (Chính xác)" 
                    : "Tính trong ngoặc: (3 + 2) = 5 (Chưa thực hiện hoặc tính nhầm phép tính trong ngoặc)";

                // Kiem tra loi nham thu tu thuc hien phep tinh: 15 - 5 = 10 roi lay 10 x 2 = 20
                orderError = cleanText.Contains("10x2") || cleanText.Contains("=20") || cleanText.Contains("15-5=10");

                // Buoc 2: Nhan truoc 5 x 2 = 10
                if (rubrics.Count > 1)
                {
                    bool hasStep2 = cleanText.Contains("15-10") || cleanText.Contains("5x2=10");
                    if (hasStep2)
                    {
                        stepOk[1] = true;
                        stepComments[1] = "Nhân trước: 5 × 2 = 10 (Chính xác)";
                    }
                    else if (orderError)
                    {
                        stepOk[1] = false;
                        detectedMistake = "ORDER_OF_OPERATIONS";
                        stepComments[1] = "Cần nhân trước: 5 × 2 = 10 (Em đã lấy 15 - 5 = 10 trước là sai thứ tự ưu tiên!)";
                    }
                    else
                    {
                        stepOk[1] = false;
                        stepComments[1] = "Cần nhân trước: 5 × 2 = 10 (Chưa thực hiện phép nhân theo thứ tự ưu tiên)";
                    }
                }

                // Buoc 3: Tru sau 15 - 10 = 5
                if (rubrics.Count > 2)
                {
                    bool hasStep3 = CheckStudentAnswer(cleanText, "5");
                    stepOk[2] = hasStep3;
                    stepComments[2] = hasStep3 
                        ? "Trừ sau: 15 - 10 = 5 (Chính xác)" 
                        : (orderError 
                            ? "Trừ sau: 15 - 10 = 5 (Do bước trước tính nhầm thứ tự nên kết quả ra 20 thay vì 5)" 
                            : "Trừ sau: 15 - 10 = 5 (Chưa đạt đáp số đúng 5)");
                }

                if (stepOk[0] && stepOk[1] && stepOk[2])
                {
                    resp.score = 10;
                    resp.passed = true;
                    resp.detectedMistakeType = "NONE";
                    resp.pedagogicalFeedback = $"Thầy Minh khen ngợi: Bài giải của em cho câu '{GetShortPrompt(promptLower)}' rất chuẩn xác và rõ ràng. Em đã thực hiện đúng quy tắc và đạt đáp số chính xác!";
                    resp.remedialExercise = "Em đã làm chủ dạng toán này! Hãy tiếp tục rèn luyện thêm các câu tiếp theo nhé.";
                    for (int i = 0; i < rubrics.Count; i++)
                    {
                        resp.stepAnalysis.Add(new StepAnalysisItem
                        {
                            step = rubrics[i].step > 0 ? rubrics[i].step : (i + 1),
                            isCorrect = true,
                            comment = rubrics[i].description
                        });
                    }
                    return;
                }

                if (orderError && hasStep1)
                {
                    detectedMistake = "ORDER_OF_OPERATIONS";
                    specificFeedback = "Thầy Minh nhận xét: Em đã làm đúng Bước 1 (tính trong ngoặc: 3 + 2 = 5 để được biểu thức 15 - 5 × 2). Tuy nhiên ở bước tiếp theo, em đã nhầm thứ tự ưu tiên khi lấy 15 - 5 = 10 trước! Hãy nhớ quy tắc cốt lõi: 'Nhân chia trước, cộng trừ sau' nhé.";
                    remedialAdvice = "Gợi ý của Thầy Minh: Ở biểu thức 15 - 5 × 2, em hãy thực hiện phép nhân 5 × 2 = 10 trước, sau đó mới lấy 15 - 10 nhé. Bấm 'GIẢI LẠI CÂU NÀY' để thử lại!";
                }
            }
            // TRUONG HOP B: Bai toan so nguyen A = (-25) + 48 + (-75) + 52
            else if (normPrompt.Contains("(-25)+48") || normPrompt.Contains("-25+48"))
            {
                bool hasStep1 = cleanText.Contains("-100") || cleanText.Contains("(-100)") || cleanText.Contains("100");
                stepOk[0] = hasStep1;
                stepComments[0] = hasStep1 
                    ? $"{rubrics[0].description} (Chinh xac: nhom dung cap so doi hoac cung dau)" 
                    : $"{rubrics[0].description} (Chua nhom hop ly cac so)";

                if (rubrics.Count > 1)
                {
                    bool hasStep2 = cleanText.Contains("-100+100") || cleanText.Contains("(-100)+100") || (cleanText.Contains("-100") && cleanText.Contains("100"));
                    stepOk[1] = hasStep2;
                    stepComments[1] = hasStep2 ? $"{rubrics[1].description} (Chinh xac)" : $"{rubrics[1].description} (Tinh toan cac cap so chua chuan)";
                }

                if (rubrics.Count > 2)
                {
                    bool hasStep3 = CheckStudentAnswer(cleanText, "0");
                    stepOk[2] = hasStep3;
                    stepComments[2] = hasStep3 ? $"{rubrics[2].description} (Chính xác)" : $"{rubrics[2].description} (Chưa đạt đáp số 0)";
                }

                if (stepOk[0] && (rubrics.Count < 2 || stepOk[1]) && (rubrics.Count < 3 || stepOk[2]))
                {
                    resp.score = 10;
                    resp.passed = true;
                    resp.detectedMistakeType = "NONE";
                    resp.pedagogicalFeedback = $"Thầy Minh khen ngợi: Bài giải của em cho câu '{GetShortPrompt(promptLower)}' rất chuẩn xác và rõ ràng. Em đã thực hiện đúng quy tắc và đạt đáp số chính xác!";
                    resp.remedialExercise = "Em đã làm chủ dạng toán này! Hãy tiếp tục rèn luyện thêm các câu tiếp theo nhé.";
                    for (int i = 0; i < rubrics.Count; i++)
                    {
                        resp.stepAnalysis.Add(new StepAnalysisItem
                        {
                            step = rubrics[i].step > 0 ? rubrics[i].step : (i + 1),
                            isCorrect = true,
                            comment = rubrics[i].description
                        });
                    }
                    return;
                }

                if (stepOk[0])
                {
                    specificFeedback = "Thầy Minh nhận xét: Em đã biết cách nhóm các số hạng một cách hợp lý. Hãy cẩn thận tính lại tổng hai số nguyên đối nhau (-100 + 100) nhé!";
                    remedialAdvice = "Gợi ý của Thầy Minh: Hai số nguyên đối nhau có tổng bằng 0. Em hãy kiểm tra lại và bấm 'GIẢI LẠI CÂU NÀY' nhé.";
                }
            }
            // TRUONG HOP C: Bai toan ti so phan tram / phieu bau (Tan nhan duoc 120 phieu bau...)
            else if (normPrompt.Contains("120") && normPrompt.Contains("60%"))
            {
                bool hasStep1 = cleanText.Contains("120") && (cleanText.Contains("60") || cleanText.Contains("0.6") || cleanText.Contains("0,6"));
                stepOk[0] = hasStep1;
                stepComments[0] = hasStep1 
                    ? $"{rubrics[0].description} (Chính xác: Nhận diện đúng bài toán tìm một số biết giá trị phân số)" 
                    : $"{rubrics[0].description} (Chưa nhận diện đúng dạng toán)";

                bool multiplied = cleanText.Contains("120x60") || cleanText.Contains("120*60") || cleanText.Contains("=72");
                if (rubrics.Count > 1)
                {
                    if (multiplied)
                    {
                        stepOk[1] = false;
                        detectedMistake = "ORDER_OF_OPERATIONS";
                        stepComments[1] = $"{rubrics[1].description} (Chưa chính xác: Đây là bài toán tìm một số khi biết 60% của nó, ta phải dùng phép CHIA '120 : 60%', không dùng phép nhân)";
                    }
                    else
                    {
                        bool hasDiv = cleanText.Contains("120/0.6") || cleanText.Contains("120/60") || cleanText.Contains("120/(60/100)");
                        stepOk[1] = hasDiv;
                        stepComments[1] = hasDiv ? $"{rubrics[1].description} (Chính xác)" : $"{rubrics[1].description} (Chưa thiết lập đúng phép chia)";
                    }
                }

                if (rubrics.Count > 2)
                {
                    bool hasStep3 = CheckStudentAnswer(cleanText, "200");
                    stepOk[2] = hasStep3;
                    stepComments[2] = hasStep3 ? $"{rubrics[2].description} (Chính xác)" : $"{rubrics[2].description} (Chưa tính ra đáp số chính xác 200 người)";
                }

                if (stepOk[0] && (rubrics.Count < 2 || stepOk[1]) && (rubrics.Count < 3 || stepOk[2]))
                {
                    resp.score = 10;
                    resp.passed = true;
                    resp.detectedMistakeType = "NONE";
                    resp.pedagogicalFeedback = $"Thầy Minh khen ngợi: Bài giải của em cho câu '{GetShortPrompt(promptLower)}' rất chuẩn xác và rõ ràng. Em đã thực hiện đúng quy tắc và đạt đáp số chính xác!";
                    resp.remedialExercise = "Em đã làm chủ dạng toán này! Hãy tiếp tục rèn luyện thêm các câu tiếp theo nhé.";
                    for (int i = 0; i < rubrics.Count; i++)
                    {
                        resp.stepAnalysis.Add(new StepAnalysisItem
                        {
                            step = rubrics[i].step > 0 ? rubrics[i].step : (i + 1),
                            isCorrect = true,
                            comment = rubrics[i].description
                        });
                    }
                    return;
                }

                if (multiplied)
                {
                    specificFeedback = "Thầy Minh nhận xét: Em đã nhầm lẫn giữa hai bài toán phân số: Để tìm tổng số phiếu khi biết 60% tổng số phiếu là 120, ta phải làm phép CHIA: 120 : 60% (tức 120 : 0,6), không phải phép nhân nhé!";
                    remedialAdvice = "Gợi ý của Thầy Minh: Muốn tìm một số biết m/n của nó bằng a, ta tính a : (m/n). Em hãy lấy 120 : 0,6 và bấm 'GIẢI LẠI CÂU NÀY' nhé!";
                }
            }
            // TRUONG HOP D: TONG QUAT CHO CAC BAI TOAN KHAC CO RUBRIC
            else
            {
                for (int i = 0; i < rubrics.Count; i++)
                {
                    var r = rubrics[i];
                    string rDesc = r.description ?? "";
                    string rAns = ExtractFinalAnswer(rDesc);

                    if (i == 0)
                    {
                        bool matched = !string.IsNullOrEmpty(rAns) && cleanText.Contains(rAns);
                        if (!matched && cleanText.Length > 12)
                        {
                            matched = true;
                        }
                        stepOk[i] = matched;
                        stepComments[i] = matched ? $"{r.description} (Chính xác)" : $"{r.description} (Chưa thực hiện đúng)";
                    }
                    else if (i == rubrics.Count - 1)
                    {
                        bool matched = (!string.IsNullOrEmpty(expectedTarget) && CheckStudentAnswer(cleanText, expectedTarget)) ||
                                       (!string.IsNullOrEmpty(rAns) && CheckStudentAnswer(cleanText, rAns));
                        stepOk[i] = matched;
                        stepComments[i] = matched ? $"{r.description} (Chính xác)" : $"{r.description} (Chưa đạt đáp số đúng)";
                    }
                    else
                    {
                        bool matched = !string.IsNullOrEmpty(rAns) && cleanText.Contains(rAns);
                        stepOk[i] = matched;
                        stepComments[i] = matched ? $"{r.description} (Chính xác)" : $"{r.description} (Chưa chính xác)";
                    }
                }

                bool allCorrect = true;
                for (int i = 0; i < rubrics.Count; i++)
                {
                    if (!stepOk[i]) { allCorrect = false; break; }
                }

                if (allCorrect)
                {
                    resp.score = 10;
                    resp.passed = true;
                    resp.detectedMistakeType = "NONE";
                    resp.pedagogicalFeedback = $"Thầy Minh khen ngợi: Bài giải của em cho câu '{GetShortPrompt(promptLower)}' rất chuẩn xác và rõ ràng. Em đã thực hiện đúng quy tắc và đạt đáp số chính xác!";
                    resp.remedialExercise = "Em đã làm chủ dạng toán này! Hãy tiếp tục rèn luyện thêm các câu tiếp theo nhé.";
                    for (int i = 0; i < rubrics.Count; i++)
                    {
                        resp.stepAnalysis.Add(new StepAnalysisItem
                        {
                            step = rubrics[i].step > 0 ? rubrics[i].step : (i + 1),
                            isCorrect = true,
                            comment = rubrics[i].description
                        });
                    }
                    return;
                }
            }

            // Tinh tong diem theo cac buoc lam dung
            int earnedScore = 0;
            int correctStepsCount = 0;
            for (int i = 0; i < rubrics.Count; i++)
            {
                if (stepOk[i])
                {
                    earnedScore += rubrics[i].points > 0 ? rubrics[i].points : 3;
                    correctStepsCount++;
                }
                resp.stepAnalysis.Add(new StepAnalysisItem
                {
                    step = rubrics[i].step > 0 ? rubrics[i].step : (i + 1),
                    isCorrect = stepOk[i],
                    comment = stepComments[i]
                });
            }

            if (earnedScore == 0 && (rawText ?? "").Length > 15)
            {
                earnedScore = 1;
            }

            resp.score = Mathf.Clamp(earnedScore, 0, 9);
            resp.passed = false;
            resp.detectedMistakeType = detectedMistake;

            // Xác định dòng đầu tiên bị sai & lưu lại các dòng đúng phía trước để học sinh sửa tiếp
            string[] studentLines = (rawText ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int firstWrongIndex = -1;
            string correctPrefix = "";

            if (normPrompt.Contains("15-(3+2)"))
            {
                if (orderError)
                {
                    firstWrongIndex = 1;
                    if (studentLines.Length > 0) correctPrefix = studentLines[0].Trim();
                }
                else if (!stepOk[0])
                {
                    firstWrongIndex = 0;
                }
                else
                {
                    firstWrongIndex = 2;
                    if (studentLines.Length > 1) correctPrefix = studentLines[0].Trim() + "\n" + studentLines[1].Trim();
                    else if (studentLines.Length > 0) correctPrefix = studentLines[0].Trim();
                }
            }
            else
            {
                for (int i = 0; i < rubrics.Count; i++)
                {
                    if (!stepOk[i])
                    {
                        firstWrongIndex = i;
                        break;
                    }
                }
                if (firstWrongIndex > 0 && studentLines.Length >= firstWrongIndex)
                {
                    var sb = new System.Text.StringBuilder();
                    for (int k = 0; k < firstWrongIndex; k++)
                    {
                        if (k > 0) sb.Append("\n");
                        sb.Append(studentLines[k].Trim());
                    }
                    correctPrefix = sb.ToString();
                }
            }

            resp.firstWrongStepIndex = firstWrongIndex;
            resp.correctStepsPrefix = correctPrefix;
            resp.penaltyRuleToCopy = CurriculumEssayKnowledgeBase.GetPenaltyRule(detectedMistake, promptLower, "");

            // Nếu học sinh viết nhiều dòng, hiển thị phân tích từng dòng thực tế của học sinh (không giới hạn 3 bước)
            if (studentLines.Length > 1)
            {
                resp.stepAnalysis.Clear();
                for (int i = 0; i < studentLines.Length; i++)
                {
                    bool isLineOk = firstWrongIndex < 0 || i < firstWrongIndex;
                    string lineComment = isLineOk 
                        ? $"Dòng {i + 1}: {studentLines[i].Trim()} (Chính xác)" 
                        : (i == firstWrongIndex 
                            ? $"Dòng {i + 1}: {studentLines[i].Trim()} (Sai ở dòng này: {stepComments[Mathf.Clamp(i, 0, stepComments.Length - 1)]})" 
                            : $"Dòng {i + 1}: {studentLines[i].Trim()} (Chưa chính xác: Do dòng trước tính nhầm)");

                    resp.stepAnalysis.Add(new StepAnalysisItem
                    {
                        step = i + 1,
                        isCorrect = isLineOk,
                        comment = lineComment
                    });
                }
            }

            if (string.IsNullOrEmpty(specificFeedback))
            {
                if (correctStepsCount > 0)
                {
                    specificFeedback = $"Thay Minh nhan xet: Em da lam tot {correctStepsCount} buoc dau tien! Tuy nhien o cac buoc sau ket qua chua chinh xac. Em hay kiem tra lai phep tinh cua cac buoc sau nhe!";
                }
                else
                {
                    specificFeedback = $"Thay Minh nhan xet: Bai giai cua em cho cau '{GetShortPrompt(promptLower)}' chua thuc hien dung cac buoc theo quy tac toan hoc. Em hay xem lai ly thuyet roi thu lai nhe!";
                }
            }

            if (string.IsNullOrEmpty(remedialAdvice))
            {
                remedialAdvice = "Goi y cua Thay Minh: Em hay doc ky de, thuc hien can than tung buoc va bam 'GIAI LAI CAU NAY' ben duoi nhe!";
            }

            resp.pedagogicalFeedback = specificFeedback;
            resp.remedialExercise = remedialAdvice;
        }

        /// <summary>
        /// Trich xuat dap so cuoi cung tu mo ta rubric buoc cuoi.
        /// Uu tien lay so/phan so SAU dau "=" cuoi cung.
        /// VD: "Tinh ra ket qua: 120 : 0,6 = 200 (nguoi)" => "200"
        ///     "Rut gon ve phan so toi gian: 1/2" => "1/2"
        ///     "Ket qua chinh xac: A = 0" => "0"
        /// </summary>
        private static string ExtractFinalAnswer(string rubricDesc)
        {
            if (string.IsNullOrEmpty(rubricDesc)) return "";

            // Tim dau "=" cuoi cung va lay so/phan so ngay sau no
            int lastEq = rubricDesc.LastIndexOf('=');
            if (lastEq >= 0 && lastEq < rubricDesc.Length - 1)
            {
                string afterEq = rubricDesc.Substring(lastEq + 1).Trim();
                // Lay so/phan so dau tien sau dau =
                var match = System.Text.RegularExpressions.Regex.Match(afterEq, @"^[\s]*(-?\d+(?:[.,]\d+)?(?:/\d+)?)");
                if (match.Success)
                {
                    string val = match.Groups[1].Value.Replace(",", ".");
                    if (val.EndsWith(".0")) val = val.Substring(0, val.Length - 2);
                    return val;
                }
            }

            // Fallback: lay so cuoi cung trong chuoi (thuong la dap so)
            var matches = System.Text.RegularExpressions.Regex.Matches(rubricDesc, @"(-?\d+(?:[.,]\d+)?(?:/\d+)?)");
            if (matches.Count > 0)
            {
                var lastMatch = matches[matches.Count - 1];
                string val = lastMatch.Groups[1].Value.Replace(",", ".");
                if (val.EndsWith(".0")) val = val.Substring(0, val.Length - 2);
                return val;
            }

            return "";
        }

        /// <summary>
        /// So sanh dap an hoc sinh voi dap so ky vong:
        /// - Xac dinh ro ky vong la so/phan so hay van ban
        /// - Neu la so/phan so: doi chieu so cuoi cung / bieu thuc ket luan cua hoc sinh, KHONG match substring tuy tien.
        /// </summary>
        private static bool CheckStudentAnswer(string studentTextLower, string expected)
        {
            if (string.IsNullOrEmpty(expected)) return false;

            string normExpected = expected.Trim().Replace(" ", "").Replace(",", ".").ToLowerInvariant();
            string normStudent = (studentTextLower ?? "").Trim().Replace(" ", "").Replace(",", ".").ToLowerInvariant();

            if (normExpected.StartsWith("+")) normExpected = normExpected.Substring(1);

            double expectedNum = ParseNumberOrFraction(normExpected);
            bool isNumericExpected = !double.IsNaN(expectedNum);

            if (isNumericExpected)
            {
                // 1. Kiem tra dong cuoi cung (noi ket luan dap so)
                string[] lines = (studentTextLower ?? "").Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    string lastLine = lines[lines.Length - 1].Trim().Replace(" ", "").Replace(",", ".").ToLowerInvariant();
                    if (lastLine == normExpected || lastLine == "=" + normExpected || lastLine.EndsWith("=" + normExpected))
                    {
                        return true;
                    }

                    var lastLineNums = System.Text.RegularExpressions.Regex.Matches(lastLine, @"(-?\d+(?:\.\d+)?(?:/\d+(?:\.\d+)?)?)");
                    if (lastLineNums.Count > 0)
                    {
                        string lastNum = lastLineNums[lastLineNums.Count - 1].Groups[1].Value;
                        if (CompareNumberOrFraction(lastNum, normExpected))
                        {
                            return true;
                        }
                    }
                }

                // 2. Kiem tra co bieu thuc ro rang: "= <expected>" hoac ": <expected>" hoac "dap so <expected>"
                // Luu y dung Regex de kiem tra boundary khong bi nham "5" voi "50" hay "15-5"
                string escapedExp = System.Text.RegularExpressions.Regex.Escape(normExpected);
                var eqMatch = System.Text.RegularExpressions.Regex.Match(studentTextLower ?? "", @"(?:=|:\s*|đáp\s*số:?\s*|kq:?\s*)\s*" + escapedExp + @"(?!\d)");
                if (eqMatch.Success)
                {
                    return true;
                }

                // 3. Kiem tra so cuoi cung trong toan bo bai lam cua hoc sinh
                var allNums = System.Text.RegularExpressions.Regex.Matches(normStudent, @"(-?\d+(?:\.\d+)?(?:/\d+(?:\.\d+)?)?)");
                if (allNums.Count > 0)
                {
                    string finalNum = allNums[allNums.Count - 1].Groups[1].Value;
                    if (CompareNumberOrFraction(finalNum, normExpected))
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                // Dap an la chu (vd: "truc doi xung", "tam doi xung", "goc nhon", "so sanh", "vi khuan")
                string plainStudent = (studentTextLower ?? "").ToLowerInvariant();
                return plainStudent.Contains(normExpected) || (studentTextLower ?? "").Contains(expected.ToLowerInvariant());
            }
        }

        private static bool CompareNumberOrFraction(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            if (a == b) return true;

            double valA = ParseNumberOrFraction(a);
            double valB = ParseNumberOrFraction(b);

            if (!double.IsNaN(valA) && !double.IsNaN(valB))
            {
                return System.Math.Abs(valA - valB) < 0.001;
            }
            return false;
        }

        private static double ParseNumberOrFraction(string s)
        {
            if (string.IsNullOrEmpty(s)) return double.NaN;
            if (s.Contains("/"))
            {
                var parts = s.Split('/');
                if (parts.Length == 2 && 
                    double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double num) && 
                    double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double den) && 
                    den != 0)
                {
                    return num / den;
                }
            }
            else if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                return val;
            }
            return double.NaN;
        }

        private static string GetHardcodedAnswer(string promptLower)
        {
            return CurriculumEssayKnowledgeBase.GetAnswer(promptLower, "");
        }

        private static string GetShortPrompt(string p)
        {
            if (string.IsNullOrEmpty(p)) return "Bai tap tu luan";
            string clean = p.Replace("\n", " ").Replace("\r", "").Trim();
            if (clean.Length > 55) return clean.Substring(0, 52) + "...";
            return clean;
        }
        
        #endregion

        private IEnumerator SimulateDelay(Action action)
        {
            yield return new WaitForSeconds(0.3f);
            action?.Invoke();
        }
    }
}
