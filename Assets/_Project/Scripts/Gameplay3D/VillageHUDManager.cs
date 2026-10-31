using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core.Contracts;
using Math6Companion.Core.Services;

namespace Math6Companion.Gameplay3D
{
    /// <summary>
    /// Quản lý toàn bộ HUD trong Village World:
    /// - Thông tin học sinh (Sao, Xu)
    /// - Mở/đóng các panel tương tác (NoticeBoard, PhoShop, Pagoda, Terrace, AI Tutor)
    /// - Hệ thống kinh tế: Sao (từ trả lời đúng), Xu (từ hoàn thành mini-game, dùng mua gợi ý)
    /// - Fix cursor lock/unlock đúng thứ tự
    /// </summary>
    public class VillageHUDManager : MonoBehaviour
    {
        public static VillageHUDManager Instance { get; private set; }

        // ─── HUD fields — tên phải khớp chính xác với VillageWorldBuilder.SetPrivateField ───
        [SerializeField] private TextMeshProUGUI txtPlayerNick;
        [SerializeField] private TextMeshProUGUI txtStars;
        [SerializeField] private TextMeshProUGUI txtCoins;
        [SerializeField] private Button btnReturnMainMenu;

        // Các panel tương tác — gán qua SetPrivateField
        [SerializeField] private GameObject panelNoticeBoard;
        [SerializeField] private GameObject panelPhoMinigame;
        [SerializeField] private GameObject panelTerraceFields;
        [SerializeField] private GameObject panelPagodaSymmetry;
        [SerializeField] private GameObject panelAITutorChat;

        // AI Companion reference (tùy chọn — cho FloatingAICompanion)
        [SerializeField] private FloatingAICompanion aiCompanion;

        // ─── Economy ───
        private int _stars = 0;
        private int _coins = 50;   // khởi đầu với 50 xu
        private const int HINT_COST  = 10;   // 10 xu / gợi ý
        private const int COINS_PER_MINIGAME = 20; // hoàn thành mini-game
        private const int STARS_PER_CORRECT  = 2;  // trả lời đúng

        // ─── State ───
        private bool _anyModalOpen;
        private GameObject _activePanel;

        // ─── Minigame quiz state ───
        private int _phoCorrect;
        private int _terraceCorrect;
        private int _pagodaCorrect;

        private struct MinigameQuestion
        {
            public string prompt;
            public string[] opts;
            public int ans;
        }

        private List<MinigameQuestion> _currentQuestions = new List<MinigameQuestion>();
        private string _currentZoneTitle = "";
        private string _currentStoryContext = "";

        // ─── Fallback Quiz questions (kho câu hỏi chuyên đề phong phú bám sát Toán Lớp 6) ───
        private static readonly (string q, string[] opts, int ans)[] PHO_QUESTIONS =
        {
            ("Quán Cụ Ba có 6 tô phở chia đều cho 8 người. Phân số chỉ phần phở mỗi người nhận được là:",
             new[]{"3/4 tô","1/2 tô","2/3 tô","6/8 tô"},0),
            ("Tính tổng hai phân số cùng mẫu: 1/5 + 3/5 = ?",
             new[]{"4/5","4/10","2/5","3/10"},0),
            ("Rút gọn phân số 18/24 về dạng tối giản:",
             new[]{"3/4","6/8","9/12","2/3"},0),
            ("Tìm x biết rằng: x/15 = 2/5",
             new[]{"x = 6","x = 5","x = 10","x = 3"},0),
            ("Quy đồng mẫu số của 1/4 và 2/3 với mẫu số chung nhỏ nhất là bao nhiêu?",
             new[]{"Mẫu số chung là 12","Mẫu số chung là 24","Mẫu số chung là 7","Mẫu số chung là 16"},0),
            ("Tính: 3/5 * 10/9 = ?",
             new[]{"2/3","30/45","1/2","5/3"},0),
            ("Trong các phân số sau, phân số nào lớn hơn 1?",
             new[]{"7/5","4/5","8/8","2/3"},0),
            ("Nồi nước phở có 20 lít, cụ Ba đã bán hết 3/4 nồi. Số lít nước dùng đã bán là:",
             new[]{"15 lít","12 lít","16 lít","10 lít"},0),
            ("Tính: 7/12 - 1/4 = ?",
             new[]{"1/3","6/8","1/2","5/12"},0),
            ("Nghịch đảo của phân số -3/7 là:",
             new[]{"-7/3","7/3","3/7","-3/7"},0)
        };

        private static readonly (string q, string[] opts, int ans)[] TERRACE_QUESTIONS =
        {
            ("Số nào sau đây là số nguyên âm?",
             new[]{"-5","0","7","+3"},0),
            ("Giá trị tuyệt đối của |-12| bằng bao nhiêu?",
             new[]{"12","-12","0","24"},0),
            ("Tính: (-8) + 5 = ?",
             new[]{"-3","3","-13","13"},0),
            ("Điểm cách mốc 0 một khoảng 6 đơn vị về phía bên trái trên trục số biểu diễn số nào?",
             new[]{"-6","+6","0","-12"},0),
            ("Tính tích hai số nguyên cùng dấu âm: (-4) * (-5) = ?",
             new[]{"+20","-20","-9","+9"},0),
            ("Số đối của số nguyên -15 là:",
             new[]{"15","-15","0","1/15"},0),
            ("Nhiệt độ sáng sớm ở Ruộng Bậc Thang là -2°C, đến trưa tăng thêm 7°C. Nhiệt độ trưa là:",
             new[]{"5°C","-9°C","9°C","-5°C"},0),
            ("Sắp xếp các số sau theo thứ tự tăng dần: -7, 4, 0, -2, 5",
             new[]{"-7; -2; 0; 4; 5","5; 4; 0; -2; -7","-2; -7; 0; 4; 5","0; -2; -7; 4; 5"},0),
            ("Tính: (-15) - (-6) = ?",
             new[]{"-9","-21","9","21"},0),
            ("Thửa ruộng thấp nhất ở độ cao -3m, thửa cao nhất ở +8m. Chênh lệch độ cao là:",
             new[]{"11m","5m","-11m","8m"},0)
        };

