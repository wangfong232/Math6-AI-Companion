// Assets/_Project/Scripts/Network/Mock/MockNetworkService.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Math6Companion.Core.Contracts;

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
                if (username == "teacher01")
                {
                    callback(true, new UserData
                    {
                        userId = "TEA_001",
                        username = "teacher01",
                        fullName = "Co Mai",
                        role = "TEACHER",
                        grade = 6,
                        classId = "6A1",
                        nickname = "Cô Mai Toán"
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
                        videoUrl = "https://www.youtube.com/watch?v=mock_lesson_01",
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
                        videoUrl = "https://www.youtube.com/watch?v=mock_lesson_02",
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
                        videoUrl = "https://www.youtube.com/watch?v=mock_lesson_03",
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
                string text = payload.studentSteps ?? "";
                bool hasText = !string.IsNullOrEmpty(text) && !text.Contains("(Không có bài làm)");

                if (!hasImage && !hasText)
                {
                    callback(true, new AIGradingResponse
                    {
                        score = 0,
                        maxScore = 10,
                        passed = false,
                        detectedMistakeType = "UNREADABLE_OR_IRRELEVANT_IMAGE",
                        pedagogicalFeedback = "Học sinh chưa cung cấp bài giải hoặc ảnh bài làm không hợp lệ.",
                        stepAnalysis = new List<StepAnalysisItem>
                        {
                            new StepAnalysisItem { step = 1, isCorrect = false, comment = "Chưa có lời giải bước 1" },
                            new StepAnalysisItem { step = 2, isCorrect = false, comment = "Chưa có lời giải bước 2" },
                            new StepAnalysisItem { step = 3, isCorrect = false, comment = "Chưa có lời giải bước 3" }
                        }
                    }, "Success");
                    return;
                }

                callback(true, new AIGradingResponse
                {
                    score = 10,
                    maxScore = 10,
                    passed = true,
                    detectedMistakeType = "NONE",
                    pedagogicalFeedback = "Bài làm rất rõ ràng, thực hiện chuẩn xác thứ tự các phép tính trong ngoặc trước, nhân chia trước cộng trừ sau!",
                    stepAnalysis = new List<StepAnalysisItem>
                    {
                        new StepAnalysisItem { step = 1, isCorrect = true, comment = "Tính đúng trong ngoặc: (3 + 2) = 5" },
                        new StepAnalysisItem { step = 2, isCorrect = true, comment = "Nhân trước: 5 * 2 = 10" },
                        new StepAnalysisItem { step = 3, isCorrect = true, comment = "Trừ sau: 15 - 10 = 5" }
                    }
                }, "Success");
            }));
        }
        #endregion

        private IEnumerator SimulateDelay(Action action)
        {
            yield return new WaitForSeconds(0.3f);
            action?.Invoke();
        }
    }
}
