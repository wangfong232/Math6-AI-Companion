using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;
using Math6Companion.Core.Services;

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

        // Thanh điều khiển Video Player trong game (In-Game Video Controls Bar)
        private GameObject videoControlsBar;
        private Button videoPlayPauseButton;
        private TextMeshProUGUI videoPlayPauseText;
        private Slider videoTimelineSlider;
        private TextMeshProUGUI videoTimeDisplay;
        private Button videoReplayButton;
        private Button videoMuteButton;
        private TextMeshProUGUI videoMuteText;
        private bool isVideoControlsDragging = false;
        private bool isVideoMuted = false;
        private bool isStreamVideoPlaying = false;

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


        private void Update()
        {
            if (isStreamVideoPlaying && videoPlayer != null && videoPlayer.isPlaying)
            {
                if (!isVideoControlsDragging && videoTimelineSlider != null && videoPlayer.length > 0)
                {
                    float progress = Mathf.Clamp01((float)(videoPlayer.time / videoPlayer.length));
                    videoTimelineSlider.SetValueWithoutNotify(progress);
                }

                if (videoTimeDisplay != null)
                {
                    int curMin = (int)videoPlayer.time / 60;
                    int curSec = (int)videoPlayer.time % 60;
                    int totMin = (int)videoPlayer.length / 60;
                    int totSec = (int)videoPlayer.length % 60;
                    videoTimeDisplay.text = $"{curMin:D2}:{curSec:D2} / {totMin:D2}:{totSec:D2}";
                }
            }
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
                tmpI.text = ">";
                tmpI.fontSize = 38;
                tmpI.alignment = TextAlignmentOptions.Center;
                tmpI.color = Color.white;
            }

            inGamePlayButton = playOverlayGo.GetComponent<Button>();
            inGamePlayButton.onClick.RemoveAllListeners();
            inGamePlayButton.onClick.AddListener(OnPlayInGameVideoClicked);

            // Xây dựng Giao Diện Bài Giảng Điện Tử Hoạt Họa Trong Game (Interactive Lecture Screen)
            BuildLecturePlayerUI(screenContainer);

            // Xây dựng Thanh Điều Khiển Video Player Trong Game (In-Game Video Controls Bar)
            BuildVideoControlsBar(screenContainer);

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

            // Định vị lại 2 nút điều hướng phía dưới màn hình video
            // Nút 1: Mở xem trực tiếp trên YouTube (watch?v=) - Tránh 100% Lỗi 153 của Embed
            if (playExternalVideoButton != null)
            {
                var rtBtn = playExternalVideoButton.GetComponent<RectTransform>();
                rtBtn.anchorMin = new Vector2(0.5f, 0.5f);
                rtBtn.anchorMax = new Vector2(0.5f, 0.5f);
                rtBtn.pivot = new Vector2(0.5f, 0.5f);
                rtBtn.anchoredPosition = new Vector2(-190, -215);
                rtBtn.sizeDelta = new Vector2(360, 44);

                var imgBtn = playExternalVideoButton.GetComponent<Image>();
                if (imgBtn != null) imgBtn.color = new Color(0.85f, 0.16f, 0.16f);

                var txtBtn = playExternalVideoButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txtBtn != null)
                {
                    txtBtn.text = "<b>Xem Video Bài Giảng Trên YouTube</b>";
                    txtBtn.fontSize = 15;
                    txtBtn.color = Color.white;
                }

                playExternalVideoButton.onClick.RemoveAllListeners();
                playExternalVideoButton.onClick.AddListener(OnOpenVideoUrl);
            }

            // Nút 2: Mở Slide Bài Giảng Tương Tác Trong Game
            var inGameSlideBtnGo = videoContentPanel.transform.Find("Btn_InGameSlides")?.gameObject;
            if (inGameSlideBtnGo == null)
            {
                inGameSlideBtnGo = new GameObject("Btn_InGameSlides", typeof(RectTransform), typeof(Image), typeof(Button));
                inGameSlideBtnGo.transform.SetParent(videoContentPanel.transform, false);
                var rt = inGameSlideBtnGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(190, -215);
                rt.sizeDelta = new Vector2(360, 44);
                inGameSlideBtnGo.GetComponent<Image>().color = new Color(0.12f, 0.45f, 0.75f);

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(inGameSlideBtnGo.transform, false);
                var rtT = txtGo.GetComponent<RectTransform>();
                rtT.anchorMin = Vector2.zero; rtT.anchorMax = Vector2.one;
                rtT.offsetMin = Vector2.zero; rtT.offsetMax = Vector2.zero;
                var tmp = txtGo.GetComponent<TextMeshProUGUI>();
                tmp.text = "<b>Xem Slide Tóm Tắt Trong Game</b>";
                tmp.fontSize = 15;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }
            var btnSlides = inGameSlideBtnGo.GetComponent<Button>();
            btnSlides.onClick.RemoveAllListeners();
            btnSlides.onClick.AddListener(() =>
            {
                PlayClick();
                if (lecturePlayerOverlay != null)
                {
                    lecturePlayerOverlay.SetActive(true);
                    StartLecturePlayback();
                }
            });
        }


        private void BuildVideoControlsBar(GameObject container)
        {
            var barGo = container.transform.Find("VideoControlsBar")?.gameObject;
            if (barGo == null)
            {
                barGo = new GameObject("VideoControlsBar", typeof(RectTransform), typeof(Image));
                barGo.transform.SetParent(container.transform, false);
                var rt = barGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(0.5f, 0);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0, 44);
                barGo.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.12f, 0.95f);
            }
            videoControlsBar = barGo;
            videoControlsBar.SetActive(false);

            // Nút Play / Pause
            var btnPP = barGo.transform.Find("Btn_PlayPause")?.gameObject;
            if (btnPP == null)
            {
                btnPP = new GameObject("Btn_PlayPause", typeof(RectTransform), typeof(Image), typeof(Button));
                btnPP.transform.SetParent(barGo.transform, false);
                var rt = btnPP.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(30, 0);
                rt.sizeDelta = new Vector2(38, 32);
                btnPP.GetComponent<Image>().color = new Color(0.18f, 0.55f, 0.92f);

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(btnPP.transform, false);
                var rtT = txtGo.GetComponent<RectTransform>();
                rtT.anchorMin = Vector2.zero; rtT.anchorMax = Vector2.one;
                rtT.offsetMin = Vector2.zero; rtT.offsetMax = Vector2.zero;
                videoPlayPauseText = txtGo.GetComponent<TextMeshProUGUI>();
                videoPlayPauseText.text = "[||]";
                videoPlayPauseText.fontSize = 16;
                videoPlayPauseText.fontStyle = FontStyles.Bold;
                videoPlayPauseText.alignment = TextAlignmentOptions.Center;
                videoPlayPauseText.color = Color.white;
            }
            videoPlayPauseButton = btnPP.GetComponent<Button>();
            videoPlayPauseButton.onClick.RemoveAllListeners();
            videoPlayPauseButton.onClick.AddListener(OnToggleVideoPlayPause);

            // Time Display text
            var timeGo = barGo.transform.Find("TimeDisplay")?.gameObject;
            if (timeGo == null)
            {
                timeGo = new GameObject("TimeDisplay", typeof(RectTransform), typeof(TextMeshProUGUI));
                timeGo.transform.SetParent(barGo.transform, false);
                var rt = timeGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(105, 0);
                rt.sizeDelta = new Vector2(105, 28);
                videoTimeDisplay = timeGo.GetComponent<TextMeshProUGUI>();
                videoTimeDisplay.text = "00:00 / 00:00";
                videoTimeDisplay.fontSize = 13;
                videoTimeDisplay.alignment = TextAlignmentOptions.MidlineLeft;
                videoTimeDisplay.color = new Color(0.85f, 0.92f, 1f);
            }

            // Timeline Slider
            var sliderGo = barGo.transform.Find("TimelineSlider")?.gameObject;
            if (sliderGo == null)
            {
                sliderGo = new GameObject("TimelineSlider", typeof(RectTransform), typeof(Slider));
                sliderGo.transform.SetParent(barGo.transform, false);
                var rt = sliderGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(1, 0.5f);
                rt.offsetMin = new Vector2(170, -10);
                rt.offsetMax = new Vector2(-110, 10);

                var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(sliderGo.transform, false);
                var rtBg = bgGo.GetComponent<RectTransform>();
                rtBg.anchorMin = new Vector2(0, 0.25f);
                rtBg.anchorMax = new Vector2(1, 0.75f);
                rtBg.offsetMin = Vector2.zero; rtBg.offsetMax = Vector2.zero;
                bgGo.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 0.8f);

                var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
                fillAreaGo.transform.SetParent(sliderGo.transform, false);
                var rtFA = fillAreaGo.GetComponent<RectTransform>();
                rtFA.anchorMin = new Vector2(0, 0.25f);
                rtFA.anchorMax = new Vector2(1, 0.75f);
                rtFA.offsetMin = Vector2.zero; rtFA.offsetMax = Vector2.zero;

                var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(fillAreaGo.transform, false);
                var rtFill = fillGo.GetComponent<RectTransform>();
                rtFill.sizeDelta = Vector2.zero;
                fillGo.GetComponent<Image>().color = new Color(0.22f, 0.65f, 0.95f);

                var sliderComp = sliderGo.GetComponent<Slider>();
                sliderComp.fillRect = rtFill;
                sliderComp.minValue = 0f;
                sliderComp.maxValue = 1f;
                videoTimelineSlider = sliderComp;
            }
            if (videoTimelineSlider != null)
            {
                videoTimelineSlider.onValueChanged.RemoveAllListeners();
                videoTimelineSlider.onValueChanged.AddListener(OnVideoTimelineSeek);
            }

            // Nút Replay [<<]
            var btnRep = barGo.transform.Find("Btn_Replay")?.gameObject;
            if (btnRep == null)
            {
                btnRep = new GameObject("Btn_Replay", typeof(RectTransform), typeof(Image), typeof(Button));
                btnRep.transform.SetParent(barGo.transform, false);
                var rt = btnRep.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0.5f);
                rt.anchorMax = new Vector2(1, 0.5f);
                rt.anchoredPosition = new Vector2(-75, 0);
                rt.sizeDelta = new Vector2(34, 30);
                btnRep.GetComponent<Image>().color = new Color(0.15f, 0.25f, 0.35f);

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(btnRep.transform, false);
                var rtT = txtGo.GetComponent<RectTransform>();
                rtT.anchorMin = Vector2.zero; rtT.anchorMax = Vector2.one;
                rtT.offsetMin = Vector2.zero; rtT.offsetMax = Vector2.zero;
                var tmp = txtGo.GetComponent<TextMeshProUGUI>();
                tmp.text = "[R]";
                tmp.fontSize = 15;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }
            videoReplayButton = btnRep.GetComponent<Button>();
            videoReplayButton.onClick.RemoveAllListeners();
            videoReplayButton.onClick.AddListener(OnReplayVideoInGame);

            // Nút Mute [Mute / Sound]
            var btnMute = barGo.transform.Find("Btn_Mute")?.gameObject;
            if (btnMute == null)
            {
                btnMute = new GameObject("Btn_Mute", typeof(RectTransform), typeof(Image), typeof(Button));
                btnMute.transform.SetParent(barGo.transform, false);
                var rt = btnMute.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0.5f);
                rt.anchorMax = new Vector2(1, 0.5f);
                rt.anchoredPosition = new Vector2(-30, 0);
                rt.sizeDelta = new Vector2(34, 30);
                btnMute.GetComponent<Image>().color = new Color(0.15f, 0.25f, 0.35f);

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(btnMute.transform, false);
                var rtT = txtGo.GetComponent<RectTransform>();
                rtT.anchorMin = Vector2.zero; rtT.anchorMax = Vector2.one;
                rtT.offsetMin = Vector2.zero; rtT.offsetMax = Vector2.zero;
                videoMuteText = txtGo.GetComponent<TextMeshProUGUI>();
                videoMuteText.text = "[Vol]";
                videoMuteText.fontSize = 16;
                videoMuteText.alignment = TextAlignmentOptions.Center;
                videoMuteText.color = Color.white;
            }
            videoMuteButton = btnMute.GetComponent<Button>();
            videoMuteButton.onClick.RemoveAllListeners();
            videoMuteButton.onClick.AddListener(OnToggleMuteVideo);
        }

        private void OnToggleVideoPlayPause()
        {
            PlayClick();
            if (videoPlayer == null) return;

            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
                if (videoPlayPauseText != null) videoPlayPauseText.text = ">";
            }
            else
            {
                videoPlayer.Play();
                if (videoPlayPauseText != null) videoPlayPauseText.text = "[||]";
            }
        }

        private void OnVideoTimelineSeek(float value)
        {
            if (videoPlayer != null && videoPlayer.length > 0)
            {
                videoPlayer.time = value * videoPlayer.length;
            }
        }

        private void OnReplayVideoInGame()
        {
            PlayClick();
            if (videoPlayer != null)
            {
                videoPlayer.time = 0;
                videoPlayer.Play();
                if (videoPlayPauseText != null) videoPlayPauseText.text = "[||]";
            }
        }

        private void OnToggleMuteVideo()
        {
            PlayClick();
            if (videoPlayer == null) return;
            isVideoMuted = !isVideoMuted;
            videoPlayer.SetDirectAudioMute(0, isVideoMuted);
            if (videoMuteText != null) videoMuteText.text = isVideoMuted ? "[Mute]" : "[Vol]";
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

                // Tự động gán video YouTube Toán 6 thực tế nếu URL đang rỗng hoặc là link mock
                if (string.IsNullOrEmpty(currentVideoUrl) || currentVideoUrl.Contains("mock_lesson"))
                {
                    string topicNorm = (lesson.topic ?? "").ToLower();
                    if (topicNorm.Contains("frac") || topicNorm.Contains("phan"))
                        currentVideoUrl = "https://www.youtube.com/watch?v=ZKNbig3XpM8";
                    else if (topicNorm.Contains("hinh") || topicNorm.Contains("geom"))
                        currentVideoUrl = "https://www.youtube.com/watch?v=XUq_incBNXY";
                    else
                        currentVideoUrl = "https://www.youtube.com/watch?v=vcR0jey6xcI";
                }

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
                currentVideoUrl = "https://www.youtube.com/watch?v=vcR0jey6xcI";
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
                    if (parts.Length > 1) return parts[1].Split('?')[0].Split('&')[0].Split('#')[0];
                }
                else if (url.Contains("shorts/"))
                {
                    var parts = url.Split(new string[] { "shorts/" }, System.StringSplitOptions.None);
                    if (parts.Length > 1) return parts[1].Split('?')[0].Split('&')[0].Split('#')[0];
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

            var lesson = LessonListUIController.CurrentSelectedLesson;
            string lessonTitle = lesson != null ? lesson.title : "Toán Lớp 6";

            if (videoInfoText != null)
            {
                videoInfoText.text = $"<color=#f39c12>[...] Đang kết nối luồng phát video: <b>{lessonTitle}</b>...</color>";
            }

            // 1. Nếu là file video MP4 trực tiếp, phát ngay trên Unity VideoPlayer trong Canvas
            if (videoPlayer != null && !string.IsNullOrEmpty(currentVideoUrl) && 
               (currentVideoUrl.EndsWith(".mp4", System.StringComparison.OrdinalIgnoreCase) || currentVideoUrl.EndsWith(".webm", System.StringComparison.OrdinalIgnoreCase)))
            {
                PlayDirectStream(currentVideoUrl, lessonTitle);
                return;
            }

            // 2. Thử bóc tách luồng phát trực tiếp trong game qua YouTubeStreamResolver
            if (YouTubeStreamResolver.Instance != null && !string.IsNullOrEmpty(currentVideoUrl))
            {
                YouTubeStreamResolver.Instance.Resolve(currentVideoUrl, (success, streamUrl, err) =>
                {
                    if (this == null || gameObject == null) return;
                    if (success && !string.IsNullOrEmpty(streamUrl) && videoPlayer != null)
                    {
                        PlayDirectStream(streamUrl, lessonTitle);
                    }
                    else
                    {
                        // Fallback an toàn: Mở trình duyệt ngoài với safe watch?v= URL (không bao giờ bị lỗi 153 của Embed)
                        string safeUrl = GetSafeYouTubeWatchUrl(currentVideoUrl);
                        Debug.Log($"[LessonContent] Fallback sang YouTube Watch URL: {safeUrl}");
                        Application.OpenURL(safeUrl);
                        if (videoInfoText != null)
                        {
                            videoInfoText.text = $"<color=#2ecc71><b>[ĐÃ MỞ VIDEO BÀI GIẢNG]</b></color> <b>{lessonTitle}</b> (Đang phát trên YouTube không giới hạn bản quyền).";
                        }
                    }
                });
                return;
            }

            // 3. Fallback mặc định mở trình duyệt ngoài
            string fallbackUrl = GetSafeYouTubeWatchUrl(currentVideoUrl);
            Application.OpenURL(fallbackUrl);
        }

        private void PlayDirectStream(string streamUrl, string lessonTitle)
        {
            if (inGamePlayButton != null) inGamePlayButton.gameObject.SetActive(false);
            isStreamVideoPlaying = true;
            if (videoThumbnailPreview != null) videoThumbnailPreview.texture = videoRenderTexture;
            if (lecturePlayerOverlay != null) lecturePlayerOverlay.SetActive(false);

            videoPlayer.url = streamUrl;
            videoPlayer.prepareCompleted += (source) =>
            {
                source.Play();
                if (videoControlsBar != null) videoControlsBar.SetActive(true);
                if (videoPlayPauseText != null) videoPlayPauseText.text = "[||]";
                if (videoInfoText != null)
                {
                    videoInfoText.text = $"<color=#2ecc71><b>[ĐANG PHÁT TRỰC TIẾP TRONG GAME]</b></color> <b>{lessonTitle}</b>";
                }
            };
            videoPlayer.errorReceived += (source, message) =>
            {
                ActivateLectureFallback(lessonTitle);
            };
            videoPlayer.Prepare();
        }

        public string GetSafeYouTubeWatchUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return "https://www.youtube.com/watch?v=vcR0jey6xcI";
            string videoId = ExtractYouTubeVideoId(url);
            if (!string.IsNullOrEmpty(videoId))
            {
                return $"https://www.youtube.com/watch?v={videoId}";
            }
            return url;
        }

        private void ActivateLectureFallback(string lessonTitle)
        {
            isStreamVideoPlaying = false;
            if (videoControlsBar != null) videoControlsBar.SetActive(false);

            // 1. Chạy Trình chiếu Bài Giảng Hoạt Họa Tương Tác Trực Tiếp Trong Game
            if (lecturePlayerOverlay != null)
            {
                lecturePlayerOverlay.SetActive(true);
                StartLecturePlayback();
            }

            // 2. Cập nhật thông báo hướng dẫn học sinh
            if (videoInfoText != null)
            {
                videoInfoText.text = $"<color=#e67e22><b>[BÀI GIẢNG ĐIỆN TỬ TÓM TẮT]</b></color> <b>{lessonTitle}</b>  |  <i>(Em có thể bấm nút bên dưới để mở xem video gốc trên YouTube)</i>";
            }

            // 3. Làm nổi bật nút mở xem YouTube gốc ngoài trình duyệt
            if (playExternalVideoButton != null)
            {
                var txt = playExternalVideoButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = "<b>Mở Xem Video Gốc Trên YouTube (Trình Duyệt)</b>";
                    txt.color = new Color(1f, 0.85f, 0.2f);
                }
                var img = playExternalVideoButton.GetComponent<Image>();
                if (img != null) img.color = new Color(0.75f, 0.15f, 0.15f);
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
            string safeUrl = GetSafeYouTubeWatchUrl(currentVideoUrl);
            Application.OpenURL(safeUrl);
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
