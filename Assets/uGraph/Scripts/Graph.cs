// Copyright (c) 2020 Cloudcell Limited

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace uGraph
{
    public class Graph : MonoBehaviour
    {
        public RectTransform LinesHolder;
        public RectTransform NodesHolder;
        public RectTransform Center;

        public Node NodePrefab;

        public string SceneFilePath = null;

        public static Graph Instance { get; private set; }

        private void Start()
        {
            Instance = this;
        }

        public string ProjectDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(SceneFilePath))
                    return null;
                return Path.GetDirectoryName(SceneFilePath);
            }
        }

        public bool DirectoryIsDefined
        {
            get
            {
                if (string.IsNullOrEmpty(SceneFilePath))
                    return false;
                if(!Directory.Exists(ProjectDirectory))
                    return false;
                return true;
            }
        }        

        internal void Clear()
        {
            //clear nodes
            foreach (var node in NodesHolder.GetComponentsInChildren<Node>().ToArray())
                node.Remove();
        }

        public string GetProposedNameOfScene()
        {
            if (!string.IsNullOrEmpty(SceneFilePath))
                return Path.GetFileNameWithoutExtension(SceneFilePath);

            //var model = Get<Model>().FirstOrDefault(m => !(m is TextModel));
            //if (model != null)
            //    return Path.GetFileNameWithoutExtension(model.name);

            return "Graph";
        }
    }

    public static class GraphHelper
    {
        static Graph graph => Graph.Instance;

        public static bool CheckNodeName(string name)
        {
            return Regex.IsMatch(name, @"^([a-zA-Z_][a-zA-Z\d_]*)$");
        }

        public static void NewGraph()
        {
            graph.Clear();
            UndoRedoManager.Instance.ClearHistory();

            Bus.SceneFilePathChanged += true;
            Bus.SceneChanged += true;

            ClearMemory();
        }

        public static void SaveGraph(string filePath)
        {
            SaverLoader.Save(graph, filePath);
            Bus.SceneFilePathChanged += true;
        }

        public static void LoadGraph(string filePath)
        {
            SaverLoader.Load(graph, filePath, true);
            Bus.SceneFilePathChanged += true;
            UndoRedoManager.Instance.ClearHistory();
        }

        public static void ClearMemory()
        {
            GC.Collect();
            Resources.UnloadUnusedAssets();
            var mem = GC.GetTotalMemory(true);
            Debug.Log("Memory: " + mem.ToString("0,0."));

            //check memory
            if (!Is64Bit)
            {
                const long waringiMemorySize = 1700000000;//1.7 gb
                if (mem > waringiMemorySize)
                {
                    Bus.SetStatusLabel += "<color=magenta>WARNING! Free memory is critically low. Please restart Application!</color>";
                }
            }
        }

        internal static void AddNode(Graph graph, string folder)
        {
            using (var command = new StateCommand("Add node"))
            {
                var node = GameObject.Instantiate(graph.NodePrefab, graph.NodesHolder.transform);
                node.Init();
                node.transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
                node.HeaderText = Path.GetFileName(folder);
                node.SourceLibraryFolder = folder;
                node.RemoveKnobs();
                node.AddKnobsFromSource(folder);
            }
        }

        internal static void CloneNode(Graph graph, Node source)
        {
            using (var command = new StateCommand("Clone node"))
            {
                var node = GameObject.Instantiate(graph.NodePrefab, graph.NodesHolder.transform);
                node.Init();
                node.transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
                node.HeaderText = source.HeaderText;
                node.SourceLibraryFolder = source.SourceLibraryFolder;
                node.RemoveKnobs();
                node.AddKnobsFromSource(source.FullFolderPath);
                SaverLoader.Save(Graph.Instance, Graph.Instance.SceneFilePath);
                SaverLoader.CopyFilesRecursively(new DirectoryInfo(source.FullFolderPath), new DirectoryInfo(node.FullFolderPath), true);
            }
            Bus.SceneChanged += true;
        }

        public static bool Is64Bit
        {
            get { return Marshal.SizeOf(typeof(IntPtr)) == 8; }
        }

        public static void CleanUp()
        {
            foreach (var node in Graph.Instance.NodesHolder.GetComponentsInChildren<Node>())
                CleanUp(node);
        }

        private static void CleanUp(Node node)
        {
            var names = node.GetComponentsInChildren<InputKnob>().Select(k => k.Name.ToLower())
                .Union(node.GetComponentsInChildren<OutputKnob>().Select(k => k.Name.ToLower())).Distinct().ToList();

            var files = Directory.GetFiles(node.FullFolderPath).Where(f => names.Contains(Path.GetFileNameWithoutExtension(f).ToLower())).ToArray();
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        private static void CleanUp_old(Node node)
        {
            var files = Directory.GetFiles(node.FullFolderPath).Where(f=>!Path.GetFileName(f).ToLower().Contains(UserSettings.Instance.MetaKeyword)).ToArray();

            foreach(var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }
    }
}
