// Assets/_Project/Scripts/Network/Services/YouTubeStreamResolver.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace Math6Companion.Core.Services
{
    /// <summary>
    /// Bộ giải mã luồng video YouTube và đường dẫn video trực tiếp (MP4 / WebM / Google Drive)
    /// Dành cho Unity VideoPlayer trong Math6 AI Companion.
    /// Nếu video không thể bóc tách luồng (do YouTube siết bản quyền / n-token), 
    /// hệ thống kích hoạt cơ chế Fallback mở trình duyệt an toàn không làm crash game.
    /// </summary>
    public class YouTubeStreamResolver : MonoBehaviour
    {
        private static YouTubeStreamResolver _instance;
        private static bool isQuitting = false;

        public static YouTubeStreamResolver Instance
        {
            get
            {
                if (isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<YouTubeStreamResolver>();
                    if (_instance == null && !isQuitting)
                    {
                        var go = new GameObject("[YouTubeStreamResolver]");
                        _instance = go.AddComponent<YouTubeStreamResolver>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        /// <summary>
        /// Phân giải URL video (YouTube, Direct MP4, Google Drive) thành đường dẫn Stream trực tiếp.
        /// </summary>
        /// <param name="rawUrl">URL nguồn do giáo viên hoặc hệ thống cung cấp</param>
        /// <param name="callback">Callback (isSuccess, directStreamUrl, errorMessage)</param>
        public void Resolve(string rawUrl, Action<bool, string, string> callback)
        {
            if (string.IsNullOrEmpty(rawUrl))
            {
                callback?.Invoke(false, null, "Đường dẫn video rỗng.");
                return;
            }

            string trimmed = rawUrl.Trim();

            // 1. Kiểm tra nếu là file video trực tiếp (.mp4, .webm, hoặc chứa luồng videoplayback)
            if (IsDirectVideoUrl(trimmed))
            {
                Debug.Log($"[YouTubeStreamResolver] Phát hiện URL video trực tiếp: {trimmed}");
                callback?.Invoke(true, trimmed, null);
                return;
            }

            // 2. Kiểm tra nếu là link Google Drive
            if (trimmed.Contains("drive.google.com"))
            {
                string driveStream = ConvertGoogleDriveUrl(trimmed);
                if (!string.IsNullOrEmpty(driveStream))
                {
                    Debug.Log($"[YouTubeStreamResolver] Chuyển đổi link Google Drive sang direct stream: {driveStream}");
                    callback?.Invoke(true, driveStream, null);
                    return;
                }
            }

            // 3. Phân giải link YouTube
            string videoId = ExtractVideoId(trimmed);
            if (string.IsNullOrEmpty(videoId))
            {
                callback?.Invoke(false, null, "Không nhận diện được ID video YouTube hợp lệ.");
                return;
            }

            StartCoroutine(ResolveYouTubeStreamCoroutine(videoId, callback));
        }

        private bool IsDirectVideoUrl(string url)
        {
            string clean = url.Split('?')[0].ToLowerInvariant();
            return clean.EndsWith(".mp4") || clean.EndsWith(".webm") || clean.EndsWith(".m4v") ||
                   clean.EndsWith(".mov") || url.Contains("videoplayback?");
        }

        private string ConvertGoogleDriveUrl(string driveUrl)
        {
            // Định dạng: https://drive.google.com/file/d/FILE_ID/view...
            var match = Regex.Match(driveUrl, @"/d/([a-zA-Z0-9_-]+)");
            if (match.Success)
            {
                string id = match.Groups[1].Value;
                return $"https://drive.google.com/uc?export=download&id={id}";
            }

            // Định dạng id=: https://drive.google.com/open?id=FILE_ID
            var matchId = Regex.Match(driveUrl, @"[?&]id=([a-zA-Z0-9_-]+)");
            if (matchId.Success)
            {
                string id = matchId.Groups[1].Value;
                return $"https://drive.google.com/uc?export=download&id={id}";
            }

            return null;
        }

        public static string ExtractVideoId(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            // Dạng rút gọn: https://youtu.be/dQw4w9WgXcQ
            if (url.Contains("youtu.be/"))
            {
                var parts = url.Split(new string[] { "youtu.be/" }, StringSplitOptions.None);
                if (parts.Length > 1) return parts[1].Split('?')[0].Split('&')[0].Split('#')[0];
            }

            // Dạng chuẩn: https://www.youtube.com/watch?v=dQw4w9WgXcQ
            if (url.Contains("watch?v="))
            {
                var parts = url.Split(new string[] { "watch?v=" }, StringSplitOptions.None);
                if (parts.Length > 1) return parts[1].Split('&')[0].Split('#')[0];
            }

            // Dạng embed: https://www.youtube.com/embed/dQw4w9WgXcQ
            if (url.Contains("embed/"))
            {
                var parts = url.Split(new string[] { "embed/" }, StringSplitOptions.None);
                if (parts.Length > 1) return parts[1].Split('?')[0].Split('&')[0].Split('#')[0];
            }

            // Dạng Shorts: https://www.youtube.com/shorts/dQw4w9WgXcQ
            if (url.Contains("shorts/"))
            {
                var parts = url.Split(new string[] { "shorts/" }, StringSplitOptions.None);
                if (parts.Length > 1) return parts[1].Split('?')[0].Split('&')[0].Split('#')[0];
            }

            // Nếu người dùng chỉ nhập ID 11 ký tự
            if (Regex.IsMatch(url, @"^[a-zA-Z0-9_-]{11}$"))
            {
                return url;
            }

            return null;
        }

        private IEnumerator ResolveYouTubeStreamCoroutine(string videoId, Action<bool, string, string> callback)
        {
            Debug.Log($"[YouTubeStreamResolver] Bắt đầu phân giải luồng cho video ID: {videoId}");

            // Thử Endpoint 1: YouTube Innertube API với client TV / Android (hỗ trợ trực tiếp luồng stream)
            string innertubeUrl = "https://www.youtube.com/youtubei/v1/player?prettyPrint=false";
            string jsonBody = "{" +
                "\"videoId\":\"" + videoId + "\"," +
                "\"context\":{" +
                    "\"client\":{" +
                        "\"clientName\":\"ANDROID\"," +
                        "\"clientVersion\":\"19.29.35\"," +
                        "\"androidSdkVersion\":30," +
                        "\"hl\":\"vi\"," +
                        "\"gl\":\"VN\"" +
                    "}" +
                "}" +
            "}";

            using (var req = new UnityWebRequest(innertubeUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("User-Agent", "com.google.android.youtube/19.29.35 (Linux; U; Android 11) gzip");
                req.timeout = 5;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string json = req.downloadHandler.text;
                    string streamUrl = ExtractDirectStreamFromJson(json);
                    if (!string.IsNullOrEmpty(streamUrl))
                    {
                        Debug.Log($"[YouTubeStreamResolver] Thành công phân giải direct MP4 stream từ Innertube!");
                        callback?.Invoke(true, streamUrl, null);
                        yield break;
                    }
                }
            }

            // Thử Endpoint 2: Truy vấn Web Watch Data (fallback)
            string watchUrl = $"https://www.youtube.com/watch?v={videoId}&hl=vi";
            using (var reqWatch = UnityWebRequest.Get(watchUrl))
            {
                reqWatch.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                reqWatch.timeout = 5;

                yield return reqWatch.SendWebRequest();

                if (reqWatch.result == UnityWebRequest.Result.Success)
                {
                    string html = reqWatch.downloadHandler.text;
                    string streamUrl = ExtractDirectStreamFromHtml(html);
                    if (!string.IsNullOrEmpty(streamUrl))
                    {
                        Debug.Log($"[YouTubeStreamResolver] Thành công phân giải stream từ Watch Page!");
                        callback?.Invoke(true, streamUrl, null);
                        yield break;
                    }
                }
            }

            // Nếu YouTube chặn hoặc video yêu cầu chữ ký cipher n-token
            Debug.LogWarning($"[YouTubeStreamResolver] Video {videoId} được bảo vệ bởi thuật toán chữ ký hoặc giới hạn độ tuổi. Chuyển sang chế độ xem trình duyệt.");
            callback?.Invoke(false, null, "YOUTUBE_CIPHER_OR_PROTECTED");
        }

        private string ExtractDirectStreamFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                // Tìm kiếm các khối "url":"https://..." có mimeType mp4
                // Ưu tiên itag 22 (720p) hoặc itag 18 (360p) có chứa cả video và audio (muxed stream)
                var urlMatches = Regex.Matches(json, @"""url"":\s*""(https:[^""]+)""");
                foreach (Match m in urlMatches)
                {
                    string u = Regex.Unescape(m.Groups[1].Value);
                    if (u.Contains("mime=video%2Fmp4") || u.Contains("mime=video/mp4") || u.Contains("itag=18") || u.Contains("itag=22"))
                    {
                        // Kiểm tra URL không chứa signature dạng thô cần mã hóa
                        if (!u.Contains("s=") && !u.Contains("sp="))
                        {
                            return u;
                        }
                    }
                }

                // Nếu có bất kỳ url nào chạy được
                if (urlMatches.Count > 0)
                {
                    string firstUrl = Regex.Unescape(urlMatches[0].Groups[1].Value);
                    if (firstUrl.StartsWith("https://") && !firstUrl.Contains("s="))
                    {
                        return firstUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[YouTubeStreamResolver] Lỗi đọc stream JSON: {ex.Message}");
            }

            return null;
        }

        private string ExtractDirectStreamFromHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return null;

            try
            {
                var match = Regex.Match(html, @"ytInitialPlayerResponse\s*=\s*({.+?});");
                if (match.Success)
                {
                    return ExtractDirectStreamFromJson(match.Groups[1].Value);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[YouTubeStreamResolver] Lỗi đọc stream HTML: {ex.Message}");
            }

            return null;
        }
    }
}
