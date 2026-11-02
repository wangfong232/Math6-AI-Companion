using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;

namespace Math6Companion.UI
{
    /// <summary>
    /// Điều khiển Scene 08_AIFeedback.unity.
    /// AI Companion ("Thầy Minh") phân tích chi tiết lỗi sai từ trắc nghiệm hoặc tự luận,
    /// giải thích nguyên nhân và đưa ra bài tập tương tự.
    /// </summary>
    public class AIFeedbackUIController : MonoBehaviour
    {
        [Header("Tutor Character & Dialogue")]
        [SerializeField] private TextMeshProUGUI tutorGreetingText;
        [SerializeField] private TextMeshProUGUI questionTitleText;
        [SerializeField] private TextMeshProUGUI diagnosisBodyText;
        [SerializeField] private TextMeshProUGUI ruleReminderText;
        [SerializeField] private TextMeshProUGUI analogousProblemText;

        [Header("Navigation Buttons")]
        [SerializeField] private Button nextWrongQuestionButton;
        [SerializeField] private Button prevWrongQuestionButton;
        [SerializeField] private TextMeshProUGUI pageIndicatorText;

        [Header("System Buttons")]
        [SerializeField] private Button backToScoreButton;
        [SerializeField] private Button backToMenuButton;

        private int currentWrongIndex = 0;
        private int _penaltyStepIdx = 0;
        private System.Collections.Generic.List<string> _penaltySteps = new System.Collections.Generic.List<string>();

        private void Start()
        {
            if (backToScoreButton != null)
                backToScoreButton.onClick.AddListener(() => NavigateScene("07_Score"));

            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(() => NavigateScene(SessionManager.Instance != null ? SessionManager.Instance.ReturnSceneName : "02_VillageWorld"));

            if (nextWrongQuestionButton != null)
                nextWrongQuestionButton.onClick.AddListener(OnNextClicked);

            if (prevWrongQuestionButton != null)
                prevWrongQuestionButton.onClick.AddListener(OnPrevClicked);

            LoadFeedbackData();
        }

        private void LoadFeedbackData()
        {
            if ((ScoreSessionContext.ActivityType == "ESSAY" || !string.IsNullOrEmpty(ScoreSessionContext.EssayFeedback)) && (ScoreSessionContext.WrongQuestionPrompts == null || ScoreSessionContext.WrongQuestionPrompts.Count == 0))
            {
                DisplayEssayFeedback();
            }
            else if (ScoreSessionContext.WrongQuestionPrompts != null && ScoreSessionContext.WrongQuestionPrompts.Count > 0)
            {
                currentWrongIndex = 0;
                DisplayWrongQuestion(currentWrongIndex);
            }
            else if (!string.IsNullOrEmpty(ScoreSessionContext.EssayFeedback))
            {
                DisplayEssayFeedback();
            }
            else
            {
                // Dữ liệu mẫu minh họa khi test trực tiếp trong Unity Editor
                if (tutorGreetingText != null)
                    tutorGreetingText.text = "Chào em! Thầy Minh ở đây để cùng em xem lại các câu cần lưu ý nhé:";

                if (questionTitleText != null)
                    questionTitleText.text = "Câu sai mẫu: Tính giá trị biểu thức: (-15) + 7";

                if (diagnosisBodyText != null)
                    diagnosisBodyText.text = "Nguyên nhân nhầm lẫn: Em đã chọn phương án A (8) vì lấy 15 - 7 nhưng quên đặt dấu '-' của số có giá trị tuyệt đối lớn hơn (-15).";

                if (ruleReminderText != null)
                    ruleReminderText.text = "Quy tắc cốt lõi: Khi cộng hai số nguyên khác dấu, ta lấy số lớn trừ số bé (theo giá trị tuyệt đối) và mang dấu của số lớn hơn: (-15) + 7 = -(15 - 7) = -8.";

                if (analogousProblemText != null)
                    analogousProblemText.text = "Thử làm lại câu tương tự: Tính (-20) + 12 = ?";

                if (pageIndicatorText != null) pageIndicatorText.text = "Câu 1/1";
            }
        }

