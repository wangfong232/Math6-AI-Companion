using System;
using UnityEngine;

namespace Math6Companion.Gameplay3D
{
    /// <summary>
    /// Điều khiển nhân vật góc nhìn thứ ba độc lập, tối ưu,
    /// tích hợp hệ thống phát hiện rơi vực (Fall Detection & Respawn),
    /// chặn lưu tọa độ lỗi, và đồng bộ animation mượt mà chuẩn ithappy Controller.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerThirdPersonController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float runSpeed = 6.0f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float jumpForce = 5.0f;
        [SerializeField] private float gravity = 18f;

        [Header("Fall & Respawn Settings")]
        [SerializeField] private float killPlaneY = -18.0f;
        private Vector3 _initialSpawnPosition;
        private Quaternion _initialSpawnRotation;

        [Header("Camera & References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Animator characterAnimator;

        [Header("Footsteps Audio")]
        [SerializeField] private float footstepInterval = 0.45f;
        private float _footstepTimer;

        private CharacterController _characterController;
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _canMove = true;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (characterAnimator == null)
            {
                characterAnimator = GetComponentInChildren<Animator>();
            }

            _initialSpawnPosition = transform.position;
            _initialSpawnRotation = transform.rotation;
        }

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            RestoreSavedPosition();
        }

        private void OnDisable()
        {
            SavePosition();
        }

        private void OnDestroy()
        {
            SavePosition();
        }

        public void SavePosition()
        {
            // Chỉ lưu tọa độ khi người chơi còn ở trên mặt đảo an toàn (không bị rơi)
            if (transform.position.y > killPlaneY && Mathf.Abs(transform.position.x) < 50f && Mathf.Abs(transform.position.z) < 50f)
            {
                if (Math6Companion.Core.SessionManager.Instance != null)
                {
                    Math6Companion.Core.SessionManager.Instance.SaveVillagePosition(transform.position, transform.rotation);
                }
            }
            else
            {
                if (Math6Companion.Core.SessionManager.Instance != null)
                {
                    Math6Companion.Core.SessionManager.Instance.ClearSavedVillagePosition();
                }
            }
        }

        public void RestoreSavedPosition()
        {
            if (Math6Companion.Core.SessionManager.Instance != null && Math6Companion.Core.SessionManager.Instance.HasSavedVillagePosition)
            {
                Vector3 savedPos = Math6Companion.Core.SessionManager.Instance.LastVillagePlayerPosition;
                Quaternion savedRot = Math6Companion.Core.SessionManager.Instance.LastVillagePlayerRotation;

                // Nếu vị trí lưu nằm dưới vực hoặc bất thường, đưa về vị trí xuất phát an toàn
                if (savedPos.y < killPlaneY || Mathf.Abs(savedPos.x) > 50f || Mathf.Abs(savedPos.z) > 50f)
                {
                    RespawnAtSpawnPoint();
                    return;
                }

                if (_characterController != null)
                {
                    _characterController.enabled = false;
                    transform.position = savedPos;
                    transform.rotation = savedRot;
                    _characterController.enabled = true;
                }
                else
                {
                    transform.position = savedPos;
                    transform.rotation = savedRot;
                }

                // Đồng bộ AI Companion
                var companion = FloatingAICompanion.Instance ?? FindAnyObjectByType<FloatingAICompanion>();
                if (companion != null)
                {
                    companion.TeleportToPlayer();
                }

                // Căn chỉnh camera theo hướng nhìn nhân vật
                if (cameraTransform == null && Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                }
                if (cameraTransform != null)
                {
                    var orbitCam = cameraTransform.GetComponent<SmoothOrbitCamera>();
                    if (orbitCam != null)
                    {
                        orbitCam.SetTarget(transform);
                        orbitCam.ResetCameraFacing(savedRot.eulerAngles.y);
                    }
                }
            }
        }

