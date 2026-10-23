using System.Collections;
using UnityEngine;
using TMPro;

namespace Math6Companion.Gameplay3D
{
    /// <summary>
    /// Điều khiển Robot AI Đồng Hành (Scout Bot) bay lơ lửng bên vai người chơi,
    /// tự động bay theo, xoay mượt mà, bồng bềnh và đưa ra lời nhắc/hội thoại toán học.
    /// </summary>
    public class FloatingAICompanion : MonoBehaviour
    {
        public static FloatingAICompanion Instance { get; private set; }

        [Header("Follow Settings")]
        [SerializeField] private Transform targetPlayer;
        [SerializeField] private Vector3 followOffset = new Vector3(0.55f, 1.15f, 0.25f);
        [SerializeField] private float smoothSpeed = 8f;
        [SerializeField] private float rotationSpeed = 8f;

        [Header("Hover Animation")]
        [SerializeField] private float bobbingAmplitude = 0.08f;
        [SerializeField] private float bobbingFrequency = 2.0f;
        [SerializeField] private float tiltAngle = 10f;

        [Header("Speech Bubble (World Space)")]
        [SerializeField] private GameObject speechBubbleRoot;
        [SerializeField] private TextMeshPro speechBubbleText;
        [SerializeField] private float bubbleDuration = 5f;

        private Vector3 _currentVelocity;
        private float _bobbingTimer;
        private Coroutine _bubbleCoroutine;

        private readonly string[] _idleGreetings = new string[]
        {
            "Thầy Minh chào em! Cùng khám phá Làng Sen và ôn luyện Toán 6 nhé!",
            "Em có biết: Trục số nguyên giúp chúng ta đo cả độ cao lẫn độ sâu dưới biển đấy!",
            "Quán Phở Cụ Ba có bài toán chia phân số thú vị lắm, em qua thử sức nhé!",
            "Chùa Một Cột có kiến trúc đối xứng tuyệt đẹp, hình học kỳ diệu lắm!",
            "Bấm phím [F] bất kỳ lúc nào nếu em cần Thầy Minh hướng dẫn giải bài nhé!"
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (targetPlayer == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) targetPlayer = player.transform;
            }

            // Snap ngay bên vai người chơi khi vừa khởi tạo
            TeleportToPlayer();

            // Đảm bảo robot chạy animation Idle bay lơ lửng
            var anim = GetComponent<Animator>();
            if (anim != null)
            {
                anim.Play("01_Scout_Bot_Idle", 0, 0f);
            }

            // Tắt các mảnh bom tấn công
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Contains("Bomd"))
                {
                    t.gameObject.SetActive(false);
                }
            }

            if (speechBubbleRoot != null)
            {
                speechBubbleRoot.transform.SetParent(null, true);
                speechBubbleRoot.SetActive(false);
            }

            // Chào mừng lần đầu khi vừa vào game
            StartCoroutine(InitialGreetingRoutine());
        }

        private void OnDestroy()
        {
            if (speechBubbleRoot != null)
            {
                Destroy(speechBubbleRoot);
            }
        }

        public void TeleportToPlayer()
        {
            if (targetPlayer == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) targetPlayer = player.transform;
            }

            if (targetPlayer != null)
            {
                transform.position = targetPlayer.position 
                                     + (targetPlayer.right * followOffset.x) 
                                     + (Vector3.up * followOffset.y) 
                                     + (targetPlayer.forward * followOffset.z);
                transform.rotation = targetPlayer.rotation;
                _currentVelocity = Vector3.zero;
            }
        }

        private IEnumerator InitialGreetingRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            SayRandomGreeting();
        }

        private void LateUpdate()
        {
            if (targetPlayer == null) return;

            // Nếu cách quá xa (ví dụ sau khi teleport/load scene), snap lại ngay
            if (Vector3.Distance(transform.position, targetPlayer.position) > 6f)
            {
                TeleportToPlayer();
            }

            _bobbingTimer += Time.deltaTime * bobbingFrequency;
            float bobbingOffset = Mathf.Sin(_bobbingTimer) * bobbingAmplitude;

            // Tính vị trí mục tiêu theo vai nhân vật
            Vector3 worldTargetPos = targetPlayer.position 
                                     + (targetPlayer.right * followOffset.x) 
                                     + (Vector3.up * (followOffset.y + bobbingOffset)) 
                                     + (targetPlayer.forward * followOffset.z);

            transform.position = Vector3.SmoothDamp(transform.position, worldTargetPos, ref _currentVelocity, 1f / smoothSpeed);

            // Xoay mặt robot cùng hướng với hướng nhìn người chơi kèm hiệu ứng nghiêng nhẹ khi bay
            Quaternion desiredRotation = Quaternion.LookRotation(targetPlayer.forward, Vector3.up) * Quaternion.Euler(Mathf.Sin(_bobbingTimer) * tiltAngle, 0, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationSpeed);

            // Giữ speech bubble luôn nổi ngay trên đỉnh robot và hướng thẳng vào Camera chính
            if (speechBubbleRoot != null && speechBubbleRoot.activeSelf)
            {
                speechBubbleRoot.transform.position = transform.position + Vector3.up * 0.65f;
                if (Camera.main != null)
                {
                    speechBubbleRoot.transform.rotation = Camera.main.transform.rotation;
                }
            }
        }

        public void SetTarget(Transform newTarget)
        {
            targetPlayer = newTarget;
        }

        public void Say(string message, float duration = 5f)
        {
            if (speechBubbleRoot == null || speechBubbleText == null) return;

            if (_bubbleCoroutine != null) StopCoroutine(_bubbleCoroutine);
            _bubbleCoroutine = StartCoroutine(ShowBubbleRoutine(message, duration));
        }

        public void SayRandomGreeting()
        {
            if (_idleGreetings.Length == 0) return;
            string msg = _idleGreetings[Random.Range(0, _idleGreetings.Length)];
            Say(msg, bubbleDuration);
        }

        private IEnumerator ShowBubbleRoutine(string message, float duration)
        {
            speechBubbleText.text = message;
            speechBubbleRoot.SetActive(true);
            yield return new WaitForSeconds(duration);
            speechBubbleRoot.SetActive(false);
            _bubbleCoroutine = null;
        }
    }
}
