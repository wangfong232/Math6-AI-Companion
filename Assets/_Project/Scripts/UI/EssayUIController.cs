using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;
using Math6Companion.Core.Contracts;
using Math6Companion.Core.Mock;
using Math6Companion.Core.Services;

namespace Math6Companion.UI
{
    public class EssayUIController : MonoBehaviour
    {
        [Header("Header & Problem")]
        [SerializeField] private TextMeshProUGUI problemTitleText;
        [SerializeField] private TextMeshProUGUI problemPromptText;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button prevQuestionButton;
        [SerializeField] private Button nextQuestionButton;
        [SerializeField] private TextMeshProUGUI questionProgressText;

        [Header("Photo Submission Mode")]
        [SerializeField] private Button capturePhotoButton;    // Button 1: Chụp ảnh camera
        [SerializeField] private Button pickImageButton;        // Button 2: Chọn ảnh từ máy tính
        [SerializeField] private Button loadSamplePhotoButton;  // Button 3: Tải ảnh vở bài tập mẫu (Test nhanh)
        [SerializeField] private RawImage imagePreview;
        [SerializeField] private GameObject previewContainer;
        [SerializeField] private TextMeshProUGUI photoStatusText;

        [Header("Input & Keypad (Tùy chọn)")]
        [SerializeField] private TMP_InputField solutionInputField;
        [SerializeField] private MathKeypadController mathKeypad;

        [Header("Action Buttons")]
        [SerializeField] private Button submitButton;
        [SerializeField] private Button closeButton;

        private INetworkService networkService;
        private IAITutorService aiTutorService;
        private QuestionData currentQuestion;
        private List<QuestionData> availableEssayQuestions = new List<QuestionData>();
        private int currentEssayIndex = 0;
        private WebCamTexture webCamTexture;
        private static WebCamTexture s_ActiveCam;
        private bool hasPhoto = false;

        private void Awake()
        {
            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmitClicked);

            if (capturePhotoButton != null)
                capturePhotoButton.onClick.AddListener(OnCapturePhotoClicked);

            if (pickImageButton != null)
                pickImageButton.onClick.AddListener(OnPickImageClicked);

            if (loadSamplePhotoButton != null)
                loadSamplePhotoButton.onClick.AddListener(OnLoadSamplePhotoClicked);

            if (prevQuestionButton != null)
                prevQuestionButton.onClick.AddListener(OnPrevQuestionClicked);

            if (nextQuestionButton != null)
                nextQuestionButton.onClick.AddListener(OnNextQuestionClicked);

            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(() =>
                {
                    StopAndReleaseCamera();
                    SceneManager.LoadScene(SessionManager.Instance != null ? SessionManager.Instance.ReturnSceneName : "02_VillageWorld");
                });

