using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Math6Companion.UI
{
    public class EssayImageUploader : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RawImage studentWorkImage;       // Kéo ô chứa ảnh vào đây
        [SerializeField] private TextMeshProUGUI placeholderText; // Kéo chữ "CHƯA CÓ ẢNH BÀI LÀM" vào đây
        [SerializeField] private Button selectImageBtn;           // Nút "2. Chọn Tệp Ảnh"

        // Thuộc tính lưu trữ để gửi cho AI OCR sau này
        public Texture2D CurrentUploadedTexture { get; private set; }

        private void Start()
        {
            selectImageBtn.onClick.AddListener(OpenFileDialog);
        }

        private void OpenFileDialog()
        {
#if UNITY_EDITOR
            // Dùng hàm có sẵn của Unity Editor (Chỉ chạy được trong màn hình Editor)
            string path = EditorUtility.OpenFilePanel("Chọn ảnh bài làm", "", "png,jpg,jpeg");
            if (!string.IsNullOrEmpty(path))
            {
                StartCoroutine(LoadLocalImage("file://" + path));
            }
#else
            // TRÊN BẢN BUILD (WINDOWS/WEBGL): BẠN CẦN DÙNG PLUGIN BÊN THỨ 3
            // Khuyến nghị tải Asset miễn phí: "Simple File Browser" (bởi yasirkula)
            // Thay thế đoạn này bằng: SimpleFileBrowser.FileBrowser.ShowLoadDialog(...)
            Debug.LogWarning("Chức năng mở File Explorer trên bản Build cần tích hợp SimpleFileBrowser.");
#endif
        }

        private IEnumerator LoadLocalImage(string filePath)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(filePath))
            {
                yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                if (uwr.result != UnityWebRequest.Result.Success)
#else
                if (uwr.isNetworkError || uwr.isHttpError)
#endif
                {
                    Debug.LogError("Lỗi tải ảnh: " + uwr.error);
                }
                else
                {
                    // Lấy texture từ file máy tính
                    CurrentUploadedTexture = DownloadHandlerTexture.GetContent(uwr);
                    
                    // Gán vào giao diện
                    studentWorkImage.texture = CurrentUploadedTexture;
                    studentWorkImage.color = Color.white; // Xóa màu mờ nếu có
                    
                    // Ẩn dòng chữ Placeholder
                    if (placeholderText != null) placeholderText.gameObject.SetActive(false);
                }
            }
        }
    }
}