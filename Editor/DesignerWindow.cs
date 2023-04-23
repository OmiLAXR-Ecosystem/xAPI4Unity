using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace xAPI4Unity.Editor
{
    public class DesignerWindow: EditorWindow
    {
        private static Settings Settings => Settings.instance;
        private readonly LocalSourceView _localSourceView = new LocalSourceView(Settings.instance);

        private string _selectedContext = "";
        
        [MenuItem("EduXR / xAPI / Designer")]
        public static void ShowWindow()
        {
            GetWindow<DesignerWindow>("xAPI Designer").Show();
        }

        public void OnGUI()
        {
            CustomGUI.Title("xAPI Definitions Designer");
            
            _localSourceView.Show();
            
            CustomGUI.Divider();
            CustomGUI.Space();

            var contexts = GetContexts();
            _selectedContext = CustomGUI.Popup("Context", contexts, _selectedContext);
        }

        private string[] GetContexts()
        {
            var dirs = Directory.GetDirectories(Path.Combine(Settings.localSource.path, "definitions"));
            return dirs.Select(dir => Path.GetFileName(dir)).ToArray();
        }
        private void SelectContext(string context) {}
    }
}