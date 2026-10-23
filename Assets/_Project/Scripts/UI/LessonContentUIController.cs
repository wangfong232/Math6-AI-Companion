using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;

namespace Math6Companion.UI
{
    /// <summary>
    /// Điều khiển giao diện Nội dung bài học (04_LessonContent.unity).
    /// Hỗ trợ chuyển đổi 2 Tab: Lý thuyết Text & Video bài giảng minh họa.
    /// </summary>
    public class LessonContentUIController : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TextMeshProUGUI lessonTitleText;
        [SerializeField] private Button backToListButton;

        [Header("Tab Buttons")]
        [SerializeField] private Button textTabButton;
        [SerializeField] private Button videoTabButton;
        [SerializeField] private Image textTabHighlight;
        [SerializeField] private Image videoTabHighlight;

        [Header("Panels")]
        [SerializeField] private GameObject textContentPanel;
        [SerializeField] private GameObject videoContentPanel;

        [Header("Text Tab Content")]
        [SerializeField] private TextMeshProUGUI theoryBodyText;

        [Header("Video Tab Content")]
        [SerializeField] private TextMeshProUGUI videoInfoText;
        [SerializeField] private Button playExternalVideoButton;
        [SerializeField] private RawImage videoThumbnailPreview;

        [Header("Quick Practice Actions")]
        [SerializeField] private Button startMcqPracticeButton;
        [SerializeField] private Button startEssayPracticeButton;

        private string currentVideoUrl = "";
        private UnityEngine.Video.VideoPlayer videoPlayer;
        private RenderTexture videoRenderTexture;
        private Button inGamePlayButton;

        // Các thành phần Video Player Hoạt Họa Trong Game
        private GameObject lecturePlayerOverlay;
        private TextMeshProUGUI lectureHeaderTitleText;
        private TextMeshProUGUI lectureSlideTitleText;
        private TextMeshProUGUI lectureBodyContentText;
        private TextMeshProUGUI lectureTimeCounterText;
        private Image lectureProgressBarFill;
        private Button pauseResumeButton;
        private TextMeshProUGUI pauseResumeButtonText;
        private Button prevSlideButton;
        private Button nextSlideButton;
        private Button replayButton;

        private Coroutine lecturePlaybackCoroutine;
        private bool isLecturePlaying = false;
        private int currentSlideIndex = 0;
        private float playbackTimer = 0f;
        private const float TOTAL_LECTURE_TIME = 120f; // 2 phút thời lượng mô phỏng bài giảng
        private System.Collections.Generic.List<LectureSlideData> currentLectureSlides = new System.Collections.Generic.List<LectureSlideData>();

        private class LectureSlideData
        {
            public string sectionTitle;
            public string bodyText;
            public float startSecond;
            public float durationSecond;
        }

