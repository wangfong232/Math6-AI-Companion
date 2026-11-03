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
                    string cleanUser = string.IsNullOrEmpty(username) ? "student_01" : username.Trim();
                    string uId = cleanUser.ToUpper().StartsWith("STU_") ? cleanUser.ToUpper() : "STU_" + cleanUser;
                    callback(true, new UserData
                    {
                        userId = uId,
                        username = cleanUser,
                        fullName = cleanUser,
                        role = "STUDENT",
                        grade = 6,
                        classId = "6A1",
                        nickname = cleanUser
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
                        title = "BÃ i 1: PhÃ©p cá»™ng vÃ  trá»« sá»‘ nguyÃªn",
                        description = "Quy táº¯c cá»™ng trá»« sá»‘ nguyÃªn cÃ¹ng dáº¥u vÃ  khÃ¡c dáº¥u",
                        grade = 6,
                        summaryText = "1. Cá»™ng hai sá»‘ nguyÃªn cÃ¹ng dáº¥u:\n- Cá»™ng hai pháº§n tá»± nhiÃªn rá»“i Ä‘áº·t dáº¥u chung trÆ°á»›c káº¿t quáº£.\n\n2. Cá»™ng hai sá»‘ nguyÃªn khÃ¡c dáº¥u:\n- Láº¥y sá»‘ cÃ³ pháº§n tá»± nhiÃªn lá»›n hÆ¡n trá»« sá»‘ cÃ³ pháº§n tá»± nhiÃªn bÃ© hÆ¡n, Ä‘áº·t dáº¥u cá»§a sá»‘ cÃ³ pháº§n tá»± nhiÃªn lá»›n hÆ¡n trÆ°á»›c hiá»‡u tÃ¬m Ä‘Æ°á»£c.",
                        videoUrl = "https://www.youtube.com/watch?v=vcR0jey6xcI",
                        userHighestScore = 10,
                        topStudentNickname = "BÃ¬nhToÃ¡nHá»c",
                        topStudentAttempts = 16
                    },
                    new LessonData
                    {
                        lessonId = "LES_INT_02",
                        topic = "Integers",
                        title = "BÃ i 2: PhÃ©p nhÃ¢n vÃ  chia hai sá»‘ nguyÃªn",
                        description = "Quy táº¯c nhÃ¢n chia sá»‘ nguyÃªn, nhÃ¢n khÃ¡c dáº¥u vÃ  cÃ¹ng dáº¥u",
                        grade = 6,
                        summaryText = "1. NhÃ¢n hai sá»‘ nguyÃªn cÃ¹ng dáº¥u: Káº¿t quáº£ luÃ´n lÃ  sá»‘ nguyÃªn DÆ¯Æ NG (+).\n2. NhÃ¢n hai sá»‘ nguyÃªn khÃ¡c dáº¥u: Káº¿t quáº£ luÃ´n lÃ  sá»‘ nguyÃªn Ã‚M (-).\nQuy táº¯c dáº¥u: (+) * (+) = (+), (-) * (-) = (+), (+) * (-) = (-), (-) * (+) = (-).",
                        videoUrl = "https://www.youtube.com/watch?v=mYyUEaSUcu4",
                        userHighestScore = 8,
                        topStudentNickname = "ChiTháº§nÄá»“ng",
                        topStudentAttempts = 12
                    },
                    new LessonData
                    {
                        lessonId = "LES_FRAC_01",
                        topic = "Fractions",
                        title = "BÃ i 3: KhÃ¡i niá»‡m phÃ¢n sá»‘ & RÃºt gá»n phÃ¢n sá»‘",
                        description = "Äá»‹nh nghÄ©a phÃ¢n sá»‘, tá»­ vÃ  máº«u, quy táº¯c rÃºt gá»n tá»‘i giáº£n",
                        grade = 6,
                        summaryText = "PhÃ¢n sá»‘ cÃ³ dáº¡ng a/b vá»›i a, b thuá»™c Z, b khÃ¡c 0. a lÃ  tá»­ sá»‘, b lÃ  máº«u sá»‘.\nRÃºt gá»n phÃ¢n sá»‘: Chia cáº£ tá»­ vÃ  máº«u cho Æ°á»›c chung lá»›n hÆ¡n 1 Ä‘á»ƒ Ä‘Æ°á»£c phÃ¢n sá»‘ tá»‘i giáº£n.",
                        videoUrl = "https://www.youtube.com/watch?v=ZKNbig3XpM8",
                        userHighestScore = 9,
                        topStudentNickname = "AnTháº§nÄá»“ng",
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
                        prompt = "TÃ­nh giÃ¡ trá»‹ biá»ƒu thá»©c: (-15) + 7",
                        options = new QuestionOptionMap { A = "8", B = "-8", C = "22", D = "-22" },
                        correctAnswer = "B",
                        explanation = "Cá»™ng hai sá»‘ khÃ¡c dáº¥u: 15 - 7 = 8, mang dáº¥u cá»§a -15 nÃªn káº¿t quáº£ lÃ  -8."
                    },
                    new QuestionData
                    {
                        questionId = "Q_INT_002",
                        lessonId = "LES_INT_01",
                        topic = "Integers",
                        type = "MCQ",
                        difficulty = "MEDIUM",
                        prompt = "Káº¿t quáº£ cá»§a phÃ©p tÃ­nh: (-4) * (-5) lÃ :",
                        options = new QuestionOptionMap { A = "-20", B = "20", C = "-9", D = "9" },
                        correctAnswer = "B",
                        explanation = "TÃ­ch cá»§a hai sá»‘ nguyÃªn cÃ¹ng dáº¥u lÃ  má»™t sá»‘ nguyÃªn dÆ°Æ¡ng: (-4) * (-5) = 20."
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
                        name = "NhÃ  TÃ­nh ToÃ¡n HoÃ n Háº£o",
                        description = "Äáº¡t Ä‘iá»ƒm tá»‘i Ä‘a trong bÃ i kiá»ƒm tra",
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
                callback?.Invoke(true, "Ghi nháº­n AI Tutor log (Mock) thÃ nh cÃ´ng!");
            }));
        }

        public void GetStudentAnalytics(string studentId, Action<bool, StudentAnalyticsData, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                callback(true, new StudentAnalyticsData
                {
                    userId = studentId,
                    fullName = "Học sinh",
                    completedLessons = 4,
                    averageScore = 9.5f,
                    badges = new List<BadgeData>
                    {
                        new BadgeData { badgeCode = "FIRST_LESSON", name = "Khá»Ÿi Äáº§u Nan" }
                    }
                }, "Success");
            }));
        }

        public void JoinVersusQueue(string userId, string nickname, string mode, Action<bool, VersusQueueResponse, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                callback?.Invoke(true, new VersusQueueResponse
                {
                    success = true,
                    status = "MATCHED",
                    message = "Ghép trận giả lập thành công!",
                    roomId = "MOCK_ROOM_" + System.DateTime.Now.Ticks,
                    opponentId = "STU_BOT_01",
                    opponentNickname = "BảoToánHọc (Lớp 6A2)",
                    mode = mode ?? "MCQ",
                    questionIds = new List<string> { "Q_INT_01", "Q_INT_02", "Q_INT_03", "Q_INT_04", "Q_INT_05" }
                }, "Success");
            }));
        }

        public void PollVersusQueue(string userId, Action<bool, VersusQueueResponse, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                callback?.Invoke(true, new VersusQueueResponse
                {
                    success = true,
                    status = "MATCHED",
                    message = "Đã tìm thấy phòng!",
                    roomId = "MOCK_ROOM_" + System.DateTime.Now.Ticks,
                    opponentId = "STU_BOT_01",
                    opponentNickname = "BảoToánHọc (Lớp 6A2)",
                    mode = "MCQ"
                }, "Success");
            }));
        }

        public void CancelVersusQueue(string userId, Action<bool, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                callback?.Invoke(true, "Đã hủy tìm trận (Mock)");
            }));
        }

        public void SubmitVersusScore(string roomId, string userId, int score, Action<bool, VersusRoomResultResponse, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                int botScore = UnityEngine.Random.Range(6, 10);
                callback?.Invoke(true, new VersusRoomResultResponse
                {
                    success = true,
                    roomId = roomId,
                    player1Id = userId,
                    player1Score = score,
                    player1Done = true,
                    player2Id = "STU_BOT_01",
                    player2Name = "BảoToánHọc (Lớp 6A2)",
                    player2Score = botScore,
                    player2Done = true,
                    isFinished = true,
                    winnerId = score >= botScore ? userId : "STU_BOT_01",
                    message = "Mock Score Submitted"
                }, "Success");
            }));
        }

        public void PollVersusRoomResult(string roomId, string userId, Action<bool, VersusRoomResultResponse, string> callback)
        {
            StartCoroutine(SimulateDelay(() =>
            {
                int botScore = UnityEngine.Random.Range(6, 10);
                callback?.Invoke(true, new VersusRoomResultResponse
                {
                    success = true,
                    roomId = roomId,
                    player1Id = userId,
                    player1Score = 8,
                    player1Done = true,
                    player2Id = "STU_BOT_01",
                    player2Name = "BảoToánHọc (Lớp 6A2)",
                    player2Score = botScore,
                    player2Done = true,
                    isFinished = true,
                    winnerId = 8 >= botScore ? userId : "STU_BOT_01"
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
                    if (input.Contains("ngoáº·c") || input.Contains("parentheses") || input.Contains("3+2") || input.Contains("5"))
                    {
                        callback(true, new AITutorStepResponse
                        {
                            isCorrect = true,
                            state = "STEP_PASSED",
                            tutorDialogue = "ChÃ­nh xÃ¡c! Ta Æ°u tiÃªn tÃ­nh trong ngoáº·c (3 + 2) trÆ°á»›c. Káº¿t quáº£ (3 + 2) báº±ng máº¥y?",
                            hint = "PhÃ©p cá»™ng Ä‘Æ¡n giáº£n 3 + 2",
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
                            tutorDialogue = "ChÆ°a Ä‘Ãºng rá»“i! HÃ£y nhá»› quy táº¯c thá»© tá»± phÃ©p tÃ­nh: trong ngoáº·c hay ngoÃ i ngoáº·c lÃ m trÆ°á»›c?",
                            hint = "NhÃ¬n vÃ o dáº¥u ngoáº·c trÃ²n nhÃ©.",
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
                            tutorDialogue = "Ráº¥t tá»‘t! Biá»ƒu thá»©c trá»Ÿ thÃ nh 15 - 5 * 2. BÃ¢y giá» ta thá»±c hiá»‡n phÃ©p trá»« hay phÃ©p nhÃ¢n trÆ°á»›c?",
                            hint = "NhÃ¢n chia trÆ°á»›c, cá»™ng trá»« sau.",
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
                            tutorDialogue = "TÃ­nh láº¡i giÃºp tháº§y: 3 + 2 báº±ng máº¥y nÃ o?",
                            hint = "3 cá»™ng thÃªm 2 Ä‘Æ¡n vá»‹.",
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
                        tutorDialogue = "Xuáº¥t sáº¯c! Em Ä‘Ã£ hoÃ n thÃ nh bÃ i toÃ¡n tá»«ng bÆ°á»›c má»™t cÃ¡ch Ä‘á»™c láº­p!",
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
                bool hasText = !string.IsNullOrEmpty(text) && !text.Contains("(KhÃ´ng cÃ³ bÃ i lÃ m)") && !text.Contains("(Há»c sinh khÃ´ng nháº­p vÄƒn báº£n");

                if (!hasImage && !hasText)
                {
                    callback(true, new AIGradingResponse
                    {
                        score = 0,
                        maxScore = 10,
                        passed = false,
                        detectedMistakeType = "UNREADABLE_OR_IRRELEVANT_IMAGE",
                        pedagogicalFeedback = "Há»c sinh chÆ°a cung cáº¥p bÃ i giáº£i hoáº·c áº£nh bÃ i lÃ m khÃ´ng há»£p lá»‡.",
                        remedialExercise = "Em hÃ£y nháº­p lá»i giáº£i hoáº·c táº£i áº£nh bÃ i lÃ m lÃªn Ä‘á»ƒ Tháº§y Minh nháº­n xÃ©t nhÃ©.",
                        stepAnalysis = new List<StepAnalysisItem>
                        {
                            new StepAnalysisItem { step = 1, isCorrect = false, comment = "ChÆ°a cÃ³ lá»i giáº£i cho cÃ¢u há»i nÃ y" }
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
                    callback?.Invoke(false, "", "áº¢nh rá»—ng");
                    return;
                }

                // PhÃ¢n tÃ­ch hÃ¬nh áº£nh cá»¥c bá»™: PhÃ¢n biá»‡t áº£nh tÃ i liá»‡u/trang vá»Ÿ vs áº£nh phong cáº£nh, con váº­t, Ä‘á»“ váº­t
                bool isMathDocument = false;
                bool isSampleExponentPhoto = false;

                try
                {
                    byte[] bytes = System.Convert.FromBase64String(imageBase64);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(bytes))
                    {
                        // Kiá»ƒm tra náº¿u lÃ  file sample_math_essay.png (bÃ i nhÃ¡p luá»¹ thá»«a bá»‹ chá»¥p ngÆ°á»£c 180 Ä‘á»™)
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
                            // Láº¥y máº«u cÃ¡c Ä‘iá»ƒm áº£nh Ä‘á»ƒ Ä‘o Ä‘á»™ sÃ¡ng trung bÃ¬nh vÃ  Ä‘á»™ bÃ£o hÃ²a mÃ u sáº¯c
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

                            // Giáº¥y táº­p há»c sinh luÃ´n cÃ³ ná»n sÃ¡ng (lum >= 0.40) vÃ  Ä‘á»™ bÃ£o hÃ²a tháº¥p (sat <= 0.35)
                            // áº¢nh phong cáº£nh lÃ ng quÃª ban Ä‘Ãªm, con váº­t, cá» cÃ¢y, mÃ n hÃ¬nh game cÃ³ lum tháº¥p hoáº·c sat ráº¥t cao
                            if (avgLum >= 0.38f && avgSat <= 0.35f)
                            {
                                isMathDocument = true;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[MockOCR] Lá»—i phÃ¢n tÃ­ch áº£nh: " + ex.Message);
                }

                // TRÆ¯á»œNG Há»¢P 1: áº¢nh khÃ´ng pháº£i lÃ  tÃ i liá»‡u vá»Ÿ bÃ i táº­p (áº£nh phong cáº£nh, con váº­t, Ä‘á»“ váº­t...)
                if (!isMathDocument)
                {
                    callback?.Invoke(false, "", "áº¢nh khÃ´ng chá»©a chá»¯ viáº¿t tay hoáº·c bÃ i lÃ m toÃ¡n há»c há»£p lá»‡ (HÃ¬nh áº£nh phong cáº£nh / con váº­t / Ä‘á»“ váº­t hoáº·c khÃ´ng cÃ³ phÃ©p tÃ­nh)!");
                    return;
                }

                // TRÆ¯á»œNG Há»¢P 2: LÃ  áº£nh chá»¥p bÃ i lÃ m nhÃ¡p sample_math_essay.png (bá»‹ chá»¥p ngÆ°á»£c 180 Ä‘á»™)
                // OCR tá»± Ä‘á»™ng xoay Ä‘Ãºng chiá»u vÃ  trÃ­ch xuáº¥t nguyÃªn vÄƒn phÃ©p tÃ­nh ghi trÃªn áº£nh Ä‘Ã³
                if (isSampleExponentPhoto)
                {
                    string sampleExp = "= 2 Ã— 3Â² + 5 Ã— (2 + 3)\n= 2 Ã— 9 + 5 Ã— 5\n= 18 + 25\n= 43";
                    callback?.Invoke(true, sampleExp, "Success");
                    return;
                }

                // TRÆ¯á»œNG Há»¢P 3: áº¢nh giáº¥y táº­p nhÆ°ng Ä‘ang á»Ÿ cháº¿ Ä‘á»™ Offline (chÆ°a cÃ³ Gemini API Key)
                callback?.Invoke(false, "", "ChÆ°a cáº¥u hÃ¬nh GEMINI_API_KEY Ä‘á»ƒ OCR áº£nh chá»¯ viáº¿t tay tá»± do. Vui lÃ²ng báº¥m [ðŸ”‘ Cáº¥u hÃ¬nh Gemini Key] hoáº·c tá»± gÃµ bÃ i giáº£i vÃ o Ã´ vÄƒn báº£n!");
            }));
        }

        private AIGradingResponse EvaluateEssayLocally(EssaySubmissionPayload payload, bool hasImage, string text)
        {
            // 0. Tu dong bo sung Rubric chuan va Dap so tu Ngan hang Kien thuc neu Google Sheet bi trong
            CurriculumEssayKnowledgeBase.EnrichSubmissionPayload(payload);

            string prompt = payload.prompt ?? "";
            string promptLower = prompt.ToLower();
            string textLower = (text ?? "").ToLower();

            // 1. Xac dinh dap so ky vong â€” kiem tra ca correctAnswer va rubric buoc cuoi
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

            bool isSampleNotebook = text != null && (text.Contains("Vá»Ÿ bÃ i táº­p máº«u") || text.Contains("Vo bai tap mau"));

            bool hasRealText = !string.IsNullOrEmpty(text) && 
                               !text.Contains("(Khong co bai lam)") && 
                               !text.Contains("(Hoc sinh khong nhap van ban") &&
                               !text.Contains("(Há»c sinh khÃ´ng nháº­p vÄƒn báº£n") &&
                               !text.Contains("(KhÃ´ng cÃ³ bÃ i lÃ m)") &&
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
                // Vá»Ÿ máº«u ToÃ¡n 6 chuáº©n luÃ´n giáº£i Ä‘Ãºng bÃ i toÃ¡n
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
                resp.pedagogicalFeedback = $"Tháº§y Minh khen ngá»£i: BÃ i giáº£i cá»§a em cho cÃ¢u '{GetShortPrompt(prompt)}' ráº¥t chuáº©n xÃ¡c vÃ  rÃµ rÃ ng. Em Ä‘Ã£ thá»±c hiá»‡n Ä‘Ãºng quy táº¯c vÃ  Ä‘áº¡t Ä‘Ã¡p sá»‘ chÃ­nh xÃ¡c!";
                resp.remedialExercise = "Em Ä‘Ã£ lÃ m chá»§ dáº¡ng toÃ¡n nÃ y! HÃ£y tiáº¿p tá»¥c rÃ¨n luyá»‡n thÃªm cÃ¡c cÃ¢u tiáº¿p theo nhÃ©.";
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
                            comment = $"DÃ²ng {i + 1}: {studentLines[i].Trim()} (ChÃ­nh xÃ¡c)"
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
            string cleanText = (textLower ?? "").Replace(" ", "").Replace("Ã—", "x").Replace("*", "x").Replace("Â·", "x").Replace(":", "/");
            string normPrompt = (promptLower ?? "").Replace(" ", "").Replace("Ã—", "x").Replace("*", "x").Replace("Â·", "x").Replace(":", "/");

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
                stepComments[0] = hasStep1 ? "Thiáº¿t láº­p phÃ©p tÃ­nh chia: 6,32 : 4 (ChÃ­nh xÃ¡c)" : "Thiáº¿t láº­p phÃ©p tÃ­nh chia: 6,32 : 4 (ChÆ°a thiáº¿t láº­p phÃ©p tÃ­nh)";

                bool hasStep2 = cleanText.Contains("1,58") || cleanText.Contains("1.58");
                if (rubrics.Count > 1)
                {
                    stepOk[1] = hasStep2;
                    stepComments[1] = hasStep2 ? "Thá»±c hiá»‡n phÃ©p tÃ­nh chia: 6,32 : 4 = 1,58 (m) (ChÃ­nh xÃ¡c)" : "Thá»±c hiá»‡n phÃ©p tÃ­nh chia: 6,32 : 4 = 1,58 (m) (ChÆ°a tÃ­nh ra 1,58)";
                }

                bool hasStep3 = CheckStudentAnswer(cleanText, "1.6") || CheckStudentAnswer(cleanText, "1,6") || cleanText.Contains("1,6") || cleanText.Contains("1.6");
                if (rubrics.Count > 2)
                {
                    stepOk[2] = hasStep3;
                    stepComments[2] = hasStep3 ? "LÃ m trÃ²n Ä‘áº¿n hÃ ng pháº§n mÆ°á»i: 1,58 â‰ˆ 1,6 (m) (ChÃ­nh xÃ¡c)" : "LÃ m trÃ²n Ä‘áº¿n hÃ ng pháº§n mÆ°á»i: 1,58 â‰ˆ 1,6 (m) (ChÆ°a lÃ m trÃ²n Ä‘Ãºng 1,6)";
                }

                if (stepOk[0] && (rubrics.Count < 2 || stepOk[1]) && (rubrics.Count < 3 || stepOk[2]))
                {
                    resp.score = 10;
                    resp.passed = true;
                    resp.detectedMistakeType = "NONE";
                    resp.pedagogicalFeedback = "Tháº§y Minh khen ngá»£i: BÃ i giáº£i cá»§a em ráº¥t xuáº¥t sáº¯c! Em Ä‘Ã£ thá»±c hiá»‡n phÃ©p chia sá»‘ tháº­p phÃ¢n chuáº©n xÃ¡c vÃ  lÃ m trÃ²n Ä‘Ãºng quy táº¯c Ä‘áº¿n hÃ ng pháº§n mÆ°á»i.";
                    resp.remedialExercise = "Em Ä‘Ã£ lÃ m chá»§ dáº¡ng toÃ¡n sá»‘ tháº­p phÃ¢n!";
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
                    ? "TÃ­nh trong ngoáº·c: (3 + 2) = 5 (ChÃ­nh xÃ¡c)" 
                    : "TÃ­nh trong ngoáº·c: (3 + 2) = 5 (ChÆ°a thá»±c hiá»‡n hoáº·c tÃ­nh nháº§m phÃ©p tÃ­nh trong ngoáº·c)";

                // Kiem tra loi nham thu tu thuc hien phep tinh: 15 - 5 = 10 roi lay 10 x 2 = 20
                orderError = cleanText.Contains("10x2") || cleanText.Contains("=20") || cleanText.Contains("15-5=10");

                // Buoc 2: Nhan truoc 5 x 2 = 10
                if (rubrics.Count > 1)
                {
                    bool hasStep2 = cleanText.Contains("15-10") || cleanText.Contains("5x2=10");
                    if (hasStep2)
                    {
                        stepOk[1] = true;
                        stepComments[1] = "NhÃ¢n trÆ°á»›c: 5 Ã— 2 = 10 (ChÃ­nh xÃ¡c)";
                    }
                    else if (orderError)
                    {
                        stepOk[1] = false;
                        detectedMistake = "ORDER_OF_OPERATIONS";
                        stepComments[1] = "Cáº§n nhÃ¢n trÆ°á»›c: 5 Ã— 2 = 10 (Em Ä‘Ã£ láº¥y 15 - 5 = 10 trÆ°á»›c lÃ  sai thá»© tá»± Æ°u tiÃªn!)";
                    }
                    else
                    {
                        stepOk[1] = false;
                        stepComments[1] = "Cáº§n nhÃ¢n trÆ°á»›c: 5 Ã— 2 = 10 (ChÆ°a thá»±c hiá»‡n phÃ©p nhÃ¢n theo thá»© tá»± Æ°u tiÃªn)";
                    }
                }

                // Buoc 3: Tru sau 15 - 10 = 5
                if (rubrics.Count > 2)
                {
                    bool hasStep3 = CheckStudentAnswer(cleanText, "5");
                    stepOk[2] = hasStep3;
                    stepComments[2] = hasStep3 
                        ? "Trá»« sau: 15 - 10 = 5 (ChÃ­nh xÃ¡c)" 
                        : (orderError 
                            ? "Trá»« sau: 15 - 10 = 5 (Do bÆ°á»›c trÆ°á»›c tÃ­nh nháº§m thá»© tá»± nÃªn káº¿t quáº£ ra 20 thay vÃ¬ 5)" 
                            : "Trá»« sau: 15 - 10 = 5 (ChÆ°a Ä‘áº¡t Ä‘Ã¡p sá»‘ Ä‘Ãºng 5)");
                }

                if (stepOk[0] && stepOk[1] && stepOk[2])
                {
                    resp.score = 10;
                    resp.passed = true;
                    resp.detectedMistakeType = "NONE";
                    resp.pedagogicalFeedback = $"Tháº§y Minh khen ngá»£i: BÃ i giáº£i cá»§a em cho cÃ¢u '{GetShortPrompt(promptLower)}' ráº¥t chuáº©n xÃ¡c vÃ  rÃµ rÃ ng. Em Ä‘Ã£ thá»±c hiá»‡n Ä‘Ãºng quy táº¯c vÃ  Ä‘áº¡t Ä‘Ã¡p sá»‘ chÃ­nh xÃ¡c!";
                    resp.remedialExercise = "Em Ä‘Ã£ lÃ m chá»§ dáº¡ng toÃ¡n nÃ y! HÃ£y tiáº¿p tá»¥c rÃ¨n luyá»‡n thÃªm cÃ¡c cÃ¢u tiáº¿p theo nhÃ©.";
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
                    specificFeedback = "Tháº§y Minh nháº­n xÃ©t: Em Ä‘Ã£ lÃ m Ä‘Ãºng BÆ°á»›c 1 (tÃ­nh trong ngoáº·c: 3 + 2 = 5 Ä‘á»ƒ Ä‘Æ°á»£c biá»ƒu thá»©c 15 - 5 Ã— 2). Tuy nhiÃªn á»Ÿ bÆ°á»›c tiáº¿p theo, em Ä‘Ã£ nháº§m thá»© tá»± Æ°u tiÃªn khi láº¥y 15 - 5 = 10 trÆ°á»›c! HÃ£y nhá»› quy táº¯c cá»‘t lÃµi: 'NhÃ¢n chia trÆ°á»›c, cá»™ng trá»« sau' nhÃ©.";
                    remedialAdvice = "Gá»£i Ã½ cá»§a Tháº§y Minh: á»ž biá»ƒu thá»©c 15 - 5 Ã— 2, em hÃ£y thá»±c hiá»‡n phÃ©p nhÃ¢n 5 Ã— 2 = 10 trÆ°á»›c, sau Ä‘Ã³ má»›i láº¥y 15 - 10 nhÃ©. Báº¥m 'GIáº¢I Láº I CÃ‚U NÃ€Y' Ä‘á»ƒ thá»­ láº¡i!";
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
                    stepComments[2] = hasStep3 ? $"{rubrics[2].description} (ChÃ­nh xÃ¡c)" : $"{rubrics[2].description} (ChÆ°a Ä‘áº¡t Ä‘Ã¡p sá»‘ 0)";
                }

                if (stepOk[0] && (rubrics.Count < 2 || stepOk[1]) && (rubrics.Count < 3 || stepOk[2]))
                {
                    resp.score = 10;
                    resp.passed = true;
                    resp.detectedMistakeType = "NONE";
                    resp.pedagogicalFeedback = $"Tháº§y Minh khen ngá»£i: BÃ i giáº£i cá»§a em cho cÃ¢u '{GetShortPrompt(promptLower)}' ráº¥t chuáº©n xÃ¡c vÃ  rÃµ rÃ ng. Em Ä‘Ã£ thá»±c hiá»‡n Ä‘Ãºng quy táº¯c vÃ  Ä‘áº¡t Ä‘Ã¡p sá»‘ chÃ­nh xÃ¡c!";
                    resp.remedialExercise = "Em Ä‘Ã£ lÃ m chá»§ dáº¡ng toÃ¡n nÃ y! HÃ£y tiáº¿p tá»¥c rÃ¨n luyá»‡n thÃªm cÃ¡c cÃ¢u tiáº¿p theo nhÃ©.";
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
                    specificFeedback = "Tháº§y Minh nháº­n xÃ©t: Em Ä‘Ã£ biáº¿t cÃ¡ch nhÃ³m cÃ¡c sá»‘ háº¡ng má»™t cÃ¡ch há»£p lÃ½. HÃ£y cáº©n tháº­n tÃ­nh láº¡i tá»•ng hai sá»‘ nguyÃªn Ä‘á»‘i nhau (-100 + 100) nhÃ©!";
                    remedialAdvice = "Gá»£i Ã½ cá»§a Tháº§y Minh: Hai sá»‘ nguyÃªn Ä‘á»‘i nhau cÃ³ tá»•ng báº±ng 0. Em hÃ£y kiá»ƒm tra láº¡i vÃ  báº¥m 'GIáº¢I Láº I CÃ‚U NÃ€Y' nhÃ©.";
                }
            }
            // TRUONG HOP C: Bai toan ti so phan tram / phieu bau (Tan nhan duoc 120 phieu bau...)
            else if (normPrompt.Contains("120") && normPrompt.Contains("60%"))
            {
                bool hasStep1 = cleanText.Contains("120") && (cleanText.Contains("60") || cleanText.Contains("0.6") || cleanText.Contains("0,6"));
                stepOk[0] = hasStep1;
                stepComments[0] = hasStep1 
                    ? $"{rubrics[0].description} (ChÃ­nh xÃ¡c: Nháº­n diá»‡n Ä‘Ãºng bÃ i toÃ¡n tÃ¬m má»™t sá»‘ biáº¿t giÃ¡ trá»‹ phÃ¢n sá»‘)" 
                    : $"{rubrics[0].description} (ChÆ°a nháº­n diá»‡n Ä‘Ãºng dáº¡ng toÃ¡n)";

                bool multiplied = cleanText.Contains("120x60") || cleanText.Contains("120*60") || cleanText.Contains("=72");
                if (rubrics.Count > 1)
                {
                    if (multiplied)
                    {
                        stepOk[1] = false;
                        detectedMistake = "ORDER_OF_OPERATIONS";
                        stepComments[1] = $"{rubrics[1].description} (ChÆ°a chÃ­nh xÃ¡c: ÄÃ¢y lÃ  bÃ i toÃ¡n tÃ¬m má»™t sá»‘ khi biáº¿t 60% cá»§a nÃ³, ta pháº£i dÃ¹ng phÃ©p CHIA '120 : 60%', khÃ´ng dÃ¹ng phÃ©p nhÃ¢n)";
                    }
                    else
                    {
                        bool hasDiv = cleanText.Contains("120/0.6") || cleanText.Contains("120/60") || cleanText.Contains("120/(60/100)");
                        stepOk[1] = hasDiv;
                        stepComments[1] = hasDiv ? $"{rubrics[1].description} (ChÃ­nh xÃ¡c)" : $"{rubrics[1].description} (ChÆ°a thiáº¿t láº­p Ä‘Ãºng phÃ©p chia)";
                    }
                }

                if (rubrics.Count > 2)
                {
                    bool hasStep3 = CheckStudentAnswer(cleanText, "200");
                    stepOk[2] = hasStep3;
                    stepComments[2] = hasStep3 ? $"{rubrics[2].description} (ChÃ­nh xÃ¡c)" : $"{rubrics[2].description} (ChÆ°a tÃ­nh ra Ä‘Ã¡p sá»‘ chÃ­nh xÃ¡c 200 ngÆ°á»i)";
                }

                if (stepOk[0] && (rubrics.Count < 2 || stepOk[1]) && (rubrics.Count < 3 || stepOk[2]))
                {
                    resp.score = 10;
                    resp.passed = true;
                    resp.detectedMistakeType = "NONE";
                    resp.pedagogicalFeedback = $"Tháº§y Minh khen ngá»£i: BÃ i giáº£i cá»§a em cho cÃ¢u '{GetShortPrompt(promptLower)}' ráº¥t chuáº©n xÃ¡c vÃ  rÃµ rÃ ng. Em Ä‘Ã£ thá»±c hiá»‡n Ä‘Ãºng quy táº¯c vÃ  Ä‘áº¡t Ä‘Ã¡p sá»‘ chÃ­nh xÃ¡c!";
                    resp.remedialExercise = "Em Ä‘Ã£ lÃ m chá»§ dáº¡ng toÃ¡n nÃ y! HÃ£y tiáº¿p tá»¥c rÃ¨n luyá»‡n thÃªm cÃ¡c cÃ¢u tiáº¿p theo nhÃ©.";
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
                    specificFeedback = "Tháº§y Minh nháº­n xÃ©t: Em Ä‘Ã£ nháº§m láº«n giá»¯a hai bÃ i toÃ¡n phÃ¢n sá»‘: Äá»ƒ tÃ¬m tá»•ng sá»‘ phiáº¿u khi biáº¿t 60% tá»•ng sá»‘ phiáº¿u lÃ  120, ta pháº£i lÃ m phÃ©p CHIA: 120 : 60% (tá»©c 120 : 0,6), khÃ´ng pháº£i phÃ©p nhÃ¢n nhÃ©!";
                    remedialAdvice = "Gá»£i Ã½ cá»§a Tháº§y Minh: Muá»‘n tÃ¬m má»™t sá»‘ biáº¿t m/n cá»§a nÃ³ báº±ng a, ta tÃ­nh a : (m/n). Em hÃ£y láº¥y 120 : 0,6 vÃ  báº¥m 'GIáº¢I Láº I CÃ‚U NÃ€Y' nhÃ©!";
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
                        stepComments[i] = matched ? $"{r.description} (ChÃ­nh xÃ¡c)" : $"{r.description} (ChÆ°a thá»±c hiá»‡n Ä‘Ãºng)";
                    }
                    else if (i == rubrics.Count - 1)
                    {
                        bool matched = (!string.IsNullOrEmpty(expectedTarget) && CheckStudentAnswer(cleanText, expectedTarget)) ||
                                       (!string.IsNullOrEmpty(rAns) && CheckStudentAnswer(cleanText, rAns));
                        stepOk[i] = matched;
                        stepComments[i] = matched ? $"{r.description} (ChÃ­nh xÃ¡c)" : $"{r.description} (ChÆ°a Ä‘áº¡t Ä‘Ã¡p sá»‘ Ä‘Ãºng)";
                    }
                    else
                    {
                        bool matched = !string.IsNullOrEmpty(rAns) && cleanText.Contains(rAns);
                        stepOk[i] = matched;
                        stepComments[i] = matched ? $"{r.description} (ChÃ­nh xÃ¡c)" : $"{r.description} (ChÆ°a chÃ­nh xÃ¡c)";
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
                    resp.pedagogicalFeedback = $"Tháº§y Minh khen ngá»£i: BÃ i giáº£i cá»§a em cho cÃ¢u '{GetShortPrompt(promptLower)}' ráº¥t chuáº©n xÃ¡c vÃ  rÃµ rÃ ng. Em Ä‘Ã£ thá»±c hiá»‡n Ä‘Ãºng quy táº¯c vÃ  Ä‘áº¡t Ä‘Ã¡p sá»‘ chÃ­nh xÃ¡c!";
                    resp.remedialExercise = "Em Ä‘Ã£ lÃ m chá»§ dáº¡ng toÃ¡n nÃ y! HÃ£y tiáº¿p tá»¥c rÃ¨n luyá»‡n thÃªm cÃ¡c cÃ¢u tiáº¿p theo nhÃ©.";
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

            // XÃ¡c Ä‘á»‹nh dÃ²ng Ä‘áº§u tiÃªn bá»‹ sai & lÆ°u láº¡i cÃ¡c dÃ²ng Ä‘Ãºng phÃ­a trÆ°á»›c Ä‘á»ƒ há»c sinh sá»­a tiáº¿p
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

            // Náº¿u há»c sinh viáº¿t nhiá»u dÃ²ng, hiá»ƒn thá»‹ phÃ¢n tÃ­ch tá»«ng dÃ²ng thá»±c táº¿ cá»§a há»c sinh (khÃ´ng giá»›i háº¡n 3 bÆ°á»›c)
            if (studentLines.Length > 1)
            {
                resp.stepAnalysis.Clear();
                for (int i = 0; i < studentLines.Length; i++)
                {
                    bool isLineOk = firstWrongIndex < 0 || i < firstWrongIndex;
                    string lineComment = isLineOk 
                        ? $"DÃ²ng {i + 1}: {studentLines[i].Trim()} (ChÃ­nh xÃ¡c)" 
                        : (i == firstWrongIndex 
                            ? $"DÃ²ng {i + 1}: {studentLines[i].Trim()} (Sai á»Ÿ dÃ²ng nÃ y: {stepComments[Mathf.Clamp(i, 0, stepComments.Length - 1)]})" 
                            : $"DÃ²ng {i + 1}: {studentLines[i].Trim()} (ChÆ°a chÃ­nh xÃ¡c: Do dÃ²ng trÆ°á»›c tÃ­nh nháº§m)");

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
                var eqMatch = System.Text.RegularExpressions.Regex.Match(studentTextLower ?? "", @"(?:=|:\s*|Ä‘Ã¡p\s*sá»‘:?\s*|kq:?\s*)\s*" + escapedExp + @"(?!\d)");
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
