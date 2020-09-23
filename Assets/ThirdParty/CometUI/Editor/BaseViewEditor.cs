using System;
using System.Linq;
using UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CometUI
{
    [CustomEditor(typeof(BaseView), true)]
    public class BaseViewEditor : Editor
    {
        Texture2D logo;

        void OnEnable()
        {
            logo = Resources.Load<Texture2D>("comet_icon_big");
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Space(30);
            if (GUI.Button(new Rect(120, 4, 70, 18), new GUIContent("Open", "Open or Create User Script")))
                OpenUserScript();

            if (GUI.Button(new Rect(190, 4, 70, 18), new GUIContent("Rebuild", "Grab UI elements and rebuild script")))
                RebuildAutoScript();

            GUI.DrawTexture(new Rect(0, 2, 100, 26), logo, ScaleMode.StretchToFill, true);

            DrawDefaultInspector();
        }

        private void OpenUserScript()
        {
            var node = FindNodeInGraph();

            if (node != null)
                UIGraphEditor.OpenOrCreateUserScript(node);
        }

        private void RebuildAutoScript()
        {
            var node = FindNodeInGraph();

            if (node != null)
                ScriptBuilder.CreateScript(node);
        }

        ViewNode FindNodeInGraph()
        {
            //get graph
            var obj = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(o => o.GetComponent<UIManager>() != null);
            if (obj == null)
            {
                EditorUtility.DisplayDialog("Error", "UIManager is not found on scene", "OK");
                return null;
            }
            var uiManager = obj.GetComponent<UIManager>();
            var graph = uiManager.UIGraph as UIGraph;
            if (graph == null)
            {
                EditorUtility.DisplayDialog("Error", "UIGraph is not assigned", "OK");
                return null;
            }

            //get my node
            var node = graph.nodes.FirstOrDefault(n => n.name == target.GetType().Name) as ViewNode;
            if (node == null)
            {
                EditorUtility.DisplayDialog("Error", "Node is not found in UIGraph", "OK");
                return null;
            }

            node.rt = (target as Component).GetComponent<RectTransform>();
            node.GrabInfoAboutView(node.rt);

            return node;
        }
    }
}