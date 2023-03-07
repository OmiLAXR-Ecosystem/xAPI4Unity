using UnityEditor;

namespace EduXR.xAPI.Editor
{
    public class xAPI_Designer_Window: EditorWindow
    {
        [MenuItem("EduXR Tools/xAPI/Fetch custom definitions")]
        public static void ShowWindow()
        {
            GetWindow<xAPI_Designer_Window>("xAPI Designer").Show();
        }
    }
}