using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Math6Companion.Core;

namespace Math6Companion.UI
{
    /// <summary>
    /// =========================================================================
    ///             GIAO DIỆN BẢNG ĐIỀU KHIỂN & CẤU HÌNH DÀNH CHO GIÁO VIÊN
    ///                         (TEACHER PORTAL CONTROLLER)
    /// =========================================================================
    /// Phân quyền bảo mật:
    /// - Tài khoản có Role == "TEACHER" truy cập trực tiếp.
    /// - Tài khoản học sinh (hoặc khách) phải nhập đúng Mã PIN Giáo Viên (mặc định "1234").
    ///
    /// 5 Tab Quản trị chuyên sâu:
    /// 1. LỘ TRÌNH & BÀI HỌC: Mở rộng khối 6, 7, 8, 9, bật/tắt bài học, đổi link video.
    /// 2. QUẢN LÝ LỚP HỌC: Lớp 6A1, 6A2, 7A1... xem sĩ số, điểm TB, giao bài tập.
    /// 3. NGÂN HÀNG ĐỀ & RUBRIC: Quản lý câu trắc nghiệm và tự luận kèm barem chấm Gemini.
    /// 4. TINH CHỈNH GIA SƯ AI: Prompt Socratic/Ân cần, Model Gemini, API Key.
    /// 5. CẤU HÌNH HỆ THỐNG: Thời lượng PvP, số câu quiz, Backend Google Sheets.
    /// =========================================================================
    /// </summary>
    public class TeacherPortalUIController : MonoBehaviour
    {
        private static bool isQuitting = false;
        private static TeacherPortalUIController instance;

