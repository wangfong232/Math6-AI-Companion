using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Math6Companion.Core;
using Math6Companion.Core.Contracts;
using Math6Companion.Core.Mock;

namespace Math6Companion.UI
{
    public class EssayUIController : MonoBehaviour
    {
        [Header("Header & Problem")]
        [SerializeField] private TextMeshProUGUI problemTitleText;
        [SerializeField] private TextMeshProUGUI problemPromptText;
        [SerializeField] private Button backToMenuButton;

        [Header("Photo Submission Mode")]
        [SerializeField] private Button capturePhotoButton;    // Button 1: Chụp ảnh camera
        [SerializeField] private Button pickImageButton;        // Button 2: Chọn ảnh từ máy tính
        [SerializeField] private Button loadSamplePhotoButton;  // Button 3: Tải ảnh vở bài tập mẫu (Test nhanh)
        [SerializeField] private RawImage imagePreview;
        [SerializeField] private GameObject previewContainer;
        [SerializeField] private TextMeshProUGUI photoStatusText;

        [Header("Input & Keypad (Tùy chọn)")]
        [SerializeField] private TMP_InputField solutionInputField;
        [SerializeField] private MathKeypadController mathKeypad;

        [Header("Action Buttons")]
        [SerializeField] private Button submitButton;
        [SerializeField] private Button closeButton;

        private IAITutorService aiTutorService;
        private QuestionData currentQuestion;
        private WebCamTexture webCamTexture;
        private bool hasPhoto = false;

        private void Awake()
        {
            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmitClicked);

            if (capturePhotoButton != null)
                capturePhotoButton.onClick.AddListener(OnCapturePhotoClicked);

            if (pickImageButton != null)
                pickImageButton.onClick.AddListener(OnPickImageClicked);

            if (loadSamplePhotoButton != null)
                loadSamplePhotoButton.onClick.AddListener(OnLoadSamplePhotoClicked);

            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene("02_MainMenu"));

            if (closeButton != null)
                closeButton.onClick.AddListener(() => SceneManager.LoadScene("02_MainMenu"));
        }

        private void Start()
        {
            aiTutorService = MockNetworkService.Instance ?? FindAnyObjectByType<MockNetworkService>();
            if (aiTutorService == null)
            {
                var fallback = new GameObject("[MockService_Fallback]");
                aiTutorService = fallback.AddComponent<MockNetworkService>();
            }

            // Gắn bàn phím ảo vào ô input này
            if (mathKeypad != null && solutionInputField != null)
            {
                mathKeypad.SetTargetInputField(solutionInputField);
            }

            if (previewContainer != null) previewContainer.SetActive(false);

            InitSampleProblem();
        }

        public void InitSampleProblem()
        {
            // Đề bài tự luận mẫu chuẩn Toán 6
            currentQuestion = new QuestionData
            {
                questionId = "ESSAY_001",
                topic = "Thứ tự thực hiện phép tính",
                prompt = "Thực hiện phép tính theo từng bước:\n A = 15 - (3 + 2) × 2",
                maxScore = 10,
                rubric = new List<RubricItem>
                {
                    new RubricItem { step = 1, description = "Tính trong ngoặc: (3 + 2) = 5", points = 4 },
                    new RubricItem { step = 2, description = "Nhân trước: 5 × 2 = 10", points = 3 },
                    new RubricItem { step = 3, description = "Trừ sau: 15 - 10 = 5", points = 3 }
                }
            };

            if (problemTitleText != null) problemTitleText.text = $"Bài tự luận: {currentQuestion.topic}";
            if (problemPromptText != null) problemPromptText.text = currentQuestion.prompt;
            if (solutionInputField != null) solutionInputField.text = string.Empty;
        }

        private void OnCapturePhotoClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

            if (webCamTexture == null && WebCamTexture.devices.Length > 0)
            {
                webCamTexture = new WebCamTexture(640, 480);
                if (imagePreview != null)
                {
                    imagePreview.texture = webCamTexture;
                    if (previewContainer != null) previewContainer.SetActive(true);
                }
                webCamTexture.Play();
                hasPhoto = true;
                if (photoStatusText != null) photoStatusText.text = "Camera đang hoạt động. Bấm [Chụp ảnh] lần nữa để chụp cố định!";
            }
            else if (webCamTexture != null && webCamTexture.isPlaying)
            {
                // Cố định ảnh chụp
                Texture2D photo = new Texture2D(webCamTexture.width, webCamTexture.height);
                photo.SetPixels(webCamTexture.GetPixels());
                photo.Apply();
                webCamTexture.Stop();

                if (imagePreview != null) imagePreview.texture = photo;
                hasPhoto = true;
                if (photoStatusText != null) photoStatusText.text = "Đã chụp ảnh bài làm thành công! Sẵn sàng gửi.";
            }
            else
            {
                // Giả lập chụp ảnh nếu thiết bị không có camera phần cứng
                OnLoadSamplePhotoClicked();
            }
        }

        private void OnPickImageClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

            string selectedPath = null;

