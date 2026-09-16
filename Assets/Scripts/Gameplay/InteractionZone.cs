using UnityEngine;

public class InteractionZone : MonoBehaviour
{
    public enum ZoneType
    {
        StudentDesk,      // Bàn học (Làm Quiz / Chọn bài)
        TeacherPodium,    // Bục giảng (Gia sư AI Socratic)
        NoticeBoard       // Bảng tin (Huy hiệu / Tiến độ)
    }

    [Header("Cấu hình vùng tương tác")]
    [SerializeField] private ZoneType zoneType;
    [SerializeField] private string promptMessage = "Nhấn [E] để tương tác";

    private bool isPlayerInside = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            InteractionPromptUI.Instance?.ShowPrompt(promptMessage);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            InteractionPromptUI.Instance?.HidePrompt();
        }
    }

    private void Update()
    {
        if (isPlayerInside && Input.GetKeyDown(KeyCode.E))
        {
            TriggerAction();
        }
    }

   [SerializeField] private GameObject quizPanelInstance;

private void TriggerAction()
{
    switch (zoneType)
    {
        case ZoneType.StudentDesk:
            if (quizPanelInstance != null)
            {
                quizPanelInstance.SetActive(true);
                quizPanelInstance.GetComponent<Math6Companion.UI.QuizUIController>()?.LoadQuizData("Integers", "6");
            }
            break;
        case ZoneType.TeacherPodium:
            Debug.Log(">> Mở AI Tutor (U-05)");
            break;
        case ZoneType.NoticeBoard:
            Debug.Log(">> Mở Bảng tin (U-07)");
            break;
    }
}
}