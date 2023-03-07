using UnityEditor;

namespace EduXR.xAPI.Editor
{
    public class xAPI_Designer_Window: EditorWindow
    {
        [MenuItem("EduXR Tools/xAPI/Designer")]
        public static void ShowWindow()
        {
            GetWindow<xAPI_Designer_Window>("xAPI Designer").Show();
        }
    }
}