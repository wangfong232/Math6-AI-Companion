using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Math6Companion.Gameplay3D
{
    /// <summary>
    /// Gắn vào bất kỳ vật thể nào trong thế giới 3D (Bảng Cáo Thị, Bàn Quán Phở, Bục Ruộng Bậc Thang, Chùa Một Cột)
    /// để tạo vùng tương tác với phím [E] và hiện bảng chỉ dẫn lơ lửng.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WorldInteractable : MonoBehaviour
    {
        public enum InteractableType
        {
            NoticeBoard,    // Mở Bảng Cáo Thị (Bài học, Trắc nghiệm, PvP)
            PhoShop,        // Mở Minigame Phân Số Quán Phở
            TerraceFields,  // Mở Thử Thách Trục Số Ruộng Bậc Thang
            PagodaSymmetry, // Mở Thử Thách Đối Xứng Chùa Một Cột
            Custom
        }

        [Header("Interaction Configuration")]
        [SerializeField] private InteractableType interactionType = InteractableType.NoticeBoard;
        [SerializeField] private string interactPrompt = "Nhấn [E] để tương tác";
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private float promptHeightOffset = 2.2f;

        [Header("Floating 3D Prompt UI")]
        [SerializeField] private GameObject promptCanvasObject;
        [SerializeField] private TextMeshPro promptText;

        [Header("Events")]
        public UnityEvent onInteracted;

        private bool _isPlayerInside;
        private Transform _mainCameraTransform;

        private void Start()
        {
            if (Camera.main != null)
            {
                _mainCameraTransform = Camera.main.transform;
            }

            // Tự động thiết lập Collider là Trigger nếu chưa đặt
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Ẩn prompt khi bắt đầu
            if (promptCanvasObject != null)
            {
                promptCanvasObject.transform.localPosition = new Vector3(0f, promptHeightOffset, 0f);
                promptCanvasObject.SetActive(false);
            }
            if (promptText != null)
            {
                promptText.text = interactPrompt;
            }
        }

        private void Update()
        {
            if (!_isPlayerInside) return;

            // Xoay bảng nhắc luôn hướng về Camera không bị xiên lệch
            if (promptCanvasObject != null && promptCanvasObject.activeSelf && _mainCameraTransform != null)
            {
                promptCanvasObject.transform.rotation = Quaternion.Euler(0f, _mainCameraTransform.eulerAngles.y, 0f);
            }

            // Kiểm tra bấm phím
            if (Input.GetKeyDown(interactKey))
            {
                TriggerInteract();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
            {
                _isPlayerInside = true;
                if (promptCanvasObject != null) promptCanvasObject.SetActive(true);

                // Phát âm thanh nhẹ báo hiệu có điểm tương tác
                if (Math6Companion.Core.AudioManager.Instance != null)
                {
                    Math6Companion.Core.AudioManager.Instance.PlayButtonClick();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
            {
                _isPlayerInside = false;
                if (promptCanvasObject != null) promptCanvasObject.SetActive(false);
            }
        }

        public void TriggerInteract()
        {
            onInteracted?.Invoke();

            if (VillageHUDManager.Instance != null)
            {
                VillageHUDManager.Instance.HandleInteraction(interactionType);
            }
        }

        public void SetPromptText(string newPrompt)
        {
            interactPrompt = newPrompt;
            if (promptText != null) promptText.text = newPrompt;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 2.5f);
        }
    }
}
