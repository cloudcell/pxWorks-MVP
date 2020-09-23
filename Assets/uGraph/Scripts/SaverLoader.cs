using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using UnityEngine;

namespace uGraph
{
    public static class SaverLoader
    {
        #region Save

        public static void Save(Graph graph, string filePath)
        {
            graph.SceneFilePath = filePath;

            using (var fs = new FileStream(filePath, FileMode.Create))
                SavePxw(graph, fs);

            //get nodes
            var nodes = graph.NodesHolder.GetComponentsInChildren<Node>().ToList();

            //create folders for nodes
            var rootFolder = Path.GetDirectoryName(filePath);
            foreach (var node in nodes)
            {
                var targetFolder = Path.Combine(rootFolder, node.Id.ToString());
                if (!Directory.Exists(targetFolder))
                    SaveNodeInFolder(node, targetFolder);

                //create paths.meta
                CreatePathsMetaFile(node, targetFolder);
            }

            //remove old folders
            var ids = new HashSet<string>(nodes.Select(n => n.Id.ToString()));
            foreach (var folder in Directory.GetDirectories(rootFolder).ToArray())
                if (!Path.GetFileName(folder).Contains('.'))//skip output.graphics, etc
                if (!ids.Contains(Path.GetFileName(folder)))
                {
                    try
                    {
                        //Directory.Delete(folder, true);
                        //move folder to .temp
                        MoveToTemp(rootFolder, folder);
                    }
                    catch { }
                }

            //create spec folders
            var outGrFolder = Path.Combine(rootFolder, UserSettings.Instance.OutputGraphicsFolder);
            if (!Directory.Exists(outGrFolder))
                Directory.CreateDirectory(outGrFolder);
        }

        private static void MoveToTemp(string rootFolder, string folder)
        {
            var tempFolder = Path.Combine(rootFolder, UserSettings.Instance.TempFolder);
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);

            var targetFolder = Path.Combine(tempFolder, Path.GetFileName(folder));
            if (Directory.Exists(targetFolder))
                Directory.Delete(targetFolder, true);

            Directory.Move(folder, targetFolder);
        }

        private static void SaveNodeInFolder(Node node, string dir)
        {
            //check folder in .temp
            var rootFolder = Path.GetDirectoryName(dir);
            var savedFolder = Path.Combine(rootFolder, UserSettings.Instance.TempFolder, node.Id.ToString());
            if (Directory.Exists(savedFolder))
            {
                //copy from .temp
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                CopyFilesRecursively(new DirectoryInfo(savedFolder), new DirectoryInfo(dir), false);
                Directory.Delete(savedFolder, true);
                return;
            }

            //copy from library
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            
            var source = node.SourceLibraryFolder;
            if (Directory.Exists(source))
            {
                CopyFilesRecursively(new DirectoryInfo(source), new DirectoryInfo(dir), false);
            }
        }