        public void RespawnAtSpawnPoint()
        {
            _velocity = Vector3.zero;

            if (_characterController != null) _characterController.enabled = false;
            transform.position = _initialSpawnPosition;
            transform.rotation = _initialSpawnRotation;
            if (_characterController != null) _characterController.enabled = true;

            if (Math6Companion.Core.SessionManager.Instance != null)
            {
                Math6Companion.Core.SessionManager.Instance.ClearSavedVillagePosition();
            }

            var companion = FloatingAICompanion.Instance ?? FindAnyObjectByType<FloatingAICompanion>();
            if (companion != null)
            {
                companion.TeleportToPlayer();
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            if (cameraTransform != null)
            {
                var orbitCam = cameraTransform.GetComponent<SmoothOrbitCamera>();
                if (orbitCam != null)
                {
                    orbitCam.SetTarget(transform);
                    orbitCam.ResetCameraFacing(_initialSpawnRotation.eulerAngles.y);
                }
            }
        }

        private void Update()
        {
            // Kiểm tra rơi khỏi map (Fall Detection / Kill Plane)
            if (transform.position.y < killPlaneY)
            {
                RespawnAtSpawnPoint();
                return;
            }

            // Kiểm tra trạng thái UI: nếu con trỏ chuột đang mở (modal popup đang hiện), tạm dừng di chuyển
            _canMove = Cursor.lockState == CursorLockMode.Locked;

            _isGrounded = _characterController.isGrounded;
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Giữ nhân vật bám sát mặt đất
            }

            if (!_canMove)
            {
                UpdateAnimation(0f, 0f, 0f, false);
                return;
            }

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            Vector3 moveDirection = Vector3.zero;

            if (direction.magnitude >= 0.1f)
            {
                // Tính góc xoay theo hướng nhìn của camera
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 
                                    (cameraTransform != null ? cameraTransform.eulerAngles.y : 0f);
                
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

                moveDirection = targetRotation * Vector3.forward;
                _characterController.Move(moveDirection.normalized * (currentSpeed * Time.deltaTime));

                // Âm thanh bước chân
                if (_isGrounded)
                {
                    _footstepTimer += Time.deltaTime;
                    if (_footstepTimer >= (isRunning ? footstepInterval * 0.7f : footstepInterval))
                    {
                        _footstepTimer = 0f;
                        if (Math6Companion.Core.AudioManager.Instance != null)
                        {
                            Math6Companion.Core.AudioManager.Instance.PlayButtonClick();
                        }
                    }
                }
            }
            else
            {
                _footstepTimer = 0f;
            }

            // Nhảy
            if (Input.GetButtonDown("Jump") && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpForce * 2f * gravity);
                if (characterAnimator != null)
                {
                    characterAnimator.SetBool("IsJump", true);
                }
            }

            // Trọng lực
            _velocity.y -= gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);

            // Cập nhật Animation với cả Hor/Vert (BlendTree của ithappy) và Speed/State
            float speedParam = direction.magnitude * (isRunning ? 2f : 1f);
            float animHor = horizontal * (isRunning ? 2f : 1f);
            float animVert = vertical * (isRunning ? 2f : 1f);
            UpdateAnimation(animHor, animVert, speedParam, !_isGrounded);
        }

        private void UpdateAnimation(float hor, float vert, float speed, bool inAir)
        {
            if (characterAnimator == null) return;

            try
            {
                characterAnimator.SetFloat("Hor", hor);
                characterAnimator.SetFloat("Vert", vert);
                characterAnimator.SetFloat("Speed", speed);
                // Gán State dạng Float đúng kiểu dữ liệu trong ithappy Animator Controller
                characterAnimator.SetFloat("State", speed > 1.2f ? 2f : (speed > 0.1f ? 1f : 0f));
                characterAnimator.SetBool("IsGrounded", !inAir);
                if (!inAir) characterAnimator.SetBool("IsJump", false);
            }
            catch
            {
                // Phòng ngừa trường hợp Animator chưa nạp đủ parameter
            }
        }

        public void SetCanMove(bool canMove)
        {
            _canMove = canMove;
        }

        public void SetSpawnPoint(Vector3 pos, Quaternion rot)
        {
            _initialSpawnPosition = pos;
            _initialSpawnRotation = rot;
        }
    }
}
