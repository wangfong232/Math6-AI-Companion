#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Math6Companion.Core;
using Math6Companion.UI;

namespace Math6Companion.Editor
{
    public class TeacherConfigEditorWindow : EditorWindow
    {
        private int selectedTab = 0;
        private string[] tabTitles = { "[1] Lộ Trình & Khối Lớp", "[2] Quản Lý Lớp Học", "[3] Gia Sư AI", "[4] Luật Chơi & Hệ Thống" };

        private int activeGrade = 6;
        private string activeClass = "6A1";
        private int pvpSeconds = 180;
        private int quizCount = 10;
        private string aiPedagogy = "SOCRATIC";
        private string aiModel = "models/gemini-3.5-flash-lite";

        [MenuItem("Math6/Quản Trị Giáo Viên & Mở Rộng Khối Lớp", false, 11)]
        public static void ShowWindow()
        {
            var win = GetWindow<TeacherConfigEditorWindow>("Quản Trị Giáo Viên");
            win.minSize = new Vector2(620, 480);
            win.Show();
        }

        private void OnEnable()
        {
            activeGrade = PlayerPrefs.GetInt("Teacher_ActiveGrade", 6);
            activeClass = PlayerPrefs.GetString("Teacher_ActiveClass", "6A1");
            pvpSeconds = PlayerPrefs.GetInt("Teacher_PvPDuration", 180);
            quizCount = PlayerPrefs.GetInt("Teacher_QuizQuestionCount", 10);
            aiPedagogy = PlayerPrefs.GetString("Teacher_AIPedagogyMode", "SOCRATIC");
            aiModel = PlayerPrefs.GetString("Teacher_AIGeminiModel", "models/gemini-3.5-flash-lite");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("BẢNG CẤU HÌNH DÀNH CHO GIÁO VIÊN (TEACHER PORTAL)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Tùy chỉnh toàn diện lộ trình môn học, mở rộng khối lớp (Lớp 6, 7, 8, 9), quản lý các lớp (6A1, 6A2...), tinh chỉnh Gia sư AI và cấu hình thi đấu đối kháng.", MessageType.Info);

            EditorGUILayout.Space(8);
            selectedTab = GUILayout.Toolbar(selectedTab, tabTitles, GUILayout.Height(30));
            EditorGUILayout.Space(12);

            switch (selectedTab)
            {
                case 0:
                    DrawCurriculumTab();
                    break;
                case 1:
                    DrawClassTab();
                    break;
                case 2:
                    DrawAITab();
                    break;
                case 3:
                    DrawSystemTab();
                    break;
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("LƯU CẤU HÌNH VÀO HỆ THỐNG", GUILayout.Height(38)))
            {
                SaveConfig();
                EditorUtility.DisplayDialog("Thành Công", "Đã lưu toàn bộ cấu hình giáo viên vào hệ thống!", "OK");
            }

            if (Application.isPlaying && GUILayout.Button("MỞ TEACHER PORTAL TRONG GAME", GUILayout.Height(38)))
            {
                TeacherPortalUIController.Open();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCurriculumTab()
        {
            GUILayout.Label("1. MỞ RỘNG & QUẢN LÝ KHỐI LỚP (GRADE EXPANSION)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            string[] grades = { "Khối Lớp 6 (Cốt lõi)", "Khối Lớp 7 (Số hữu tỉ, Đa thức, Tam giác bằng)", "Khối Lớp 8 (Hằng đẳng thức, Thalès, Pythagore)", "Khối Lớp 9 (Căn bậc 2, Hệ PT, Viète, Đường tròn)" };
            int currentGradeIndex = Mathf.Clamp(activeGrade - 6, 0, 3);
            int newGradeIndex = EditorGUILayout.Popup("Chọn Khối Lớp Hiện Hành:", currentGradeIndex, grades);
            activeGrade = newGradeIndex + 6;

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox($"Đang kích hoạt nội dung: Khối Lớp {activeGrade}.\nTất cả các bài học và câu hỏi sẽ tự động tải theo cấu hình của Khối Lớp {activeGrade}.", MessageType.None);
        }

        private void DrawClassTab()
        {
            GUILayout.Label("2. QUẢN LÝ LỚP HỌC (CLASS SELECTION)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            string[] classes = { "6A1", "6A2", "6A3", "7A1", "7A2", "8A1", "9A1" };
            int curClassIndex = System.Array.IndexOf(classes, activeClass);
            if (curClassIndex < 0) curClassIndex = 0;
            int newClassIndex = EditorGUILayout.Popup("Chọn Lớp Đang Quản Lý:", curClassIndex, classes);
            activeClass = classes[newClassIndex];

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox($"Lớp hiện tại: {activeClass}.\nGiáo viên có thể theo dõi tiến độ, giao bài tập về nhà và mở giải đấu đối kháng cho học sinh lớp này.", MessageType.None);
        }

        private void DrawAITab()
        {
            GUILayout.Label("3. TINH CHỈNH GIA SƯ AI THẦY MINH", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            string[] modes = { "SOCRATIC (Hỏi gợi mở từng bước)", "ENCOURAGING (Ân cần động viên)", "STRICT_RUBRIC (Bám sát barem chấm điểm)" };
            int curMode = aiPedagogy == "ENCOURAGING" ? 1 : (aiPedagogy == "STRICT_RUBRIC" ? 2 : 0);
            int newMode = EditorGUILayout.Popup("Phong Cách Sư Phạm AI:", curMode, modes);
            aiPedagogy = newMode == 1 ? "ENCOURAGING" : (newMode == 2 ? "STRICT_RUBRIC" : "SOCRATIC");

            EditorGUILayout.Space(4);
            string[] models = { "models/gemini-3.5-flash-lite", "models/gemini-3.8-flash", "models/gemini-2.5-flash" };
            int curModel = System.Array.IndexOf(models, aiModel);
            if (curModel < 0) curModel = 0;
            int newModel = EditorGUILayout.Popup("Mô Hình Gemini AI:", curModel, models);
            aiModel = models[newModel];
        }

        private void DrawSystemTab()
        {
            GUILayout.Label("4. CẤU HÌNH THỜI GIAN & LUẬT CHƠI", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            pvpSeconds = EditorGUILayout.IntSlider("Thời gian Đối Kháng PvP (giây):", pvpSeconds, 60, 300);
            quizCount = EditorGUILayout.IntSlider("Số câu mỗi lượt trắc nghiệm:", quizCount, 5, 20);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Backend Web App URL:", AppConfig.GetActiveBackendUrl(), EditorStyles.wordWrappedMiniLabel);
        }

        private void SaveConfig()
        {
            PlayerPrefs.SetInt("Teacher_ActiveGrade", activeGrade);
            PlayerPrefs.SetString("Teacher_ActiveClass", activeClass);
            PlayerPrefs.SetInt("Teacher_PvPDuration", pvpSeconds);
            PlayerPrefs.SetInt("Teacher_QuizQuestionCount", quizCount);
            PlayerPrefs.SetString("Teacher_AIPedagogyMode", aiPedagogy);
            PlayerPrefs.SetString("Teacher_AIGeminiModel", aiModel);
            PlayerPrefs.Save();

            if (TeacherConfigManager.Instance != null)
            {
                TeacherConfigManager.Instance.LoadConfiguration();
            }
        }
    }
}
#endif

