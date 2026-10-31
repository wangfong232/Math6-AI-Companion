using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Math6Companion.Gameplay3D
{
    /// <summary>
    /// Thử thách Trục Đối Xứng Chùa Một Cột (Hình học trực quan lớp 6).
    /// </summary>
    public class SymmetryPagodaPuzzle : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI puzzleTitleText;
        [SerializeField] private TextMeshProUGUI puzzleDescText;
        [SerializeField] private Button btnOptionA;
        [SerializeField] private Button btnOptionB;
        [SerializeField] private Button btnOptionC;
        [SerializeField] private Button btnOptionD;
        [SerializeField] private TextMeshProUGUI textOptionA;
        [SerializeField] private TextMeshProUGUI textOptionB;
        [SerializeField] private TextMeshProUGUI textOptionC;
        [SerializeField] private TextMeshProUGUI textOptionD;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private Button btnClose;

        [Header("Rewards")]
        [SerializeField] private int rewardCoins = 50;
        [SerializeField] private int rewardStars = 2;

        private int _correctIndex = 0; // 0=A

        private void Start()
        {
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(() =>
                {
                    if (VillageHUDManager.Instance != null) VillageHUDManager.Instance.CloseAllPanels();
                });
            }

            if (btnOptionA != null) btnOptionA.onClick.AddListener(() => CheckAnswer(0));
            if (btnOptionB != null) btnOptionB.onClick.AddListener(() => CheckAnswer(1));
            if (btnOptionC != null) btnOptionC.onClick.AddListener(() => CheckAnswer(2));
            if (btnOptionD != null) btnOptionD.onClick.AddListener(() => CheckAnswer(3));

            InitPuzzle();
        }

        public void InitPuzzle()
        {
            if (puzzleTitleText != null) puzzleTitleText.text = "[Chua] BÍ MẬT TRỤC ĐỐI XỨNG CHÙA MỘT CỘT";
            if (puzzleDescText != null)
            {
                puzzleDescText.text = "Kiến trúc Chùa Một Cột và cây cầu đá soi bóng xuống mặt hồ sen tạo nên <b>Trục Đối Xứng tuyệt đối</b> qua đường thẳng giữa cầu (trục tung Y: x = 0).\n\n" +
                                      "Bên trái hồ sen đang có một cụm đèn hoa sen tại vị trí tọa độ <b>(-3, 2)</b>.\n" +
                                      "Hỏi để hai bên bờ hồ đối xứng nhau qua trục Y, bạn cần đặt cụm đèn bên phải tại tọa độ nào?";
            }

            if (textOptionA != null) textOptionA.text = "A. (3, 2) [Đổi dấu x, giữ nguyên y]";
            if (textOptionB != null) textOptionB.text = "B. (-3, -2)";
            if (textOptionC != null) textOptionC.text = "C. (3, -2)";
            if (textOptionD != null) textOptionD.text = "D. (0, 2)";

            _correctIndex = 0; // A

            if (feedbackText != null) feedbackText.text = "Hãy chọn vị trí đối xứng chính xác để thắp sáng hồ sen!";
        }

        private void CheckAnswer(int index)
        {
            if (index == _correctIndex)
            {
                if (feedbackText != null)
                {
                    feedbackText.text = "<color=#2ECC71><b>XUẤT SẮC!</b> Hai điểm đối xứng qua trục tung Y sẽ có tung độ y bằng nhau và hoành độ x đối dấu nhau: (-3, 2) đối xứng với (3, 2)! Chùa Một Cột đã tỏa ánh hào quang!</color>";
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
                    feedbackText.text = "<color=#E74C3C><b>Chưa đúng!</b> Gợi ý: Khi lấy đối xứng qua trục thẳng đứng Y, khoảng cách từ 2 điểm tới trục phải bằng nhau, do đó x đối dấu còn y giữ nguyên.</color>";
                }
            }
        }
    }
}