            if (closeButton != null)
                closeButton.onClick.AddListener(() =>
                {
                    StopAndReleaseCamera();
                    SceneManager.LoadScene(SessionManager.Instance != null ? SessionManager.Instance.ReturnSceneName : "02_VillageWorld");
                });
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StopAndReleaseCamera();
                SceneManager.LoadScene(SessionManager.Instance != null ? SessionManager.Instance.ReturnSceneName : "02_VillageWorld");
            }
        }

        private void Start()
        {
            networkService = Math6Companion.Core.Services.NetworkServiceProvider.NetworkService;
            aiTutorService = Math6Companion.Core.Services.NetworkServiceProvider.AITutorService;

            // Xóa sạch trạng thái cũ tránh rò rỉ câu sai trắc nghiệm sang tự luận
            ScoreSessionContext.Reset();
            ScoreSessionContext.ActivityType = "ESSAY";

            // Gắn bàn phím ảo vào ô input này
            if (mathKeypad != null && solutionInputField != null)
            {
                mathKeypad.SetTargetInputField(solutionInputField);
            }

            if (previewContainer != null) previewContainer.SetActive(false);

            EnsureNavigationButtons();

            string selectedTopic = PlayerPrefs.GetString("Current_Lesson_Topic", "ALL");
            string selectedLessonId = PlayerPrefs.GetString("Current_Lesson_ID", "");
            LoadEssayQuestions(selectedTopic, selectedLessonId);
        }

        private void EnsureNavigationButtons()
        {
            if (prevQuestionButton == null || nextQuestionButton == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null && problemTitleText != null)
                {
                    var navContainer = new GameObject("EssayNavContainer", typeof(RectTransform));
                    navContainer.transform.SetParent(problemTitleText.transform.parent, false);
                    var rt = navContainer.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 1);
                    rt.anchorMax = new Vector2(0.5f, 1);
                    rt.anchoredPosition = new Vector2(0, -95);
                    rt.sizeDelta = new Vector2(480, 40);

                    // Nút Prev
                    var prevGo = new GameObject("Btn_PrevEssay", typeof(RectTransform), typeof(Image), typeof(Button));
                    prevGo.transform.SetParent(navContainer.transform, false);
                    var rtPrev = prevGo.GetComponent<RectTransform>();
                    rtPrev.anchoredPosition = new Vector2(-150, 0);
                    rtPrev.sizeDelta = new Vector2(130, 36);
                    prevGo.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.55f);
                    prevQuestionButton = prevGo.GetComponent<Button>();
                    prevQuestionButton.onClick.AddListener(OnPrevQuestionClicked);
                    var txtP = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtP.transform.SetParent(prevGo.transform, false);
                    var tmpP = txtP.GetComponent<TextMeshProUGUI>();
                    tmpP.text = "◄ Câu Trước";
                    tmpP.fontSize = 16;
                    tmpP.alignment = TextAlignmentOptions.Center;
                    tmpP.color = Color.white;

                    // Text Progress
                    var progGo = new GameObject("Txt_Progress", typeof(RectTransform), typeof(TextMeshProUGUI));
                    progGo.transform.SetParent(navContainer.transform, false);
                    var rtProg = progGo.GetComponent<RectTransform>();
                    rtProg.anchoredPosition = new Vector2(0, 0);
                    rtProg.sizeDelta = new Vector2(120, 36);
                    questionProgressText = progGo.GetComponent<TextMeshProUGUI>();
                    questionProgressText.text = "Câu 1 / 1";
                    questionProgressText.fontSize = 18;
                    questionProgressText.fontStyle = FontStyles.Bold;
                    questionProgressText.alignment = TextAlignmentOptions.Center;
                    questionProgressText.color = new Color(0.95f, 0.75f, 0.2f);

                    // Nút Next
                    var nextGo = new GameObject("Btn_NextEssay", typeof(RectTransform), typeof(Image), typeof(Button));
                    nextGo.transform.SetParent(navContainer.transform, false);
                    var rtNext = nextGo.GetComponent<RectTransform>();
                    rtNext.anchoredPosition = new Vector2(150, 0);
                    rtNext.sizeDelta = new Vector2(130, 36);
                    nextGo.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.55f);
                    nextQuestionButton = nextGo.GetComponent<Button>();
                    nextQuestionButton.onClick.AddListener(OnNextQuestionClicked);
                    var txtN = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtN.transform.SetParent(nextGo.transform, false);
                    var tmpN = txtN.GetComponent<TextMeshProUGUI>();
                    tmpN.text = "Câu Tiếp ►";
                    tmpN.fontSize = 16;
                    tmpN.alignment = TextAlignmentOptions.Center;
                    tmpN.color = Color.white;
                }
            }
        }

        public void LoadEssayQuestions(string topic, string lessonId)
        {
            if (problemPromptText != null) problemPromptText.text = "Đang tải đề bài tự luận từ ngân hàng câu hỏi...";
            string diffFilter = PlayerPrefs.GetString("Quiz_Difficulty_Filter", "ALL");
            string currentLessonTitle = LessonListUIController.CurrentSelectedLesson != null ? LessonListUIController.CurrentSelectedLesson.title : "";
            string currentLessonTopic = LessonListUIController.CurrentSelectedLesson != null ? LessonListUIController.CurrentSelectedLesson.topic : topic;

            bool isSpecificLesson = !string.IsNullOrEmpty(lessonId) || !string.IsNullOrEmpty(currentLessonTitle);
            bool isSpecificTopic = !string.IsNullOrEmpty(currentLessonTopic) && !currentLessonTopic.Equals("ALL", StringComparison.OrdinalIgnoreCase);

            string queryTopic = isSpecificLesson ? currentLessonTopic : (isSpecificTopic ? currentLessonTopic : "ALL");

            if (networkService != null)
            {
                networkService.GetQuestions(queryTopic, "6", (success, questions, msg) =>
                {
                    if (this == null || gameObject == null) return;

                    availableEssayQuestions.Clear();
                    if (success && questions != null && questions.Count > 0)
                    {
                        foreach (var q in questions)
                        {
                            if (q.type == "ESSAY" || (q.rubric != null && q.rubric.Count > 0))
                            {
                                bool diffMatch = string.IsNullOrEmpty(diffFilter) || diffFilter == "ALL" || GoogleSheetsNetworkService.IsDifficultyMatch(q.difficulty, diffFilter);
                                
                                bool contentMatch = false;
                                if (isSpecificLesson)
                                {
                                    contentMatch = (!string.IsNullOrEmpty(lessonId) && q.lessonId.Equals(lessonId, StringComparison.OrdinalIgnoreCase)) || GoogleSheetsNetworkService.IsTopicMatch(q.topic, currentLessonTopic);
                                }
                                else if (isSpecificTopic)
                                {
                                    contentMatch = GoogleSheetsNetworkService.IsTopicMatch(q.topic, currentLessonTopic);
                                }
                                else
                                {
                                    contentMatch = true; // Mục 3: Luyện tập tự luận tổng hợp: Lấy toàn bộ câu tự luận trên sheet!
                                }

                                if (diffMatch && contentMatch)
                                {
                                    availableEssayQuestions.Add(q);
                                }
                            }
                        }
                    }

                    // Nếu Google Sheet chưa có câu tự luận, nạp bộ câu hỏi tự luận dự phòng
                    if (availableEssayQuestions.Count == 0)
                    {
                        var topicEssayList = GetTopicFallbackEssayQuestions(currentLessonTopic, currentLessonTitle, diffFilter);
                        availableEssayQuestions.AddRange(topicEssayList);
                    }
                    else if (!isSpecificLesson && !isSpecificTopic)
                    {
                        // Trộn ngẫu nhiên câu hỏi tự luận từ toàn bộ kho
                        var rnd = new System.Random();
                        for (int i = availableEssayQuestions.Count - 1; i > 0; i--)
                        {
                            int j = rnd.Next(i + 1);
                            var t = availableEssayQuestions[i]; availableEssayQuestions[i] = availableEssayQuestions[j]; availableEssayQuestions[j] = t;
                        }
                    }

                    if (availableEssayQuestions.Count > 0)
                    {
                        currentEssayIndex = 0;
                        SetCurrentEssayQuestion(availableEssayQuestions[0]);
                    }
                    else
                    {
                        InitDefaultSampleProblem();
                    }
                    UpdateNavButtonsState();
                });
            }
            else
            {
                var topicEssayList = GetTopicFallbackEssayQuestions(currentLessonTopic, currentLessonTitle, diffFilter);
                availableEssayQuestions = topicEssayList;
                if (availableEssayQuestions.Count > 0)
                {
                    currentEssayIndex = 0;
                    SetCurrentEssayQuestion(availableEssayQuestions[0]);
                }
                else
                {
                    InitDefaultSampleProblem();
                }
                UpdateNavButtonsState();
            }
        }

        private List<QuestionData> GetTopicFallbackEssayQuestions(string topic, string lessonTitle, string diffFilter)
        {
            var list = new List<QuestionData>();
            string norm = (topic ?? "").ToLowerInvariant();
            string titleNorm = (lessonTitle ?? "").ToLowerInvariant();

            if (norm.Contains("tap") || norm.Contains("set") || titleNorm.Contains("tập hợp") || titleNorm.Contains("bài 1") || titleNorm.Contains("bài 2") || titleNorm.Contains("bài 3"))
            {
                list.Add(new QuestionData
                {
                    questionId = "ESSAY_SET_01",
                    topic = "Tập hợp & Phần tử của tập hợp",
                    prompt = "Cho tập hợp A các số tự nhiên lớn hơn 3 và không vượt quá 8:\n a) Viết tập hợp A bằng 2 cách (Liệt kê phần tử & Chỉ ra tính chất đặc trưng).\n b) Điền ký hiệu thích hợp (∈ hoặc ∉) vào chỗ trống: 4 ... A ; 9 ... A ; 8 ... A.",
                    maxScore = 10,
                    rubric = new List<RubricItem>
                    {
                        new RubricItem { step = 1, description = "Liệt kê phần tử: A = {4; 5; 6; 7; 8}", points = 4 },
                        new RubricItem { step = 2, description = "Tính chất đặc trưng: A = {x ∈ ℕ | 3 < x ≤ 8}", points = 3 },
                        new RubricItem { step = 3, description = "Điền đúng ký hiệu: 4 ∈ A; 9 ∉ A; 8 ∈ A", points = 3 }
                    }
                });
                list.Add(new QuestionData
                {
                    questionId = "ESSAY_SET_02",
                    topic = "Tập hợp số tự nhiên",
                    prompt = "Cho tập hợp B = {x ∈ ℕ* | x ≤ 6}.\n a) Hãy viết tập hợp B bằng cách liệt kê các phần tử.\n b) Tính số phần tử của tập hợp B và tính tổng các phần tử đó.",
                    maxScore = 10,
                    rubric = new List<RubricItem>
                    {
                        new RubricItem { step = 1, description = "Viết đúng B = {1; 2; 3; 4; 5; 6}", points = 4 },
                        new RubricItem { step = 2, description = "Xác định đúng số phần tử: Tập B có 6 phần tử", points = 3 },
                        new RubricItem { step = 3, description = "Tính tổng: 1 + 2 + 3 + 4 + 5 + 6 = 21", points = 3 }
                    }
                });
            }
            else if (norm.Contains("nguyen") || norm.Contains("int") || titleNorm.Contains("số nguyên") || titleNorm.Contains("âm") || titleNorm.Contains("dương"))
            {
                list.Add(new QuestionData
                {
                    questionId = "ESSAY_INT_01",
                    topic = "Phép tính với số nguyên",
                    prompt = "Thực hiện phép tính một cách hợp lý:\n A = (-25) + 48 + (-75) + 52",
                    maxScore = 10,
                    rubric = new List<RubricItem>
                    {
                        new RubricItem { step = 1, description = "Nhóm các số cùng dấu: [(-25) + (-75)] + (48 + 52)", points = 4 },
                        new RubricItem { step = 2, description = "Tính trong ngoặc: (-100) + 100", points = 3 },
                        new RubricItem { step = 3, description = "Kết quả chính xác: A = 0", points = 3 }
                    }
                });
                list.Add(new QuestionData
                {
                    questionId = "ESSAY_INT_02",
                    topic = "Quy tắc dấu ngoặc & Thứ tự phép tính số nguyên",
                    prompt = "Tìm số nguyên x biết:\n 2x - (-14) = -6",
                    maxScore = 10,
                    rubric = new List<RubricItem>
                    {
                        new RubricItem { step = 1, description = "Bỏ ngoặc: 2x + 14 = -6", points = 4 },
                        new RubricItem { step = 2, description = "Chuyển vế: 2x = -6 - 14 = -20", points = 3 },
                        new RubricItem { step = 3, description = "Tìm x: x = -20 : 2 = -10", points = 3 }
                    }
                });
            }
            else if (norm.Contains("phan") || norm.Contains("frac") || titleNorm.Contains("phân số"))
            {
                list.Add(new QuestionData
                {
                    questionId = "ESSAY_FRAC_01",
                    topic = "Phép cộng và trừ phân số",
                    prompt = "Thực hiện phép tính phân số:\n B = 3/4 + 1/6 - 5/12",
                    maxScore = 10,
                    rubric = new List<RubricItem>
                    {
                        new RubricItem { step = 1, description = "Quy đồng mẫu số chung là 12: 9/12 + 2/12 - 5/12", points = 4 },
                        new RubricItem { step = 2, description = "Cộng trừ tử số: (9 + 2 - 5)/12 = 6/12", points = 3 },
                        new RubricItem { step = 3, description = "Rút gọn về phân số tối giản: 1/2", points = 3 }
                    }
                });
                list.Add(new QuestionData
                {
                    questionId = "ESSAY_FRAC_02",
                    topic = "Tính chất phép nhân phân số",
                    prompt = "Tính giá trị biểu thức bằng cách hợp lý:\n C = (7/15) × (3/8) + (7/15) × (5/8)",
                    maxScore = 10,
                    rubric = new List<RubricItem>
                    {
                        new RubricItem { step = 1, description = "Đặt thừa số chung: (7/15) × (3/8 + 5/8)", points = 4 },
                        new RubricItem { step = 2, description = "Tính trong ngoặc: 3/8 + 5/8 = 8/8 = 1", points = 3 },
                        new RubricItem { step = 3, description = "Kết luận: C = (7/15) × 1 = 7/15", points = 3 }
                    }
                });
            }
            else if (norm.Contains("hinh") || norm.Contains("geom") || titleNorm.Contains("hình học"))
            {
                list.Add(new QuestionData
                {
                    questionId = "ESSAY_GEOM_01",
                    topic = "Hình học trực quan & Diện tích",
                    prompt = "Một mảnh vườn hình chữ nhật ABCD có chiều dài AB = 12 m và chiều rộng BC = 8 m.\n a) Tính chu vi và diện tích mảnh vườn.\n b) Người ta làm một lối đi xung quanh rộng 1 m. Tính diện tích phần đất trồng hoa còn lại.",
                    maxScore = 10,
                    rubric = new List<RubricItem>
                    {
                        new RubricItem { step = 1, description = "Chu vi = (12 + 8) × 2 = 40 m; Diện tích = 12 × 8 = 96 m²", points = 4 },
                        new RubricItem { step = 2, description = "Kích thước phần trồng hoa: Dài = 10 m, Rộng = 6 m", points = 3 },
                        new RubricItem { step = 3, description = "Diện tích trồng hoa: 10 × 6 = 60 m²", points = 3 }
                    }
                });
            }
            else
            {
                list.Add(new QuestionData
                {
                    questionId = "ESSAY_ORDER_01",
                    topic = "Thứ tự thực hiện phép tính",
                    prompt = "Thực hiện phép tính theo từng bước:\n A = 15 - (3 + 2) × 2",
                    maxScore = 10,
                    rubric = new List<RubricItem>
                    {
                        new RubricItem { step = 1, description = "Tính trong ngoặc: (3 + 2) = 5", points = 4 },
                        new RubricItem { step = 2, description = "Nhân trước: 5 × 2 = 10", points = 3 },
                        new RubricItem { step = 3, description = "Trừ sau: 15 - 10 = 5", points = 3 }
                    }
                });
                list.Add(new QuestionData
                {
                    questionId = "ESSAY_ORDER_02",
                    topic = "Phép tính với luỹ thừa",
                    prompt = "Thực hiện phép tính:\n B = 2³ × 5 - 3² × 4 + 2026⁰",
                    maxScore = 10,
                    rubric = new List<RubricItem>
                    {
                        new RubricItem { step = 1, description = "Tính các luỹ thừa: 2³ = 8, 3² = 9, 2026⁰ = 1", points = 4 },
                        new RubricItem { step = 2, description = "Nhân trước: 8 × 5 = 40, 9 × 4 = 36", points = 3 },
                        new RubricItem { step = 3, description = "Cộng trừ: 40 - 36 + 1 = 5", points = 3 }
                    }
                });
            }

            return list;
        }

        private void OnNextQuestionClicked()
        {
            if (availableEssayQuestions == null || availableEssayQuestions.Count == 0) return;
            PlayClick();
            currentEssayIndex++;
            if (currentEssayIndex >= availableEssayQuestions.Count) currentEssayIndex = 0;
            SetCurrentEssayQuestion(availableEssayQuestions[currentEssayIndex]);
            UpdateNavButtonsState();
        }

        private void OnPrevQuestionClicked()
        {
            if (availableEssayQuestions == null || availableEssayQuestions.Count == 0) return;
            PlayClick();
            currentEssayIndex--;
            if (currentEssayIndex < 0) currentEssayIndex = availableEssayQuestions.Count - 1;
            SetCurrentEssayQuestion(availableEssayQuestions[currentEssayIndex]);
            UpdateNavButtonsState();
        }

        private void UpdateNavButtonsState()
        {
            int total = availableEssayQuestions != null && availableEssayQuestions.Count > 0 ? availableEssayQuestions.Count : 1;
            if (questionProgressText != null)
            {
                questionProgressText.text = $"Câu {currentEssayIndex + 1} / {total}";
            }
            if (prevQuestionButton != null) prevQuestionButton.interactable = total > 1;
            if (nextQuestionButton != null) nextQuestionButton.interactable = total > 1;
        }

        private void PlayClick()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        }

        private void SetCurrentEssayQuestion(QuestionData q)
        {
            currentQuestion = q;
            if (currentQuestion.rubric == null || currentQuestion.rubric.Count == 0)
            {
                currentQuestion.rubric = new List<RubricItem>
                {
                    new RubricItem { step = 1, description = "Biến đổi / tính toán bước 1", points = 4 },
                    new RubricItem { step = 2, description = "Thực hiện phép tính bước 2", points = 3 },
                    new RubricItem { step = 3, description = "Kết luận đáp số chính xác", points = 3 }
                };
            }

            string lessonTitle = LessonListUIController.CurrentSelectedLesson != null ? LessonListUIController.CurrentSelectedLesson.title : "";
            if (problemTitleText != null)
            {
                problemTitleText.text = $"Bài tự luận: {(!string.IsNullOrEmpty(lessonTitle) ? lessonTitle : (!string.IsNullOrEmpty(q.topic) ? q.topic : "Toán 6"))}";
            }
            if (problemPromptText != null) problemPromptText.text = q.prompt;
            if (solutionInputField != null) solutionInputField.text = string.Empty;
        }

        public void InitDefaultSampleProblem()
        {
            // Đề bài tự luận mẫu chuẩn Toán 6
            var fallback = new QuestionData
            {
                questionId = "ESSAY_001",
                topic = "Thứ tự thực hiện phép tính",
                prompt = "Thực hiện phép tính theo từng bước:\n A = 15 - (3 + 2) × 2",
                maxScore = 10,
                rubric = new List<RubricItem>
                {
                    new RubricItem { step = 1, description = "Tính trong ngoặc: (3 + 2) = 5", points = 4 },
                    new RubricItem { step = 2, description = "Nhân trước: 5 × 2 = 10", points = 3 },
                    new RubricItem { step = 3, description = "Trừ sau: 15 - 10 = 5", points = 3 }
                }
            };
            SetCurrentEssayQuestion(fallback);
        }

        private Texture2D activePhotoTexture;

        private void OnCapturePhotoClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

            if (webCamTexture == null)
            {
                if (WebCamTexture.devices == null || WebCamTexture.devices.Length == 0)
                {
                    if (photoStatusText != null) 
                        photoStatusText.text = "<color=#f39c12>Không tìm thấy webcam phần cứng. Đã chuyển sang Vở bài tập mẫu Toán 6!</color>";
                    OnLoadSamplePhotoClicked();
                    return;
                }

                try
                {
                    webCamTexture = new WebCamTexture(640, 480);
                    s_ActiveCam = webCamTexture;
                    if (imagePreview != null)
                    {
                        imagePreview.texture = webCamTexture;
                        if (previewContainer != null) previewContainer.SetActive(true);
                    }
                    webCamTexture.Play();
                    hasPhoto = true;
                    if (photoStatusText != null) 
                        photoStatusText.text = "Camera đang bật. Căn chỉnh bài làm rồi bấm [Chụp ảnh] để lưu hoặc [Thoát/ESC] để tắt camera.";
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Không thể khởi động WebCam: " + ex.Message);
                    StopAndReleaseCamera();
                    if (photoStatusText != null) 
                        photoStatusText.text = "<color=#f39c12>Camera không phản hồi (đang bận/bị chặn). Đã chuyển sang Vở bài tập mẫu Toán 6!</color>";
                    OnLoadSamplePhotoClicked();
                }
            }
            else
            {
                try
                {
                    if (webCamTexture.isPlaying)
                    {
                        // Cố định ảnh chụp và giải phóng camera ngay lập tức
                        Texture2D photo = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
                        photo.SetPixels(webCamTexture.GetPixels());
                        photo.Apply();
                        activePhotoTexture = photo;

                        StopAndReleaseCamera();

                        if (imagePreview != null) imagePreview.texture = photo;
                        hasPhoto = true;
                        if (photoStatusText != null) 
                            photoStatusText.text = "<color=#2ecc71>Đã chụp ảnh bài làm thành công! Sẵn sàng gửi AI Thầy Minh chấm.</color>";
                    }
                    else
                    {
                        StopAndReleaseCamera();
                        OnLoadSamplePhotoClicked();
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Lỗi chụp ảnh từ webcam: " + ex.Message);
                    StopAndReleaseCamera();
                    OnLoadSamplePhotoClicked();
                }
            }
        }

        public void StopAndReleaseCamera()
        {
            if (webCamTexture != null)
            {
                try
                {
                    if (webCamTexture.isPlaying)
                    {
                        webCamTexture.Stop();
                    }
                    Destroy(webCamTexture);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Không thể giải phóng WebCamTexture: " + ex.Message);
                }
                finally
                {
                    webCamTexture = null;
                }
            }

            if (s_ActiveCam != null)
            {
                try
                {
                    if (s_ActiveCam.isPlaying) s_ActiveCam.Stop();
                    Destroy(s_ActiveCam);
                }
                catch {}
                s_ActiveCam = null;
            }
        }

        private void OnDisable()
        {
            StopAndReleaseCamera();
        }

        private void OnDestroy()
        {
            StopAndReleaseCamera();
        }

        private void OnApplicationQuit()
        {
            StopAndReleaseCamera();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterEditorCameraCleaner()
        {
            UnityEditor.EditorApplication.playModeStateChanged += state =>
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode || 
                    state == UnityEditor.PlayModeStateChange.EnteredEditMode)
                {
                    if (s_ActiveCam != null)
                    {
                        try
                        {
                            if (s_ActiveCam.isPlaying) s_ActiveCam.Stop();
                            DestroyImmediate(s_ActiveCam);
                        }
                        catch {}
                        s_ActiveCam = null;
                    }
                }
            };
        }
