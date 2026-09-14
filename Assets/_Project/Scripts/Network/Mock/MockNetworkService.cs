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
        public static MockNetworkService Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); }
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
                        classId = "6A1"
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
                        classId = "6A1"
                    }, "Success");
                }
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
                callback(true, new AIGradingResponse
                {
                    score = 8,
                    maxScore = 10,
                    passed = true,
                    detectedMistakeType = "NONE",
                    pedagogicalFeedback = "Bài làm rất rõ ràng, thực hiện đúng thứ tự các phép tính!",
                    stepAnalysis = new List<StepAnalysisItem>
                    {
                        new StepAnalysisItem { step = 1, isCorrect = true, comment = "Đúng bước 1" },
                        new StepAnalysisItem { step = 2, isCorrect = true, comment = "Đúng bước 2" }
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
