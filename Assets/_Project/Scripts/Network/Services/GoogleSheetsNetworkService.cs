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
    /// Module 1: Dịch vụ kết nối Google Sheets qua Google Apps Script Web App REST API.
    /// Xử lý Đăng nhập, Danh sách bài học, Ngân hàng câu hỏi, Lưu kết quả thi đấu & Thống kê học sinh.
    /// Tự động đồng bộ topic Tiếng Anh <-> Tiếng Việt, cache bộ nhớ đệm và chống nghẽn request kép.
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

        // Cookie jar dùng chung — duy trì cookie qua redirect chain của Google
        private static readonly System.Net.CookieContainer sharedCookieJar = new System.Net.CookieContainer();

        // Bộ nhớ đệm toàn cục (In-Memory Cache)
        private static List<LessonData> cachedLessons = new List<LessonData>();
        private static List<QuestionData> cachedAllQuestions = new List<QuestionData>();
        private static Dictionary<string, List<QuestionData>> cachedTopicQuestions = new Dictionary<string, List<QuestionData>>();

        // Quản lý chống trùng lặp request (Deduplication / In-Flight Request Coordinator)
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

                // Cấu hình TLS & Connection
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

                // Kích hoạt warm-up thông minh (dùng chung pipeline GetLessons, không gửi request trùng lặp)
                GetLessons("ALL", (success, lessons, msg) =>
                {
                    if (success && lessons != null && lessons.Count > 0)
                    {
                        Debug.Log($"[NET-DIAG] ✅ SERVER SẴN SÀNG! Đã nạp trước {lessons.Count} bài học từ Google Sheet.");
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
        /// Gửi HTTP GET chuẩn Postman — bypass proxy, duy trì cookie, auto redirect.
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
        /// Gửi HTTP POST chuẩn Postman — bypass proxy, duy trì cookie, auto redirect.
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
                    callback?.Invoke(true, resp.data, resp.message ?? "Đăng nhập thành công!");
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
                        callback?.Invoke(false, null, resp?.message ?? "Không thể kết nối đến máy chủ Google Sheets!");
                    }
                }
            }));
        }

        public void GetLessons(string topic, Action<bool, List<LessonData>, string> callback)
        {
            // 1. Nếu đã có trong Cache, lọc theo topic và trả về ngay tức thì (0ms)!
            if (cachedLessons != null && cachedLessons.Count > 0)
            {
                var filtered = FilterLessonsByTopic(cachedLessons, topic);
                var returnList = (filtered != null && filtered.Count > 0) ? filtered : cachedLessons;
                callback?.Invoke(true, returnList, "Nạp bài học từ bộ nhớ đệm thành công!");
                return;
            }

            // 2. Nếu đang có một request lấy bài học đang chạy, đăng ký nhận chung kết quả (chống gửi request trùng lặp)
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
                return; // Đã có request đang chạy, chỉ cần đợi nhận callback chung
            }

            // 3. Bắt đầu gửi HTTP request lấy danh sách bài học
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
                        cb?.Invoke(true, cachedLessons, "Tải bài học thành công!");
                    }
                }
                else
                {
                    if (cachedLessons != null && cachedLessons.Count > 0)
                    {
                        foreach (var cb in callbacksToInvoke)
                        {
                            cb?.Invoke(true, cachedLessons, "Nạp từ bộ nhớ đệm thành công!");
                        }
                    }
                    else if (fallbackToMockOnError)
                    {
                        Debug.LogWarning($"[GoogleSheetsNetworkService] GetLessons không thành công, sử dụng dữ liệu cục bộ.");
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
                            cb?.Invoke(false, null, "Không thể tải danh sách bài học từ Google Sheets!");
                        }
                    }
                }
            }));
        }

        public void GetQuestions(string topic, string grade, Action<bool, List<QuestionData>, string> callback)
        {
            string cacheKey = $"{topic ?? "ALL"}_{grade ?? "6"}";

            // 1. Kiểm tra cache câu hỏi theo topic
            if (cachedTopicQuestions.TryGetValue(cacheKey, out var topicList) && topicList.Count > 0)
            {
                callback?.Invoke(true, topicList, "Nạp câu hỏi từ bộ nhớ đệm thành công!");
                return;
            }

            // 2. Nếu đã có toàn bộ câu hỏi trong RAM, lọc ngay
            if (cachedAllQuestions != null && cachedAllQuestions.Count > 0)
            {
                var filtered = FilterQuestionsByTopic(cachedAllQuestions, topic);
                if (filtered != null && filtered.Count > 0)
                {
                    cachedTopicQuestions[cacheKey] = filtered;
                    callback?.Invoke(true, filtered, "Nạp câu hỏi theo chủ đề thành công!");
                }
                else
                {
                    callback?.Invoke(true, new List<QuestionData>(), "Chưa có câu hỏi cho chủ đề này trên máy chủ.");
                }
                return;
            }

            // 3. Đăng ký nhận chung kết quả nếu đang có request tải câu hỏi
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
                return; // Đợi request đang chạy
            }

            // 4. Tải toàn bộ kho câu hỏi từ Google Sheet (tối đa 1000 câu để nạp trọn vẹn ngân hàng đề)
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
                        cb?.Invoke(true, cachedAllQuestions, "Tải câu hỏi thành công!");
                    }
                }
                else
                {
                    if (cachedAllQuestions != null && cachedAllQuestions.Count > 0)
                    {
                        foreach (var cb in callbacksToInvoke)
                        {
                            cb?.Invoke(true, cachedAllQuestions, "Nạp câu hỏi từ bộ nhớ đệm!");
                        }
                    }
                    else if (fallbackToMockOnError)
                    {
                        Debug.LogWarning($"[GoogleSheetsNetworkService] GetQuestions không thành công, sử dụng ngân hàng câu hỏi cục bộ.");
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
                            cb?.Invoke(false, null, "Không thể tải câu hỏi từ Google Sheets!");
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
                    callback?.Invoke(true, resp.unlockedBadges ?? new List<BadgeData>(), "Đã lưu kết quả thành công!");
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
                        callback?.Invoke(false, null, "Không thể lưu kết quả vào Google Sheets!");
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
                    callback?.Invoke(true, resp.message ?? "Đã ghi nhận AI Tutor log lên Google Sheets!");
                }
                else
                {
                    if (fallbackToMockOnError)
                    {
                        MockNetworkService.Instance.LogAITutor(logSubmission, callback);
                    }
                    else
                    {
                        callback?.Invoke(false, "Không thể ghi log AI lên Google Sheets");
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
                    callback?.Invoke(true, resp.analytics, "Tải báo cáo phân tích thành công!");
                }
                else
                {
                    if (fallbackToMockOnError)
                    {
                        MockNetworkService.Instance.GetStudentAnalytics(studentId, callback);
                    }
                    else
                    {
                        callback?.Invoke(false, null, "Không thể tải phân tích từ Google Sheets!");
                    }
                }
            }));
        }

        #endregion

        #region Topic Mapping & Filtering Logic

        /// <summary>
        /// Kiểm tra độ khớp chủ đề giữa câu hỏi/bài học với chủ đề được yêu cầu.
        /// Hỗ trợ map linh hoạt cả tiếng Anh và tiếng Việt:
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

            // 1. Thứ tự thực hiện phép tính vs Thứ tự số tự nhiên vs Phép tính thông thường
            if (t.Contains("thutupheptinh") || t.Contains("thứtựphéptính") || t.Contains("thutupheptoan") || t.Contains("bieuthuc") || t.Contains("biểuthức")) return "thutupheptinh";
            if (t.Contains("thutu") || t.Contains("thứtự")) return "thutu";
            if (t.Contains("pheptinh") || t.Contains("phéptính") || t.Contains("phepcong") || t.Contains("phepnhan") || t.Contains("phepchia") || t.Contains("pheptru")) return "pheptinh";

            // 2. Ghi số & Chữ số La Mã
            if (t.Contains("ghiso") || t.Contains("ghisố") || t.Contains("chuso") || t.Contains("chữsố") || t.Contains("lama") || t.Contains("lamã")) return "ghiso";

            // 3. Luỹ thừa
            if (t.Contains("luythua") || t.Contains("luỹthừa") || t.Contains("lũythừa") || t.Contains("somu") || t.Contains("sốmũ") || t.Contains("power")) return "luythua";

            // 4. Tính chia hết (Ước, bội, số nguyên tố, hợp số, ƯCLN, BCNN)
            if (t.Contains("tinhchiahet") || t.Contains("chiahet") || t.Contains("chiahết") || t.Contains("dauhieu") || t.Contains("dấuhiệu") || t.Contains("uocvaboi") || t.Contains("ướcvàbội") || t.Contains("ucln") || t.Contains("bcnn") || t.Contains("uoc") || t.Contains("ước") || t.Contains("boi") || t.Contains("bội") || t.Contains("songuyento") || t.Contains("sốnguyêntố") || t.Contains("nguyento") || t.Contains("nguyêntố") || t.Contains("hopso") || t.Contains("hợpsố")) return "tinhchiahet";

            // 5. Số nguyên
            if (t.Contains("integer") || t.Contains("songuyen") || t.Contains("sốnguyên") || t.Contains("nguyenam") || t.Contains("nguyênâm") || t.Contains("trucso") || t.Contains("trụcsố")) return "songuyen";

            // 6. Tập hợp
            if (t.Contains("set") || t.Contains("taphop") || t.Contains("tậphợp") || t.Contains("phantu") || t.Contains("phầntử") || t.Contains("sotunhien") || t.Contains("sốtựnhiên")) return "taphop";

            // 7. Hình phẳng & Tính đối xứng & Hình học trực quan
            if (t.Contains("tinhdoixung") || t.Contains("doixung") || t.Contains("đốixứng") || t.Contains("trucdoixung") || t.Contains("tamdoixung")) return "tinhdoixung";
            if (t.Contains("hinhphang") || t.Contains("hìnhphẳng") || t.Contains("tamgiac") || t.Contains("tamgiác") || t.Contains("hinhvuong") || t.Contains("hìnhvuông") || t.Contains("hinhchunhat") || t.Contains("hìnhchữnhật") || t.Contains("hinhthoi") || t.Contains("hìnhthoi") || t.Contains("hinhbinhhanh") || t.Contains("hìnhbìnhhành") || t.Contains("hinhthang") || t.Contains("hìnhthang") || t.Contains("lucgiac") || t.Contains("lụcgiác")) return "hinhphang";
            if (t.Contains("geometry") || t.Contains("hinhhoc") || t.Contains("hìnhhọc") || t.Contains("doanthang") || t.Contains("đoạnthẳng") || t.Contains("trungdiem") || t.Contains("trungđiểm") || t.Contains("goc") || t.Contains("góc") || t.Contains("duongthang") || t.Contains("đườngthẳng") || t.Contains("diem") || t.Contains("điểm")) return "hinhhoc";

            // 8. Phân số & Số thập phân
            if (t.Contains("fraction") || t.Contains("phanso") || t.Contains("phânsố") || t.Contains("honso") || t.Contains("hỗnsố")) return "phanso";
            if (t.Contains("decimal") || t.Contains("sothapphan") || t.Contains("thapphan") || t.Contains("thậpphân") || t.Contains("phantram") || t.Contains("phầntrăm") || t.Contains("tiso") || t.Contains("tỉsố")) return "sothapphan";

            // 9. Thống kê & Xác suất
            if (t.Contains("thongke") || t.Contains("thốngkê") || t.Contains("xacsuat") || t.Contains("xácsuất") || t.Contains("dulieu") || t.Contains("dữliệu") || t.Contains("bieudo") || t.Contains("biểuđồ")) return "thongke";

            if (t.Contains("equation") || t.Contains("phuongtrinh") || t.Contains("phươngtrình")) return "phuongtrinh";

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
            if (d.Contains("NHANBIET") || d.Contains("EASY") || d.Contains("DE") || d.Contains("DỄ") || d.Contains("1")) return "EASY";
            if (d.Contains("THONGHIEU") || d.Contains("MEDIUM") || d.Contains("TRUNGBINH") || d.Contains("VUA") || d.Contains("2")) return "MEDIUM";
            if (d.Contains("VANDUNGCAO") || d.Contains("EXPERT") || d.Contains("VERYHARD") || d.Contains("KHO") || d.Contains("4")) return "HARD";
            if (d.Contains("VANDUNG") || d.Contains("HARD") || d.Contains("3")) return "HARD";
            return "EASY";
        }

        /// <summary>
        /// Tự động khắc phục lỗi Google Sheet chuyển phân số (ví dụ 1/2, 3/2, 2/3) thành chuỗi Ngày tháng:
        /// "Sun Feb 01 2026 00:00:00 GMT+0700" -> "1/2"
        /// "Tue Feb 03 2026 00:00:00 GMT+0700" -> "3/2"
        /// "Mon Mar 02 2026 00:00:00 GMT+0700" -> "2/3"
        /// </summary>
        public static string SanitizeMathText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            if (text.Contains("GMT") || text.Contains("00:00:00") || text.Contains("Giờ Đông Dương") || text.Contains("Indochina Time"))
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
                    Debug.LogWarning($"[GoogleSheetsNetworkService] Lỗi parse JSON GET: {ex.Message}");
                    onComplete?.Invoke(false, null, responseText);
                }
            }
            else
            {
                Debug.LogWarning($"[GoogleSheetsNetworkService] GET Lỗi: {errorMsg} -> {url}");
                onComplete?.Invoke(false, null, errorMsg ?? "Lỗi mạng");
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
                    Debug.LogWarning($"[GoogleSheetsNetworkService] Lỗi parse JSON POST: {ex.Message}");
                    onComplete?.Invoke(false, null, responseText);
                }
            }
            else
            {
                Debug.LogWarning($"[GoogleSheetsNetworkService] POST Lỗi: {errorMsg}");
                onComplete?.Invoke(false, null, errorMsg ?? "Lỗi mạng");
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