        private static readonly (string q, string[] opts, int ans)[] PAGODA_QUESTIONS =
        {
            ("Cột trụ đá Chùa Một Cột dựng giữa hồ nước đóng vai trò là yếu tố hình học nào?",
             new[]{"Trục đối xứng thẳng đứng","Tâm đối xứng phẳng","Đường chéo hình vuông","Cạnh đáy tam giác"},0),
            ("Hồ sen bao quanh Chùa Một Cột có hình vuông. Hình vuông có tất cả bao nhiêu trục đối xứng?",
             new[]{"4 trục đối xứng","2 trục đối xứng","8 trục đối xứng","Vô số trục"},0),
            ("Mái ngói Chùa Một Cột xòe đều 4 góc như bông sen. Điểm tâm mái chùa là:",
             new[]{"Tâm đối xứng của hình","Trục đối xứng nằm ngang","Đỉnh nhọn tam giác","Tâm đường tròn ngoại tiếp"},0),
            ("Tam giác đều có tất cả bao nhiêu trục đối xứng?",
             new[]{"3 trục đối xứng","1 trục đối xứng","2 trục đối xứng","Không có trục nào"},0),
            ("Hình nào dưới đây có tâm đối xứng nhưng KHÔNG có trục đối xứng?",
             new[]{"Hình bình hành","Hình vuông","Hình chữ nhật","Hình thoi"},0),
            ("Hình chữ nhật có tất cả bao nhiêu trục đối xứng?",
             new[]{"2 trục đối xứng","4 trục đối xứng","1 trục đối xứng","Không có trục nào"},0),
            ("Hình thoi có bao nhiêu trục đối xứng?",
             new[]{"2 trục đối xứng (hai đường chéo)","4 trục đối xứng","1 trục đối xứng","Không có"},0),
            ("Hình tròn có bao nhiêu trục đối xứng?",
             new[]{"Vô số trục đối xứng (mọi đường kính)","1 trục đối xứng","2 trục đối xứng","360 trục"},0),
            ("Một mảnh vườn hình vuông cạnh 5m ở cạnh chùa. Chu vi mảnh vườn là:",
             new[]{"20m","25m","10m","15m"},0),
            ("Diện tích của một tấm biển hình chữ nhật có chiều dài 8m, chiều rộng 4m là:",
             new[]{"32 m²","24 m²","16 m²","64 m²"},0),
            ("Hình lục giác đều có tất cả bao nhiêu trục đối xứng?",
             new[]{"6 trục đối xứng","3 trục đối xứng","12 trục đối xứng","4 trục đối xứng"},0),
            ("Biển báo giao thông hình tròn có viền đỏ có bao nhiêu tâm đối xứng?",
             new[]{"1 tâm đối xứng (chính là tâm hình tròn)","2 tâm đối xứng","Vô số tâm","Không có tâm đối xứng"},0)
        };

        // ─────────────────────────────────────────────────
        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        void Start()
        {
            // Hiển thị tên người chơi
            string nick = "Trạng Tí";
            if (Core.SessionManager.Instance != null && !string.IsNullOrEmpty(Core.SessionManager.Instance.CurrentNickname))
                nick = Core.SessionManager.Instance.CurrentNickname;
            else
                nick = PlayerPrefs.GetString("User_Nickname", "Trạng Tí");

            if (txtPlayerNick != null)
                txtPlayerNick.text = $"<b>{nick}</b>  <color=#ecf0f1><size=80%>(Lớp 6A)</size></color>";

            RefreshEconomyUI();
            CloseAllPanels();
            SetCursorLock(true);

            // Nút menu
            if (btnReturnMainMenu != null)
                btnReturnMainMenu.onClick.AddListener(() => LoadScene("02_MainMenu"));

            // Build internal quiz UI cho mỗi panel nếu cần
            BuildPhoQuizUI();
            BuildTerraceQuizUI();
            BuildPagodaQuizUI();
            BuildAIPanelButtons();
            FormatAllHUDTextsToVietnamese();
        }

