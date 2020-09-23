using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UI;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XNode;
using XNodeEditor;

namespace CometUI
{
    [NodeGraphEditor.CustomNodeGraphEditor(typeof(UIGraph))]
    public class UIGraphEditor : NodeGraphEditor
    {
        public override void OnOpen()
        {
            base.OnCreate();
            base.window.titleContent.text = "Comet UI";
            if (target != null)
                base.window.titleContent.text += " - " + target.name;

            NodePort.CanBeConnectedTo = CanBeConnectedTo;

            NodeEditorWindow.NodeSelected -= OnNodeSelected;
            NodeEditorWindow.NodeSelected += OnNodeSelected;

            ViewNode.DoubleClicked -= OpenOrCreateUserScript;
            ViewNode.DoubleClicked += OpenOrCreateUserScript;

            ViewNode.RenameRequest -= RenameView;
            ViewNode.RenameRequest += RenameView;

            ViewNode.OnCreateUserScript -= OpenOrCreateUserScript;
            ViewNode.OnCreateUserScript += OpenOrCreateUserScript;

            ViewNode.OnShowAutoScript -= OpenAutoScript;
            ViewNode.OnShowAutoScript += OpenAutoScript;

            NodeEditorWindow.OnNodeClick -= NodeEditorWindow_OnNodeClick;
            NodeEditorWindow.OnNodeClick += NodeEditorWindow_OnNodeClick;

            NodeEditorWindow.OnNodeHeaderClick -= NodeEditorWindow_OnNodeHeaderClick;
            NodeEditorWindow.OnNodeHeaderClick += NodeEditorWindow_OnNodeHeaderClick;

            //Debug.Log("OnOpen " + DateTime.Now);
        }

        private bool CanBeConnectedTo(NodePort port1, NodePort port2)
        {
            return port1.ValueType.GetPortInfo().CanConnect(port1, port2) &&
                   port2.ValueType.GetPortInfo().CanConnect(port1, port2);
        }

        private void RenameView(ViewNode node)
        {
            //ask new name
            NameWizard.CreateWizard(node.name, (n) =>
            {
                RenameView(node, n);
                var set = NodeEditorPreferences.GetSettings();
                if (set != null && set.autoSave)
                    AssetDatabase.SaveAssets();
            });
        }