        public static TeacherPortalUIController Instance
        {
            get
            {
                if (isQuitting) return null;
                if (instance == null)
                {
                    instance = FindAnyObjectByType<TeacherPortalUIController>();
                    if (instance == null && !isQuitting)
                    {
                        var go = new GameObject("[TeacherPortalUIController]");
                        instance = go.AddComponent<TeacherPortalUIController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        #region UI State & Controls
        private GameObject rootPortalCanvas;
        private GameObject portalPanel;
        private GameObject pinModalGo;
        private TMP_InputField pinInputField;
        private TextMeshProUGUI pinErrorText;

        // Modal Edit Lesson
        private GameObject editLessonModalGo;
        private TMP_InputField editLessonTitleInput;
        private TMP_InputField editLessonTopicInput;
        private TMP_InputField editLessonSummaryInput;
        private TMP_InputField editLessonVideoInput;
        private string currentEditingLessonId;
        private bool isCreatingNewLesson = false;
        // Modal Edit Question & Answers
        private GameObject editQuestionModalGo;
        private TMP_InputField editQPromptInput;
        private TMP_InputField editQTopicInput;
        private TMP_InputField editQOptAInput;
        private TMP_InputField editQOptBInput;
        private TMP_InputField editQOptCInput;
        private TMP_InputField editQOptDInput;
        private TMP_InputField editQCorrectInput;
        private TMP_InputField editQRubricInput;
        private TMP_InputField editQExplainInput;
        private GameObject editQMcqGroup;
        private GameObject editQEssayGroup;
        private TextMeshProUGUI editQHeaderTitle;
        private string currentEditingQuestionId;
        private string currentEditingQuestionType;
        private bool isAddingNewQuestion = false;

        private int currentTabIndex = 0;
        private string selectedClassView = "6A1";

        // Tab Content Containers
        private GameObject tabContentCurriculum;
        private GameObject tabContentClasses;
        private GameObject tabContentQuestions;
        private GameObject tabContentAI;
        private GameObject tabContentSystem;

        // Header Tab Buttons
        private Button[] tabButtons = new Button[5];
        private Image[] tabButtonImages = new Image[5];
        private TextMeshProUGUI[] tabButtonTexts = new TextMeshProUGUI[5];

        // Toast Feedback Text
        private TextMeshProUGUI toastText;
        private float toastTimer = 0f;

        // Visual Colors Palette
        private static readonly Color ColorDarkBackdrop = new Color(0.04f, 0.06f, 0.10f, 0.96f);
        private static readonly Color ColorWindowBg = new Color(0.08f, 0.11f, 0.18f, 1f);
        private static readonly Color ColorCardBg = new Color(0.12f, 0.16f, 0.25f, 1f);
        private static readonly Color ColorHeaderBg = new Color(0.09f, 0.13f, 0.22f, 1f);
        private static readonly Color ColorBorderLine = new Color(0.20f, 0.26f, 0.38f, 1f);
        private static readonly Color ColorPrimaryBlue = new Color(0.15f, 0.45f, 0.85f, 1f);
        private static readonly Color ColorEmeraldGreen = new Color(0.10f, 0.65f, 0.45f, 1f);
        private static readonly Color ColorAmberGold = new Color(0.95f, 0.65f, 0.15f, 1f);
        private static readonly Color ColorCoralRed = new Color(0.88f, 0.25f, 0.25f, 1f);
        private static readonly Color ColorPurple = new Color(0.55f, 0.30f, 0.85f, 1f);
        private static readonly Color ColorTextTitle = new Color(0.95f, 0.97f, 1.0f, 1f);
        private static readonly Color ColorTextMuted = new Color(0.70f, 0.75f, 0.85f, 1f);
        #endregion

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void Update()
        {
            if (toastTimer > 0f)
            {
                toastTimer -= Time.deltaTime;
                if (toastTimer <= 0f && toastText != null)
                {
                    toastText.text = "";
                }
            }

            // Phím tắt F2 hoặc Alt+T để mở Teacher Portal
            if (Input.GetKeyDown(KeyCode.F2) || (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.T)))
            {
                if (rootPortalCanvas != null && rootPortalCanvas.activeSelf)
                {
                    HideAll();
                }
                else
                {
                    RequestOpen();
                }
            }
        }

        #region Access Control & Public API
        public static void Open()
        {
            Instance.RequestOpen();
        }

        public static void Close()
        {
            Instance.HideAll();
        }

        public void RequestOpen()
        {
            EnsureCanvasCreated();

            // Kiểm tra phân quyền vai trò
            bool isTeacher = SessionManager.Instance != null && SessionManager.Instance.IsTeacher;
            if (isTeacher)
            {
                // Giáo viên đăng nhập chính thức -> Vào thẳng Dashboard
                ShowPortalWindow();
            }
            else
            {
                // Học sinh hoặc tài khoản chưa xác thực -> Yêu cầu nhập mã PIN Giáo Viên
                ShowPinModal();
            }
        }

        public void ShowPortalWindow()
        {
            EnsureCanvasCreated();
            if (pinModalGo != null) pinModalGo.SetActive(false);

            rootPortalCanvas.SetActive(true);
            portalPanel.SetActive(true);
            SwitchTab(currentTabIndex);
            ShowToast("Chào mừng Thầy/Cô đến với Bảng Điều Khiển Quản Trị Giáo Viên!");
        }

        public void ShowPinModal()
        {
            EnsureCanvasCreated();
            if (portalPanel != null) portalPanel.SetActive(false);

            rootPortalCanvas.SetActive(true);
            pinModalGo.SetActive(true);

            if (pinInputField != null)
            {
                pinInputField.text = "";
                pinInputField.ActivateInputField();
            }
            if (pinErrorText != null)
            {
                pinErrorText.text = "";
            }
        }

        public void HideAll()
        {
            if (rootPortalCanvas != null)
            {
                rootPortalCanvas.SetActive(false);
            }
        }

        public void ShowToast(string message, float duration = 3.5f)
        {
            if (toastText != null)
            {
                toastText.text = message;
                toastTimer = duration;
            }
            Debug.Log($"[TeacherPortal] {message}");
        }

        private void OnVerifyPinSubmit()
        {
            string enteredPin = pinInputField != null ? pinInputField.text.Trim() : "";

            if (TeacherConfigManager.Instance.VerifyTeacherPin(enteredPin))
            {
                if (pinErrorText != null)
                {
                    pinErrorText.color = ColorEmeraldGreen;
                    pinErrorText.text = "Xác thực thành công! Đang mở bảng điều khiển...";
                }
                ShowPortalWindow();
            }
            else
            {
                if (pinErrorText != null)
                {
                    pinErrorText.color = ColorCoralRed;
                    pinErrorText.text = "Mã PIN không chính xác! Vui lòng liên hệ Giáo viên bộ môn.";
                }
            }
        }
        #endregion

        #region UI Construction
        private void EnsureCanvasCreated()
        {
            if (rootPortalCanvas != null) return;

            // 1. Root Canvas
            rootPortalCanvas = new GameObject("[TeacherPortalCanvas]", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rootPortalCanvas.transform.SetParent(transform, false);
            var canvas = rootPortalCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32700;

            var scaler = rootPortalCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // 2. Fullscreen Dark Backdrop Dimmer (Opaque dark to block lower scenes completely)
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdrop.transform.SetParent(rootPortalCanvas.transform, false);
            var bRt = backdrop.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;
            var bImg = backdrop.GetComponent<Image>();
            bImg.color = ColorDarkBackdrop;
            var bBtn = backdrop.GetComponent<Button>();
            bBtn.onClick.AddListener(HideAll);

            // 3. Main Dashboard Window Panel (Large, expansive framed layout)
            portalPanel = new GameObject("DashboardWindow", typeof(RectTransform), typeof(Image));
            portalPanel.transform.SetParent(rootPortalCanvas.transform, false);
            var pRt = portalPanel.GetComponent<RectTransform>();
            pRt.anchorMin = Vector2.zero;
            pRt.anchorMax = Vector2.one;
            pRt.offsetMin = new Vector2(50, 30);
            pRt.offsetMax = new Vector2(-50, -30);
            var pImg = portalPanel.GetComponent<Image>();
            pImg.color = ColorWindowBg;

            // 4. Header Bar
            BuildHeader(portalPanel);

            // 5. 5-Tab Navigation Bar
            BuildTabBar(portalPanel);

            // 6. Content Containers for 5 Tabs
            BuildCurriculumTab(portalPanel);
            BuildClassManagementTab(portalPanel);
            BuildQuestionBankTab(portalPanel);
            BuildAIConfigTab(portalPanel);
            BuildSystemConfigTab(portalPanel);

            // 7. Footer Status & Toast Bar
            BuildFooter(portalPanel);

            // 8. Teacher Verification PIN Modal
            BuildTeacherPinModal(rootPortalCanvas);

            // 9. Edit Lesson Modal Dialog
            BuildEditLessonModal(rootPortalCanvas);

            // 10. Edit Question Modal Dialog
            BuildEditQuestionModal(rootPortalCanvas);

            rootPortalCanvas.SetActive(false);
        }

        private void BuildHeader(GameObject parent)
        {
            var header = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(parent.transform, false);
            var rt = header.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, 85);
            header.GetComponent<Image>().color = ColorHeaderBg;

            // Title & Subtitle (Left Anchored)
            var title = CreateLeftText(header, "TitleText", "<b>BẢNG ĐIỀU KHIỂN & CẤU HÌNH DÀNH CHO GIÁO VIÊN</b> <color=#FBBF24>(TEACHER PORTAL)</color>", 30, 12, 1100, 22, ColorTextTitle);
            var sub = CreateLeftText(header, "SubtitleText", "Tùy chỉnh lộ trình bài học Toán 6 • Quản lý các lớp 6A1 - 6A4 • Tinh chỉnh Gia sư AI Thầy Minh & Barem Rubric", 30, -18, 1100, 14, ColorTextMuted);

            // Close Button (Right Anchored)
            var closeBtn = CreateRightButton(header, "Btn_Close", "Đóng [X]", 25, 0, new Vector2(120, 44), ColorCoralRed);
            closeBtn.onClick.AddListener(HideAll);
        }

        private void BuildTabBar(GameObject parent)
        {
            var bar = new GameObject("TabBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent.transform, false);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -85);
            rt.sizeDelta = new Vector2(0, 56);
            bar.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.22f, 1f);

            string[] tabNames = new string[]
            {
                "[1] LỘ TRÌNH & BÀI HỌC",
                "[2] QUẢN LÝ LỚP HỌC",
                "[3] NGÂN HÀNG ĐỀ & RUBRIC",
                "[4] CẤU HÌNH GIA SƯ AI",
                "[5] CẤU HÌNH HỆ THỐNG"
            };

            for (int i = 0; i < tabNames.Length; i++)
            {
                int tabIdx = i;
                float normMin = (float)i / tabNames.Length;
                float normMax = (float)(i + 1) / tabNames.Length;

                var btnGo = new GameObject($"TabBtn_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(bar.transform, false);
                var bRt = btnGo.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(normMin, 0f);
                bRt.anchorMax = new Vector2(normMax, 1f);
                bRt.offsetMin = new Vector2(2, 2);
                bRt.offsetMax = new Vector2(-2, -2);

                var img = btnGo.GetComponent<Image>();
                img.color = (i == 0) ? ColorPrimaryBlue : Color.clear;

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(btnGo.transform, false);
                var tRt = txtGo.GetComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.offsetMin = Vector2.zero;
                tRt.offsetMax = Vector2.zero;
                var txt = txtGo.GetComponent<TextMeshProUGUI>();
                txt.text = tabNames[i];
                txt.fontSize = 15;
                txt.fontStyle = (i == 0) ? FontStyles.Bold : FontStyles.Normal;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = (i == 0) ? Color.white : ColorTextMuted;

                tabButtons[i] = btnGo.GetComponent<Button>();
                tabButtonImages[i] = img;
                tabButtonTexts[i] = txt;

                tabButtons[i].onClick.AddListener(() => SwitchTab(tabIdx));
            }
        }

        private void SwitchTab(int index)
        {
            currentTabIndex = index;

            for (int i = 0; i < 5; i++)
            {
                if (tabButtonImages[i] != null)
                {
                    tabButtonImages[i].color = (i == index) ? ColorPrimaryBlue : Color.clear;
                }
                if (tabButtonTexts[i] != null)
                {
                    tabButtonTexts[i].fontStyle = (i == index) ? FontStyles.Bold : FontStyles.Normal;
                    tabButtonTexts[i].color = (i == index) ? Color.white : ColorTextMuted;
                }
            }

            if (tabContentCurriculum != null) tabContentCurriculum.SetActive(index == 0);
            if (tabContentClasses != null) tabContentClasses.SetActive(index == 1);
            if (tabContentQuestions != null) tabContentQuestions.SetActive(index == 2);
            if (tabContentAI != null) tabContentAI.SetActive(index == 3);
            if (tabContentSystem != null) tabContentSystem.SetActive(index == 4);

            if (index == 0) RefreshCurriculumTab();
            if (index == 1) RefreshClassManagementTab();
            if (index == 2) RefreshQuestionBankTab();
        }
        #endregion

        #region Tab 1: Curriculum & Lessons Roadmap
        private GameObject curriculumListContainer;

        private void BuildCurriculumTab(GameObject parent)
        {
            tabContentCurriculum = CreateTabContentContainer(parent, "TabContent_Curriculum");

            // Sub-header: Grade Banner Bar (Toán Lớp 6 chuẩn mực)
            var gradeBar = new GameObject("GradeSelectorBar", typeof(RectTransform), typeof(Image));
            gradeBar.transform.SetParent(tabContentCurriculum.transform, false);
            var gRt = gradeBar.GetComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0f, 1f);
            gRt.anchorMax = new Vector2(1f, 1f);
            gRt.pivot = new Vector2(0.5f, 1f);
            gRt.anchoredPosition = new Vector2(0, -10);
            gRt.sizeDelta = new Vector2(-40, 56);
            gradeBar.GetComponent<Image>().color = ColorCardBg;

            CreateLeftText(gradeBar, "GradeLbl", "<b>CHƯƠNG TRÌNH & LỘ TRÌNH TOÁN LỚP 6 (BỘ CHUẨN GDPT 2018)</b>", 30, 0, 600, 16, ColorAmberGold);

            // Nút Đồng Bộ Google Sheets Database
            var btnSync = CreateRightButton(gradeBar, "Btn_SyncCurriculum", "[Đồng Bộ] Google Sheets", 235, 0, new Vector2(210, 40), ColorEmeraldGreen);
            btnSync.onClick.AddListener(() =>
            {
                ShowToast("Đang đồng bộ danh mục bài học từ Google Sheets Database...");
                TeacherConfigManager.Instance.SyncFromDatabase((ok, msg) =>
                {
                    ShowToast(msg);
                    RefreshCurriculumTab();
                });
            });

            // Button Thêm bài học mới (Right Anchored)
            var btnAdd = CreateRightButton(gradeBar, "Btn_AddLesson", "[+] Thêm Bài Học Mới", 15, 0, new Vector2(205, 40), ColorPrimaryBlue);
            btnAdd.onClick.AddListener(OpenCreateLessonModal);

            // Scrollable Lesson List View
            curriculumListContainer = CreateScrollListContainer(tabContentCurriculum, "LessonScrollList", new Vector2(0, -78), new Vector2(-40, -100));
            RefreshCurriculumTab();
        }

        private void RefreshCurriculumTab()
        {
            if (curriculumListContainer == null) return;

            for (int i = curriculumListContainer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(curriculumListContainer.transform.GetChild(i).gameObject);
            }

            var lessons = TeacherConfigManager.Instance.GetLessonsForGrade(6);

            for (int i = 0; i < lessons.Count; i++)
            {
                var les = lessons[i];
                BuildLessonItemRow(curriculumListContainer, les, i);
            }

            StartCoroutine(ResetScrollToTopRoutine(curriculumListContainer));
        }

        private System.Collections.IEnumerator ResetScrollToTopRoutine(GameObject container)
        {
            yield return null;
            if (container != null)
            {
                var cRt = container.GetComponent<RectTransform>();
                if (cRt != null) cRt.anchoredPosition = Vector2.zero;
                var sr = container.GetComponentInParent<ScrollRect>();
                if (sr != null)
                {
                    sr.StopMovement();
                    sr.verticalNormalizedPosition = 1f;
                }
            }
        }

        private void BuildLessonItemRow(GameObject parent, TeacherConfigManager.TeacherLessonConfig les, int index)
        {
            var row = new GameObject($"LessonRow_{les.lessonId}", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent.transform, false);
            var rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 115);
            row.GetComponent<Image>().color = index % 2 == 0 ? ColorCardBg : new Color(0.10f, 0.13f, 0.20f, 1f);

            // Left Info (Anchored Left, lùi vào 35px để tuyệt đối không bị che mất chữ cái đầu)
            CreateLeftText(row, "Title", $"<b>{les.title}</b>  <color=#FBBF24>[{les.topic}]</color>  <color=#38BDF8>({les.lessonId})</color>", 35, 26, 1050, 17, ColorTextTitle);
            CreateLeftText(row, "Summary", les.summaryText.Replace("\n", " • "), 35, -6, 1050, 14, ColorTextMuted);
            CreateLeftText(row, "Video", $"Video: <color=#38BDF8>{les.videoUrl}</color>", 35, -34, 1050, 12, new Color(0.5f, 0.7f, 0.9f));

            // Right Action Buttons (Right Anchored)
            var btnToggle = CreateRightButton(row, "Btn_Toggle", les.isEnabled ? "Đang Mở (Active) [ON]" : "Đã Khóa (Off) [OFF]", 25, 20, new Vector2(210, 38), les.isEnabled ? ColorEmeraldGreen : ColorCoralRed);
            btnToggle.onClick.AddListener(() =>
            {
                TeacherConfigManager.Instance.ToggleLessonStatus(les.lessonId, !les.isEnabled);
                ShowToast($"Đã {(les.isEnabled ? "khóa" : "mở")} bài học '{les.title}'!");
                RefreshCurriculumTab();
            });

            var btnEdit = CreateRightButton(row, "Btn_Edit", "Chỉnh Sửa", 115, -22, new Vector2(100, 38), ColorPrimaryBlue);
            btnEdit.onClick.AddListener(() =>
            {
                OpenEditLessonModal(les);
            });

            var btnDel = CreateRightButton(row, "Btn_Del", "Xóa [X]", 25, -22, new Vector2(80, 38), ColorCoralRed);
            btnDel.onClick.AddListener(() =>
            {
                TeacherConfigManager.Instance.DeleteLesson(les.lessonId);
                Destroy(row);
                ShowToast($"Đã xóa bài học '{les.title}'!");
                RefreshCurriculumTab();
            });
        }
        #endregion

        #region Tab 2: Class & Student Management
        private GameObject classCardsContainer;
        private GameObject classAssignContainer;

        private void BuildClassManagementTab(GameObject parent)
        {
            tabContentClasses = CreateTabContentContainer(parent, "TabContent_Classes");

            // Sub-header: Class Selector Bar
            var classBar = new GameObject("ClassSelectorBar", typeof(RectTransform), typeof(Image));
            classBar.transform.SetParent(tabContentClasses.transform, false);
            var cRt = classBar.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.anchoredPosition = new Vector2(0, -10);
            cRt.sizeDelta = new Vector2(-40, 56);
            classBar.GetComponent<Image>().color = ColorCardBg;

            CreateLeftText(classBar, "ClassLbl", "<b>CHỌN LỚP HỌC QUẢN LÝ (CLASS SELECTION):</b>", 25, 0, 400, 15, ColorAmberGold);

            string[] classNames = { "6A1", "6A2", "6A3", "6A4" };
            for (int i = 0; i < classNames.Length; i++)
            {
                string cId = classNames[i];
                var btn = CreateLeftButton(classBar, $"Btn_Class_{cId}", $"Lớp {cId}", 430 + i * 140, 0, new Vector2(125, 38),
                    cId == selectedClassView ? ColorPurple : ColorBorderLine);
                btn.onClick.AddListener(() =>
                {
                    selectedClassView = cId;
                    TeacherConfigManager.Instance.SetActiveClassId(cId);
                    ShowToast($"Đã chuyển sang quản lý Lớp {cId}!");
                    RefreshClassManagementTab();
                });
            }

            // Cards Container (Horizontal Grid for 4 Metric Cards)
            classCardsContainer = new GameObject("ClassCardsContainer", typeof(RectTransform));
            classCardsContainer.transform.SetParent(tabContentClasses.transform, false);
            var ccRt = classCardsContainer.GetComponent<RectTransform>();
            ccRt.anchorMin = new Vector2(0f, 1f);
            ccRt.anchorMax = new Vector2(1f, 1f);
            ccRt.pivot = new Vector2(0.5f, 1f);
            ccRt.anchoredPosition = new Vector2(0, -78);
            ccRt.sizeDelta = new Vector2(-40, 160);

            // Assign & Action Box Container
            classAssignContainer = new GameObject("ClassAssignContainer", typeof(RectTransform), typeof(Image));
            classAssignContainer.transform.SetParent(tabContentClasses.transform, false);
            var caRt = classAssignContainer.GetComponent<RectTransform>();
            caRt.anchorMin = new Vector2(0f, 0f);
            caRt.anchorMax = new Vector2(1f, 1f);
            caRt.offsetMin = new Vector2(20, 20);
            caRt.offsetMax = new Vector2(-20, -250);
            classAssignContainer.GetComponent<Image>().color = ColorCardBg;

            RefreshClassManagementTab();
        }

        private void RefreshClassManagementTab()
        {
            if (classCardsContainer == null || classAssignContainer == null) return;

            for (int i = classCardsContainer.transform.childCount - 1; i >= 0; i--)
                Destroy(classCardsContainer.transform.GetChild(i).gameObject);

            for (int i = classAssignContainer.transform.childCount - 1; i >= 0; i--)
                Destroy(classAssignContainer.transform.GetChild(i).gameObject);

            if (!selectedClassView.StartsWith("6"))
            {
                selectedClassView = "6A1";
            }
            var cls = TeacherConfigManager.Instance.GetClassInfo(selectedClassView);
            if (cls == null)
            {
                selectedClassView = "6A1";
                cls = TeacherConfigManager.Instance.GetClassInfo(selectedClassView);
            }
            if (cls == null) return;

            // 4 Metric Cards Evenly Distributed Across Width (-630, -210, +210, +630)
            BuildMetricCard(classCardsContainer, "SĨ SỐ HỌC SINH", $"{cls.studentCount} em", "Học sinh chính thức", -630, ColorPrimaryBlue);
            BuildMetricCard(classCardsContainer, "ĐIỂM TRUNG BÌNH LỚP", $"{cls.averageScore:F1} / 10", "Xếp loại Giỏi", -210, ColorAmberGold);
            BuildMetricCard(classCardsContainer, "TỶ LỆ HOÀN THÀNH", $"{cls.completionRate * 100f:F0}%", "Tiến độ toàn khóa", 210, ColorEmeraldGreen);
            BuildMetricCard(classCardsContainer, "THỦ KHOA DẪN ĐẦU", cls.topStudentName, "16 lượt làm hoàn hảo", 630, ColorPurple);

            // Assign Box Content
            CreateLeftText(classAssignContainer, "Header", $"<b>GIAO BÀI TẬP VỀ NHÀ & LẬP GIẢI ĐẤU CHO LỚP {cls.classId}</b>", 30, 110, 1200, 20, ColorAmberGold);
            CreateLeftText(classAssignContainer, "CurTask", $"• Bài tập hiện tại đang giao: <color=#38BDF8><b>{cls.assignedLessonId}</b></color> | Hạn nộp bài: <color=#F87171><b>{cls.homeworkDeadline}</b></color>", 30, 65, 1200, 16, ColorTextTitle);
            CreateLeftText(classAssignContainer, "Note", "Học sinh trong lớp khi đăng nhập sẽ tự động nhận thông báo bài tập từ Thầy/Cô trên Bảng Cáo Thị Làng Sen 3D.", 30, 25, 1200, 14, ColorTextMuted);

            // 3 Bottom Action Buttons Symmetrically Centered (-420, 0, +420)
            var btnAssign = CreateCenteredButton(classAssignContainer, "Btn_Assign", "[+] Giao Bài Tập Mới (Đổi Bài / Hạn Nộp)", -420, -60, new Vector2(380, 52), ColorPrimaryBlue);
            btnAssign.onClick.AddListener(() =>
            {
                TeacherConfigManager.Instance.AssignHomeworkToClass(cls.classId, "LES_G6_03", "Chủ Nhật 23:59");
                ShowToast($"Đã giao bài tập mới (Chuyên đề Phân số) cho Lớp {cls.classId}!");
                RefreshClassManagementTab();
            });

            var btnPvpTour = CreateCenteredButton(classAssignContainer, "Btn_PvPTournament", "[PvP] Mở Giải Đấu Đối Kháng Liên Lớp", 0, -60, new Vector2(380, 52), ColorAmberGold);
            btnPvpTour.onClick.AddListener(() =>
            {
                ShowToast($"Đã kích hoạt Giải đấu Đối Kháng 3P cho Lớp {cls.classId}!");
            });

            var btnExport = CreateCenteredButton(classAssignContainer, "Btn_Export", "[Báo Cáo] Xuất Điểm Lớp Excel/PDF", 420, -60, new Vector2(380, 52), ColorEmeraldGreen);
            btnExport.onClick.AddListener(() =>
            {
                ShowToast($"Đã kết xuất báo cáo thống kê tiến độ học tập Lớp {cls.classId}!");
            });
        }

        private void BuildMetricCard(GameObject parent, string label, string value, string note, float posX, Color accent)
        {
            var card = new GameObject($"Card_{label}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent.transform, false);
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(posX, 0);
            rt.sizeDelta = new Vector2(380, 140);
            card.GetComponent<Image>().color = ColorCardBg;

            CreateLeftText(card, "Lbl", label, 20, 40, 340, 14, accent);
            CreateLeftText(card, "Val", value, 20, 0, 340, 22, ColorTextTitle);
            CreateLeftText(card, "Note", note, 20, -36, 340, 13, ColorTextMuted);
        }
        #endregion

        #region Tab 3: Question Bank & Rubric
        private GameObject questionListContainer;
        private TextMeshProUGUI questionCountLabel;

        private void BuildQuestionBankTab(GameObject parent)
        {
            tabContentQuestions = CreateTabContentContainer(parent, "TabContent_Questions");

            var qBar = new GameObject("QBar", typeof(RectTransform), typeof(Image));
            qBar.transform.SetParent(tabContentQuestions.transform, false);
            var rt = qBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -10);
            rt.sizeDelta = new Vector2(-40, 56);
            qBar.GetComponent<Image>().color = ColorCardBg;

            questionCountLabel = CreateLeftText(qBar, "QLbl", "<b>NGÂN HÀNG ĐỀ & BAREM RUBRIC AI CHẤM:</b>", 25, 0, 480, 15, ColorAmberGold);

            // Nút Đồng bộ câu hỏi từ Google Sheets Database
            var btnSync = CreateRightButton(qBar, "Btn_SyncQuestions", "[Đồng Bộ] Google Sheets", 495, 0, new Vector2(210, 40), ColorPrimaryBlue);
            btnSync.onClick.AddListener(() =>
            {
                ShowToast("Đang đồng bộ toàn bộ ngân hàng câu hỏi từ Google Sheets Database...");
                TeacherConfigManager.Instance.SyncFromDatabase((ok, msg) =>
                {
                    ShowToast(msg);
                    RefreshQuestionBankTab();
                });
            });

            var btnAddMcq = CreateRightButton(qBar, "Btn_AddMCQ", "[+] Thêm Trắc Nghiệm", 255, 0, new Vector2(230, 40), ColorEmeraldGreen);
            btnAddMcq.onClick.AddListener(() =>
            {
                OpenAddQuestionModal("MCQ");
            });

            var btnAddEssay = CreateRightButton(qBar, "Btn_AddEssay", "[+] Thêm Tự Luận + Rubric", 15, 0, new Vector2(230, 40), ColorPurple);
            btnAddEssay.onClick.AddListener(() =>
            {
                OpenAddQuestionModal("ESSAY");
            });

            questionListContainer = CreateScrollListContainer(tabContentQuestions, "QuestionScrollList", new Vector2(0, -78), new Vector2(-40, -100));
            RefreshQuestionBankTab();
        }

        private void RefreshQuestionBankTab()
        {
            if (questionListContainer == null) return;

            for (int i = questionListContainer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(questionListContainer.transform.GetChild(i).gameObject);
            }

            var qList = TeacherConfigManager.Instance.GetAllQuestions(6, "ALL");

            if (questionCountLabel != null)
            {
                questionCountLabel.text = $"<b>NGÂN HÀNG ĐỀ & RUBRIC ({qList.Count} câu hỏi trong Database):</b>";
            }

            // Nếu chưa có câu hỏi nào trong RAM, tự động nạp từ Google Sheets Database
            if (qList.Count == 0)
            {
                TeacherConfigManager.Instance.SyncFromDatabase((ok, msg) =>
                {
                    RefreshQuestionBankTab();
                });
                return;
            }

            foreach (var q in qList)
            {
                bool isDb = TeacherConfigManager.Instance.DatabaseQuestions.Exists(x => x.questionId == q.questionId);
                string sourceBadge = isDb ? "<color=#38BDF8>[DATABASE GOOGLE SHEETS]</color>" : "<color=#34D399>[GIÁO VIÊN SOẠN]</color>";

                string choicesOrRubric = (q.type == "MCQ") 
                    ? $"A. {q.optA}  |  B. {q.optB}  |  C. {q.optC}  |  D. {q.optD}" 
                    : (!string.IsNullOrEmpty(q.rubricSummary) ? $"Barem Rubric: {q.rubricSummary}" : "Barem chấm tự động theo Gemini AI");

                CreateQuestionPreviewRow(questionListContainer, q, choicesOrRubric, sourceBadge);
            }

            StartCoroutine(ResetScrollToTopRoutine(questionListContainer));
        }

        private void CreateQuestionPreviewRow(GameObject parent, TeacherConfigManager.TeacherQuestionConfig q, string choicesOrRubric, string sourceBadge = "")
        {
            var row = new GameObject($"QRow_{q.questionId}", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent.transform, false);
            var rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 115);
            row.GetComponent<Image>().color = q.type == "MCQ" ? ColorCardBg : new Color(0.14f, 0.12f, 0.22f, 1f);

            string badgePrefix = !string.IsNullOrEmpty(sourceBadge) ? $"{sourceBadge} " : "";
            CreateLeftText(row, "QPrompt", $"{badgePrefix}<b>[{q.type}] {q.prompt}</b>  <color=#FBBF24>[{q.topic}]</color>  <color=#94A3B8>({q.questionId})</color>", 35, 28, 1050, 15, ColorTextTitle);
            CreateLeftText(row, "QOpts", choicesOrRubric, 35, -6, 1050, 14, q.type == "MCQ" ? new Color(0.6f, 0.85f, 0.7f) : new Color(0.85f, 0.7f, 0.95f));
            CreateLeftText(row, "QExp", $"Giải thích / Đáp án: {q.explanation}", 35, -34, 1050, 13, ColorTextMuted);

            var btnEdit = CreateRightButton(row, "Btn_Edit", "Chỉnh Sửa", 140, 0, new Vector2(110, 44), ColorPrimaryBlue);
            btnEdit.onClick.AddListener(() =>
            {
                OpenEditQuestionModal(q);
            });

            var btnDel = CreateRightButton(row, "Btn_Del", "Xóa [X]", 25, 0, new Vector2(105, 44), ColorCoralRed);
            btnDel.onClick.AddListener(() =>
            {
                TeacherConfigManager.Instance.DeleteQuestion(q.questionId);
                Destroy(row);
                ShowToast($"Đã xóa câu hỏi {q.questionId} khỏi danh sách!");
            });
        }
        #endregion

        #region Tab 4: AI Companion Tuning
        private void BuildAIConfigTab(GameObject parent)
        {
            tabContentAI = CreateTabContentContainer(parent, "TabContent_AI");

            var panel = new GameObject("AIPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(tabContentAI.transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(20, 20);
            rt.offsetMax = new Vector2(-20, -20);
            panel.GetComponent<Image>().color = ColorCardBg;

            CreateLeftText(panel, "Title", "<b>TINH CHỈNH GIA SƯ AI THẦY MINH & BÉ AI SCOUT BOT</b>", 40, 260, 1200, 22, ColorAmberGold);
            CreateLeftText(panel, "Desc", "Giáo viên có thể điều chỉnh phong cách sư phạm, chọn mô hình Gemini API và tùy biến lời thoại giảng dạy.", 40, 220, 1200, 15, ColorTextMuted);

            // 1. Phong cách Sư phạm (Pedagogy Mode)
            CreateLeftText(panel, "PedLbl", "1. Phong cách giảng dạy sư phạm:", 40, 160, 600, 16, ColorTextTitle);
            string[] modes = { "SOCRATIC", "ENCOURAGING", "STRICT_RUBRIC" };
            string[] modeLabels = { "Socratic (Hỏi gợi mở từng bước)", "Ân cần động viên (Tâm lý, khích lệ)", "Nghiêm khắc học thuật (Bám sát barem)" };
            for (int i = 0; i < modes.Length; i++)
            {
                string m = modes[i];
                var btn = CreateLeftButton(panel, $"Btn_Mode_{m}", modeLabels[i], 40 + i * 390, 110, new Vector2(370, 46),
                    TeacherConfigManager.Instance.AIPedagogyMode == m ? ColorEmeraldGreen : ColorBorderLine);
                btn.onClick.AddListener(() =>
                {
                    TeacherConfigManager.Instance.SetAIPedagogyMode(m);
                    ShowToast($"Đã đổi phong cách sư phạm sang: {m}!");
                });
            }

            // 2. Chọn Model Gemini
            CreateLeftText(panel, "ModelLbl", "2. Chọn mô hình Gemini AI:", 40, 40, 600, 16, ColorTextTitle);
            string[] aiModels = { "models/gemini-2.0-flash", "models/gemini-1.5-flash" };
            string[] modelLabels = { "Gemini 2.0 Flash (Chuẩn thị giác Multimodal - Khuyên dùng)", "Gemini 1.5 Flash (Dự phòng ổn định cao)" };
            for (int i = 0; i < aiModels.Length; i++)
            {
                string mdl = aiModels[i];
                var btn = CreateLeftButton(panel, $"Btn_Mdl_{i}", modelLabels[i], 40 + i * 580, -10, new Vector2(560, 46),
                    TeacherConfigManager.Instance.AIGeminiModel == mdl ? ColorPrimaryBlue : ColorBorderLine);
                btn.onClick.AddListener(() =>
                {
                    TeacherConfigManager.Instance.SetAIGeminiModel(mdl);
                    ShowToast($"Đã chọn mô hình: {mdl}!");
                });
            }

            // 3. API Key & Prompt Preview
            CreateLeftText(panel, "KeyLbl", "3. Khóa Google Gemini API Key (Bắt buộc để AI nhìn ảnh bài tập thật & nhận diện ảnh con vật / rác):", 40, -80, 900, 16, ColorTextTitle);
            
            string currentKey = PlayerPrefs.GetString("GEMINI_API_KEY", "");
            var keyInput = CreateInputField(panel, "InputApiKey", "Dán khóa Gemini API Key vào đây (bắt đầu bằng AIzaSy...)...", new Vector2(-170, -130), new Vector2(780, 44));
            if (keyInput != null && !string.IsNullOrEmpty(currentKey))
            {
                keyInput.text = currentKey;
            }

            var btnKey = CreateLeftButton(panel, "Btn_SetKey", "💾 Lưu & Kích Hoạt AI Vision", 830, -130, new Vector2(330, 44), ColorEmeraldGreen);
            
            string statusStr = !string.IsNullOrEmpty(currentKey) 
                ? "<color=#2ecc71>● Đã kết nối Gemini API Key: Gia sư AI Gemini Vision đã sẵn sàng 'nhìn' ảnh bài giải.</color>" 
                : "<color=#f39c12>▲ Chưa có API Key: Đang ở chế độ Mô phỏng ngoại tuyến (Chỉ chấm văn bản, ảnh không liên quan sẽ chấm 0đ).</color>";
            var keyStatus = CreateLeftText(panel, "KeyStatus", statusStr, 40, -165, 1100, 13, ColorTextMuted);

            btnKey.onClick.AddListener(() =>
            {
                string entered = keyInput != null ? keyInput.text.Trim() : "";
                if (!string.IsNullOrEmpty(entered))
                {
                    PlayerPrefs.SetString("GEMINI_API_KEY", entered);
                    PlayerPrefs.Save();
                    if (Math6Companion.Core.Services.GeminiAITutorService.Instance != null)
                    {
                        Math6Companion.Core.Services.GeminiAITutorService.Instance.ApiKey = entered;
                    }
                    if (keyStatus != null)
                    {
                        keyStatus.text = "<color=#2ecc71>● Đã kích hoạt Gemini API Key! Gia sư AI Gemini Vision đã sẵn sàng 'nhìn' ảnh bài giải.</color>";
                    }
                    ShowToast("Đã lưu khóa Gemini API Key thành công!");
                }
                else
                {
                    PlayerPrefs.DeleteKey("GEMINI_API_KEY");
                    PlayerPrefs.Save();
                    if (Math6Companion.Core.Services.GeminiAITutorService.Instance != null)
                    {
                        Math6Companion.Core.Services.GeminiAITutorService.Instance.ApiKey = "";
                    }
                    if (keyStatus != null)
                    {
                        keyStatus.text = "<color=#f39c12>▲ Chưa có API Key: Đang ở chế độ Mô phỏng ngoại tuyến.</color>";
                    }
                    ShowToast("Đã xóa API Key! Hệ thống chuyển về chế độ Mock ngoại tuyến.");
                }
            });

            // Prompt Preview Card at Bottom
            var promptBox = new GameObject("PromptBox", typeof(RectTransform), typeof(Image));
            promptBox.transform.SetParent(panel.transform, false);
            var pbRt = promptBox.GetComponent<RectTransform>();
            pbRt.anchorMin = new Vector2(0f, 0f);
            pbRt.anchorMax = new Vector2(1f, 0f);
            pbRt.pivot = new Vector2(0.5f, 0f);
            pbRt.anchoredPosition = new Vector2(0, 20);
            pbRt.sizeDelta = new Vector2(-80, 90);
            promptBox.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.14f, 1f);

            CreateLeftText(promptBox, "PBTitle", "<i>SYSTEM PROMPT ĐANG ÁP DỤNG CHO THẦY MINH:</i>", 20, 22, 1000, 13, ColorAmberGold);
            CreateLeftText(promptBox, "PBText", "\"Bạn là Thầy Minh - Gia sư AI dạy Toán THCS chuẩn Bộ GD&ĐT Việt Nam. Hãy phân tích lỗi sai của học sinh bằng phương pháp Socratic, không nói ngay đáp án, đưa ra gợi ý từng bước và bài tập tương tự.\"",
                20, -12, 1200, 13, new Color(0.7f, 0.85f, 0.95f));
        }
        #endregion

        #region Tab 5: Game & System Configuration
        private void BuildSystemConfigTab(GameObject parent)
        {
            tabContentSystem = CreateTabContentContainer(parent, "TabContent_System");

            var panel = new GameObject("SysPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(tabContentSystem.transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(20, 20);
            rt.offsetMax = new Vector2(-20, -20);
            panel.GetComponent<Image>().color = ColorCardBg;

            CreateLeftText(panel, "Title", "<b>CẤU HÌNH LUẬT CHƠI, THỜI GIAN & HẠ TẦNG KỸ THUẬT</b>", 40, 260, 1200, 22, ColorAmberGold);

            // 1. Thời gian Đối Kháng PvP
            CreateLeftText(panel, "PvPLbl", "1. Thời lượng thi đấu đối kháng (Versus Mode):", 40, 200, 600, 16, ColorTextTitle);
            int[] pvpTimes = { 90, 120, 180, 240, 300 };
            for (int i = 0; i < pvpTimes.Length; i++)
            {
                int t = pvpTimes[i];
                var btn = CreateLeftButton(panel, $"Btn_PvP_{t}", $"{t / 60} Phút {t % 60}s", 40 + i * 195, 155, new Vector2(180, 42),
                    TeacherConfigManager.Instance.PvPDurationSeconds == t ? ColorAmberGold : ColorBorderLine);
                btn.onClick.AddListener(() =>
                {
                    TeacherConfigManager.Instance.SetPvPDuration(t);
                    ShowToast($"Đã đặt thời gian thi đấu đối kháng thành: {t} giây!");
                });
            }

            // 2. Số lượng câu hỏi mỗi phiên trắc nghiệm
            CreateLeftText(panel, "QuizLbl", "2. Số lượng câu hỏi mỗi lượt Luyện Trắc Nghiệm:", 40, 105, 600, 16, ColorTextTitle);
            int[] quizCounts = { 5, 10, 15, 20 };
            for (int i = 0; i < quizCounts.Length; i++)
            {
                int q = quizCounts[i];
                var btn = CreateLeftButton(panel, $"Btn_Quiz_{q}", $"{q} Câu Hỏi", 40 + i * 195, 65, new Vector2(180, 42),
                    TeacherConfigManager.Instance.QuizQuestionCount == q ? ColorEmeraldGreen : ColorBorderLine);
                btn.onClick.AddListener(() =>
                {
                    TeacherConfigManager.Instance.SetQuizQuestionCount(q);
                    ShowToast($"Đã đặt số câu hỏi luyện tập thành: {q} câu!");
                });
            }

            // 3. Cấu hình Giới hạn thời lượng phiên học của học sinh (Screen Time)
            CreateLeftText(panel, "SessionLimitLbl", "3. Giới hạn thời lượng phiên học (Screen Time / Bảo vệ thị lực):", 40, 15, 700, 16, ColorTextTitle);
            int[] sessionOptions = { 1, 15, 20, 30, 45, 60 };
            string[] sessionLabels = { "1 Phút (TEST)", "15 Phút", "20 Phút", "30 Phút", "45 Phút", "60 Phút" };
            for (int i = 0; i < sessionOptions.Length; i++)
            {
                int sMin = sessionOptions[i];
                bool isCur = PlaySessionManager.Instance != null && PlaySessionManager.Instance.MaxContinuousMinutes == sMin;
                var btn = CreateLeftButton(panel, $"Btn_Session_{sMin}", sessionLabels[i], 40 + i * 190, -25, new Vector2(180, 42),
                    isCur ? (sMin == 1 ? ColorCoralRed : ColorAmberGold) : ColorBorderLine);
                btn.onClick.AddListener(() =>
                {
                    if (PlaySessionManager.Instance != null)
                    {
                        PlaySessionManager.Instance.UpdateSettings(true, sMin, PlaySessionManager.Instance.MaxDailyMinutes, PlaySessionManager.Instance.BreakDurationMinutes);
                    }
                    ShowToast($"Đã đặt giới hạn phiên học: {sMin} Phút {(sMin == 1 ? "(Chế độ KIỂM THỬ NHANH 60s)" : "")}!");
                    SwitchTab(4); // Refresh lại tab
                });
            }

            // 4. Backend Google Apps Script Web App URL
            CreateLeftText(panel, "BackLbl", "4. Đường dẫn Backend Google Sheets Web App REST API:", 40, -80, 600, 16, ColorTextTitle);
            CreateLeftText(panel, "BackUrl", AppConfig.GetActiveBackendUrl(), 40, -110, 1200, 13, new Color(0.4f, 0.8f, 1f));

            // Bottom Actions Centered (-400, 0, +400)
            var btnSaveAll = CreateCenteredButton(panel, "Btn_SaveAll", "[LƯU] TOÀN BỘ CẤU HÌNH", -400, -165, new Vector2(360, 52), ColorEmeraldGreen);
            btnSaveAll.onClick.AddListener(() =>
            {
                TeacherConfigManager.Instance.SaveConfiguration();
                ShowToast("ĐÃ LƯU TẤT CẢ THAY ĐỔI VÀO HỆ THỐNG THÀNH CÔNG!");
            });

            var btnSyncSheets = CreateCenteredButton(panel, "Btn_SyncSheets", "[ĐỒNG BỘ] GOOGLE SHEETS", 0, -165, new Vector2(360, 52), ColorPrimaryBlue);
            btnSyncSheets.onClick.AddListener(() =>
            {
                Math6Companion.Core.Services.GoogleSheetsNetworkService.InvalidateCache();
                TeacherConfigManager.Instance.SyncFromDatabase((ok, msg) =>
                {
                    ShowToast(msg);
                });
            });

            var btnReset = CreateCenteredButton(panel, "Btn_Reset", "[RESET] KHÔI PHỤC MẶC ĐỊNH", 400, -165, new Vector2(360, 52), ColorCoralRed);
            btnReset.onClick.AddListener(() =>
            {
                TeacherConfigManager.Instance.ResetToDefaults();
                ShowToast("Đã đặt lại toàn bộ thông số về cấu hình chuẩn!");
                SwitchTab(currentTabIndex);
            });
        }
        #endregion

        #region Teacher PIN Verification Modal
        private void BuildTeacherPinModal(GameObject parent)
        {
            pinModalGo = new GameObject("TeacherPinModal", typeof(RectTransform), typeof(Image));
            pinModalGo.transform.SetParent(parent.transform, false);
            var mRt = pinModalGo.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.5f, 0.5f);
            mRt.anchorMax = new Vector2(0.5f, 0.5f);
            mRt.pivot = new Vector2(0.5f, 0.5f);
            mRt.sizeDelta = new Vector2(620, 420);
            pinModalGo.GetComponent<Image>().color = ColorWindowBg;

            // Modal Header
            var hdr = new GameObject("ModalHdr", typeof(RectTransform), typeof(Image));
            hdr.transform.SetParent(pinModalGo.transform, false);
            var hRt = hdr.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.sizeDelta = new Vector2(0, 65);
            hdr.GetComponent<Image>().color = ColorHeaderBg;

            CreateLeftText(hdr, "Title", "<b>XÁC THỰC QUYỀN GIÁO VIÊN</b>", 25, 0, 450, 18, ColorAmberGold);
            var btnClose = CreateRightButton(hdr, "Btn_ClosePin", "[X]", 15, 0, new Vector2(40, 40), ColorCoralRed);
            btnClose.onClick.AddListener(HideAll);

            // Modal Body Description
            CreateCenteredText(pinModalGo, "Desc", "Khu vực này dành riêng cho Giáo viên bộ môn & Quản trị viên.\nHọc sinh không được phép truy cập tùy chỉnh cấu hình.\n\nVui lòng nhập Mã PIN Giáo Viên (Mặc định: 1234):", 0, 60, new Vector2(560, 90), 14, ColorTextMuted);

            // PIN InputField
            var inputGo = new GameObject("PinInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(pinModalGo.transform, false);
            var iRt = inputGo.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.5f, 0.5f);
            iRt.anchorMax = new Vector2(0.5f, 0.5f);
            iRt.pivot = new Vector2(0.5f, 0.5f);
            iRt.anchoredPosition = new Vector2(0, -15);
            iRt.sizeDelta = new Vector2(300, 52);
            inputGo.GetComponent<Image>().color = ColorCardBg;

            var txtArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            txtArea.transform.SetParent(inputGo.transform, false);
            var taRt = txtArea.GetComponent<RectTransform>();
            taRt.anchorMin = Vector2.zero;
            taRt.anchorMax = Vector2.one;
            taRt.offsetMin = new Vector2(15, 5);
            taRt.offsetMax = new Vector2(-15, -5);

            var inpTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            inpTxt.transform.SetParent(txtArea.transform, false);
            var itRt = inpTxt.GetComponent<RectTransform>();
            itRt.anchorMin = Vector2.zero;
            itRt.anchorMax = Vector2.one;
            var t = inpTxt.GetComponent<TextMeshProUGUI>();
            t.fontSize = 22;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;

            pinInputField = inputGo.GetComponent<TMP_InputField>();
            pinInputField.textViewport = taRt;
            pinInputField.textComponent = t;
            pinInputField.contentType = TMP_InputField.ContentType.Password;
            pinInputField.characterLimit = 8;
            pinInputField.onSubmit.AddListener(val => OnVerifyPinSubmit());

            // Error Text
            var errGo = new GameObject("ErrorTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
            errGo.transform.SetParent(pinModalGo.transform, false);
            var eRt = errGo.GetComponent<RectTransform>();
            eRt.anchorMin = new Vector2(0.5f, 0.5f);
            eRt.anchorMax = new Vector2(0.5f, 0.5f);
            eRt.pivot = new Vector2(0.5f, 0.5f);
            eRt.anchoredPosition = new Vector2(0, -60);
            eRt.sizeDelta = new Vector2(500, 30);
            pinErrorText = errGo.GetComponent<TextMeshProUGUI>();
            pinErrorText.fontSize = 13;
            pinErrorText.alignment = TextAlignmentOptions.Center;
            pinErrorText.color = ColorCoralRed;

            // Submit & Cancel Buttons
            var btnSubmit = CreateCenteredButton(pinModalGo, "Btn_Submit", "Xác Thực Quản Trị", -110, -125, new Vector2(200, 48), ColorPrimaryBlue);
            btnSubmit.onClick.AddListener(OnVerifyPinSubmit);

            var btnCancel = CreateCenteredButton(pinModalGo, "Btn_Cancel", "Hủy Bỏ", 110, -125, new Vector2(200, 48), ColorBorderLine);
            btnCancel.onClick.AddListener(HideAll);

            pinModalGo.SetActive(false);
        }

        private void BuildEditLessonModal(GameObject parent)
        {
            editLessonModalGo = new GameObject("EditLessonModal", typeof(RectTransform), typeof(Image));
            editLessonModalGo.transform.SetParent(parent.transform, false);
            var mRt = editLessonModalGo.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.5f, 0.5f);
            mRt.anchorMax = new Vector2(0.5f, 0.5f);
            mRt.pivot = new Vector2(0.5f, 0.5f);
            mRt.sizeDelta = new Vector2(780, 560);
            editLessonModalGo.GetComponent<Image>().color = ColorWindowBg;

            // Modal Header
            var hdr = new GameObject("ModalHdr", typeof(RectTransform), typeof(Image));
            hdr.transform.SetParent(editLessonModalGo.transform, false);
            var hRt = hdr.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.sizeDelta = new Vector2(0, 60);
            hdr.GetComponent<Image>().color = ColorHeaderBg;

            CreateLeftText(hdr, "Title", "<b>CHỈNH SỬA THÔNG TIN BÀI HỌC (TOÁN LỚP 6)</b>", 25, 0, 550, 18, ColorAmberGold);
            var btnClose = CreateRightButton(hdr, "Btn_CloseEdit", "[X]", 15, 0, new Vector2(40, 40), ColorCoralRed);
            btnClose.onClick.AddListener(() => editLessonModalGo.SetActive(false));

            // Form inputs
            // 1. Title
            CreateLeftText(editLessonModalGo, "LblTitle", "Tên bài học (Title):", 40, 185, 300, 14, ColorTextTitle);
            editLessonTitleInput = CreateInputField(editLessonModalGo, "InputTitle", "Nhập tên bài học...", new Vector2(0, 150), new Vector2(700, 42));

            // 2. Topic
            CreateLeftText(editLessonModalGo, "LblTopic", "Chuyên đề (Topic):", 40, 110, 300, 14, ColorTextTitle);
            editLessonTopicInput = CreateInputField(editLessonModalGo, "InputTopic", "Nhập chuyên đề (Số nguyên, Phân số, Hình học...)", new Vector2(0, 75), new Vector2(700, 42));

            // 3. Summary (Multiline)
            CreateLeftText(editLessonModalGo, "LblSummary", "Tóm tắt lý thuyết cốt lõi (Summary):", 40, 35, 300, 14, ColorTextTitle);
            editLessonSummaryInput = CreateInputField(editLessonModalGo, "InputSummary", "Nhập tóm tắt công thức, định nghĩa...", new Vector2(0, -35), new Vector2(700, 85), true);

            // 4. Video URL
            CreateLeftText(editLessonModalGo, "LblVideo", "Đường dẫn Video bài giảng (Video URL):", 40, -100, 300, 14, ColorTextTitle);
            editLessonVideoInput = CreateInputField(editLessonModalGo, "InputVideo", "https://youtube.com/...", new Vector2(0, -135), new Vector2(700, 42));

            // Submit & Cancel Buttons
            var btnSave = CreateCenteredButton(editLessonModalGo, "Btn_SaveLesson", "[LƯU THAY ĐỔI]", -140, -215, new Vector2(240, 48), ColorEmeraldGreen);
            btnSave.onClick.AddListener(OnSaveEditLessonSubmit);

            var btnCancel = CreateCenteredButton(editLessonModalGo, "Btn_CancelLesson", "HỦY BỎ", 140, -215, new Vector2(240, 48), ColorBorderLine);
            btnCancel.onClick.AddListener(() => editLessonModalGo.SetActive(false));

            editLessonModalGo.SetActive(false);
        }

        private void OpenEditLessonModal(TeacherConfigManager.TeacherLessonConfig les)
        {
            if (editLessonModalGo == null) return;
            isCreatingNewLesson = false;
            currentEditingLessonId = les.lessonId;
            if (editLessonTitleInput != null) editLessonTitleInput.text = les.title;
            if (editLessonTopicInput != null) editLessonTopicInput.text = les.topic;
            if (editLessonSummaryInput != null) editLessonSummaryInput.text = les.summaryText;
            if (editLessonVideoInput != null) editLessonVideoInput.text = les.videoUrl;
            editLessonModalGo.transform.SetAsLastSibling();
            editLessonModalGo.SetActive(true);
        }

        private void OpenCreateLessonModal()
        {
            if (editLessonModalGo == null) return;
            isCreatingNewLesson = true;
            currentEditingLessonId = null;
            int nextIdx = TeacherConfigManager.Instance.GetLessonsForGrade(6).Count + 1;
            if (editLessonTitleInput != null) editLessonTitleInput.text = $"Bài {nextIdx}: Chuyên đề mới";
            if (editLessonTopicInput != null) editLessonTopicInput.text = "Số học 6";
            if (editLessonSummaryInput != null) editLessonSummaryInput.text = "• Lý thuyết cốt lõi cần ghi nhớ...";
            if (editLessonVideoInput != null) editLessonVideoInput.text = "https://youtube.com/mock_video";
            editLessonModalGo.transform.SetAsLastSibling();
            editLessonModalGo.SetActive(true);
        }

        private void OnSaveEditLessonSubmit()
        {
            string title = editLessonTitleInput != null ? editLessonTitleInput.text.Trim() : "";
            string topic = editLessonTopicInput != null ? editLessonTopicInput.text.Trim() : "Toán 6";
            string summary = editLessonSummaryInput != null ? editLessonSummaryInput.text.Trim() : "";
            string video = editLessonVideoInput != null ? editLessonVideoInput.text.Trim() : "";

            if (string.IsNullOrEmpty(title))
            {
                ShowToast("Tên bài học không được để trống!");
                return;
            }

            if (isCreatingNewLesson)
            {
                TeacherConfigManager.Instance.AddNewLesson(6, topic, title, summary, video);
                ShowToast($"Đã tạo mới bài học '{title}'!");
            }
            else
            {
                TeacherConfigManager.Instance.UpdateLesson(currentEditingLessonId, title, topic, summary, video);
                ShowToast($"Đã lưu thay đổi cho bài học '{title}'!");
            }

            if (editLessonModalGo != null) editLessonModalGo.SetActive(false);
            RefreshCurriculumTab();
        }

        private TMP_InputField CreateInputField(GameObject parent, string name, string placeholder, Vector2 pos, Vector2 size, bool multiLine = false)
        {
            var inputGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(parent.transform, false);
            var iRt = inputGo.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.5f, 0.5f);
            iRt.anchorMax = new Vector2(0.5f, 0.5f);
            iRt.pivot = new Vector2(0.5f, 0.5f);
            iRt.anchoredPosition = pos;
            iRt.sizeDelta = size;
            inputGo.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.14f, 1f);

