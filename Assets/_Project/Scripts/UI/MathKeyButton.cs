using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Math6Companion.UI
{
    [RequireComponent(typeof(Button))]
    public class MathKeyButton : MonoBehaviour
    {
        public enum KeyActionType
        {
            InsertSymbol,     // Chèn ký tự đơn
            InsertTemplate,   // Chèn cặp mẫu như (), []
            Backspace,        // Xóa ký tự
            Clear             // Xóa hết
        }

        [SerializeField] private KeyActionType actionType = KeyActionType.InsertSymbol;
        [SerializeField] private string symbolValue = "+";
        [SerializeField] private string templatePrefix = "(";
        [SerializeField] private string templateSuffix = ")";

        private Button button;
        private MathKeypadController keypadController;

        private void Awake()
        {
            button = GetComponent<Button>();
            keypadController = GetComponentInParent<MathKeypadController>();

            button.onClick.AddListener(OnClickKey);
        }

        private void OnClickKey()
        {
            if (keypadController == null)
            {
                keypadController = FindAnyObjectByType<MathKeypadController>();
            }
            if (keypadController == null) return;

            switch (actionType)
            {
                case KeyActionType.InsertSymbol:
                    keypadController.InsertSymbol(symbolValue);
                    break;
                case KeyActionType.InsertTemplate:
                    keypadController.InsertTemplate(templatePrefix, templateSuffix);
                    break;
                case KeyActionType.Backspace:
                    keypadController.Backspace();
                    break;
                case KeyActionType.Clear:
                    keypadController.ClearAll();
                    break;
            }
        }
    }
}