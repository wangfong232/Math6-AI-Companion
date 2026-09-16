using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Math6Companion.UI
{
    public class MathKeypadController : MonoBehaviour
    {
        [Header("Target Input Field")]
        [SerializeField] private TMP_InputField targetInputField;

        [Header("Keypad Containers")]
        [SerializeField] private GameObject keypadPanel;
        [SerializeField] private Button toggleKeypadButton;

        private void Awake()
        {
            if (toggleKeypadButton != null)
            {
                toggleKeypadButton.onClick.AddListener(ToggleKeypad);
            }
        }

        public void SetTargetInputField(TMP_InputField inputField)
        {
            targetInputField = inputField;
        }

        public void ToggleKeypad()
        {
            if (keypadPanel != null)
            {
                keypadPanel.SetActive(!keypadPanel.activeSelf);
            }
        }

      public void InsertSymbol(string symbol)
{
    if (targetInputField == null) return;

    targetInputField.ActivateInputField();

    int caretPos = GetValidCaretPosition();
    string currentText = targetInputField.text ?? "";

    string newText = currentText.Insert(caretPos, symbol);
    targetInputField.text = newText;

    int nextPos = caretPos + symbol.Length;
    
    // Đặt vị trí con trỏ và HỦY bôi đen toàn bộ
    targetInputField.stringPosition = nextPos;
    targetInputField.caretPosition = nextPos;
    targetInputField.selectionStringAnchorPosition = nextPos;
    targetInputField.selectionStringFocusPosition = nextPos;
}

public void InsertTemplate(string prefix, string suffix)
{
    if (targetInputField == null) return;

    targetInputField.ActivateInputField();

    int caretPos = GetValidCaretPosition();
    string currentText = targetInputField.text ?? "";

    string fullInsert = prefix + suffix;
    string newText = currentText.Insert(caretPos, fullInsert);
    targetInputField.text = newText;

    int insidePos = caretPos + prefix.Length;
    
    // Đặt con trỏ vào giữa và HỦY bôi đen
    targetInputField.stringPosition = insidePos;
    targetInputField.caretPosition = insidePos;
    targetInputField.selectionStringAnchorPosition = insidePos;
    targetInputField.selectionStringFocusPosition = insidePos;
}

public void Backspace()
{
    if (targetInputField == null) return;

    targetInputField.ActivateInputField();

    if (string.IsNullOrEmpty(targetInputField.text)) return;

    // Nếu đang có bôi đen thì xóa đúng đoạn bôi đen
    if (targetInputField.selectionStringAnchorPosition != targetInputField.selectionStringFocusPosition)
    {
        int start = Mathf.Min(targetInputField.selectionStringAnchorPosition, targetInputField.selectionStringFocusPosition);
        int length = Mathf.Abs(targetInputField.selectionStringFocusPosition - targetInputField.selectionStringAnchorPosition);
        targetInputField.text = targetInputField.text.Remove(start, length);
        
        targetInputField.stringPosition = start;
        targetInputField.caretPosition = start;
        targetInputField.selectionStringAnchorPosition = start;
        targetInputField.selectionStringFocusPosition = start;
        return;
    }

    int caretPos = GetValidCaretPosition();
    if (caretPos > 0)
    {
        // Kiểm tra xóa cặp ngoặc rỗng liền kề: () hoặc [] hoặc {}
        string currentText = targetInputField.text;
        bool isPair = false;
        if (caretPos < currentText.Length)
        {
            char prev = currentText[caretPos - 1];
            char next = currentText[caretPos];
            if ((prev == '(' && next == ')') || (prev == '[' && next == ']') || (prev == '{' && next == '}'))
            {
                // Xóa cả cặp
                targetInputField.text = currentText.Remove(caretPos - 1, 2);
                int newPos = caretPos - 1;
                targetInputField.stringPosition = newPos;
                targetInputField.caretPosition = newPos;
                targetInputField.selectionStringAnchorPosition = newPos;
                targetInputField.selectionStringFocusPosition = newPos;
                isPair = true;
            }
        }

        if (!isPair)
        {
            targetInputField.text = targetInputField.text.Remove(caretPos - 1, 1);
            int newPos = caretPos - 1;
            targetInputField.stringPosition = newPos;
            targetInputField.caretPosition = newPos;
            targetInputField.selectionStringAnchorPosition = newPos;
            targetInputField.selectionStringFocusPosition = newPos;
        }
    }
}

        public void ClearAll()
        {
            if (targetInputField == null) return;
            targetInputField.text = string.Empty;
            targetInputField.stringPosition = 0;
            targetInputField.caretPosition = 0;
            targetInputField.ActivateInputField();
        }

        // Đảm bảo con trỏ luôn trỏ vào cuối text nếu ô bị mất focus
        private int GetValidCaretPosition()
        {
            int caretPos = targetInputField.stringPosition;
            string text = targetInputField.text ?? "";

            if (caretPos < 0 || caretPos > text.Length)
            {
                caretPos = text.Length;
            }
            return caretPos;
        }
    }
}