#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Math6Companion.Core;

namespace Math6Companion.Editor
{
    public class BackendConfigEditorWindow : EditorWindow
    {
        private string currentUrl;

        [MenuItem("Math6/⚙️ Cấu Hình Backend Google Sheets", false, 10)]
        public static void ShowWindow()
        {
            var win = GetWindow<BackendConfigEditorWindow>("Cấu Hình Backend");
            win.minSize = new Vector2(560, 240);
            win.Show();
        }

        private void OnEnable()
        {
            currentUrl = AppConfig.GetActiveBackendUrl();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(12);
            GUILayout.Label("CẤU HÌNH BACKEND GOOGLE APPS SCRIPT", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Dán đường link Web App mới từ Google Apps Script vào ô bên dưới.\nTất cả các Scene và Module sẽ tự động chuyển sang sử dụng link này ngay lập tức!", MessageType.Info);
            
            EditorGUILayout.Space(10);
            GUILayout.Label("Google Apps Script Web App URL (/exec):", EditorStyles.label);
            currentUrl = EditorGUILayout.TextField(currentUrl, GUILayout.Height(28));

            EditorGUILayout.Space(12);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("💾 LƯU & ÁP DỤNG NGAY", GUILayout.Height(36)))
            {
                if (!string.IsNullOrEmpty(currentUrl) && currentUrl.StartsWith("https://script.google.com/macros/s/"))
                {
                    AppConfig.SetCustomBackendUrl(currentUrl);
                    Math6Companion.Core.Services.GoogleSheetsNetworkService.InvalidateCache();
                    EditorUtility.DisplayDialog("Thành Công", "Đã lưu và áp dụng link Backend mới cho toàn bộ Game!\n\n" + currentUrl, "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Cảnh Báo", "Link Web App phải có dạng:\nhttps://script.google.com/macros/s/.../exec", "Đồng ý");
                }
            }

            if (GUILayout.Button("🔄 Reset Về Link Code", GUILayout.Height(36), GUILayout.Width(160)))
            {
                AppConfig.ResetToDefaultUrl();
                currentUrl = AppConfig.APPS_SCRIPT_URL;
                Math6Companion.Core.Services.GoogleSheetsNetworkService.InvalidateCache();
                EditorUtility.DisplayDialog("Đã Reset", "Đã khôi phục về link khai báo trong AppConfig.cs:\n\n" + currentUrl, "OK");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("File code gốc:", "Assets/_Project/Scripts/Core/AppConfig.cs", EditorStyles.miniLabel);
        }
    }
}
#endif
