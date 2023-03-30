using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EduXR.xAPI.Editor
{
    public class DesignerWindow: EditorWindow
    {
        private DefinitionsTreeView _definitionsTreeView;
        
        [MenuItem("EduXR / xAPI / Designer")]
        public static void ShowWindow()
        {
            GetWindow<DesignerWindow>("xAPI Designer").Show();
        }

        public void OnGUI()
        {
           /* CustomGUI.Horizontal.Wrap(() =>
            {
                CustomGUI.BeginVertical();
                CustomGUI.Title("Definitions");
                
                _definitionsTreeView.OnGUI(new Rect(0, 20, Math.Max(position.width / 2, 600), position.height));
                CustomGUI.EndVertical();
            
                CustomGUI.BeginVertical();
                CustomGUI.Label("TEST");
                CustomGUI.EndVertical();
            });*/
           
           // https://docs.unity3d.com/Manual/UIE-HowTo-CreateEditorWindow.html
           // Create a two-pane view with the left pane being fixed with
           var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);

            // Add the view to the visual tree by adding it as a child to the root element
           rootVisualElement.Add(splitView);

            // A TwoPaneSplitView always needs exactly two child elements
           var leftPane = new VisualElement();
           leftPane.Add(new IMGUIContainer(() =>
           {
               _definitionsTreeView.OnGUI(new Rect(0, 20, 250, position.height));
           }));
           
           
           splitView.Add(leftPane);
           
           
           
           var rightPane = new VisualElement();
           splitView.Add(rightPane);
        }
    }
}