        private void Start()
        {
            if (backToListButton != null)
                backToListButton.onClick.AddListener(OnBackToListClicked);

            if (textTabButton != null)
                textTabButton.onClick.AddListener(() => SwitchTab("TEXT"));

            if (videoTabButton != null)
                videoTabButton.onClick.AddListener(() => SwitchTab("VIDEO"));

            if (playExternalVideoButton != null)
            {
                playExternalVideoButton.onClick.RemoveAllListeners();
                playExternalVideoButton.onClick.AddListener(OnOpenVideoUrl);
            }

            if (startMcqPracticeButton != null)
            {
                startMcqPracticeButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    SceneManager.LoadScene("05_MCQPractice");
                });
            }

            if (startEssayPracticeButton != null)
            {
                startEssayPracticeButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    SceneManager.LoadScene("06_EssayPractice");
                });
            }

            SetupVideoLayoutAndPlayer();
            LoadLessonDetails();

            // Mở tab ưu tiên đã chọn từ LessonList
            SwitchTab(LessonListUIController.PreferredContentTab);
        }

        private void SetupVideoLayoutAndPlayer()
        {
            if (videoContentPanel == null) return;

            // 1. Định vị lại videoInfoText lên phía trên (không đè lên video)
            if (videoInfoText != null)
            {
                var rtInfo = videoInfoText.GetComponent<RectTransform>();
                rtInfo.anchorMin = new Vector2(0.5f, 0.5f);
                rtInfo.anchorMax = new Vector2(0.5f, 0.5f);
                rtInfo.pivot = new Vector2(0.5f, 0.5f);
                rtInfo.anchoredPosition = new Vector2(0, 240);
                rtInfo.sizeDelta = new Vector2(850, 60);
                videoInfoText.alignment = TextAlignmentOptions.Center;
                videoInfoText.fontSize = 18;
            }

            // 2. Định vị khung Video Screen ở trung tâm
            var screenContainer = videoContentPanel.transform.Find("VideoScreenContainer")?.gameObject;
            if (screenContainer == null)
            {
                screenContainer = new GameObject("VideoScreenContainer", typeof(RectTransform), typeof(Image));
                screenContainer.transform.SetParent(videoContentPanel.transform, false);
                var rtSC = screenContainer.GetComponent<RectTransform>();
                rtSC.anchorMin = new Vector2(0.5f, 0.5f);
                rtSC.anchorMax = new Vector2(0.5f, 0.5f);
                rtSC.pivot = new Vector2(0.5f, 0.5f);
                rtSC.anchoredPosition = new Vector2(0, 25);
                rtSC.sizeDelta = new Vector2(740, 415);
                screenContainer.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f);
            }

            // Tạo hoặc tái sử dụng RawImage Thumbnail
            var thumbGo = screenContainer.transform.Find("VideoDisplay")?.gameObject;
            if (thumbGo == null)
            {
                thumbGo = new GameObject("VideoDisplay", typeof(RectTransform), typeof(RawImage));
                thumbGo.transform.SetParent(screenContainer.transform, false);
                var rtD = thumbGo.GetComponent<RectTransform>();
                rtD.anchorMin = Vector2.zero;
                rtD.anchorMax = Vector2.one;
                rtD.offsetMin = Vector2.zero;
                rtD.offsetMax = Vector2.zero;
            }
            videoThumbnailPreview = thumbGo.GetComponent<RawImage>();
            videoThumbnailPreview.color = Color.white;

            // Tạo nút Play Overlay lớn ở giữa màn hình video
            var playOverlayGo = screenContainer.transform.Find("Btn_CenterPlay")?.gameObject;
            if (playOverlayGo == null)
            {
                playOverlayGo = new GameObject("Btn_CenterPlay", typeof(RectTransform), typeof(Image), typeof(Button));
                playOverlayGo.transform.SetParent(screenContainer.transform, false);
                var rtP = playOverlayGo.GetComponent<RectTransform>();
                rtP.anchorMin = new Vector2(0.5f, 0.5f);
                rtP.anchorMax = new Vector2(0.5f, 0.5f);
                rtP.anchoredPosition = Vector2.zero;
                rtP.sizeDelta = new Vector2(90, 90);
                playOverlayGo.GetComponent<Image>().color = new Color(0.85f, 0.18f, 0.18f, 0.95f);

                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(TextMeshProUGUI));
                iconGo.transform.SetParent(playOverlayGo.transform, false);
                var rtI = iconGo.GetComponent<RectTransform>();
                rtI.anchorMin = Vector2.zero; rtI.anchorMax = Vector2.one;
                rtI.offsetMin = Vector2.zero; rtI.offsetMax = Vector2.zero;
                var tmpI = iconGo.GetComponent<TextMeshProUGUI>();
                tmpI.text = "▶";
                tmpI.fontSize = 38;
                tmpI.alignment = TextAlignmentOptions.Center;
                tmpI.color = Color.white;
            }

            inGamePlayButton = playOverlayGo.GetComponent<Button>();
            inGamePlayButton.onClick.RemoveAllListeners();
            inGamePlayButton.onClick.AddListener(OnPlayInGameVideoClicked);

            // Xây dựng Giao Diện Bài Giảng Điện Tử Hoạt Họa Trong Game (Interactive Lecture Screen)
            BuildLecturePlayerUI(screenContainer);

            // Khởi tạo Unity VideoPlayer Component
            videoPlayer = screenContainer.GetComponent<UnityEngine.Video.VideoPlayer>();
            if (videoPlayer == null) videoPlayer = screenContainer.AddComponent<UnityEngine.Video.VideoPlayer>();

            if (videoRenderTexture == null)
            {
                videoRenderTexture = new RenderTexture(1280, 720, 16, RenderTextureFormat.ARGB32);
                videoRenderTexture.Create();
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;

            // Định vị lại nút Mở Xem Trình Duyệt xuống phía dưới màn hình video
            if (playExternalVideoButton != null)
            {
                var rtBtn = playExternalVideoButton.GetComponent<RectTransform>();
                rtBtn.anchorMin = new Vector2(0.5f, 0.5f);
                rtBtn.anchorMax = new Vector2(0.5f, 0.5f);
                rtBtn.pivot = new Vector2(0.5f, 0.5f);
                rtBtn.anchoredPosition = new Vector2(0, -215);
                rtBtn.sizeDelta = new Vector2(380, 44);

                var txtBtn = playExternalVideoButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txtBtn != null) txtBtn.text = "<b>[>] Mở Xem Video Gốc Trên YouTube</b>";
            }
        }

        private void BuildLecturePlayerUI(GameObject container)
        {
            var overlay = container.transform.Find("LecturePlayerOverlay")?.gameObject;
            if (overlay == null)
            {
                overlay = new GameObject("LecturePlayerOverlay", typeof(RectTransform), typeof(Image));
                overlay.transform.SetParent(container.transform, false);
                var rt = overlay.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                overlay.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.14f, 0.98f);
            }
            lecturePlayerOverlay = overlay;
            lecturePlayerOverlay.SetActive(false);

            // 1. Header Bar (Tiêu đề bài giảng & Trạng thái phát)
            var headGo = new GameObject("HeadBar", typeof(RectTransform), typeof(Image));
            headGo.transform.SetParent(overlay.transform, false);
            var rtHead = headGo.GetComponent<RectTransform>();
            rtHead.anchorMin = new Vector2(0, 1); rtHead.anchorMax = new Vector2(1, 1);
            rtHead.pivot = new Vector2(0.5f, 1);
            rtHead.anchoredPosition = Vector2.zero;
            rtHead.sizeDelta = new Vector2(0, 42);
            headGo.GetComponent<Image>().color = new Color(0.1f, 0.15f, 0.22f, 1f);

            var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(headGo.transform, false);
            var rtT = titleGo.GetComponent<RectTransform>();
            rtT.anchorMin = Vector2.zero; rtT.anchorMax = Vector2.one;
            rtT.offsetMin = new Vector2(15, 0); rtT.offsetMax = new Vector2(-15, 0);
            lectureHeaderTitleText = titleGo.GetComponent<TextMeshProUGUI>();
            lectureHeaderTitleText.fontSize = 15;
            lectureHeaderTitleText.fontStyle = FontStyles.Bold;
            lectureHeaderTitleText.color = new Color(0.95f, 0.77f, 0.06f);
            lectureHeaderTitleText.alignment = TextAlignmentOptions.MidlineLeft;
            lectureHeaderTitleText.text = "[LIVE] DANG PHAT: BAI GIANG DIEN TU TOAN 6";

            // 2. Slide Title
            var slideTitleGo = new GameObject("SlideTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            slideTitleGo.transform.SetParent(overlay.transform, false);
            var rtST = slideTitleGo.GetComponent<RectTransform>();
            rtST.anchorMin = new Vector2(0, 1); rtST.anchorMax = new Vector2(1, 1);
            rtST.pivot = new Vector2(0.5f, 1);
            rtST.anchoredPosition = new Vector2(0, -50);
            rtST.sizeDelta = new Vector2(-40, 36);
            lectureSlideTitleText = slideTitleGo.GetComponent<TextMeshProUGUI>();
            lectureSlideTitleText.fontSize = 18;
            lectureSlideTitleText.fontStyle = FontStyles.Bold;
            lectureSlideTitleText.color = new Color(0.2f, 0.85f, 0.85f);
            lectureSlideTitleText.alignment = TextAlignmentOptions.MidlineLeft;

            // 3. Body Slide Content Text
            var bodyGo = new GameObject("SlideBody", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyGo.transform.SetParent(overlay.transform, false);
            var rtBody = bodyGo.GetComponent<RectTransform>();
            rtBody.anchorMin = new Vector2(0, 0); rtBody.anchorMax = new Vector2(1, 1);
            rtBody.pivot = new Vector2(0.5f, 0.5f);
            rtBody.offsetMin = new Vector2(24, 65);
            rtBody.offsetMax = new Vector2(-24, -90);
            lectureBodyContentText = bodyGo.GetComponent<TextMeshProUGUI>();
            lectureBodyContentText.fontSize = 16;
            lectureBodyContentText.lineSpacing = 12;
            lectureBodyContentText.color = new Color(0.92f, 0.95f, 0.98f);
            lectureBodyContentText.alignment = TextAlignmentOptions.TopLeft;

            // 4. Bottom Control Bar
            var bottomGo = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
            bottomGo.transform.SetParent(overlay.transform, false);
            var rtBot = bottomGo.GetComponent<RectTransform>();
            rtBot.anchorMin = new Vector2(0, 0); rtBot.anchorMax = new Vector2(1, 0);
            rtBot.pivot = new Vector2(0.5f, 0);
            rtBot.anchoredPosition = Vector2.zero;
            rtBot.sizeDelta = new Vector2(0, 52);
            bottomGo.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.18f, 1f);

            // Progress Bar Track & Fill
            var trackGo = new GameObject("ProgressTrack", typeof(RectTransform), typeof(Image));
            trackGo.transform.SetParent(bottomGo.transform, false);
            var rtTrack = trackGo.GetComponent<RectTransform>();
            rtTrack.anchorMin = new Vector2(0, 1); rtTrack.anchorMax = new Vector2(1, 1);
            rtTrack.pivot = new Vector2(0.5f, 1);
            rtTrack.anchoredPosition = new Vector2(0, 0);
            rtTrack.sizeDelta = new Vector2(0, 5);
            trackGo.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f);

            var fillGo = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(trackGo.transform, false);
            var rtFill = fillGo.GetComponent<RectTransform>();
            rtFill.anchorMin = new Vector2(0, 0); rtFill.anchorMax = new Vector2(0, 1);
            rtFill.pivot = new Vector2(0, 0.5f);
            rtFill.offsetMin = Vector2.zero; rtFill.offsetMax = Vector2.zero;
            lectureProgressBarFill = fillGo.GetComponent<Image>();
            lectureProgressBarFill.color = new Color(0.95f, 0.77f, 0.06f);

            // Time Counter Text
            var timeGo = new GameObject("TimeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            timeGo.transform.SetParent(bottomGo.transform, false);
            var rtTm = timeGo.GetComponent<RectTransform>();
            rtTm.anchorMin = new Vector2(0, 0.5f); rtTm.anchorMax = new Vector2(0, 0.5f);
            rtTm.pivot = new Vector2(0.5f, 0.5f);
            rtTm.anchoredPosition = new Vector2(15, -4);
            rtTm.sizeDelta = new Vector2(120, 30);
            lectureTimeCounterText = timeGo.GetComponent<TextMeshProUGUI>();
            lectureTimeCounterText.fontSize = 13;
            lectureTimeCounterText.color = new Color(0.7f, 0.8f, 0.9f);
            lectureTimeCounterText.alignment = TextAlignmentOptions.MidlineLeft;
            lectureTimeCounterText.text = "00:00 / 02:00";

            // Control Buttons: [< Truoc] [|| Tam dung] [Sau >] [Phat lai]
            prevSlideButton = CreatePlayerButton(bottomGo, "BtnPrev", "[<] Truoc", new Vector2(145, -4), new Vector2(75, 30));
            prevSlideButton.onClick.AddListener(OnPrevSlideClicked);

            pauseResumeButton = CreatePlayerButton(bottomGo, "BtnPause", "[||] Tam dung", new Vector2(230, -4), new Vector2(100, 30));
            pauseResumeButtonText = pauseResumeButton.GetComponentInChildren<TextMeshProUGUI>();
            pauseResumeButton.onClick.AddListener(OnTogglePauseResumeClicked);

            nextSlideButton = CreatePlayerButton(bottomGo, "BtnNext", "Sau [>]", new Vector2(340, -4), new Vector2(75, 30));
            nextSlideButton.onClick.AddListener(OnNextSlideClicked);

            replayButton = CreatePlayerButton(bottomGo, "BtnReplay", "Phat lai", new Vector2(425, -4), new Vector2(85, 30));
            replayButton.onClick.AddListener(OnReplayClicked);
        }

        private Button CreatePlayerButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent.transform, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            btnGo.GetComponent<Image>().color = new Color(0.18f, 0.28f, 0.42f);

            var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            var rtT = txtGo.GetComponent<RectTransform>();
            rtT.anchorMin = Vector2.zero; rtT.anchorMax = Vector2.one;
            rtT.offsetMin = Vector2.zero; rtT.offsetMax = Vector2.zero;
            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 12;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnGo.GetComponent<Button>();
        }

        private void LoadLessonDetails()
        {
            var lesson = LessonListUIController.CurrentSelectedLesson;
            if (lesson != null)
            {
                if (lessonTitleText != null) lessonTitleText.text = lesson.title;
                if (theoryBodyText != null) theoryBodyText.text = lesson.summaryText;
                currentVideoUrl = lesson.videoUrl;

                if (videoInfoText != null)
                {
                    videoInfoText.text = $"<b>Video bài giảng:</b> {lesson.title}  |  <b>Nguồn:</b> Bộ GD&ĐT Toán Lớp 6";
                }
                GenerateLectureSlides(lesson);
            }
            else
            {
                // Dữ liệu mẫu khi test trực tiếp Scene trong Editor
                if (lessonTitleText != null) lessonTitleText.text = "Bài 1: Phép cộng và trừ số nguyên";
                if (theoryBodyText != null)
                {
                    theoryBodyText.text = "1. Cộng hai số nguyên cùng dấu:\n- Muốn cộng hai số nguyên âm, ta cộng hai phần tự nhiên rồi đặt dấu '-' trước kết quả.\nVí dụ: (-5) + (-3) = -(5 + 3) = -8.\n\n2. Cộng hai số nguyên khác dấu:\n- Lấy số có phần tự nhiên lớn hơn trừ số có phần tự nhiên bé hơn, đặt dấu của số có phần tự nhiên lớn hơn trước hiệu.\nVí dụ: (-15) + 7 = -(15 - 7) = -8.";
                }
                currentVideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
                if (videoInfoText != null) videoInfoText.text = "<b>Video bài giảng:</b> Bài 1: Phép cộng và trừ số nguyên";

                var dummyLesson = new Math6Companion.Core.Contracts.LessonData
                {
                    lessonId = "LES_DEV_01",
                    title = "Bài 1: Phép cộng và trừ số nguyên",
                    topic = "SoNguyen",
                    summaryText = theoryBodyText.text
                };
                GenerateLectureSlides(dummyLesson);
            }

            // Tải Thumbnail YouTube Preview nếu có URL hợp lệ
            string videoId = ExtractYouTubeVideoId(currentVideoUrl);
            if (!string.IsNullOrEmpty(videoId))
            {
                StartCoroutine(LoadYouTubeThumbnail(videoId));
            }
        }

        private void GenerateLectureSlides(Math6Companion.Core.Contracts.LessonData lesson)
        {
            currentLectureSlides.Clear();
            string title = lesson != null ? lesson.title : "Toán Lớp 6";
            string summary = lesson != null && !string.IsNullOrEmpty(lesson.summaryText) ? lesson.summaryText : "";

            // Slide 1: Khái niệm & Mục tiêu
            currentLectureSlides.Add(new LectureSlideData
            {
                sectionTitle = "Phần 1: Khái niệm & Mục tiêu bài học",
                bodyText = $"Chào các em! Trong bài giảng <b>{title}</b> hôm nay, chúng ta sẽ làm chủ các kiến thức trọng tâm:\n\n" +
                           $"- Nắm vững bản chất toán học và ý nghĩa thực tế.\n" +
                           $"- Thành thạo các bước giải chi tiết và tránh bẫy tính toán.\n" +
                           $"- Ứng dụng giải quyết các bài toán thực tế của chương trình Toán 6 mới.",
                startSecond = 0f,
                durationSecond = 30f
            });

            // Slide 2: Quy tắc và Công thức Cốt Lõi
            currentLectureSlides.Add(new LectureSlideData
            {
                sectionTitle = "Phần 2: Quy tắc & Công thức cốt lõi",
                bodyText = !string.IsNullOrEmpty(summary)
                    ? $"<color=#f1c40f><b>Tóm tắt lý thuyết trọng tâm:</b></color>\n\n{summary}"
                    : "- <b>Quy tắc thực hiện:</b> Ưu tiên ngoặc tròn () -> ngoặc vuông [] -> ngoặc nhọn {}.\n- Phép tính: Lũy thừa -> Nhân & Chia -> Cộng & Trừ.\n- Đổi dấu khi chuyển vế hoặc phá ngoặc có dấu trừ đằng trước.",
                startSecond = 30f,
                durationSecond = 35f
            });

            // Slide 3: Ví Dụ Mẫu & Phương Pháp Giải
            currentLectureSlides.Add(new LectureSlideData
            {
                sectionTitle = "Phần 3: Ví dụ mẫu minh họa trực quan",
                bodyText = "<b>Bài toán mẫu:</b>\n" +
                           "Thực hiện phép tính: <color=#00d2d3><b>A = (-15) + 28 - (-12)</b></color>\n\n" +
                           "<b>Các bước thực hiện chuẩn:</b>\n" +
                           "1. Quy đổi phép trừ sang cộng số đối: A = (-15) + 28 + 12\n" +
                           "2. Nhóm các số nguyên dương thuận tiện: A = (-15) + (28 + 12)\n" +
                           "3. Tính trong ngoặc: A = (-15) + 40\n" +
                           "4. Cộng hai số nguyên khác dấu: <color=#2ecc71><b>A = 25</b></color> (Chính xác!)",
                startSecond = 65f,
                durationSecond = 35f
            });

            // Slide 4: Tổng Kết & Ghi Nhớ
            currentLectureSlides.Add(new LectureSlideData
            {
                sectionTitle = "Phần 4: Tổng kết & Luyện tập ứng dụng",
                bodyText = "<b>Ghi nhớ quan trọng để đạt điểm tối đa:</b>\n\n" +
                           "+ Luôn kiểm tra kỹ dấu âm/dương trước khi đặt kết quả.\n" +
                           "+ Tối giản phân số và viết rõ đơn vị khi làm bài tự luận.\n\n" +
                           "-> <i>Hãy nhấn nút 'Luyện Trắc Nghiệm' hoặc 'Luyện Tự Luận' bên phải để thực hành ngay kiến thức vừa học!</i>",
                startSecond = 100f,
                durationSecond = 20f
            });
        }

        public string ExtractYouTubeVideoId(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            try
            {
                if (url.Contains("youtu.be/"))
                {
                    var parts = url.Split(new string[] { "youtu.be/" }, System.StringSplitOptions.None);
                    if (parts.Length > 1) return parts[1].Split('?')[0].Split('&')[0];
                }
                else if (url.Contains("watch?v="))
                {
                    var parts = url.Split(new string[] { "watch?v=" }, System.StringSplitOptions.None);
                    if (parts.Length > 1) return parts[1].Split('&')[0].Split('#')[0];
                }
                else if (url.Contains("embed/"))
                {
                    var parts = url.Split(new string[] { "embed/" }, System.StringSplitOptions.None);
                    if (parts.Length > 1) return parts[1].Split('?')[0];
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LessonContent] Lỗi tách YouTube ID: {ex.Message}");
            }

            return null;
        }

        private System.Collections.IEnumerator LoadYouTubeThumbnail(string videoId)
        {
            string thumbUrl = $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";
            using (var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(thumbUrl))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(req);
                    if (tex != null && videoThumbnailPreview != null)
                    {
                        videoThumbnailPreview.texture = tex;
                        videoThumbnailPreview.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void OnPlayInGameVideoClicked()
        {
            PlayClick();

            if (inGamePlayButton != null) inGamePlayButton.gameObject.SetActive(false);

            // 1. Nếu là video file trực tiếp (mp4, webm), chạy bằng Unity VideoPlayer
            if (videoPlayer != null && !string.IsNullOrEmpty(currentVideoUrl))
            {
                if (currentVideoUrl.EndsWith(".mp4", System.StringComparison.OrdinalIgnoreCase) || 
                    currentVideoUrl.EndsWith(".webm", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (videoThumbnailPreview != null) videoThumbnailPreview.texture = videoRenderTexture;
                    videoPlayer.url = currentVideoUrl;
                    videoPlayer.Play();
                    return;
                }
            }

            // 2. Chạy Trình chiếu Bài Giảng Hoạt Họa Tương Tác Trực Tiếp Trong Game
            if (lecturePlayerOverlay != null)
            {
                lecturePlayerOverlay.SetActive(true);
                StartLecturePlayback();
            }

            if (videoInfoText != null)
            {
                var lesson = LessonListUIController.CurrentSelectedLesson;
                string title = lesson != null ? lesson.title : "Toán 6";
                videoInfoText.text = $"<color=#2ecc71>[DANG PHAT]</color> <b>{title}</b>";
            }
        }

        private void StartLecturePlayback()
        {
            isLecturePlaying = true;
            playbackTimer = 0f;
            currentSlideIndex = 0;
            if (pauseResumeButtonText != null) pauseResumeButtonText.text = "[||] Tam dung";

            if (lecturePlaybackCoroutine != null) StopCoroutine(lecturePlaybackCoroutine);
            lecturePlaybackCoroutine = StartCoroutine(LecturePlaybackLoop());
        }

        private System.Collections.IEnumerator LecturePlaybackLoop()
        {
            while (playbackTimer < TOTAL_LECTURE_TIME)
            {
                if (isLecturePlaying)
                {
                    playbackTimer += Time.deltaTime;
                    UpdateLectureUI();
                }
                yield return null;
            }

            // Kết thúc bài giảng
            isLecturePlaying = false;
            if (pauseResumeButtonText != null) pauseResumeButtonText.text = "Phat lai";
        }

        private void UpdateLectureUI()
        {
            // Cập nhật thanh thời lượng (Progress Bar)
            float progress = Mathf.Clamp01(playbackTimer / TOTAL_LECTURE_TIME);
            if (lectureProgressBarFill != null)
            {
                var rt = lectureProgressBarFill.rectTransform;
                rt.anchorMax = new Vector2(progress, 1f);
            }

            // Cập nhật text thời gian (vd: 00:35 / 02:00)
            if (lectureTimeCounterText != null)
            {
                int curMin = (int)playbackTimer / 60;
                int curSec = (int)playbackTimer % 60;
                int totMin = (int)TOTAL_LECTURE_TIME / 60;
                int totSec = (int)TOTAL_LECTURE_TIME % 60;
                lectureTimeCounterText.text = $"{curMin:D2}:{curSec:D2} / {totMin:D2}:{totSec:D2}";
            }

            // Xác định slide hiện tại theo thời gian
            for (int i = currentLectureSlides.Count - 1; i >= 0; i--)
            {
                if (playbackTimer >= currentLectureSlides[i].startSecond)
                {
                    if (currentSlideIndex != i)
                    {
                        currentSlideIndex = i;
                        RenderSlide(currentSlideIndex);
                    }
                    break;
                }
            }
        }

        private void RenderSlide(int slideIdx)
        {
            if (slideIdx < 0 || slideIdx >= currentLectureSlides.Count) return;
            var slide = currentLectureSlides[slideIdx];

            if (lectureSlideTitleText != null)
            {
                lectureSlideTitleText.text = $"<b>{slide.sectionTitle}</b> <color=#95a5a6>({slideIdx + 1}/{currentLectureSlides.Count})</color>";
            }

            if (lectureBodyContentText != null)
            {
                lectureBodyContentText.text = slide.bodyText;
            }

            var lesson = LessonListUIController.CurrentSelectedLesson;
            string lessonTitle = lesson != null ? lesson.title : "Bài Giảng Điện Tử Toán 6";
            if (lectureHeaderTitleText != null)
            {
                lectureHeaderTitleText.text = $"[LIVE] DANG PHAT: <b>{lessonTitle}</b>";
            }
        }

        private void OnTogglePauseResumeClicked()
        {
            PlayClick();
            if (playbackTimer >= TOTAL_LECTURE_TIME)
            {
                OnReplayClicked();
                return;
            }

            isLecturePlaying = !isLecturePlaying;
            if (pauseResumeButtonText != null)
            {
                pauseResumeButtonText.text = isLecturePlaying ? "[||] Tam dung" : "[>] Tiep tuc";
            }
        }

        private void OnPrevSlideClicked()
        {
            PlayClick();
            if (currentSlideIndex > 0)
            {
                currentSlideIndex--;
                playbackTimer = currentLectureSlides[currentSlideIndex].startSecond;
                RenderSlide(currentSlideIndex);
                UpdateLectureUI();
            }
        }

        private void OnNextSlideClicked()
        {
            PlayClick();
            if (currentSlideIndex < currentLectureSlides.Count - 1)
            {
                currentSlideIndex++;
                playbackTimer = currentLectureSlides[currentSlideIndex].startSecond;
                RenderSlide(currentSlideIndex);
                UpdateLectureUI();
            }
        }

        private void OnReplayClicked()
        {
            PlayClick();
            StartLecturePlayback();
        }

        public void SwitchTab(string tabName)
        {
            PlayClick();
            bool isText = tabName == "TEXT";

            if (textContentPanel != null) textContentPanel.SetActive(isText);
            if (videoContentPanel != null) videoContentPanel.SetActive(!isText);

            if (textTabHighlight != null) textTabHighlight.gameObject.SetActive(isText);
            if (videoTabHighlight != null) videoTabHighlight.gameObject.SetActive(!isText);

            if (isText)
            {
                if (videoPlayer != null && videoPlayer.isPlaying) videoPlayer.Pause();
                isLecturePlaying = false;
                if (pauseResumeButtonText != null) pauseResumeButtonText.text = "[>] Tiep tuc";
            }
        }

        private void OnOpenVideoUrl()
        {
            PlayClick();
            if (!string.IsNullOrEmpty(currentVideoUrl))
            {
                Application.OpenURL(currentVideoUrl);
            }
        }

        private void OnBackToListClicked()
        {
            PlayClick();
            if (videoPlayer != null && videoPlayer.isPlaying) videoPlayer.Stop();
            if (lecturePlaybackCoroutine != null) StopCoroutine(lecturePlaybackCoroutine);
            SceneManager.LoadScene("03_LessonList");
        }

        private void PlayClick()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }

        private void OnDestroy()
        {
            if (lecturePlaybackCoroutine != null) StopCoroutine(lecturePlaybackCoroutine);

            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                Destroy(videoRenderTexture);
            }
        }
    }
}
