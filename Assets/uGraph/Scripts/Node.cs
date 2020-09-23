using CometUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace uGraph
{
    public partial class Node : MonoBehaviour, IDraggable
    {
        public string Id;
        [SerializeField] TMPro.TextMeshProUGUI headerText;

        public InputKnob InputKnobPrefab;
        public OutputKnob OutputKnobPrefab;
        [SerializeField] Image SelectedImage;

        [SerializeField] Image StateImage;
        [SerializeField] TMPro.TextMeshProUGUI StateText;
        [SerializeField] TMPro.TextMeshProUGUI idText;

        [SerializeField] GameObject pnMenu;

        public void ShowHideMenu()
        {
            if (pnMenu.gameObject.activeSelf)
                pnMenu.gameObject.SetActive(false);
            else
            {
                Bus.ClosePopupMenu += true;
                pnMenu.gameObject.SetActive(true);
            }
        }

        public void RefreshNode()
        {
            Bus.ClosePopupMenu += true;
            AddKnobsFromSource(ProjectDirectory);

            SaverLoader.Save(Graph.Instance, Graph.Instance.SceneFilePath);
            Bus.SceneChanged += true;
        }

        string GetProposedName(string name, int counter)
        {
            if (counter == 0)
                return name;
            return name + " " + counter;
        }

        public void SaveNodeToLibrary()
        {
            Bus.ClosePopupMenu += true;

            OpenFileController.Instance.OpenFolder("Select Folder", UserSettings.Instance.LibraryPath, (path) =>
            {
                if (string.IsNullOrWhiteSpace(path))
                    return;

                try
                {
                    //is block folder?
                    if (File.Exists(Path.Combine(path, UserSettings.Instance.RunMetaFileName)))
                        throw new Exception("This folder contains node already");

                    var name = HeaderText;
                    var counter = 0;
                    while (Directory.Exists(Path.Combine(path, GetProposedName(name, counter))))
                        counter++;

                    UIManager.ShowDialogInput(null, "Name of Node", GetProposedName(name, counter), "Node name", "Save", onClosed: (nodeName) =>
                    {
                        if (string.IsNullOrEmpty(nodeName))
                            return;

                        if (Directory.Exists(Path.Combine(path, nodeName)))
                            UIManager.ShowDialog(null, "Folder with this name exists already!", "Ok");
                        else
                            SaveNodeToLibrary(this, Path.Combine(path, nodeName));
                    });
                }
                catch (Exception ex)
                {
                    UIManager.ShowDialog(null, ex.Message, "Ok");
                    UnityEngine.Debug.LogException(ex);
                }
            });
        }

        private void SaveNodeToLibrary(Node node, string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                //copy from library
                var source = node.ProjectDirectory;
                if (Directory.Exists(source))
                {
                    SaverLoader.CopyFilesRecursively(new DirectoryInfo(source), new DirectoryInfo(path), true);
                }

                //delete paths.meta
                var pathMetaFile = Path.Combine(path, UserSettings.Instance.PathsMetaFileName);
                if (File.Exists(pathMetaFile))
                    File.Delete(pathMetaFile);

                Bus.LibraryChanged += true;
            }
            catch (Exception ex)
            {
                UIManager.ShowDialog(null, ex.Message, "Ok");
                UnityEngine.Debug.LogException(ex);
            }
        }

        public void OpenFolder()
        {
            Bus.ClosePopupMenu += true;
            Process.Start(ProjectDirectory);
        }

        public void Init()
        {
            var parts = Guid.NewGuid().ToString().Split('-');
            Id = parts[parts.Length - 1];
        }

        private void Start()
        {
            idText.text = Id.ToString();
            Bus.ClosePopupMenu.Subscribe(this, CloseMenu);
        }

        private void CloseMenu()
        {
            pnMenu.SetActive(false);
        }

        public bool Selected
        {
            get => SelectedImage.IsActive();
            set => SelectedImage.gameObject.SetActive(value);
        }

        public string HeaderText
        {
            get => headerText.text;
            set => headerText.text = value;
        }

        internal void Remove()
        {
            //remove input knobs
            foreach (var ib in GetComponentsInChildren<InputKnob>())
                ib.SetInputConnection(null);

            //remove output knobs
            foreach (var ib in Graph.Instance.NodesHolder.GetComponentsInChildren<InputKnob>())
            if (ib.JoinedNode == this)
                ib.SetInputConnection(null);

            DestroyImmediate(gameObject);
        }

        public void RemoveKnobs()
        {
            foreach (var knob in GetComponentsInChildren<InputKnob>().ToArray())
                DestroyImmediate(knob.gameObject);

            foreach (var knob in GetComponentsInChildren<OutputKnob>().ToArray())
                DestroyImmediate(knob.gameObject);
        }

        public IEnumerable<OutputKnob> GetInputConnections()
        {
            foreach (var knob in GetComponentsInChildren<InputKnob>())
                if (knob.JoinedKnob != null)
                    yield return knob.JoinedKnob;
        }

        public IEnumerable<InputKnob> GetInputs()
        {
            foreach (var knob in GetComponentsInChildren<InputKnob>())
                yield return knob;
        }

        public IEnumerable<OutputKnob> GetOutputs()
        {
            foreach (var knob in GetComponentsInChildren<OutputKnob>())
                yield return knob;
        }

        public IEnumerable<InputKnob> GetOutputConnections()
        {
            return Graph.Instance.NodesHolder.GetComponentsInChildren<InputKnob>().Where(k => k.JoinedNode == this);
        }

        public void SetState(NodeRunState state)
        {
            switch(state)
            {
                case NodeRunState.None:
                    const float floatGray = 0.4f;
                    StateImage.color = new Color(floatGray, floatGray, floatGray, 1);
                    StateText.text = "Waiting";
                    StateText.color = new Color(0.8f, 0.8f, 0.8f, 1);

                    break;
                case NodeRunState.Ready:
                    StateText.color = StateImage.color = new Color(0, 1, 0, 1);
                    StateText.text = "Ready";
                    break;
                case NodeRunState.Running:
                    StateText.color = StateImage.color = new Color(1, 1, 0, 1);
                    StateText.text = "Running";
                    break;

                case NodeRunState.Exception:
                    StateImage.color = new Color(1, 0, 0, 1);
                    StateText.text = "Exception";
                    StateText.color = new Color(0.8f, 0.8f, 0.8f, 1);
                    break;
            }
        }

        internal void AddKnobsFromSource(string sourceFolder)
        {
            HashSet<string> knownInputKnobs = new HashSet<string>();
            HashSet<string> knownOutputKnobs = new HashSet<string>();

            //add input knobs
            var inMetaFilePath = Path.Combine(sourceFolder, UserSettings.Instance.InputMetaFileName);
            if (File.Exists(inMetaFilePath))
            {
                var lines = File.ReadAllLines(inMetaFilePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    var parts = line.Split(' ');
                    var knobName = parts[0].Trim();
                    knownInputKnobs.Add(knobName);

                    var knob = GetComponentsInChildren<InputKnob>().FirstOrDefault(k => k.Name == knobName);

                    //not exists?
                    if (knob == null)
                    {
                        knob = GameObject.Instantiate(InputKnobPrefab, transform);
                        knob.Name = knobName;
                        knob.Id = Guid.NewGuid();
                    }

                    //parse knob type
                    knob.Type = KnobType.data;

                    if (parts.Length > 1)
                        if(Enum.TryParse<KnobType>(parts[1], out var type))
                            knob.Type = type;

                    knob.Init();
                }
            }

            //add output knobs
            var outMetaFilePath = Path.Combine(sourceFolder, UserSettings.Instance.OutputMetaFileName);
            if (File.Exists(outMetaFilePath))
            {
                var lines = File.ReadAllLines(outMetaFilePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    var parts = line.Split(' ');
                    var knobName = parts[0].Trim();
                    knownOutputKnobs.Add(knobName);

                    var knob = GetComponentsInChildren<OutputKnob>().FirstOrDefault(k => k.Name == knobName);

                    //not exists?
                    if (knob == null)
                    { 
                        knob = GameObject.Instantiate(OutputKnobPrefab, transform);
                        knob.Name = knobName;
                        knob.Id = Guid.NewGuid();
                    }

                    ////parse knob type
                    //if (parts.Length > 1)
                    //if (Enum.TryParse<KnobType>(parts[1], out var type))
                    //    knob.Type = type;
                }
            }

            //remove knobs
            foreach (var kn in GetComponentsInChildren<InputKnob>().Where(k => !knownInputKnobs.Contains(k.Name)).ToArray())
            {
                kn.SetInputConnection(null);
                DestroyImmediate(kn.gameObject);
            }

            foreach (var kn in GetComponentsInChildren<OutputKnob>().Where(k => !knownOutputKnobs.Contains(k.Name)).ToArray())
            {
                foreach (var ib in Graph.Instance.NodesHolder.GetComponentsInChildren<InputKnob>())
                    if (ib.JoinedKnob == kn)
                        ib.SetInputConnection(null);

                DestroyImmediate(kn.gameObject);
            }
        }

        #region IDraggable

        public DragMode DragMode => DragMode.Move;
        public RectTransform RectTransform => transform as RectTransform;

        public void OnDropped(IAcceptor acceptor)
        {
        }

        public void OnStartDrag()
        {
        }

        public void OnDragging()
        {
        }

        public void OnStopDrag(IAcceptor acceptor)
        {
            SaverLoader.Save(Graph.Instance, Graph.Instance.SceneFilePath);
            Bus.SceneChanged += true;
        }

        #endregion
    }

    public enum NodeRunState
    {
        None, Running, Ready, Exception
    }
}