        private static void CreatePathsMetaFile(Node node, string dir)
        {
            var sb = new StringBuilder();
            foreach (var knob in node.GetInputs())
            {
                if (knob.JoinedNode != null)
                {
                    var relativePath = ".." + Path.DirectorySeparatorChar + knob.JoinedNode.Id.ToString() + Path.DirectorySeparatorChar + knob.JoinedKnob.Name;
                    sb.AppendLine(relativePath);
                }
                else
                {
                    sb.AppendLine();
                }
            }
            File.WriteAllText(Path.Combine(dir, UserSettings.Instance.PathsMetaFileName), sb.ToString());
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target, bool overwrite)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name), overwrite);
            foreach (FileInfo file in source.GetFiles())
            {
                var targetFile = Path.Combine(target.FullName, file.Name);
                if (!File.Exists(targetFile) || overwrite)
                    file.CopyTo(targetFile, overwrite);
            }
        }

        public static void SavePxw(Graph graph, Stream stream)
        {
            var settings = new XmlWriterSettings();
            var prevCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                using (var wr = new XmlTextWriter(stream, Encoding.UTF8))
                {
                    wr.Formatting = Formatting.Indented;
                    wr.Indentation = 4;
                    wr.WriteStartDocument();
                    WriteGraph(graph, wr);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = prevCulture;
            }
        }

        private static void WriteGraph(Graph graph, XmlTextWriter wr)
        {
            //write Graph
            wr.WriteStartElement("graph");
            var rt = graph.transform as RectTransform;
            wr.WriteAttributeString("scale", rt.localScale.x.ToString());
            wr.WriteAttributeString("x", rt.position.x.ToString());
            wr.WriteAttributeString("y", rt.position.y.ToString());
            wr.WriteAttributeString("file", graph.SceneFilePath);

            //write nodes
            foreach (var node in graph.GetComponentsInChildren<Node>().Where(n => n))
                WriteNode(node, wr);

            wr.WriteEndElement();
        }

        private static void WriteNode(Node node, XmlTextWriter wr)
        {
            wr.WriteStartElement("node");

            var rt = node.transform as RectTransform;
            wr.WriteAttributeString("id", node.Id.ToString());
            wr.WriteAttributeString("name", node.HeaderText);
            wr.WriteAttributeString("x", rt.position.x.ToString());
            wr.WriteAttributeString("y", rt.position.y.ToString());
            wr.WriteAttributeString("source", node.SourceLibraryFolder);

            //write knobs
            foreach (var inputKnob in node.GetComponentsInChildren<InputKnob>())
                WriteInputKnob(inputKnob, wr);

            //write knobs
            foreach (var outputKnob in node.GetComponentsInChildren<OutputKnob>())
                WriteOutputKnob(outputKnob, wr);

            wr.WriteEndElement();
        }

        private static void WriteInputKnob(InputKnob knob, XmlTextWriter wr)
        {
            wr.WriteStartElement("input");

            var rt = knob.transform as RectTransform;
            wr.WriteAttributeString("id", knob.Id.ToString());
            wr.WriteAttributeString("name", knob.Name);
            wr.WriteAttributeString("type", knob.Type.ToString());

            //write connection
            if (knob.JoinedKnob != null)
                wr.WriteAttributeString("join", knob.JoinedKnob.Id.ToString());

            wr.WriteEndElement();
        }

        private static void WriteOutputKnob(OutputKnob knob, XmlTextWriter wr)
        {
            wr.WriteStartElement("output");

            var rt = knob.transform as RectTransform;
            wr.WriteAttributeString("id", knob.Id.ToString());
            wr.WriteAttributeString("name", knob.Name);
            //wr.WriteAttributeString("type", knob.Type.ToString());

            wr.WriteEndElement();
        }

        #endregion

        #region Load

        public static void Load(Graph graph, string filePath, bool clearGraph)
        {
            using (var fs = new FileStream(filePath, FileMode.Open))
                Load(graph, fs, clearGraph);

            ClearSpecFolders(Path.GetDirectoryName(filePath));

            graph.SceneFilePath = filePath;
        }

        public static void ClearSpecFolders(string folder)
        {
            //clear spec folders
            var outGrFolder = Path.Combine(folder, UserSettings.Instance.OutputGraphicsFolder);
            try
            {
                if (!Directory.Exists(outGrFolder))
                    Directory.CreateDirectory(outGrFolder);
                ClearFolder(outGrFolder);
            }
            catch
            {
            }
        }

        private static void ClearFolder(string FolderName)
        {
            DirectoryInfo dir = new DirectoryInfo(FolderName);

            foreach (FileInfo fi in dir.GetFiles())
                try
                {
                    fi.Delete();
                }
                catch { }

            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                ClearFolder(di.FullName);
                try
                { 
                    di.Delete();
                }
                catch { }
            }
        }

        public static void Load(Graph graph, Stream stream, bool clearGraph)
        {
            if (clearGraph)
                graph.Clear();

            var prevCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                var xml = new XmlDocument();
                xml.Load(stream);
                //read graph
                ReadGraph(graph, xml);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = prevCulture;
            }
        }

        private static void ReadGraph(Graph graph, XmlDocument xml)
        {
            var node = xml.SelectSingleNode("graph");
            var scale = float.Parse(node.Attributes["scale"].Value);
            var x = float.Parse(node.Attributes["x"].Value);
            var y = float.Parse(node.Attributes["y"].Value);
            var file = node.Attributes["file"].Value;
            graph.transform.localScale = Vector3.one * scale;
            graph.transform.position = new Vector3(x, y, 0);
            graph.SceneFilePath = file;

            //read nodes
            var list = xml.SelectNodes("graph/node");
            foreach (XmlNode n in list)
                ReadNode(n, graph);

            //creater joins
            CreateJoins(graph);
        }

        private static void CreateJoins(Graph graph)
        {
            var idToKnob = new Dictionary<Guid, OutputKnob>();
            foreach (var ok in graph.GetComponentsInChildren<OutputKnob>())
                idToKnob[ok.Id] = ok;

            foreach (var ik in graph.GetComponentsInChildren<InputKnob>())
            if (ik.joinedGuid != Guid.Empty)
            {
                var ok = idToKnob[ik.joinedGuid];
                ik.SetInputConnection(ok);
                ik.joinedGuid = Guid.Empty;
            }
        }

        private static void ReadNode(XmlNode n, Graph graph)
        {
            var name = n.Attributes["name"].Value;
            var x = float.Parse(n.Attributes["x"].Value);
            var y = float.Parse(n.Attributes["y"].Value);
            var id = n.Attributes["id"].Value;
            var sourceLibraryFolder = n.Attributes["source"].Value;

            var node = GameObject.Instantiate(graph.NodePrefab, graph.NodesHolder).GetComponent<Node>();
            node.Id = id;
            var rt = node.GetComponent<RectTransform>();
            rt.position = new Vector3(x, y, 0);
            node.HeaderText = name;
            node.SourceLibraryFolder = sourceLibraryFolder;

            //read and create knobs
            node.RemoveKnobs();

            foreach (XmlNode kn in n.SelectNodes("input"))
                ReadInputKnob(kn, node);
            foreach (XmlNode kn in n.SelectNodes("output"))
                ReadOutputKnob(kn, node);
        }

        private static void ReadOutputKnob(XmlNode n, Node parent)
        {
            var knob = GameObject.Instantiate(parent.OutputKnobPrefab, parent.transform).GetComponent<OutputKnob>();
            knob.Name = n.Attributes["name"].Value;
            knob.Id = Guid.Parse(n.Attributes["id"].Value);

            //if (n.Attributes["type"] != null)
            //    Enum.TryParse<KnobType>(n.Attributes["type"].Value, out knob.Type);
        }

        private static void ReadInputKnob(XmlNode n, Node parent)
        {
            var knob = GameObject.Instantiate(parent.InputKnobPrefab, parent.transform).GetComponent<InputKnob>();
            knob.Name = n.Attributes["name"].Value;
            knob.Id = Guid.Parse(n.Attributes["id"].Value);

            if (n.Attributes["type"] != null)
                Enum.TryParse<KnobType>(n.Attributes["type"].Value, out knob.Type);

            var a = n.Attributes["join"];
            if (a != null)
                knob.joinedGuid = Guid.Parse(a.Value);
            else
                knob.joinedGuid = Guid.Empty;
        }

        #endregion
    }
}