#if UNITY_EDITOR
            selectedPath = UnityEditor.EditorUtility.OpenFilePanel("Chọn ảnh bài làm Toán 6 (PNG/JPG)", "", "png,jpg,jpeg,bmp");
#elif UNITY_STANDALONE_WIN
            selectedPath = OpenWindowsFilePicker("Chọn ảnh bài làm Toán 6");
#endif

            if (!string.IsNullOrEmpty(selectedPath))
            {
                LoadImageFromFile(selectedPath);
            }
            else
            {
                if (!hasPhoto && photoStatusText != null)
                {
                    photoStatusText.text = "<color=#f39c12>Bạn chưa chọn ảnh từ máy. Bấm [Tải ảnh vở mẫu] để thử nghiệm ngay!</color>";
                }
            }
        }

        public void OnLoadSamplePhotoClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();

            string samplePath = System.IO.Path.Combine(Application.dataPath, "_Project/Textures/sample_math_essay.png");
            if (System.IO.File.Exists(samplePath))
            {
                LoadImageFromFile(samplePath);
                return;
            }

            Texture2D sampleTex = CreateSampleNotebookTexture();
            if (imagePreview != null) imagePreview.texture = sampleTex;
            if (previewContainer != null) previewContainer.SetActive(true);
            hasPhoto = true;

            if (photoStatusText != null)
            {
                photoStatusText.text = "<color=#2ecc71>[OK] Đã tải trang vở bài tập mẫu Toán 6 (Kẻ ô ly)! Sẵn sàng gửi chấm.</color>";
            }
        }

        private void LoadImageFromFile(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                {
                    byte[] fileBytes = System.IO.File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2);
                    if (tex.LoadImage(fileBytes))
                    {
                        if (imagePreview != null) imagePreview.texture = tex;
                        if (previewContainer != null) previewContainer.SetActive(true);
                        hasPhoto = true;

                        if (photoStatusText != null)
                        {
                            string fileName = System.IO.Path.GetFileName(path);
                            photoStatusText.text = $"<color=#2ecc71>[OK] Đã tải ảnh: {fileName} ({tex.width}x{tex.height})!</color>";
                        }
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Không thể tải ảnh: " + ex.Message);
            }

            OnLoadSamplePhotoClicked();
        }

        private Texture2D CreateSampleNotebookTexture()
        {
            int w = 600;
            int h = 400;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color paperBg = new Color(0.98f, 0.98f, 0.96f, 1f); // Giấy tập học sinh
            Color gridLine = new Color(0.85f, 0.90f, 0.96f, 1f); // Dòng kẻ ô ly xanh nhạt
            Color marginLine = new Color(0.92f, 0.35f, 0.35f, 1f); // Dòng kẻ lề đỏ
            Color inkColor = new Color(0.10f, 0.20f, 0.60f, 1f); // Mực bút máy xanh

            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = paperBg;
                    // Kẻ ô ly (mỗi 25px)
                    if (x % 25 == 0 || y % 25 == 0) c = gridLine;
                    // Lề vở màu đỏ
                    if (x >= 68 && x <= 71) c = marginLine;
                    pixels[y * w + x] = c;
                }
            }

            // Vẽ các dòng kẻ mô phỏng chữ viết tay học sinh Toán 6
            DrawHorizontalLine(pixels, w, h, 90, 340, 240, 3, inkColor); // Dòng tiêu đề
            DrawHorizontalLine(pixels, w, h, 90, 280, 280, 3, inkColor); // Bước 1: A = 15 - (3 + 2) x 2
            DrawHorizontalLine(pixels, w, h, 90, 220, 200, 3, inkColor); // Bước 2: = 15 - 5 x 2
            DrawHorizontalLine(pixels, w, h, 90, 160, 170, 3, inkColor); // Bước 3: = 15 - 10
            DrawHorizontalLine(pixels, w, h, 90, 100, 110, 4, inkColor); // Bước 4: = 5
            DrawHorizontalLine(pixels, w, h, 90, 50, 180, 3, new Color(0.8f, 0.15f, 0.15f, 1f)); // Đáp số

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static void DrawHorizontalLine(Color[] pixels, int texW, int texH, int startX, int startY, int length, int thickness, Color col)
        {
            for (int y = startY; y < startY + thickness && y < texH; y++)
            {
                for (int x = startX; x < startX + length && x < texW; x++)
                {
                    pixels[y * texW + x] = col;
                }
            }
        }

