using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Mục tiêu theo dõi")]
    [SerializeField] private Transform target;

    [Header("Khoảng cách & Góc nhìn")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 9f, -7f);
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1f, 0f);

    private void LateUpdate()
    {
        if (target == null) return;

        // Tính vị trí đích và dịch chuyển mượt
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Hướng mắt nhìn về phía thân nhân vật
        transform.LookAt(target.position + lookOffset);
    }
}