        private void RenameView(ViewNode node, string newName)
        {
            //check name
            if (!Regex.IsMatch(newName, "^[a-zA-Z0-9_]+$") || target.nodes.OfType<ViewNode>().Any(n => n.name == newName))
            {
                EditorUtility.DisplayDialog("Bad name", $"Name '{newName}' is not allowed!", "OK");
                return;
            }

            //check node with same name
            if (target.nodes.OfType<ViewNode>().Any(n => n.name == newName))
            {
                EditorUtility.DisplayDialog("Bad name", $"Node with name '{newName}' already exists!", "OK");
                return;
            }

            //check exists type
            var type = GetTypeByName(newName, node.rt.gameObject.scene);
            if (type != null)
            {
                EditorUtility.DisplayDialog("Bad name", $"Class with name '{newName}' already exists!", "OK");
                return;
            }

            //check if file exists
            if (Directory.EnumerateFiles(Application.dataPath, newName + ".cs", SearchOption.AllDirectories).Any())
            {
                EditorUtility.DisplayDialog("Bad name", $"File with name '{newName}.cs' already exists!", "OK");
                return;
            }

            //rename files and classes
            AssetDatabase.Refresh();

            foreach (var file in Directory.GetFiles(Application.dataPath, node.name + ".cs", SearchOption.AllDirectories))
            {
                //rename class
                var text = File.ReadAllText(file);
                //text = Regex.Replace(text, $@"\bclass\s+{node.name}\b", $"class {newName}");
                text = Regex.Replace(text, $@"\b{node.name}\b", $"{newName}");
                File.WriteAllText(file, text);

                //rename file
                var dir = Path.GetDirectoryName(file);
                var newPath = Path.Combine(dir, newName + ".cs");
                var res = AssetDatabase.RenameAsset(GetRelativePath(file), newName + ".cs");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            //rename RectTransform and node
            node.rt.name = newName;
            node.name = newName;

            //rebuild scripts
            ScriptBuilder.CreateScripts(target as UIGraph);
        }

        string GetRelativePath(string absolutePath)
        {
            var i = absolutePath.IndexOf("Assets");
            if (i >= 0)
            {
                return absolutePath.Substring(i);
            }

            return absolutePath;
        }

        DateTime lastNodeClickTime;

        private void NodeEditorWindow_OnNodeClick(Node node)
        {
            OnNodeclick(node, false);
        }

        private void NodeEditorWindow_OnNodeHeaderClick(Node node)
        {
            OnNodeclick(node, true);
        }

        private void OnNodeclick(Node node, bool header)
        {
            if (node is ViewNode vn)
            {
                if (header)
                if ((DateTime.Now - lastNodeClickTime).TotalSeconds < 0.3f)
                {
                    OpenOrCreateUserScript(vn);
                    lastNodeClickTime = DateTime.Now;
                    return;
                }

                if (vn.rt != null)
                {
                    EditorGUIUtility.PingObject(vn.rt);

                    var view = vn.rt.GetComponent<BaseView>();
                    var selected = new List<UnityEngine.Object>();
                    selected.Add(view == null ? (UnityEngine.Object)vn.rt : (UnityEngine.Object)view);
                    Selection.objects = selected.ToArray();

                    vn.GrabInfoAboutView(vn.rt);
                }
            }

            lastNodeClickTime = DateTime.Now;
        }

        private void OnNodeSelected(Node node)
        {
            if (node is ViewNode vn)
            {
                if (vn.rt != null)
                {
                    EditorGUIUtility.PingObject(vn.rt);
                    vn.GrabInfoAboutView(vn.rt);
                }
            }
        }

        public static void OpenOrCreateUserScript(ViewNode node)
        {
            var sceneName = ScriptBuilder.GetSceneName(node.rt.gameObject.scene);
            var scriptFolder = ScriptBuilder.GetScriptsFolder(node.rt.gameObject.scene);

            //find script 
            var fullPath = (string)null;
            if(Directory.Exists(scriptFolder))
                fullPath = Directory.GetFiles(scriptFolder, node.name + ".cs", SearchOption.AllDirectories)
                .Where(p=>!p.Contains("_autogenerated"))
                .FirstOrDefault();

            if (fullPath != null)
                fullPath = ScriptBuilder.GetFolderRelativeToRoot(fullPath);

            var script = fullPath != null ? AssetDatabase.LoadAssetAtPath(fullPath, typeof(MonoScript)) : null;

            if (script == null)
            {
                if (!EditorUtility.DisplayDialog("User Script " + node.name, $"Script {node.name}.cs does not exists.\r\nDo you want to create?", "Create", "Cancel"))
                    return;

                fullPath = Path.Combine(ScriptBuilder.GetScriptsFolder(node.rt.gameObject.scene),  node.name + ".cs");
                fullPath = ScriptBuilder.GetFolderRelativeToRoot(fullPath);
                ScriptBuilder.CreateScripts(node.graph as UIGraph);
                ScriptBuilder.CreateUserScript(node, false);
                script = AssetDatabase.LoadAssetAtPath(fullPath, typeof(MonoScript));
            }

            if (script != null)
                AssetDatabase.OpenAsset(script);
        }

        private void OpenAutoScript(ViewNode node)
        {
            //find script 
            var fullPath = Path.Combine(ScriptBuilder.GetAutogeneratedScriptFolder(node.rt.gameObject.scene), node.name + ".cs");
            fullPath = ScriptBuilder.GetFolderRelativeToRoot(fullPath);

            var script = AssetDatabase.LoadAssetAtPath(fullPath, typeof(MonoScript));
            if (script == null)
            {
                if (!EditorUtility.DisplayDialog("Script does not exists.\r\nDo you want to Build Scripts now?", "Build Scripts", "Cancel"))
                    return;
                OnBuildScripts();
                script = AssetDatabase.LoadAssetAtPath(fullPath, typeof(MonoScript));
            }

            if (script != null)
                AssetDatabase.OpenAsset(script);
        }

        //called when editor is opening or after scripts reloaded
        public override void OnCreate()
        {
            base.OnCreate();
            //Debug.Log("OnCreate " + DateTime.Now);

            SynchronizeGraphAndScene();
        }

        private void SynchronizeGraphAndScene()
        {
            var infos = new Dictionary<string, ViewInfo <BaseView>> ();
            foreach(var info in SceneInfoGrabber<BaseView>.GrabInfos(true))
                infos[info.Name] = info;

            Dictionary<string, RectTransform> rectTransforms = null;

            //is nodes w/o GameObject?
            foreach(var node in target.nodes.OfType<ViewNode>())
            {
                if (!infos.ContainsKey(node.name))
                {
                    //find object with the name
                    GetOrCreateRectTransformList(ref rectTransforms);

                    if (rectTransforms.TryGetValue(node.name, out var rt))
                    {
                        //found => add component
                        node.rt = rt;
                        var type = GetTypeByName(node.name, node.rt.gameObject.scene);
                        if (type != null)
                            rt.gameObject.AddComponent(type);
                        else
                            ScriptBuilder.CreateScript(node);
                    }
                    else
                    {
                        //node w/o GameObject
                        node.rt = null;
                    }
                }
                else
                {
                    //found GameObject
                    node.rt = infos[node.name].Main.RectTransform;
                }
            }
        }

        public static Type GetTypeByName(string nodeClassName, Scene scene)
        {
            return GetTypeByName(nodeClassName, scene.name + "_UI.");
        }

        public static Type GetTypeByName(string nodeClassName, string prefix)
        {
            var ass = typeof(GameObject).Assembly;

            var path = Application.dataPath + @"\..\Library\ScriptAssemblies\Assembly-CSharp.dll";
            if (File.Exists(path))
            {
                var dll = Assembly.LoadFrom(path);
                var type = dll.GetType(prefix + nodeClassName);
                //var type = typeof(UI.Dump).Assembly.GetType("UI." + node.ClassName);
                return type;
            }

            return null;
        }

        private static void GetOrCreateRectTransformList(ref Dictionary<string, RectTransform> rectTransforms)
        {
            if (rectTransforms == null)
            {
                rectTransforms = new Dictionary<string, RectTransform>();
                foreach (var item in SceneInfoGrabber<RectTransform>.GetUIComponentsOnScene())
                {
                    rectTransforms[item.name] = item;
                }
            }
        }

        public override XNode.Node CreateNode(Type type, Vector2 position)
        {
            var res = base.CreateNode(type, position);
            if (res is BaseNode node)
                node.OnCreated();

            return res;
        }

        public override void OnDropObjects(UnityEngine.Object[] objects)
        {
            foreach (var obj in objects)
            {
                var go = obj as GameObject;
                if (go)
                {
                    CreateNode(go);
                }
            }
        }

        private void CreateNode(GameObject go)
        {
            var pos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);

            var rt = go.GetComponent<RectTransform>();
            if (!rt)
                return;

            //check name
            if (target.nodes.OfType<ViewNode>().Any(n => n.name == rt.name))
            {
                EditorUtility.DisplayDialog("Bad name", $"Node with name '{rt.name}' already exists!", "OK");
                return;
            }

            //check name
            if (!Regex.IsMatch(rt.name, "^[a-zA-Z0-9_]+$") ||
                target.nodes.OfType<ViewNode>().Any(n=>n.name == rt.name))
            {
                EditorUtility.DisplayDialog("Bad name", $"Name '{rt.name}' is not allowed!", "OK");
                return;
            }

            //
            var wellKnown = SceneInfoGrabber<BaseView>.GetWellKnownComponent(rt);

            if (wellKnown is BaseView v)
            {
                var node = CreateNode(typeof(ViewNode), pos) as ViewNode;
                node.Build(v);
                //contains new items?
                if (node.ViewInfo.Members.Values.Any(m => m.Component != null || m.Binded != null))
                    ScriptBuilder.CreateScript(node);

                return;
            }

            if (wellKnown is RectTransform || wellKnown is Image)
            {
                //check name
                var type = GetTypeByName(wellKnown.name, go.scene);

                if (type != null && !typeof(BaseView).IsAssignableFrom(type))
                {
                    //the type already exists
                    EditorUtility.DisplayDialog("Bad name", $"Name '{wellKnown.name}' is not allowed!", "OK");
                    return;
                }
                //
                var node = CreateNode(typeof(ViewNode), pos) as ViewNode;
                node.Build(wellKnown.transform as RectTransform);
                BuildScriptForNewNode(node, go);
                return;
            }

            EditorUtility.DisplayDialog("You can not drag this", "You can not drag here Button, Text, InpputField etc.\r\nYou can drag here only RectTransform or View.", "OK");
        }