#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private class OpenFileName
        {
            public int structSize = 0;
            public System.IntPtr dlgOwner = System.IntPtr.Zero;
            public System.IntPtr instance = System.IntPtr.Zero;
            public string filter = null;
            public string customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public string file = null;
            public int maxFile = 0;
            public string fileTitle = null;
            public int maxFileTitle = 0;
            public string initialDir = null;
            public string title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt = null;
            public System.IntPtr custData = System.IntPtr.Zero;
            public System.IntPtr hook = System.IntPtr.Zero;
            public string templateName = null;
            public System.IntPtr reservedPtr = System.IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        [System.Runtime.InteropServices.DllImport("comdlg32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool GetOpenFileName([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] OpenFileName ofn);

        private static string OpenWindowsFilePicker(string title)
        {
            OpenFileName ofn = new OpenFileName();
            ofn.structSize = System.Runtime.InteropServices.Marshal.SizeOf(ofn);
            ofn.filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)\0*.png;*.jpg;*.jpeg;*.bmp\0All Files (*.*)\0*.*\0";
            ofn.file = new string(new char[512]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new string(new char[128]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.initialDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);
            ofn.title = title;
            ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008; // OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR

            if (GetOpenFileName(ofn))
            {
                return ofn.file;
            }
            return null;
        }
#endif

        private void OnSubmitClicked()
        {
            string studentSteps = solutionInputField != null ? solutionInputField.text.Trim() : "";

            if (!hasPhoto && string.IsNullOrEmpty(studentSteps))
            {
                if (photoStatusText != null)
                {
                    photoStatusText.text = "<color=red>Vui lòng chụp ảnh bài làm, chọn ảnh từ máy hoặc nhập lời giải!</color>";
                }
                return;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (submitButton != null) submitButton.interactable = false;

            if (photoStatusText != null) photoStatusText.text = "AI Thầy Minh đang chấm bài giải theo Rubric...";

            var payload = new EssaySubmissionPayload
            {
                questionId = currentQuestion.questionId,
                prompt = currentQuestion.prompt,
                studentSteps = !string.IsNullOrEmpty(studentSteps) ? studentSteps : "Bài giải nộp qua ảnh chụp",
                rubric = currentQuestion.rubric
            };

            // Gọi Mock AI Chấm điểm
            aiTutorService.GradeEssay(payload, (success, response, message) =>
            {
                if (submitButton != null) submitButton.interactable = true;

                if (success && response != null)
                {
                    string details = "";
                    if (response.stepAnalysis != null)
                    {
                        foreach (var item in response.stepAnalysis)
                        {
                            string icon = item.isCorrect ? "[Đúng]" : "[Chú ý]";
                            details += $"{icon} Bước {item.step}: {item.comment}\n";
                        }
                    }

                    // Lưu vào ScoreSessionContext dùng chung
                    ScoreSessionContext.ActivityType = "ESSAY";
                    ScoreSessionContext.Score = response.score;
                    ScoreSessionContext.MaxScore = response.maxScore;
                    ScoreSessionContext.EssayFeedback = $"{response.pedagogicalFeedback}\n\n{details}";
                    ScoreSessionContext.IsVersusMode = false;
                    ScoreSessionContext.TimeSpentSeconds = 90;

                    // Chuyển sang Scene 07_Score
                    SceneManager.LoadScene("07_Score");
                }
                else
                {
                    if (photoStatusText != null) photoStatusText.text = "<color=red>Lỗi khi chấm bài: " + message + "</color>";
                }
            });
        }

        private void OnDestroy()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }
        }
    }
}