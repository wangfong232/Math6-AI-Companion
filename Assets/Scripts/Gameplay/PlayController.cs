using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Di chuyển")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private float rotateSpeed = 12f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private Vector3 verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // 1. Đọc phím bấm WASD / Phím mũi tên (chuẩn mặc định Unity)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        float currentSpeed = isSprinting ? runSpeed : walkSpeed;

        // 2. Xoay và di chuyển theo phương ngang
        if (inputDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);

            controller.Move(inputDirection * currentSpeed * Time.deltaTime);
        }

        // 3. Xử lý trọng lực bám sàn
        if (controller.isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }
}