        static void BuildScriptForNewNode(ViewNode node, GameObject go)
        {
            ScriptBuilder.CreateScript(node);

            var type = GetTypeByName(node.name, node.rt.gameObject.scene);
            if (type != null)
                go.AddComponent(type);
        }

        public override string GetPortTooltip(NodePort port)
        {
            //return base.GetPortTooltip(port);
            return null;
        }

        Color DataPortColor = Color.white;
        Color ActionPortColor = (Color)new Color32(0, 255, 0, 255);

        public override Color GetTypeColor(Type type)
        {
            if (type == typeof(BindInputPort))  return DataPortColor;
            if (type == typeof(BindOutputPort)) return DataPortColor;

            if (type == typeof(ActionInputPort))  return ActionPortColor;
            if (type == typeof(ActionOutputPort)) return ActionPortColor;

            return new Color(0, 1, 0);
        }

        public override void AddContextMenuItems(GenericMenu menu)
        {
            //base.AddContextMenuItems(menu);

            Vector2 pos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);

            menu.AddItem(new GUIContent("Add/Global"), false, () => CreateRootDataView(pos));

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Refresh"), false, () => SynchronizeGraphAndScene());
            menu.AddItem(new GUIContent("Delete unused ports"), false, () => DeleteUnusedPorts());