#endif

        private void OnPickImageClicked()
        {
            StopAndReleaseCamera();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

            string selectedPath = null;

#if UNITY_EDITOR
            selectedPath = UnityEditor.EditorUtility.OpenFilePanel("Chọn ảnh bài làm Toán 6 (PNG/JPG)", "", "png,jpg,jpeg,bmp");
#elif UNITY_STANDALONE_WIN
            selectedPath = OpenWindowsFilePicker("Chọn ảnh bài làm Toán 6");
#endif

            if (!string.IsNullOrEmpty(selectedPath))
            {
                LoadImageFromFile(selectedPath);
            }
            else
            {
                if (!hasPhoto && photoStatusText != null)
                {
                    photoStatusText.text = "<color=#f39c12>Bạn chưa chọn ảnh từ máy. Bấm [Tải ảnh vở mẫu] để thử nghiệm ngay!</color>";
                }
            }
        }

        public void OnLoadSamplePhotoClicked()
        {
            StopAndReleaseCamera();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

            string samplePath = System.IO.Path.Combine(Application.dataPath, "_Project/Textures/sample_math_essay.png");
            if (System.IO.File.Exists(samplePath))
            {
                LoadImageFromFile(samplePath);
                return;
            }

            Texture2D sampleTex = CreateSampleNotebookTexture();
            activePhotoTexture = sampleTex;
            if (imagePreview != null) imagePreview.texture = sampleTex;
            if (previewContainer != null) previewContainer.SetActive(true);
            hasPhoto = true;

            if (photoStatusText != null)
            {
                photoStatusText.text = "<color=#2ecc71>[OK] Đã tải trang vở bài tập mẫu Toán 6 (Kẻ ô ly)! Sẵn sàng gửi chấm.</color>";
            }
        }

        private void LoadImageFromFile(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                {
                    byte[] fileBytes = System.IO.File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(fileBytes))
                    {
                        activePhotoTexture = tex;
                        if (imagePreview != null) imagePreview.texture = tex;
                        if (previewContainer != null) previewContainer.SetActive(true);
                        hasPhoto = true;

                        if (photoStatusText != null)
                        {
                            string fileName = System.IO.Path.GetFileName(path);
                            photoStatusText.text = $"<color=#2ecc71>[OK] Đã tải ảnh: {fileName} ({tex.width}x{tex.height})!</color>";
                        }
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Không thể tải ảnh: " + ex.Message);
            }

            OnLoadSamplePhotoClicked();
        }

        private Texture2D CreateSampleNotebookTexture()
        {
            int w = 600;
            int h = 400;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color paperBg = new Color(0.98f, 0.98f, 0.96f, 1f); // Giấy tập học sinh
            Color gridLine = new Color(0.85f, 0.90f, 0.96f, 1f); // Dòng kẻ ô ly xanh nhạt
            Color marginLine = new Color(0.92f, 0.35f, 0.35f, 1f); // Dòng kẻ lề đỏ
            Color inkColor = new Color(0.10f, 0.20f, 0.60f, 1f); // Mực bút máy xanh

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = paperBg;
                    // Kẻ ô ly (mỗi 25px)
                    if (x % 25 == 0 || y % 25 == 0) c = gridLine;
                    // Lề vở màu đỏ
                    if (x >= 68 && x <= 71) c = marginLine;
                    pixels[y * w + x] = c;
                }
            }

            // Vẽ các dòng kẻ mô phỏng chữ viết tay học sinh Toán 6
            DrawHorizontalLine(pixels, w, h, 90, 340, 240, 3, inkColor); // Dòng tiêu đề
            DrawHorizontalLine(pixels, w, h, 90, 280, 280, 3, inkColor); // Bước 1: A = 15 - (3 + 2) x 2
            DrawHorizontalLine(pixels, w, h, 90, 220, 200, 3, inkColor); // Bước 2: = 15 - 5 x 2
            DrawHorizontalLine(pixels, w, h, 90, 160, 170, 3, inkColor); // Bước 3: = 15 - 10
            DrawHorizontalLine(pixels, w, h, 90, 100, 110, 4, inkColor); // Bước 4: = 5
            DrawHorizontalLine(pixels, w, h, 90, 50, 180, 3, new Color(0.8f, 0.15f, 0.15f, 1f)); // Đáp số

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static void DrawHorizontalLine(Color[] pixels, int texW, int texH, int startX, int startY, int length, int thickness, Color col)
        {
            for (int y = startY; y < startY + thickness && y < texH; y++)
            {
                for (int x = startX; x < startX + length && x < texW; x++)
                {
                    pixels[y * texW + x] = col;
                }
            }
        }

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private class OpenFileName
        {
            public int structSize = 0;
            public System.IntPtr dlgOwner = System.IntPtr.Zero;
            public System.IntPtr instance = System.IntPtr.Zero;
            public string filter = null;
            public string customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public string file = null;
            public int maxFile = 0;
            public string fileTitle = null;
            public int maxFileTitle = 0;
            public string initialDir = null;
            public string title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt = null;
            public System.IntPtr custData = System.IntPtr.Zero;
            public System.IntPtr hook = System.IntPtr.Zero;
            public string templateName = null;
            public System.IntPtr reservedPtr = System.IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        [System.Runtime.InteropServices.DllImport("comdlg32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool GetOpenFileName([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] OpenFileName ofn);

        private static string OpenWindowsFilePicker(string title)
        {
            OpenFileName ofn = new OpenFileName();
            ofn.structSize = System.Runtime.InteropServices.Marshal.SizeOf(ofn);
            ofn.filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)\0*.png;*.jpg;*.jpeg;*.bmp\0All Files (*.*)\0*.*\0";
            ofn.file = new string(new char[512]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new string(new char[128]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.initialDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);
            ofn.title = title;
            ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008; // OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR

            if (GetOpenFileName(ofn))
            {
                return ofn.file;
            }
            return null;
        }
#endif

        private void OnSubmitClicked()
        {
            StopAndReleaseCamera();

            string studentSteps = solutionInputField != null ? solutionInputField.text.Trim() : "";

            if (!hasPhoto && string.IsNullOrEmpty(studentSteps))
            {
                if (photoStatusText != null)
                {
                    photoStatusText.text = "<color=red>Vui lòng chụp ảnh bài làm, chọn ảnh từ máy hoặc nhập lời giải!</color>";
                }
                return;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (submitButton != null) submitButton.interactable = false;

            if (photoStatusText != null) photoStatusText.text = "AI Thầy Minh đang quan sát ảnh & chấm bài theo Rubric...";

            string base64Image = null;
            if (activePhotoTexture != null)
            {
                try
                {
                    byte[] jpgBytes = activePhotoTexture.EncodeToJPG(75);
                    if (jpgBytes != null && jpgBytes.Length > 0)
                    {
                        base64Image = System.Convert.ToBase64String(jpgBytes);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("EncodeToJPG failed: " + ex.Message);
                }
            }

            string submissionText = !string.IsNullOrEmpty(studentSteps) 
                ? studentSteps 
                : (activePhotoTexture != null ? "(Học sinh không nhập văn bản, nộp qua hình ảnh bài giải)" : "(Không có bài làm)");

            var payload = new EssaySubmissionPayload
            {
                questionId = currentQuestion.questionId,
                prompt = currentQuestion.prompt,
                studentSteps = submissionText,
                imageBase64 = base64Image,
                rubric = currentQuestion.rubric
            };

            // Gọi AI Chấm điểm (Gemini Vision / Text)
            aiTutorService.GradeEssay(payload, (success, response, message) =>
            {
                if (submitButton != null) submitButton.interactable = true;

                if (success && response != null)
                {
                    string details = "";
                    if (response.stepAnalysis != null)
                    {
                        foreach (var item in response.stepAnalysis)
                        {
                            string icon = item.isCorrect ? "[Đúng]" : "[Chú ý]";
                            details += $"{icon} Bước {item.step}: {item.comment}\n";
                        }
                    }

                    // Reset và lưu vào ScoreSessionContext dùng chung
                    ScoreSessionContext.Reset();
                    ScoreSessionContext.ActivityType = "ESSAY";
                    ScoreSessionContext.Score = response.score;
                    ScoreSessionContext.MaxScore = response.maxScore;
                    ScoreSessionContext.EssayFeedback = $"{response.pedagogicalFeedback}\n\n{details}";
                    ScoreSessionContext.IsVersusMode = false;
                    ScoreSessionContext.TimeSpentSeconds = 90;

                    // Nếu bài làm chưa đạt (dưới 8 điểm hoặc không pass), lưu vào Sổ tay câu sai (Anki)
                    if (!response.passed || response.score < 8)
                    {
                        WrongQuestionBankManager.RecordWrongEssay(
                            currentQuestion?.prompt,
                            submissionText,
                            currentQuestion?.correctAnswer,
                            $"{response.pedagogicalFeedback} | {details}",
                            currentQuestion?.topic ?? "ALL",
                            currentQuestion?.difficulty ?? "MEDIUM"
                        );
                    }

                    string essayLesId = !string.IsNullOrEmpty(currentQuestion?.lessonId) ? currentQuestion.lessonId : PlayerPrefs.GetString("Current_Lesson_ID", "LES_ESSAY");
                    int calculatedScore = Mathf.RoundToInt((float)response.score / Mathf.Max(1, response.maxScore) * 10f);
                    if (!string.IsNullOrEmpty(essayLesId))
                    {
                        int currentHigh = PlayerPrefs.GetInt($"Lesson_HighestScore_{essayLesId}", 0);
                        if (calculatedScore > currentHigh)
                        {
                            PlayerPrefs.SetInt($"Lesson_HighestScore_{essayLesId}", calculatedScore);
                        }
                        int att = PlayerPrefs.GetInt($"Lesson_Attempts_{essayLesId}", 0);
                        PlayerPrefs.SetInt($"Lesson_Attempts_{essayLesId}", att + 1);
                        PlayerPrefs.SetInt($"Lesson_LastScore_{essayLesId}", calculatedScore);
                        PlayerPrefs.SetString($"Lesson_LastDate_{essayLesId}", System.DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                        PlayerPrefs.Save();
                    }

                    // 1. Gửi điểm & thành tích bài tự luận lên Google Sheets
                    if (networkService != null)
                    {
                        var submission = new ResultSubmission
                        {
                            userId = PlayerPrefs.GetString("User_ID", "STU_001"),
                            lessonId = essayLesId,
                            activityType = "ESSAY",
                            score = response.score,
                            maxScore = response.maxScore,
                            timeSpentSeconds = 90,
                            mistakesCount = response.passed ? 0 : 1,
                            timestamp = System.DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        networkService.SaveResult(submission, null);

                        // 2. Gửi nhật ký nhận xét AI Thầy Minh lên tab AI_Tutor_Logs trên Google Sheet
                        var tutorLog = new AITutorLogSubmission
                        {
                            userId = PlayerPrefs.GetString("User_ID", "STU_001"),
                            lessonId = submission.lessonId,
                            questionId = currentQuestion?.questionId ?? "ESSAY_Q",
                            sessionType = "ESSAY_GRADING",
                            studentPrompt = currentQuestion?.prompt ?? "",
                            studentAnswer = submissionText,
                            aiFeedback = $"{response.pedagogicalFeedback} | {details}",
                            detectedMistake = response.detectedMistakeType ?? "NONE",
                            score = response.score,
                            maxScore = response.maxScore,
                            timestamp = System.DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        networkService.LogAITutor(tutorLog, null);
                    }

                    // 3. Chuyển sang Scene 07_Score
                    SceneManager.LoadScene("07_Score");
                }
                else
                {
                    if (photoStatusText != null) photoStatusText.text = "<color=red>Lỗi khi chấm bài: " + message + "</color>";
                }
            });
        }
    }
}