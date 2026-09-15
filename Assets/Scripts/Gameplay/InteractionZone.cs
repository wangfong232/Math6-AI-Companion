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

    private void TriggerAction()
    {
        switch (zoneType)
        {
            case ZoneType.StudentDesk:
                Debug.Log(">> [Zone A]: Mở giao diện Chọn bài / Trắc nghiệm.");
                break;
            case ZoneType.TeacherPodium:
                Debug.Log(">> [Zone B]: Mở giao diện Gia sư AI (Thầy Minh).");
                break;
            case ZoneType.NoticeBoard:
                Debug.Log(">> [Zone C]: Mở Bảng tin Tiến độ & Huy hiệu.");
                break;
        }
    }
}