            var txtArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            txtArea.transform.SetParent(inputGo.transform, false);
            var taRt = txtArea.GetComponent<RectTransform>();
            taRt.anchorMin = Vector2.zero;
            taRt.anchorMax = Vector2.one;
            taRt.offsetMin = new Vector2(12, 6);
            taRt.offsetMax = new Vector2(-12, -6);

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phGo.transform.SetParent(txtArea.transform, false);
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = Vector2.zero;
            phRt.offsetMax = Vector2.zero;
            var phText = phGo.GetComponent<TextMeshProUGUI>();
            phText.text = placeholder;
            phText.fontSize = multiLine ? 14 : 15;
            phText.color = new Color(0.45f, 0.52f, 0.65f, 0.8f);
            phText.alignment = multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;

            var inpTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            inpTxt.transform.SetParent(txtArea.transform, false);
            var itRt = inpTxt.GetComponent<RectTransform>();
            itRt.anchorMin = Vector2.zero;
            itRt.anchorMax = Vector2.one;
            itRt.offsetMin = Vector2.zero;
            itRt.offsetMax = Vector2.zero;
            var t = inpTxt.GetComponent<TextMeshProUGUI>();
            t.fontSize = multiLine ? 14 : 15;
            t.color = Color.white;
            t.alignment = multiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;

