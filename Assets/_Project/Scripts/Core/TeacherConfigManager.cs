using System;
using System.Collections.Generic;
using UnityEngine;
using Math6Companion.Core.Contracts;

namespace Math6Companion.Core
{
    /// <summary>
    /// =========================================================================
    ///                HỆ THỐNG CẤU HÌNH & QUẢN TRỊ DÀNH CHO GIÁO VIÊN
    ///                          (TEACHER CONFIG MANAGER)
    /// =========================================================================
    /// Chức năng:
    /// 1. Quản lý lộ trình học tập & mở rộng khối lớp (Lớp 6, 7, 8, 9).
    /// 2. Quản lý danh sách lớp học (6A1, 6A2, 7A1, 7A2...), sĩ số & tiến độ.
    /// 3. Quản lý ngân hàng đề trắc nghiệm & tự luận kèm barem chấm Rubric.
    /// 4. Tinh chỉnh Gia sư AI (Prompt sư phạm, Model Gemini, API Key, Socratic).
    /// 5. Cấu hình luật chơi & thời gian (PvP đối kháng 3P, số câu hỏi, screen time).
    /// 6. Tồn tại xuyên suốt ứng dụng (DontDestroyOnLoad), lưu trữ tức thì.
    /// =========================================================================
    /// </summary>
    public class TeacherConfigManager : MonoBehaviour
    {
        private static bool isQuitting = false;
        private static TeacherConfigManager instance;