        private void DisplayEssayFeedback()
        {
            if (tutorGreetingText != null)
            {
                tutorGreetingText.text = "Thầy Minh đã phân tích bài tự luận của em:";
                var rtG = tutorGreetingText.GetComponent<RectTransform>();
                if (rtG != null)
                {
                    rtG.anchoredPosition = new Vector2(0, -60);
                    rtG.sizeDelta = new Vector2(1440, 36);
                    tutorGreetingText.fontSize = 22;
                }
            }

            var cardRt = (questionTitleText != null && questionTitleText.transform.parent != null) 
                ? questionTitleText.transform.parent.GetComponent<RectTransform>() 
                : null;
            if (cardRt != null)
            {
                cardRt.anchorMin = new Vector2(0.5f, 1f);
                cardRt.anchorMax = new Vector2(0.5f, 1f);
                cardRt.pivot = new Vector2(0.5f, 1f);
                cardRt.anchoredPosition = new Vector2(0, -96);
                cardRt.sizeDelta = new Vector2(1540, 840);
            }

            if (questionTitleText != null)
            {
                string p = !string.IsNullOrEmpty(ScoreSessionContext.CurrentQuestionPrompt)
                    ? ScoreSessionContext.CurrentQuestionPrompt.Replace("\n", " ").Trim()
                    : "Đánh giá chi tiết bài giải tự luận theo Rubric";
                questionTitleText.text = $"<b>Đề bài:</b> {p}";
                questionTitleText.fontSize = 20;
                questionTitleText.alignment = TextAlignmentOptions.MidlineLeft;
                questionTitleText.color = new Color(0.95f, 0.75f, 0.20f);
                var rtQ = questionTitleText.GetComponent<RectTransform>();
                if (rtQ != null)
                {
                    rtQ.anchorMin = new Vector2(0.5f, 1f);
                    rtQ.anchorMax = new Vector2(0.5f, 1f);
                    rtQ.pivot = new Vector2(0.5f, 1f);
                    rtQ.anchoredPosition = new Vector2(0, -22);
                    rtQ.sizeDelta = new Vector2(1460, 52);
                }
            }

            var boxDiag = diagnosisBodyText != null ? diagnosisBodyText.transform.parent.gameObject : null;
            var boxRule = ruleReminderText != null ? ruleReminderText.transform.parent.gameObject : null;
            var boxAnalog = analogousProblemText != null ? analogousProblemText.transform.parent.gameObject : null;

            bool isMistake = ScoreSessionContext.Score < 8;

            if (boxDiag != null)
            {
                boxDiag.SetActive(true);
                var bRt = boxDiag.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.5f, 1f);
                bRt.anchorMax = new Vector2(0.5f, 1f);
                bRt.pivot = new Vector2(0.5f, 1f);
                bRt.anchoredPosition = new Vector2(0, -84);
                bRt.sizeDelta = new Vector2(1460, isMistake ? 260 : 420);
                var img = boxDiag.GetComponent<Image>();
                if (img != null) img.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);
            }

            if (diagnosisBodyText != null)
            {
                diagnosisBodyText.gameObject.SetActive(true);
                var rt = diagnosisBodyText.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(25, 14);
                rt.offsetMax = new Vector2(-25, -14);
                diagnosisBodyText.fontSize = 17;
                diagnosisBodyText.alignment = TextAlignmentOptions.TopLeft;
                diagnosisBodyText.color = new Color(0.92f, 0.95f, 1f);
                diagnosisBodyText.text = Math6Companion.Core.MathTextSanitizer.SanitizeForDisplay(ScoreSessionContext.EssayFeedback);
            }

            if (isMistake && boxRule != null)
            {
                // CHE DO CHEP PHAT TUNG BUOC SAI
                boxRule.SetActive(true);
                var bRt = boxRule.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.5f, 1f);
                bRt.anchorMax = new Vector2(0.5f, 1f);
                bRt.pivot = new Vector2(0.5f, 1f);
                bRt.anchoredPosition = new Vector2(0, -356);
                bRt.sizeDelta = new Vector2(1460, 260);
                var img = boxRule.GetComponent<Image>();
                if (img != null) img.color = new Color(0.18f, 0.12f, 0.08f, 0.95f);

                // Xac dinh danh sach buoc sai can chep phat
                _penaltySteps.Clear();
                _penaltyStepIdx = 0;
                if (ScoreSessionContext.WrongStepsToCopy != null && ScoreSessionContext.WrongStepsToCopy.Count > 0)
                {
                    _penaltySteps.AddRange(ScoreSessionContext.WrongStepsToCopy);
                }
                else if (!string.IsNullOrEmpty(ScoreSessionContext.PenaltyRuleToCopy))
                {
                    _penaltySteps.Add(ScoreSessionContext.PenaltyRuleToCopy);
                }
                else
                {
                    string fallbackRule = CurriculumEssayKnowledgeBase.GetPenaltyRule(
                        "ORDER_OF_OPERATIONS",
                        ScoreSessionContext.CurrentQuestionPrompt,
                        ScoreSessionContext.CurrentQuestionId);
                    _penaltySteps.Add(fallbackRule);
                }

                // Khoa nut giai tiep
                LockNextButton($"CHÉP PHẠT BƯỚC 1/{_penaltySteps.Count} ĐỂ TIẾP TỤC");