            var img = inputGo.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            if (phText != null) phText.raycastTarget = false;
            if (t != null) t.raycastTarget = false;

            var input = inputGo.GetComponent<TMP_InputField>();
            input.textViewport = taRt;
            input.textComponent = t;
            input.placeholder = phText;
            input.targetGraphic = img;
            input.interactable = true;
            if (multiLine)
            {
                input.lineType = TMP_InputField.LineType.MultiLineNewline;
            }
            return input;
        }
        #endregion

        #region Footer & Helpers
        private void BuildFooter(GameObject parent)
        {
            var footer = new GameObject("FooterBar", typeof(RectTransform), typeof(Image));
            footer.transform.SetParent(parent.transform, false);
            var rt = footer.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, 48);
            footer.GetComponent<Image>().color = ColorHeaderBg;

            toastText = CreateLeftText(footer, "ToastMsg", "", 30, 0, 900, 15, ColorAmberGold);
            toastText.fontStyle = FontStyles.Bold;

            CreateRightText(footer, "Tip", "Phím tắt: Bấm <b>F2</b> hoặc <b>Alt+T</b> để mở bảng điều khiển giáo viên.", 30, 0, 650, 13, ColorTextMuted);
        }

        private GameObject CreateTabContentContainer(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0, 48);
            rt.offsetMax = new Vector2(0, -141);
            return go;
        }

        private GameObject CreateScrollListContainer(GameObject parent, string name, Vector2 pos, Vector2 size)
        {
            var scrollRoot = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollRoot.transform.SetParent(parent.transform, false);
            var sRt = scrollRoot.GetComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(20, 20);
            sRt.offsetMax = new Vector2(-20, -78);
            scrollRoot.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.9f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scrollRoot.transform, false);
            var vRt = viewport.GetComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero;
            vRt.anchorMax = Vector2.one;
            vRt.offsetMin = Vector2.zero;
            vRt.offsetMax = Vector2.zero;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0f, 1f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            cRt.anchoredPosition = Vector2.zero;

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(20, 20, 15, 15);
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scrollRoot.GetComponent<ScrollRect>();
            sr.content = cRt;
            sr.viewport = vRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 25f;

            return content;
        }

        private TextMeshProUGUI CreateLeftText(GameObject parent, string name, string text, float xFromLeft, float yFromCenter, float width, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(xFromLeft, yFromCenter);
            rt.sizeDelta = new Vector2(width, 32);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            return t;
        }

        private TextMeshProUGUI CreateRightText(GameObject parent, string name, string text, float xFromRight, float yFromCenter, float width, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-xFromRight, yFromCenter);
            rt.sizeDelta = new Vector2(width, 32);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.MidlineRight;
            return t;
        }

        private TextMeshProUGUI CreateCenteredText(GameObject parent, string name, string text, float xFromCenter, float yFromCenter, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(xFromCenter, yFromCenter);
            rt.sizeDelta = size;
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            return t;
        }

        private Button CreateLeftButton(GameObject parent, string name, string label, float xFromLeft, float yFromCenter, Vector2 size, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(xFromLeft, yFromCenter);
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = bgColor;

            var txtGo = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var tRt = txtGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            var t = txtGo.GetComponent<TextMeshProUGUI>();
            t.text = label;
            t.fontSize = 14;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;

            return go.GetComponent<Button>();
        }

        private Button CreateRightButton(GameObject parent, string name, string label, float xFromRight, float yFromCenter, Vector2 size, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-xFromRight, yFromCenter);
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = bgColor;

            var txtGo = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var tRt = txtGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            var t = txtGo.GetComponent<TextMeshProUGUI>();
            t.text = label;
            t.fontSize = 14;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;

            return go.GetComponent<Button>();
        }

        private Button CreateCenteredButton(GameObject parent, string name, string label, float xFromCenter, float yFromCenter, Vector2 size, Color bgColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(xFromCenter, yFromCenter);
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = bgColor;

            var txtGo = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(go.transform, false);
            var tRt = txtGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            var t = txtGo.GetComponent<TextMeshProUGUI>();
            t.text = label;
            t.fontSize = 15;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;

            return go.GetComponent<Button>();
        }
        #endregion
        #region Edit Question Modal Dialog
        private void BuildEditQuestionModal(GameObject parent)
        {
            editQuestionModalGo = new GameObject("EditQuestionModal", typeof(RectTransform), typeof(Image));
            editQuestionModalGo.transform.SetParent(parent.transform, false);
            var mRt = editQuestionModalGo.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.5f, 0.5f);
            mRt.anchorMax = new Vector2(0.5f, 0.5f);
            mRt.pivot = new Vector2(0.5f, 0.5f);
            mRt.sizeDelta = new Vector2(900, 680);
            editQuestionModalGo.GetComponent<Image>().color = ColorWindowBg;

            // Modal Header
            var hdr = new GameObject("ModalHdr", typeof(RectTransform), typeof(Image));
            hdr.transform.SetParent(editQuestionModalGo.transform, false);
            var hRt = hdr.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.sizeDelta = new Vector2(0, 60);
            hdr.GetComponent<Image>().color = ColorHeaderBg;

            editQHeaderTitle = CreateLeftText(hdr, "Title", "<b>CHỈNH SỬA CÂU HỎI & ĐÁP ÁN</b>", 25, 0, 600, 18, ColorAmberGold);
            var btnClose = CreateRightButton(hdr, "Btn_CloseEditQ", "[X]", 15, 0, new Vector2(40, 40), ColorCoralRed);
            btnClose.onClick.AddListener(() => editQuestionModalGo.SetActive(false));

            // 1. Prompt
            CreateLeftText(editQuestionModalGo, "LblPrompt", "Đề bài / Câu hỏi:", 40, 245, 300, 14, ColorTextTitle);
            editQPromptInput = CreateInputField(editQuestionModalGo, "InputPrompt", "Nhập nội dung câu hỏi...", new Vector2(0, 195), new Vector2(820, 70), true);

            // 2. Topic
            CreateLeftText(editQuestionModalGo, "LblTopic", "Chuyên đề (Topic):", 40, 135, 300, 14, ColorTextTitle);
            editQTopicInput = CreateInputField(editQuestionModalGo, "InputTopic", "SoNguyen / PhanSo / TapHop...", new Vector2(-205, 100), new Vector2(410, 40));

            // MCQ Group
            editQMcqGroup = new GameObject("MCQGroup", typeof(RectTransform));
            editQMcqGroup.transform.SetParent(editQuestionModalGo.transform, false);
            var mcqRt = editQMcqGroup.GetComponent<RectTransform>();
            mcqRt.anchorMin = Vector2.zero;
            mcqRt.anchorMax = Vector2.one;
            mcqRt.offsetMin = Vector2.zero;
            mcqRt.offsetMax = Vector2.zero;

            CreateLeftText(editQMcqGroup, "LblOptA", "Đáp án A:", 40, 50, 120, 13, ColorTextTitle);
            editQOptAInput = CreateInputField(editQMcqGroup, "InputA", "Lựa chọn A...", new Vector2(-205, 20), new Vector2(410, 36));

            CreateLeftText(editQMcqGroup, "LblOptB", "Đáp án B:", 470, 50, 120, 13, ColorTextTitle);
            editQOptBInput = CreateInputField(editQMcqGroup, "InputB", "Lựa chọn B...", new Vector2(215, 20), new Vector2(410, 36));

            CreateLeftText(editQMcqGroup, "LblOptC", "Đáp án C:", 40, -15, 120, 13, ColorTextTitle);
            editQOptCInput = CreateInputField(editQMcqGroup, "InputC", "Lựa chọn C...", new Vector2(-205, -45), new Vector2(410, 36));

            CreateLeftText(editQMcqGroup, "LblOptD", "Đáp án D:", 470, -15, 120, 13, ColorTextTitle);
            editQOptDInput = CreateInputField(editQMcqGroup, "InputD", "Lựa chọn D...", new Vector2(215, -45), new Vector2(410, 36));

            CreateLeftText(editQMcqGroup, "LblCorrect", "Đáp án đúng (A/B/C/D):", 40, -80, 200, 13, ColorAmberGold);
            editQCorrectInput = CreateInputField(editQMcqGroup, "InputCorrect", "A", new Vector2(-270, -110), new Vector2(280, 36));

            // Essay Group
            editQEssayGroup = new GameObject("EssayGroup", typeof(RectTransform));
            editQEssayGroup.transform.SetParent(editQuestionModalGo.transform, false);
            var essayRt = editQEssayGroup.GetComponent<RectTransform>();
            essayRt.anchorMin = Vector2.zero;
            essayRt.anchorMax = Vector2.one;
            essayRt.offsetMin = Vector2.zero;
            essayRt.offsetMax = Vector2.zero;

            CreateLeftText(editQEssayGroup, "LblRubric", "Barem Rubric chấm điểm tự luận (từng bước & thang điểm):", 40, 50, 600, 13, ColorAmberGold);
            editQRubricInput = CreateInputField(editQEssayGroup, "InputRubric", "B1: Tính trong ngoặc (+3đ)...", new Vector2(0, -35), new Vector2(820, 135), true);

            // 3. Explanation
            CreateLeftText(editQuestionModalGo, "LblExplain", "Lời giải chi tiết / Hướng dẫn giải:", 40, -150, 400, 13, ColorTextTitle);
            editQExplainInput = CreateInputField(editQuestionModalGo, "InputExplain", "Giải thích vì sao chọn đáp án này...", new Vector2(0, -195), new Vector2(820, 55), true);

            // Submit & Cancel Buttons
            var btnSave = CreateCenteredButton(editQuestionModalGo, "Btn_SaveQ", "[LƯU THAY ĐỔI]", -140, -280, new Vector2(240, 48), ColorEmeraldGreen);
            btnSave.onClick.AddListener(OnSaveEditQuestionSubmit);

            var btnCancel = CreateCenteredButton(editQuestionModalGo, "Btn_CancelQ", "HỦY BỎ", 140, -280, new Vector2(240, 48), ColorBorderLine);
            btnCancel.onClick.AddListener(() => editQuestionModalGo.SetActive(false));

            editQuestionModalGo.SetActive(false);
        }

        private void OpenAddQuestionModal(string type)
        {
            if (editQuestionModalGo == null) return;
            isAddingNewQuestion = true;
            currentEditingQuestionId = "";
            currentEditingQuestionType = type;

            bool isMCQ = type == "MCQ" || string.IsNullOrEmpty(type);
            if (editQHeaderTitle != null)
            {
                editQHeaderTitle.text = isMCQ
                    ? "<b>THÊM CÂU HỎI TRẮC NGHIỆM MỚI (TOÁN 6)</b>"
                    : "<b>THÊM ĐỀ BÀI TỰ LUẬN & BAREM RUBRIC MỚI (TOÁN 6)</b>";
            }

            if (editQPromptInput != null) editQPromptInput.text = "";
            if (editQTopicInput != null) editQTopicInput.text = isMCQ ? "Số nguyên" : "Thứ tự thực hiện phép tính";
            if (editQExplainInput != null) editQExplainInput.text = "";

            if (editQMcqGroup != null) editQMcqGroup.SetActive(isMCQ);
            if (editQEssayGroup != null) editQEssayGroup.SetActive(!isMCQ);

            if (isMCQ)
            {
                if (editQOptAInput != null) editQOptAInput.text = "";
                if (editQOptBInput != null) editQOptBInput.text = "";
                if (editQOptCInput != null) editQOptCInput.text = "";
                if (editQOptDInput != null) editQOptDInput.text = "";
                if (editQCorrectInput != null) editQCorrectInput.text = "A";
            }
            else
            {
                if (editQRubricInput != null)
                {
                    editQRubricInput.text = "Bước 1: Tính trong ngoặc (+4đ)\nBước 2: Nhân chia trước (+3đ)\nBước 3: Cộng trừ ra kết quả (+3đ)";
                }
            }

            Transform btnSaveTr = editQuestionModalGo.transform.Find("Btn_SaveQ");
            if (btnSaveTr != null)
            {
                var txt = btnSaveTr.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "[+] LƯU CÂU HỎI MỚI";
            }

            editQuestionModalGo.transform.SetAsLastSibling();
            editQuestionModalGo.SetActive(true);
        }

        private void OpenEditQuestionModal(TeacherConfigManager.TeacherQuestionConfig q)
        {
            if (editQuestionModalGo == null || q == null) return;
            isAddingNewQuestion = false;
            currentEditingQuestionId = q.questionId;
            currentEditingQuestionType = q.type;

            if (editQHeaderTitle != null) editQHeaderTitle.text = $"<b>CHỈNH SỬA CÂU HỎI: {q.questionId}</b> <color=#94A3B8>[{q.type}]</color>";
            if (editQPromptInput != null) editQPromptInput.text = q.prompt;
            if (editQTopicInput != null) editQTopicInput.text = q.topic;
            if (editQExplainInput != null) editQExplainInput.text = q.explanation;

            bool isMCQ = q.type == "MCQ" || string.IsNullOrEmpty(q.type);
            if (editQMcqGroup != null) editQMcqGroup.SetActive(isMCQ);
            if (editQEssayGroup != null) editQEssayGroup.SetActive(!isMCQ);

            if (isMCQ)
            {
                if (editQOptAInput != null) editQOptAInput.text = q.optA;
                if (editQOptBInput != null) editQOptBInput.text = q.optB;
                if (editQOptCInput != null) editQOptCInput.text = q.optC;
                if (editQOptDInput != null) editQOptDInput.text = q.optD;
                if (editQCorrectInput != null) editQCorrectInput.text = q.correctAnswer;
            }
            else
            {
                if (editQRubricInput != null) editQRubricInput.text = q.rubricSummary;
            }

            Transform btnSaveTr = editQuestionModalGo.transform.Find("Btn_SaveQ");
            if (btnSaveTr != null)
            {
                var txt = btnSaveTr.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "[LƯU THAY ĐỔI]";
            }

            editQuestionModalGo.transform.SetAsLastSibling();
            editQuestionModalGo.SetActive(true);
        }

        private void OnSaveEditQuestionSubmit()
        {
            string prompt = editQPromptInput != null ? editQPromptInput.text.Trim() : "";
            string topic = editQTopicInput != null ? editQTopicInput.text.Trim() : "Toán 6";
            string explain = editQExplainInput != null ? editQExplainInput.text.Trim() : "";

            if (string.IsNullOrEmpty(prompt))
            {
                ShowToast("Đề bài câu hỏi không được để trống!");
                return;
            }

            bool isMCQ = currentEditingQuestionType == "MCQ" || string.IsNullOrEmpty(currentEditingQuestionType);
            string optA = isMCQ && editQOptAInput != null ? editQOptAInput.text.Trim() : "";
            string optB = isMCQ && editQOptBInput != null ? editQOptBInput.text.Trim() : "";
            string optC = isMCQ && editQOptCInput != null ? editQOptCInput.text.Trim() : "";
            string optD = isMCQ && editQOptDInput != null ? editQOptDInput.text.Trim() : "";
            string correct = isMCQ && editQCorrectInput != null ? editQCorrectInput.text.Trim().ToUpper() : "";
            string rubric = !isMCQ && editQRubricInput != null ? editQRubricInput.text.Trim() : "";

            if (isAddingNewQuestion)
            {
                if (isMCQ)
                {
                    TeacherConfigManager.Instance.AddCustomMCQ(6, topic, prompt, optA, optB, optC, optD, correct, explain);
                    ShowToast("Đã thêm câu hỏi trắc nghiệm mới thành công vào đầu ngân hàng đề!");
                }
                else
                {
                    TeacherConfigManager.Instance.AddCustomEssay(6, topic, prompt, rubric, explain);
                    ShowToast("Đã thêm đề bài tự luận kèm Barem Rubric mới thành công!");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(currentEditingQuestionId)) return;

                bool ok = TeacherConfigManager.Instance.UpdateQuestion(
                    currentEditingQuestionId,
                    prompt,
                    topic,
                    currentEditingQuestionType,
                    optA,
                    optB,
                    optC,
                    optD,
                    correct,
                    explain,
                    rubric
                );

                if (ok)
                {
                    ShowToast($"Đã lưu thay đổi cho câu hỏi {currentEditingQuestionId}!");
                }
                else
                {
                    ShowToast($"Không thể cập nhật câu hỏi {currentEditingQuestionId}!");
                }
            }

            if (editQuestionModalGo != null) editQuestionModalGo.SetActive(false);
            RefreshQuestionBankTab();
        }
        #endregion
    }
}