        private void FormatAllHUDTextsToVietnamese()
        {
            // Nút Menu góc trên phải
            if (btnReturnMainMenu != null)
            {
                var lbl = btnReturnMainMenu.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.text = "<b>Menu chính (O)</b>";
            }

            // Hộp nhiệm vụ Khám phá Làng Sen góc trên phải
            var questBox = transform.Find("QuestBox");
            if (questBox != null)
            {
                var qTitle = questBox.Find("QuestTitle")?.GetComponent<TextMeshProUGUI>();
                if (qTitle != null) qTitle.text = "<color=#f1c40f><b>Khám phá Làng Sen</b></color>";

                var qDesc = questBox.Find("QuestDesc")?.GetComponent<TextMeshProUGUI>();
                if (qDesc != null) qDesc.text = "Ghé Bảng Cáo Thị [TAB], Quán Phở Cụ Ba hoặc Ruộng Bậc Thang!";
            }

            // Thanh phím tắt HotkeyPill ở dưới cùng màn hình
            var pill = transform.Find("HotkeyPill");
            if (pill != null)
            {
                var pText = pill.Find("PillText")?.GetComponent<TextMeshProUGUI>();
                if (pText != null) pText.text = "<b>[E] Tương tác  |  [F] Gia sư Thầy Minh  |  [TAB] Bảng Cáo Thị  |  [O] Menu chính</b>";
            }

            // Format toàn bộ bảng hiển thị 3D trong thế giới Làng Sen
            var interactables = FindObjectsByType<WorldInteractable>(FindObjectsInactive.Include);
            foreach (var wi in interactables)
            {
                var promptTmp = wi.GetComponentInChildren<TextMeshPro>();
                if (promptTmp != null)
                {
                    string cur = promptTmp.text;
                    if (cur.Contains("Bang Cao Thi") || cur.Contains("Notice") || cur.Contains("Bảng Cáo Thị"))
                        promptTmp.text = "[E] Bảng Cáo Thị Làng Sen";
                    else if (cur.Contains("Quan Pho") || cur.Contains("Pho") || cur.Contains("Quán Phở"))
                        promptTmp.text = "[E] Quán Phở Cụ Ba (Phân Số)";
                    else if (cur.Contains("Ruong Bac Thang") || cur.Contains("Terrace") || cur.Contains("Ruộng"))
                        promptTmp.text = "[E] Ruộng Bậc Thang (Số Nguyên)";
                    else if (cur.Contains("Chua Mot Cot") || cur.Contains("Pagoda") || cur.Contains("Chùa"))
                        promptTmp.text = "[E] Chùa Một Cột (Đối Xứng)";
                }
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))   TogglePanel(panelAITutorChat);
            if (Input.GetKeyDown(KeyCode.Tab)) TogglePanel(panelNoticeBoard);
            if (Input.GetKeyDown(KeyCode.O))   LoadScene("02_MainMenu");
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_anyModalOpen) CloseAllPanels();
            }

            if (_anyModalOpen)
            {
                // Luôn mở khóa con trỏ chuột và hiện rõ để người chơi click nút UI
                if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                // Hỗ trợ phím tắt A, B, C, D hoặc 1, 2, 3, 4 để chọn đáp án nhanh
                if (_activePanel != null && _activePanel != panelAITutorChat && _activePanel != panelNoticeBoard)
                {
                    if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                        TriggerQuizOption(0);
                    else if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                        TriggerQuizOption(1);
                    else if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                        TriggerQuizOption(2);
                    else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
                        TriggerQuizOption(3);
                }
            }
        }

        private void TriggerQuizOption(int optionIndex)
        {
            if (_activePanel == null) return;
            var card = _activePanel.transform.Find("Card");
            if (card == null) return;
            var btn = card.Find($"BtnOption_{optionIndex}")?.GetComponent<Button>();
            if (btn != null && btn.gameObject.activeInHierarchy && btn.interactable)
            {
                btn.onClick.Invoke();
            }
        }

        // ─────────────────────────────────────────────────
        //  HANDLE INTERACTION (gọi từ WorldInteractable)
        // ─────────────────────────────────────────────────
        public void HandleInteraction(WorldInteractable.InteractableType type)
        {
            switch (type)
            {
                case WorldInteractable.InteractableType.NoticeBoard:
                    OpenPanel(panelNoticeBoard);
                    break;

                case WorldInteractable.InteractableType.PhoShop:
                    OpenPanel(panelPhoMinigame);
                    _currentQuizType = QuizType.Pho;
                    LoadAndStartMinigame("PhanSo", "🍲 QUÁN PHỞ CỤ BA - THỬ THÁCH PHÂN SỐ",
                        "Cụ Ba vừa nấu xong nồi nước phở gia truyền thơm nức mũi! Hôm nay quán đông khách, cụ cần sĩ tử phụ tính toán chia bánh phở, nước dùng và thanh toán theo phân số để phục vụ dân làng.",
                        PHO_QUESTIONS);
                    break;

                case WorldInteractable.InteractableType.TerraceFields:
                    OpenPanel(panelTerraceFields);
                    _currentQuizType = QuizType.Terrace;
                    LoadAndStartMinigame("SoNguyen", "🌾 RUỘNG BẬC THANG - THỬ THÁCH SỐ NGUYÊN & TRỤC SỐ",
                        "Ruộng bậc thang Làng Sen trải dài từ đáy hồ sâu (-5m) lên đỉnh đồi dương (+5m). Hãy cùng bác nông dân đo đạc mực nước, nhiệt độ và độ cao bằng số nguyên!",
                        TERRACE_QUESTIONS);
                    break;

                case WorldInteractable.InteractableType.PagodaSymmetry:
                    OpenPanel(panelPagodaSymmetry);
                    _currentQuizType = QuizType.Pagoda;
                    LoadAndStartMinigame("HinhHoc", "🏛️ CHÙA MỘT CỘT - THỬ THÁCH HÌNH HỌC TRỰC QUAN",
                        "Chùa Một Cột cổ kính ngự giữa hồ sen hình vuông. Trụ đá là trục đối xứng thẳng đứng, mái ngói 4 góc đối xứng tâm. Thầy đồ đố sĩ tử tìm ra các quy luật hình học và tính đối xứng!",
                        PAGODA_QUESTIONS);
                    break;
            }
        }

        // ─────────────────────────────────────────────────
        //  PANEL MANAGEMENT
        // ─────────────────────────────────────────────────
        public void OpenPanel(GameObject panel)
        {
            if (panel == null) return;
            CloseAllPanels();
            panel.SetActive(true);
            _activePanel = panel;
            _anyModalOpen = true;
            SetCursorLock(false);
            if (panel == panelAITutorChat) BuildAIPanelButtons();
            if (panel == panelNoticeBoard) SetupNoticeBoardButtons();
        }

        public void TogglePanel(GameObject panel)
        {
            if (panel == null) return;
            if (_activePanel == panel && _anyModalOpen) { CloseAllPanels(); return; }
            OpenPanel(panel);
        }

        public void CloseAllPanels()
        {
            foreach (var p in new[] { panelNoticeBoard, panelPhoMinigame, panelTerraceFields, panelPagodaSymmetry, panelAITutorChat })
                if (p != null) p.SetActive(false);
            _anyModalOpen = false;
            _activePanel  = null;
            SetCursorLock(true);
        }

        public void CloseAllModals() => CloseAllPanels();

        // ─────────────────────────────────────────────────
        //  NOTICE BOARD — đặt nút thủ công khi mở
        // ─────────────────────────────────────────────────
        private void SetupNoticeBoardButtons()
        {
            if (panelNoticeBoard == null) return;
            var card = panelNoticeBoard.transform.Find("Card");
            if (card == null) return;

            // Ẩn các nút option quiz nếu có
            for (int i = 0; i < 4; i++)
            {
                var opt = card.Find($"BtnOption_{i}");
                if (opt != null) opt.gameObject.SetActive(false);
            }
            var fb = card.Find("FeedbackText");
            if (fb != null) fb.gameObject.SetActive(false);

            var cardImg = card.GetComponent<Image>();
            if (cardImg != null) cardImg.color = new Color(0.08f, 0.12f, 0.19f, 0.98f);

            var titleTmp = card.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (titleTmp != null)
            {
                var titleRt = titleTmp.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(1, 1);
                titleRt.pivot = new Vector2(0.5f, 1);
                titleRt.anchoredPosition = new Vector2(0, -18);
                titleRt.sizeDelta = new Vector2(-40, 44);
                titleTmp.text = "<color=#f1c40f><b>BẢNG CÁO THỊ LÀNG SEN</b></color>";
                titleTmp.fontSize = 24;
            }

            var bodyTmp = card.Find("Body")?.GetComponent<TextMeshProUGUI>();
            if (bodyTmp != null)
            {
                var bodyRt = bodyTmp.GetComponent<RectTransform>();
                bodyRt.anchorMin = new Vector2(0, 1); bodyRt.anchorMax = new Vector2(1, 1);
                bodyRt.pivot = new Vector2(0.5f, 1);
                bodyRt.anchoredPosition = new Vector2(0, -70);
                bodyRt.sizeDelta = new Vector2(-60, 260);
                bodyTmp.text = "Chào mừng sĩ tử đến với không gian học Toán 6 tương tác!\n\n" +
                               "<b>[1] Quán Phở Cụ Ba:</b> Ôn luyện Phân số & Rút gọn phân số\n" +
                               "<b>[2] Ruộng Bậc Thang:</b> Ôn luyện Trục số nguyên & Âm/Dương\n" +
                               "<b>[3] Chùa Một Cột:</b> Khám phá Trục đối xứng & Hình học\n" +
                               "<b>[4] Gia Sư AI Thầy Minh (Phím F):</b> Hướng dẫn & Mua gợi ý (10 Xu)\n\n" +
                               "Bấm <b>[VÀO BÀI HỌC]</b> bên dưới để mở toàn bộ Chuyên đề Toán Lớp 6!";
                bodyTmp.fontSize = 17;
                bodyTmp.alignment = TextAlignmentOptions.TopLeft;
            }

            var btnAct = card.Find("BtnAction")?.GetComponent<Button>();
            if (btnAct != null)
            {
                btnAct.gameObject.SetActive(true);
                var rt = btnAct.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(-190, -195);
                rt.sizeDelta = new Vector2(190, 50);
                var lbl = btnAct.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.text = "VÀO BÀI HỌC";
                btnAct.onClick.RemoveAllListeners();
                btnAct.onClick.AddListener(() => LoadScene("03_LessonList"));
            }

            // Nút Ôn tập câu sai (Anki / Quizlet)
            var btnReviewGo = card.Find("BtnWrongReview")?.gameObject;
            if (btnReviewGo == null)
            {
                btnReviewGo = new GameObject("BtnWrongReview", typeof(RectTransform), typeof(Image), typeof(Button));
                btnReviewGo.transform.SetParent(card, false);
                var rtr = btnReviewGo.GetComponent<RectTransform>();
                rtr.anchorMin = new Vector2(0.5f, 0.5f); rtr.anchorMax = new Vector2(0.5f, 0.5f);
                rtr.pivot = new Vector2(0.5f, 0.5f);
                rtr.anchoredPosition = new Vector2(10, -195);
                rtr.sizeDelta = new Vector2(180, 50);

                var imgR = btnReviewGo.GetComponent<Image>();
                imgR.color = new Color(0.85f, 0.35f, 0.15f);

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(btnReviewGo.transform, false);
                var txt = txtGo.GetComponent<TextMeshProUGUI>();
                int wrongCnt = Core.WrongQuestionBankManager.GetTotalWrongCount();
                txt.text = $"ÔN CÂU SAI ({wrongCnt})";
                txt.fontSize = 16;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;
            }
            if (btnReviewGo != null)
            {
                var btnRev = btnReviewGo.GetComponent<Button>();
                btnRev.onClick.RemoveAllListeners();
                btnRev.onClick.AddListener(() =>
                {
                    int wrongCnt = Core.WrongQuestionBankManager.GetTotalWrongCount();
                    if (wrongCnt == 0)
                    {
                        var bodyT = card.Find("Body")?.GetComponent<TextMeshProUGUI>();
                        if (bodyT != null)
                        {
                            bodyT.text = "<color=#2ecc71><b>XUẤT SẮC!</b></color>\n\nSĩ tử hiện không có câu hỏi nào làm sai trong Sổ tay ôn tập. Hãy tiếp tục khám phá các thử thách khác trong Làng Sen nhé!";
                        }
                        return;
                    }
                    PlayerPrefs.SetInt("Quiz_IsWrongReviewMode", 1);
                    LoadScene("05_MCQPractice");
                });
            }

            var btnCls = card.Find("BtnClose")?.GetComponent<Button>();
            if (btnCls != null)
            {
                btnCls.gameObject.SetActive(true);
                var rt = btnCls.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(200, -195);
                rt.sizeDelta = new Vector2(170, 50);
                btnCls.onClick.RemoveAllListeners();
                btnCls.onClick.AddListener(CloseAllPanels);
            }
        }

        // ─────────────────────────────────────────────────
        //  QUIZ SYSTEM — 4 nút đáp án tương tác phong phú
        // ─────────────────────────────────────────────────
        private enum QuizType { None, Pho, Terrace, Pagoda }
        private QuizType _currentQuizType = QuizType.None;

        private int GetCurrentCorrectCount()
        {
            return _currentQuizType switch
            {
                QuizType.Pho => _phoCorrect,
                QuizType.Terrace => _terraceCorrect,
                QuizType.Pagoda => _pagodaCorrect,
                _ => 0
            };
        }

        private void IncrementCurrentCorrectCount()
        {
            switch (_currentQuizType)
            {
                case QuizType.Pho: _phoCorrect++; break;
                case QuizType.Terrace: _terraceCorrect++; break;
                case QuizType.Pagoda: _pagodaCorrect++; break;
            }
        }

        private void BuildPhoQuizUI() => PrebuildQuizUI(panelPhoMinigame);
        private void BuildTerraceQuizUI() => PrebuildQuizUI(panelTerraceFields);
        private void BuildPagodaQuizUI() => PrebuildQuizUI(panelPagodaSymmetry);

        private void PrebuildQuizUI(GameObject panel)
        {
            if (panel == null) return;
            var card = panel.transform.Find("Card");
            if (card == null) return;

            var oldBtnA = card.Find("BtnAction");
            if (oldBtnA != null) oldBtnA.gameObject.SetActive(false);

            var btnClose = card.Find("BtnClose")?.GetComponent<Button>();
            if (btnClose != null)
            {
                var rtClose = btnClose.GetComponent<RectTransform>();
                rtClose.anchorMin = new Vector2(0.5f, 0.5f); rtClose.anchorMax = new Vector2(0.5f, 0.5f);
                rtClose.pivot = new Vector2(0.5f, 0.5f);
                rtClose.anchoredPosition = new Vector2(0, -205);
                rtClose.sizeDelta = new Vector2(220, 46);
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(CloseAllPanels);
            }
        }

        private void LoadAndStartMinigame(string topic, string title, string story, (string q, string[] opts, int ans)[] fallback)
        {
            _currentZoneTitle = title;
            _currentStoryContext = story;

            ShowStoryLoadingUI(title, story);

            if (GoogleSheetsNetworkService.Instance != null)
            {
                GoogleSheetsNetworkService.Instance.GetQuestions(topic, "6", (ok, qs, msg) =>
                {
                    if (this == null || gameObject == null || _activePanel == null) return;

                    var list = new List<MinigameQuestion>();
                    if (ok && qs != null && qs.Count > 0)
                    {
                        var filtered = new List<QuestionData>();
                        foreach (var q in qs)
                        {
                            if (q.type == "MCQ" && GoogleSheetsNetworkService.IsTopicMatch(q.topic, topic))
                            {
                                if (q.options != null && !string.IsNullOrEmpty(q.options.A) && !string.IsNullOrEmpty(q.options.B))
                                {
                                    filtered.Add(q);
                                }
                            }
                        }

                        if (filtered.Count > 0)
                        {
                            // Trộn ngẫu nhiên câu hỏi (Fisher-Yates)
                            var rnd = new System.Random();
                            for (int i = filtered.Count - 1; i > 0; i--)
                            {
                                int j = rnd.Next(i + 1);
                                var t = filtered[i]; filtered[i] = filtered[j]; filtered[j] = t;
                            }

                            foreach (var q in filtered)
                            {
                                list.Add(ConvertToMinigameQuestion(q));
                            }
                        }
                    }

                    // Nếu Google Sheet chưa có câu hỏi thuộc đúng chủ đề này -> Dùng kho câu hỏi dự phòng chuyên đề
                    if (list.Count == 0 && fallback != null && fallback.Length > 0)
                    {
                        var fallbackList = new List<(string q, string[] opts, int ans)>(fallback);
                        var rnd = new System.Random();
                        for (int i = fallbackList.Count - 1; i > 0; i--)
                        {
                            int j = rnd.Next(i + 1);
                            var t = fallbackList[i]; fallbackList[i] = fallbackList[j]; fallbackList[j] = t;
                        }

                        foreach (var item in fallbackList)
                        {
                            list.Add(new MinigameQuestion { prompt = FormatMathString(item.q), opts = item.opts, ans = item.ans });
                        }
                    }

                    _currentQuestions = list;
                    DisplayCurrentMinigameQuestion();
                });
            }
            else
            {
                var list = new List<MinigameQuestion>();
                if (fallback != null)
                {
                    var fallbackList = new List<(string q, string[] opts, int ans)>(fallback);
                    var rnd = new System.Random();
                    for (int i = fallbackList.Count - 1; i > 0; i--)
                    {
                        int j = rnd.Next(i + 1);
                        var t = fallbackList[i]; fallbackList[i] = fallbackList[j]; fallbackList[j] = t;
                    }

                    foreach (var item in fallbackList)
                    {
                        list.Add(new MinigameQuestion { prompt = FormatMathString(item.q), opts = item.opts, ans = item.ans });
                    }
                }
                _currentQuestions = list;
                DisplayCurrentMinigameQuestion();
            }
        }

        private void ShowStoryLoadingUI(string title, string story)
        {
            if (_activePanel == null) return;
            var card = _activePanel.transform.Find("Card");
            if (card == null) return;

            var titleTmp = card.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (titleTmp != null)
            {
                titleTmp.text = $"<color=#f1c40f><b>{title}</b></color>";
            }

            var bodyTmp = card.Find("Body")?.GetComponent<TextMeshProUGUI>();
            if (bodyTmp != null)
            {
                bodyTmp.text = $"<color=#7ed6df><i>\"{story}\"</i></color>\n\n<color=#f39c12>Đang chuẩn bị câu hỏi thử thách...</color>";
            }

            for (int i = 0; i < 4; i++)
            {
                var opt = card.Find($"BtnOption_{i}");
                if (opt != null) opt.gameObject.SetActive(false);
            }
        }

        private MinigameQuestion ConvertToMinigameQuestion(QuestionData q)
        {
            var opts = new List<string>();
            int correctIdx = 0;

            if (q.options != null)
            {
                if (!string.IsNullOrEmpty(q.options.A)) opts.Add(FormatMathString(q.options.A));
                if (!string.IsNullOrEmpty(q.options.B)) opts.Add(FormatMathString(q.options.B));
                if (!string.IsNullOrEmpty(q.options.C)) opts.Add(FormatMathString(q.options.C));
                if (!string.IsNullOrEmpty(q.options.D)) opts.Add(FormatMathString(q.options.D));
            }

            if (opts.Count == 0)
            {
                opts.Add("A"); opts.Add("B"); opts.Add("C"); opts.Add("D");
            }

            string c = (q.correctAnswer ?? "").Trim().ToUpperInvariant();
            if (c == "A" || c == "0") correctIdx = 0;
            else if (c == "B" || c == "1") correctIdx = 1;
            else if (c == "C" || c == "2") correctIdx = 2;
            else if (c == "D" || c == "3") correctIdx = 3;

            return new MinigameQuestion
            {
                prompt = FormatMathString(q.prompt),
                opts = opts.ToArray(),
                ans = Mathf.Clamp(correctIdx, 0, opts.Count - 1)
            };
        }

        private string FormatMathString(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return Math6Companion.Core.Services.GoogleSheetsNetworkService.SanitizeMathText(text)
                .Replace("\\u2208", " thuộc ")
                .Replace("∈", " thuộc ")
                .Replace("\\u2209", " không thuộc ")
                .Replace("∉", " không thuộc ")
                .Replace("\\u2282", " con ")
                .Replace("⊂", " con ")
                .Replace("\\u2286", " con hoặc bằng ")
                .Replace("⊆", " con hoặc bằng ")
                .Replace("\\u2229", " giao ")
                .Replace("∩", " giao ")
                .Replace("\\u222A", " hợp ")
                .Replace("∪", " hợp ");
        }

        private void DisplayCurrentMinigameQuestion()
        {
            if (_activePanel == null || _currentQuestions == null || _currentQuestions.Count == 0) return;
            var card = _activePanel.transform.Find("Card");
            if (card == null) return;

            var cardImg = card.GetComponent<Image>();
            if (cardImg != null) cardImg.color = new Color(0.08f, 0.12f, 0.19f, 0.98f);

            var oldBtnA = card.Find("BtnAction");
            if (oldBtnA != null) oldBtnA.gameObject.SetActive(false);

            int correctCount = GetCurrentCorrectCount();
            int qIndex = correctCount % _currentQuestions.Count;
            var curQ = _currentQuestions[qIndex];

            // 1. Tiêu đề khu vực
            var titleTmp = card.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (titleTmp != null)
            {
                var titleRt = titleTmp.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(1, 1);
                titleRt.pivot = new Vector2(0.5f, 1);
                titleRt.anchoredPosition = new Vector2(0, -16);
                titleRt.sizeDelta = new Vector2(-40, 42);

                titleTmp.text = $"<color=#f1c40f><b>{_currentZoneTitle}</b></color>  <size=80%>(Đúng: {correctCount})</size>";
                titleTmp.fontSize = 22;
                titleTmp.alignment = TextAlignmentOptions.Center;
            }

            // 2. Lời dẫn câu chuyện + Nội dung câu hỏi
            var bodyTmp = card.Find("Body")?.GetComponent<TextMeshProUGUI>();
            if (bodyTmp != null)
            {
                var bodyRt = bodyTmp.GetComponent<RectTransform>();
                bodyRt.anchorMin = new Vector2(0, 1); bodyRt.anchorMax = new Vector2(1, 1);
                bodyRt.pivot = new Vector2(0.5f, 1);
                bodyRt.anchoredPosition = new Vector2(0, -68);
                bodyRt.sizeDelta = new Vector2(-60, 85);

                bodyTmp.text = $"<color=#7ed6df><i>{_currentStoryContext}</i></color>\n\n<b>Câu {qIndex + 1}/{_currentQuestions.Count}:</b>  {curQ.prompt}";
                bodyTmp.fontSize = 17;
                bodyTmp.alignment = TextAlignmentOptions.TopLeft;
                bodyTmp.color = new Color(0.95f, 0.96f, 0.98f);
            }

            // 3. Text Feedback kết quả
            var fbObj = card.Find("FeedbackText");
            TextMeshProUGUI fbTmp = null;
            if (fbObj == null)
            {
                var go = new GameObject("FeedbackText", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(card, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(0, -158);
                rt.sizeDelta = new Vector2(-60, 32);
                fbTmp = go.GetComponent<TextMeshProUGUI>();
                fbTmp.fontSize = 17;
                fbTmp.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                fbObj.gameObject.SetActive(true);
                var rt = fbObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(0, -158);
                rt.sizeDelta = new Vector2(-60, 32);
                fbTmp = fbObj.GetComponent<TextMeshProUGUI>();
            }
            if (fbTmp != null) fbTmp.text = "";

            // 4. Bố cục 4 nút đáp án (A, B, C, D) dạng 2x2
            string[] prefix = { "A", "B", "C", "D" };
            Vector2[] btnPositions = {
                new Vector2(-205, -18),   // A (Góc trên trái)
                new Vector2(205, -18),    // B (Góc trên phải)
                new Vector2(-205, -82),   // C (Góc dưới trái)
                new Vector2(205, -82)     // D (Góc dưới phải)
            };

            for (int i = 0; i < 4; i++)
            {
                string optName = $"BtnOption_{i}";
                var optTransform = card.Find(optName);
                GameObject optGo = null;
                Button btn = null;
                Image img = null;
                TextMeshProUGUI lbl = null;

                if (optTransform == null)
                {
                    optGo = new GameObject(optName, typeof(RectTransform), typeof(Image), typeof(Button));
                    optGo.transform.SetParent(card, false);
                    var rt = optGo.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = btnPositions[i];
                    rt.sizeDelta = new Vector2(370, 52);

                    img = optGo.GetComponent<Image>();
                    btn = optGo.GetComponent<Button>();

                    var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    lblGo.transform.SetParent(optGo.transform, false);
                    var lblRt = lblGo.GetComponent<RectTransform>();
                    lblRt.anchorMin = Vector2.zero;
                    lblRt.anchorMax = Vector2.one;
                    lblRt.offsetMin = new Vector2(16, 0);
                    lblRt.offsetMax = new Vector2(-16, 0);

                    lbl = lblGo.GetComponent<TextMeshProUGUI>();
                    lbl.fontSize = 17;
                    lbl.alignment = TextAlignmentOptions.MidlineLeft;
                    lbl.color = Color.white;
                }
                else
                {
                    optGo = optTransform.gameObject;
                    optGo.SetActive(true);
                    var rt = optGo.GetComponent<RectTransform>();
                    rt.anchoredPosition = btnPositions[i];
                    rt.sizeDelta = new Vector2(370, 52);
                    btn = optGo.GetComponent<Button>();
                    img = optGo.GetComponent<Image>();
                    lbl = optGo.GetComponentInChildren<TextMeshProUGUI>();
                }

                if (i < curQ.opts.Length)
                {
                    optGo.SetActive(true);
                    img.color = new Color(0.18f, 0.24f, 0.34f);
                    btn.interactable = true;
                    if (lbl != null)
                    {
                        lbl.text = $"<b><color=#f1c40f>[{prefix[i]}]</color></b>  {curQ.opts[i]}";
                        lbl.color = Color.white;
                    }

                    int choiceIdx = i;
                    int capturedQIdx = qIndex;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnQuizAnswer(capturedQIdx, choiceIdx, card));
                }
                else
                {
                    optGo.SetActive(false);
                }
            }

            // 5. Nút đóng
            var btnClose = card.Find("BtnClose")?.GetComponent<Button>();
            if (btnClose != null)
            {
                btnClose.gameObject.SetActive(true);
                var rtClose = btnClose.GetComponent<RectTransform>();
                rtClose.anchorMin = new Vector2(0.5f, 0.5f); rtClose.anchorMax = new Vector2(0.5f, 0.5f);
                rtClose.pivot = new Vector2(0.5f, 0.5f);
                rtClose.anchoredPosition = new Vector2(0, -195);
                rtClose.sizeDelta = new Vector2(220, 46);
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(CloseAllPanels);
            }
        }

        private void OnQuizAnswer(int qIdx, int chosen, Transform card)
        {
            if (_currentQuestions == null || qIdx >= _currentQuestions.Count) return;

            for (int i = 0; i < 4; i++)
            {
                var opt = card.Find($"BtnOption_{i}")?.GetComponent<Button>();
                if (opt != null) opt.interactable = false;
            }

            var curQ = _currentQuestions[qIdx];
            bool correct = chosen == curQ.ans;
            var chosenBtnImg = card.Find($"BtnOption_{chosen}")?.GetComponent<Image>();
            var correctBtnImg = card.Find($"BtnOption_{curQ.ans}")?.GetComponent<Image>();
            var fbTmp = card.Find("FeedbackText")?.GetComponent<TextMeshProUGUI>();

            if (correct)
            {
                if (chosenBtnImg != null) chosenBtnImg.color = new Color(0.15f, 0.68f, 0.38f);
                IncrementCurrentCorrectCount();
                AddStars(STARS_PER_CORRECT);
                AddCoins(COINS_PER_MINIGAME);

                if (fbTmp != null)
                    fbTmp.text = $"<color=#2ecc71><b>[ĐÚNG] Chính xác! +{STARS_PER_CORRECT} Sao, +{COINS_PER_MINIGAME} Xu</b></color>";

                if (aiCompanion != null)
                    aiCompanion.Say("Tuyệt vời! Em trả lời đúng rồi!", 3f);
            }
            else
            {
                if (chosenBtnImg != null) chosenBtnImg.color = new Color(0.85f, 0.25f, 0.2f);
                if (correctBtnImg != null) correctBtnImg.color = new Color(0.15f, 0.68f, 0.38f);

                if (fbTmp != null)
                    fbTmp.text = $"<color=#e74c3c><b>[CHƯA ĐÚNG] Đáp án đúng là: {curQ.opts[curQ.ans]}</b></color>";

                if (aiCompanion != null)
                    aiCompanion.Say("Chưa đúng rồi! Hãy xem đáp án nhé!", 3f);
            }

            StartCoroutine(NextQuestionDelayed());
        }

        private IEnumerator NextQuestionDelayed()
        {
            yield return new WaitForSecondsRealtime(2f);
            if (_activePanel != null)
                DisplayCurrentMinigameQuestion();
        }

        // ─────────────────────────────────────────────────
        //  AI TUTOR PANEL (Không bị tràn chữ, đầy đủ nút bấm)
        // ─────────────────────────────────────────────────
        private void BuildAIPanelButtons()
        {
            if (panelAITutorChat == null) return;
            var card = panelAITutorChat.transform.Find("Card");
            if (card == null) return;

            // Ẩn toàn bộ nút trắc nghiệm hoặc feedback thừa nếu có trên Card AI Tutor
            for (int i = 0; i < 4; i++)
            {
                var opt = card.Find($"BtnOption_{i}");
                if (opt != null) opt.gameObject.SetActive(false);
            }
            var fb = card.Find("FeedbackText");
            if (fb != null) fb.gameObject.SetActive(false);
            var btnAct = card.Find("BtnAction");
            if (btnAct != null) btnAct.gameObject.SetActive(false);

            var cardImg = card.GetComponent<Image>();
            if (cardImg != null) cardImg.color = new Color(0.08f, 0.12f, 0.19f, 0.98f);

            // Tiêu đề card AI Thầy Minh
            var titleTmp = card.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (titleTmp != null)
            {
                var titleRt = titleTmp.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0, 1); titleRt.anchorMax = new Vector2(1, 1);
                titleRt.pivot = new Vector2(0.5f, 1);
                titleRt.anchoredPosition = new Vector2(0, -18);
                titleRt.sizeDelta = new Vector2(-40, 44);
                titleTmp.text = "<color=#38d9a9><b>GIA SƯ AI THẦY MINH</b></color>";
                titleTmp.fontSize = 24;
                titleTmp.alignment = TextAlignmentOptions.Center;
            }

            // ChatScroll rộng rãi (chiều cao 365px, không bị cắt chữ)
            var scroll = card.Find("ChatScroll");
            if (scroll != null)
            {
                if (scroll.GetComponent<RectMask2D>() == null)
                    scroll.gameObject.AddComponent<RectMask2D>();

                var scrollRt = scroll.GetComponent<RectTransform>();
                scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
                scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
                scrollRt.pivot = new Vector2(0.5f, 0.5f);
                scrollRt.anchoredPosition = new Vector2(0, 38);
                scrollRt.sizeDelta = new Vector2(800, 365);

                var scrollImg = scroll.GetComponent<Image>();
                if (scrollImg != null) scrollImg.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);

                var chatTxt = scroll.Find("ChatHistory")?.GetComponent<TextMeshProUGUI>();
                if (chatTxt != null)
                {
                    var chatRt = chatTxt.GetComponent<RectTransform>();
                    chatRt.anchorMin = Vector2.zero;
                    chatRt.anchorMax = Vector2.one;
                    chatRt.offsetMin = new Vector2(24, 18);
                    chatRt.offsetMax = new Vector2(-24, -18);
                    chatTxt.fontSize = 17;
                    chatTxt.lineSpacing = 1.15f;
                    chatTxt.alignment = TextAlignmentOptions.TopLeft;
                    chatTxt.textWrappingMode = TextWrappingModes.Normal;
                    chatTxt.overflowMode = TextOverflowModes.Overflow;

                    if (string.IsNullOrEmpty(chatTxt.text) || chatTxt.text.Contains("Thay dong hanh"))
                    {
                        chatTxt.text = "<color=#5dade2><b>Thầy Minh:</b></color> Chào em! Thầy đồng hành cùng em trên Làng Sen tri thức.\nHãy chọn chủ đề bên dưới hoặc bấm <b>[Gợi ý]</b> để thầy hướng dẫn chi tiết nhé!";
                    }
                }
            }

            // BtnAsk1 — Hỏi Phân Số
            var btnAsk1 = card.Find("BtnAsk1")?.GetComponent<Button>();
            if (btnAsk1 != null)
            {
                var rt1 = btnAsk1.GetComponent<RectTransform>();
                rt1.anchorMin = new Vector2(0.5f, 0.5f); rt1.anchorMax = new Vector2(0.5f, 0.5f);
                rt1.pivot = new Vector2(0.5f, 0.5f);
                rt1.anchoredPosition = new Vector2(-275, -188);
                rt1.sizeDelta = new Vector2(240, 48);
                var lbl1 = btnAsk1.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl1 != null) lbl1.text = "<b>[?] Phân Số</b>";
                btnAsk1.onClick.RemoveAllListeners();
                btnAsk1.onClick.AddListener(() =>
                {
                    UpdateAIChat(card, "HƯỚNG DẪN: PHÂN SỐ LỚP 6",
                        "Muốn cộng/trừ hai phân số khác mẫu số:\n" +
                        "1. Quy đồng mẫu số hai phân số.\n" +
                        "2. Cộng/trừ tử số và giữ nguyên mẫu số chung.\n" +
                        "• Công thức: <b>a/m + b/n = (a*n + b*m)/(m*n)</b>\n" +
                        "• Ví dụ: 1/2 + 1/3 = 3/6 + 2/6 = <b>5/6</b>");
                });
            }

            // BtnAsk2 — Hỏi Số Nguyên
            var btnAsk2 = card.Find("BtnAsk2")?.GetComponent<Button>();
            if (btnAsk2 != null)
            {
                var rt2 = btnAsk2.GetComponent<RectTransform>();
                rt2.anchorMin = new Vector2(0.5f, 0.5f); rt2.anchorMax = new Vector2(0.5f, 0.5f);
                rt2.pivot = new Vector2(0.5f, 0.5f);
                rt2.anchoredPosition = new Vector2(0, -188);
                rt2.sizeDelta = new Vector2(240, 48);
                var lbl2 = btnAsk2.GetComponentInChildren<TextMeshProUGUI>();
                if (lbl2 != null) lbl2.text = "<b>[123] Số Nguyên</b>";
                btnAsk2.onClick.RemoveAllListeners();
                btnAsk2.onClick.AddListener(() =>
                {
                    UpdateAIChat(card, "HƯỚNG DẪN: SỐ NGUYÊN LỚP 6",
                        "• <b>Cộng cùng dấu:</b> Cộng phần tự nhiên, giữ dấu chung: (-3) + (-5) = <b>-8</b>\n" +
                        "• <b>Cộng khác dấu:</b> Lấy số lớn trừ số bé, giữ dấu của số có trị tuyệt đối lớn: (-7) + 3 = <b>-4</b>\n" +
                        "• <b>Nhân/Chia:</b> Cùng dấu ra (+), khác dấu ra (-): (-2) * (-3) = <b>+6</b>");
                });
            }

            // Nút MUA GỢI Ý (10 xu)
            EnsureHintButton(card);

            // BtnClose
            var btnCls = card.Find("BtnClose")?.GetComponent<Button>();
            if (btnCls != null)
            {
                var rtC = btnCls.GetComponent<RectTransform>();
                rtC.anchorMin = new Vector2(0.5f, 0.5f); rtC.anchorMax = new Vector2(0.5f, 0.5f);
                rtC.pivot = new Vector2(0.5f, 0.5f);
                rtC.anchoredPosition = new Vector2(0, -248);
                rtC.sizeDelta = new Vector2(240, 44);
                var lblC = btnCls.GetComponentInChildren<TextMeshProUGUI>();
                if (lblC != null) lblC.text = "<b>ĐÓNG [ESC]</b>";
                btnCls.onClick.RemoveAllListeners();
                btnCls.onClick.AddListener(CloseAllPanels);
            }
        }

        private void UpdateAIChat(Transform card, string header, string msg)
        {
            var scroll = card.Find("ChatScroll");
            if (scroll != null)
            {
                var chat = scroll.Find("ChatHistory")?.GetComponent<TextMeshProUGUI>();
                if (chat != null)
                {
                    chat.text = $"<color=#f1c40f><b>{header}</b></color>\n\n<color=#5dade2><b>Thầy Minh:</b></color>\n{msg}";
                    if (aiCompanion != null) aiCompanion.Say("Thầy đã gửi hướng dẫn chi tiết cho em rồi nhé!", 3f);
                }
            }
        }

        private void EnsureHintButton(Transform card)
        {
            var hintBtnGo = card.Find("BtnBuyHint")?.gameObject;
            if (hintBtnGo == null)
            {
                hintBtnGo = new GameObject("BtnBuyHint", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(Button));
                hintBtnGo.transform.SetParent(card, false);
                var lblGo = new GameObject("Text", typeof(RectTransform));
                lblGo.transform.SetParent(hintBtnGo.transform, false);
                var lbl = lblGo.AddComponent<TextMeshProUGUI>();
                lbl.text = $"[Gợi ý] Mua [10 Xu]";
                lbl.fontSize = 16;
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.color = Color.white;
                var lblRt = lblGo.GetComponent<RectTransform>();
                lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
                lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
            }

            var rt = hintBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(275, -188);
            rt.sizeDelta = new Vector2(240, 48);

            var img = hintBtnGo.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.9f, 0.65f, 0.1f);

            var btn = hintBtnGo.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (_coins >= HINT_COST)
                {
                    SpendCoins(HINT_COST);
                    int tokens = PlayerPrefs.GetInt("User_Hint_Tokens", 0) + 1;
                    PlayerPrefs.SetInt("User_Hint_Tokens", tokens);
                    PlayerPrefs.Save();

                    UpdateAIChat(card, "GỢI Ý ĐẶC BIỆT TỪ THẦY MINH",
                        $"<b>[ĐÃ NHẬN 1 THẺ GỢI Ý AI]:</b>\n" +
                        $"Em hiện có <b>{tokens} Thẻ Gợi Ý Thầy Minh</b> để dùng trợ giúp khi làm bài tập hoặc giải câu đố minigame.\n\n" +
                        "<b>[Mẹo Giải Toán 6 Nhanh]:</b>\n" +
                        "• <i>Phân số:</i> Tìm <b>BCNN</b> của các mẫu số để quy đồng mẫu nhanh nhất.\n" +
                        "• <i>Số nguyên:</i> Khi cộng hai số khác dấu, lấy số lớn trừ số bé và giữ dấu của số có trị tuyệt đối lớn hơn!");
                }
                else
                {
                    UpdateAIChat(card, "THÔNG BÁO TỪ THẦY MINH",
                        $"Em chưa đủ Xu để mua Thẻ Gợi Ý! (Cần <b>{HINT_COST} Xu</b>, hiện có <b>{_coins} Xu</b>).\n" +
                        "Hãy làm thêm các bài tập tại Quán Phở Cụ Ba, Tháp Chàm hoặc Ruộng Bậc Thang để nhận thêm Xu nhé!");
                }
            });
        }

        // ─────────────────────────────────────────────────
        //  ECONOMY API — gọi từ bất kỳ đâu
        // ─────────────────────────────────────────────────
        public void AddStars(int amount)
        {
            _stars += amount;
            RefreshEconomyUI();
            PlayerPrefs.SetInt("Village_Stars", _stars);
            PlayerPrefs.Save();
        }

        public void AddCoins(int amount)
        {
            _coins += amount;
            RefreshEconomyUI();
            PlayerPrefs.SetInt("Village_Coins", _coins);
            PlayerPrefs.Save();
        }

        public bool SpendCoins(int amount)
        {
            if (_coins < amount) return false;
            _coins -= amount;
            RefreshEconomyUI();
            PlayerPrefs.SetInt("Village_Coins", _coins);
            PlayerPrefs.Save();
            return true;
        }

        private void RefreshEconomyUI()
        {
            if (txtStars != null) txtStars.text = $"<color=#f1c40f><b>Sao: {_stars}</b></color>";
            if (txtCoins != null) txtCoins.text = $"<color=#2ecc71><b>Xu: {_coins}</b></color>";
        }

        // Legacy — gọi từ code cũ
        public void AddReward(int stars, int coins) { AddStars(stars); AddCoins(coins); }

        // ─────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────
        private static void SetCursorLock(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible   = !locked;
        }

        private void LoadScene(string name)
        {
            SetCursorLock(false);
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && Core.SessionManager.Instance != null)
                Core.SessionManager.Instance.SaveVillagePosition(player.transform.position, player.transform.rotation);
            if (Core.SessionManager.Instance != null)
                Core.SessionManager.Instance.ReturnSceneName = "02_VillageWorld";
            SceneManager.LoadScene(name);
        }

        private void WireButton(GameObject panel, string childName, UnityEngine.Events.UnityAction act)
        {
            var card = panel.transform.Find("Card");
            WireButtonInCard(card, childName, act);
        }

        private void WireButtonInCard(Transform card, string childName, UnityEngine.Events.UnityAction act)
        {
            if (card == null) return;
            var btn = card.Find(childName)?.GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(act);
        }
    }
}
