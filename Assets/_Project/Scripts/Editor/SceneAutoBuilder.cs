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
    /// Công cụ Unity Editor tự động tạo, dựng UI hiện đại (Modern AAA/EdTech Aesthetic) 
    /// và liên kết 100% cho toàn bộ 11 Scene trong Math6-AI-Companion.
    /// Trang bị đầy đủ 9-slice rounded sprites, responsive anchors, glassmorphism cards, 
    /// micro-interactions và visual hierarchy chuyên nghiệp.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneAutoBuilder
    {
        private const string SCENES_DIR = "Assets/Scenes";
        private const string UI_SPRITES_DIR = "Assets/_Project/Textures/UI";

        #region Design System Colors
        public static readonly Color ColorBgDark = new Color(0.06f, 0.08f, 0.14f);        // #0F1424 Deep Space Navy
        public static readonly Color ColorCardGlass = new Color(0.09f, 0.13f, 0.22f, 0.95f); // Semi-translucent Glass
        public static readonly Color ColorHeaderBg = new Color(0.07f, 0.10f, 0.18f, 0.98f);  // Top Bar Navy
        public static readonly Color ColorBorderLine = new Color(0.18f, 0.25f, 0.38f, 0.75f);// Border / Divider

        // Brand Accents
        public static readonly Color ColorPrimaryBlue = new Color(0.15f, 0.45f, 0.90f);   // #2673E6 Royal Blue
        public static readonly Color ColorEmeraldGreen = new Color(0.08f, 0.72f, 0.46f);  // #14B875 Mint Green
        public static readonly Color ColorAmberGold = new Color(0.96f, 0.65f, 0.15f);     // #F5A626 Amber Gold
        public static readonly Color ColorCoralRed = new Color(0.90f, 0.26f, 0.26f);      // #E64242 Ruby Red
        public static readonly Color ColorPurpleViolet = new Color(0.55f, 0.35f, 0.92f);  // #8C59EB AI Magic
        public static readonly Color ColorCyanTeal = new Color(0.06f, 0.68f, 0.85f);      // #0FAED9 Arena Cyan
        public static readonly Color ColorSlateSecondary = new Color(0.20f, 0.26f, 0.36f);// Dark Slate Neutral

        // Typography
        public static readonly Color ColorTextTitle = new Color(0.98f, 0.98f, 1.0f);      // Pure White
        public static readonly Color ColorTextBody = new Color(0.90f, 0.93f, 0.98f);       // Soft White
        public static readonly Color ColorTextMuted = new Color(0.60f, 0.68f, 0.80f);      // Cool Slate Gray
        #endregion

        #region Sprites Cache
        private static Sprite s_roundedRect;
        private static Sprite s_cardGlass;
        private static Sprite s_buttonModern;
        private static Sprite s_pill;
        private static Sprite s_circle;
        private static Sprite s_inputField;
        #endregion

        static SceneAutoBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                string flagPath = "Temp/Math6_ScenesBuilt_V10.flag";
                if (File.Exists(flagPath)) return;

                try
                {
                    File.WriteAllText(flagPath, DateTime.UtcNow.ToString());
                    Debug.Log("[Math6 SceneAutoBuilder] 🔄 Tự động đồng bộ hóa & nâng cấp giao diện toàn bộ 11 Scene sang phong cách Modern EdTech...");
                    ExecuteBuildAllScenesDirect(false);
                    Debug.Log("[Math6 SceneAutoBuilder] ✅ Đã tự động cập nhật xong toàn bộ 11 Scene giao diện mới đẹp mắt!");
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

        public static void GenerateAllScenesBatch()
        {
            ExecuteBuildAllScenesDirect(false);
        }

        [MenuItem("Math6 / 🧹 DỌN DẸP SCRIPT MISSING TRONG TẤT CẢ SCENE", priority = 10)]
        public static void CleanMissingScriptsInAllScenes()
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { SCENES_DIR });
            int totalRemoved = 0;
            foreach (var guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    var all = root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in all)
                    {
                        totalRemoved += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                    }
                }
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[Math6] ✅ Đã dọn dẹp sạch {totalRemoved} script missing trong toàn bộ Scene!");
        }

        public static void ExecuteBuildAllScenesDirect(bool showDialog)
        {
            if (!Directory.Exists(SCENES_DIR))
            {
                Directory.CreateDirectory(SCENES_DIR);
            }

            EnsureUISprites();

            if (showDialog)
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Xác nhận nâng cấp UI cho toàn bộ 11 Scene",
                    "Công cụ sẽ tự động thiết kế lại giao diện hiện đại (Modern EdTech) cho toàn bộ 11 Scene:\n\n" +
                    "00_Bootstrapper\n01_Login\n02_MainMenu\n03_LessonList\n04_LessonContent\n" +
                    "05_MCQPractice\n06_EssayPractice\n07_Score\n08_AIFeedback\n09_VersusLobby\n" +
                    "10_PracticeLog\n11_Settings\n\nBạn có muốn bắt đầu?",
                    "Nâng Cấp Ngay!", "Hủy"
                );

                if (!confirm) return;
            }

            try
            {
                if (showDialog) EditorUtility.DisplayProgressBar("Đang nâng cấp Scene", "00_Bootstrapper...", 0.08f);
                BuildBootstrapperScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang nâng cấp Scene", "01_Login...", 0.16f);
                BuildLoginScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang nâng cấp Scene", "02_MainMenu...", 0.25f);
                BuildMainMenuScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang nâng cấp Scene", "03_LessonList...", 0.33f);
                BuildLessonListScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang nâng cấp Scene", "04_LessonContent...", 0.41f);
                BuildLessonContentScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang nâng cấp Scene", "05_MCQPractice...", 0.50f);
                BuildMCQPracticeScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang nâng cấp Scene", "06_EssayPractice...", 0.58f);
                BuildEssayPracticeScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang nâng cấp Scene", "07_Score...", 0.66f);
                BuildScoreScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang nâng cấp Scene", "08_AIFeedback...", 0.75f);
                BuildAIFeedbackScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang nâng cấp Scene", "09_VersusLobby...", 0.83f);
                BuildVersusLobbyScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang nâng cấp Scene", "10_PracticeLog...", 0.91f);
                BuildPracticeLogScene();

                if (showDialog) EditorUtility.DisplayProgressBar("Đang nâng cấp Scene", "11_Settings...", 0.98f);
                BuildSettingsScene();

                RegisterScenesInBuildSettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorSceneManager.OpenScene($"{SCENES_DIR}/00_Bootstrapper.unity");

                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Hoàn Tất Nâng Cấp!",
                        "✅ Đã nâng cấp toàn bộ 11 Scene với giao diện hiện đại, bo góc mềm mại, đổ bóng và màu sắc chuẩn UI EdTech!\n" +
                        "✅ Giữ nguyên 100% liên kết code và logic gameplay không đổi.",
                        "Tuyệt Vời!"
                    );
                }
            }
            finally
            {
                if (showDialog) EditorUtility.ClearProgressBar();
            }
        }

        #region Sprite Loader Helper
        private static void EnsureUISprites()
        {
            AssetDatabase.Refresh();
            s_roundedRect = LoadOrCreateUISprite("ui_rounded_rect.png", new Vector4(28, 28, 28, 28));
            s_cardGlass = LoadOrCreateUISprite("ui_card_glass.png", new Vector4(24, 24, 24, 24));
            s_buttonModern = LoadOrCreateUISprite("ui_button_modern.png", new Vector4(26, 26, 26, 26));
            s_pill = LoadOrCreateUISprite("ui_pill.png", new Vector4(30, 20, 30, 20));
            s_circle = LoadOrCreateUISprite("ui_circle.png", Vector4.zero);
            s_inputField = LoadOrCreateUISprite("ui_input_field.png", new Vector4(18, 16, 18, 16));
            Debug.Log($"[Math6 SceneAutoBuilder] 🎨 Đã tải thành công bộ Sprites UI: Glass={s_cardGlass != null}, Button={s_buttonModern != null}, Pill={s_pill != null}, Circle={s_circle != null}, Input={s_inputField != null}");
        }

        private static Sprite LoadOrCreateUISprite(string fileName, Vector4 border)
        {
            string path = $"{UI_SPRITES_DIR}/{fileName}";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (importer.spriteBorder != border) { importer.spriteBorder = border; dirty = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                var all = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var a in all)
                {
                    if (a is Sprite s) return s;
                }
            }
            return sprite;
        }
        #endregion

        #region Base UI Factory Helpers
        private static Canvas CreateBaseScene(string sceneName, out GameObject canvasObj)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 0. Main Camera
            var cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            var cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ColorBgDark;
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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 3. Deep Background
            var bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            SetFullStretch(bgRect);
            bgObj.GetComponent<Image>().color = ColorBgDark;

            return canvas;
        }

        private static void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static TextMeshProUGUI CreateText(GameObject parent, string name, string content, float fontSize, Vector2 pos, Vector2 size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            if (anchorMin.HasValue) rt.anchorMin = anchorMin.Value;
            if (anchorMax.HasValue) rt.anchorMax = anchorMax.Value;
            if (pivot.HasValue) rt.pivot = pivot.Value;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            return tmp;
        }

        private static GameObject CreateCard(GameObject parent, string name, Vector2 pos, Vector2 size, Color? cardColor = null, Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            if (anchorMin.HasValue) rt.anchorMin = anchorMin.Value;
            if (anchorMax.HasValue) rt.anchorMax = anchorMax.Value;
            if (pivot.HasValue) rt.pivot = pivot.Value;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.sprite = s_cardGlass;
            img.type = Image.Type.Sliced;
            img.color = cardColor ?? ColorCardGlass;
            return go;
        }

        private static Button CreateButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size, Color btnColor, Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null, float fontSize = 20f, Sprite customSprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            if (anchorMin.HasValue) rt.anchorMin = anchorMin.Value;
            if (anchorMax.HasValue) rt.anchorMax = anchorMax.Value;
            if (pivot.HasValue) rt.pivot = pivot.Value;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.sprite = customSprite ?? s_buttonModern;
            img.type = Image.Type.Sliced;
            img.color = btnColor;

            var btn = go.GetComponent<Button>();
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            // Interactive Color Tint Feedback
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.normalColor = btnColor;
            colors.highlightedColor = Color.Lerp(btnColor, Color.white, 0.25f);
            colors.pressedColor = Color.Lerp(btnColor, Color.black, 0.22f);
            colors.selectedColor = Color.Lerp(btnColor, Color.white, 0.15f);
            colors.disabledColor = new Color(0.35f, 0.40f, 0.48f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            SetFullStretch(textRt);
            textRt.offsetMin = new Vector2(10, 0);
            textRt.offsetMax = new Vector2(-10, 0);

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static GameObject CreateBadge(GameObject parent, string name, string text, Vector2 pos, Vector2 size, Color badgeColor, float fontSize = 14f, Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            if (anchorMin.HasValue) rt.anchorMin = anchorMin.Value;
            if (anchorMax.HasValue) rt.anchorMax = anchorMax.Value;
            if (pivot.HasValue) rt.pivot = pivot.Value;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.sprite = s_pill;
            img.type = Image.Type.Sliced;
            img.color = badgeColor;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            SetFullStretch(textGo.GetComponent<RectTransform>());

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return go;
        }

        private static Button CreateTopLeftButton(GameObject parent, string name, string label, Vector2 size, Color btnColor, float offsetX = 35f, float offsetY = 25f, float fontSize = 18f)
        {
            return CreateButton(parent, name, label, new Vector2(offsetX, -offsetY), size, btnColor,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), fontSize);
        }

        private static Button CreateTopRightButton(GameObject parent, string name, string label, Vector2 size, Color btnColor, float offsetX = 35f, float offsetY = 25f, float fontSize = 18f)
        {
            return CreateButton(parent, name, label, new Vector2(-offsetX, -offsetY), size, btnColor,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), fontSize);
        }

        private static TextMeshProUGUI CreateTopCenterText(GameObject parent, string name, string content, float fontSize, Vector2 size, Color color, float offsetY = 22f, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            return CreateText(parent, name, content, fontSize, new Vector2(0f, -offsetY), size, color, align,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), FontStyles.Bold);
        }

        private static Button CreateBottomCenterButton(GameObject parent, string name, string label, Vector2 size, Color btnColor, float offsetY = 35f, float fontSize = 20f)
        {
            return CreateButton(parent, name, label, new Vector2(0f, offsetY), size, btnColor,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), fontSize);
        }

        private static Slider CreateSlider(GameObject parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            // Background track
            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            SetFullStretch(bg.GetComponent<RectTransform>());
            var bgImage = bg.GetComponent<Image>();
            bgImage.sprite = s_pill;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.12f, 0.16f, 0.25f);
            bgImage.raycastTarget = true;

            // Fill Area
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0, 0.15f);
            fillAreaRt.anchorMax = new Vector2(1, 0.85f);
            fillAreaRt.offsetMin = new Vector2(4, 0);
            fillAreaRt.offsetMax = new Vector2(-4, 0);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            SetFullStretch(fill.GetComponent<RectTransform>());
            var fillImage = fill.GetComponent<Image>();
            fillImage.sprite = s_pill;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = ColorEmeraldGreen;
            fillImage.raycastTarget = false;

            // Handle Slide Area
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(size.y / 2f, 0);
            handleAreaRt.offsetMax = new Vector2(-size.y / 2f, 0);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(size.y + 6, size.y + 6);
            var handleImage = handle.GetComponent<Image>();
            handleImage.sprite = s_circle;
            handleImage.color = Color.white;
            handleImage.raycastTarget = true;

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }

        private static Toggle CreateToggle(GameObject parent, string name, string labelText, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.5f);
            bgRt.anchorMax = new Vector2(0, 0.5f);
            bgRt.pivot = new Vector2(0, 0.5f);
            bgRt.anchoredPosition = new Vector2(0, 0);
            bgRt.sizeDelta = new Vector2(46, 26);
            var bgImage = bg.GetComponent<Image>();
            bgImage.sprite = s_pill;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.18f, 0.24f, 0.35f);
            bgImage.raycastTarget = true;

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(bg.transform, false);
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.5f, 0.5f);
            checkRt.anchorMax = new Vector2(0.5f, 0.5f);
            checkRt.anchoredPosition = Vector2.zero;
            checkRt.sizeDelta = new Vector2(18, 18);
            var checkImage = check.GetComponent<Image>();
            checkImage.sprite = s_circle;
            checkImage.color = ColorEmeraldGreen;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 1);
            labelRt.offsetMin = new Vector2(58, 0);
            labelRt.offsetMax = Vector2.zero;

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 20;
            tmp.color = ColorTextBody;
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

            var img = go.GetComponent<Image>();
            img.sprite = s_inputField;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.08f, 0.12f, 0.20f, 0.95f);

            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(go.transform, false);
            var areaRt = textArea.GetComponent<RectTransform>();
            SetFullStretch(areaRt);
            areaRt.offsetMin = new Vector2(18, 5);
            areaRt.offsetMax = new Vector2(-18, -5);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(textArea.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            SetFullStretch(textRt);

            var textTmp = textGo.GetComponent<TextMeshProUGUI>();
            textTmp.fontSize = 20;
            textTmp.color = ColorTextTitle;
            textTmp.alignment = TextAlignmentOptions.MidlineLeft;
            textTmp.richText = false;
            textTmp.raycastTarget = false;

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            phGo.transform.SetParent(textArea.transform, false);
            var phRt = phGo.GetComponent<RectTransform>();
            SetFullStretch(phRt);

            var phTmp = phGo.GetComponent<TextMeshProUGUI>();
            phTmp.text = placeholderText;
            phTmp.fontSize = 20;
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.color = new Color(0.55f, 0.65f, 0.78f, 0.65f);
            phTmp.alignment = TextAlignmentOptions.MidlineLeft;
            phTmp.richText = false;
            phTmp.raycastTarget = false;

            var input = go.GetComponent<TMP_InputField>();
            input.textViewport = areaRt;
            input.textComponent = textTmp;
            input.placeholder = phTmp;
            input.richText = false;

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
            rootImg.sprite = s_inputField;
            rootImg.type = Image.Type.Sliced;
            rootImg.color = new Color(0.12f, 0.17f, 0.28f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            SetFullStretch(labelRt);
            labelRt.offsetMin = new Vector2(16, 2);
            labelRt.offsetMax = new Vector2(-40, -2);
            var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
            labelTmp.fontSize = 20;
            labelTmp.color = Color.white;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
            arrowGo.transform.SetParent(go.transform, false);
            var arrowRt = arrowGo.GetComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(1, 0.5f);
            arrowRt.anchorMax = new Vector2(1, 0.5f);
            arrowRt.pivot = new Vector2(1, 0.5f);
            arrowRt.anchoredPosition = new Vector2(-15, 0);
            arrowRt.sizeDelta = new Vector2(25, 25);
            var arrowTmp = arrowGo.GetComponent<TextMeshProUGUI>();
            arrowTmp.text = "▼";
            arrowTmp.fontSize = 14;
            arrowTmp.color = ColorTextMuted;
            arrowTmp.alignment = TextAlignmentOptions.Center;

            var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(go.transform, false);
            var tRt = template.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 0);
            tRt.anchorMax = new Vector2(1, 0);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -5);
            tRt.sizeDelta = new Vector2(0, 200);
            var templateImg = template.GetComponent<Image>();
            templateImg.sprite = s_cardGlass;
            templateImg.type = Image.Type.Sliced;
            templateImg.color = new Color(0.09f, 0.13f, 0.22f, 0.98f);
            template.SetActive(false);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(template.transform, false);
            SetFullStretch(viewport.GetComponent<RectTransform>());

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.sizeDelta = new Vector2(0, 50);

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            var itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 1);
            itemRt.anchorMax = new Vector2(1, 1);
            itemRt.pivot = new Vector2(0.5f, 1);
            itemRt.sizeDelta = new Vector2(0, 45);

            var itemLabelGo = new GameObject("Item Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            itemLabelGo.transform.SetParent(item.transform, false);
            var itemLabelRt = itemLabelGo.GetComponent<RectTransform>();
            SetFullStretch(itemLabelRt);
            itemLabelRt.offsetMin = new Vector2(15, 0);
            var itemLabelTmp = itemLabelGo.GetComponent<TextMeshProUGUI>();
            itemLabelTmp.fontSize = 18;
            itemLabelTmp.color = Color.white;
            itemLabelTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var sr = template.GetComponent<ScrollRect>();
            sr.content = cRt;
            sr.viewport = viewport.GetComponent<RectTransform>();

            var dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.template = tRt;
            dropdown.captionText = labelTmp;
            dropdown.itemText = itemLabelTmp;

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

        #region 11 Scene Builders (Modern EdTech Redesign)
        // 00_Bootstrapper
        private static void BuildBootstrapperScene()
        {
            CreateBaseScene("00_Bootstrapper", out GameObject canvas);

            // Center Hero Container
            var hero = CreateCard(canvas, "HeroCard", new Vector2(0, 60), new Vector2(750, 360));
            
            // App Badge
            CreateBadge(hero, "AppBadge", "📐 HỆ THỐNG HỌC TẬP THÔNG MINH 4.0", new Vector2(0, 110), new Vector2(360, 36), ColorPrimaryBlue, 14);

            CreateText(hero, "Title", "MATH 6 • AI COMPANION", 46, new Vector2(0, 50), new Vector2(700, 70), ColorAmberGold, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);
            CreateText(hero, "SubTitle", "Ôn Luyện Toán 6 Theo SGK Mới • Tích Hợp Gia Sư Trực Tuyến & Thế Giới 3D", 18, new Vector2(0, -10), new Vector2(680, 50), ColorTextBody);

            // Loading Progress Bar
            var slider = CreateSlider(canvas, "LoadingSlider", new Vector2(0, 150), new Vector2(640, 22));
            SetRect(slider.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(640, 22));

            var statusText = CreateText(canvas, "StatusText", "Đang khởi tạo tài nguyên hệ thống...", 18, new Vector2(0, 95), new Vector2(600, 35), ColorTextMuted, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            CreateText(canvas, "VersionText", "Phiên bản v1.2 • Bộ Giáo Dục & Đào Tạo", 14, new Vector2(0, 40), new Vector2(400, 30), new Color(0.45f, 0.55f, 0.68f), TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

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

            // Floating Modern Glass Card
            var panel = CreateCard(canvas, "LoginCard", Vector2.zero, new Vector2(520, 620));

            CreateBadge(panel, "LoginBadge", "📐 TOÁN 6 AI COMPANION", new Vector2(0, 245), new Vector2(250, 32), ColorPrimaryBlue, 13);
            CreateText(panel, "CardTitle", "ĐĂNG NHẬP", 32, new Vector2(0, 195), new Vector2(400, 50), ColorTextTitle, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);
            CreateText(panel, "CardSubTitle", "Học tập thông minh cùng gia sư ảo Gemini", 16, new Vector2(0, 155), new Vector2(400, 30), ColorTextMuted);

            var userInput = CreateInputField(panel, "UsernameInput", "Tên đăng nhập...", new Vector2(0, 85), new Vector2(420, 55));
            var passInput = CreateInputField(panel, "PasswordInput", "Mật khẩu...", new Vector2(0, 15), new Vector2(420, 55));
            passInput.contentType = TMP_InputField.ContentType.Password;

            var loginBtn = CreateButton(panel, "LoginButton", "BẮT ĐẦU HỌC NGAY  🚀", new Vector2(0, -60), new Vector2(420, 56), ColorPrimaryBlue, null, null, null, 20);
            var status = CreateText(panel, "StatusText", "", 16, new Vector2(0, -115), new Vector2(420, 30), ColorAmberGold);

            // Quick login outline pills
            CreateText(panel, "QuickLabel", "── Hoặc chọn tài khoản mẫu ──", 14, new Vector2(0, -155), new Vector2(300, 25), ColorTextMuted);
            var btnStudent = CreateButton(panel, "Btn_QuickStudent", "👨‍🎓 Học Sinh (student_01)", new Vector2(0, -195), new Vector2(420, 44), ColorEmeraldGreen, null, null, null, 16);
            var btnTeacher = CreateButton(panel, "Btn_QuickTeacher", "👩‍🏫 Giáo Viên (teacher_01)", new Vector2(0, -250), new Vector2(420, 44), ColorSlateSecondary, null, null, null, 16);

            var loading = new GameObject("LoadingIndicator", typeof(RectTransform));
            loading.transform.SetParent(panel.transform, false);
            loading.SetActive(false);

            var manager = new GameObject("_LoginManager").AddComponent<LoginUIController>();
            SetField(manager, "usernameInput", userInput);
            SetField(manager, "passwordInput", passInput);
            SetField(manager, "loginButton", loginBtn);
            SetField(manager, "statusText", status);
            SetField(manager, "loadingIndicator", loading);

            btnStudent.onClick.AddListener(() => manager.QuickFillAccount("student_01", "123"));
            btnTeacher.onClick.AddListener(() => manager.QuickFillAccount("teacher_01", "123"));

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/01_Login.unity");
        }

        // 02_MainMenu
        private static void BuildMainMenuScene()
        {
            CreateBaseScene("02_MainMenu", out GameObject canvas);

            // Modern Top Header Bar (Anchor: Top Stretch)
            var header = new GameObject("ProfileHeader", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(canvas.transform, false);
            var hRt = header.GetComponent<RectTransform>();
            SetRect(hRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0, 85));
            header.GetComponent<Image>().color = ColorHeaderBg;

            // Header Bottom Divider
            var div = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            div.transform.SetParent(header.transform, false);
            SetRect(div.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0, 2));
            div.GetComponent<Image>().color = ColorBorderLine;

            // Avatar Ring & Icon
            var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatar.transform.SetParent(header.transform, false);
            SetRect(avatar.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(35, 0), new Vector2(56, 56));
            var avImg = avatar.GetComponent<Image>();
            avImg.sprite = s_circle;
            avImg.color = ColorAmberGold;

            var nickText = CreateText(header, "NicknameText", "AnThầnĐồng", 22, new Vector2(105, 12), new Vector2(250, 30), ColorTextTitle, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), FontStyles.Bold);
            var subText = CreateText(header, "UserSubInfoText", "Lớp 6A1 • THCS Chu Văn An", 15, new Vector2(105, -14), new Vector2(250, 24), ColorTextMuted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));

            // Stats Badges (Center-Right Header)
            CreateBadge(header, "StreakBadge", "🔥 5 Ngày Học", new Vector2(-195, 0), new Vector2(130, 36), new Color(0.85f, 0.40f, 0.10f), 14, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
            var logoutBtn = CreateButton(header, "Btn_Logout", "Đăng xuất ⎋", new Vector2(-35, 0), new Vector2(130, 42), ColorCoralRed, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), 16);

            // Main Menu Title
            CreateTopCenterText(canvas, "MenuTitle", "TRUNG TÂM HỌC TẬP TOÁN 6", 34, new Vector2(800, 50), ColorAmberGold, 115);
            CreateText(canvas, "MenuSub", "Chọn một phương thức học tập để rèn luyện kỹ năng", 16, new Vector2(0, -155), new Vector2(800, 30), ColorTextMuted, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            // 6 Interactive Game Mode Cards (Modern Palette)
            var btnLesson = CreateButton(canvas, "Btn_LessonList", "📘  1. BÀI HỌC TOÁN 6\n<size=15><color=#A8C5E8>Lý thuyết & Video SGK Mới</color></size>", new Vector2(-370, 115), new Vector2(340, 130), ColorPrimaryBlue, null, null, null, 19);
            var btnMCQ = CreateButton(canvas, "Btn_MCQPractice", "🎯  2. LUYỆN TRẮC NGHIỆM\n<size=15><color=#A6E8C8>Ngân hàng câu hỏi thông minh</color></size>", new Vector2(0, 115), new Vector2(340, 130), ColorEmeraldGreen, null, null, null, 19);
            var btnEssay = CreateButton(canvas, "Btn_EssayPractice", "📝  3. LUYỆN TỰ LUẬN\n<size=15><color=#D8B4F8>Chụp ảnh bài, AI chấm bước</color></size>", new Vector2(370, 115), new Vector2(340, 130), ColorPurpleViolet, null, null, null, 19);
            var btnVersus = CreateButton(canvas, "Btn_VersusLobby", "⚔️  4. ĐẤU ĐỐI KHÁNG (3P)\n<size=15><color=#FCD34D>Thi đấu tốc độ thời gian thực</color></size>", new Vector2(-370, -35), new Vector2(340, 130), new Color(0.88f, 0.42f, 0.12f), null, null, null, 19);
            var btnLog = CreateButton(canvas, "Btn_PracticeLog", "📊  5. NHẬT KÝ HỌC TẬP\n<size=15><color=#BAE6FD>Thành tích & Tiến độ rèn luyện</color></size>", new Vector2(0, -35), new Vector2(340, 130), ColorCyanTeal, null, null, null, 19);
            var btnSettings = CreateButton(canvas, "Btn_Settings", "⚙️  6. CÀI ĐẶT HỆ THỐNG\n<size=15><color=#CBD5E1>Âm thanh, đồ họa & tài khoản</color></size>", new Vector2(370, -35), new Vector2(340, 130), ColorSlateSecondary, null, null, null, 19);

            // Hero 3D Village Banner (Bottom-Center Anchor)
            var btnVillage3D = CreateBottomCenterButton(canvas, "Btn_Village3D", "🏡  KHÁM PHÁ THẾ GIỚI 3D LÀNG SEN TRI THỨC  ✨\n<size=15><color=#D1FAE5>Tương tác thời gian thực • Làm nhiệm vụ học tập thế giới mở</color></size>", new Vector2(780, 75), ColorEmeraldGreen, 30, 20);

            var manager = new GameObject("_MainMenuManager").AddComponent<MainMenuUIController>();
            SetField(manager, "nicknameText", nickText);
            SetField(manager, "userSubInfoText", subText);
            SetField(manager, "avatarImage", avatar.GetComponent<Image>());
            SetField(manager, "enterVillage3DButton", btnVillage3D);
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

            var backBtn = CreateTopLeftButton(canvas, "Btn_Back", "← Quay lại", new Vector2(130, 44), ColorSlateSecondary, 35, 20);
            CreateTopCenterText(canvas, "Title", "CHUYÊN ĐỀ TOÁN HỌC LỚP 6", 32, new Vector2(650, 48), ColorAmberGold, 18);
            CreateBadge(canvas, "CurriculumBadge", "SGK MỚI", new Vector2(-35, -20), new Vector2(110, 36), ColorPrimaryBlue, 13, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));

            // Scroll View - Responsive Stretch: 5% margin each side
            var scrollGo = new GameObject("ScrollView_Lessons", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(canvas.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            SetRect(scrollRt, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var scrollImg = scrollGo.GetComponent<Image>();
            scrollImg.sprite = s_cardGlass;
            scrollImg.type = Image.Type.Sliced;
            scrollImg.color = new Color(0.08f, 0.11f, 0.18f, 0.90f);

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
            vlg.spacing = 16;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.padding = new RectOffset(25, 25, 25, 25);

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scrollGo.GetComponent<ScrollRect>();
            sr.content = cRt;
            sr.viewport = viewport.GetComponent<RectTransform>();
            sr.scrollSensitivity = 38f;
            sr.inertia = true;
            sr.decelerationRate = 0.135f;
            sr.movementType = ScrollRect.MovementType.Elastic;

            // Action Modal (Modern Glass Popup)
            var modal = new GameObject("ActionModal", typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(canvas.transform, false);
            SetFullStretch(modal.GetComponent<RectTransform>());
            modal.GetComponent<Image>().color = new Color(0, 0, 0, 0.80f);
            modal.SetActive(false);

            var card = CreateCard(modal, "ModalCard", Vector2.zero, new Vector2(660, 580));

            var modalTitle = CreateText(card, "ModalLessonTitle", "Tên Bài Học", 26, new Vector2(0, 225), new Vector2(560, 48), ColorAmberGold, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);
            var modalStats = CreateText(card, "ModalStatsText", "Điểm cao nhất: 10/10 • Top 1: AnThầnĐồng (15 lần)", 17, new Vector2(0, 175), new Vector2(560, 35), ColorTextMuted);

            var btnText = CreateButton(card, "Btn_TextLesson", "📖  Xem Tóm Tắt Lý Thuyết (SGK)", new Vector2(0, 105), new Vector2(540, 54), ColorPrimaryBlue, null, null, null, 18);
            var btnVideo = CreateButton(card, "Btn_VideoLesson", "🎬  Xem Video Bài Giảng Trực Quan", new Vector2(0, 35), new Vector2(540, 54), ColorCoralRed, null, null, null, 18);
            var btnMCQ = CreateButton(card, "Btn_MCQPractice", "🎯  Luyện Tập Trắc Nghiệm Thông Minh", new Vector2(0, -35), new Vector2(540, 54), ColorEmeraldGreen, null, null, null, 18);
            var btnEssay = CreateButton(card, "Btn_EssayPractice", "📝  Luyện Tự Luận & Nhờ AI Chấm", new Vector2(0, -105), new Vector2(540, 54), ColorPurpleViolet, null, null, null, 18);
            var btnLog = CreateButton(card, "Btn_PracticeLog", "📊  Xem Lịch Sử Điểm Của Bài Này", new Vector2(0, -175), new Vector2(540, 54), ColorCyanTeal, null, null, null, 18);
            var btnClose = CreateButton(card, "Btn_CloseModal", "✕", new Vector2(290, 250), new Vector2(44, 44), ColorSlateSecondary, null, null, null, 20, s_circle);

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

            var backBtn = CreateTopLeftButton(canvas, "Btn_BackToList", "← Danh Sách Bài", new Vector2(170, 44), ColorSlateSecondary, 35, 20);
            var title = CreateTopCenterText(canvas, "LessonTitleText", "Bài 1: Phép cộng và phép trừ số nguyên", 30, new Vector2(850, 48), ColorAmberGold, 18);

            // Tab Buttons
            var btnTabTxt = CreateButton(canvas, "TabBtn_Text", "📖  Lý Thuyết Tóm Tắt", new Vector2(-165, -85), new Vector2(300, 50), ColorPrimaryBlue, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 18);
            var btnTabVid = CreateButton(canvas, "TabBtn_Video", "🎬  Video Bài Giảng", new Vector2(165, -85), new Vector2(300, 50), ColorSlateSecondary, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 18);

            // Responsive Content Panels
            var txtPanel = CreateCard(canvas, "TextContentPanel", Vector2.zero, Vector2.zero, null, new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.84f), new Vector2(0.5f, 0.5f));
            var theoryText = CreateText(txtPanel, "TheoryBodyText", "Nội dung lý thuyết trọng tâm Toán 6...", 22, Vector2.zero, new Vector2(-60, -50), ColorTextBody, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));

            var vidPanel = CreateCard(canvas, "VideoContentPanel", Vector2.zero, Vector2.zero, null, new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.84f), new Vector2(0.5f, 0.5f));
            vidPanel.SetActive(false);

            var vidInfo = CreateText(vidPanel, "VideoInfoText", "Video bài giảng minh họa trực quan chuẩn Bộ GD&ĐT", 24, new Vector2(0, 60), new Vector2(900, 80), ColorTextTitle, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);
            var playVidBtn = CreateButton(vidPanel, "Btn_PlayExternalVideo", "▶   MỞ XEM VIDEO BÀI GIẢNG FULL HD", new Vector2(0, -40), new Vector2(450, 65), ColorCoralRed, null, null, null, 19);

            // Bottom CTA Buttons
            var btnStartMCQ = CreateButton(canvas, "Btn_StartMCQ", "🎯  Làm Bài Trắc Nghiệm Này", new Vector2(-190, 32), new Vector2(350, 56), ColorEmeraldGreen, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 18);
            var btnStartEssay = CreateButton(canvas, "Btn_StartEssay", "📝  Làm Bài Tự Luận Này", new Vector2(190, 32), new Vector2(350, 56), ColorPurpleViolet, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 18);

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

            // Top Status Bar
            var backBtn = CreateTopLeftButton(canvas, "Btn_BackToMenu", "← Menu", new Vector2(120, 42), ColorSlateSecondary, 35, 20);
            var topicTitle = CreateText(canvas, "TopicTitleText", "📘 Chủ đề: Số nguyên (DỄ)", 22, new Vector2(175, -20), new Vector2(360, 42), ColorAmberGold, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), FontStyles.Bold);
            
            // Progress & Timer Badges
            var progress = CreateTopCenterText(canvas, "ProgressText", "CÂU 01 / 10", 24, new Vector2(220, 42), ColorTextTitle, 20);
            var timer = CreateText(canvas, "TimerText", "⏱ 00:30", 24, new Vector2(-195, -20), new Vector2(140, 42), ColorAmberGold, TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), FontStyles.Bold);
            var closeBtn = CreateTopRightButton(canvas, "CloseQuizButton", "Nộp sớm ✕", new Vector2(130, 42), ColorCoralRed, 35, 20);

            // Elevated Question Card
            var card = CreateCard(canvas, "QuestionCard", new Vector2(0, -90), new Vector2(1150, 200), null, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            CreateBadge(card, "QTypeBadge", "CÂU HỎI TRẮC NGHIỆM", new Vector2(0, 75), new Vector2(220, 28), ColorPrimaryBlue, 12);
            var prompt = CreateText(card, "PromptText", "Tính giá trị của biểu thức: (-15) + 7", 28, new Vector2(0, -10), new Vector2(1050, 140), ColorTextTitle, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);

            // 4 Modern Answer Option Buttons
            var btnA = CreateButton(canvas, "Btn_OptA", "A.   8", new Vector2(-290, 45), new Vector2(540, 92), ColorSlateSecondary, null, null, null, 22);
            var btnB = CreateButton(canvas, "Btn_OptB", "B.   -8", new Vector2(290, 45), new Vector2(540, 92), ColorSlateSecondary, null, null, null, 22);
            var btnC = CreateButton(canvas, "Btn_OptC", "C.   22", new Vector2(-290, -70), new Vector2(540, 92), ColorSlateSecondary, null, null, null, 22);
            var btnD = CreateButton(canvas, "Btn_OptD", "D.   -22", new Vector2(290, -70), new Vector2(540, 92), ColorSlateSecondary, null, null, null, 22);

            var feedback = CreateText(canvas, "FeedbackText", "", 24, new Vector2(0, 115), new Vector2(1000, 50), ColorEmeraldGreen, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), FontStyles.Bold);
            var nextBtn = CreateBottomCenterButton(canvas, "NextButton", "CÂU TIẾP THEO  →", new Vector2(340, 62), ColorEmeraldGreen, 35, 20);
            nextBtn.gameObject.SetActive(false);

            var manager = new GameObject("_QuizManager").AddComponent<QuizUIController>();
            SetField(manager, "topicTitleText", topicTitle);
            SetField(manager, "progressText", progress);
            SetField(manager, "timerText", timer);
            SetField(manager, "promptText", prompt);
            SetField(manager, "feedbackText", feedback);
            SetField(manager, "nextButton", nextBtn);
            SetField(manager, "closeQuizButton", closeBtn);
            SetField(manager, "backToMenuButton", backBtn);

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

            var backBtn = CreateTopLeftButton(canvas, "Btn_Back", "← Menu", new Vector2(130, 44), ColorSlateSecondary, 35, 20);
            var title = CreateTopCenterText(canvas, "ProblemTitleText", "BÀI TẬP TỰ LUẬN: THỨ TỰ THỰC HIỆN PHÉP TÍNH", 30, new Vector2(900, 48), ColorAmberGold, 18);

            // Problem Card
            var card = CreateCard(canvas, "ProblemCard", new Vector2(0, -90), new Vector2(1180, 135), null, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            CreateBadge(card, "PBadge", "ĐỀ BÀI TỰ LUẬN", new Vector2(0, 48), new Vector2(180, 26), ColorPurpleViolet, 12);
            var prompt = CreateText(card, "ProblemPromptText", "Thực hiện phép tính theo từng bước giải: A = 15 - (3 + 2) × 2", 24, new Vector2(0, -10), new Vector2(1100, 75), ColorTextTitle, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);

            // 3 Action Buttons
            var btnCapture = CreateButton(canvas, "Btn_CapturePhoto", "📸  1. Chụp Ảnh Bài Làm", new Vector2(-330, -250), new Vector2(310, 52), ColorPrimaryBlue, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 18);
            var btnPick = CreateButton(canvas, "Btn_PickImage", "📁  2. Chọn Ảnh Từ Máy", new Vector2(0, -250), new Vector2(310, 52), ColorPurpleViolet, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 18);
            var btnSample = CreateButton(canvas, "Btn_SamplePhoto", "📄  3. Vở Mẫu Toán 6", new Vector2(330, -250), new Vector2(310, 52), ColorEmeraldGreen, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 18);

            // Photo Preview Frame
            var prevContainer = CreateCard(canvas, "PreviewContainer", new Vector2(0, -35), new Vector2(500, 290), new Color(0.05f, 0.07f, 0.12f));
            var preview = new GameObject("ImagePreview", typeof(RectTransform), typeof(RawImage));
            preview.transform.SetParent(prevContainer.transform, false);
            SetFullStretch(preview.GetComponent<RectTransform>());

            var photoStatus = CreateText(canvas, "PhotoStatusText", "Hãy chụp ảnh bài làm, chọn ảnh từ máy hoặc bấm xem vở mẫu để AI chấm", 19, new Vector2(0, 115), new Vector2(950, 40), ColorAmberGold, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            var submitBtn = CreateBottomCenterButton(canvas, "SubmitButton", "GỬI BÀI GIẢI ĐỂ GIA SƯ AI CHẤM NGAY  🤖", new Vector2(460, 62), ColorEmeraldGreen, 35, 19);

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

            // Victory Glass Card
            var card = CreateCard(canvas, "ResultCard", Vector2.zero, new Vector2(760, 720));

            // Stars
            CreateText(card, "StarsText", "⭐ ⭐ ⭐", 48, new Vector2(0, 280), new Vector2(400, 60), ColorAmberGold);
            var scoreBig = CreateText(card, "ScoreBigText", "8.5 / 10", 72, new Vector2(0, 205), new Vector2(450, 95), ColorAmberGold, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);
            CreateBadge(card, "ScorePill", "🎉 HOÀN THÀNH XUẤT SẮC!", new Vector2(0, 140), new Vector2(280, 36), ColorEmeraldGreen, 15);

            var scoreDetail = CreateText(card, "ScoreDetailText", "Số câu trả lời đúng: 8 / 10 câu", 22, new Vector2(0, 85), new Vector2(500, 35), ColorTextTitle);
            var timeSpent = CreateText(card, "TimeSpentText", "⏱ Thời gian hoàn thành: 00:45", 20, new Vector2(0, 45), new Vector2(500, 35), ColorTextMuted);

            // Versus Banner (Conditional)
            var vsBanner = CreateCard(card, "VersusBannerPanel", new Vector2(0, -10), new Vector2(620, 95), new Color(0.12f, 0.18f, 0.30f));
            vsBanner.SetActive(false);

            var vsResult = CreateText(vsBanner, "VersusResultText", "🏆 CHIẾN THẮNG (WINNER)!", 28, new Vector2(0, 18), new Vector2(550, 42), ColorEmeraldGreen, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);
            var vsCompare = CreateText(vsBanner, "VersusCompareText", "Bạn: 80đ   VS   BảoToánHọc: 60đ", 18, new Vector2(0, -22), new Vector2(550, 32), ColorTextTitle);

            // Action Buttons
            var btnAI = CreateButton(card, "Btn_AIFeedback", "🤖  XEM GỢI Ý & SỬA CÂU SAI BẰNG AI  >", new Vector2(0, -125), new Vector2(540, 62), ColorPurpleViolet, null, null, null, 19);
            var btnRetry = CreateButton(card, "Btn_Retry", "🔄  Làm Lại Đề Này", new Vector2(0, -200), new Vector2(540, 54), ColorEmeraldGreen, null, null, null, 18);
            var btnMenu = CreateButton(card, "Btn_BackToMenu", "🏠  Về Menu Chính", new Vector2(0, -265), new Vector2(540, 50), ColorSlateSecondary, null, null, null, 17);

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

            var backScore = CreateTopLeftButton(canvas, "Btn_BackToScore", "← Bảng Điểm", new Vector2(150, 44), ColorSlateSecondary, 35, 20);
            var backMenu = CreateTopRightButton(canvas, "Btn_BackToMenu", "Về Menu →", new Vector2(140, 44), ColorSlateSecondary, 35, 20);

            // Tutor Badge & Greeting
            CreateBadge(canvas, "TutorBadge", "🤖 GIA SƯ AI GEMINI • THẦY MINH", new Vector2(0, -18), new Vector2(350, 36), ColorPurpleViolet, 14, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            var greeting = CreateText(canvas, "TutorGreetingText", "Chào em! Thầy ở đây để cùng em phân tích chi tiết lỗi sai và cách khắc phục nhé:", 22, new Vector2(0, -60), new Vector2(1000, 35), ColorTextTitle, TextAlignmentOptions.Center, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            // Dialogue Card
            var card = CreateCard(canvas, "DialogueCard", new Vector2(0, -110), new Vector2(1150, 480), null, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

            var qTitle = CreateText(card, "QuestionTitleText", "Câu hỏi: Tính (-15) + 7", 26, new Vector2(0, 185), new Vector2(1050, 50), ColorAmberGold, TextAlignmentOptions.MidlineLeft, null, null, null, FontStyles.Bold);
            
            // 3 Structured Sub-Cards
            var boxDiag = CreateCard(card, "Box_Diag", new Vector2(0, 100), new Vector2(1060, 95), new Color(0.25f, 0.12f, 0.15f, 0.85f));
            var diag = CreateText(boxDiag, "DiagnosisBodyText", "❌ Nguyên nhân sai: Em đã quên đặt dấu trừ của số có giá trị tuyệt đối lớn hơn.", 20, Vector2.zero, new Vector2(1020, 80), new Color(1f, 0.8f, 0.8f), TextAlignmentOptions.TopLeft);

            var boxRule = CreateCard(card, "Box_Rule", new Vector2(0, -10), new Vector2(1060, 95), new Color(0.10f, 0.22f, 0.18f, 0.85f));
            var rule = CreateText(boxRule, "RuleReminderText", "💡 Quy tắc cốt lõi: Muốn cộng hai số nguyên khác dấu, ta lấy số lớn trừ số bé và đặt dấu của số lớn hơn trước kết quả.", 20, Vector2.zero, new Vector2(1020, 80), new Color(0.8f, 1f, 0.85f), TextAlignmentOptions.TopLeft);

            var boxAnalog = CreateCard(card, "Box_Analog", new Vector2(0, -120), new Vector2(1060, 95), new Color(0.25f, 0.20f, 0.10f, 0.85f));
            var analog = CreateText(boxAnalog, "AnalogousProblemText", "🎯 Bài tập rèn luyện tương tự: Hãy tính giá trị biểu thức: (-20) + 12 = ?", 21, Vector2.zero, new Vector2(1020, 80), new Color(1f, 0.95f, 0.7f), TextAlignmentOptions.TopLeft, null, null, null, FontStyles.Bold);

            // Pagination
            var btnPrev = CreateButton(canvas, "Btn_Prev", "← Câu Trước", new Vector2(-250, 40), new Vector2(210, 54), ColorSlateSecondary, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 18);
            var page = CreateText(canvas, "PageIndicatorText", "CÂU 1 / 3", 22, new Vector2(0, 40), new Vector2(160, 45), ColorTextTitle, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), FontStyles.Bold);
            var btnNext = CreateButton(canvas, "Btn_Next", "Câu Tiếp Theo →", new Vector2(250, 40), new Vector2(210, 54), ColorPrimaryBlue, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 18);

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

            var backBtn = CreateTopLeftButton(canvas, "Btn_Back", "← Menu", new Vector2(130, 44), ColorSlateSecondary, 35, 20);
            CreateTopCenterText(canvas, "Title", "ĐẤU TRƯỜNG ĐỐI KHÁNG TOÁN 6", 32, new Vector2(750, 48), ColorAmberGold, 18);
            CreateBadge(canvas, "TimeBadge", "⏱ THỜI GIAN: 3 PHÚT (180s)", new Vector2(-35, -20), new Vector2(230, 36), ColorPrimaryBlue, 13, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));

            // Arena Card
            var card = CreateCard(canvas, "ScanningCard", new Vector2(0, 40), new Vector2(850, 360));

            // Duel Cards (Player vs Opponent)
            var pCard = CreateCard(card, "PlayerCard", new Vector2(-220, 35), new Vector2(280, 180), new Color(0.10f, 0.18f, 0.32f));
            CreateBadge(pCard, "PBadge", "BẠN", new Vector2(0, 60), new Vector2(90, 26), ColorPrimaryBlue, 12);
            CreateText(pCard, "PName", "AnThầnĐồng", 22, new Vector2(0, 15), new Vector2(250, 30), ColorTextTitle, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);
            CreateText(pCard, "PRank", "Lv. 3 • Hạng Bạc", 16, new Vector2(0, -25), new Vector2(250, 25), ColorTextMuted);

            // VS Emblem
            var vsEmblem = CreateCard(card, "VSEmblem", new Vector2(0, 35), new Vector2(80, 80), new Color(0.85f, 0.40f, 0.10f));
            CreateText(vsEmblem, "VSText", "VS", 36, Vector2.zero, new Vector2(80, 80), Color.white, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);

            // Opponent Card
            var oCard = CreateCard(card, "OpponentCard", new Vector2(220, 35), new Vector2(280, 180), new Color(0.25f, 0.12f, 0.15f));
            CreateBadge(oCard, "OBadge", "ĐỐI THỦ", new Vector2(0, 60), new Vector2(90, 26), ColorCoralRed, 12);
            var scanningText = CreateText(oCard, "ScanningIndicatorText", "🔍 Đang tìm đối thủ...", 19, new Vector2(0, 0), new Vector2(260, 60), ColorAmberGold);

            var matchBtn = CreateButton(card, "Btn_RandomMatch", "⚔️  BẮT ĐẦU GHÉP CẶP NGẪU NHIÊN  >", new Vector2(0, -115), new Vector2(480, 64), new Color(0.88f, 0.42f, 0.12f), null, null, null, 20);

            var status = CreateText(canvas, "StatusText", "Hệ thống sẽ ghép cặp học sinh có cùng trình độ kỹ năng", 18, new Vector2(0, 45), new Vector2(800, 35), ColorTextMuted, TextAlignmentOptions.Center, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

            // Match Found Modal
            var matchModal = new GameObject("MatchModal", typeof(RectTransform), typeof(Image));
            matchModal.transform.SetParent(canvas.transform, false);
            SetFullStretch(matchModal.GetComponent<RectTransform>());
            matchModal.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
            matchModal.SetActive(false);

            var mCard = CreateCard(matchModal, "MCard", Vector2.zero, new Vector2(620, 430));
            CreateBadge(mCard, "FoundBadge", "ĐÃ TÌM THẤY TRẬN ĐẤU!", new Vector2(0, 155), new Vector2(260, 36), ColorEmeraldGreen, 14);
            var foundText = CreateText(mCard, "MatchFoundText", "Đối thủ: BảoToánHọc (Lớp 6A2)\nTrình độ: Cao cấp", 24, new Vector2(0, 75), new Vector2(520, 80), ColorTextTitle, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);
            var countdown = CreateText(mCard, "CountdownAcceptText", "⏱ Tự động hủy sau: 10s", 19, new Vector2(0, 10), new Vector2(500, 35), ColorAmberGold);
            var btnAccept = CreateButton(mCard, "Btn_Accept", "✓   CHẤP NHẬN THI ĐẤU (ACCEPT)", new Vector2(0, -60), new Vector2(440, 58), ColorEmeraldGreen, null, null, null, 19);
            var btnDecline = CreateButton(mCard, "Btn_Decline", "✕   TỪ CHỐI", new Vector2(0, -135), new Vector2(440, 50), ColorCoralRed, null, null, null, 17);

            // Mode Selection Modal
            var modeModal = new GameObject("ModeSelectionModal", typeof(RectTransform), typeof(Image));
            modeModal.transform.SetParent(canvas.transform, false);
            SetFullStretch(modeModal.GetComponent<RectTransform>());
            modeModal.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
            modeModal.SetActive(false);

            var modeCard = CreateCard(modeModal, "ModeCard", Vector2.zero, new Vector2(620, 380));
            CreateBadge(modeCard, "ModeBadge", "THỂ THỨC THI ĐẤU", new Vector2(0, 135), new Vector2(200, 34), ColorPrimaryBlue, 14);
            CreateText(modeCard, "ModeTitle", "CHỌN THỂ THỨC ĐỐI KHÁNG (3 PHÚT)", 26, new Vector2(0, 75), new Vector2(560, 45), ColorAmberGold, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);
            var btnMcqMode = CreateButton(modeCard, "Btn_ChooseMCQ", "🎯  Đấu Trắc Nghiệm Tốc Độ (10 Câu)", new Vector2(0, -5), new Vector2(450, 60), ColorPrimaryBlue, null, null, null, 19);
            var btnEssayMode = CreateButton(modeCard, "Btn_ChooseEssay", "📝  Đấu Tự Luận Nhanh (1 Bài Chấm AI)", new Vector2(0, -85), new Vector2(450, 60), ColorPurpleViolet, null, null, null, 19);

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

            var backBtn = CreateTopLeftButton(canvas, "Btn_Back", "← Menu", new Vector2(130, 44), ColorSlateSecondary, 35, 20);
            CreateTopCenterText(canvas, "Title", "NHẬT KÝ LUYỆN TẬP TOÁN 6", 32, new Vector2(600, 48), ColorAmberGold, 18);
            var toggleBtn = CreateTopRightButton(canvas, "Btn_ToggleMode", "Xem Chi Tiết Theo Bài  >", new Vector2(250, 44), ColorPurpleViolet, 35, 20, 16);
            var toggleText = toggleBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Mode 1: Summary Dashboard
            var sumPanel = CreateCard(canvas, "Mode1_SummaryPanel", new Vector2(0, -25), new Vector2(920, 620));
            CreateBadge(sumPanel, "SumBadge", "TỔNG QUAN TIẾN ĐỘ", new Vector2(0, 245), new Vector2(220, 34), ColorPrimaryBlue, 14);
            CreateText(sumPanel, "SumHeader", "TỔNG HỢP THÀNH TÍCH HỌC TẬP", 28, new Vector2(0, 190), new Vector2(650, 45), ColorAmberGold, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);

            // 4 Modern Metrics Badges
            var cardL = CreateCard(sumPanel, "MetricCard1", new Vector2(0, 105), new Vector2(750, 62), new Color(0.12f, 0.17f, 0.28f));
            var totalLess = CreateText(cardL, "TotalLessonsCompletedText", "📘   Số bài học đã hoàn thành:  3 / 3 Bài", 22, Vector2.zero, new Vector2(700, 40), ColorTextTitle);

            var cardS = CreateCard(sumPanel, "MetricCard2", new Vector2(0, 25), new Vector2(750, 62), new Color(0.12f, 0.17f, 0.28f));
            var avgScore = CreateText(cardS, "AverageScoreText", "🎯   Điểm trung bình toàn khóa:  8.8 / 10", 22, Vector2.zero, new Vector2(700, 40), ColorEmeraldGreen, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);

            var cardA = CreateCard(sumPanel, "MetricCard3", new Vector2(0, -55), new Vector2(750, 62), new Color(0.12f, 0.17f, 0.28f));
            var totalAtt = CreateText(cardA, "TotalAttemptsText", "🔄   Tổng số lượt rèn luyện:  19 lần", 22, Vector2.zero, new Vector2(700, 40), ColorTextTitle);

            var cardT = CreateCard(sumPanel, "MetricCard4", new Vector2(0, -135), new Vector2(750, 62), new Color(0.12f, 0.17f, 0.28f));
            var totalTime = CreateText(cardT, "TotalTimeText", "⏱   Tổng thời lượng học tập:  1 giờ 45 phút", 22, Vector2.zero, new Vector2(700, 40), ColorTextTitle);

            // Mode 2: Detail
            var detPanel = CreateCard(canvas, "Mode2_DetailPanel", new Vector2(0, -25), new Vector2(920, 620));
            detPanel.SetActive(false);

            CreateBadge(detPanel, "DetBadge", "BÁO CÁO CHI TIẾT", new Vector2(0, 245), new Vector2(220, 34), ColorPurpleViolet, 14);
            CreateText(detPanel, "DetHeader", "CHI TIẾT THEO TỪNG BÀI HỌC", 28, new Vector2(0, 190), new Vector2(650, 45), ColorAmberGold, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);

            var prevLessonBtn = CreateButton(detPanel, "Btn_PrevLesson", "‹", new Vector2(-320, 110), new Vector2(50, 52), ColorPrimaryBlue, null, null, null, 24);
            var drop = CreateTMPDropdown(detPanel, "LessonDropdown", new Vector2(0, 110), new Vector2(550, 52));
            var nextLessonBtn = CreateButton(detPanel, "Btn_NextLesson", "›", new Vector2(320, 110), new Vector2(50, 52), ColorPrimaryBlue, null, null, null, 24);

            var selTitle = CreateText(detPanel, "SelectedLessonTitleText", "Bài 1: Phép cộng và trừ số nguyên", 24, new Vector2(0, 35), new Vector2(700, 40), ColorAmberGold, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);
            var attCount = CreateText(detPanel, "AttemptsCountText", "Số lần rèn luyện: 8 lần", 21, new Vector2(0, -20), new Vector2(700, 35), ColorTextTitle);
            var hiScore = CreateText(detPanel, "HighestScoreText", "Điểm cao nhất: 10 / 10", 22, new Vector2(0, -70), new Vector2(700, 35), ColorEmeraldGreen, TextAlignmentOptions.Center, null, null, null, FontStyles.Bold);
            var lastScore = CreateText(detPanel, "LastScoreText", "Điểm lần gần nhất: 9 / 10", 21, new Vector2(0, -120), new Vector2(700, 35), ColorTextTitle);
            var lastDate = CreateText(detPanel, "LastDateText", "Lần học gần nhất: Hôm qua lúc 19:30", 18, new Vector2(0, -170), new Vector2(700, 35), ColorTextMuted);

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

            var backBtn = CreateTopLeftButton(canvas, "Btn_Back", "← Menu", new Vector2(130, 44), ColorSlateSecondary, 35, 20);
            CreateTopCenterText(canvas, "Title", "CÀI ĐẶT HỆ THỐNG", 32, new Vector2(600, 48), ColorAmberGold, 18);

            var card = CreateCard(canvas, "SettingsCard", new Vector2(0, -25), new Vector2(880, 760));

            // Section 1: Audio
            CreateBadge(card, "AudioBadge", "🔊 ÂM THANH & HIỆU ỨNG", new Vector2(-220, 305), new Vector2(250, 32), ColorPrimaryBlue, 13);
            
            CreateText(card, "LblMaster", "Âm lượng tổng:", 20, new Vector2(-220, 245), new Vector2(250, 35), ColorTextTitle, TextAlignmentOptions.MidlineLeft);
            var slMaster = CreateSlider(card, "MasterSlider", new Vector2(70, 245), new Vector2(320, 22));
            var txtMasterVal = CreateText(card, "MasterValText", "100%", 18, new Vector2(295, 245), new Vector2(80, 35), ColorAmberGold);

            CreateText(card, "LblBgm", "Nhạc nền (BGM):", 20, new Vector2(-220, 190), new Vector2(250, 35), ColorTextTitle, TextAlignmentOptions.MidlineLeft);
            var slBgm = CreateSlider(card, "BgmSlider", new Vector2(70, 190), new Vector2(320, 22));
            var txtBgmVal = CreateText(card, "BgmValText", "80%", 18, new Vector2(295, 190), new Vector2(80, 35), ColorAmberGold);

            CreateText(card, "LblSfx", "Hiệu ứng (SFX):", 20, new Vector2(-220, 135), new Vector2(250, 35), ColorTextTitle, TextAlignmentOptions.MidlineLeft);
            var slSfx = CreateSlider(card, "SfxSlider", new Vector2(70, 135), new Vector2(320, 22));
            var txtSfxVal = CreateText(card, "SfxValText", "100%", 18, new Vector2(295, 135), new Vector2(80, 35), ColorAmberGold);

            // Section 2: Display
            CreateBadge(card, "DispBadge", "🖥️ HIỂN THỊ & GIAO DIỆN", new Vector2(-220, 50), new Vector2(250, 32), ColorPurpleViolet, 13);
            
            CreateText(card, "LblBright", "Độ sáng màn hình:", 20, new Vector2(-220, -5), new Vector2(250, 35), ColorTextTitle, TextAlignmentOptions.MidlineLeft);
            var slBright = CreateSlider(card, "BrightSlider", new Vector2(70, -5), new Vector2(320, 22));
            var txtBrightVal = CreateText(card, "BrightValText", "100%", 18, new Vector2(295, -5), new Vector2(80, 35), ColorAmberGold);

            var darkToggle = CreateToggle(card, "DarkModeToggle", "Bật giao diện tối dịu mắt (Dark Mode)", new Vector2(-220, -65), new Vector2(500, 38));
            var fullToggle = CreateToggle(card, "FullScreenToggle", "Chế độ toàn màn hình (Full Screen)", new Vector2(-220, -115), new Vector2(500, 38));

            // Section 3: Data & Account
            CreateBadge(card, "AccBadge", "👤 DỮ LIỆU & TÀI KHOẢN", new Vector2(-220, -185), new Vector2(250, 32), ColorEmeraldGreen, 13);
            var btnReset = CreateButton(card, "Btn_ResetProgress", "Làm mới dữ liệu luyện tập", new Vector2(-120, -245), new Vector2(300, 48), ColorCoralRed, null, null, null, 17);
            var btnSave = CreateButton(card, "Btn_SaveSettings", "LƯU CÀI ĐẶT  ✓", new Vector2(200, -245), new Vector2(260, 48), ColorEmeraldGreen, null, null, null, 18);

            var status = CreateText(card, "SettingsStatusText", "", 17, new Vector2(0, -305), new Vector2(600, 35), ColorAmberGold);

            var manager = new GameObject("_SettingsManager").AddComponent<SettingsUIController>();
            SetField(manager, "backToMenuButton", backBtn);
            SetField(manager, "masterVolumeSlider", slMaster);
            SetField(manager, "masterVolumeValueText", txtMasterVal);
            SetField(manager, "bgmVolumeSlider", slBgm);
            SetField(manager, "bgmVolumeValueText", txtBgmVal);
            SetField(manager, "sfxVolumeSlider", slSfx);
            SetField(manager, "sfxVolumeValueText", txtSfxVal);
            SetField(manager, "brightnessSlider", slBright);
            SetField(manager, "brightnessValueText", txtBrightVal);
            SetField(manager, "darkModeToggle", darkToggle);
            SetField(manager, "fullScreenToggle", fullToggle);
            SetField(manager, "resetProgressButton", btnReset);
            SetField(manager, "saveSettingsButton", btnSave);
            SetField(manager, "statusText", status);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), $"{SCENES_DIR}/11_Settings.unity");
        }
        #endregion

        #region Build Settings Auto-Register
        [MenuItem("Math6 / 📋 ĐỒNG BỘ TẤT CẢ SCENE VÀO BUILD SETTINGS", priority = 2)]
        public static void RegisterScenesInBuildSettings()
        {
            var orderedScenes = new List<string>
            {
                "00_Bootstrapper",
                "01_Login",
                "02_MainMenu",
                "02_VillageWorld",
                "03_LessonList",
                "04_LessonContent",
                "05_MCQPractice",
                "06_EssayPractice",
                "07_Score",
                "08_AIFeedback",
                "09_VersusLobby",
                "10_PracticeLog",
                "11_Settings"
            };

            var editorBuildScenes = new List<EditorBuildSettingsScene>();
            var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in orderedScenes)
            {
                string path = $"{SCENES_DIR}/{name}.unity";
                if (File.Exists(path) && !addedPaths.Contains(path))
                {
                    editorBuildScenes.Add(new EditorBuildSettingsScene(path, true));
                    addedPaths.Add(path);
                }
            }

            // Quét bất kỳ scene nào khác trong Assets/Scenes/ nếu chưa có
            if (Directory.Exists(SCENES_DIR))
            {
                foreach (var file in Directory.GetFiles(SCENES_DIR, "*.unity"))
                {
                    string norm = file.Replace('\\', '/');
                    if (!addedPaths.Contains(norm))
                    {
                        editorBuildScenes.Add(new EditorBuildSettingsScene(norm, true));
                        addedPaths.Add(norm);
                    }
                }
            }

            // Thêm các Scene 3D gameplay nếu có
            string forestPath = "Assets/VertexRageStudio/LowPolyNature/Scenes/ForestPath.unity";
            if (File.Exists(forestPath) && !addedPaths.Contains(forestPath))
            {
                editorBuildScenes.Add(new EditorBuildSettingsScene(forestPath, true));
                addedPaths.Add(forestPath);
            }

            EditorBuildSettings.scenes = editorBuildScenes.ToArray();
            Debug.Log($"[Math6 SceneAutoBuilder] 📋 Đã tự động cập nhật {EditorBuildSettings.scenes.Length} Scene vào Build Settings!");
        }
        #endregion
    }
}
#endif
