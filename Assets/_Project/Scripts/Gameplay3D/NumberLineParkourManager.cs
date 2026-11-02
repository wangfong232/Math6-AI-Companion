using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Math6Companion.Gameplay3D
{
    /// <summary>
    /// Thử thách Trục Số & Cao Độ Ruộng Bậc Thang (Số Nguyên Z).
    /// </summary>
    public class NumberLineParkourManager : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI questionTitleText;
        [SerializeField] private TextMeshProUGUI questionContentText;
        [SerializeField] private Button btnChoiceA;
        [SerializeField] private Button btnChoiceB;
        [SerializeField] private Button btnChoiceC;
        [SerializeField] private Button btnChoiceD;
        [SerializeField] private TextMeshProUGUI choiceTextA;
        [SerializeField] private TextMeshProUGUI choiceTextB;
        [SerializeField] private TextMeshProUGUI choiceTextC;
        [SerializeField] private TextMeshProUGUI choiceTextD;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private Button btnClose;

        [Header("Rewards")]
        [SerializeField] private int rewardCoins = 50;
        [SerializeField] private int rewardStars = 2;

        private int _correctIndex = 1; // 0=A, 1=B, 2=C, 3=D

        private void Start()
        {
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(() =>
                {
                    if (VillageHUDManager.Instance != null) VillageHUDManager.Instance.CloseAllModals();
                });
            }

            if (btnChoiceA != null) btnChoiceA.onClick.AddListener(() => SelectAnswer(0));
            if (btnChoiceB != null) btnChoiceB.onClick.AddListener(() => SelectAnswer(1));
            if (btnChoiceC != null) btnChoiceC.onClick.AddListener(() => SelectAnswer(2));
            if (btnChoiceD != null) btnChoiceD.onClick.AddListener(() => SelectAnswer(3));

            InitQuestion();
        }

        public void InitQuestion()
        {
            if (questionTitleText != null) questionTitleText.text = "[THỬ THÁCH TRỤC SỐ RUỘNG BẬC THANG]";
            if (questionContentText != null)
            {
                questionContentText.text = "Thửa ruộng bậc thang thứ nhất ở độ cao <b>+3m</b>.\n" +
                                           "Giếng nước tưới tiêu nằm sâu dưới lòng đất ở độ sâu <b>-2m</b>.\n" +
                                           "Bác nông dân cần lắp đường ống dài bao nhiêu mét để bơm nước từ giếng lên thửa ruộng?\n\n" +
                                           "<i>Công thức: Khoảng cách = Điểm cao - Điểm thấp = 3 - (-2) = ?</i>";
            }

            if (choiceTextA != null) choiceTextA.text = "A. 1 mét";
            if (choiceTextB != null) choiceTextB.text = "B. 5 mét";
            if (choiceTextC != null) choiceTextC.text = "C. -5 mét";
            if (choiceTextD != null) choiceTextD.text = "D. 6 mét";

            _correctIndex = 1; // B. 5 mét

            if (feedbackText != null) feedbackText.text = "Hãy chọn đáp án đúng để kích hoạt guồng nước!";
        }

        private void SelectAnswer(int index)
        {
            if (index == _correctIndex)
            {
                if (feedbackText != null)
                {
                    feedbackText.text = "<color=#2ECC71><b>CHÍNH XÁC!</b> 3 - (-2) = 3 + 2 = 5m. Nước đã được bơm lên ruộng bậc thang xanh tốt!</color>";
                }
                if (VillageHUDManager.Instance != null)
                {
                    VillageHUDManager.Instance.AddReward(rewardStars, rewardCoins);
                }
            }
            else
            {
                if (feedbackText != null)
                {
                    feedbackText.text = "<color=#E74C3C><b>Chưa đúng!</b> Nhớ quy tắc: Trừ đi một số âm tương đương với cộng số dương: 3 - (-2) = 3 + 2 = 5m!</color>";
                }
            }
        }
    }
}
