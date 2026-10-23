using System;
using UnityEngine;

namespace Math6Companion.Gameplay3D
{
    /// <summary>
    /// Camera góc nhìn thứ ba Isometric mượt mà, tự động căn góc cao tránh bị che khuất tầm nhìn,
    /// tích hợp chống kẹt xuyên tường/đất (Camera Collision Avoidance),
    /// và lập tức định vị chuẩn xác ngay frame đầu tiên khi load scene.
    /// </summary>
    public class SmoothOrbitCamera : MonoBehaviour
    {
        [Header("Target & Focus")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);
        [SerializeField] private float distance = 6.0f;
        [SerializeField] private float minDistance = 2.0f;

        [Header("Sensitivity & Limits")]
        [SerializeField] private float mouseSensitivityX = 2.5f;
        [SerializeField] private float mouseSensitivityY = 2.0f;
        [SerializeField] private float minPitch = 8f;   // Không cho hạ camera quá thấp để tránh đâm vào đất
        [SerializeField] private float maxPitch = 60f;
        [SerializeField] private float smoothTime = 0.05f;

        [Header("Collision Avoidance")]
        [SerializeField] private LayerMask collisionMask = ~0; // Mặc định né tất cả collider

        private float _yaw = 25f;
        private float _pitch = 22f; // Góc nhìn Isometric từ trên chếch xuống
        private Vector3 _currentVelocity;

        private void Awake()
        {
            // Dọn dẹp bất kỳ camera phụ nào còn sót lại từ demo scene
            var allCams = FindObjectsByType<Camera>(FindObjectsInactive.Include);
            foreach (var c in allCams)
            {
                if (c.gameObject != gameObject && c.name.Equals("Camera", StringComparison.OrdinalIgnoreCase))
                {
                    Destroy(c.gameObject);
                }
            }
        }

        private void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }

            // Thiết lập LayerMask bỏ qua Trigger & Player để không bị giật camera
            collisionMask &= ~(1 << 2); // Ignore Raycast
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0) collisionMask &= ~(1 << playerLayer);

            if (target != null)
            {
                _yaw = target.eulerAngles.y;
                _pitch = 22f;
                SnapToDesiredPosition();
            }
        }

        public void ResetCameraFacing(float targetYaw)
        {
            _yaw = targetYaw;
            _pitch = 22f;
            SnapToDesiredPosition();
        }

        public void SnapToDesiredPosition()
        {
            if (target == null) return;
            Vector3 focusPoint = target.position + targetOffset;
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 dir = rotation * Vector3.forward;
            Vector3 desiredPos = focusPoint - (dir * distance);

            // Kiểm tra va chạm để không bị xuyên đất
            if (Physics.SphereCast(focusPoint, 0.2f, -dir, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                float safeDist = Mathf.Clamp(hit.distance - 0.15f, minDistance, distance);
                desiredPos = focusPoint - (dir * safeDist);
            }

            transform.position = desiredPos;
            transform.LookAt(focusPoint);
            _currentVelocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Chỉ nhận input xoay camera khi con trỏ chuột đang bị khóa
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                _yaw += Input.GetAxis("Mouse X") * mouseSensitivityX * 50f * Time.deltaTime;
                _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivityY * 50f * Time.deltaTime;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }

            Vector3 focusPoint = target.position + targetOffset;
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 dir = rotation * Vector3.forward;
            Vector3 targetPosition = focusPoint - (dir * distance);

            // SphereCast chống camera chui vào trong đất/tường
            if (Physics.SphereCast(focusPoint, 0.2f, -dir, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                float safeDist = Mathf.Clamp(hit.distance - 0.15f, minDistance, distance);
                targetPosition = focusPoint - (dir * safeDist);
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, smoothTime);
            transform.LookAt(focusPoint);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                _yaw = target.eulerAngles.y;
                _pitch = 22f;
                SnapToDesiredPosition();
            }
        }
    }
}
