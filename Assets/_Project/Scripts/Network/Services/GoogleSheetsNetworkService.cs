// Assets/_Project/Scripts/Network/Services/GoogleSheetsNetworkService.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Math6Companion.Core.Contracts;
using Math6Companion.Core.Mock;

namespace Math6Companion.Core.Services
{
    /// <summary>
    /// Module 1: Dá»‹ch vá»¥ káº¿t ná»‘i Google Sheets qua Google Apps Script Web App REST API.
    /// Xá»­ lÃ½ ÄÄƒng nháº­p, Danh sÃ¡ch bÃ i há»c, NgÃ¢n hÃ ng cÃ¢u há»i, LÆ°u káº¿t quáº£ thi Ä‘áº¥u & Thá»‘ng kÃª há»c sinh.
    /// Tá»± Ä‘á»™ng Ä‘á»“ng bá»™ topic Tiáº¿ng Anh <-> Tiáº¿ng Viá»‡t, cache bá»™ nhá»› Ä‘á»‡m vÃ  chá»‘ng ngháº½n request kÃ©p.
    /// </summary>
    public class GoogleSheetsNetworkService : MonoBehaviour, INetworkService
    {
        private static bool isQuitting = false;
        private static GoogleSheetsNetworkService instance;
        public static GoogleSheetsNetworkService Instance
        {
            get
            {
                if (isQuitting) return null;
                if (instance == null)
                {
                    instance = FindAnyObjectByType<GoogleSheetsNetworkService>();
                    if (instance == null && !isQuitting)
                    {
                        var go = new GameObject("[GoogleSheetsNetworkService]");
                        instance = go.AddComponent<GoogleSheetsNetworkService>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("Backend Configuration")]
        [SerializeField] private float timeoutSeconds = 45f;
        [SerializeField] private bool fallbackToMockOnError = true;

        public string AppsScriptUrl
        {
            get => AppConfig.GetActiveBackendUrl();
            set => AppConfig.SetCustomBackendUrl(value);
        }

        private string appsScriptUrl => AppConfig.GetActiveBackendUrl();

        public static string ACTIVE_APPS_SCRIPT_URL => AppConfig.GetActiveBackendUrl();

        // Cookie jar dÃ¹ng chung â€” duy trÃ¬ cookie qua redirect chain cá»§a Google
        private static readonly System.Net.CookieContainer sharedCookieJar = new System.Net.CookieContainer();

        // Bá»™ nhá»› Ä‘á»‡m toÃ n cá»¥c (In-Memory Cache)
        private static List<LessonData> cachedLessons = new List<LessonData>();
        private static List<QuestionData> cachedAllQuestions = new List<QuestionData>();
        private static Dictionary<string, List<QuestionData>> cachedTopicQuestions = new Dictionary<string, List<QuestionData>>();

        // Quáº£n lÃ½ chá»‘ng trÃ¹ng láº·p request (Deduplication / In-Flight Request Coordinator)
        private static bool isFetchingLessons = false;
        private static readonly List<Action<bool, List<LessonData>, string>> pendingLessonCallbacks = new List<Action<bool, List<LessonData>, string>>();

        private static bool isFetchingQuestions = false;
        private static readonly List<Action<bool, List<QuestionData>, string>> pendingQuestionCallbacks = new List<Action<bool, List<QuestionData>, string>>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);

                // Cáº¥u hÃ¬nh TLS & Connection
                try
                {
                    System.Net.ServicePointManager.SecurityProtocol =
                        System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;
                    System.Net.ServicePointManager.DefaultConnectionLimit = 30;
                    System.Net.ServicePointManager.Expect100Continue = false;
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[GoogleSheetsNetworkService] TLS config warning: {e.Message}");
                }

                // KÃ­ch hoáº¡t warm-up thÃ´ng minh (dÃ¹ng chung pipeline GetLessons, khÃ´ng gá»­i request trÃ¹ng láº·p)
                GetLessons("ALL", (success, lessons, msg) =>
                {
                    if (success && lessons != null && lessons.Count > 0)
                    {
                        Debug.Log($"[NET-DIAG] âœ… SERVER Sáº´N SÃ€NG! ÄÃ£ náº¡p trÆ°á»›c {lessons.Count} bÃ i há»c tá»« Google Sheet.");
                    }
                });
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

        #region Core HTTP Methods

        /// <summary>
        /// Gá»­i HTTP GET chuáº©n Postman â€” bypass proxy, duy trÃ¬ cookie, auto redirect.
        /// </summary>
        private static string DoHttpGet(string url, int timeoutMs)
        {
            var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            req.Method = "GET";
            req.Proxy = null;
            req.CookieContainer = sharedCookieJar;
            req.KeepAlive = false;

            req.UserAgent = "PostmanRuntime/7.36.0";
            req.Accept = "*/*";
            req.AllowAutoRedirect = true;
            req.MaximumAutomaticRedirections = 10;
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            req.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;

            using (var resp = (System.Net.HttpWebResponse)req.GetResponse())
            using (var stream = resp.GetResponseStream())
            using (var reader = new System.IO.StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Gá»­i HTTP POST chuáº©n Postman â€” bypass proxy, duy trÃ¬ cookie, auto redirect.
        /// </summary>
        private static string DoHttpPost(string url, string jsonBody, int timeoutMs)
        {
            var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/json; charset=utf-8";
            req.Proxy = null;
            req.CookieContainer = sharedCookieJar;
            req.KeepAlive = false;

            req.UserAgent = "PostmanRuntime/7.36.0";
            req.Accept = "*/*";
            req.AllowAutoRedirect = true;
            req.MaximumAutomaticRedirections = 10;
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            req.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;

            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            req.ContentLength = bodyBytes.Length;
            using (var reqStream = req.GetRequestStream())
            {
                reqStream.Write(bodyBytes, 0, bodyBytes.Length);
            }

            using (var resp = (System.Net.HttpWebResponse)req.GetResponse())
            using (var stream = resp.GetResponseStream())
            using (var reader = new System.IO.StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        #endregion

        #region INetworkService Implementation

        public void Login(string username, string passwordOrHash, Action<bool, UserData, string> callback)
        {
            string url = $"{appsScriptUrl}?action=login&username={Uri.EscapeDataString(username ?? "")}&password={Uri.EscapeDataString(passwordOrHash ?? "")}";
            StartCoroutine(RunGetRequest<LoginResponseWrapper>(url, (success, resp, raw) =>
            {
                if (success && resp != null && resp.success && resp.data != null)
                {
                    callback?.Invoke(true, resp.data, resp.message ?? "ÄÄƒng nháº­p thÃ nh cÃ´ng!");
                }
                else if (resp != null && !resp.success && !string.IsNullOrEmpty(resp.message))
                {
                    // Lỗi xác thực từ máy chủ Google Sheets (ví dụ: Mật khẩu không chính xác!)
                    // Phải trả lỗi trực tiếp về UI, TUYỆT ĐỐI KHÔNG fallback sang Mock!
                    callback?.Invoke(false, null, resp.message);
                }
                else
                {
                    if (fallbackToMockOnError)
                    {
                        Debug.LogWarning($"[GoogleSheetsNetworkService] Apps Script Login ({resp?.message ?? raw}) -> Fallback to Mock.");
                        MockNetworkService.Instance.Login(username, passwordOrHash, callback);
                    }
                    else
                    {
                        callback?.Invoke(false, null, resp?.message ?? "KhÃ´ng thá»ƒ káº¿t ná»‘i Ä‘áº¿n mÃ¡y chá»§ Google Sheets!");
                    }
                }
            }));
        }

        public void GetLessons(string topic, Action<bool, List<LessonData>, string> callback)
        {
            // 1. Náº¿u Ä‘Ã£ cÃ³ trong Cache, lá»c theo topic vÃ  tráº£ vá» ngay tá»©c thÃ¬ (0ms)!
            if (cachedLessons != null && cachedLessons.Count > 0)
            {
                var filtered = FilterLessonsByTopic(cachedLessons, topic);
                var returnList = (filtered != null && filtered.Count > 0) ? filtered : cachedLessons;
                callback?.Invoke(true, returnList, "Náº¡p bÃ i há»c tá»« bá»™ nhá»› Ä‘á»‡m thÃ nh cÃ´ng!");
                return;
            }

            // 2. Náº¿u Ä‘ang cÃ³ má»™t request láº¥y bÃ i há»c Ä‘ang cháº¡y, Ä‘Äƒng kÃ½ nháº­n chung káº¿t quáº£ (chá»‘ng gá»­i request trÃ¹ng láº·p)
            if (callback != null)
            {
                pendingLessonCallbacks.Add((success, lessons, msg) =>
                {
                    if (success && lessons != null && lessons.Count > 0)
                    {
                        var filtered = FilterLessonsByTopic(lessons, topic);
                        callback.Invoke(true, (filtered != null && filtered.Count > 0) ? filtered : lessons, msg);
                    }
                    else
                    {
                        callback.Invoke(success, lessons, msg);
                    }
                });
            }

            if (isFetchingLessons)
            {
                return; // ÄÃ£ cÃ³ request Ä‘ang cháº¡y, chá»‰ cáº§n Ä‘á»£i nháº­n callback chung
            }

            // 3. Báº¯t Ä‘áº§u gá»­i HTTP request láº¥y danh sÃ¡ch bÃ i há»c
            isFetchingLessons = true;
            string url = $"{appsScriptUrl}?action=getLessons";

            StartCoroutine(RunGetRequest<LessonsResponseWrapper>(url, (success, resp, raw) =>
            {
                isFetchingLessons = false;
                var callbacksToInvoke = new List<Action<bool, List<LessonData>, string>>(pendingLessonCallbacks);
                pendingLessonCallbacks.Clear();

                if (success && resp != null && resp.success && resp.lessons != null && resp.lessons.Count > 0)
                {
                    cachedLessons = resp.lessons;
                    foreach (var cb in callbacksToInvoke)
                    {
                        cb?.Invoke(true, cachedLessons, "Táº£i bÃ i há»c thÃ nh cÃ´ng!");
                    }
                }
                else
                {
                    if (cachedLessons != null && cachedLessons.Count > 0)
                    {
                        foreach (var cb in callbacksToInvoke)
                        {
                            cb?.Invoke(true, cachedLessons, "Náº¡p tá»« bá»™ nhá»› Ä‘á»‡m thÃ nh cÃ´ng!");
                        }
                    }
                    else if (fallbackToMockOnError)
                    {
                        Debug.LogWarning($"[GoogleSheetsNetworkService] GetLessons khÃ´ng thÃ nh cÃ´ng, sá»­ dá»¥ng dá»¯ liá»‡u cá»¥c bá»™.");
                        MockNetworkService.Instance.GetLessons(topic, (mockOk, mockList, mockMsg) =>
                        {
                            foreach (var cb in callbacksToInvoke)
                            {
                                cb?.Invoke(mockOk, mockList, mockMsg);
                            }
                        });
                    }
                    else
                    {
                        foreach (var cb in callbacksToInvoke)
                        {
                            cb?.Invoke(false, null, "KhÃ´ng thá»ƒ táº£i danh sÃ¡ch bÃ i há»c tá»« Google Sheets!");
                        }
                    }
                }
            }));
        }

        public void GetQuestions(string topic, string grade, Action<bool, List<QuestionData>, string> callback)
        {
            string cacheKey = $"{topic ?? "ALL"}_{grade ?? "6"}";

            // 1. Kiá»ƒm tra cache cÃ¢u há»i theo topic
            if (cachedTopicQuestions.TryGetValue(cacheKey, out var topicList) && topicList.Count > 0)
            {
                callback?.Invoke(true, topicList, "Náº¡p cÃ¢u há»i tá»« bá»™ nhá»› Ä‘á»‡m thÃ nh cÃ´ng!");
                return;
            }

            // 2. Náº¿u Ä‘Ã£ cÃ³ toÃ n bá»™ cÃ¢u há»i trong RAM, lá»c ngay
            if (cachedAllQuestions != null && cachedAllQuestions.Count > 0)
            {
                var filtered = FilterQuestionsByTopic(cachedAllQuestions, topic);
                if (filtered != null && filtered.Count > 0)
                {
                    cachedTopicQuestions[cacheKey] = filtered;
                    callback?.Invoke(true, filtered, "Náº¡p cÃ¢u há»i theo chá»§ Ä‘á» thÃ nh cÃ´ng!");
                }
                else
                {
                    callback?.Invoke(true, new List<QuestionData>(), "ChÆ°a cÃ³ cÃ¢u há»i cho chá»§ Ä‘á» nÃ y trÃªn mÃ¡y chá»§.");
                }
                return;
            }

            // 3. ÄÄƒng kÃ½ nháº­n chung káº¿t quáº£ náº¿u Ä‘ang cÃ³ request táº£i cÃ¢u há»i
            if (callback != null)
            {
                pendingQuestionCallbacks.Add((success, questions, msg) =>
                {
                    if (success && questions != null && questions.Count > 0)
                    {
                        var filtered = FilterQuestionsByTopic(questions, topic);
                        if (filtered != null && filtered.Count > 0)
                        {
                            cachedTopicQuestions[cacheKey] = filtered;
                            callback.Invoke(true, filtered, msg);
                        }
                        else
                        {
                            callback.Invoke(true, new List<QuestionData>(), msg);
                        }
                    }
                    else
                    {
                        callback.Invoke(success, questions, msg);
                    }
                });
            }

            if (isFetchingQuestions)
            {
                return; // Äá»£i request Ä‘ang cháº¡y
            }

            // 4. Táº£i toÃ n bá»™ kho cÃ¢u há»i tá»« Google Sheet (tá»‘i Ä‘a 1000 cÃ¢u Ä‘á»ƒ náº¡p trá»n váº¹n ngÃ¢n hÃ ng Ä‘á»)
            isFetchingQuestions = true;
            string url = $"{appsScriptUrl}?action=getQuestions&limit=1000";

            StartCoroutine(RunGetRequest<QuestionsResponseWrapper>(url, (success, resp, raw) =>
            {
                isFetchingQuestions = false;
                var callbacksToInvoke = new List<Action<bool, List<QuestionData>, string>>(pendingQuestionCallbacks);
                pendingQuestionCallbacks.Clear();

                if (success && resp != null && resp.success && resp.questions != null && resp.questions.Count > 0)
                {
                    foreach (var q in resp.questions)
                    {
                        if (q.rubric == null || q.rubric.Count == 0)
                        {
                            q.rubric = ParseRubricRaw(q.rubricRaw);
                        }
                        if (q.options != null)
                        {
                            q.options.A = SanitizeMathText(q.options.A);
                            q.options.B = SanitizeMathText(q.options.B);
                            q.options.C = SanitizeMathText(q.options.C);
                            q.options.D = SanitizeMathText(q.options.D);
                        }
                        q.prompt = SanitizeMathText(q.prompt);
                        q.explanation = SanitizeMathText(q.explanation);
                        if (string.IsNullOrEmpty(q.imageUrl) && !string.IsNullOrEmpty(q.imgUrl)) q.imageUrl = q.imgUrl;
                        if (!string.IsNullOrEmpty(q.imageUrl)) q.imageUrl = q.imageUrl.Trim();
                    }

                    cachedAllQuestions = resp.questions;

                    foreach (var cb in callbacksToInvoke)
                    {
                        cb?.Invoke(true, cachedAllQuestions, "Táº£i cÃ¢u há»i thÃ nh cÃ´ng!");
                    }
                }
                else
                {
                    if (cachedAllQuestions != null && cachedAllQuestions.Count > 0)
                    {
                        foreach (var cb in callbacksToInvoke)
                        {
                            cb?.Invoke(true, cachedAllQuestions, "Náº¡p cÃ¢u há»i tá»« bá»™ nhá»› Ä‘á»‡m!");
                        }
                    }
                    else if (fallbackToMockOnError)
                    {
                        Debug.LogWarning($"[GoogleSheetsNetworkService] GetQuestions khÃ´ng thÃ nh cÃ´ng, sá»­ dá»¥ng ngÃ¢n hÃ ng cÃ¢u há»i cá»¥c bá»™.");
                        MockNetworkService.Instance.GetQuestions(topic, grade, (mockOk, mockList, mockMsg) =>
                        {
                            foreach (var cb in callbacksToInvoke)
                            {
                                cb?.Invoke(mockOk, mockList, mockMsg);
                            }
                        });
                    }
                    else
                    {
                        foreach (var cb in callbacksToInvoke)
                        {
                            cb?.Invoke(false, null, "KhÃ´ng thá»ƒ táº£i cÃ¢u há»i tá»« Google Sheets!");
                        }
                    }
                }
            }));
        }

        public void SaveResult(ResultSubmission submission, Action<bool, List<BadgeData>, string> callback)
        {
            string jsonBody = JsonUtility.ToJson(submission);
            string postUrl = $"{appsScriptUrl}?action=saveResult";
            StartCoroutine(RunPostRequest<SaveResultResponseWrapper>(postUrl, jsonBody, (success, resp, raw) =>
            {
                // Invalidate cached lessons to ensure updated high score and top stats on next load
                cachedLessons.Clear();

                if (success && resp != null && resp.success)
                {
                    callback?.Invoke(true, resp.unlockedBadges ?? new List<BadgeData>(), "ÄÃ£ lÆ°u káº¿t quáº£ thÃ nh cÃ´ng!");
                }
                else
                {
                    if (fallbackToMockOnError)
                    {
                        Debug.LogWarning($"[GoogleSheetsNetworkService] SaveResult fallback to local mock.");
                        MockNetworkService.Instance.SaveResult(submission, callback);
                    }
                    else
                    {
                        callback?.Invoke(false, null, "KhÃ´ng thá»ƒ lÆ°u káº¿t quáº£ vÃ o Google Sheets!");
                    }
                }
            }));
        }

        public void LogAITutor(AITutorLogSubmission logSubmission, Action<bool, string> callback)
        {
            if (logSubmission == null)
            {
                callback?.Invoke(false, "Log payload is null");
                return;
            }

            string jsonBody = JsonUtility.ToJson(logSubmission);
            string postUrl = $"{appsScriptUrl}?action=logAITutor";
            StartCoroutine(RunPostRequest<BaseResponseWrapper>(postUrl, jsonBody, (success, resp, raw) =>
            {
                if (success && resp != null && resp.success)
                {
                    callback?.Invoke(true, resp.message ?? "ÄÃ£ ghi nháº­n AI Tutor log lÃªn Google Sheets!");
                }
                else
                {
                    if (fallbackToMockOnError)
                    {
                        MockNetworkService.Instance.LogAITutor(logSubmission, callback);
                    }
                    else
                    {
                        callback?.Invoke(false, "KhÃ´ng thá»ƒ ghi log AI lÃªn Google Sheets");
                    }
                }
            }));
        }

        public static void InvalidateCache()
        {
            cachedLessons.Clear();
            cachedTopicQuestions.Clear();
            cachedAllQuestions.Clear();
        }

        public void GetStudentAnalytics(string studentId, Action<bool, StudentAnalyticsData, string> callback)
        {
            string url = $"{appsScriptUrl}?action=getStudentAnalytics&userId={Uri.EscapeDataString(studentId ?? "")}";
            StartCoroutine(RunGetRequest<AnalyticsResponseWrapper>(url, (success, resp, raw) =>
            {
                if (success && resp != null && resp.success && resp.analytics != null)
                {
                    callback?.Invoke(true, resp.analytics, "Táº£i bÃ¡o cÃ¡o phÃ¢n tÃ­ch thÃ nh cÃ´ng!");
                }
                else
                {
                    if (fallbackToMockOnError)
                    {
                        MockNetworkService.Instance.GetStudentAnalytics(studentId, callback);
                    }
                    else
                    {
                        callback?.Invoke(false, null, "KhÃ´ng thá»ƒ táº£i phÃ¢n tÃ­ch tá»« Google Sheets!");
                    }
                }
            }));
        }

        public void JoinVersusQueue(string userId, string nickname, string mode, Action<bool, VersusQueueResponse, string> callback)
        {
            string url = $"{appsScriptUrl}?action=joinVersusQueue&userId={Uri.EscapeDataString(userId ?? "")}&nickname={Uri.EscapeDataString(nickname ?? "")}&mode={Uri.EscapeDataString(mode ?? "MCQ")}";
            StartCoroutine(RunGetRequest<VersusQueueResponse>(url, (success, resp, raw) =>
            {
                if (success && resp != null && resp.success)
                {
                    callback?.Invoke(true, resp, resp.message ?? "OK");
                }
                else
                {
                    if (fallbackToMockOnError)
                    {
                        MockNetworkService.Instance.JoinVersusQueue(userId, nickname, mode, callback);
                    }
                    else
                    {
                        callback?.Invoke(false, null, raw ?? "Lỗi ghép trận!");
                    }
                }
            }));
        }

        public void PollVersusQueue(string userId, Action<bool, VersusQueueResponse, string> callback)
        {
            string url = $"{appsScriptUrl}?action=pollVersusQueue&userId={Uri.EscapeDataString(userId ?? "")}";
            StartCoroutine(RunGetRequest<VersusQueueResponse>(url, (success, resp, raw) =>
            {
                if (success && resp != null && resp.success)
                {
                    callback?.Invoke(true, resp, resp.message ?? "OK");
                }
                else
                {
                    if (fallbackToMockOnError)
                    {
                        MockNetworkService.Instance.PollVersusQueue(userId, callback);
                    }
                    else
                    {
                        callback?.Invoke(false, null, raw ?? "Lỗi poll queue!");
                    }
                }
            }));
        }

        public void CancelVersusQueue(string userId, Action<bool, string> callback)
        {
            string url = $"{appsScriptUrl}?action=cancelVersusQueue&userId={Uri.EscapeDataString(userId ?? "")}";
            StartCoroutine(RunGetRequest<BaseResponseWrapper>(url, (success, resp, raw) =>
            {
                callback?.Invoke(success, raw);
            }));
        }

        public void SubmitVersusScore(string roomId, string userId, int score, Action<bool, VersusRoomResultResponse, string> callback)
        {
            string url = $"{appsScriptUrl}?action=submitVersusScore&roomId={Uri.EscapeDataString(roomId ?? "")}&userId={Uri.EscapeDataString(userId ?? "")}&score={score}";
            StartCoroutine(RunGetRequest<VersusRoomResultResponse>(url, (success, resp, raw) =>
            {
                if (success && resp != null && resp.success)
                {
                    callback?.Invoke(true, resp, resp.message ?? "OK");
                }
                else
                {
                    if (fallbackToMockOnError)
                    {
                        MockNetworkService.Instance.SubmitVersusScore(roomId, userId, score, callback);
                    }
                    else
                    {
                        callback?.Invoke(false, null, raw ?? "Lỗi nộp điểm!");
                    }
                }
            }));
        }

        public void PollVersusRoomResult(string roomId, string userId, Action<bool, VersusRoomResultResponse, string> callback)
        {
            string url = $"{appsScriptUrl}?action=pollVersusRoomResult&roomId={Uri.EscapeDataString(roomId ?? "")}&userId={Uri.EscapeDataString(userId ?? "")}";
            StartCoroutine(RunGetRequest<VersusRoomResultResponse>(url, (success, resp, raw) =>
            {
                if (success && resp != null && resp.success)
                {
                    callback?.Invoke(true, resp, resp.message ?? "OK");
                }
                else
                {
                    if (fallbackToMockOnError)
                    {
                        MockNetworkService.Instance.PollVersusRoomResult(roomId, userId, callback);
                    }
                    else
                    {
                        callback?.Invoke(false, null, raw ?? "Lỗi poll kết quả phòng!");
                    }
                }
            }));
        }

        #endregion

        #region Topic Mapping & Filtering Logic

        /// <summary>
        /// Kiá»ƒm tra Ä‘á»™ khá»›p chá»§ Ä‘á» giá»¯a cÃ¢u há»i/bÃ i há»c vá»›i chá»§ Ä‘á» Ä‘Æ°á»£c yÃªu cáº§u.
        /// Há»— trá»£ map linh hoáº¡t cáº£ tiáº¿ng Anh vÃ  tiáº¿ng Viá»‡t:
        /// Integers <-> SoNguyen, Fractions <-> PhanSo, Sets <-> TapHop, Decimals <-> SoThapPhan.
        /// </summary>
        public static bool IsTopicMatch(string itemTopic, string requestedTopic)
        {
            if (string.IsNullOrEmpty(requestedTopic) || requestedTopic.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.IsNullOrEmpty(itemTopic))
                return false;

            if (itemTopic.Equals(requestedTopic, StringComparison.OrdinalIgnoreCase))
                return true;

            string normItem = NormalizeTopic(itemTopic);
            string normReq = NormalizeTopic(requestedTopic);

            return normItem.Equals(normReq, StringComparison.OrdinalIgnoreCase) ||
                   normItem.Contains(normReq) ||
                   normReq.Contains(normItem);
        }

        private static string NormalizeTopic(string topic)
        {
            if (string.IsNullOrEmpty(topic)) return "";
            string t = topic.Trim().ToLowerInvariant()
                .Replace(" ", "").Replace("_", "").Replace("-", "");

            // 1. Thá»© tá»± thá»±c hiá»‡n phÃ©p tÃ­nh vs Thá»© tá»± sá»‘ tá»± nhiÃªn vs PhÃ©p tÃ­nh thÃ´ng thÆ°á»ng
            if (t.Contains("thutupheptinh") || t.Contains("thá»©tá»±phÃ©ptÃ­nh") || t.Contains("thutupheptoan") || t.Contains("bieuthuc") || t.Contains("biá»ƒuthá»©c")) return "thutupheptinh";
            if (t.Contains("thutu") || t.Contains("thá»©tá»±")) return "thutu";
            if (t.Contains("pheptinh") || t.Contains("phÃ©ptÃ­nh") || t.Contains("phepcong") || t.Contains("phepnhan") || t.Contains("phepchia") || t.Contains("pheptru")) return "pheptinh";

            // 2. Ghi sá»‘ & Chá»¯ sá»‘ La MÃ£
            if (t.Contains("ghiso") || t.Contains("ghisá»‘") || t.Contains("chuso") || t.Contains("chá»¯sá»‘") || t.Contains("lama") || t.Contains("lamÃ£")) return "ghiso";

            // 3. Luá»¹ thá»«a
            if (t.Contains("luythua") || t.Contains("luá»¹thá»«a") || t.Contains("lÅ©ythá»«a") || t.Contains("somu") || t.Contains("sá»‘mÅ©") || t.Contains("power")) return "luythua";

            // 4. TÃ­nh chia háº¿t (Æ¯á»›c, bá»™i, sá»‘ nguyÃªn tá»‘, há»£p sá»‘, Æ¯CLN, BCNN)
            if (t.Contains("tinhchiahet") || t.Contains("chiahet") || t.Contains("chiaháº¿t") || t.Contains("dauhieu") || t.Contains("dáº¥uhiá»‡u") || t.Contains("uocvaboi") || t.Contains("Æ°á»›cvÃ bá»™i") || t.Contains("ucln") || t.Contains("bcnn") || t.Contains("uoc") || t.Contains("Æ°á»›c") || t.Contains("boi") || t.Contains("bá»™i") || t.Contains("songuyento") || t.Contains("sá»‘nguyÃªntá»‘") || t.Contains("nguyento") || t.Contains("nguyÃªntá»‘") || t.Contains("hopso") || t.Contains("há»£psá»‘")) return "tinhchiahet";

            // 5. Sá»‘ nguyÃªn
            if (t.Contains("integer") || t.Contains("songuyen") || t.Contains("sá»‘nguyÃªn") || t.Contains("nguyenam") || t.Contains("nguyÃªnÃ¢m") || t.Contains("trucso") || t.Contains("trá»¥csá»‘")) return "songuyen";

            // 6. Táº­p há»£p
            if (t.Contains("set") || t.Contains("taphop") || t.Contains("táº­phá»£p") || t.Contains("phantu") || t.Contains("pháº§ntá»­") || t.Contains("sotunhien") || t.Contains("sá»‘tá»±nhiÃªn")) return "taphop";

            // 7. HÃ¬nh pháº³ng & TÃ­nh Ä‘á»‘i xá»©ng & HÃ¬nh há»c trá»±c quan
            if (t.Contains("tinhdoixung") || t.Contains("doixung") || t.Contains("Ä‘á»‘ixá»©ng") || t.Contains("trucdoixung") || t.Contains("tamdoixung")) return "tinhdoixung";
            if (t.Contains("hinhphang") || t.Contains("hÃ¬nhpháº³ng") || t.Contains("tamgiac") || t.Contains("tamgiÃ¡c") || t.Contains("hinhvuong") || t.Contains("hÃ¬nhvuÃ´ng") || t.Contains("hinhchunhat") || t.Contains("hÃ¬nhchá»¯nháº­t") || t.Contains("hinhthoi") || t.Contains("hÃ¬nhthoi") || t.Contains("hinhbinhhanh") || t.Contains("hÃ¬nhbÃ¬nhhÃ nh") || t.Contains("hinhthang") || t.Contains("hÃ¬nhthang") || t.Contains("lucgiac") || t.Contains("lá»¥cgiÃ¡c")) return "hinhphang";
            if (t.Contains("geometry") || t.Contains("hinhhoc") || t.Contains("hÃ¬nhhá»c") || t.Contains("doanthang") || t.Contains("Ä‘oáº¡ntháº³ng") || t.Contains("trungdiem") || t.Contains("trungÄ‘iá»ƒm") || t.Contains("goc") || t.Contains("gÃ³c") || t.Contains("duongthang") || t.Contains("Ä‘Æ°á»ngtháº³ng") || t.Contains("diem") || t.Contains("Ä‘iá»ƒm")) return "hinhhoc";

            // 8. PhÃ¢n sá»‘ & Sá»‘ tháº­p phÃ¢n
            if (t.Contains("fraction") || t.Contains("phanso") || t.Contains("phÃ¢nsá»‘") || t.Contains("honso") || t.Contains("há»—nsá»‘")) return "phanso";
            if (t.Contains("decimal") || t.Contains("sothapphan") || t.Contains("thapphan") || t.Contains("tháº­pphÃ¢n") || t.Contains("phantram") || t.Contains("pháº§ntrÄƒm") || t.Contains("tiso") || t.Contains("tá»‰sá»‘")) return "sothapphan";

            // 9. Thá»‘ng kÃª & XÃ¡c suáº¥t
            if (t.Contains("thongke") || t.Contains("thá»‘ngkÃª") || t.Contains("xacsuat") || t.Contains("xÃ¡csuáº¥t") || t.Contains("dulieu") || t.Contains("dá»¯liá»‡u") || t.Contains("bieudo") || t.Contains("biá»ƒuÄ‘á»“")) return "thongke";

            if (t.Contains("equation") || t.Contains("phuongtrinh") || t.Contains("phÆ°Æ¡ngtrÃ¬nh")) return "phuongtrinh";

            return t;
        }

        private static List<LessonData> FilterLessonsByTopic(List<LessonData> list, string topic)
        {
            if (list == null) return new List<LessonData>();
            if (string.IsNullOrEmpty(topic) || topic.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                return list;

            var result = new List<LessonData>();
            foreach (var item in list)
            {
                if (IsTopicMatch(item.topic, topic))
                {
                    result.Add(item);
                }
            }
            return result;
        }

        private static List<QuestionData> FilterQuestionsByTopic(List<QuestionData> list, string topic)
        {
            if (list == null) return new List<QuestionData>();
            if (string.IsNullOrEmpty(topic) || topic.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                return list;

            var result = new List<QuestionData>();
            foreach (var item in list)
            {
                if (IsTopicMatch(item.topic, topic))
                {
                    result.Add(item);
                }
            }
            return result;
        }

        public static bool IsDifficultyMatch(string itemDifficulty, string requestedDifficulty)
        {
            if (string.IsNullOrEmpty(requestedDifficulty) || requestedDifficulty.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.IsNullOrEmpty(itemDifficulty))
                return true;

            string normItem = NormalizeDifficulty(itemDifficulty);
            string normReq = NormalizeDifficulty(requestedDifficulty);
            return normItem.Equals(normReq, StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeDifficulty(string diff)
        {
            if (string.IsNullOrEmpty(diff)) return "EASY";
            string d = diff.Trim().ToUpperInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
            if (d.Contains("NHANBIET") || d.Contains("EASY") || d.Contains("DE") || d.Contains("Dá»„") || d.Contains("1")) return "EASY";
            if (d.Contains("THONGHIEU") || d.Contains("MEDIUM") || d.Contains("TRUNGBINH") || d.Contains("VUA") || d.Contains("2")) return "MEDIUM";
            if (d.Contains("VANDUNGCAO") || d.Contains("EXPERT") || d.Contains("VERYHARD") || d.Contains("KHO") || d.Contains("4")) return "HARD";
            if (d.Contains("VANDUNG") || d.Contains("HARD") || d.Contains("3")) return "HARD";
            return "EASY";
        }

        /// <summary>
        /// Tá»± Ä‘á»™ng kháº¯c phá»¥c lá»—i Google Sheet chuyá»ƒn phÃ¢n sá»‘ (vÃ­ dá»¥ 1/2, 3/2, 2/3) thÃ nh chuá»—i NgÃ y thÃ¡ng:
        /// "Sun Feb 01 2026 00:00:00 GMT+0700" -> "1/2"
        /// "Tue Feb 03 2026 00:00:00 GMT+0700" -> "3/2"
        /// "Mon Mar 02 2026 00:00:00 GMT+0700" -> "2/3"
        /// </summary>
        public static string SanitizeMathText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            if (text.Contains("GMT") || text.Contains("00:00:00") || text.Contains("Giá» ÄÃ´ng DÆ°Æ¡ng") || text.Contains("Indochina Time"))
            {
                if (System.DateTime.TryParse(text, out var dt))
                {
                    return $"{dt.Day}/{dt.Month}";
                }

                var match = System.Text.RegularExpressions.Regex.Match(text, @"\b(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+(\d{1,2})\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string mStr = match.Groups[1].Value.ToLower();
                    int day = int.Parse(match.Groups[2].Value);
                    int month = 1;
                    switch (mStr)
                    {
                        case "jan": month = 1; break;
                        case "feb": month = 2; break;
                        case "mar": month = 3; break;
                        case "apr": month = 4; break;
                        case "may": month = 5; break;
                        case "jun": month = 6; break;
                        case "jul": month = 7; break;
                        case "aug": month = 8; break;
                        case "sep": month = 9; break;
                        case "oct": month = 10; break;
                        case "nov": month = 11; break;
                        case "dec": month = 12; break;
                    }
                    return $"{day}/{month}";
                }
            }

            return Math6Companion.Core.MathTextSanitizer.SanitizeForDisplay(text);
        }

        #endregion

        #region Coroutine Wrappers

        private IEnumerator RunGetRequest<T>(string url, Action<bool, T, string> onComplete) where T : class
        {
            string responseText = null;
            string errorMsg = null;
            bool isDone = false;

            int timeoutMs = Mathf.Max((int)(timeoutSeconds * 1000), 40000);

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    responseText = DoHttpGet(url, timeoutMs);
                }
                catch (Exception ex)
                {
                    errorMsg = ex.Message;
                }
                finally
                {
                    isDone = true;
                }
            });

            while (!isDone) yield return null;

            if (!string.IsNullOrEmpty(responseText))
            {
                try
                {
                    T result = JsonUtility.FromJson<T>(responseText);
                    onComplete?.Invoke(result != null, result, responseText);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GoogleSheetsNetworkService] Lá»—i parse JSON GET: {ex.Message}");
                    onComplete?.Invoke(false, null, responseText);
                }
            }
            else
            {
                Debug.LogWarning($"[GoogleSheetsNetworkService] GET Lá»—i: {errorMsg} -> {url}");
                onComplete?.Invoke(false, null, errorMsg ?? "Lá»—i máº¡ng");
            }
        }

        private IEnumerator RunPostRequest<T>(string url, string jsonBody, Action<bool, T, string> onComplete) where T : class
        {
            string responseText = null;
            string errorMsg = null;
            bool isDone = false;

            int timeoutMs = Mathf.Max((int)(timeoutSeconds * 1000), 45000);

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    responseText = DoHttpPost(url, jsonBody, timeoutMs);
                }
                catch (Exception ex)
                {
                    errorMsg = ex.Message;
                }
                finally
                {
                    isDone = true;
                }
            });

            while (!isDone) yield return null;

            if (!string.IsNullOrEmpty(responseText))
            {
                try
                {
                    T result = JsonUtility.FromJson<T>(responseText);
                    onComplete?.Invoke(result != null, result, responseText);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GoogleSheetsNetworkService] Lá»—i parse JSON POST: {ex.Message}");
                    onComplete?.Invoke(false, null, responseText);
                }
            }
            else
            {
                Debug.LogWarning($"[GoogleSheetsNetworkService] POST Lá»—i: {errorMsg}");
                onComplete?.Invoke(false, null, errorMsg ?? "Lá»—i máº¡ng");
            }
        }

        #endregion

        #region Utility

        private static List<RubricItem> ParseRubricRaw(string rubricRaw)
        {
            if (string.IsNullOrEmpty(rubricRaw) || rubricRaw == "[]")
                return new List<RubricItem>();
            try
            {
                var wrapper = JsonUtility.FromJson<RubricListWrapper>($"{{\"items\":{rubricRaw}}}");
                return wrapper?.items ?? new List<RubricItem>();
            }
            catch
            {
                return new List<RubricItem>();
            }
        }

        [Serializable]
        private class RubricListWrapper { public List<RubricItem> items; }

        #endregion

        #region Internal JSON DTO Wrappers

        [Serializable]
        private class LoginRequestWrapper
        {
            public string action;
            public string username;
            public string password;
            public string passwordHash;
        }

        [Serializable]
        private class LoginResponseWrapper
        {
            public bool success;
            public string message;
            public UserData data;
        }

        [Serializable]
        private class LessonsResponseWrapper
        {
            public bool success;
            public string message;
            public List<LessonData> lessons;
        }

        [Serializable]
        private class QuestionsResponseWrapper
        {
            public bool success;
            public string message;
            public List<QuestionData> questions;
        }

        [Serializable]
        private class SaveResultResponseWrapper
        {
            public bool success;
            public string message;
            public List<BadgeData> unlockedBadges;
        }

        [Serializable]
        private class AnalyticsResponseWrapper
        {
            public bool success;
            public string message;
            public StudentAnalyticsData analytics;
        }

        [Serializable]
        private class BaseResponseWrapper
        {
            public bool success;
            public string message;
        }

        #endregion
    }
}
