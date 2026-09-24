#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Math6Companion.Core;
using Math6Companion.UI;

namespace Math6Companion.EditorTools
{
    /// <summary>
    /// Công cụ Unity Editor tự động tạo, dựng UI và liên kết 100% cho toàn bộ 11 Scene trong Math6-AI-Companion.
    /// Giúp sinh viên không cần phải kéo thả thủ công từng nút hay từng component.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneAutoBuilder
    {
        private const string SCENES_DIR = "Assets/Scenes";

        static SceneAutoBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                string flagPath = "Temp/Math6_ScenesBuilt_V6.flag";
                if (File.Exists(flagPath)) return;

                try
                {
                    File.WriteAllText(flagPath, DateTime.UtcNow.ToString());
                    Debug.Log("[Math6 SceneAutoBuilder] 🔄 Tự động đồng bộ hóa & tái tạo toàn bộ 11 Scene với cấu hình mới nhất...");
                    ExecuteBuildAllScenesDirect(false);
                    Debug.Log("[Math6 SceneAutoBuilder] ✅ Đã tự động cập nhật xong toàn bộ 11 Scene chuẩn không lỗi!");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Math6 SceneAutoBuilder] AutoSync skipped or failed: " + ex.Message);
                }
            };
        }

        [MenuItem("Math6 / 🚀 TỰ ĐỘNG TẠO ĐẦY ĐỦ 11 SCENE VÀO DỰ ÁN", priority = 0)]
        public static void GenerateAllScenes()
        {
            ExecuteBuildAllScenesDirect(true);
        }

        public static void ExecuteBuildAllScenesDirect(bool showDialog)
        {
            if (!Directory.Exists(SCENES_DIR))
            {
                Directory.CreateDirectory(SCENES_DIR);
                AssetDatabase.Refresh();
            }

            if (showDialog)
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Xác nhận tự động tạo 11 Scene",
                    "Công cụ sẽ tự động tạo và cấu hình đầy đủ 11 Scene chuẩn hóa trong Assets/Scenes/:\n\n" +
                    "00_Bootstrapper\n01_Login\n02_MainMenu\n03_LessonList\n04_LessonContent\n" +
                    "05_MCQPractice\n06_EssayPractice\n07_Score\n08_AIFeedback\n09_VersusLobby\n" +
                    "10_PracticeLog\n11_Settings\n\nBạn có muốn tiếp tục?",
                    "Tạo Ngay!", "Hủy"
                );

                if (!confirm) return;
            }

            try
            {
                if (showDialog) EditorUtility.DisplayProgressBar("Đang tạo Scene", "00_Bootstrapper...", 0.08f);
                BuildBootstrapperScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang tạo Scene", "01_Login...", 0.16f);
                BuildLoginScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang tạo Scene", "02_MainMenu...", 0.25f);
                BuildMainMenuScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang tạo Scene", "03_LessonList...", 0.33f);
                BuildLessonListScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang tạo Scene", "04_LessonContent...", 0.41f);
                BuildLessonContentScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang tạo Scene", "05_MCQPractice...", 0.50f);
                BuildMCQPracticeScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang tạo Scene", "06_EssayPractice...", 0.58f);
                BuildEssayPracticeScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang tạo Scene", "07_Score...", 0.66f);
                BuildScoreScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang tạo Scene", "08_AIFeedback...", 0.75f);
                BuildAIFeedbackScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang tạo Scene", "09_VersusLobby...", 0.83f);
                BuildVersusLobbyScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang tạo Scene", "10_PracticeLog...", 0.91f);
                BuildPracticeLogScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang tạo Scene", "11_Settings...", 0.98f);
                BuildSettingsScene();

                RegisterScenesInBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Mở lại 00_Bootstrapper sẵn sàng nhấn Play
                EditorSceneManager.OpenScene($"{SCENES_DIR}/00_Bootstrapper.unity");

                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Hoàn Tất!",
                        "✅ Đã tạo và cấu hình đầy đủ 11 Scene trong Assets/Scenes/!\n" +
                        "✅ Đã tự động đăng ký thứ tự 0 -> 11 vào Build Settings!\n\n" +
                        "Bây giờ bạn chỉ cần nhấn nút PLAY để trải nghiệm toàn bộ luồng game.",
                        "Tuyệt Vời!"
                    );
                }
            }
            finally
            {
                if (showDialog) EditorUtility.ClearProgressBar();
            }
        }

        #region Helper Methods (Tạo UI nhanh)
        private static Canvas CreateBaseScene(string sceneName, out GameObject canvasObj)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 0. Main Camera (Xóa vĩnh viễn thông báo 'No cameras rendering' của Unity)
            var cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            var cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.12f, 0.18f); // Đồng bộ màu nền
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0, 0, -10f);
            cameraObj.AddComponent<AudioListener>();

            // 1. EventSystem
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            Type inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem")
                ?? typeof(StandaloneInputModule);
            eventSystem.AddComponent(inputModuleType);

            // 2. Canvas
            canvasObj = new GameObject($"{sceneName}Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 3. Background
            var bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            SetFullStretch(bgRect);
            bgObj.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.18f); // Xanh đen sang trọng

            return canvas;
        }

        private static void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static TextMeshProUGUI CreateText(GameObject parent, string name, string content, float fontSize, Vector2 pos, Vector2 size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            return tmp;
        }

        private static Button CreateButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size, Color btnColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = btnColor;

            var btn = go.GetComponent<Button>();
            // Tắt Navigation tự động để tránh việc EventSystem tự chọn làm trắng nút
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            SetFullStretch(textRt);

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static Slider CreateSlider(GameObject parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            // Nền thanh trượt
            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            SetFullStretch(bg.GetComponent<RectTransform>());
            var bgImage = bg.GetComponent<Image>();
            bgImage.color = new Color(0.18f, 0.22f, 0.30f);
            bgImage.raycastTarget = true;

            // Vùng đổ màu (Fill Area)
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0, 0.2f);
            fillAreaRt.anchorMax = new Vector2(1, 0.8f);
            fillAreaRt.offsetMin = new Vector2(5, 0);
            fillAreaRt.offsetMax = new Vector2(-5, 0);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            SetFullStretch(fill.GetComponent<RectTransform>());
            var fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.18f, 0.75f, 0.45f); // Xanh lục bảo đẹp mắt
            fillImage.raycastTarget = false;

            // Vùng núm kéo (Handle Slide Area & Handle Knob)
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10, 0);
            handleAreaRt.offsetMax = new Vector2(-10, 0);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(size.y + 8, size.y + 8);
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = new Color(0.95f, 0.75f, 0.2f); // Núm vàng gold sang trọng
            handleImage.raycastTarget = true;

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;

            return slider;
        }

        private static Toggle CreateToggle(GameObject parent, string name, string labelText, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            // Nền hộp kiểm
            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.5f);
            bgRt.anchorMax = new Vector2(0, 0.5f);
            bgRt.pivot = new Vector2(0, 0.5f);
            bgRt.anchoredPosition = new Vector2(0, 0);
            bgRt.sizeDelta = new Vector2(30, 30);
            var bgImage = bg.GetComponent<Image>();
            bgImage.color = new Color(0.2f, 0.25f, 0.35f);
            bgImage.raycastTarget = true;

            // Dấu tích checkmark
            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(bg.transform, false);
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.5f, 0.5f);
            checkRt.anchorMax = new Vector2(0.5f, 0.5f);
            checkRt.anchoredPosition = Vector2.zero;
            checkRt.sizeDelta = new Vector2(20, 20);
            var checkImage = check.GetComponent<Image>();
            checkImage.color = new Color(0.18f, 0.75f, 0.45f);

            // Nhãn chữ
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 1);
            labelRt.offsetMin = new Vector2(42, 0);
            labelRt.offsetMax = Vector2.zero;

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = false;

            return toggle;
        }

        private static TMP_InputField CreateInputField(GameObject parent, string name, string placeholderText, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.25f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            SetFullStretch(textRt);
            textRt.offsetMin = new Vector2(15, 5);
            textRt.offsetMax = new Vector2(-15, -5);

            var textTmp = textGo.GetComponent<TextMeshProUGUI>();
            textTmp.fontSize = 20;
            textTmp.color = Color.white;
            textTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phGo.transform.SetParent(go.transform, false);
            var phRt = phGo.GetComponent<RectTransform>();
            SetFullStretch(phRt);
            phRt.offsetMin = new Vector2(15, 5);
            phRt.offsetMax = new Vector2(-15, -5);

            var phTmp = phGo.GetComponent<TextMeshProUGUI>();
            phTmp.text = placeholderText;
            phTmp.fontSize = 20;
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            phTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var input = go.GetComponent<TMP_InputField>();
            input.textComponent = textTmp;
            input.placeholder = phTmp;

            return input;
        }

        private static TMP_Dropdown CreateTMPDropdown(GameObject parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var rootImg = go.GetComponent<Image>();
            rootImg.color = new Color(0.18f, 0.24f, 0.34f);

            // Caption Text
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            SetFullStretch(labelRt);
            labelRt.offsetMin = new Vector2(20, 0);
            labelRt.offsetMax = new Vector2(-40, 0);
            var captionTmp = labelGo.GetComponent<TextMeshProUGUI>();
            captionTmp.fontSize = 20;
            captionTmp.color = Color.white;
            captionTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Arrow
            var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
            arrowGo.transform.SetParent(go.transform, false);
            var arrowRt = arrowGo.GetComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(1, 0.5f);
            arrowRt.anchorMax = new Vector2(1, 0.5f);
            arrowRt.anchoredPosition = new Vector2(-25, 0);
            arrowRt.sizeDelta = new Vector2(30, 30);
            var arrowTmp = arrowGo.GetComponent<TextMeshProUGUI>();
            arrowTmp.text = "v";
            arrowTmp.fontSize = 18;
            arrowTmp.color = Color.white;
            arrowTmp.alignment = TextAlignmentOptions.Center;

            // Template (Dropdown list container)
            var templateGo = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateGo.transform.SetParent(go.transform, false);
            var templateRt = templateGo.GetComponent<RectTransform>();
            templateRt.anchorMin = new Vector2(0, 0);
            templateRt.anchorMax = new Vector2(1, 0);
            templateRt.pivot = new Vector2(0.5f, 1);
            templateRt.anchoredPosition = new Vector2(0, -2);
            templateRt.sizeDelta = new Vector2(0, 200);
            templateGo.GetComponent<Image>().color = new Color(0.12f, 0.17f, 0.25f);

            var scroll = templateGo.GetComponent<ScrollRect>();

            // Viewport
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewportGo.transform.SetParent(templateGo.transform, false);
            SetFullStretch(viewportGo.GetComponent<RectTransform>());
            viewportGo.GetComponent<Image>().color = Color.white;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            // Content
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 50);

            // Item (Toggle)
            var itemGo = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(Image));
            itemGo.transform.SetParent(contentGo.transform, false);
            var itemRt = itemGo.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 0.5f);
            itemRt.anchorMax = new Vector2(1, 0.5f);
            itemRt.sizeDelta = new Vector2(0, 45);
            var itemImg = itemGo.GetComponent<Image>();
            itemImg.color = new Color(0.18f, 0.24f, 0.32f);

            var toggle = itemGo.GetComponent<Toggle>();
            toggle.targetGraphic = itemImg;

            // Item Text
            var itemLabelGo = new GameObject("Item Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            itemLabelGo.transform.SetParent(itemGo.transform, false);
            var itemLabelRt = itemLabelGo.GetComponent<RectTransform>();
            SetFullStretch(itemLabelRt);
            itemLabelRt.offsetMin = new Vector2(20, 0);
            itemLabelRt.offsetMax = new Vector2(-20, 0);
            var itemTmp = itemLabelGo.GetComponent<TextMeshProUGUI>();
            itemTmp.fontSize = 18;
            itemTmp.color = Color.white;
            itemTmp.alignment = TextAlignmentOptions.MidlineLeft;

            scroll.content = contentRt;
            scroll.viewport = viewportGo.GetComponent<RectTransform>();
            scroll.horizontal = false;

            templateGo.SetActive(false); // Template phải ẩn mặc định

            var dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.targetGraphic = rootImg;
            dropdown.template = templateRt;
            dropdown.captionText = captionTmp;
            dropdown.itemText = itemTmp;

            return dropdown;
        }

        private static void SetField(object component, string fieldName, object value)
        {
            if (component == null) return;
            var so = new SerializedObject(component as UnityEngine.Object);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                if (value is UnityEngine.Object uObj)
                {
                    prop.objectReferenceValue = uObj;
                }
                so.ApplyModifiedProperties();
            }
        }
        #endregion

        #region 11 Scene Builders
        // 00_Bootstrapper
        private static void BuildBootstrapperScene()
        {
            CreateBaseScene("00_Bootstrapper", out GameObject canvas);

            CreateText(canvas, "Title", "MATH 6 - AI COMPANION", 44, new Vector2(0, 100), new Vector2(800, 80), new Color(0.95f, 0.75f, 0.2f));
            CreateText(canvas, "SubTitle", "Hệ Thống Học Toán Lớp 6 Tích Hợp Gia Sư AI & Đối Kháng", 22, new Vector2(0, 40), new Vector2(800, 50), Color.white);

            var slider = CreateSlider(canvas, "LoadingSlider", new Vector2(0, -100), new Vector2(600, 24));
            var statusText = CreateText(canvas, "StatusText", "Đang khởi tạo hệ thống...", 20, new Vector2(0, -150), new Vector2(600, 40), new Color(0.8f, 0.8f, 0.8f));

            var manager = new GameObject("_BootstrapperManager").AddComponent<AppBootstrapper>();
            SetField(manager, "progressBar", slider);
            SetField(manager, "statusText", statusText);
            SetField(manager, "minimumBootTime", 1.5f);
            SetField(manager, "nextSceneName", "01_Login");

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/00_Bootstrapper.unity");
        }

        // 01_Login
        private static void BuildLoginScene()
        {
            CreateBaseScene("01_Login", out GameObject canvas);

            var panel = new GameObject("LoginCard", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 520);
            panel.GetComponent<Image>().color = new Color(0.12f, 0.17f, 0.24f);

            CreateText(panel, "CardTitle", "ĐĂNG NHẬP", 32, new Vector2(0, 190), new Vector2(400, 60), new Color(0.95f, 0.75f, 0.2f));
            var userInput = CreateInputField(panel, "UsernameInput", "Nhập tên đăng nhập...", new Vector2(0, 90), new Vector2(400, 55));
            var passInput = CreateInputField(panel, "PasswordInput", "Nhập mật khẩu...", new Vector2(0, 15), new Vector2(400, 55));
            passInput.contentType = TMP_InputField.ContentType.Password;

            var loginBtn = CreateButton(panel, "LoginButton", "ĐĂNG NHẬP >", new Vector2(0, -70), new Vector2(400, 55), new Color(0.2f, 0.6f, 0.9f));
            var status = CreateText(panel, "StatusText", "", 18, new Vector2(0, -140), new Vector2(400, 40), Color.yellow);

            var loading = new GameObject("LoadingIndicator", typeof(RectTransform));
            loading.transform.SetParent(panel.transform, false);
            loading.SetActive(false);

            var manager = new GameObject("_LoginManager").AddComponent<LoginUIController>();
            SetField(manager, "usernameInput", userInput);
            SetField(manager, "passwordInput", passInput);
            SetField(manager, "loginButton", loginBtn);
            SetField(manager, "statusText", status);
            SetField(manager, "loadingIndicator", loading);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/01_Login.unity");
        }

        // 02_MainMenu
        private static void BuildMainMenuScene()
        {
            CreateBaseScene("02_MainMenu", out GameObject canvas);

            // Header
            var header = new GameObject("ProfileHeader", typeof(RectTransform));
            header.transform.SetParent(canvas.transform, false);
            var hRt = header.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 1);
            hRt.anchorMax = new Vector2(1, 1);
            hRt.sizeDelta = new Vector2(0, 100);
            hRt.anchoredPosition = new Vector2(0, -50);

            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(header.transform, false);
            avatar.GetComponent<RectTransform>().anchoredPosition = new Vector2(-400, 0);
            avatar.GetComponent<RectTransform>().sizeDelta = new Vector2(65, 65);
            avatar.GetComponent<Image>().color = new Color(0.95f, 0.75f, 0.2f);

            var nickText = CreateText(header, "NicknameText", "AnThầnĐồng", 26, new Vector2(-220, 12), new Vector2(280, 35), Color.white, TextAlignmentOptions.MidlineLeft);
            var subText = CreateText(header, "UserSubInfoText", "Lớp 6A1 | Nguyễn Văn An", 18, new Vector2(-220, -16), new Vector2(280, 30), new Color(0.7f, 0.8f, 0.9f), TextAlignmentOptions.MidlineLeft);

            var logoutBtn = CreateButton(header, "Btn_Logout", "Đăng xuất", new Vector2(800, 0), new Vector2(150, 45), new Color(0.8f, 0.25f, 0.25f));

            CreateText(canvas, "MenuTitle", "TRUNG TÂM HỌC TẬP TOÁN 6", 36, new Vector2(0, 330), new Vector2(800, 60), new Color(0.95f, 0.75f, 0.2f));

            // Grid buttons
            var btnLesson = CreateButton(canvas, "Btn_LessonList", "1. BÀI HỌC TOÁN 6", new Vector2(-360, 160), new Vector2(330, 130), new Color(0.2f, 0.45f, 0.75f));
            var btnMCQ = CreateButton(canvas, "Btn_MCQPractice", "2. LUYỆN TRẮC NGHIỆM", new Vector2(0, 160), new Vector2(330, 130), new Color(0.18f, 0.6f, 0.45f));
            var btnEssay = CreateButton(canvas, "Btn_EssayPractice", "3. LUYỆN TỰ LUẬN", new Vector2(360, 160), new Vector2(330, 130), new Color(0.65f, 0.4f, 0.75f));
            var btnVersus = CreateButton(canvas, "Btn_VersusLobby", "4. ĐẤU ĐỐI KHÁNG (3P)", new Vector2(-360, 0), new Vector2(330, 130), new Color(0.85f, 0.45f, 0.15f));
            var btnLog = CreateButton(canvas, "Btn_PracticeLog", "5. NHẬT KÝ HỌC TẬP", new Vector2(0, 0), new Vector2(330, 130), new Color(0.25f, 0.6f, 0.75f));
            var btnSettings = CreateButton(canvas, "Btn_Settings", "6. CÀI ĐẶT HỆ THỐNG", new Vector2(360, 0), new Vector2(330, 130), new Color(0.45f, 0.5f, 0.6f));

            var manager = new GameObject("_MainMenuManager").AddComponent<MainMenuUIController>();
            SetField(manager, "nicknameText", nickText);
            SetField(manager, "userSubInfoText", subText);
            SetField(manager, "avatarImage", avatar.GetComponent<Image>());
            SetField(manager, "lessonListButton", btnLesson);
            SetField(manager, "mcqPracticeButton", btnMCQ);
            SetField(manager, "essayPracticeButton", btnEssay);
            SetField(manager, "versusLobbyButton", btnVersus);
            SetField(manager, "practiceLogButton", btnLog);
            SetField(manager, "settingsButton", btnSettings);
            SetField(manager, "logoutButton", logoutBtn);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/02_MainMenu.unity");
        }

        // 03_LessonList
        private static void BuildLessonListScene()
        {
            CreateBaseScene("03_LessonList", out GameObject canvas);

            var backBtn = CreateButton(canvas, "Btn_Back", "< Menu", new Vector2(-800, 480), new Vector2(140, 50), new Color(0.3f, 0.35f, 0.45f));
            CreateText(canvas, "Title", "CHUYÊN ĐỀ TOÁN LỚP 6", 36, new Vector2(0, 480), new Vector2(600, 60), new Color(0.95f, 0.75f, 0.2f));

            // Scroll View
            var scrollGo = new GameObject("ScrollView_Lessons", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(canvas.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchoredPosition = new Vector2(0, -20);
            scrollRt.sizeDelta = new Vector2(1200, 750);
            scrollGo.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            SetFullStretch(viewport.GetComponent<RectTransform>());

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.sizeDelta = new Vector2(0, 0);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.padding = new RectOffset(20, 20, 20, 20);

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollGo.GetComponent<ScrollRect>().content = cRt;
            scrollGo.GetComponent<ScrollRect>().viewport = viewport.GetComponent<RectTransform>();

            // Action Modal
            var modal = new GameObject("ActionModal", typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(canvas.transform, false);
            SetFullStretch(modal.GetComponent<RectTransform>());
            modal.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
            modal.SetActive(false);

            var card = new GameObject("ModalCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(modal.transform, false);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(650, 560);
            card.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.28f);

            var modalTitle = CreateText(card, "ModalLessonTitle", "Tên Bài Học", 26, new Vector2(0, 220), new Vector2(550, 50), new Color(0.95f, 0.75f, 0.2f));
            var modalStats = CreateText(card, "ModalStatsText", "Điểm cao: 10/10 | Top: An (15 lần)", 18, new Vector2(0, 165), new Vector2(550, 45), Color.white);

            var btnText = CreateButton(card, "Btn_TextLesson", "[Lý Thuyết] Xem Tóm Tắt (Text)", new Vector2(0, 95), new Vector2(520, 55), new Color(0.2f, 0.5f, 0.8f));
            var btnVideo = CreateButton(card, "Btn_VideoLesson", "[Video] Xem Video Bài Giảng", new Vector2(0, 25), new Vector2(520, 55), new Color(0.7f, 0.3f, 0.3f));
            var btnMCQ = CreateButton(card, "Btn_MCQPractice", "[Trắc Nghiệm] Luyện Tập Bài Này", new Vector2(0, -45), new Vector2(520, 55), new Color(0.18f, 0.6f, 0.4f));
            var btnEssay = CreateButton(card, "Btn_EssayPractice", "[Tự Luận] Luyện Tập Bài Này", new Vector2(0, -115), new Vector2(520, 55), new Color(0.6f, 0.35f, 0.7f));
            var btnLog = CreateButton(card, "Btn_PracticeLog", "[Nhật Ký] Xem Điểm Bài Này", new Vector2(0, -185), new Vector2(520, 55), new Color(0.25f, 0.6f, 0.7f));
            var btnClose = CreateButton(card, "Btn_CloseModal", "X", new Vector2(285, 240), new Vector2(45, 45), new Color(0.8f, 0.25f, 0.25f));

            var manager = new GameObject("_LessonListManager").AddComponent<LessonListUIController>();
            SetField(manager, "backToMenuButton", backBtn);
            SetField(manager, "lessonListContent", content.transform);
            SetField(manager, "actionModal", modal);
            SetField(manager, "modalLessonTitle", modalTitle);
            SetField(manager, "modalStatsText", modalStats);
            SetField(manager, "textLessonButton", btnText);
            SetField(manager, "videoLessonButton", btnVideo);
            SetField(manager, "mcqPracticeButton", btnMCQ);
            SetField(manager, "essayPracticeButton", btnEssay);
            SetField(manager, "practiceLogButton", btnLog);
            SetField(manager, "closeModalButton", btnClose);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/03_LessonList.unity");
        }

        // 04_LessonContent
        private static void BuildLessonContentScene()
        {
            CreateBaseScene("04_LessonContent", out GameObject canvas);

            var backBtn = CreateButton(canvas, "Btn_BackToList", "< Danh Sách Bài", new Vector2(-780, 480), new Vector2(180, 50), new Color(0.3f, 0.35f, 0.45f));
            var title = CreateText(canvas, "LessonTitleText", "Bài 1: Phép cộng và trừ số nguyên", 32, new Vector2(0, 480), new Vector2(800, 60), new Color(0.95f, 0.75f, 0.2f));

            var btnTabTxt = CreateButton(canvas, "TabBtn_Text", "[Text] Lý Thuyết Tóm Tắt", new Vector2(-220, 390), new Vector2(300, 55), new Color(0.2f, 0.45f, 0.75f));
            var btnTabVid = CreateButton(canvas, "TabBtn_Video", "[Video] Video Minh Họa", new Vector2(220, 390), new Vector2(300, 55), new Color(0.25f, 0.3f, 0.4f));

            // Panels
            var txtPanel = new GameObject("TextContentPanel", typeof(RectTransform), typeof(Image));
            txtPanel.transform.SetParent(canvas.transform, false);
            txtPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);
            txtPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 600);
            txtPanel.GetComponent<Image>().color = new Color(0.12f, 0.17f, 0.24f);

            var theoryText = CreateText(txtPanel, "TheoryBodyText", "Nội dung lý thuyết Toán 6...", 22, Vector2.zero, new Vector2(1100, 540), Color.white, TextAlignmentOptions.TopLeft);

            var vidPanel = new GameObject("VideoContentPanel", typeof(RectTransform), typeof(Image));
            vidPanel.transform.SetParent(canvas.transform, false);
            vidPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);
            vidPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 600);
            vidPanel.GetComponent<Image>().color = new Color(0.12f, 0.17f, 0.24f);
            vidPanel.SetActive(false);

            var vidInfo = CreateText(vidPanel, "VideoInfoText", "Video bài giảng minh họa chuẩn Bộ GD&ĐT", 24, new Vector2(0, 80), new Vector2(900, 80), Color.white);
            var playVidBtn = CreateButton(vidPanel, "Btn_PlayExternalVideo", "> Mở Xem Video Bài Giảng Đầy Đủ", new Vector2(0, -60), new Vector2(380, 65), new Color(0.85f, 0.3f, 0.3f));

            var btnStartMCQ = CreateButton(canvas, "Btn_StartMCQ", "[Trắc Nghiệm] Làm Bài Này", new Vector2(-220, -420), new Vector2(340, 60), new Color(0.18f, 0.6f, 0.4f));
            var btnStartEssay = CreateButton(canvas, "Btn_StartEssay", "[Tự Luận] Làm Bài Này", new Vector2(220, -420), new Vector2(340, 60), new Color(0.6f, 0.35f, 0.7f));

            var manager = new GameObject("_LessonContentManager").AddComponent<LessonContentUIController>();
            SetField(manager, "lessonTitleText", title);
            SetField(manager, "backToListButton", backBtn);
            SetField(manager, "textTabButton", btnTabTxt);
            SetField(manager, "videoTabButton", btnTabVid);
            SetField(manager, "textContentPanel", txtPanel);
            SetField(manager, "videoContentPanel", vidPanel);
            SetField(manager, "theoryBodyText", theoryText);
            SetField(manager, "videoInfoText", vidInfo);
            SetField(manager, "playExternalVideoButton", playVidBtn);
            SetField(manager, "startMcqPracticeButton", btnStartMCQ);
            SetField(manager, "startEssayPracticeButton", btnStartEssay);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/04_LessonContent.unity");
        }

        // 05_MCQPractice
        private static void BuildMCQPracticeScene()
        {
            CreateBaseScene("05_MCQPractice", out GameObject canvas);

            var topicTitle = CreateText(canvas, "TopicTitleText", "Chủ đề: Số nguyên (DỄ)", 26, new Vector2(-400, 480), new Vector2(500, 50), new Color(0.95f, 0.75f, 0.2f), TextAlignmentOptions.MidlineLeft);
            var progress = CreateText(canvas, "ProgressText", "Câu 1/10", 26, new Vector2(0, 480), new Vector2(200, 50), Color.white);
            var timer = CreateText(canvas, "TimerText", "00:30", 28, new Vector2(400, 480), new Vector2(200, 50), Color.yellow, TextAlignmentOptions.MidlineRight);

            var card = new GameObject("QuestionCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(canvas.transform, false);
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 240);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(1100, 220);
            card.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.28f);

            var prompt = CreateText(card, "PromptText", "Tính giá trị của biểu thức: (-15) + 7", 28, Vector2.zero, new Vector2(1000, 180), Color.white);

            var btnA = CreateButton(canvas, "Btn_OptA", "A. 8", new Vector2(-280, 50), new Vector2(520, 90), new Color(0.2f, 0.25f, 0.35f));
            var btnB = CreateButton(canvas, "Btn_OptB", "B. -8", new Vector2(280, 50), new Vector2(520, 90), new Color(0.2f, 0.25f, 0.35f));
            var btnC = CreateButton(canvas, "Btn_OptC", "C. 22", new Vector2(-280, -60), new Vector2(520, 90), new Color(0.2f, 0.25f, 0.35f));
            var btnD = CreateButton(canvas, "Btn_OptD", "D. -22", new Vector2(280, -60), new Vector2(520, 90), new Color(0.2f, 0.25f, 0.35f));

            var feedback = CreateText(canvas, "FeedbackText", "", 24, new Vector2(0, -180), new Vector2(1000, 60), Color.green);
            var nextBtn = CreateButton(canvas, "NextButton", "Câu Tiếp Theo >", new Vector2(0, -280), new Vector2(300, 60), new Color(0.18f, 0.6f, 0.45f));
            nextBtn.gameObject.SetActive(false);

            var closeBtn = CreateButton(canvas, "CloseQuizButton", "Nộp bài sớm [X]", new Vector2(780, 480), new Vector2(160, 45), new Color(0.8f, 0.3f, 0.3f));

            var manager = new GameObject("_QuizManager").AddComponent<QuizUIController>();
            SetField(manager, "topicTitleText", topicTitle);
            SetField(manager, "progressText", progress);
            SetField(manager, "timerText", timer);
            SetField(manager, "promptText", prompt);
            SetField(manager, "feedbackText", feedback);
            SetField(manager, "nextButton", nextBtn);
            SetField(manager, "closeQuizButton", closeBtn);

            // Mảng Button
            var so = new SerializedObject(manager);
            var optArray = so.FindProperty("optionButtons");
            if (optArray != null)
            {
                optArray.arraySize = 4;
                optArray.GetArrayElementAtIndex(0).objectReferenceValue = btnA;
                optArray.GetArrayElementAtIndex(1).objectReferenceValue = btnB;
                optArray.GetArrayElementAtIndex(2).objectReferenceValue = btnC;
                optArray.GetArrayElementAtIndex(3).objectReferenceValue = btnD;
            }

            var textArray = so.FindProperty("optionTexts");
            if (textArray != null)
            {
                textArray.arraySize = 4;
                textArray.GetArrayElementAtIndex(0).objectReferenceValue = btnA.GetComponentInChildren<TextMeshProUGUI>();
                textArray.GetArrayElementAtIndex(1).objectReferenceValue = btnB.GetComponentInChildren<TextMeshProUGUI>();
                textArray.GetArrayElementAtIndex(2).objectReferenceValue = btnC.GetComponentInChildren<TextMeshProUGUI>();
                textArray.GetArrayElementAtIndex(3).objectReferenceValue = btnD.GetComponentInChildren<TextMeshProUGUI>();
            }
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/05_MCQPractice.unity");
        }

        // 06_EssayPractice
        private static void BuildEssayPracticeScene()
        {
            CreateBaseScene("06_EssayPractice", out GameObject canvas);

            var backBtn = CreateButton(canvas, "Btn_Back", "< Menu", new Vector2(-780, 480), new Vector2(160, 50), new Color(0.3f, 0.35f, 0.45f));
            var title = CreateText(canvas, "ProblemTitleText", "Bài Tự Luận: Thứ Tự Thực Hiện Phép Tính", 32, new Vector2(0, 480), new Vector2(800, 60), new Color(0.95f, 0.75f, 0.2f));

            var card = new GameObject("ProblemCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(canvas.transform, false);
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 330);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 150);
            card.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.28f);

            var prompt = CreateText(card, "ProblemPromptText", "Thực hiện phép tính theo từng bước: A = 15 - (3 + 2) × 2", 26, Vector2.zero, new Vector2(1100, 120), Color.white);

            var btnCapture = CreateButton(canvas, "Btn_CapturePhoto", "1. CHỤP ẢNH BÀI LÀM", new Vector2(-320, 180), new Vector2(300, 60), new Color(0.2f, 0.5f, 0.8f));
            var btnPick = CreateButton(canvas, "Btn_PickImage", "2. CHỌN ẢNH TỪ MÁY", new Vector2(0, 180), new Vector2(300, 60), new Color(0.6f, 0.35f, 0.7f));
            var btnSample = CreateButton(canvas, "Btn_SamplePhoto", "3. VỞ MẪU TOÁN 6", new Vector2(320, 180), new Vector2(300, 60), new Color(0.18f, 0.65f, 0.4f));

            var prevContainer = new GameObject("PreviewContainer", typeof(RectTransform), typeof(Image));
            prevContainer.transform.SetParent(canvas.transform, false);
            prevContainer.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
            prevContainer.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 320);
            prevContainer.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f);

            var preview = new GameObject("ImagePreview", typeof(RectTransform), typeof(RawImage));
            preview.transform.SetParent(prevContainer.transform, false);
            SetFullStretch(preview.GetComponent<RectTransform>());

            var photoStatus = CreateText(canvas, "PhotoStatusText", "Hãy chụp ảnh, chọn ảnh từ máy hoặc bấm xem vở mẫu để AI chấm", 22, new Vector2(0, -250), new Vector2(900, 45), Color.yellow);
            var submitBtn = CreateButton(canvas, "SubmitButton", "GỬI BÀI GIẢI ĐỂ AI CHẤM >", new Vector2(0, -340), new Vector2(380, 70), new Color(0.18f, 0.65f, 0.4f));

            var manager = new GameObject("_EssayManager").AddComponent<EssayUIController>();
            SetField(manager, "problemTitleText", title);
            SetField(manager, "problemPromptText", prompt);
            SetField(manager, "backToMenuButton", backBtn);
            SetField(manager, "capturePhotoButton", btnCapture);
            SetField(manager, "pickImageButton", btnPick);
            SetField(manager, "loadSamplePhotoButton", btnSample);
            SetField(manager, "imagePreview", preview.GetComponent<RawImage>());
            SetField(manager, "previewContainer", prevContainer);
            SetField(manager, "photoStatusText", photoStatus);
            SetField(manager, "submitButton", submitBtn);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/06_EssayPractice.unity");
        }

        // 07_Score
        private static void BuildScoreScene()
        {
            CreateBaseScene("07_Score", out GameObject canvas);

            var card = new GameObject("ResultCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(canvas.transform, false);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(750, 720);
            card.GetComponent<Image>().color = new Color(0.13f, 0.18f, 0.25f);

            var scoreBig = CreateText(card, "ScoreBigText", "8/10", 72, new Vector2(0, 220), new Vector2(400, 100), new Color(0.95f, 0.75f, 0.2f));
            var scoreDetail = CreateText(card, "ScoreDetailText", "Số câu đúng: 8/10 câu", 24, new Vector2(0, 130), new Vector2(500, 45), Color.white);
            var timeSpent = CreateText(card, "TimeSpentText", "Thời gian: 00:45", 22, new Vector2(0, 80), new Vector2(500, 40), new Color(0.8f, 0.85f, 0.9f));

            // Versus Banner
            var vsBanner = new GameObject("VersusBannerPanel", typeof(RectTransform));
            vsBanner.transform.SetParent(card.transform, false);
            vsBanner.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            vsBanner.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 90);
            vsBanner.SetActive(false);

            var vsResult = CreateText(vsBanner, "VersusResultText", "CHIẾN THẮNG (WINNER)!", 32, new Vector2(0, 20), new Vector2(550, 45), Color.green);
            var vsCompare = CreateText(vsBanner, "VersusCompareText", "Bạn: 80đ  VS  BảoToánHọc: 60đ", 20, new Vector2(0, -25), new Vector2(550, 40), Color.white);

            var btnAI = CreateButton(card, "Btn_AIFeedback", "XEM GỢI Ý CÁC CÂU SAI BẰNG AI >", new Vector2(0, -110), new Vector2(520, 65), new Color(0.2f, 0.5f, 0.85f));
            var btnRetry = CreateButton(card, "Btn_Retry", "Làm Lại Bài Tập", new Vector2(0, -190), new Vector2(520, 55), new Color(0.18f, 0.6f, 0.45f));
            var btnMenu = CreateButton(card, "Btn_BackToMenu", "Về Menu Chính", new Vector2(0, -260), new Vector2(520, 55), new Color(0.35f, 0.4f, 0.5f));

            var manager = new GameObject("_ScoreManager").AddComponent<ScoreUIController>();
            SetField(manager, "scoreBigText", scoreBig);
            SetField(manager, "scoreDetailText", scoreDetail);
            SetField(manager, "timeSpentText", timeSpent);
            SetField(manager, "versusBannerPanel", vsBanner);
            SetField(manager, "versusResultText", vsResult);
            SetField(manager, "versusCompareText", vsCompare);
            SetField(manager, "aiFeedbackButton", btnAI);
            SetField(manager, "retryButton", btnRetry);
            SetField(manager, "backToMenuButton", btnMenu);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/07_Score.unity");
        }

        // 08_AIFeedback
        private static void BuildAIFeedbackScene()
        {
            CreateBaseScene("08_AIFeedback", out GameObject canvas);

            var backScore = CreateButton(canvas, "Btn_BackToScore", "< Bảng Điểm", new Vector2(-780, 480), new Vector2(160, 50), new Color(0.3f, 0.35f, 0.45f));
            var backMenu = CreateButton(canvas, "Btn_BackToMenu", "Về Menu <", new Vector2(780, 480), new Vector2(160, 50), new Color(0.3f, 0.35f, 0.45f));

            var greeting = CreateText(canvas, "TutorGreetingText", "Chào em! Thầy Minh ở đây để cùng em phân tích câu làm sai nhé:", 26, new Vector2(0, 400), new Vector2(1000, 50), new Color(0.95f, 0.75f, 0.2f));

            var card = new GameObject("DialogueCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(canvas.transform, false);
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 90);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(1100, 500);
            card.GetComponent<Image>().color = new Color(0.13f, 0.18f, 0.25f);

            var qTitle = CreateText(card, "QuestionTitleText", "Câu hỏi: Tính (-15) + 7", 26, new Vector2(0, 190), new Vector2(1000, 55), Color.white);
            var diag = CreateText(card, "DiagnosisBodyText", "Nguyên nhân: Em đã quên đặt dấu trừ của số có giá trị tuyệt đối lớn hơn.", 22, new Vector2(0, 90), new Vector2(1000, 110), new Color(0.9f, 0.7f, 0.7f), TextAlignmentOptions.TopLeft);
            var rule = CreateText(card, "RuleReminderText", "Quy tắc cốt lõi: Muốn cộng hai số nguyên khác dấu, ta lấy số lớn trừ số bé và đặt dấu của số lớn hơn trước kết quả.", 22, new Vector2(0, -30), new Vector2(1000, 100), new Color(0.7f, 0.9f, 0.7f), TextAlignmentOptions.TopLeft);
            var analog = CreateText(card, "AnalogousProblemText", "Bài tập tương tự: Hãy tính giá trị biểu thức: (-20) + 12 = ?", 24, new Vector2(0, -150), new Vector2(1000, 80), new Color(0.95f, 0.85f, 0.4f), TextAlignmentOptions.TopLeft);

            var btnPrev = CreateButton(canvas, "Btn_Prev", "< Câu Trước", new Vector2(-250, -250), new Vector2(200, 55), new Color(0.2f, 0.45f, 0.75f));
            var page = CreateText(canvas, "PageIndicatorText", "Câu 1/3", 22, new Vector2(0, -250), new Vector2(150, 45), Color.white);
            var btnNext = CreateButton(canvas, "Btn_Next", "Câu Tiếp Theo >", new Vector2(250, -250), new Vector2(200, 55), new Color(0.2f, 0.45f, 0.75f));

            var manager = new GameObject("_AIFeedbackManager").AddComponent<AIFeedbackUIController>();
            SetField(manager, "tutorGreetingText", greeting);
            SetField(manager, "questionTitleText", qTitle);
            SetField(manager, "diagnosisBodyText", diag);
            SetField(manager, "ruleReminderText", rule);
            SetField(manager, "analogousProblemText", analog);
            SetField(manager, "prevWrongQuestionButton", btnPrev);
            SetField(manager, "pageIndicatorText", page);
            SetField(manager, "nextWrongQuestionButton", btnNext);
            SetField(manager, "backToScoreButton", backScore);
            SetField(manager, "backToMenuButton", backMenu);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/08_AIFeedback.unity");
        }

        // 09_VersusLobby
        private static void BuildVersusLobbyScene()
        {
            CreateBaseScene("09_VersusLobby", out GameObject canvas);

            var backBtn = CreateButton(canvas, "Btn_Back", "< Menu", new Vector2(-780, 480), new Vector2(160, 50), new Color(0.3f, 0.35f, 0.45f));
            CreateText(canvas, "Title", "SẢNH THI ĐẤU ĐỐI KHÁNG TOÁN 6", 36, new Vector2(0, 480), new Vector2(700, 60), new Color(0.95f, 0.75f, 0.2f));

            var card = new GameObject("ScanningCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(canvas.transform, false);
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 350);
            card.GetComponent<Image>().color = new Color(0.12f, 0.17f, 0.24f);

            var scanningText = CreateText(card, "ScanningIndicatorText", "[...] Đang quét danh sách học sinh trực tuyến...", 24, new Vector2(0, 60), new Vector2(700, 60), Color.white);
            var matchBtn = CreateButton(card, "Btn_RandomMatch", "BẮT ĐẦU GHÉP CẶP NGẪU NHIÊN >", new Vector2(0, -60), new Vector2(450, 75), new Color(0.9f, 0.45f, 0.15f));

            var status = CreateText(canvas, "StatusText", "Thời gian thi đấu: 3 phút (180 giây)", 22, new Vector2(0, -180), new Vector2(700, 45), new Color(0.7f, 0.8f, 0.9f));

            // Match Modal
            var matchModal = new GameObject("MatchModal", typeof(RectTransform), typeof(Image));
            matchModal.transform.SetParent(canvas.transform, false);
            SetFullStretch(matchModal.GetComponent<RectTransform>());
            matchModal.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            matchModal.SetActive(false);

            var mCard = new GameObject("MCard", typeof(RectTransform), typeof(Image));
            mCard.transform.SetParent(matchModal.transform, false);
            mCard.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 420);
            mCard.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.28f);

            var foundText = CreateText(mCard, "MatchFoundText", "ĐÃ TÌM THẤY ĐỐI THỦ!\nBảoToánHọc (Lớp 6A2)", 26, new Vector2(0, 110), new Vector2(500, 80), new Color(0.95f, 0.75f, 0.2f));
            var countdown = CreateText(mCard, "CountdownAcceptText", "Tự động hủy sau: 10s", 20, new Vector2(0, 30), new Vector2(500, 40), Color.yellow);
            var btnAccept = CreateButton(mCard, "Btn_Accept", "[OK] CHẤP NHẬN (ACCEPT)", new Vector2(0, -50), new Vector2(400, 60), new Color(0.18f, 0.65f, 0.4f));
            var btnDecline = CreateButton(mCard, "Btn_Decline", "[X] TỪ CHỐI", new Vector2(0, -130), new Vector2(400, 50), new Color(0.8f, 0.3f, 0.3f));

            // Mode selection Modal
            var modeModal = new GameObject("ModeSelectionModal", typeof(RectTransform), typeof(Image));
            modeModal.transform.SetParent(canvas.transform, false);
            SetFullStretch(modeModal.GetComponent<RectTransform>());
            modeModal.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            modeModal.SetActive(false);

            var modeCard = new GameObject("ModeCard", typeof(RectTransform), typeof(Image));
            modeCard.transform.SetParent(modeModal.transform, false);
            modeCard.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 350);
            modeCard.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.28f);

            CreateText(modeCard, "ModeTitle", "CHỌN THỂ THỨC ĐỐI KHÁNG (3 PHÚT)", 26, new Vector2(0, 90), new Vector2(550, 60), new Color(0.95f, 0.75f, 0.2f));
            var btnMcqMode = CreateButton(modeCard, "Btn_ChooseMCQ", "[TN] Đấu Trắc Nghiệm (10 Câu)", new Vector2(0, 0), new Vector2(420, 60), new Color(0.2f, 0.5f, 0.8f));
            var btnEssayMode = CreateButton(modeCard, "Btn_ChooseEssay", "[TL] Đấu Tự Luận (1 Bài Tốc Độ)", new Vector2(0, -80), new Vector2(420, 60), new Color(0.6f, 0.35f, 0.7f));

            var manager = new GameObject("_VersusManager").AddComponent<VersusLobbyUIController>();
            SetField(manager, "backToMenuButton", backBtn);
            SetField(manager, "statusText", status);
            SetField(manager, "scanningIndicatorText", scanningText);
            SetField(manager, "randomMatchButton", matchBtn);
            SetField(manager, "matchModal", matchModal);
            SetField(manager, "matchFoundText", foundText);
            SetField(manager, "countdownAcceptText", countdown);
            SetField(manager, "acceptButton", btnAccept);
            SetField(manager, "declineButton", btnDecline);
            SetField(manager, "modeSelectionModal", modeModal);
            SetField(manager, "chooseMcqModeButton", btnMcqMode);
            SetField(manager, "chooseEssayModeButton", btnEssayMode);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/09_VersusLobby.unity");
        }

        // 10_PracticeLog
        private static void BuildPracticeLogScene()
        {
            CreateBaseScene("10_PracticeLog", out GameObject canvas);

            var backBtn = CreateButton(canvas, "Btn_Back", "< Menu", new Vector2(-780, 480), new Vector2(160, 50), new Color(0.3f, 0.35f, 0.45f));
            CreateText(canvas, "Title", "NHẬT KÝ LUYỆN TẬP TOÁN 6", 36, new Vector2(0, 480), new Vector2(600, 60), new Color(0.95f, 0.75f, 0.2f));

            var toggleBtn = CreateButton(canvas, "Btn_ToggleMode", "Xem Chi Tiết Theo Bài >", new Vector2(720, 480), new Vector2(280, 50), new Color(0.55f, 0.35f, 0.75f));
            var toggleText = toggleBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Mode 1: Summary
            var sumPanel = new GameObject("Mode1_SummaryPanel", typeof(RectTransform), typeof(Image));
            sumPanel.transform.SetParent(canvas.transform, false);
            sumPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            sumPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 600);
            sumPanel.GetComponent<Image>().color = new Color(0.12f, 0.17f, 0.24f);

            CreateText(sumPanel, "SumHeader", "TỔNG HỢP THÀNH TÍCH HỌC TẬP", 28, new Vector2(0, 230), new Vector2(600, 50), new Color(0.95f, 0.75f, 0.2f));
            var totalLess = CreateText(sumPanel, "TotalLessonsCompletedText", "Số bài học hoàn thành: 3 / 3 Bài", 24, new Vector2(0, 130), new Vector2(700, 50), Color.white);
            var avgScore = CreateText(sumPanel, "AverageScoreText", "Điểm trung bình toàn khóa: 8.8 / 10", 24, new Vector2(0, 50), new Vector2(700, 50), Color.white);
            var totalAtt = CreateText(sumPanel, "TotalAttemptsText", "Tổng số lần rèn luyện: 19 lần", 24, new Vector2(0, -30), new Vector2(700, 50), Color.white);
            var totalTime = CreateText(sumPanel, "TotalTimeText", "Tổng thời gian học tập: 1 giờ 45 phút", 24, new Vector2(0, -110), new Vector2(700, 50), Color.white);

            // Mode 2: Detail
            var detPanel = new GameObject("Mode2_DetailPanel", typeof(RectTransform), typeof(Image));
            detPanel.transform.SetParent(canvas.transform, false);
            detPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            detPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 600);
            detPanel.GetComponent<Image>().color = new Color(0.12f, 0.17f, 0.24f);
            detPanel.SetActive(false);

            CreateText(detPanel, "DetHeader", "CHI TIẾT THEO BÀI HỌC", 28, new Vector2(0, 230), new Vector2(600, 50), new Color(0.95f, 0.75f, 0.2f));

            var prevLessonBtn = CreateButton(detPanel, "Btn_PrevLesson", "<", new Vector2(-310, 140), new Vector2(50, 55), new Color(0.2f, 0.45f, 0.75f));
            var drop = CreateTMPDropdown(detPanel, "LessonDropdown", new Vector2(0, 140), new Vector2(540, 55));
            var nextLessonBtn = CreateButton(detPanel, "Btn_NextLesson", ">", new Vector2(310, 140), new Vector2(50, 55), new Color(0.2f, 0.45f, 0.75f));

            var selTitle = CreateText(detPanel, "SelectedLessonTitleText", "Bài 1: Phép cộng và trừ số nguyên", 24, new Vector2(0, 60), new Vector2(700, 45), Color.yellow);
            var attCount = CreateText(detPanel, "AttemptsCountText", "Số lần làm bài: 8 lần", 22, new Vector2(0, 0), new Vector2(700, 40), Color.white);
            var hiScore = CreateText(detPanel, "HighestScoreText", "Điểm cao nhất: 10/10", 22, new Vector2(0, -50), new Vector2(700, 40), Color.green);
            var lastScore = CreateText(detPanel, "LastScoreText", "Điểm lần cuối cùng: 9/10", 22, new Vector2(0, -100), new Vector2(700, 40), Color.white);
            var lastDate = CreateText(detPanel, "LastDateText", "Lần cuối: Hôm qua lúc 19:30", 20, new Vector2(0, -150), new Vector2(700, 40), new Color(0.7f, 0.7f, 0.8f));

            var manager = new GameObject("_PracticeLogManager").AddComponent<PracticeLogUIController>();
            SetField(manager, "backToMenuButton", backBtn);
            SetField(manager, "toggleModeButton", toggleBtn);
            SetField(manager, "toggleModeButtonText", toggleText);
            SetField(manager, "summaryPanel", sumPanel);
            SetField(manager, "totalLessonsCompletedText", totalLess);
            SetField(manager, "averageScoreText", avgScore);
            SetField(manager, "totalAttemptsText", totalAtt);
            SetField(manager, "totalTimeText", totalTime);
            SetField(manager, "detailPanel", detPanel);
            SetField(manager, "lessonDropdown", drop);
            SetField(manager, "prevLessonButton", prevLessonBtn);
            SetField(manager, "nextLessonButton", nextLessonBtn);
            SetField(manager, "selectedLessonTitleText", selTitle);
            SetField(manager, "attemptsCountText", attCount);
            SetField(manager, "highestScoreText", hiScore);
            SetField(manager, "lastScoreText", lastScore);
            SetField(manager, "lastDateText", lastDate);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/10_PracticeLog.unity");
        }

        // 11_Settings
        private static void BuildSettingsScene()
        {
            CreateBaseScene("11_Settings", out GameObject canvas);

            var bgImg = canvas.transform.Find("Background")?.GetComponent<Image>();
            var backBtn = CreateButton(canvas, "Btn_Back", "< Menu", new Vector2(-780, 480), new Vector2(160, 50), new Color(0.3f, 0.35f, 0.45f));
            var title = CreateText(canvas, "Title", "CÀI ĐẶT HỆ THỐNG", 36, new Vector2(0, 480), new Vector2(600, 60), new Color(0.95f, 0.75f, 0.2f));

            var card = new GameObject("SettingsCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(canvas.transform, false);
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(850, 780);
            var cardImg = card.GetComponent<Image>();
            cardImg.color = new Color(0.12f, 0.17f, 0.24f);

            // Âm lượng
            CreateText(card, "AudioHeader", "CÀI ĐẶT ÂM THANH", 24, new Vector2(-220, 300), new Vector2(350, 40), new Color(0.95f, 0.75f, 0.2f), TextAlignmentOptions.MidlineLeft);
            
            CreateText(card, "LblMaster", "Âm lượng tổng:", 20, new Vector2(-220, 240), new Vector2(250, 35), Color.white, TextAlignmentOptions.MidlineLeft);
            var slMaster = CreateSlider(card, "MasterSlider", new Vector2(70, 240), new Vector2(300, 24));
            var txtMasterVal = CreateText(card, "MasterValText", "100%", 18, new Vector2(275, 240), new Vector2(80, 35), Color.yellow);

            CreateText(card, "LblBgm", "Nhạc nền (BGM):", 20, new Vector2(-220, 180), new Vector2(250, 35), Color.white, TextAlignmentOptions.MidlineLeft);
            var slBgm = CreateSlider(card, "BgmSlider", new Vector2(70, 180), new Vector2(300, 24));
            var txtBgmVal = CreateText(card, "BgmValText", "80%", 18, new Vector2(275, 180), new Vector2(80, 35), Color.yellow);

            CreateText(card, "LblSfx", "Hiệu ứng (SFX):", 20, new Vector2(-220, 120), new Vector2(250, 35), Color.white, TextAlignmentOptions.MidlineLeft);
            var slSfx = CreateSlider(card, "SfxSlider", new Vector2(70, 120), new Vector2(300, 24));
            var txtSfxVal = CreateText(card, "SfxValText", "100%", 18, new Vector2(275, 120), new Vector2(80, 35), Color.yellow);

            // Hiển thị
            CreateText(card, "DispHeader", "CÀI ĐẶT HIỂN THỊ", 24, new Vector2(-220, 40), new Vector2(350, 40), new Color(0.95f, 0.75f, 0.2f), TextAlignmentOptions.MidlineLeft);
            
            CreateText(card, "LblBright", "Độ sáng màn hình:", 20, new Vector2(-220, -15), new Vector2(250, 35), Color.white, TextAlignmentOptions.MidlineLeft);
            var slBright = CreateSlider(card, "BrightSlider", new Vector2(70, -15), new Vector2(300, 24));
            var txtBrightVal = CreateText(card, "BrightValText", "100%", 18, new Vector2(275, -15), new Vector2(80, 35), Color.yellow);

            var darkToggle = CreateToggle(card, "DarkModeToggle", "Bật giao diện ban đêm (Dark Mode)", new Vector2(-220, -70), new Vector2(480, 40));

            // Profile
            CreateText(card, "ProfHeader", "ĐỔI BÍ DANH HỌC SINH", 24, new Vector2(-220, -140), new Vector2(350, 40), new Color(0.95f, 0.75f, 0.2f), TextAlignmentOptions.MidlineLeft);
            var nickInput = CreateInputField(card, "NicknameInput", "Nhập bí danh hiển thị...", new Vector2(0, -200), new Vector2(550, 50));

            var saveBtn = CreateButton(card, "Btn_Save", "LƯU CÀI ĐẶT", new Vector2(0, -275), new Vector2(300, 55), new Color(0.18f, 0.65f, 0.4f));
            var toast = CreateText(card, "ToastText", "", 20, new Vector2(0, -340), new Vector2(700, 35), Color.green);

            var manager = new GameObject("_SettingsManager").AddComponent<SettingsUIController>();
            SetField(manager, "backToMenuButton", backBtn);
            SetField(manager, "masterVolumeSlider", slMaster);
            SetField(manager, "masterValueText", txtMasterVal);
            SetField(manager, "bgmVolumeSlider", slBgm);
            SetField(manager, "bgmValueText", txtBgmVal);
            SetField(manager, "sfxVolumeSlider", slSfx);
            SetField(manager, "sfxValueText", txtSfxVal);
            SetField(manager, "brightnessSlider", slBright);
            SetField(manager, "brightnessValueText", txtBrightVal);
            SetField(manager, "darkModeToggle", darkToggle);
            SetField(manager, "settingsBackground", bgImg);
            SetField(manager, "settingsCard", cardImg);
            SetField(manager, "settingsTitleText", title);
            SetField(manager, "nicknameInputField", nickInput);
            SetField(manager, "saveSettingsButton", saveBtn);
            SetField(manager, "notifyToastText", toast);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/11_Settings.unity");
        }
        #endregion

        #region Register Build Settings
        private static void RegisterScenesInBuildSettings()
        {
            string[] scenePaths = new string[]
            {
                $"{SCENES_DIR}/00_Bootstrapper.unity",
                $"{SCENES_DIR}/01_Login.unity",
                $"{SCENES_DIR}/02_MainMenu.unity",
                $"{SCENES_DIR}/03_LessonList.unity",
                $"{SCENES_DIR}/04_LessonContent.unity",
                $"{SCENES_DIR}/05_MCQPractice.unity",
                $"{SCENES_DIR}/06_EssayPractice.unity",
                $"{SCENES_DIR}/07_Score.unity",
                $"{SCENES_DIR}/08_AIFeedback.unity",
                $"{SCENES_DIR}/09_VersusLobby.unity",
                $"{SCENES_DIR}/10_PracticeLog.unity",
                $"{SCENES_DIR}/11_Settings.unity"
            };

            var editorScenes = new List<EditorBuildSettingsScene>();
            for (int i = 0; i < scenePaths.Length; i++)
            {
                editorScenes.Add(new EditorBuildSettingsScene(scenePaths[i], true));
            }

            EditorBuildSettings.scenes = editorScenes.ToArray();
        }
        #endregion
    }
}
#endif
