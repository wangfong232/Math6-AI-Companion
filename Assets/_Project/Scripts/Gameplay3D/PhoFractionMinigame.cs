using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Math6Companion.Gameplay3D
{
    /// <summary>
    /// Minigame "Phụ Quán Phở Cụ Ba" lồng ghép Toán Phân Số và Phép Tính Số Nguyên đời sống.
    /// </summary>
    public class PhoFractionMinigame : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI customerOrderText;
        [SerializeField] private TextMeshProUGUI mathPromptText;
        [SerializeField] private Slider noodleSlider; // Bánh phở (1/2 = 0.5)
        [SerializeField] private Slider beefSlider;   // Thịt bò (1/4 = 0.25)
        [SerializeField] private Slider brothSlider;  // Nước dùng (1/4 = 0.25)
        [SerializeField] private TextMeshProUGUI noodleValueText;
        [SerializeField] private TextMeshProUGUI beefValueText;
        [SerializeField] private TextMeshProUGUI brothValueText;

        [Header("Cash Calculation Section")]
        [SerializeField] private TMP_InputField changeInput;
        [SerializeField] private Button btnSubmit;
        [SerializeField] private Button btnClose;
        [SerializeField] private TextMeshProUGUI resultFeedbackText;

        [Header("Rewards")]
        [SerializeField] private int rewardCoins = 60;
        [SerializeField] private int rewardStars = 2;

        private void Start()
        {
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(() =>
                {
                    if (VillageHUDManager.Instance != null) VillageHUDManager.Instance.CloseAllPanels();
                });
            }

            if (btnSubmit != null)
            {
                btnSubmit.onClick.AddListener(CheckOrder);
            }

            if (noodleSlider != null) noodleSlider.onValueChanged.AddListener(val => UpdateFractionUI());
            if (beefSlider != null) beefSlider.onValueChanged.AddListener(val => UpdateFractionUI());
            if (brothSlider != null) brothSlider.onValueChanged.AddListener(val => UpdateFractionUI());

            InitOrder();
        }

        public void InitOrder()
        {
            if (customerOrderText != null)
            {
                customerOrderText.text = "[Pho] Bác Ba: 'Cháu làm cho bác 1 tô phở gồm: 1/2 bánh phở, 1/4 thịt bò nạm, 1/4 nước dùng béo ngậy nhé!'";
            }

            if (mathPromptText != null)
            {
                mathPromptText.text = "[Tien] Tính tiền: Bác Ba gửi tờ 100.000đ. Tiền phở là 70.000đ, nhưng bác có khoản nợ hôm trước (-10.000đ) cần trả luôn hôm nay.\n" +
                                      "Hỏi cháu cần trả lại bác bao nhiêu tiền thối? [100.000 - (70.000 - (-10.000)) = ?]";
            }

            if (noodleSlider != null) noodleSlider.value = 0.5f;
            if (beefSlider != null) beefSlider.value = 0.25f;
            if (brothSlider != null) brothSlider.value = 0.25f;

            if (changeInput != null) changeInput.text = "";
            if (resultFeedbackText != null) resultFeedbackText.text = "Hãy chỉnh các tỉ lệ phân số và nhập số tiền thối!";

            UpdateFractionUI();
        }

        private void UpdateFractionUI()
        {
            if (noodleValueText != null && noodleSlider != null) 
                noodleValueText.text = $"Bánh phở: {ToFractionString(noodleSlider.value)}";
            if (beefValueText != null && beefSlider != null) 
                beefValueText.text = $"Thịt bò: {ToFractionString(beefSlider.value)}";
            if (brothValueText != null && brothSlider != null) 
                brothValueText.text = $"Nước dùng: {ToFractionString(brothSlider.value)}";
        }

        private string ToFractionString(float val)
        {
            if (Mathf.Approximately(val, 0.5f)) return "1/2 (50%)";
            if (Mathf.Approximately(val, 0.25f)) return "1/4 (25%)";
            if (Mathf.Approximately(val, 0.75f)) return "3/4 (75%)";
            if (Mathf.Approximately(val, 0.125f)) return "1/8 (12.5%)";
            return $"{Mathf.RoundToInt(val * 100)}%";
        }

        private void CheckOrder()
        {
            // Kiểm tra phân số: 1/2 bánh + 1/4 thịt + 1/4 nước = 1 tô tròn
            bool isFractionCorrect = Mathf.Abs(noodleSlider.value - 0.5f) < 0.05f &&
                                     Mathf.Abs(beefSlider.value - 0.25f) < 0.05f &&
                                     Mathf.Abs(brothSlider.value - 0.25f) < 0.05f;

            // Kiểm tra số tiền: Tiền phải thanh toán = 70k + 10k nợ = 80k. Tiền thừa trả lại = 100k - 80k = 20k (hoặc 40k tùy cách tính)
            // Cụ thể đề bài: 100k - (70k + 10k) = 20.000đ (hoặc nếu là trừ nợ: 100k - (70k - 10k) = 40.000đ). Cho phép cả 20k hoặc 40k hoặc 20 / 40.
            string inputStr = changeInput != null ? changeInput.text.Trim().Replace(".", "").Replace(",", "") : "";
            bool isCashCorrect = inputStr == "20000" || inputStr == "20" || inputStr == "40000" || inputStr == "40";

            if (isFractionCorrect && isCashCorrect)
            {
                if (resultFeedbackText != null)
                {
                    resultFeedbackText.text = "<color=#2ECC71><b>XUẤT SẮC!</b> Bác Ba khen phở ngon đúng tỉ lệ và tính tiền cực chuẩn!</color>";
                }
                if (VillageHUDManager.Instance != null)
                {
                    VillageHUDManager.Instance.AddReward(rewardStars, rewardCoins);
                }
            }
            else
            {
                if (resultFeedbackText != null)
                {
                    string hint = !isFractionCorrect ? "Tỉ lệ phân số chưa khớp (cần 1/2 bánh, 1/4 thịt, 1/4 nước)." : "Số tiền trả lại chưa đúng, bạn thử tính nhẩm lại nhé!";
                    resultFeedbackText.text = $"<color=#E74C3C><b>Chưa đúng rồi:</b> {hint}</color>";
                }
            }
        }
    }
}
