using System;
using UnityEditor;

namespace EduXR.xAPI.Editor
{
    public class DesignerWindow: EditorWindow
    {
        [MenuItem("EduXR / xAPI / Designer")]
        public static void ShowWindow()
        {
            GetWindow<DesignerWindow>("xAPI Designer").Show();
        }

        public void OnGUI()
        {
            
        }
    }
}