            menu.AddSeparator("");

            //if (NodeEditorWindow.copyBuffer != null && NodeEditorWindow.copyBuffer.Length > 0)
            //    menu.AddItem(new GUIContent("Paste"), false, () => NodeEditorWindow.current.PasteNodes(pos));
            //else
            //    menu.AddDisabledItem(new GUIContent("Paste"));
            menu.AddItem(new GUIContent("Preferences"), false, () => NodeEditorReflection.OpenPreferences());
            menu.AddCustomContextMenuItems(target);

            
            //menu.AddItem(new GUIContent("Update"), false, () => FrameUtilites.PushGraphsToFrames());
        }

        private void DeleteUnusedPorts()
        {
            foreach (var node in target.nodes.OfType<BaseNode>())
                node.DeleteUnusedPorts();
        }

        private void CreateRootDataView(Vector2 pos)
        {
            (CreateNode(typeof(GlobalNode), pos) as GlobalNode).Build();
        }

        public override float GetNoodleThickness(NodePort output, NodePort input)
        {
            return 3;
            //return base.GetNoodleThickness(output, input);
        }

        public override void OnGUI()
        {
            base.OnGUI();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Build Scripts", GUILayout.Width(100), GUILayout.Height(25)))
                OnBuildScripts();
                
            GUILayout.EndHorizontal();

            if (target.nodes.Count == 0)
            {
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUIStyle style = new GUIStyle("Label");
                style.fontSize = 40;
                style.normal.textColor = new Color32(255, 255, 255, 80);
                GUILayout.Label("Drag RectTransform here", style);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }

            //style.fontSize = 50;
            //GUI.Label(new Rect(650, 650, 300, 50), "HELLO WORLD", style);
        }

        void OnBuildScripts()
        {
            var graph = target as UIGraph;
            //build scripts
            ScriptBuilder.CreateScripts(graph);

            //find obj with UIManager
            var obj = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(o => o.GetComponent<UIManager>() != null);
            if (obj == null)
            {
                var uiManager = Resources.Load<UIManager>("Prefabs/CometUI");
                uiManager = GameObject.Instantiate(uiManager);
                uiManager.name = "CometUI";
                uiManager.UIGraph = graph;
            }
            else
            {
                var uiManager = obj.GetComponent<UIManager>();
                uiManager.UIGraph = graph;
            }
        }
    }
}