                // Hien thi buoc dau tien
                ShowPenaltyStep(boxRule, 0);
            }
            else if (boxRule != null)
            {
                // Học sinh đã giải đúng 100%
                boxRule.SetActive(true);
                var bRt = boxRule.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.5f, 1f);
                bRt.anchorMax = new Vector2(0.5f, 1f);
                bRt.pivot = new Vector2(0.5f, 1f);
                bRt.anchoredPosition = new Vector2(0, -515);
                bRt.sizeDelta = new Vector2(1460, 60);
                var img = boxRule.GetComponent<Image>();
                if (img != null) img.color = new Color(0.10f, 0.22f, 0.18f, 0.95f);

                if (ruleReminderText != null)
                {
                    ruleReminderText.gameObject.SetActive(true);
                    var rtR = ruleReminderText.GetComponent<RectTransform>();
                    rtR.anchorMin = Vector2.zero;
                    rtR.anchorMax = Vector2.one;
                    rtR.offsetMin = new Vector2(25, 10);
                    rtR.offsetMax = new Vector2(-25, -10);
                    ruleReminderText.fontSize = 16;
                    ruleReminderText.alignment = TextAlignmentOptions.Center;
                    ruleReminderText.color = new Color(0.8f, 1f, 0.85f);
                    ruleReminderText.text = "<b>Chúc mừng em!</b> Em đã nắm vững các quy tắc toán học và hoàn thành bài giải xuất sắc!";
                }

                var existingInput = boxRule.transform.Find("PenaltyInputField");
                if (existingInput != null) existingInput.gameObject.SetActive(false);
                var existingStatus = boxRule.transform.Find("PenaltyStatusText");
                if (existingStatus != null) existingStatus.gameObject.SetActive(false);
            }

            if (boxAnalog != null)
            {
                boxAnalog.SetActive(true);
                var bRt = boxAnalog.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.5f, 1f);
                bRt.anchorMax = new Vector2(0.5f, 1f);
                bRt.pivot = new Vector2(0.5f, 1f);
                bRt.anchoredPosition = new Vector2(0, isMistake ? -590 : -520);
                bRt.sizeDelta = new Vector2(1460, 75);
                var img = boxAnalog.GetComponent<Image>();
                if (img != null) img.color = new Color(0.08f, 0.18f, 0.14f, 0.95f);
            }

            if (analogousProblemText != null)
            {
                analogousProblemText.gameObject.SetActive(true);
                var rt = analogousProblemText.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(25, 8);
                rt.offsetMax = new Vector2(-25, -8);
                analogousProblemText.fontSize = 16;
                analogousProblemText.color = new Color(0.95f, 0.85f, 0.4f);
                analogousProblemText.alignment = TextAlignmentOptions.TopLeft;
                analogousProblemText.text = !string.IsNullOrEmpty(ScoreSessionContext.RemedialExercise)
                    ? $"<b>[Gợi ý của Thầy Minh]:</b> {Math6Companion.Core.MathTextSanitizer.SanitizeForDisplay(ScoreSessionContext.RemedialExercise)}"
                    : "<b>[Lời khuyên của Thầy Minh]:</b> Sau khi chép phạt quy tắc, em hãy bấm nút bên dưới để giải tiếp từ bước sai cho đến khi chính xác nhé!";
            }

            // Nút GIẢI LẠI CÂU NÀY / GIẢI TIẾP TỪ DÒNG BỊ SAI
            if (nextWrongQuestionButton != null)
            {
                nextWrongQuestionButton.gameObject.SetActive(true);
                var rtBtn = nextWrongQuestionButton.GetComponent<RectTransform>();
                rtBtn.anchorMin = new Vector2(0.5f, 0f);
                rtBtn.anchorMax = new Vector2(0.5f, 0f);
                rtBtn.pivot = new Vector2(0.5f, 0f);
                rtBtn.anchoredPosition = new Vector2(0, 36);
                rtBtn.sizeDelta = new Vector2(620, 58);

                if (!isMistake)
                {
                    nextWrongQuestionButton.interactable = true;
                    var img = nextWrongQuestionButton.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.12f, 0.65f, 0.42f);

                    var btnTxt = nextWrongQuestionButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnTxt != null)
                    {
                        btnTxt.text = "LUYỆN TẬP CÂU TIẾP THEO";
                        btnTxt.fontSize = 19;
                        btnTxt.fontStyle = FontStyles.Bold;
                    }
                }

                nextWrongQuestionButton.onClick.RemoveAllListeners();
                nextWrongQuestionButton.onClick.AddListener(() =>
                {
                    PlayClick();
                    if (!string.IsNullOrEmpty(ScoreSessionContext.CurrentQuestionId))
                    {
                        PlayerPrefs.SetString("Retry_Essay_Question_ID", ScoreSessionContext.CurrentQuestionId);
                    }
                    if (!string.IsNullOrEmpty(ScoreSessionContext.CurrentQuestionPrompt))
                    {
                        PlayerPrefs.SetString("Retry_Essay_Question_Prompt", ScoreSessionContext.CurrentQuestionPrompt);
                    }
                    PlayerPrefs.SetString("Retry_Essay_Correct_Lines", ScoreSessionContext.RetryCorrectLines ?? "");
                    PlayerPrefs.SetInt("Retry_Essay_First_Wrong_Index", ScoreSessionContext.FirstWrongLineIndex);
                    if (!string.IsNullOrEmpty(ScoreSessionContext.RetryWrongLineHint))
                    {
                        PlayerPrefs.SetString("Retry_Essay_Wrong_Hint", ScoreSessionContext.RetryWrongLineHint);
                    }
                    PlayerPrefs.Save();
                    SceneManager.LoadScene("06_EssayPractice");
                });
            }

            if (prevWrongQuestionButton != null) prevWrongQuestionButton.gameObject.SetActive(false);
            if (pageIndicatorText != null) pageIndicatorText.gameObject.SetActive(false);
        }

        private void LockNextButton(string label)
        {
            if (nextWrongQuestionButton == null) return;
            nextWrongQuestionButton.interactable = false;
            var btnImg = nextWrongQuestionButton.GetComponent<Image>();
            if (btnImg != null) btnImg.color = new Color(0.35f, 0.4f, 0.45f);
            var btnTxt = nextWrongQuestionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTxt != null) { btnTxt.text = label; btnTxt.fontSize = 17; btnTxt.fontStyle = FontStyles.Bold; }
        }

        private void UnlockNextButton()
        {
            if (nextWrongQuestionButton == null) return;
            nextWrongQuestionButton.interactable = true;
            var btnImg = nextWrongQuestionButton.GetComponent<Image>();
            if (btnImg != null) btnImg.color = new Color(0.12f, 0.65f, 0.42f);
            var btnTxt = nextWrongQuestionButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTxt != null) { btnTxt.text = "GIẢI TIẾP TỪ DÒNG BỊ SAI (ĐÃ LƯU BƯỚC ĐÚNG)"; btnTxt.fontSize = 17; }
        }

        private void ShowPenaltyStep(GameObject boxRule, int stepIdx)
        {
            if (_penaltySteps == null || stepIdx >= _penaltySteps.Count) return;
            int total = _penaltySteps.Count;
            string stepText = _penaltySteps[stepIdx];
            if (ruleReminderText != null)
            {
                ruleReminderText.gameObject.SetActive(true);
                var rtR = ruleReminderText.GetComponent<RectTransform>();
                rtR.anchorMin = new Vector2(0.5f, 1f); rtR.anchorMax = new Vector2(0.5f, 1f);
                rtR.pivot = new Vector2(0.5f, 1f);
                rtR.anchoredPosition = new Vector2(0, -14);
                rtR.sizeDelta = new Vector2(1400, 52);
                ruleReminderText.fontSize = 16;
                ruleReminderText.alignment = TextAlignmentOptions.TopLeft;
                ruleReminderText.color = new Color(1f, 0.85f, 0.4f);
                ruleReminderText.text = $"<b>[CHÉP PHẠT BƯỚC {stepIdx + 1}/{total}]:</b>  \"{Math6Companion.Core.MathTextSanitizer.SanitizeForDisplay(stepText)}\"";
            }
            TextMeshProUGUI penaltyStatus;
            Button checkBtn;
            Button autoFillBtn;
            TMP_InputField penaltyInput = EnsurePenaltyInputField(boxRule, stepText, out penaltyStatus, out checkBtn, out autoFillBtn);
            penaltyInput.text = "";
            penaltyStatus.text = $"<color=#f39c12>Hãy gõ lại nội dung bước {stepIdx + 1} ở trên rồi nhấn Enter hoặc nút 'Xác Nhận Bước'.</color>";
            LockNextButton($"CHÉP PHẠT BƯỚC {stepIdx + 1}/{total} ĐỂ TIẾP TỤC");

            bool isAdvancing = false;
            void DoAdvance()
            {
                if (isAdvancing) return;
                isAdvancing = true;
                if (stepIdx + 1 < total)
                {
                    _penaltyStepIdx = stepIdx + 1;
                    penaltyStatus.text = $"<color=#2ecc71>[OK] Bước {stepIdx + 1} đã chép đúng! Chuyển sang bước {stepIdx + 2}/{total}...</color>";
                    StartCoroutine(DelayThenNextPenaltyStep(boxRule, stepIdx + 1, 0.8f));
                }
                else
                {
                    penaltyStatus.text = "<color=#2ecc71>[OK] Tuyệt vời! Đã chép phạt xong tất cả bước sai. Bấm nút bên dưới để giải tiếp!</color>";
                    UnlockNextButton();
                }
            }

            // 1. Khi bấm nút [Điền Mẫu Bước Này]
            if (autoFillBtn != null)
            {
                autoFillBtn.onClick.RemoveAllListeners();
                autoFillBtn.onClick.AddListener(() =>
                {
                    PlayClick();
                    penaltyInput.text = stepText;
                    DoAdvance();
                });
            }

            // 2. Khi bấm nút [Xác Nhận Bước]
            if (checkBtn != null)
            {
                checkBtn.onClick.RemoveAllListeners();
                checkBtn.onClick.AddListener(() =>
                {
                    PlayClick();
                    bool match = CheckPenaltyRuleMatch(penaltyInput.text, stepText);
                    if (match)
                    {
                        DoAdvance();
                    }
                    else
                    {
                        penaltyStatus.text = $"<color=#e74c3c>[!] Em cần gõ đầy đủ nội dung bước {stepIdx + 1} hoặc bấm nút 'Điền Mẫu Bước Này'.</color>";
                        LockNextButton($"CHÉP PHẠT BƯỚC {stepIdx + 1}/{total} ĐỂ TIẾP TỤC");
                    }
                });
            }

            // 3. Khi bấm Enter trong ô nhập
            penaltyInput.onSubmit.RemoveAllListeners();
            penaltyInput.onSubmit.AddListener((val) =>
            {
                bool match = CheckPenaltyRuleMatch(val, stepText);
                if (match)
                {
                    DoAdvance();
                }
                else
                {
                    penaltyStatus.text = $"<color=#e74c3c>[!] Em cần gõ đầy đủ nội dung bước {stepIdx + 1} hoặc bấm nút 'Điền Mẫu Bước Này'.</color>";
                    LockNextButton($"CHÉP PHẠT BƯỚC {stepIdx + 1}/{total} ĐỂ TIẾP TỤC");
                }
            });

            // 4. Khi đang gõ: CHỈ cập nhật trạng thái gợi ý, KHÔNG bao giờ tự ý nhảy bước khi đang gõ dở!
            penaltyInput.onValueChanged.RemoveAllListeners();
            penaltyInput.onValueChanged.AddListener((val) =>
            {
                if (isAdvancing) return;
                bool match = CheckPenaltyRuleMatch(val, stepText);
                if (match)
                {
                    penaltyStatus.text = $"<color=#2ecc71>[OK] Nội dung bước {stepIdx + 1} đã chuẩn xác! Hãy bấm Enter hoặc nút 'Xác Nhận Bước' để tiếp tục.</color>";
                }
                else
                {
                    penaltyStatus.text = $"<color=#f39c12>Đang nhập bước {stepIdx + 1}/{total}... (Nhấn Enter hoặc bấm 'Xác Nhận Bước' khi gõ xong)</color>";
                }
            });
        }

        private System.Collections.IEnumerator DelayThenNextPenaltyStep(GameObject boxRule, int nextIdx, float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            if (this != null && gameObject != null)
                ShowPenaltyStep(boxRule, nextIdx);
        }

        private void DisplayWrongQuestion(int index)
        {
            int total = ScoreSessionContext.WrongQuestionPrompts.Count;
            if (index < 0 || index >= total) return;

            string prompt = ScoreSessionContext.WrongQuestionPrompts[index];
            string wrongAns = ScoreSessionContext.WrongAnswers.Count > index ? ScoreSessionContext.WrongAnswers[index] : "";
            string correctAns = ScoreSessionContext.CorrectAnswers.Count > index ? ScoreSessionContext.CorrectAnswers[index] : "";
            string explanation = ScoreSessionContext.Explanations.Count > index ? ScoreSessionContext.Explanations[index] : "";

            if (tutorGreetingText != null)
                tutorGreetingText.text = "Chào em! Thầy Minh hướng dẫn em câu này nhé:";

            var cardRt = (questionTitleText != null && questionTitleText.transform.parent != null) 
                ? questionTitleText.transform.parent.GetComponent<RectTransform>() 
                : null;
            if (cardRt != null)
            {
                cardRt.anchorMin = new Vector2(0.5f, 1f);
                cardRt.anchorMax = new Vector2(0.5f, 1f);
                cardRt.pivot = new Vector2(0.5f, 1f);
                cardRt.anchoredPosition = new Vector2(0, -96);
                cardRt.sizeDelta = new Vector2(1540, 840);
            }

            if (questionTitleText != null)
            {
                questionTitleText.text = $"<b>Câu hỏi:</b> {prompt}";
                questionTitleText.fontSize = 20;
                var rtQ = questionTitleText.GetComponent<RectTransform>();
                if (rtQ != null)
                {
                    rtQ.anchorMin = new Vector2(0.5f, 1f);
                    rtQ.anchorMax = new Vector2(0.5f, 1f);
                    rtQ.pivot = new Vector2(0.5f, 1f);
                    rtQ.anchoredPosition = new Vector2(0, -22);
                    rtQ.sizeDelta = new Vector2(1460, 52);
                }
            }

            var boxDiag = diagnosisBodyText != null ? diagnosisBodyText.transform.parent.gameObject : null;
            var boxRule = ruleReminderText != null ? ruleReminderText.transform.parent.gameObject : null;
            var boxAnalog = analogousProblemText != null ? analogousProblemText.transform.parent.gameObject : null;

            if (boxDiag != null)
            {
                boxDiag.SetActive(true);
                var bRt = boxDiag.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.5f, 1f);
                bRt.anchorMax = new Vector2(0.5f, 1f);
                bRt.pivot = new Vector2(0.5f, 1f);
                bRt.anchoredPosition = new Vector2(0, -84);
                bRt.sizeDelta = new Vector2(1460, 230);
            }

            if (diagnosisBodyText != null)
            {
                diagnosisBodyText.fontSize = 17;
                diagnosisBodyText.text = $"Em đã chọn: '{wrongAns}' (Chưa chính xác).\nĐáp án đúng là: '{correctAns}'.\n\nGiải thích chi tiết: {explanation}";
            }

            if (boxRule != null)
            {
                boxRule.SetActive(true);
                var bRt = boxRule.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.5f, 1f);
                bRt.anchorMax = new Vector2(0.5f, 1f);
                bRt.pivot = new Vector2(0.5f, 1f);
                bRt.anchoredPosition = new Vector2(0, -328);
                bRt.sizeDelta = new Vector2(1460, 160);
                var existingInput = boxRule.transform.Find("PenaltyInputField");
                if (existingInput != null) existingInput.gameObject.SetActive(false);
                var existingStatus = boxRule.transform.Find("PenaltyStatusText");
                if (existingStatus != null) existingStatus.gameObject.SetActive(false);
            }

            if (ruleReminderText != null)
            {
                ruleReminderText.fontSize = 16;
                ruleReminderText.text = "Lưu ý: Đọc kỹ đề bài và đối chiếu với lời giải chi tiết ở trên để nắm vững phương pháp giải nhé!";
            }

            if (boxAnalog != null)
            {
                boxAnalog.SetActive(true);
                var bRt = boxAnalog.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.5f, 1f);
                bRt.anchorMax = new Vector2(0.5f, 1f);
                bRt.pivot = new Vector2(0.5f, 1f);
                bRt.anchoredPosition = new Vector2(0, -502);
                bRt.sizeDelta = new Vector2(1460, 100);
            }

            if (analogousProblemText != null)
            {
                analogousProblemText.fontSize = 16;
                analogousProblemText.text = "Em hãy ghi nhớ quy tắc này để không bị nhầm lẫn trong lần làm tiếp theo nhé!";
            }

            if (pageIndicatorText != null)
                pageIndicatorText.text = $"Câu sai {index + 1}/{total}";

            if (prevWrongQuestionButton != null) prevWrongQuestionButton.interactable = index > 0;
            if (nextWrongQuestionButton != null) nextWrongQuestionButton.interactable = index < total - 1;
        }

        private void OnNextClicked()
        {
            PlayClick();
            if (currentWrongIndex < ScoreSessionContext.WrongQuestionPrompts.Count - 1)
            {
                currentWrongIndex++;
                DisplayWrongQuestion(currentWrongIndex);
            }
        }

        private void OnPrevClicked()
        {
            PlayClick();
            if (currentWrongIndex > 0)
            {
                currentWrongIndex--;
                DisplayWrongQuestion(currentWrongIndex);
            }
        }

        private void NavigateScene(string sceneName)
        {
            PlayClick();
            SceneManager.LoadScene(sceneName);
        }

        private TMP_InputField EnsurePenaltyInputField(GameObject parent, string ruleToCopy, out TextMeshProUGUI statusText, out Button checkBtn, out Button autoFillBtn)
        {
            var existingInput = parent.transform.Find("PenaltyInputField");
            var existingStatus = parent.transform.Find("PenaltyStatusText");
            var existingBtn = parent.transform.Find("Btn_AutoFillRule");
            var existingCheckBtn = parent.transform.Find("Btn_CheckStep");

            if (existingInput != null && existingStatus != null && existingBtn != null && existingCheckBtn != null)
            {
                existingInput.gameObject.SetActive(true);
                existingStatus.gameObject.SetActive(true);
                existingBtn.gameObject.SetActive(true);
                existingCheckBtn.gameObject.SetActive(true);

                var inRtExisting = existingInput.GetComponent<RectTransform>();
                inRtExisting.anchoredPosition = new Vector2(-240, -78);
                inRtExisting.sizeDelta = new Vector2(920, 50);

                var chkRt = existingCheckBtn.GetComponent<RectTransform>();
                chkRt.anchoredPosition = new Vector2(345, -78);
                chkRt.sizeDelta = new Vector2(210, 50);

                var autoRt = existingBtn.GetComponent<RectTransform>();
                autoRt.anchoredPosition = new Vector2(580, -78);
                autoRt.sizeDelta = new Vector2(220, 50);

                statusText = existingStatus.GetComponent<TextMeshProUGUI>();
                checkBtn = existingCheckBtn.GetComponent<Button>();
                autoFillBtn = existingBtn.GetComponent<Button>();
                return existingInput.GetComponent<TMP_InputField>();
            }

            // Xoa cac layout cu neu co
            if (existingInput != null) Destroy(existingInput.gameObject);
            if (existingStatus != null) Destroy(existingStatus.gameObject);
            if (existingBtn != null) Destroy(existingBtn.gameObject);
            if (existingCheckBtn != null) Destroy(existingCheckBtn.gameObject);

            // 1. Input Field Root
            var inputGo = new GameObject("PenaltyInputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(parent.transform, false);
            var inRt = inputGo.GetComponent<RectTransform>();
            inRt.anchorMin = new Vector2(0.5f, 1f);
            inRt.anchorMax = new Vector2(0.5f, 1f);
            inRt.pivot = new Vector2(0.5f, 1f);
            inRt.anchoredPosition = new Vector2(-240, -78);
            inRt.sizeDelta = new Vector2(920, 50);

            var img = inputGo.GetComponent<Image>();
            img.color = new Color(0.06f, 0.09f, 0.14f, 0.95f);

            var inputField = inputGo.GetComponent<TMP_InputField>();

            // Text Area
            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputGo.transform, false);
            var taRt = textArea.GetComponent<RectTransform>();
            taRt.anchorMin = Vector2.zero;
            taRt.anchorMax = Vector2.one;
            taRt.offsetMin = new Vector2(16, 6);
            taRt.offsetMax = new Vector2(-16, -6);

            // Placeholder
            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phGo.transform.SetParent(textArea.transform, false);
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = Vector2.zero;
            phRt.offsetMax = Vector2.zero;
            var phText = phGo.GetComponent<TextMeshProUGUI>();
            phText.text = "Gõ nội dung bước sai ở trên rồi nhấn Enter hoặc bấm [Xác Nhận]...";
            phText.fontSize = 15;
            phText.fontStyle = FontStyles.Italic;
            phText.color = new Color(0.6f, 0.65f, 0.75f, 0.6f);
            phText.alignment = TextAlignmentOptions.MidlineLeft;

            // Text
            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(textArea.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var mainText = txtGo.GetComponent<TextMeshProUGUI>();
            mainText.fontSize = 16;
            mainText.color = Color.white;
            mainText.alignment = TextAlignmentOptions.MidlineLeft;

            inputField.textViewport = taRt;
            inputField.textComponent = mainText;
            inputField.placeholder = phText;
            inputField.targetGraphic = img;

            // 2. Nút [Xác Nhận Bước] (Màu xanh lá)
            var checkBtnGo = new GameObject("Btn_CheckStep", typeof(RectTransform), typeof(Image), typeof(Button));
            checkBtnGo.transform.SetParent(parent.transform, false);
            var chkRtNew = checkBtnGo.GetComponent<RectTransform>();
            chkRtNew.anchorMin = new Vector2(0.5f, 1f);
            chkRtNew.anchorMax = new Vector2(0.5f, 1f);
            chkRtNew.pivot = new Vector2(0.5f, 1f);
            chkRtNew.anchoredPosition = new Vector2(345, -78);
            chkRtNew.sizeDelta = new Vector2(210, 50);

            var chkImg = checkBtnGo.GetComponent<Image>();
            chkImg.color = new Color(0.14f, 0.65f, 0.38f, 1f);

            var chkTxtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            chkTxtGo.transform.SetParent(checkBtnGo.transform, false);
            var chkTxtRt = chkTxtGo.GetComponent<RectTransform>();
            chkTxtRt.anchorMin = Vector2.zero;
            chkTxtRt.anchorMax = Vector2.one;
            chkTxtRt.offsetMin = Vector2.zero;
            chkTxtRt.offsetMax = Vector2.zero;
            var chkTmp = chkTxtGo.GetComponent<TextMeshProUGUI>();
            chkTmp.text = "<b>[OK] Xác Nhận Bước</b>";
            chkTmp.fontSize = 15;
            chkTmp.alignment = TextAlignmentOptions.Center;
            chkTmp.color = Color.white;

            checkBtn = checkBtnGo.GetComponent<Button>();

            // 3. Nút [Điền Mẫu Bước Này] (Màu vàng cam)
            var autoFillBtnGo = new GameObject("Btn_AutoFillRule", typeof(RectTransform), typeof(Image), typeof(Button));
            autoFillBtnGo.transform.SetParent(parent.transform, false);
            var autoRtNew = autoFillBtnGo.GetComponent<RectTransform>();
            autoRtNew.anchorMin = new Vector2(0.5f, 1f);
            autoRtNew.anchorMax = new Vector2(0.5f, 1f);
            autoRtNew.pivot = new Vector2(0.5f, 1f);
            autoRtNew.anchoredPosition = new Vector2(580, -78);
            autoRtNew.sizeDelta = new Vector2(220, 50);

            var autoImg = autoFillBtnGo.GetComponent<Image>();
            autoImg.color = new Color(0.85f, 0.55f, 0.12f, 0.95f);

            var autoTxtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            autoTxtGo.transform.SetParent(autoFillBtnGo.transform, false);
            var autoTxtRt = autoTxtGo.GetComponent<RectTransform>();
            autoTxtRt.anchorMin = Vector2.zero;
            autoTxtRt.anchorMax = Vector2.one;
            autoTxtRt.offsetMin = Vector2.zero;
            autoTxtRt.offsetMax = Vector2.zero;
            var autoTmp = autoTxtGo.GetComponent<TextMeshProUGUI>();
            autoTmp.text = "<b>Điền Mẫu Bước Này</b>";
            autoTmp.fontSize = 15;
            autoTmp.alignment = TextAlignmentOptions.Center;
            autoTmp.color = Color.white;

            autoFillBtn = autoFillBtnGo.GetComponent<Button>();

            // 4. Status Text
            var statusGo = new GameObject("PenaltyStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusGo.transform.SetParent(parent.transform, false);
            var statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.5f, 1f);
            statusRt.anchorMax = new Vector2(0.5f, 1f);
            statusRt.pivot = new Vector2(0.5f, 1f);
            statusRt.anchoredPosition = new Vector2(0, -145);
            statusRt.sizeDelta = new Vector2(1400, 32);
            statusText = statusGo.GetComponent<TextMeshProUGUI>();
            statusText.fontSize = 15;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = new Color(0.95f, 0.4f, 0.4f);

            return inputField;
        }

        private static bool CheckPenaltyRuleMatch(string typed, string targetRule)
        {
            if (string.IsNullOrEmpty(targetRule)) return true;
            if (string.IsNullOrEmpty(typed)) return false;

            string normTyped = NormalizeRuleText(typed);
            string normTarget = NormalizeRuleText(targetRule);

            if (string.IsNullOrEmpty(normTyped)) return false;

            // 1. Khớp hoàn toàn hoặc học sinh gõ nguyên văn câu đích
            if (normTyped == normTarget || normTyped.Contains(normTarget))
            {
                return true;
            }

            // 2. Nếu đề mục có dấu hai chấm 'Bước 1: ...', cho phép học sinh gõ phần công thức/lời giải sau dấu hai chấm
            int colonIdx = targetRule.IndexOf(':');
            if (colonIdx >= 0 && colonIdx < targetRule.Length - 1)
            {
                string formulaPart = targetRule.Substring(colonIdx + 1).Trim();
                string normFormula = NormalizeRuleText(formulaPart);
                if (!string.IsNullOrEmpty(normFormula))
                {
                    if (normTyped == normFormula || normTyped.Contains(normFormula))
                    {
                        return true;
                    }
                    if (normTyped.Length >= normFormula.Length * 0.80f && normFormula.Contains(normTyped))
                    {
                        return true;
                    }
                }
            }

            // 3. Khớp tỷ lệ từ vựng cao (chỉ chấp nhận khi học sinh đã gõ tối thiểu 75% độ dài VÀ khớp >= 80% từ khóa)
            if (normTyped.Length >= normTarget.Length * 0.75f)
            {
                string[] targetWords = normTarget.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                int matchedWords = 0;
                foreach (var w in targetWords)
                {
                    if (normTyped.Contains(w)) matchedWords++;
                }

                float matchRatio = targetWords.Length > 0 ? (float)matchedWords / targetWords.Length : 0f;
                if (matchRatio >= 0.80f) return true;
            }

            return false;
        }

        private static string NormalizeRuleText(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char c in s.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] != ' ')
                        sb.Append(' ');
                }
            }
            return sb.ToString().Trim();
        }

        private void PlayClick()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }
    }
}
