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

        [Header("Question Illustration (Đề Bài)")]
        [SerializeField] private GameObject questionImageContainer;
        [SerializeField] private RawImage questionIllustrationImage;
        [SerializeField] private Button questionImageZoomButton;
        [SerializeField] private Button viewQuestionImageInWorkspaceButton;

        [Header("Photo Submission Mode")]
        [SerializeField] private Button capturePhotoButton;    // Button 1: Chụp ảnh camera
        [SerializeField] private Button pickImageButton;        // Button 2: Chọn ảnh từ máy tính
        [SerializeField] private Button loadSamplePhotoButton;  // Button 3: Tải ảnh vở bài tập mẫu (Test nhanh)
        [SerializeField] private RawImage imagePreview;
        [SerializeField] private GameObject previewContainer;
        [SerializeField] private TextMeshProUGUI photoStatusText;

        [Header("Input & Keypad (Tùy chọn)")]
        [SerializeField] private TMP_InputField solutionInputField;
        private TextMeshProUGUI lineTrackerText;
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
        private bool isSamplePhoto = false;
        public bool IsSamplePhoto => isSamplePhoto;

        private GameObject questionZoomModal;
        private RawImage questionZoomModalImage;
        private AspectRatioFitter questionZoomFitter;
        private Texture2D activeQuestionTexture;
        private Coroutine questionImageDownloadRoutine;
        private static readonly Dictionary<string, Texture2D> s_QuestionImageCache = new Dictionary<string, Texture2D>();

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
                if (questionZoomModal != null && questionZoomModal.activeSelf)
                {
                    OnCloseQuestionImageZoomModal();
                    return;
                }
                StopAndReleaseCamera();
                SceneManager.LoadScene(SessionManager.Instance != null ? SessionManager.Instance.ReturnSceneName : "02_VillageWorld");
            }

            if (lineTrackerText != null && solutionInputField != null)
            {
                UpdateLineTracker();
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

            SetPhotoPreview(null);

            EnsureNavigationButtons();
            SetupDualWorkspaceLayout();
            EnsureQuestionIllustrationUI();

            string selectedTopic = PlayerPrefs.GetString("Current_Lesson_Topic", "ALL");
            string selectedLessonId = PlayerPrefs.GetString("Current_Lesson_ID", "");
            LoadEssayQuestions(selectedTopic, selectedLessonId);
        }

        
        private void SetPhotoPreview(Texture tex)
        {
            if (imagePreview != null)
            {
                imagePreview.texture = tex;
                imagePreview.gameObject.SetActive(tex != null);
            }
            if (previewContainer != null)
            {
                previewContainer.SetActive(true);
                var ph = previewContainer.transform.Find("PlaceholderText");
                if (ph != null) ph.gameObject.SetActive(tex == null);
            }
            if (tex == null)
            {
                HideSampleNotebookOverlay();
            }
        }

        private void ShowSampleNotebookOverlay(string prompt, string questionId)
        {
            if (previewContainer == null) return;

            var overlayTr = previewContainer.transform.Find("SampleNotebookOverlay");
            GameObject overlayGo;
            if (overlayTr == null)
            {
                overlayGo = new GameObject("SampleNotebookOverlay", typeof(RectTransform), typeof(TextMeshProUGUI));
                overlayGo.transform.SetParent(previewContainer.transform, false);
                var rt = overlayGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(85, 20); // Chừa lề đỏ vở kẻ ô ly
                rt.offsetMax = new Vector2(-20, -20);

                var tmp = overlayGo.GetComponent<TextMeshProUGUI>();
                tmp.fontSize = 14;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(0.10f, 0.22f, 0.65f, 1f); // Mực bút máy xanh
                tmp.alignment = TextAlignmentOptions.TopLeft;
            }
            else
            {
                overlayGo = overlayTr.gameObject;
            }

            overlayGo.SetActive(true);
            var textComp = overlayGo.GetComponent<TextMeshProUGUI>();
            string solution = CurriculumEssayKnowledgeBase.GetSampleSolution(prompt, questionId);
            if (string.IsNullOrEmpty(solution))
            {
                solution = "= 15 - 5 × 2\n= 15 - 10\n= 5";
            }
            textComp.text = $"<color=#c0392b><b>VỞ BÀI TẬP TOÁN 6 - BÀI LÀM MẪU</b></color>\n" +
                           $"------------------------------------------------------\n" +
                           $"{solution}";
        }

        private void HideSampleNotebookOverlay()
        {
            if (previewContainer == null) return;
            var overlayTr = previewContainer.transform.Find("SampleNotebookOverlay");
            if (overlayTr != null) overlayTr.gameObject.SetActive(false);
        }

        
        
        private void SetupDualWorkspaceLayout()
        {
            var localCanvas = problemTitleText != null ? problemTitleText.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
            if (localCanvas == null) return;
            Transform sceneCanvasTr = localCanvas.transform;

            // Chỉnh lại vị trí ProblemCard để không đè lên Nav Bar
            if (problemPromptText != null && problemPromptText.transform.parent != null && problemPromptText.transform.parent != sceneCanvasTr)
            {
                var cardRt = problemPromptText.transform.parent.GetComponent<RectTransform>();
                if (cardRt != null)
                {
                    cardRt.anchorMin = new Vector2(0.5f, 1f);
                    cardRt.anchorMax = new Vector2(0.5f, 1f);
                    cardRt.pivot = new Vector2(0.5f, 1f);
                    cardRt.anchoredPosition = new Vector2(0, -125);
                    cardRt.sizeDelta = new Vector2(1460, 110);
                }
            }

            // 1. ĐẢM BẢO KHUNG BÊN TRÁI: LeftPhotoCard (Ảnh bài làm)
            Transform leftCardTr = sceneCanvasTr.Find("LeftPhotoCard");
            if (leftCardTr == null)
            {
                var leftGo = new GameObject("LeftPhotoCard", typeof(RectTransform), typeof(Image));
                leftGo.transform.SetParent(sceneCanvasTr, false);
                leftCardTr = leftGo.transform;
                var leftRt = leftGo.GetComponent<RectTransform>();
                leftRt.anchorMin = new Vector2(0.5f, 1f);
                leftRt.anchorMax = new Vector2(0.5f, 1f);
                leftRt.pivot = new Vector2(0.5f, 1f);
                leftRt.anchoredPosition = new Vector2(-375, -275);
                leftRt.sizeDelta = new Vector2(710, 480);
                var leftImg = leftGo.GetComponent<Image>();
                leftImg.color = new Color(0.10f, 0.15f, 0.24f, 0.90f);

                // Badge tiêu đề cột trái
                var badgeGo = new GameObject("PhotoBadge", typeof(RectTransform), typeof(Image));
                badgeGo.transform.SetParent(leftCardTr, false);
                var badgeRt = badgeGo.GetComponent<RectTransform>();
                badgeRt.anchoredPosition = new Vector2(0, 215);
                badgeRt.sizeDelta = new Vector2(340, 26);
                var badgeImg = badgeGo.GetComponent<Image>();
                badgeImg.color = new Color(0.18f, 0.40f, 0.85f, 0.95f);
                var badgeTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                badgeTxt.transform.SetParent(badgeGo.transform, false);
                var btRt = badgeTxt.GetComponent<RectTransform>();
                btRt.anchorMin = Vector2.zero; btRt.anchorMax = Vector2.one; btRt.offsetMin = Vector2.zero; btRt.offsetMax = Vector2.zero;
                var btTmp = badgeTxt.GetComponent<TextMeshProUGUI>();
                btTmp.text = "ẢNH BÀI LÀM (CHỤP HOẶC TẢI LÊN)";
                btTmp.fontSize = 12; btTmp.fontStyle = FontStyles.Bold; btTmp.alignment = TextAlignmentOptions.Center; btTmp.color = Color.white;
            }

            // Di chuyển và định vị lại 3 nút ảnh vào LeftPhotoCard
            if (capturePhotoButton != null)
            {
                capturePhotoButton.transform.SetParent(leftCardTr, false);
                var rt = capturePhotoButton.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(-220, 175);
                rt.sizeDelta = new Vector2(200, 38);
                var txt = capturePhotoButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "1. Chụp Ảnh";
            }
            if (pickImageButton != null)
            {
                pickImageButton.transform.SetParent(leftCardTr, false);
                var rt = pickImageButton.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 175);
                rt.sizeDelta = new Vector2(200, 38);
                var txt = pickImageButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "2. Chọn Tệp Ảnh";
            }
            if (loadSamplePhotoButton != null)
            {
                loadSamplePhotoButton.transform.SetParent(leftCardTr, false);
                var rt = loadSamplePhotoButton.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(220, 175);
                rt.sizeDelta = new Vector2(200, 38);
                var txt = loadSamplePhotoButton.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "3. Vở Mẫu";
            }

            // Di chuyển và định vị previewContainer vào LeftPhotoCard
            if (previewContainer != null)
            {
                previewContainer.transform.SetParent(leftCardTr, false);
                var rt = previewContainer.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, -38);
                rt.sizeDelta = new Vector2(675, 365);
            }

            // 2. ĐẢM BẢO KHUNG BÊN PHẢI: RightTextCard (Ô Nhập Liệu & Bàn Phím Toán)
            Transform rightCardTr = sceneCanvasTr.Find("RightTextCard");
            if (rightCardTr == null)
            {
                var rightGo = new GameObject("RightTextCard", typeof(RectTransform), typeof(Image));
                rightGo.transform.SetParent(sceneCanvasTr, false);
                rightCardTr = rightGo.transform;
                var rightRt = rightGo.GetComponent<RectTransform>();
                rightRt.anchorMin = new Vector2(0.5f, 1f);
                rightRt.anchorMax = new Vector2(0.5f, 1f);
                rightRt.pivot = new Vector2(0.5f, 1f);
                rightRt.anchoredPosition = new Vector2(375, -275);
                rightRt.sizeDelta = new Vector2(710, 480);
                var rightImg = rightGo.GetComponent<Image>();
                rightImg.color = new Color(0.10f, 0.15f, 0.24f, 0.90f);

                // Badge tiêu đề cột phải
                var badgeGo = new GameObject("TextBadge", typeof(RectTransform), typeof(Image));
                badgeGo.transform.SetParent(rightCardTr, false);
                var badgeRt = badgeGo.GetComponent<RectTransform>();
                badgeRt.anchoredPosition = new Vector2(0, 215);
                badgeRt.sizeDelta = new Vector2(430, 26);
                var badgeImg = badgeGo.GetComponent<Image>();
                badgeImg.color = new Color(0.85f, 0.55f, 0.10f, 0.95f);
                var badgeTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                badgeTxt.transform.SetParent(badgeGo.transform, false);
                var btRt = badgeTxt.GetComponent<RectTransform>();
                btRt.anchorMin = Vector2.zero; btRt.anchorMax = Vector2.one; btRt.offsetMin = Vector2.zero; btRt.offsetMax = Vector2.zero;
                var btTmp = badgeTxt.GetComponent<TextMeshProUGUI>();
                btTmp.text = "LỜI GIẢI TỰ LUẬN (OCR TỰ ĐIỀN HOẶC GÕ TAY)";
                btTmp.fontSize = 12; btTmp.fontStyle = FontStyles.Bold; btTmp.alignment = TextAlignmentOptions.Center; btTmp.color = Color.white;
            }

            // Đảm bảo solutionInputField tồn tại trong RightTextCard
            if (solutionInputField == null)
            {
                solutionInputField = CreateSolutionInputField(rightCardTr.gameObject);
            }
            else
            {
                solutionInputField.transform.SetParent(rightCardTr, false);
                var inputRt = solutionInputField.GetComponent<RectTransform>();
                inputRt.anchorMin = new Vector2(0.5f, 0.5f); inputRt.anchorMax = new Vector2(0.5f, 0.5f); inputRt.pivot = new Vector2(0.5f, 0.5f);
                inputRt.anchoredPosition = new Vector2(0, 65);
                inputRt.sizeDelta = new Vector2(660, 220);
                solutionInputField.interactable = true;
                solutionInputField.readOnly = false;
                solutionInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
                solutionInputField.customCaretColor = true;
                solutionInputField.caretColor = new Color(0.2f, 0.95f, 1f, 1f);
                solutionInputField.caretWidth = 3;
                solutionInputField.caretBlinkRate = 0.85f;
                solutionInputField.selectionColor = new Color(0.18f, 0.50f, 0.95f, 0.45f);
            }

            EnsureLineTracker(rightCardTr);
            EnsureMathToolbar();

            // Định vị lại PhotoStatusText và SubmitButton ở đáy màn hình
            if (photoStatusText != null)
            {
                var rt = photoStatusText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0, 75);
                rt.sizeDelta = new Vector2(1200, 30);
            }
            if (submitButton != null)
            {
                var rt = submitButton.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0, 20);
                rt.sizeDelta = new Vector2(580, 50);
            }
        }

        private TMP_InputField CreateSolutionInputField(GameObject parent)
        {
            var inputGo = new GameObject("SolutionInputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(parent.transform, false);
            var inRt = inputGo.GetComponent<RectTransform>();
            inRt.anchorMin = new Vector2(0.5f, 0.5f); inRt.anchorMax = new Vector2(0.5f, 0.5f); inRt.pivot = new Vector2(0.5f, 0.5f);
            inRt.anchoredPosition = new Vector2(0, 50);
            inRt.sizeDelta = new Vector2(660, 240);

            var img = inputGo.GetComponent<Image>();
            img.color = new Color(0.06f, 0.09f, 0.14f, 0.95f);

            var inputField = inputGo.GetComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            inputField.customCaretColor = true;
            inputField.caretColor = new Color(0.2f, 0.95f, 1f, 1f);
            inputField.caretWidth = 3;
            inputField.caretBlinkRate = 0.85f;
            inputField.selectionColor = new Color(0.18f, 0.50f, 0.95f, 0.45f);

            // Text Area
            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputGo.transform, false);
            var taRt = textArea.GetComponent<RectTransform>();
            taRt.anchorMin = Vector2.zero; taRt.anchorMax = Vector2.one;
            taRt.offsetMin = new Vector2(14, 10); taRt.offsetMax = new Vector2(-14, -10);

            // Placeholder
            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phGo.transform.SetParent(textArea.transform, false);
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
            phRt.offsetMin = Vector2.zero; phRt.offsetMax = Vector2.zero;
            var phText = phGo.GetComponent<TextMeshProUGUI>();
            phText.text = "AI Thầy Minh sẽ tự động chuyển các dòng viết tay từ ảnh vào đây, hoặc em có thể trực tiếp gõ/sửa lời giải tại đây...\n\nVí dụ:\nBước 1: A = 15 - 5 x 2\nBước 2: = 15 - 10\nBước 3: = 5";
            phText.fontSize = 15;
            phText.fontStyle = FontStyles.Italic;
            phText.color = new Color(0.6f, 0.65f, 0.75f, 0.55f);
            phText.alignment = TextAlignmentOptions.TopLeft;

            // Text Component
            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(textArea.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var mainText = txtGo.GetComponent<TextMeshProUGUI>();
            mainText.fontSize = 17;
            mainText.color = Color.white;
            mainText.alignment = TextAlignmentOptions.TopLeft;

            inputField.textViewport = taRt;
            inputField.textComponent = mainText;
            inputField.placeholder = phText;
            inputField.targetGraphic = img;

            return inputField;
        }

        private void EnsureMathToolbar()
        {
            if (solutionInputField == null) return;
            Transform parent = solutionInputField.transform.parent;
            if (parent == null) return;

            // Chỉnh kích thước ô nhập liệu để chừa chỗ cho thanh bàn phím toán học bên dưới
            var inputRt = solutionInputField.GetComponent<RectTransform>();
            if (inputRt != null)
            {
                inputRt.anchoredPosition = new Vector2(0, 65);
                inputRt.sizeDelta = new Vector2(660, 220);
            }

            // Đảm bảo MathKeypadController đã được khởi tạo
            if (mathKeypad == null)
            {
                mathKeypad = gameObject.GetComponent<MathKeypadController>();
                if (mathKeypad == null)
                {
                    mathKeypad = gameObject.AddComponent<MathKeypadController>();
                }
            }
            mathKeypad.SetTargetInputField(solutionInputField);

            // Nếu thanh Toolbar đã tồn tại thì không tạo lại
            var existingToolbar = parent.Find("MathToolbar");
            if (existingToolbar != null) return;

            // Tạo khung Toolbar Toán Học
            var toolbarGo = new GameObject("MathToolbar", typeof(RectTransform), typeof(Image));
            toolbarGo.transform.SetParent(parent, false);
            var tbRt = toolbarGo.GetComponent<RectTransform>();
            tbRt.anchoredPosition = new Vector2(0, -150);
            tbRt.sizeDelta = new Vector2(660, 136);

            var tbImg = toolbarGo.GetComponent<Image>();
            tbImg.color = new Color(0.08f, 0.12f, 0.18f, 0.90f);

            // Dòng 1: Phân số, Dấu ngoặc, Luỹ thừa, Căn bậc hai (Y = 36)
            CreateToolbarButton(toolbarGo.transform, "Btn_Frac", "a/b", new Vector2(-280, 36), new Vector2(72, 32), () => mathKeypad.InsertFraction(), new Color(0.18f, 0.35f, 0.55f));
            CreateToolbarButton(toolbarGo.transform, "Btn_Paren", "( )", new Vector2(-200, 36), new Vector2(72, 32), () => mathKeypad.InsertParentheses());
            CreateToolbarButton(toolbarGo.transform, "Btn_Bracket", "[ ]", new Vector2(-120, 36), new Vector2(72, 32), () => mathKeypad.InsertBrackets());
            CreateToolbarButton(toolbarGo.transform, "Btn_Brace", "{ }", new Vector2(-40, 36), new Vector2(72, 32), () => mathKeypad.InsertBraces());
            CreateToolbarButton(toolbarGo.transform, "Btn_Sq", "x²", new Vector2(40, 36), new Vector2(72, 32), () => mathKeypad.InsertSymbol("²"));
            CreateToolbarButton(toolbarGo.transform, "Btn_Cube", "x³", new Vector2(120, 36), new Vector2(72, 32), () => mathKeypad.InsertSymbol("³"));
            CreateToolbarButton(toolbarGo.transform, "Btn_Pow", "x^n", new Vector2(200, 36), new Vector2(72, 32), () => mathKeypad.InsertSymbol("^"));
            CreateToolbarButton(toolbarGo.transform, "Btn_Sqrt", "can(x)", new Vector2(280, 36), new Vector2(72, 32), () => mathKeypad.InsertSymbol("can(x)"));

            // Dòng 2: Phép tính & Ký hiệu Toán 6 (Y = 0)
            CreateToolbarButton(toolbarGo.transform, "Btn_Plus", "+", new Vector2(-284, 0), new Vector2(62, 30), () => mathKeypad.InsertSymbol(" + "));
            CreateToolbarButton(toolbarGo.transform, "Btn_Minus", "-", new Vector2(-213, 0), new Vector2(62, 30), () => mathKeypad.InsertSymbol(" - "));
            CreateToolbarButton(toolbarGo.transform, "Btn_Mul", "×", new Vector2(-142, 0), new Vector2(62, 30), () => mathKeypad.InsertSymbol(" × "));
            CreateToolbarButton(toolbarGo.transform, "Btn_Div", ":", new Vector2(-71, 0), new Vector2(62, 30), () => mathKeypad.InsertSymbol(" : "));
            CreateToolbarButton(toolbarGo.transform, "Btn_Eq", "=", new Vector2(0, 0), new Vector2(62, 30), () => mathKeypad.InsertSymbol(" = "));
            CreateToolbarButton(toolbarGo.transform, "Btn_In", "thuoc", new Vector2(71, 0), new Vector2(62, 30), () => mathKeypad.InsertSymbol(" ∈ "));
            CreateToolbarButton(toolbarGo.transform, "Btn_NotIn", "k.thuoc", new Vector2(142, 0), new Vector2(62, 30), () => mathKeypad.InsertSymbol(" ∉ "));
            CreateToolbarButton(toolbarGo.transform, "Btn_Percent", "%", new Vector2(213, 0), new Vector2(62, 30), () => mathKeypad.InsertSymbol("%"));
            CreateToolbarButton(toolbarGo.transform, "Btn_Deg", "°C", new Vector2(284, 0), new Vector2(62, 30), () => mathKeypad.InsertSymbol("°C"));

            // Dòng 3: Xuống dòng thêm dấu =, Xóa ký tự, Xóa toàn bộ (Y = -36)
            CreateToolbarButton(toolbarGo.transform, "Btn_NextLine", "[Enter] Dòng mới = ", new Vector2(-160, -36), new Vector2(280, 32), () => mathKeypad.InsertEquationLine(), new Color(0.12f, 0.45f, 0.32f));
            CreateToolbarButton(toolbarGo.transform, "Btn_Backspace", "[<] Xóa ký tự", new Vector2(70, -36), new Vector2(150, 32), () => mathKeypad.Backspace(), new Color(0.45f, 0.25f, 0.18f));
            CreateToolbarButton(toolbarGo.transform, "Btn_Clear", "[X] Xóa hết", new Vector2(235, -36), new Vector2(140, 32), () => mathKeypad.ClearAll(), new Color(0.48f, 0.16f, 0.16f));
        }

        private GameObject CreateToolbarButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Action onClick, Color? bgColor = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = bgColor ?? new Color(0.16f, 0.22f, 0.32f);

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                PlayClick();
                onClick?.Invoke();
            });

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 15;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go;
        }

        private Texture2D DownscaleTextureForOCR(Texture2D source, int maxDimension = 1024)
        {
            if (source == null) return null;
            int width = source.width;
            int height = source.height;

            if (width > maxDimension || height > maxDimension)
            {
                if (width > height)
                {
                    height = Mathf.Max(32, Mathf.RoundToInt((float)height / width * maxDimension));
                    width = maxDimension;
                }
                else
                {
                    width = Mathf.Max(32, Mathf.RoundToInt((float)width / height * maxDimension));
                    height = maxDimension;
                }
            }

            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);

            Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();

            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        private void RunOCRAndFillText(Texture2D tex)
        {
            if (tex == null || aiTutorService == null) return;

            if (photoStatusText != null)
            {
                photoStatusText.text = "[AI] <color=#3498db>AI Thầy Minh đang đọc chữ viết tay / phép tính từ ảnh vào ô văn bản...</color>";
            }

            byte[] jpgBytes = null;
            Texture2D readableTex = null;
            try
            {
                readableTex = DownscaleTextureForOCR(tex, 1024);
                if (readableTex != null)
                {
                    jpgBytes = readableTex.EncodeToJPG(75);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("EncodeToJPG OCR error: " + ex.Message);
            }
            finally
            {
                if (readableTex != null && readableTex != tex)
                {
                    Destroy(readableTex);
                }
            }

            if (jpgBytes == null || jpgBytes.Length == 0) return;
            string base64 = System.Convert.ToBase64String(jpgBytes);

            aiTutorService.ExtractTextFromImage(base64, (success, recognizedText, msg) =>
            {
                if (this == null || gameObject == null) return;

                if (!success || string.IsNullOrEmpty(recognizedText) || recognizedText.Contains("[KHÔNG_CÓ_BÀI_LÀM_TOÁN]"))
                {
                    // Nếu là ảnh phong cảnh, con vật, hoặc không tìm thấy bài làm toán:
                    if (solutionInputField != null) solutionInputField.text = "";
                    if (photoStatusText != null)
                    {
                        string reason = !string.IsNullOrEmpty(msg) ? msg : "Ảnh phong cảnh / con vật hoặc không rõ nét.";
                        photoStatusText.text = $"<color=#e74c3c>[!] Không nhận diện được bài làm toán từ ảnh ({reason})! Vui lòng chụp rõ trang vở hoặc gõ bài làm vào ô bên phải.</color>";
                    }
                    return;
                }

                // Điền đúng nội dung OCR nhận diện được từ ảnh
                if (solutionInputField != null)
                {
                    solutionInputField.text = recognizedText.Trim();
                    solutionInputField.stringPosition = solutionInputField.text.Length;
                    solutionInputField.caretPosition = solutionInputField.text.Length;
                }

                if (photoStatusText != null)
                {
                    photoStatusText.text = "<color=#2ecc71>[OK] AI đã trích xuất các dòng phép tính từ ảnh vào ô văn bản! Em hãy kiểm tra/sửa lại (nếu cần) rồi bấm 'GỬI BÀI GIẢI'.</color>";
                }
            });
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
                    rt.anchoredPosition = new Vector2(0, -82);
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
                    tmpP.text = "< Câu Trước";
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
                    tmpN.text = "Câu Tiếp >";
                    tmpN.fontSize = 16;
                    tmpN.alignment = TextAlignmentOptions.Center;
                    tmpN.color = Color.white;

                    // Nút Cấu hình Gemini Key
                    var keyGo = new GameObject("Btn_ConfigGeminiKey", typeof(RectTransform), typeof(Image), typeof(Button));
                    keyGo.transform.SetParent(navContainer.transform, false);
                    var rtKey = keyGo.GetComponent<RectTransform>();
                    rtKey.anchoredPosition = new Vector2(340, 0);
                    rtKey.sizeDelta = new Vector2(170, 36);
                    keyGo.GetComponent<Image>().color = new Color(0.16f, 0.48f, 0.72f);
                    var keyBtn = keyGo.GetComponent<Button>();
                    keyBtn.onClick.AddListener(ShowGeminiKeyModal);
                    var txtK = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtK.transform.SetParent(keyGo.transform, false);
                    var tmpK = txtK.GetComponent<TextMeshProUGUI>();
                    tmpK.text = "[Key] Gemini AI Key";
                    tmpK.fontSize = 15;
                    tmpK.fontStyle = FontStyles.Bold;
                    tmpK.alignment = TextAlignmentOptions.Center;
                    tmpK.color = Color.white;
                }
            }
        }

        public void LoadEssayQuestions(string topic, string lessonId)
        {
            if (problemPromptText != null) problemPromptText.text = "Đang tải đề bài tự luận từ ngân hàng câu hỏi...";
            string diffFilter = PlayerPrefs.GetString("Quiz_Difficulty_Filter", "ALL");
            string currentLessonTitle = LessonListUIController.CurrentSelectedLesson != null ? LessonListUIController.CurrentSelectedLesson.title : "";
            string currentLessonTopic = LessonListUIController.CurrentSelectedLesson != null ? LessonListUIController.CurrentSelectedLesson.topic : topic;

            string retryQId = PlayerPrefs.GetString("Retry_Essay_Question_ID", "");
            string retryPrompt = PlayerPrefs.GetString("Retry_Essay_Question_Prompt", "");
            bool isRetrying = !string.IsNullOrEmpty(retryQId) || !string.IsNullOrEmpty(retryPrompt);

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

                    if (TeacherConfigManager.Instance != null)
                    {
                        var customEssays = TeacherConfigManager.Instance.GetCustomQuestionDataList(6, queryTopic, "ESSAY");
                        if (customEssays != null && customEssays.Count > 0)
                        {
                            availableEssayQuestions.InsertRange(0, customEssays);
                        }
                    }

                    // Nếu Google Sheet chưa có câu tự luận, nạp bộ câu hỏi tự luận dự phòng
                    if (availableEssayQuestions.Count == 0)
                    {
                        var topicEssayList = GetTopicFallbackEssayQuestions(currentLessonTopic, currentLessonTitle, diffFilter);
                        availableEssayQuestions.AddRange(topicEssayList);
                    }

                    if (!isSpecificLesson && !isSpecificTopic && !isRetrying)
                    {
                        // Trộn ngẫu nhiên câu hỏi tự luận từ toàn bộ kho khi KHÔNG phải đang giải lại
                        var rnd = new System.Random();
                        for (int i = availableEssayQuestions.Count - 1; i > 0; i--)
                        {
                            int j = rnd.Next(i + 1);
                            var t = availableEssayQuestions[i]; availableEssayQuestions[i] = availableEssayQuestions[j]; availableEssayQuestions[j] = t;
                        }
                    }

                    if (availableEssayQuestions.Count > 0)
                    {
                        int targetIdx = 0;
                        if (isRetrying)
                        {
                            PlayerPrefs.DeleteKey("Retry_Essay_Question_ID");
                            PlayerPrefs.DeleteKey("Retry_Essay_Question_Prompt");
                            PlayerPrefs.Save();
                            int found = availableEssayQuestions.FindIndex(x => 
                                (!string.IsNullOrEmpty(retryQId) && x.questionId == retryQId) ||
                                (!string.IsNullOrEmpty(retryPrompt) && (x.prompt == retryPrompt || x.prompt.Trim() == retryPrompt.Trim()))
                            );
                            if (found >= 0) targetIdx = found;
                            else Debug.LogWarning($"[EssayUI] Không tìm thấy câu retry (QId='{retryQId}', Prompt='{retryPrompt}') trong kho câu hỏi.");
                        }

                        else if (currentQuestion != null)
                        {
                            int currentFound = availableEssayQuestions.FindIndex(x =>
                                (!string.IsNullOrEmpty(currentQuestion.questionId) && x.questionId == currentQuestion.questionId) ||
                                (!string.IsNullOrEmpty(currentQuestion.prompt) && (x.prompt == currentQuestion.prompt || x.prompt.Trim() == currentQuestion.prompt.Trim()))
                            );
                            if (currentFound >= 0) targetIdx = currentFound;
                        }

                        currentEssayIndex = targetIdx;
                        SetCurrentEssayQuestion(availableEssayQuestions[currentEssayIndex]);
                        if (isRetrying) ApplyRetryStateIfPresent();
                    }
                    else
                    {
                        InitDefaultSampleProblem();
                        if (isRetrying) ApplyRetryStateIfPresent();
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
                    int targetIdx = 0;
                    if (isRetrying)
                    {
                        PlayerPrefs.DeleteKey("Retry_Essay_Question_ID");
                        PlayerPrefs.DeleteKey("Retry_Essay_Question_Prompt");
                        PlayerPrefs.Save();
                        int found = availableEssayQuestions.FindIndex(x => 
                            (!string.IsNullOrEmpty(retryQId) && x.questionId == retryQId) ||
                            (!string.IsNullOrEmpty(retryPrompt) && (x.prompt == retryPrompt || x.prompt.Trim() == retryPrompt.Trim()))
                        );
                        if (found >= 0) targetIdx = found;
                    }
                    currentEssayIndex = targetIdx;
                    SetCurrentEssayQuestion(availableEssayQuestions[currentEssayIndex]);
                    if (isRetrying) ApplyRetryStateIfPresent();
                }
                else
                {
                    InitDefaultSampleProblem();
                    if (isRetrying) ApplyRetryStateIfPresent();
                }
                UpdateNavButtonsState();
            }
        }

        private void EnsureLineTracker(Transform parent)
        {
            if (parent == null) return;
            Transform existing = parent.Find("LineTrackerBanner");
            GameObject trackerGo;
            if (existing == null)
            {
                trackerGo = new GameObject("LineTrackerBanner", typeof(RectTransform), typeof(Image));
                trackerGo.transform.SetParent(parent, false);
                var rt = trackerGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, -66);
                rt.sizeDelta = new Vector2(660, 26);

                var img = trackerGo.GetComponent<Image>();
                img.color = new Color(0.04f, 0.10f, 0.18f, 0.95f);

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(trackerGo.transform, false);
                var txtRt = txtGo.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = new Vector2(12, 0);
                txtRt.offsetMax = new Vector2(-12, 0);

                lineTrackerText = txtGo.GetComponent<TextMeshProUGUI>();
                lineTrackerText.fontSize = 12;
                lineTrackerText.fontStyle = FontStyles.Bold;
                lineTrackerText.alignment = TextAlignmentOptions.MidlineLeft;
                lineTrackerText.color = Color.white;
            }
            else
            {
                trackerGo = existing.gameObject;
                lineTrackerText = trackerGo.GetComponentInChildren<TextMeshProUGUI>();
            }

            UpdateLineTracker();
        }

        private void UpdateLineTracker()
        {
            if (lineTrackerText == null || solutionInputField == null) return;

            string text = solutionInputField.text ?? "";
            int pos = solutionInputField.stringPosition;
            pos = Mathf.Clamp(pos, 0, text.Length);

            int currentLine = 1;
            int lineStart = 0;
            for (int i = 0; i < pos; i++)
            {
                if (text[i] == '\n')
                {
                    currentLine++;
                    lineStart = i + 1;
                }
            }
            int col = pos - lineStart + 1;

            int totalLines = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') totalLines++;
            }

            int lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd < 0) lineEnd = text.Length;
            string snippet = text.Substring(lineStart, lineEnd - lineStart).Trim();
            if (string.IsNullOrEmpty(snippet)) snippet = "(Dòng trống - đang nhập)";
            else if (snippet.Length > 28) snippet = snippet.Substring(0, 25) + "...";

            lineTrackerText.text = $"<color=#38bdf8>[VỊ TRÍ]</color> Đang nhập <b><color=#facc15>Dòng {currentLine}/{totalLines}</color></b>: <color=#e2e8f0><i>\"{snippet}\"</i></color> <size=11><color=#94a3b8>(Cột {col})</color></size>";
        }

        public void FocusAndBlinkCaret()
        {
            if (solutionInputField == null) return;
            solutionInputField.interactable = true;
            solutionInputField.customCaretColor = true;
            solutionInputField.caretColor = new Color(0.2f, 0.95f, 1f, 1f);
            solutionInputField.caretWidth = 3;
            solutionInputField.caretBlinkRate = 0.85f;
            solutionInputField.selectionColor = new Color(0.18f, 0.50f, 0.95f, 0.45f);

            int currentLen = (solutionInputField.text ?? "").Length;
            solutionInputField.stringPosition = currentLen;
            solutionInputField.caretPosition = currentLen;

            solutionInputField.ActivateInputField();
            solutionInputField.Select();
            UpdateLineTracker();
        }

        private void ApplyRetryStateIfPresent()
        {
            string retryCorrectLines = PlayerPrefs.GetString("Retry_Essay_Correct_Lines", "");
            string retryHint = PlayerPrefs.GetString("Retry_Essay_Wrong_Hint", "");
            int firstWrongIdx = PlayerPrefs.GetInt("Retry_Essay_First_Wrong_Index", -1);

            // Xóa key sau khi nạp để tránh lưu cache sang câu hỏi khác
            PlayerPrefs.DeleteKey("Retry_Essay_Correct_Lines");
            PlayerPrefs.DeleteKey("Retry_Essay_Wrong_Hint");
            PlayerPrefs.DeleteKey("Retry_Essay_First_Wrong_Index");
            PlayerPrefs.Save();

            if (string.IsNullOrEmpty(retryCorrectLines))
            {
                // SAI HẾT hoặc sai ngay từ bước 1: XÓA SẠCH ĐỂ HỌC SINH GIẢI LẠI TỪ ĐẦU!
                if (solutionInputField != null)
                {
                    solutionInputField.text = string.Empty;
                    solutionInputField.stringPosition = 0;
                    solutionInputField.caretPosition = 0;
                }
                if (photoStatusText != null)
                {
                    photoStatusText.text = "<color=#e74c3c>[!] Bài giải chưa đúng từ bước đầu. Thầy Minh đã làm mới ô nhập liệu để em giải lại từ đầu nhé!</color>";
                }
            }
            else
            {
                // Đúng 1 số bước: Giữ nguyên các bước của chính học sinh đã làm đúng, giải tiếp từ bước bị sai:
                string prefill = retryCorrectLines.Trim();
                if (!prefill.EndsWith("\n") && !prefill.EndsWith("="))
                {
                    prefill += "\n= ";
                }
                else if (!prefill.EndsWith(" "))
                {
                    prefill += " ";
                }

                if (solutionInputField != null)
                {
                    solutionInputField.text = prefill;
                    solutionInputField.stringPosition = prefill.Length;
                    solutionInputField.caretPosition = prefill.Length;
                }
                if (photoStatusText != null)
                {
                    int stepNum = (firstWrongIdx >= 0) ? (firstWrongIdx + 1) : 2;
                    photoStatusText.text = $"<color=#f1c40f>[!] [GIẢI TIẾP TỪ BƯỚC {stepNum}] Thầy Minh đã giữ lại các bước em làm đúng. Hãy tính tiếp từ dấu '=' nhé!</color>";
                }
            }

            FocusAndBlinkCaret();
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
                    imageUrl = "Assets/_Project/Textures/set_math6_illustration.png",
                    topic = "Tập hợp & Phần tử của tập hợp",
                    prompt = "Cho tập hợp A các số tự nhiên lớn hơn 3 và không vượt quá 8:\n a) Viết tập hợp A bằng 2 cách (Liệt kê phần tử & Chỉ ra tính chất đặc trưng).\n b) Điền ký hiệu thích hợp (∈ hoặc ∉) vào chỗ trống: 4 ... A ; 9 ... A ; 8 ... A.",
                    maxScore = 10,
                    rubric = new List<RubricItem>
                    {
                        new RubricItem { step = 1, description = "Liệt kê phần tử: A = {4; 5; 6; 7; 8}", points = 4 },
                        new RubricItem { step = 2, description = "Tính chất đặc trưng: A = {x thuoc N | 3 < x <= 8}", points = 3 },
                        new RubricItem { step = 3, description = "Điền đúng ký hiệu: 4 thuoc A; 9 khong thuoc A; 8 thuoc A", points = 3 }
                    }
                });
                list.Add(new QuestionData
                {
                    questionId = "ESSAY_SET_02",
                    topic = "Tập hợp số tự nhiên",
                    prompt = "Cho tập hợp B = {x thuoc N* | x ≤ 6}.\n a) Hãy viết tập hợp B bằng cách liệt kê các phần tử.\n b) Tính số phần tử của tập hợp B và tính tổng các phần tử đó.",
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
                    imageUrl = "https://images.unsplash.com/photo-1596495578065-6e0763fa1178?auto=format&fit=crop&w=800&q=80",
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
                    prompt = "Thực hiện phép tính:\n B = 2³ × 5 - 3² × 4 + 2026^0",
                    maxScore = 10,
                    rubric = new List<RubricItem>
                    {
                        new RubricItem { step = 1, description = "Tính các luỹ thừa: 2³ = 8, 3² = 9, 2026^0 = 1", points = 4 },
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
            CurriculumEssayKnowledgeBase.EnrichQuestionData(currentQuestion);

            ScoreSessionContext.CurrentQuestionPrompt = q != null ? q.prompt : "";
            ScoreSessionContext.CurrentQuestionId = q != null ? q.questionId : "";
            ScoreSessionContext.CurrentQuestionTopic = q != null ? q.topic : "";

            string lessonTitle = LessonListUIController.CurrentSelectedLesson != null ? LessonListUIController.CurrentSelectedLesson.title : "";
            if (problemTitleText != null)
            {
                problemTitleText.text = $"Bài tự luận: {(!string.IsNullOrEmpty(lessonTitle) ? lessonTitle : (!string.IsNullOrEmpty(q.topic) ? q.topic : "Toán 6"))}";
            }
            if (problemPromptText != null) problemPromptText.text = MathTextSanitizer.SanitizeForDisplay(q.prompt);
            if (solutionInputField != null) solutionInputField.text = string.Empty;
            SetPhotoPreview(null);
            hasPhoto = false;
            activePhotoTexture = null;

            // Nạp hình ảnh minh họa đề bài nếu có
            string img = q != null ? q.EffectiveImageUrl : "";
            LoadQuestionIllustration(img);
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

        #region Question Illustration Image Management
        /// <summary>
        /// Khởi tạo và liên kết các thành phần UI hiển thị hình ảnh đề bài (Thumbnail, Modal phóng to, Nút xem khung trái).
        /// Tự động sinh nếu chưa được kéo thả trong Inspector.
        /// </summary>
        public void EnsureQuestionIllustrationUI()
        {
            var localCanvas = problemTitleText != null ? problemTitleText.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
            if (localCanvas == null) return;
            Transform canvasTr = localCanvas.transform;

            Transform problemCardTr = null;
            if (problemPromptText != null && problemPromptText.transform.parent != null && problemPromptText.transform.parent != canvasTr)
            {
                problemCardTr = problemPromptText.transform.parent;
            }

            Transform targetParent = problemCardTr != null ? problemCardTr : canvasTr;

            // 1. Tim hoac tao QuestionImageContainer gan truc tiep vao ProblemCard de khong bao gio bi de chu
            Transform existingContainerTr = targetParent.Find("QuestionImageContainer");
            if (existingContainerTr == null && targetParent != canvasTr)
            {
                existingContainerTr = canvasTr.Find("QuestionImageContainer");
            }

            if (existingContainerTr != null)
            {
                questionImageContainer = existingContainerTr.gameObject;
                if (targetParent != null && questionImageContainer.transform.parent != targetParent)
                {
                    questionImageContainer.transform.SetParent(targetParent, false);
                }
            }
            else
            {
                var cGo = new GameObject("QuestionImageContainer", typeof(RectTransform), typeof(Image));
                cGo.transform.SetParent(targetParent, false);
                questionImageContainer = cGo;
                var cImg = cGo.GetComponent<Image>();
                cImg.color = new Color(0.06f, 0.10f, 0.18f, 0.95f);

                var outline = cGo.AddComponent<Outline>();
                outline.effectColor = new Color(0.25f, 0.55f, 0.95f, 0.75f);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            var cRt = questionImageContainer.GetComponent<RectTransform>();
            if (cRt != null)
            {
                cRt.anchorMin = new Vector2(1f, 0.5f);
                cRt.anchorMax = new Vector2(1f, 0.5f);
                cRt.pivot = new Vector2(1f, 0.5f);
                cRt.anchoredPosition = new Vector2(-20, 0);
                cRt.sizeDelta = new Vector2(340, 155);
            }
            // An container mac dinh cho den khi thuc su co anh duoc nap thanh cong
            questionImageContainer.SetActive(false);

            // Loading Text
            Transform plTr = questionImageContainer.transform.Find("LoadingText");
            if (plTr == null)
            {
                var plGo = new GameObject("LoadingText", typeof(RectTransform), typeof(TextMeshProUGUI));
                plGo.transform.SetParent(questionImageContainer.transform, false);
                plTr = plGo.transform;
            }
            var plRt = plTr.GetComponent<RectTransform>();
            plRt.anchorMin = Vector2.zero; plRt.anchorMax = Vector2.one;
            plRt.offsetMin = new Vector2(10, 32); plRt.offsetMax = new Vector2(-10, -10);
            var plTmp = plTr.GetComponent<TextMeshProUGUI>();
            if (plTmp != null)
            {
                plTmp.text = "[Dang nap hinh anh...]";
                plTmp.fontSize = 13;
                plTmp.fontStyle = FontStyles.Italic;
                plTmp.alignment = TextAlignmentOptions.Center;
                plTmp.color = new Color(0.6f, 0.75f, 0.95f, 0.8f);
            }

            // Question Illustration Image
            Transform imgTr = questionImageContainer.transform.Find("QuestionIllustrationImage");
            if (imgTr == null)
            {
                var imgGo = new GameObject("QuestionIllustrationImage", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
                imgGo.transform.SetParent(questionImageContainer.transform, false);
                imgTr = imgGo.transform;
            }
            var iRt = imgTr.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0f, 0f);
            iRt.anchorMax = new Vector2(1f, 1f);
            iRt.offsetMin = new Vector2(6, 32);
            iRt.offsetMax = new Vector2(-6, -6);
            questionIllustrationImage = imgTr.GetComponent<RawImage>();
            if (questionIllustrationImage == null) questionIllustrationImage = imgTr.gameObject.AddComponent<RawImage>();
            questionIllustrationImage.color = Color.white;
            var fitter = imgTr.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = imgTr.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1.5f;

            // ZoomBadge (Nut phong to anh)
            Transform badgeTr = questionImageContainer.transform.Find("ZoomBadge");
            if (badgeTr == null)
            {
                var badgeGo = new GameObject("ZoomBadge", typeof(RectTransform), typeof(Image), typeof(Button));
                badgeGo.transform.SetParent(questionImageContainer.transform, false);
                badgeTr = badgeGo.transform;
            }
            var bRt = badgeTr.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f);
            bRt.anchorMax = new Vector2(1f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(0, 3);
            bRt.sizeDelta = new Vector2(0, 24);
            var bImg = badgeTr.GetComponent<Image>();
            if (bImg != null) bImg.color = new Color(0.08f, 0.18f, 0.35f, 0.95f);

            Transform bTxtTr = badgeTr.Find("Text");
            if (bTxtTr == null)
            {
                var badgeTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                badgeTxt.transform.SetParent(badgeTr, false);
                bTxtTr = badgeTxt.transform;
            }
            var btRt = bTxtTr.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero; btRt.anchorMax = Vector2.one; btRt.offsetMin = Vector2.zero; btRt.offsetMax = Vector2.zero;
            var bTmp = bTxtTr.GetComponent<TextMeshProUGUI>();
            if (bTmp != null)
            {
                bTmp.text = "[Zoom] Phong to anh";
                bTmp.fontSize = 12;
                bTmp.fontStyle = FontStyles.Bold;
                bTmp.alignment = TextAlignmentOptions.Center;
                bTmp.color = new Color(0.35f, 0.85f, 1f);
            }

            // Gan click cho tat ca cac thanh phan trong QuestionImageContainer
            if (questionImageContainer != null)
            {
                var cBtn = questionImageContainer.GetComponent<Button>();
                if (cBtn == null) cBtn = questionImageContainer.AddComponent<Button>();
                questionImageZoomButton = cBtn;

                var allBtns = questionImageContainer.GetComponentsInChildren<Button>(true);
                foreach (var b in allBtns)
                {
                    b.onClick.RemoveListener(OnOpenQuestionImageZoomModal);
                    b.onClick.AddListener(OnOpenQuestionImageZoomModal);
                }
            }
            // 2. TÃ¬m hoáº·c táº¡o Modal phÃ³ng to áº£nh trÃªn Canvas (Cinema Scale 1720x950)
            Transform existingModalTr = canvasTr.Find("QuestionImageZoomModal");
            if (existingModalTr != null)
            {
                questionZoomModal = existingModalTr.gameObject;
                var dTr = questionZoomModal.transform.Find("DialogCard");
                if (dTr != null)
                {
                    var dRt = dTr.GetComponent<RectTransform>();
                    if (dRt != null) dRt.sizeDelta = new Vector2(1720, 950);
                    var zTr = dTr.Find("ZoomImage");
                    if (zTr != null)
                    {
                        var ziRt = zTr.GetComponent<RectTransform>();
                        if (ziRt != null) ziRt.sizeDelta = new Vector2(1640, 820);
                        questionZoomModalImage = zTr.GetComponent<RawImage>();
                        questionZoomFitter = zTr.GetComponent<AspectRatioFitter>();
                    }
                }
            }
            else
            {
                var modalGo = new GameObject("QuestionImageZoomModal", typeof(RectTransform), typeof(Image));
                modalGo.transform.SetParent(canvasTr, false);
                questionZoomModal = modalGo;
                var mRt = modalGo.GetComponent<RectTransform>();
                mRt.anchorMin = Vector2.zero; mRt.anchorMax = Vector2.one;
                mRt.offsetMin = Vector2.zero; mRt.offsetMax = Vector2.zero;
                var mImg = modalGo.GetComponent<Image>();
                mImg.color = new Color(0.01f, 0.02f, 0.05f, 0.94f);

                var dialogGo = new GameObject("DialogCard", typeof(RectTransform), typeof(Image));
                dialogGo.transform.SetParent(modalGo.transform, false);
                var dRt = dialogGo.GetComponent<RectTransform>();
                dRt.anchorMin = new Vector2(0.5f, 0.5f); dRt.anchorMax = new Vector2(0.5f, 0.5f);
                dRt.pivot = new Vector2(0.5f, 0.5f);
                dRt.anchoredPosition = Vector2.zero;
                dRt.sizeDelta = new Vector2(1720, 950);
                var dImg = dialogGo.GetComponent<Image>();
                dImg.color = new Color(0.08f, 0.12f, 0.20f, 0.98f);
                var dOutline = dialogGo.AddComponent<Outline>();
                dOutline.effectColor = new Color(0.25f, 0.65f, 1f, 0.75f);
                dOutline.effectDistance = new Vector2(2.5f, -2.5f);

                var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleGo.transform.SetParent(dialogGo.transform, false);
                var tRt = titleGo.GetComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0.5f, 1f); tRt.anchorMax = new Vector2(0.5f, 1f);
                tRt.pivot = new Vector2(0.5f, 1f);
                tRt.anchoredPosition = new Vector2(0, -18);
                tRt.sizeDelta = new Vector2(1000, 36);
                var tTmp = titleGo.GetComponent<TextMeshProUGUI>();
                tTmp.text = "HÌNH VẼ MINH HỌA ĐỀ BÀI (CHẾ ĐỘ XEM CHI TIẾT)";
                tTmp.fontSize = 22;
                tTmp.fontStyle = FontStyles.Bold;
                tTmp.alignment = TextAlignmentOptions.Center;
                tTmp.color = new Color(1f, 0.85f, 0.35f);

                var zoomImgGo = new GameObject("ZoomImage", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
                zoomImgGo.transform.SetParent(dialogGo.transform, false);
                var ziRt = zoomImgGo.GetComponent<RectTransform>();
                ziRt.anchorMin = new Vector2(0.5f, 0.5f); ziRt.anchorMax = new Vector2(0.5f, 0.5f);
                ziRt.pivot = new Vector2(0.5f, 0.5f);
                ziRt.anchoredPosition = new Vector2(0, -15);
                ziRt.sizeDelta = new Vector2(1640, 820);
                questionZoomModalImage = zoomImgGo.GetComponent<RawImage>();
                questionZoomFitter = zoomImgGo.GetComponent<AspectRatioFitter>();
                questionZoomFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                questionZoomFitter.aspectRatio = 1.6f;

                var closeBtnGo = new GameObject("Btn_CloseZoom", typeof(RectTransform), typeof(Image), typeof(Button));
                closeBtnGo.transform.SetParent(dialogGo.transform, false);
                var cbRt = closeBtnGo.GetComponent<RectTransform>();
                cbRt.anchorMin = new Vector2(1f, 1f); cbRt.anchorMax = new Vector2(1f, 1f);
                cbRt.pivot = new Vector2(1f, 1f);
                cbRt.anchoredPosition = new Vector2(-20, -16);
                cbRt.sizeDelta = new Vector2(125, 42);
                var cbImg = closeBtnGo.GetComponent<Image>();
                cbImg.color = new Color(0.88f, 0.22f, 0.22f);

                var cbTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                cbTxt.transform.SetParent(closeBtnGo.transform, false);
                var cbtRt = cbTxt.GetComponent<RectTransform>();
                cbtRt.anchorMin = Vector2.zero; cbtRt.anchorMax = Vector2.one; cbtRt.offsetMin = Vector2.zero; cbtRt.offsetMax = Vector2.zero;
                var cbtTmp = cbTxt.GetComponent<TextMeshProUGUI>();
                cbtTmp.text = "[X] Đóng (ESC)";
                cbtTmp.fontSize = 15;
                cbtTmp.fontStyle = FontStyles.Bold;
                cbtTmp.alignment = TextAlignmentOptions.Center;
                cbtTmp.color = Color.white;
            }

            // Gáº¯n sá»± kiá»‡n Ä‘Ã³ng cho modal (Backdrop & nÃºt [X] ÄÃ³ng)
            if (questionZoomModal != null)
            {
                var backdropBtn = questionZoomModal.GetComponent<Button>();
                if (backdropBtn == null) backdropBtn = questionZoomModal.AddComponent<Button>();
                backdropBtn.onClick.RemoveListener(OnCloseQuestionImageZoomModal);
                backdropBtn.onClick.AddListener(OnCloseQuestionImageZoomModal);

                Transform closeBtnTr = questionZoomModal.transform.Find("DialogCard/Btn_CloseZoom");
                if (closeBtnTr != null)
                {
                    var cbBtn = closeBtnTr.GetComponent<Button>();
                    if (cbBtn != null)
                    {
                        cbBtn.onClick.RemoveListener(OnCloseQuestionImageZoomModal);
                        cbBtn.onClick.AddListener(OnCloseQuestionImageZoomModal);
                    }
                }

                questionZoomModal.SetActive(false);
            }

            // 3. TÃ¬m hoáº·c táº¡o nÃºt [4. ðŸ–¼ï¸ HÃ¬nh Äá» BÃ i] trong LeftPhotoCard
            Transform leftCardTr = canvasTr.Find("LeftPhotoCard");
            if (leftCardTr != null)
            {
                Transform btnQImgTr = leftCardTr.Find("Btn_ViewQuestionImage");
                if (btnQImgTr != null)
                {
                    viewQuestionImageInWorkspaceButton = btnQImgTr.GetComponent<Button>();
                }
                else
                {
                    var bGo = new GameObject("Btn_ViewQuestionImage", typeof(RectTransform), typeof(Image), typeof(Button));
                    bGo.transform.SetParent(leftCardTr, false);
                    var btnQImgRt = bGo.GetComponent<RectTransform>();
                    btnQImgRt.anchorMin = new Vector2(0.5f, 0.5f); btnQImgRt.anchorMax = new Vector2(0.5f, 0.5f); btnQImgRt.pivot = new Vector2(0.5f, 0.5f);
                    btnQImgRt.anchoredPosition = new Vector2(250, 175);
                    btnQImgRt.sizeDelta = new Vector2(150, 38);
                    var btnQImgComp = bGo.GetComponent<Image>();
                    btnQImgComp.color = new Color(0.20f, 0.45f, 0.85f);
                    viewQuestionImageInWorkspaceButton = bGo.GetComponent<Button>();

                    var bTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    bTxt.transform.SetParent(bGo.transform, false);
                    var btnQTxtRt = bTxt.GetComponent<RectTransform>();
                    btnQTxtRt.anchorMin = Vector2.zero; btnQTxtRt.anchorMax = Vector2.one; btnQTxtRt.offsetMin = Vector2.zero; btnQTxtRt.offsetMax = Vector2.zero;
                    var btnQTmp = bTxt.GetComponent<TextMeshProUGUI>();
                    btnQTmp.text = "4. Hình Đề Bài";
                    btnQTmp.fontSize = 14;
                    btnQTmp.fontStyle = FontStyles.Bold;
                    btnQTmp.alignment = TextAlignmentOptions.Center;
                    btnQTmp.color = Color.white;
                }

                if (viewQuestionImageInWorkspaceButton != null)
                {
                    viewQuestionImageInWorkspaceButton.onClick.RemoveListener(OnViewQuestionImageInWorkspaceClicked);
                    viewQuestionImageInWorkspaceButton.onClick.AddListener(OnViewQuestionImageInWorkspaceClicked);
                }
            }
        }
        private void RefreshLeftPhotoCardButtons(bool hasQuestionImage)
        {
            if (viewQuestionImageInWorkspaceButton != null)
            {
                viewQuestionImageInWorkspaceButton.gameObject.SetActive(hasQuestionImage);
            }

            if (hasQuestionImage)
            {
                if (capturePhotoButton != null)
                {
                    var rt = capturePhotoButton.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(-245, 175);
                    rt.sizeDelta = new Vector2(150, 38);
                }
                if (pickImageButton != null)
                {
                    var rt = pickImageButton.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(-80, 175);
                    rt.sizeDelta = new Vector2(150, 38);
                }
                if (loadSamplePhotoButton != null)
                {
                    var rt = loadSamplePhotoButton.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(85, 175);
                    rt.sizeDelta = new Vector2(150, 38);
                }
                if (viewQuestionImageInWorkspaceButton != null)
                {
                    var rt = viewQuestionImageInWorkspaceButton.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(250, 175);
                    rt.sizeDelta = new Vector2(150, 38);
                }
            }
            else
            {
                if (capturePhotoButton != null)
                {
                    var rt = capturePhotoButton.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(-220, 175);
                    rt.sizeDelta = new Vector2(200, 38);
                }
                if (pickImageButton != null)
                {
                    var rt = pickImageButton.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0, 175);
                    rt.sizeDelta = new Vector2(200, 38);
                }
                if (loadSamplePhotoButton != null)
                {
                    var rt = loadSamplePhotoButton.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(220, 175);
                    rt.sizeDelta = new Vector2(200, 38);
                }
            }
        }

        private void RefreshProblemCardLayout(bool hasQuestionImage)
        {
            var localCanvas = problemTitleText != null ? problemTitleText.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
            Transform sceneCanvasTr = localCanvas != null ? localCanvas.transform : null;

            RectTransform cardRt = null;
            if (problemPromptText != null && problemPromptText.transform.parent != null && problemPromptText.transform.parent != sceneCanvasTr)
            {
                cardRt = problemPromptText.transform.parent.GetComponent<RectTransform>();
            }

            Transform leftCardTr = sceneCanvasTr != null ? sceneCanvasTr.Find("LeftPhotoCard") : null;
            RectTransform leftRt = leftCardTr != null ? leftCardTr.GetComponent<RectTransform>() : null;
            Transform rightCardTr = sceneCanvasTr != null ? sceneCanvasTr.Find("RightTextCard") : null;
            RectTransform rightRt = rightCardTr != null ? rightCardTr.GetComponent<RectTransform>() : null;

            if (questionImageContainer != null)
            {
                questionImageContainer.SetActive(hasQuestionImage);
                if (cardRt != null && questionImageContainer.transform.parent != cardRt.transform)
                {
                    questionImageContainer.transform.SetParent(cardRt.transform, false);
                }
                var cRt = questionImageContainer.GetComponent<RectTransform>();
                if (cRt != null)
                {
                    cRt.anchorMin = new Vector2(1f, 0.5f);
                    cRt.anchorMax = new Vector2(1f, 0.5f);
                    cRt.pivot = new Vector2(1f, 0.5f);
                    cRt.anchoredPosition = new Vector2(-20, 0);
                    cRt.sizeDelta = new Vector2(340, 155);
                }
            }

            if (hasQuestionImage)
            {
                if (cardRt != null)
                {
                    cardRt.sizeDelta = new Vector2(1460, 185);
                    cardRt.anchoredPosition = new Vector2(0, -95);
                }
                if (problemPromptText != null)
                {
                    var rt = problemPromptText.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = new Vector2(25, -5);
                    rt.sizeDelta = new Vector2(1040, -25);
                    problemPromptText.alignment = TextAlignmentOptions.MidlineLeft;
                }
                if (leftRt != null) leftRt.anchoredPosition = new Vector2(-375, -285);
                if (rightRt != null) rightRt.anchoredPosition = new Vector2(375, -285);
            }
            else
            {
                if (cardRt != null)
                {
                    cardRt.sizeDelta = new Vector2(1460, 110);
                    cardRt.anchoredPosition = new Vector2(0, -125);
                }
                if (problemPromptText != null)
                {
                    var rt = problemPromptText.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0, -5);
                    rt.sizeDelta = new Vector2(1400, 70);
                    problemPromptText.alignment = TextAlignmentOptions.Center;
                }
                if (leftRt != null) leftRt.anchoredPosition = new Vector2(-375, -270);
                if (rightRt != null) rightRt.anchoredPosition = new Vector2(375, -270);
            }
        }
        public void LoadQuestionIllustration(string rawUrl)
        {
            if (questionImageDownloadRoutine != null)
            {
                StopCoroutine(questionImageDownloadRoutine);
                questionImageDownloadRoutine = null;
            }

            string url = (rawUrl ?? "").Trim();
            if (string.IsNullOrEmpty(url))
            {
                HideQuestionIllustration();
                return;
            }

            // Ngay khi bat dau nap anh, lap tuc dieu chinh layout de Text khong bao gio bi thumbnail de len chu!
            RefreshProblemCardLayout(true);
            if (questionImageContainer != null)
            {
                questionImageContainer.SetActive(true);
                var pl = questionImageContainer.transform.Find("LoadingText");
                if (pl != null)
                {
                    pl.gameObject.SetActive(true);
                    var txt = pl.GetComponent<TextMeshProUGUI>();
                    if (txt != null) txt.text = "[Đang nạp hình ảnh...]";
                }
            }
            if (questionIllustrationImage != null)
            {
                questionIllustrationImage.texture = null;
                questionIllustrationImage.color = new Color(1f, 1f, 1f, 0f); // Trong suot, khong hien hop trang!
            }

            questionImageDownloadRoutine = StartCoroutine(DownloadQuestionImageRoutine(url));
        }

        private IEnumerator DownloadQuestionImageRoutine(string rawUrl)
        {
            string url = (rawUrl ?? "").Trim();
            if (string.IsNullOrEmpty(url)) yield break;

            // 1. Kiem tra bo nho dem (Cache)
            if (s_QuestionImageCache.TryGetValue(rawUrl, out Texture2D cachedTex) && cachedTex != null)
            {
                ApplyQuestionTexture(cachedTex);
                yield break;
            }
            if (s_QuestionImageCache.TryGetValue(url, out Texture2D cachedUrlTex) && cachedUrlTex != null)
            {
                ApplyQuestionTexture(cachedUrlTex);
                yield break;
            }

            // 2. Kiem tra tep cuc bo hoac duong dan project
            string localCandidate = url.Replace("file://", "");
            if (localCandidate.StartsWith("Assets/"))
            {
                localCandidate = System.IO.Path.Combine(Application.dataPath, localCandidate.Substring(7));
            }
            if (System.IO.File.Exists(localCandidate))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(localCandidate);
                    Texture2D localTex = new Texture2D(2, 2);
                    if (localTex.LoadImage(bytes))
                    {
                        s_QuestionImageCache[rawUrl] = localTex;
                        ApplyQuestionTexture(localTex);
                        yield break;
                    }
                }
                catch {}
            }

            // 3. Chuan hoa Google Drive share link & Dropbox
            if (url.Contains("drive.google.com"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, @"/d/([a-zA-Z0-9_-]+)");
                if (match.Success)
                {
                    string fileId = match.Groups[1].Value;
                    url = $"https://drive.google.com/uc?export=view&id={fileId}";
                }
                else
                {
                    var idMatch = System.Text.RegularExpressions.Regex.Match(url, @"id=([a-zA-Z0-9_-]+)");
                    if (idMatch.Success)
                    {
                        string fileId = idMatch.Groups[1].Value;
                        url = $"https://drive.google.com/uc?export=view&id={fileId}";
                    }
                }
            }
            else if (url.Contains("dropbox.com"))
            {
                url = url.Replace("dl=0", "raw=1").Replace("dl=1", "raw=1");
            }

            // 4. Chuoi Base64
            if (url.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) || (url.Length > 150 && !url.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !url.Contains("/") && !url.Contains(".")))
            {
                string base64Str = url;
                if (url.Contains(",")) base64Str = url.Substring(url.IndexOf(",") + 1);
                try
                {
                    byte[] bytes = Convert.FromBase64String(base64Str);
                    Texture2D b64Tex = new Texture2D(2, 2);
                    if (b64Tex.LoadImage(bytes))
                    {
                        s_QuestionImageCache[rawUrl] = b64Tex;
                        ApplyQuestionTexture(b64Tex);
                        yield break;
                    }
                }
                catch {}
            }

            // 5. Tai qua mang voi UnityWebRequestTexture (Direct texture decoder)
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                using (UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
                {
                    req.timeout = 5;
                    req.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko)");
                    yield return req.SendWebRequest();

                    Texture2D downloadedTex = null;
                    if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        downloadedTex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(req);
                    }
                    else
                    {
                        Debug.LogWarning($"[EssayUIController] Không thể tải ảnh trực tiếp từ URL ({url}): {req.error}");
                    }

                    // Neu tai online that bai, dung anh minh hoa cuc bo du phong
                    if (downloadedTex == null)
                    {
                        downloadedTex = LoadLocalFallbackTexture();
                    }

                    if (downloadedTex != null)
                    {
                        s_QuestionImageCache[rawUrl] = downloadedTex;
                        s_QuestionImageCache[url] = downloadedTex;
                        ApplyQuestionTexture(downloadedTex);
                    }
                    else
                    {
                        HideQuestionIllustration();
                    }
                }
            }
            else
            {
                Texture2D localFallback = LoadLocalFallbackTexture();
                if (localFallback != null)
                {
                    ApplyQuestionTexture(localFallback);
                }
                else
                {
                    HideQuestionIllustration();
                }
            }
        }

        private Texture2D LoadLocalFallbackTexture()
        {
            string[] candidatePaths = new string[]
            {
                System.IO.Path.Combine(Application.dataPath, "_Project/Textures/set_math6_illustration.png"),
                System.IO.Path.Combine(Application.dataPath, "_Project/Textures/sample_math_essay.png")
            };

            foreach (var p in candidatePaths)
            {
                if (System.IO.File.Exists(p))
                {
                    try
                    {
                        byte[] bytes = System.IO.File.ReadAllBytes(p);
                        Texture2D tex = new Texture2D(2, 2);
                        if (tex.LoadImage(bytes))
                        {
                            return tex;
                        }
                    }
                    catch {}
                }
            }
            return null;
        }

        private void ApplyQuestionTexture(Texture2D tex)
        {
            activeQuestionTexture = tex;
            if (tex == null)
            {
                HideQuestionIllustration();
                return;
            }

            RefreshProblemCardLayout(true);
            RefreshLeftPhotoCardButtons(true);

            if (questionIllustrationImage != null)
            {
                questionIllustrationImage.texture = tex;
                questionIllustrationImage.color = Color.white;
                var fitter = questionIllustrationImage.GetComponent<AspectRatioFitter>();
                if (fitter != null && tex.height > 0)
                {
                    fitter.aspectRatio = (float)tex.width / (float)tex.height;
                }
            }

            if (questionImageContainer != null)
            {
                var pl = questionImageContainer.transform.Find("LoadingText");
                if (pl != null) pl.gameObject.SetActive(false);
            }

            if (questionZoomModalImage != null)
            {
                questionZoomModalImage.texture = tex;
                questionZoomModalImage.color = Color.white;
                if (questionZoomFitter != null && tex.height > 0)
                {
                    questionZoomFitter.aspectRatio = (float)tex.width / (float)tex.height;
                }
            }

            // Äá»“ng thá»i náº¡p áº£nh Ä‘á» bÃ i vÃ o imagePreview á»Ÿ khung bÃªn trÃ¡i náº¿u hiá»‡n táº¡i chÆ°a cÃ³ áº£nh bÃ i lÃ m
            if (imagePreview != null && !hasPhoto)
            {
                imagePreview.texture = tex;
                imagePreview.gameObject.SetActive(true);
                var prevFitter = imagePreview.GetComponent<AspectRatioFitter>();
                if (prevFitter != null && tex.height > 0)
                {
                    prevFitter.aspectRatio = (float)tex.width / (float)tex.height;
                }
                var previewBtn = imagePreview.GetComponent<Button>();
                if (previewBtn == null) previewBtn = imagePreview.gameObject.AddComponent<Button>();
                previewBtn.onClick.RemoveListener(OnOpenQuestionImageZoomModal);
                previewBtn.onClick.AddListener(OnOpenQuestionImageZoomModal);

                if (previewContainer != null)
                {
                    var ph = previewContainer.transform.Find("PlaceholderText");
                    if (ph != null) ph.gameObject.SetActive(false);
                }
            }
        }

        private void HideQuestionIllustration()
        {
            activeQuestionTexture = null;
            RefreshProblemCardLayout(false);
            RefreshLeftPhotoCardButtons(false);
            if (questionIllustrationImage != null)
            {
                questionIllustrationImage.texture = null;
                questionIllustrationImage.color = new Color(1f, 1f, 1f, 0f);
            }
            if (questionImageContainer != null)
            {
                var pl = questionImageContainer.transform.Find("LoadingText");
                if (pl != null) pl.gameObject.SetActive(true);
            }
            if (questionZoomModal != null)
            {
                questionZoomModal.SetActive(false);
            }
        }

        public void OnOpenQuestionImageZoomModal()
        {
            if (activeQuestionTexture == null)
            {
                if (questionIllustrationImage != null && questionIllustrationImage.texture != null)
                {
                    activeQuestionTexture = questionIllustrationImage.texture as Texture2D;
                }
                else
                {
                    activeQuestionTexture = LoadLocalFallbackTexture();
                }
            }

            if (activeQuestionTexture == null)
            {
                Debug.LogWarning("[EssayUIController] Không có hình ảnh để phóng to.");
                return;
            }

            PlayClick();

            if (questionZoomModal == null)
            {
                EnsureQuestionIllustrationUI();
            }

            if (questionZoomModal != null)
            {
                questionZoomModal.transform.SetAsLastSibling();
                questionZoomModal.SetActive(true);

                if (questionZoomModalImage != null)
                {
                    questionZoomModalImage.texture = activeQuestionTexture;
                    questionZoomModalImage.color = Color.white;
                    if (questionZoomFitter != null && activeQuestionTexture.height > 0)
                    {
                        questionZoomFitter.aspectRatio = (float)activeQuestionTexture.width / (float)activeQuestionTexture.height;
                    }
                }
            }
        }

        public void OnCloseQuestionImageZoomModal()
        {
            PlayClick();
            if (questionZoomModal != null)
            {
                questionZoomModal.SetActive(false);
            }
        }

        public void OnViewQuestionImageInWorkspaceClicked()
        {
            if (activeQuestionTexture == null)
            {
                activeQuestionTexture = LoadLocalFallbackTexture();
            }
            if (activeQuestionTexture == null) return;
            PlayClick();
            HideSampleNotebookOverlay();
            activePhotoTexture = activeQuestionTexture;
            SetPhotoPreview(activeQuestionTexture);
            hasPhoto = false;
            if (photoStatusText != null)
            {
                photoStatusText.text = "<color=#3498db>Đang hiển thị hình vẽ minh họa đề bài. Em có thể quan sát để giải vào ô bên phải. (Bấm vào ảnh để phóng to)</color>";
            }
        }
        #endregion
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
                    SetPhotoPreview(webCamTexture);
                    webCamTexture.Play();
                    hasPhoto = true;
                    isSamplePhoto = false;
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

                        SetPhotoPreview(photo);
                        hasPhoto = true;
                        if (photoStatusText != null) 
                            photoStatusText.text = "<color=#2ecc71>Đã chụp ảnh bài làm thành công! Đang chuyển văn bản vào ô nhập liệu...</color>";
                        RunOCRAndFillText(photo);
                    }
                    else
                    {
                        StopAndReleaseCamera();
                        if (photoStatusText != null) photoStatusText.text = "<color=#e74c3c>[!] Camera chưa sẵn sàng. Vui lòng bấm [2. Chọn Ảnh Từ Máy] hoặc gõ trực tiếp lời giải vào ô văn bản.</color>";
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Lỗi chụp ảnh từ webcam: " + ex.Message);
                    StopAndReleaseCamera();
                    if (photoStatusText != null) photoStatusText.text = "<color=#e74c3c>[!] Không thể kết nối camera. Vui lòng bấm [2. Chọn Ảnh Từ Máy] hoặc gõ trực tiếp lời giải vào ô văn bản.</color>";
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

            // Nếu câu hỏi hiện tại là bài luỹ thừa có ảnh vở chụp 2.3^2 + 5.(2+3) thì nạp ảnh chụp
            string p = currentQuestion != null ? currentQuestion.prompt : "";
            bool isPowSampleQuestion = p.Contains("3^2") || p.Contains("3²") || currentQuestion?.questionId == "Q_CH1_029";
            if (isPowSampleQuestion)
            {
                string samplePath = System.IO.Path.Combine(Application.dataPath, "_Project/Textures/sample_math_essay.png");
                if (System.IO.File.Exists(samplePath))
                {
                    HideSampleNotebookOverlay();
                    LoadImageFromFile(samplePath);
                    return;
                }
            }

            // Với mọi câu hỏi khác: Nạp trang vở kẻ ô ly với đúng lời giải mẫu của câu hỏi hiện tại!
            Texture2D sampleTex = CreateSampleNotebookTexture();
            activePhotoTexture = sampleTex;
            SetPhotoPreview(sampleTex);
            hasPhoto = true;
            isSamplePhoto = true;

            ShowSampleNotebookOverlay(currentQuestion?.prompt, currentQuestion?.questionId);

            string sampleSol = CurriculumEssayKnowledgeBase.GetSampleSolution(currentQuestion?.prompt, currentQuestion?.questionId);
            if (solutionInputField != null)
            {
                solutionInputField.text = sampleSol;
                solutionInputField.stringPosition = solutionInputField.text.Length;
                solutionInputField.caretPosition = solutionInputField.text.Length;
            }

            if (photoStatusText != null)
            {
                photoStatusText.text = "<color=#2ecc71>[OK] Đã nạp vở bài tập mẫu Toán 6 cho bài toán này! Em có thể bấm [GỬI BÀI GIẢI] để xem cách AI chấm điểm từng bước.</color>";
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
                        HideSampleNotebookOverlay();
                        activePhotoTexture = tex;
                        SetPhotoPreview(tex);
                        hasPhoto = true;
                        isSamplePhoto = false;

                        if (photoStatusText != null)
                        {
                            string fileName = System.IO.Path.GetFileName(path);
                            photoStatusText.text = $"<color=#2ecc71>[OK] Đã tải ảnh: {fileName}! Đang chuyển các dòng vào ô nhập liệu...</color>";
                        }
                        RunOCRAndFillText(tex);
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Không thể tải ảnh: " + ex.Message);
                if (photoStatusText != null)
                {
                    photoStatusText.text = "<color=#e74c3c>[!] Không thể đọc tệp ảnh đã chọn. Vui lòng thử lại với tệp ảnh khác!</color>";
                }
            }
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

            if (string.IsNullOrEmpty(studentSteps))
            {
                if (activePhotoTexture != null)
                {
                    if (photoStatusText != null)
                    {
                        photoStatusText.text = "<color=#f39c12>[...] Vui lòng chờ AI OCR nhận diện chữ từ ảnh hoặc tự gõ lời giải vào ô bên phải trước khi gửi!</color>";
                    }
                    RunOCRAndFillText(activePhotoTexture);
                }
                else
                {
                    if (photoStatusText != null)
                    {
                        photoStatusText.text = "<color=#e74c3c>[!] Vui lòng chụp ảnh bài làm, chọn tệp ảnh hoặc nhập lời giải vào ô bên phải trước khi gửi!</color>";
                    }
                }
                return;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (submitButton != null) submitButton.interactable = false;

            if (photoStatusText != null) photoStatusText.text = "[AI] AI Thầy Minh đang đối chiếu từng bước giải với Rubric tiêu chuẩn...";

            // GIẢI PHÁP TỐI ƯU CỐT LÕI:
            // Ảnh được Gemini OCR nhận diện đưa vào ô văn bản để học sinh kiểm tra, sửa đổi.
            // Khi nộp bài, gửi PURE TEXT (imageBase64 = null) để Gemini chấm chính xác 100% từng dòng, loại bỏ hoàn toàn hallucination!
            var payload = new EssaySubmissionPayload
            {
                questionId = currentQuestion != null ? currentQuestion.questionId : "Q_ESSAY",
                prompt = currentQuestion != null ? currentQuestion.prompt : "",
                studentSteps = studentSteps,
                imageBase64 = null,
                rubric = currentQuestion != null ? currentQuestion.rubric : null,
                correctAnswer = currentQuestion != null ? currentQuestion.correctAnswer : "",
                explanation = currentQuestion != null ? currentQuestion.explanation : ""
            };

            // Gọi AI Chấm điểm (Gemini Socratic Text Evaluation)
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
                            string cleanComment = (item.comment ?? "").Trim();
                            cleanComment = System.Text.RegularExpressions.Regex.Replace(
                                cleanComment,
                                @"^\[(Đúng|Dung|Đung|Chú ý|Chu y|Chủ ý)\]\s*",
                                "",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                            ).Trim();

                            details += $"{icon} Bước {item.step}: {cleanComment}\n";
                        }
                    }

                    // Reset và lưu vào ScoreSessionContext dùng chung
                    ScoreSessionContext.Reset();
                    ScoreSessionContext.ActivityType = "ESSAY";
                    ScoreSessionContext.Score = response.score;
                    ScoreSessionContext.MaxScore = response.maxScore;
                    ScoreSessionContext.CurrentQuestionId = currentQuestion != null ? currentQuestion.questionId : "";
                    ScoreSessionContext.CurrentQuestionPrompt = currentQuestion != null ? currentQuestion.prompt : "";
                    ScoreSessionContext.CurrentQuestionTopic = currentQuestion != null ? currentQuestion.topic : "";
                    ScoreSessionContext.RemedialExercise = response.remedialExercise ?? "";
                    ScoreSessionContext.EssayFeedback = $"{response.pedagogicalFeedback}\n\n{details}";
                    ScoreSessionContext.IsVersusMode = false;
                    ScoreSessionContext.TimeSpentSeconds = 90;

                    // Phân tách các dòng học sinh THỰC SỰ ĐÃ NHẬP (không dùng chuỗi hallucinated của AI)
                    string[] studentLines = (studentSteps ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    // Xác định dòng đầu tiên bị sai
                    int firstWrongIdx = response.firstWrongStepIndex;
                    if (firstWrongIdx < 0 && response.stepAnalysis != null && response.stepAnalysis.Count > 0)
                    {
                        for (int i = 0; i < response.stepAnalysis.Count; i++)
                        {
                            if (!response.stepAnalysis[i].isCorrect)
                            {
                                firstWrongIdx = i;
                                break;
                            }
                        }
                    }

                    string studentCorrectPrefix = "";
                    if (response.passed || response.score >= 10)
                    {
                        studentCorrectPrefix = "";
                        firstWrongIdx = -1;
                    }
                    else if (firstWrongIdx <= 0 || response.score == 0)
                    {
                        // Sai hết hoặc sai ngay từ dòng 1: Xóa sạch!
                        studentCorrectPrefix = "";
                        firstWrongIdx = 0;
                    }
                    else
                    {
                        // Đúng từ dòng 0 đến firstWrongIdx - 1:
                        // CHỈ TRÍCH XUẤT ĐÚNG CÁC DÒNG CỦA CHÍNH HỌC SINH ĐÃ LÀM ĐÚNG!
                        var sb = new System.Text.StringBuilder();
                        int validCount = Mathf.Min(firstWrongIdx, studentLines.Length);
                        for (int k = 0; k < validCount; k++)
                        {
                            string trimmed = studentLines[k].Trim();
                            if (!string.IsNullOrEmpty(trimmed))
                            {
                                if (sb.Length > 0) sb.Append("\n");
                                sb.Append(trimmed);
                            }
                        }
                        studentCorrectPrefix = sb.ToString();
                    }

                    ScoreSessionContext.PenaltyRuleToCopy = response.penaltyRuleToCopy ?? "";
                    ScoreSessionContext.FirstWrongLineIndex = firstWrongIdx;
                    // Build danh sach buoc sai de chep phat tung buoc: Sai tu buoc nao thi chep phat tu buoc do den het
                    ScoreSessionContext.WrongStepsToCopy.Clear();
                    if (!response.passed)
                    {
                        // 1. Uu tien Rubric chuan cua bai toan (tu currentQuestion hoac CurriculumEssayKnowledgeBase)
                        List<RubricItem> rubrics = currentQuestion?.rubric;
                        if (rubrics == null || rubrics.Count == 0)
                        {
                            rubrics = CurriculumEssayKnowledgeBase.GetRubric(
                                currentQuestion?.prompt, 
                                currentQuestion?.questionId, 
                                currentQuestion?.correctAnswer);
                        }

                        int startWrong = (firstWrongIdx >= 0) ? firstWrongIdx : 0;

                        if (rubrics != null && rubrics.Count > 0)
                        {
                            int startRi = Mathf.Clamp(startWrong, 0, rubrics.Count - 1);
                            for (int ri = startRi; ri < rubrics.Count; ri++)
                            {
                                var rItem = rubrics[ri];
                                if (!string.IsNullOrEmpty(rItem.description))
                                {
                                    ScoreSessionContext.WrongStepsToCopy.Add($"Bước {rItem.step}: {rItem.description}");
                                }
                            }
                        }
                        else if (response.stepAnalysis != null && response.stepAnalysis.Count > 0)
                        {
                            int totalSA = response.stepAnalysis.Count;
                            int startWi = Mathf.Clamp(startWrong, 0, totalSA - 1);
                            for (int wi = startWi; wi < totalSA; wi++)
                            {
                                var sa = response.stepAnalysis[wi];
                                string stepContent2 = !string.IsNullOrEmpty(sa.comment) ? sa.comment : $"Bước {sa.step}";
                                ScoreSessionContext.WrongStepsToCopy.Add($"Bước {sa.step}: {stepContent2}");
                            }
                        }
                    }
                    ScoreSessionContext.RetryCorrectLines = studentCorrectPrefix;
                    ScoreSessionContext.RetryWrongLineHint = response.pedagogicalFeedback ?? "";

                    PlayerPrefs.SetString("Retry_Essay_Correct_Lines", studentCorrectPrefix);
                    PlayerPrefs.SetInt("Retry_Essay_First_Wrong_Index", firstWrongIdx);
                    PlayerPrefs.SetString("Retry_Essay_Wrong_Hint", response.pedagogicalFeedback ?? "");
                    PlayerPrefs.Save();

                    // Nếu bài làm chưa đạt (dưới 8 điểm hoặc không pass), lưu vào Sổ tay câu sai (Anki)
                    if (!response.passed || response.score < 8)
                    {
                        if (string.IsNullOrEmpty(ScoreSessionContext.PenaltyRuleToCopy))
                        {
                            ScoreSessionContext.PenaltyRuleToCopy = CurriculumEssayKnowledgeBase.GetPenaltyRule(
                                response.detectedMistakeType, 
                                currentQuestion?.prompt, 
                                currentQuestion?.questionId
                            );
                        }

                        WrongQuestionBankManager.RecordWrongEssay(
                            currentQuestion?.prompt,
                            studentSteps,
                            currentQuestion?.correctAnswer,
                            $"{response.pedagogicalFeedback} | {details}",
                            currentQuestion?.topic ?? "ALL",
                            currentQuestion?.difficulty ?? "MEDIUM"
                        );
                    }
                    else
                    {
                        // Đã giải đúng hoàn toàn: Xóa trạng thái retry
                        PlayerPrefs.DeleteKey("Retry_Essay_Correct_Lines");
                        PlayerPrefs.DeleteKey("Retry_Essay_Wrong_Hint");
                        PlayerPrefs.Save();
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
                            studentAnswer = studentSteps,
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

        private GameObject geminiKeyModal;

        private TMP_InputField CreateModalInputField(Transform parent, string name, string placeholder, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.06f, 0.09f, 0.15f, 0.95f);

            var inputField = go.GetComponent<TMP_InputField>();

            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(go.transform, false);
            var taRt = textArea.GetComponent<RectTransform>();
            taRt.anchorMin = Vector2.zero; taRt.anchorMax = Vector2.one;
            taRt.offsetMin = new Vector2(10, 6); taRt.offsetMax = new Vector2(-10, -6);

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phGo.transform.SetParent(textArea.transform, false);
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
            phRt.offsetMin = Vector2.zero; phRt.offsetMax = Vector2.zero;
            var phText = phGo.GetComponent<TextMeshProUGUI>();
            phText.text = placeholder;
            phText.fontSize = 15;
            phText.fontStyle = FontStyles.Italic;
            phText.color = new Color(0.5f, 0.55f, 0.65f, 0.6f);

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(textArea.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var mainText = txtGo.GetComponent<TextMeshProUGUI>();
            mainText.fontSize = 15;
            mainText.color = Color.white;

            inputField.textViewport = taRt;
            inputField.textComponent = mainText;
            inputField.placeholder = phText;
            inputField.targetGraphic = img;

            return inputField;
        }

        public void ShowGeminiKeyModal()
        {
            if (geminiKeyModal != null)
            {
                geminiKeyModal.SetActive(true);
                return;
            }

            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            geminiKeyModal = new GameObject("Modal_GeminiKeyConfig", typeof(RectTransform), typeof(Image));
            geminiKeyModal.transform.SetParent(canvas.transform, false);
            var rtModal = geminiKeyModal.GetComponent<RectTransform>();
            rtModal.anchorMin = Vector2.zero;
            rtModal.anchorMax = Vector2.one;
            rtModal.offsetMin = Vector2.zero;
            rtModal.offsetMax = Vector2.zero;
            geminiKeyModal.GetComponent<Image>().color = new Color(0.04f, 0.07f, 0.12f, 0.90f);

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(geminiKeyModal.transform, false);
            var rtPanel = panelGo.GetComponent<RectTransform>();
            rtPanel.anchorMin = new Vector2(0.5f, 0.5f);
            rtPanel.anchorMax = new Vector2(0.5f, 0.5f);
            rtPanel.sizeDelta = new Vector2(680, 420);
            panelGo.GetComponent<Image>().color = new Color(0.11f, 0.15f, 0.22f);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(panelGo.transform, false);
            var rtT = titleGo.GetComponent<RectTransform>();
            rtT.anchoredPosition = new Vector2(0, 160);
            rtT.sizeDelta = new Vector2(640, 50);
            var tmpT = titleGo.GetComponent<TextMeshProUGUI>();
            tmpT.text = "[KEY] CẤU HÌNH GEMINI VISION OCR (GOOGLE AI)";
            tmpT.fontSize = 20;
            tmpT.fontStyle = FontStyles.Bold;
            tmpT.alignment = TextAlignmentOptions.Center;
            tmpT.color = new Color(0.95f, 0.77f, 0.2f);

            var descGo = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descGo.transform.SetParent(panelGo.transform, false);
            var rtD = descGo.GetComponent<RectTransform>();
            rtD.anchoredPosition = new Vector2(0, 75);
            rtD.sizeDelta = new Vector2(620, 110);
            var tmpD = descGo.GetComponent<TextMeshProUGUI>();
            tmpD.text = "Dán khóa <b>Gemini API Key</b> vào đây để kích hoạt AI trực tiếp đọc ảnh bài làm:\n" +
                        "- Tự động OCR chữ viết tay học sinh từ ảnh chụp Camera / tệp ảnh trên máy.\n" +
                        "- Nhận diện được cả ảnh xoay ngược 180 độ, xoay nghiêng 90 độ, chữ viết tay xấu/mờ.\n" +
                        "- Tự động phân định và từ chối ảnh phong cảnh, con vật...\n" +
                        "<i>(Khóa API được cấp miễn phí tại: <color=#3498db>https://aistudio.google.com</color>)</i>";
            tmpD.fontSize = 14;
            tmpD.alignment = TextAlignmentOptions.TopLeft;
            tmpD.color = new Color(0.85f, 0.88f, 0.92f);

            string currentKey = GeminiAITutorService.Instance != null ? GeminiAITutorService.Instance.EffectiveApiKey : PlayerPrefs.GetString("GEMINI_API_KEY", "");
            var inputField = CreateModalInputField(panelGo.transform, "KeyInput", "Dán khóa API Key bắt đầu bằng AIzaSy... vào đây", new Vector2(0, -25), new Vector2(620, 48));
            inputField.text = currentKey;

            var statusGo = new GameObject("TxtStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusGo.transform.SetParent(panelGo.transform, false);
            var rtS = statusGo.GetComponent<RectTransform>();
            rtS.anchoredPosition = new Vector2(0, -85);
            rtS.sizeDelta = new Vector2(620, 36);
            var tmpS = statusGo.GetComponent<TextMeshProUGUI>();
            tmpS.text = string.IsNullOrEmpty(currentKey) ? "<color=#e74c3c>Trạng thái: Chưa cấu hình API Key (đang chạy Offline Mock).</color>" : "<color=#2ecc71>Trạng thái: Đã có API Key hoạt động!</color>";
            tmpS.fontSize = 14;
            tmpS.alignment = TextAlignmentOptions.Center;

            var saveGo = new GameObject("Btn_SaveKey", typeof(RectTransform), typeof(Image), typeof(Button));
            saveGo.transform.SetParent(panelGo.transform, false);
            var rtSave = saveGo.GetComponent<RectTransform>();
            rtSave.anchoredPosition = new Vector2(-120, -150);
            rtSave.sizeDelta = new Vector2(200, 44);
            saveGo.GetComponent<Image>().color = new Color(0.16f, 0.65f, 0.38f);
            var saveBtn = saveGo.GetComponent<Button>();
            var txtSave = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtSave.transform.SetParent(saveGo.transform, false);
            var tmpSave = txtSave.GetComponent<TextMeshProUGUI>();
            tmpSave.text = "[Lưu] Lưu & Kích Hoạt";
            tmpSave.fontSize = 16;
            tmpSave.fontStyle = FontStyles.Bold;
            tmpSave.alignment = TextAlignmentOptions.Center;
            tmpSave.color = Color.white;

            saveBtn.onClick.AddListener(() =>
            {
                string key = (inputField.text ?? "").Trim();
                PlayerPrefs.SetString("GEMINI_API_KEY", key);
                PlayerPrefs.Save();
                if (GeminiAITutorService.Instance != null) GeminiAITutorService.Instance.ApiKey = key;
                tmpS.text = string.IsNullOrEmpty(key) 
                    ? "<color=#f39c12>Đã xoá khóa API. Trở về chế độ Ngoại Tuyến.</color>" 
                    : "<color=#2ecc71>[OK] Đã lưu và kích hoạt Gemini Vision OCR thành công!</color>";
            });

            var closeGo = new GameObject("Btn_CloseModal", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(panelGo.transform, false);
            var rtClose = closeGo.GetComponent<RectTransform>();
            rtClose.anchoredPosition = new Vector2(120, -150);
            rtClose.sizeDelta = new Vector2(200, 44);
            closeGo.GetComponent<Image>().color = new Color(0.75f, 0.22f, 0.17f);
            var closeBtn = closeGo.GetComponent<Button>();
            var txtClose = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtClose.transform.SetParent(closeGo.transform, false);
            var tmpClose = txtClose.GetComponent<TextMeshProUGUI>();
            tmpClose.text = "[X] Đóng";
            tmpClose.fontSize = 16;
            tmpClose.fontStyle = FontStyles.Bold;
            tmpClose.alignment = TextAlignmentOptions.Center;
            tmpClose.color = Color.white;

            closeBtn.onClick.AddListener(() => geminiKeyModal.SetActive(false));
        }

}

}