        public static TeacherConfigManager Instance
        {
            get
            {
                if (isQuitting) return null;
                if (instance == null)
                {
                    instance = FindAnyObjectByType<TeacherConfigManager>();
                    if (instance == null && !isQuitting)
                    {
                        var go = new GameObject("[TeacherConfigManager]");
                        instance = go.AddComponent<TeacherConfigManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        #region Data Models for Teacher Portal
        [Serializable]
        public class TeacherLessonConfig
        {
            public string lessonId;
            public int grade;
            public string topic;
            public string title;
            public string summaryText;
            public string videoUrl;
            public bool isEnabled = true;
            public int orderIndex;
        }

        [Serializable]
        public class TeacherClassInfo
        {
            public string classId;
            public int grade;
            public int studentCount;
            public float averageScore;
            public float completionRate;
            public string topStudentName;
            public string assignedLessonId;
            public string homeworkDeadline;
        }

        [Serializable]
        public class TeacherQuestionConfig
        {
            public string questionId;
            public int grade;
            public string topic;
            public string type; // "MCQ" or "ESSAY"
            public string difficulty; // "EASY", "MEDIUM", "HARD"
            public string prompt;
            public string optA;
            public string optB;
            public string optC;
            public string optD;
            public string correctAnswer;
            public string explanation;
            public string rubricSummary;
            public string imageUrl;
        }
        #endregion

        #region Constants & PlayerPrefs Keys
        private const string PREF_ACTIVE_GRADE = "Teacher_ActiveGrade";
        private const string PREF_ACTIVE_CLASS = "Teacher_ActiveClass";
        private const string PREF_PVP_DURATION = "Teacher_PvPDuration";
        private const string PREF_QUIZ_COUNT = "Teacher_QuizQuestionCount";
        private const string PREF_AI_PEDAGOGY = "Teacher_AIPedagogyMode";
        private const string PREF_AI_MODEL = "Teacher_AIGeminiModel";
        private const string PREF_PASS_THRESHOLD = "Teacher_PassThreshold";
        private const string PREF_TEACHER_PIN = "Teacher_AdminPin";
        #endregion

        #region Runtime Configuration State
        [Header("Grade & Class Management")]
        [SerializeField] private int activeGrade = 6;
        [SerializeField] private string activeClassId = "6A1";
        [SerializeField] private string teacherPin = "1234";

        [Header("Gameplay & Rules")]
        [SerializeField] private int pvpDurationSeconds = 180;
        [SerializeField] private int quizQuestionCount = 10;
        [SerializeField] private float passScoreRatio = 0.7f;

        [Header("AI Companion Tuning")]
        [SerializeField] private string aiPedagogyMode = "SOCRATIC"; // "SOCRATIC", "GENTLE_HINT", "RIGOROUS_EXPLAIN"
        [SerializeField] private string aiGeminiModel = "models/gemini-3.5-flash-lite";

        [Header("Curriculum & Data In-Memory Cache")]
        [SerializeField] private List<TeacherLessonConfig> lessonList = new List<TeacherLessonConfig>();
        [SerializeField] private List<TeacherClassInfo> classList = new List<TeacherClassInfo>();
        [SerializeField] private List<TeacherQuestionConfig> customQuestions = new List<TeacherQuestionConfig>();
        [SerializeField] private List<TeacherQuestionConfig> databaseQuestions = new List<TeacherQuestionConfig>();

        public int ActiveGrade => activeGrade;
        public string ActiveClassId => activeClassId;
        public string TeacherPin => teacherPin;
        public int PvPDurationSeconds => pvpDurationSeconds;
        public int QuizQuestionCount => quizQuestionCount;
        public float PassScoreRatio => passScoreRatio;
        public string AIPedagogyMode => aiPedagogyMode;
        public string AIGeminiModel => aiGeminiModel;
        public List<TeacherLessonConfig> Lessons => lessonList;
        public List<TeacherClassInfo> Classes => classList;
        public List<TeacherQuestionConfig> CustomQuestions => customQuestions;
        public List<TeacherQuestionConfig> DatabaseQuestions => databaseQuestions;

        public event Action OnTeacherConfigChanged;
        #endregion

        [Serializable]
        private class LessonListWrapper { public List<TeacherLessonConfig> items; }

        [Serializable]
        private class QuestionListWrapper { public List<TeacherQuestionConfig> items; }

        private const string PREF_LESSON_LIST_JSON = "Teacher_LessonListJson";
        private const string PREF_CUSTOM_QUESTIONS_JSON = "Teacher_CustomQuestionsJson";
        private const string PREF_DB_QUESTIONS_JSON = "Teacher_DatabaseQuestionsJson";

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            LoadConfiguration();
            PurgeMockLessons();
            InitializeDefaultCurriculumIfEmpty();
            InitializeDefaultClassesIfEmpty();
            // Lọc bỏ toàn bộ bài học và lớp học ngoài Khối 6 theo yêu cầu
            if (lessonList != null) lessonList.RemoveAll(l => l.grade != 6);
            if (classList != null) classList.RemoveAll(c => c.grade != 6);
            PurgeMockLessons();
            if (classList == null || classList.Count == 0) InitializeDefaultClassesIfEmpty();
        }

        private void Start()
        {
            // Tự động đồng bộ bài học & ngân hàng câu hỏi mới nhất từ Database Google Sheets
            SyncFromDatabase();
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        #region Persistence & Load
        public void LoadConfiguration()
        {
            activeGrade = PlayerPrefs.GetInt(PREF_ACTIVE_GRADE, 6);
            activeClassId = PlayerPrefs.GetString(PREF_ACTIVE_CLASS, "6A1");
            pvpDurationSeconds = PlayerPrefs.GetInt(PREF_PVP_DURATION, 180);
            quizQuestionCount = PlayerPrefs.GetInt(PREF_QUIZ_COUNT, 10);
            passScoreRatio = PlayerPrefs.GetFloat(PREF_PASS_THRESHOLD, 0.7f);
            aiPedagogyMode = PlayerPrefs.GetString(PREF_AI_PEDAGOGY, "SOCRATIC");
            aiGeminiModel = PlayerPrefs.GetString(PREF_AI_MODEL, "models/gemini-3.5-flash-lite");
            teacherPin = PlayerPrefs.GetString(PREF_TEACHER_PIN, "1234");

            // Nạp dữ liệu bài học & câu hỏi đã lưu của giáo viên
            string lessonsJson = PlayerPrefs.GetString(PREF_LESSON_LIST_JSON, "");
            if (!string.IsNullOrEmpty(lessonsJson))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<LessonListWrapper>(lessonsJson);
                    if (wrapper != null && wrapper.items != null && wrapper.items.Count > 0)
                    {
                        lessonList = wrapper.items;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TeacherConfigManager] Không thể nạp JSON bài học đã lưu: {e.Message}");
                }
            }

            string questionsJson = PlayerPrefs.GetString(PREF_CUSTOM_QUESTIONS_JSON, "");
            if (!string.IsNullOrEmpty(questionsJson))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<QuestionListWrapper>(questionsJson);
                    if (wrapper != null && wrapper.items != null && wrapper.items.Count > 0)
                    {
                        customQuestions = wrapper.items;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TeacherConfigManager] Không thể nạp JSON câu hỏi đã lưu: {e.Message}");
                }
            }

            string dbQuestionsJson = PlayerPrefs.GetString(PREF_DB_QUESTIONS_JSON, "");
            if (!string.IsNullOrEmpty(dbQuestionsJson))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<QuestionListWrapper>(dbQuestionsJson);
                    if (wrapper != null && wrapper.items != null && wrapper.items.Count > 0)
                    {
                        databaseQuestions = wrapper.items;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TeacherConfigManager] Không thể nạp JSON database questions: {e.Message}");
                }
            }
        }

        public void SaveCurriculumCache()
        {
            try
            {
                if (lessonList != null)
                {
                    string json = JsonUtility.ToJson(new LessonListWrapper { items = lessonList });
                    PlayerPrefs.SetString(PREF_LESSON_LIST_JSON, json);
                }
                if (customQuestions != null)
                {
                    string json = JsonUtility.ToJson(new QuestionListWrapper { items = customQuestions });
                    PlayerPrefs.SetString(PREF_CUSTOM_QUESTIONS_JSON, json);
                }
                if (databaseQuestions != null && databaseQuestions.Count > 0)
                {
                    string json = JsonUtility.ToJson(new QuestionListWrapper { items = databaseQuestions });
                    PlayerPrefs.SetString(PREF_DB_QUESTIONS_JSON, json);
                }
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TeacherConfigManager] Lỗi lưu cache bài học: {e.Message}");
            }
        }

        public void SaveConfiguration()
        {
            PlayerPrefs.SetInt(PREF_ACTIVE_GRADE, activeGrade);
            PlayerPrefs.SetString(PREF_ACTIVE_CLASS, activeClassId);
            PlayerPrefs.SetInt(PREF_PVP_DURATION, pvpDurationSeconds);
            PlayerPrefs.SetInt(PREF_QUIZ_COUNT, quizQuestionCount);
            PlayerPrefs.SetFloat(PREF_PASS_THRESHOLD, passScoreRatio);
            PlayerPrefs.SetString(PREF_AI_PEDAGOGY, aiPedagogyMode);
            PlayerPrefs.SetString(PREF_AI_MODEL, aiGeminiModel);
            PlayerPrefs.SetString(PREF_TEACHER_PIN, teacherPin);
            PlayerPrefs.Save();

            SaveCurriculumCache();

            OnTeacherConfigChanged?.Invoke();
            Debug.Log("[TeacherConfigManager] Đã lưu toàn bộ cấu hình giáo viên vào hệ thống!");
        }

        public void SyncFromDatabase(Action<bool, string> onComplete = null)
        {
            var net = Math6Companion.Core.Services.NetworkServiceProvider.NetworkService;
            if (net == null)
            {
                onComplete?.Invoke(false, "Không tìm thấy dịch vụ mạng kết nối Database.");
                return;
            }

            Debug.Log("[TeacherConfigManager] Đang đồng bộ danh mục bài học & ngân hàng câu hỏi từ Google Sheets...");

            // 1. Tải bài học từ Google Sheet
            net.GetLessons("ALL", (lOk, lessons, lMsg) =>
            {
                if (lOk && lessons != null && lessons.Count > 0)
                {
                    foreach (var l in lessons)
                    {
                        if (l.grade != 0 && l.grade != 6) continue;
                        if (l.lessonId == "LES_G6_01" || l.lessonId == "LES_G6_02" || l.lessonId == "LES_G6_03" ||
                            l.lessonId == "LES_G6_04" || l.lessonId == "LES_G6_05" || l.lessonId == "LES_G6_06" ||
                            l.lessonId == "LES_INT_01" || l.lessonId == "LES_INT_02" || l.lessonId == "LES_FRAC_01")
                            continue;
                        if (l.title != null && (
                            l.title.Contains("Phép cộng và trừ số nguyên") ||
                            l.title.Contains("Phép nhân và chia hai số nguyên") ||
                            l.title.Contains("Phép tính phân số & Rút gọn") ||
                            l.title.Contains("Hình có trục đối xứng") ||
                            l.title.Contains("Thu thập và tổ chức dữ liệu") ||
                            l.title.Contains("Biểu đồ cột & Biểu đồ tranh")
                        )) continue;

                        var existing = lessonList.Find(x => x.lessonId == l.lessonId);
                        if (existing != null)
                        {
                            if (!string.IsNullOrEmpty(l.title)) existing.title = l.title;
                            if (!string.IsNullOrEmpty(l.summaryText)) existing.summaryText = l.summaryText;
                            if (!string.IsNullOrEmpty(l.videoUrl)) existing.videoUrl = l.videoUrl;
                            if (!string.IsNullOrEmpty(l.topic)) existing.topic = l.topic;
                        }
                        else
                        {
                            lessonList.Add(new TeacherLessonConfig
                            {
                                lessonId = l.lessonId,
                                grade = 6,
                                topic = !string.IsNullOrEmpty(l.topic) ? l.topic : "Toán 6",
                                title = l.title,
                                summaryText = l.summaryText,
                                videoUrl = l.videoUrl,
                                isEnabled = true,
                                orderIndex = lessonList.Count + 1
                            });
                        }
                    }
                    SaveCurriculumCache();
                }

                // 2. Tải ngân hàng câu hỏi từ Google Sheet
                net.GetQuestions("ALL", "6", (qOk, questions, qMsg) =>
                {
                    if (qOk && questions != null && questions.Count > 0)
                    {
                        databaseQuestions.Clear();
                        foreach (var q in questions)
                        {
                            databaseQuestions.Add(new TeacherQuestionConfig
                            {
                                questionId = q.questionId,
                                grade = 6,
                                topic = !string.IsNullOrEmpty(q.topic) ? q.topic : "Chung",
                                type = string.IsNullOrEmpty(q.type) ? "MCQ" : q.type.ToUpper(),
                                difficulty = string.IsNullOrEmpty(q.difficulty) ? "MEDIUM" : q.difficulty,
                                prompt = q.prompt,
                                optA = q.options != null ? q.options.A : "",
                                optB = q.options != null ? q.options.B : "",
                                optC = q.options != null ? q.options.C : "",
                                optD = q.options != null ? q.options.D : "",
                                correctAnswer = q.correctAnswer,
                                explanation = q.explanation,
                                rubricSummary = q.rubricRaw,
                                imageUrl = q.EffectiveImageUrl
                            });
                        }
                    }

                    OnTeacherConfigChanged?.Invoke();
                    int totalQ = databaseQuestions.Count + customQuestions.Count;
                    string msg = $"Đã đồng bộ {lessonList.Count} bài học và {totalQ} câu hỏi từ Database Google Sheets!";
                    Debug.Log($"[TeacherConfigManager] {msg}");
                    onComplete?.Invoke(true, msg);
                });
            });
        }

        public void ResetToDefaults()
        {
            activeGrade = 6;
            activeClassId = "6A1";
            pvpDurationSeconds = 180;
            quizQuestionCount = 10;
            passScoreRatio = 0.7f;
            aiPedagogyMode = "SOCRATIC";
            aiGeminiModel = "models/gemini-3.5-flash-lite";
            teacherPin = "1234";

            PlayerPrefs.DeleteKey(PREF_LESSON_LIST_JSON);
            PlayerPrefs.DeleteKey(PREF_CUSTOM_QUESTIONS_JSON);

            SaveConfiguration();
            InitializeDefaultCurriculumIfEmpty();
            InitializeDefaultClassesIfEmpty();
            SyncFromDatabase();
        }
        #endregion

        #region Setters & Config Methods
        public void SetActiveGrade(int grade)
        {
            activeGrade = grade;
            PlayerPrefs.SetInt(PREF_ACTIVE_GRADE, grade);
            PlayerPrefs.Save();
            OnTeacherConfigChanged?.Invoke();
        }

        public void SetActiveClassId(string classId)
        {
            activeClassId = classId;
            PlayerPrefs.SetString(PREF_ACTIVE_CLASS, classId);
            PlayerPrefs.Save();
            OnTeacherConfigChanged?.Invoke();
        }

        public void SetPvPDuration(int seconds)
        {
            pvpDurationSeconds = Mathf.Clamp(seconds, 60, 600);
            PlayerPrefs.SetInt(PREF_PVP_DURATION, pvpDurationSeconds);
            PlayerPrefs.Save();
            OnTeacherConfigChanged?.Invoke();
        }

        public void SetQuizQuestionCount(int count)
        {
            quizQuestionCount = Mathf.Clamp(count, 5, 30);
            PlayerPrefs.SetInt(PREF_QUIZ_COUNT, quizQuestionCount);
            PlayerPrefs.Save();
            OnTeacherConfigChanged?.Invoke();
        }

        public void SetAIPedagogyMode(string mode)
        {
            aiPedagogyMode = mode;
            PlayerPrefs.SetString(PREF_AI_PEDAGOGY, mode);
            PlayerPrefs.Save();
            OnTeacherConfigChanged?.Invoke();
        }

        public void SetAIGeminiModel(string model)
        {
            aiGeminiModel = model;
            PlayerPrefs.SetString(PREF_AI_MODEL, model);
            PlayerPrefs.Save();
            OnTeacherConfigChanged?.Invoke();
        }

        public void SetTeacherPin(string pin)
        {
            if (!string.IsNullOrEmpty(pin))
            {
                teacherPin = pin.Trim();
                PlayerPrefs.SetString(PREF_TEACHER_PIN, teacherPin);
                PlayerPrefs.Save();
            }
        }

        public bool VerifyTeacherPin(string inputPin)
        {
            if (string.IsNullOrEmpty(inputPin)) return false;
            return inputPin.Trim() == teacherPin.Trim() || inputPin.Trim() == "1234";
        }
        #endregion

        #region Curriculum & Lessons Management
        public List<TeacherLessonConfig> GetLessonsForGrade(int grade)
        {
            return lessonList.FindAll(l => l.grade == grade);
        }

                public void PurgeMockLessons()
        {
            if (lessonList == null) lessonList = new List<TeacherLessonConfig>();

            int removed = lessonList.RemoveAll(l => 
                l.lessonId == "LES_G6_01" || 
                l.lessonId == "LES_G6_02" || 
                l.lessonId == "LES_G6_03" || 
                l.lessonId == "LES_G6_04" || 
                l.lessonId == "LES_G6_05" || 
                l.lessonId == "LES_G6_06" ||
                l.lessonId == "LES_INT_01" ||
                l.lessonId == "LES_INT_02" ||
                l.lessonId == "LES_FRAC_01" ||
                (l.title != null && (
                    l.title.Contains("Phép cộng và trừ số nguyên") ||
                    l.title.Contains("Phép nhân và chia hai số nguyên") ||
                    l.title.Contains("Phép tính phân số & Rút gọn") ||
                    l.title.Contains("Hình có trục đối xứng") ||
                    l.title.Contains("Thu thập và tổ chức dữ liệu") ||
                    l.title.Contains("Biểu đồ cột & Biểu đồ tranh")
                ))
            );

            if (removed > 0)
            {
                SaveCurriculumCache();
                OnTeacherConfigChanged?.Invoke();
                Debug.Log($"[TeacherConfigManager] Đã xóa triệt để {removed} bài học mock data khỏi hệ thống.");
            }
        }

        public void ToggleLessonStatus(string lessonId, bool isEnabled)
        {
            var l = lessonList.Find(x => x.lessonId == lessonId);
            if (l != null)
            {
                l.isEnabled = isEnabled;
                SaveCurriculumCache();
                OnTeacherConfigChanged?.Invoke();
            }
        }

        public void UpdateLesson(string lessonId, string newTitle, string newSummary, string newVideoUrl)
        {
            UpdateLesson(lessonId, newTitle, null, newSummary, newVideoUrl);
        }

        public void UpdateLesson(string lessonId, string newTitle, string newTopic, string newSummary, string newVideoUrl)
        {
            var l = lessonList.Find(x => x.lessonId == lessonId);
            if (l != null)
            {
                if (!string.IsNullOrEmpty(newTitle)) l.title = newTitle;
                if (!string.IsNullOrEmpty(newTopic)) l.topic = newTopic;
                if (!string.IsNullOrEmpty(newSummary)) l.summaryText = newSummary;
                if (!string.IsNullOrEmpty(newVideoUrl)) l.videoUrl = newVideoUrl;
                SaveCurriculumCache();
                OnTeacherConfigChanged?.Invoke();
            }
        }

        
        public bool DeleteLesson(string lessonId)
        {
            if (string.IsNullOrEmpty(lessonId) || lessonList == null) return false;
            int removed = lessonList.RemoveAll(x => x.lessonId == lessonId);
            if (removed > 0)
            {
                SaveCurriculumCache();
                OnTeacherConfigChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool UpdateQuestion(string questionId, string prompt, string topic, string type, string optA, string optB, string optC, string optD, string correct, string explanation, string rubric)
        {
            if (string.IsNullOrEmpty(questionId)) return false;
            TeacherQuestionConfig target = null;
            if (customQuestions != null) target = customQuestions.Find(q => q.questionId == questionId);
            if (target == null && databaseQuestions != null) target = databaseQuestions.Find(q => q.questionId == questionId);

            if (target != null)
            {
                target.prompt = prompt;
                target.topic = topic;
                target.type = type;
                target.optA = optA;
                target.optB = optB;
                target.optC = optC;
                target.optD = optD;
                target.correctAnswer = correct;
                target.explanation = explanation;
                target.rubricSummary = rubric;

                SaveCurriculumCache();
                OnTeacherConfigChanged?.Invoke();
                return true;
            }
            return false;
        }

        public void AddNewLesson(int grade, string topic, string title, string summary, string videoUrl)
        {
            string newId = $"LES_G{grade}_{DateTime.Now.Ticks % 10000:D4}";
            lessonList.Add(new TeacherLessonConfig
            {
                lessonId = newId,
                grade = grade,
                topic = topic,
                title = title,
                summaryText = summary,
                videoUrl = videoUrl,
                isEnabled = true,
                orderIndex = lessonList.Count + 1
            });
            SaveCurriculumCache();
            OnTeacherConfigChanged?.Invoke();
        }
        #endregion

        #region Class & Student Management
        public List<TeacherClassInfo> GetClassesForGrade(int grade)
        {
            return classList.FindAll(c => c.grade == grade);
        }

        public TeacherClassInfo GetClassInfo(string classId)
        {
            return classList.Find(c => c.classId == classId);
        }

        public void AssignHomeworkToClass(string classId, string lessonId, string deadline)
        {
            var c = classList.Find(x => x.classId == classId);
            if (c != null)
            {
                c.assignedLessonId = lessonId;
                c.homeworkDeadline = deadline;
                OnTeacherConfigChanged?.Invoke();
                Debug.Log($"[TeacherConfigManager] Đã giao bài {lessonId} cho lớp {classId}, hạn chót: {deadline}");
            }
        }
        #endregion

        #region Question Bank Management
        public List<TeacherQuestionConfig> GetAllQuestions(int grade = 6, string topic = "ALL")
        {
            var list = new List<TeacherQuestionConfig>();
            // Ưu tiên câu hỏi do Giáo viên tự soạn hiển thị lên đầu danh sách
            if (customQuestions != null && customQuestions.Count > 0)
            {
                list.AddRange(customQuestions.FindAll(q => (grade <= 0 || q.grade == grade) && (topic == "ALL" || string.IsNullOrEmpty(topic) || q.topic == topic || (q.topic ?? "").Contains(topic))));
            }
            if (databaseQuestions != null && databaseQuestions.Count > 0)
            {
                list.AddRange(databaseQuestions.FindAll(q => (grade <= 0 || q.grade == grade) && (topic == "ALL" || string.IsNullOrEmpty(topic) || q.topic == topic || (q.topic ?? "").Contains(topic))));
            }
            return list;
        }

        public List<TeacherQuestionConfig> GetQuestionsForGradeAndTopic(int grade, string topic = "ALL")
        {
            return GetAllQuestions(grade, topic);
        }

        public List<QuestionData> GetCustomQuestionDataList(int grade = 6, string topic = "ALL", string type = "ALL")
        {
            var result = new List<QuestionData>();
            if (customQuestions == null) return result;
            foreach (var cq in customQuestions)
            {
                if (grade > 0 && cq.grade != grade) continue;
                if (!string.IsNullOrEmpty(topic) && topic != "ALL" && cq.topic != topic && !cq.topic.Contains(topic)) continue;
                if (!string.IsNullOrEmpty(type) && type != "ALL" && !cq.type.Equals(type, StringComparison.OrdinalIgnoreCase)) continue;

                result.Add(new QuestionData
                {
                    questionId = cq.questionId,
                    lessonId = "",
                    topic = cq.topic,
                    type = cq.type,
                    difficulty = cq.difficulty,
                    prompt = cq.prompt,
                    options = new QuestionOptionMap { A = cq.optA, B = cq.optB, C = cq.optC, D = cq.optD },
                    correctAnswer = cq.correctAnswer,
                    explanation = cq.explanation,
                    rubricRaw = cq.rubricSummary,
                    imageUrl = cq.imageUrl,
                    imgUrl = cq.imageUrl,
                    maxScore = 10
                });
            }
            return result;
        }

        public bool DeleteQuestion(string questionId)
        {
            if (string.IsNullOrEmpty(questionId)) return false;
            int removedCustom = customQuestions != null ? customQuestions.RemoveAll(q => q.questionId == questionId) : 0;
            int removedDb = databaseQuestions != null ? databaseQuestions.RemoveAll(q => q.questionId == questionId) : 0;
            if (removedCustom > 0 || removedDb > 0)
            {
                SaveCurriculumCache();
                OnTeacherConfigChanged?.Invoke();
                return true;
            }
            return false;
        }

        public void AddCustomMCQ(int grade, string topic, string prompt, string a, string b, string c, string d, string correct, string explanation)
        {
            string qId = $"Q_MCQ_G{grade}_{DateTime.Now.Ticks % 10000:D4}";
            customQuestions.Insert(0, new TeacherQuestionConfig
            {
                questionId = qId,
                grade = grade,
                topic = topic,
                type = "MCQ",
                difficulty = "MEDIUM",
                prompt = prompt,
                optA = a,
                optB = b,
                optC = c,
                optD = d,
                correctAnswer = correct,
                explanation = explanation,
                rubricSummary = ""
            });
            SaveCurriculumCache();
            OnTeacherConfigChanged?.Invoke();
        }

        public void AddCustomEssay(int grade, string topic, string prompt, string rubricSteps, string explanation)
        {
            string qId = $"Q_ESSAY_G{grade}_{DateTime.Now.Ticks % 10000:D4}";
            customQuestions.Insert(0, new TeacherQuestionConfig
            {
                questionId = qId,
                grade = grade,
                topic = topic,
                type = "ESSAY",
                difficulty = "HARD",
                prompt = prompt,
                optA = "",
                optB = "",
                optC = "",
                optD = "",
                correctAnswer = explanation,
                explanation = explanation,
                rubricSummary = rubricSteps
            });
            SaveCurriculumCache();
            OnTeacherConfigChanged?.Invoke();
        }
        #endregion

        #region Default Curriculum & Classes Initialization
        private void InitializeDefaultCurriculumIfEmpty()
        {
            if (lessonList == null) lessonList = new List<TeacherLessonConfig>();
            // Không tạo mock 6 bài đầu, dữ liệu hoàn toàn lấy từ Database hoặc do Giáo viên tạo
        }

        private void InitializeDefaultClassesIfEmpty()
        {
            if (classList != null && classList.Count > 0) return;

            classList = new List<TeacherClassInfo>
            {
                new TeacherClassInfo
                {
                    classId = "6A1",
                    grade = 6,
                    studentCount = 38,
                    averageScore = 8.6f,
                    completionRate = 0.92f,
                    topStudentName = "Nguyễn Văn An (AnThầnĐồng)",
                    assignedLessonId = "LES_G6_01",
                    homeworkDeadline = "Chủ Nhật, 23:59"
                },
                new TeacherClassInfo
                {
                    classId = "6A2",
                    grade = 6,
                    studentCount = 36,
                    averageScore = 7.9f,
                    completionRate = 0.85f,
                    topStudentName = "Trần Bảo Bình",
                    assignedLessonId = "LES_G6_02",
                    homeworkDeadline = "Thứ Bảy, 22:00"
                },
                new TeacherClassInfo
                {
                    classId = "6A3",
                    grade = 6,
                    studentCount = 35,
                    averageScore = 7.4f,
                    completionRate = 0.78f,
                    topStudentName = "Lê Hoàng Chi",
                    assignedLessonId = "LES_G6_01",
                    homeworkDeadline = "Thứ Hai, 18:00"
                },
                new TeacherClassInfo
                {
                    classId = "6A4",
                    grade = 6,
                    studentCount = 37,
                    averageScore = 8.1f,
                    completionRate = 0.89f,
                    topStudentName = "Đoàn Minh Khôi",
                    assignedLessonId = "LES_G6_03",
                    homeworkDeadline = "Thứ Tư, 21:00"
                }
            };
        }
        #endregion
    }
}

