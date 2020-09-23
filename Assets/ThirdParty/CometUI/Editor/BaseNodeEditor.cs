using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace CometUI
{
    [NodeEditor.CustomNodeEditor(typeof(BaseNode))]
    public class BaseNodeEditor : NodeEditor
    {
        private static GUIStyle editorLabelStyle;
        private static GUIStyle editorButtonStyle;
        private static Texture2D buttonTexture;
        private static Texture2D buttonTexturePressed;
        Rect lastRect;

        public override void OnBodyGUI()
        {
            serializedObject.Update();
            if (buttonTexture == null) buttonTexture = Resources.Load<Texture2D>("btCometUI");
            if (buttonTexturePressed == null) buttonTexturePressed = Resources.Load<Texture2D>("btCometUI_pressed");
            if (editorLabelStyle == null) editorLabelStyle = new GUIStyle(EditorStyles.label);
            EditorStyles.label.normal.textColor = Color.white;
            EditorStyles.label.focused.textColor = Color.yellow;

            editorButtonStyle = CreateButtonStyle();

            const int buttonSize = 15;

            //
            foreach (XNode.NodePort port in target.DynamicPorts.OrderBy(p => p.ValueType.GetPortInfo().Order))
            {
                if (NodeEditorGUILayout.IsDynamicPortListPort(port))
                    continue;

                GUILayout.BeginHorizontal();
                editorButtonStyle.alignment = port.IsInput ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;

                DrawPort(port);

                GUILayout.EndHorizontal();
            }

            //
            editorButtonStyle.alignment = TextAnchor.MiddleCenter;
            editorButtonStyle.normal.textColor = editorButtonStyle.active.textColor = Color.white;

            GUILayout.BeginHorizontal();
            //GUILayout.FlexibleSpace();

            if (GUILayout.Button("+", editorButtonStyle, /*GUILayout.Width(buttonSize),*/ GUILayout.Height(buttonSize)))
            {
                ShowAddPortMenu();
            }
            GUILayout.EndHorizontal();

            //
            EditorStyles.label.normal = editorLabelStyle.normal;
            EditorStyles.label.focused = editorLabelStyle.focused;

            serializedObject.ApplyModifiedProperties();
        }

        private static GUIStyle CreateButtonStyle()
        {
            GUIStyle style = new GUIStyle();
            style.normal.background = buttonTexture;
            style.normal.textColor = Color.white;
            style.active.background = buttonTexturePressed;
            style.active.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            style.padding = new RectOffset(1, 1, 1, 1);
            style.fontSize = 11;

            return style;
        }

        private void SetBackGround(GUIStyleState normal, Texture2D buttonTexture)
        {
            normal.background = buttonTexture;
            for (int i = 0; i < normal.scaledBackgrounds.Length; i++)
                normal.scaledBackgrounds[i] = buttonTexture;
        }

        private void DrawPort(NodePort port)
        {
            if (port == null) return;
            var options = new GUILayoutOption[] { GUILayout.MinWidth(20) };
            Vector2 position = Vector3.zero;

            editorButtonStyle.normal.textColor = editorButtonStyle.active.textColor = Color.white;
            var fieldName = ObjectNames.NicifyVariableName(port.fieldName);
            GUIContent content = new GUIContent(fieldName);

            // If property is an input, display a regular property field and put a port handle on the left side
            if (port.direction == XNode.NodePort.IO.Input)
            {
                // Display a label
                //EditorGUILayout.LabelField(content, options);
                if (GUILayout.Button(content, editorButtonStyle, options))
                {
                    ShowNodeMenu(port);
                }

                Rect rect = GUILayoutUtility.GetLastRect();
                position = rect.position - new Vector2(16, 0);
            }
            // If property is an output, display a text label and put a port handle on the right side
            else if (port.direction == XNode.NodePort.IO.Output)
            {
                // Display a label
                //EditorGUILayout.LabelField(content, NodeEditorResources.OutputPort, options);
                if (GUILayout.Button(content, editorButtonStyle, options))
                {
                    ShowNodeMenu(port);
                }

                Rect rect = GUILayoutUtility.GetLastRect();
                position = rect.position + new Vector2(rect.width, 0);
            }
            NodeEditorGUILayout.PortField(position, port);
        }

        private void ShowNodeMenu(NodePort port)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete"), false, () => target.RemoveDynamicPort(port));

            ShowMenu(menu);
        }

        private void Rename(NodePort port)
        {
            NameWizard.CreateWizard(port.fieldName, (n) =>
            {
                target.RenameDynamicPort(port, n);
                var set = NodeEditorPreferences.GetSettings();
                if (set != null && set.autoSave) AssetDatabase.SaveAssets();
            });
        }

        void ShowAddPortMenu()
        {
            var menu = new GenericMenu();

            // Add all enum display names to menu
            var allowed = (target as BaseNode).GetAllowedAddPorts().OrderBy(p => p.Item1.GetPortInfo().Order);
            var prevOrder = -1;

            foreach (var info in allowed)
            {
                var port = info.Item1.GetPortInfo();
                if (target.DynamicPorts.Any(p => p.ValueType == info.Item1 && p.fieldName == info.Item2))
                    continue;

                if (prevOrder >= 0 && port.Order >= prevOrder + 100)
                    menu.AddSeparator("");

                prevOrder = info.Item1.GetPortInfo().Order;

                var portInfo = info;
                var menuName = info.Item2;
                menuName = ObjectNames.NicifyVariableName(menuName);
                if (!string.IsNullOrEmpty(port.SubMenu))
                    menuName = port.SubMenu + "/" + menuName;
                menu.AddItem(new GUIContent(menuName), false, () => CreatePort(info.Item1, info.Item2));
            }

            ShowMenu(menu);
        }

        protected void CreatePort(Type portType, string fieldName)
        {
            NodePort port;
            BasePort portInfo = portType.GetPortInfo();

            if (portInfo.Direction == Dir.Output)
                port = target.AddDynamicOutput(portType, fieldName: fieldName);
            else
                port = target.AddDynamicInput(portType, fieldName: fieldName);

            serializedObject.ApplyModifiedProperties();
        }

        private static void ShowMenu(GenericMenu menu)
        {
            if (menu.GetItemCount() == 0)
                return;

            // cache the original matrix(we assume this is scaled)
            Matrix4x4 m4 = GUI.matrix;
            //reset to non-scaled
            GUI.matrix = Matrix4x4.identity;
            menu.ShowAsContext();
            GUI.matrix = m4;
        }

        public override void OnHeaderGUI()
        {
            var rtNode = target as ViewNode;
            if (rtNode == null)
            {
                GUILayout.Label(target.name, NodeEditorResources.styles.nodeHeader, GUILayout.Height(21));
            }
            else
            {
                var sb = new StringBuilder();
                if (rtNode.rt != null)
                {
                    var view = rtNode.rt.GetComponent<BaseView>();
                    if (view != null)
                    {
                        if (view.ShowAtStart) sb.Append("S");
                        if (view.Concurrent) sb.Append("C");
                        if (view.BackPrority != BackPrority.IgnoreBack) sb.Append("B");
                        if (view.AutoCloseChildren) sb.Append("A");
                    }

                    //if (sb.Length > 0)
                    //{
                    //    sb.Insert(0, " (");
                    //    sb.Append(")");
                    //}
                    GUILayout.Label(target.name, NodeEditorResources.styles.nodeHeader, GUILayout.Height(21));
                    GUILayout.Label(sb.ToString(), NodeEditorResources.styles.small, GUILayout.Height(10));
                    //GUI.Label(new Rect(110, -3, 30, 20), sb.ToString(), NodeEditorResources.styles.small);
                }
                else
                {
                    GUILayout.Label(target.name + " (NOT FOUND)", NodeEditorResources.styles.tooltip, GUILayout.Height(30));
                }
            }
        }
    }
}