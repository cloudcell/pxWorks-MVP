using CometUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace uGraph
{
    class Runner : MonoBehaviour
    {
        bool isRun;

        //list of nodes on the graph
        List<Node> nodes;

        //list of nodes that was executed and successfully created output data
        HashSet<Node> readyNodes;

        //list of nodes that are executed now
        HashSet<Node> runningNodes;

        List<Process> runningProcesses;

        //last exception of running
        Exception lastRunException;

        //queue of finished nodes
        Queue<(Node, Process, Exception)> queueToStop = new Queue<(Node, Process, Exception)>();

        Graph graph;

        public static Runner Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void Run(Graph graph)
        {
            if (Bus.RunnerState.Value != RunnerState.Stop)
                Stop();

            this.graph = graph;

            Prepare();
            CheckNodes();

            Bus.RunnerState.Value = RunnerState.Run;

            //if (IsGraphCompleted)
            //{
            //    Bus.RunCompleted += true;
            //    Bus.RunnerState.Value = RunnerState.Stop;
            //    return;
            //}

            FindReadyToRunNodesAndRun(true);
        }

        private void RemoveSignalFiles()
        {
            var inputs = Graph.Instance.NodesHolder.GetComponentsInChildren<InputKnob>();

            foreach(var i in inputs.Where(k => k.Type == KnobType.signal))
            {
                if (i.JoinedKnob != null)
                {
                    var path = i.JoinedKnob.OutputFilePath;
                    try
                    {
                        if (File.Exists(path))
                            File.Delete(path);
                    }
                    catch {/*can not delete file*/ }
                }
            }
        }

        private void FindReadyToRunNodesAndRun(bool isFirstRun)
        {
            foreach (var node in nodes)
            {
                //is not ready or executing ?
                if (runningNodes.Contains(node))
                    continue;

                //has running output nodes? -> skip
                var hasRunningOutputNodes = node.GetOutputConnections().Any(k => runningNodes.Contains(k.GetComponentInParent<Node>()));
                if (hasRunningOutputNodes)
                    continue;

                if (isFirstRun)
                {
                    //Run nodes w/o inputs
                    var hasInputs = node.GetInputs().Any();
                    if (!hasInputs)
                        RunNode(node);
                }else
                if (node.GetInputs().Any())//do not run nodes w/o inputs
                {
                    //all input data versions were changed?
                    var dataInputs = node.GetInputs().Where(k => k.Type == KnobType.data).ToArray();
                    var inputDataChanged = dataInputs.All( k => k.JoinedNode.OutputDataVersion > k.LastProcessedDataVersion) && dataInputs.Any();
                    if (inputDataChanged)
                    {
                        RunNode(node);
                        continue;
                    }

                    //any singal input was changed
                    var signalInputs = node.GetInputs().Where(k => k.Type == KnobType.signal).ToArray();
                    var signalDataChanged = signalInputs.Any(k => k.JoinedNode.OutputDataVersion > k.LastProcessedDataVersion && SignalFileExists(k.JoinedKnob));
                    if (signalDataChanged)
                    {
                        RunNode(node);
                        continue;
                    }


                    ////are data inputs ready?
                    //var inputs = node.GetInputs().ToArray();
                    //var isInputDataReady = inputs.Where(k => k.Type != KnobType.signal).Select(k => k.JoinedNode).All(n => readyNodes.Contains(n));
                    //var hasSignals = inputs.Any(k => k.Type == KnobType.signal);
                    //var hasReadySignals = inputs.Any(k => k.Type == KnobType.signal && signals.Contains(k));
                    //if (isInputDataReady && (!hasSignals || hasReadySignals))
                    //    RunNode(node);
                }
            }
        }

        private bool SignalFileExists(OutputKnob k)
        {
            return File.Exists(k.OutputFilePath);
        }

        private void RunNode(Node node)
        {
            //capture data versions
            foreach (var knob in node.GetInputs())
                knob.LastProcessedDataVersion = knob.JoinedNode.OutputDataVersion;

            //add to runnning list
            runningNodes.Add(node);
            node.SetState(NodeRunState.Running);

            try
            {
                //read run data
                var runData = ReadRunData(node);
                var startInfo = new ProcessStartInfo(runData.Executable, runData.CommandLine);
                startInfo.WorkingDirectory = node.ProjectDirectory;
                startInfo.CreateNoWindow = true;

                //start process
                var pr = new Process();
                pr.EnableRaisingEvents = true;
                pr.StartInfo = startInfo;
                pr.Exited += (o, O) => OnProcessExited(node, pr);
                var log = true;
                if (log)
                {
                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true;
                    pr.OutputDataReceived += (_, d) => Pr_OutputDataReceived(d.Data, node);
                }
                //
                pr.Start();
                //
                if (log)
                {
                    pr.BeginOutputReadLine();
                }
                runningProcesses.Add(pr);
            }
            catch (FileNotFoundException ex)
            {
                OnRunFailed(node, ex);
            }
            catch (Exception ex)
            {
                OnRunFailed(node, ex);
            }
        }

        private void Pr_OutputDataReceived(string data, Node node)
        {
            UnityEngine.Debug.Log(node.Id + "> " + data);
            Dispatcher.Enqueue(() => Bus.SetStatusLabel += node.Id + "> " + data);
        }

        private void OnRunFailed(Node node, Exception ex)
        {
            //remove from run list
            runningNodes.Remove(node);
            //remember exception
            lastRunException = ex;
            //
            node.SetState(NodeRunState.Exception);
            //stop
            Stop();
        }

        class RunData
        {
            public string Executable;
            public string CommandLine;
        }

        private RunData ReadRunData(Node node)
        {
            var path = Path.Combine(node.ProjectDirectory, UserSettings.Instance.RunMetaFileName);
            var lines = File.ReadAllLines(path);
            var res = new RunData();
            if (lines.Length > 0) res.Executable = lines[0].Trim();
            if (lines.Length > 1) res.CommandLine = lines[1].Trim();

            return res;
        }

        private void OnProcessExited(Node node, Process pr)
        {
            Exception ex = null;

            if (pr.ExitCode != 0)
            {
                //exception durnig script execution
                ex = new Exception("Script Exception in " + node.HeaderText + Environment.NewLine + "Exit code: " + pr.ExitCode);
            }
            lock(queueToStop)
                queueToStop.Enqueue((node, pr, ex));
            //node.SetState(ex == null ? NodeRunState.Ready : NodeRunState.Exception);
        }

        private void Update()
        {
            //if (Time.frameCount % 60 != 0)
            //    return;

            lock (queueToStop)
            if (queueToStop.Count > 0)
            {
                var newNodesAreReady = false;
                while(queueToStop.Count > 0)
                {
                    var res = queueToStop.Dequeue();

                    //remove from lists
                    runningProcesses.Remove(res.Item2);
                    runningNodes.Remove(res.Item1);

                    //exception?
                    if (res.Item3 != null)
                    {
                        lastRunException = res.Item3;
                        //
                        res.Item1.SetState(NodeRunState.Exception);
                        //stop
                        queueToStop.Clear();
                        Stop();
                        return;
                    }
                    //add to ready nodes
                    readyNodes.Add(res.Item1);
                    res.Item1.SetState(NodeRunState.Ready);
                    res.Item1.OutputDataVersion++;
                    newNodesAreReady = true;
                }

                //new nodes are ready ...
                //if (newNodesAreReady)
                //    OnNewNodesAreReady();

                ////completed?
                //if (IsGraphCompleted)
                //    Stop();

                //run next scripts
                if (Bus.RunnerState == RunnerState.Run)
                    FindReadyToRunNodesAndRun(false);
            }

            //exception?
            if (Bus.RunnerState == RunnerState.Stop && lastRunException != null)
            {
                UIManager.ShowDialog(null, lastRunException.Message, "Ok");
                lastRunException = null;
            }

            //completed?
            if (runningNodes != null && runningNodes.Count == 0 && Bus.RunnerState == RunnerState.Run)
            {
                //run next scripts
                FindReadyToRunNodesAndRun(false);

                if (runningNodes.Count == 0)
                {
                    Bus.RunnerState += RunnerState.Stop;
                    Bus.RunCompleted += true;
                }
            }
        }

        //private void OnNewNodesAreReady()
        //{
        //    //switch off ready nodes if all output data is processed
        //    foreach (var node in readyNodes.ToArray())
        //    {
        //        var outputs = node.GetOutputConnections();
        //        var outputDataNodes = outputs.Where(k => k.Type == KnobType.data).Select(k => k.GetComponentInParent<Node>()).ToArray();

        //        var allOutConnectedNodesAreReady = outputDataNodes.Any() && outputDataNodes.All(n => readyNodes.Contains(n));
        //        if (allOutConnectedNodesAreReady)
        //        {
        //            node.SetState(NodeRunState.None);
        //            readyNodes.Remove(node);
        //            SendSignalFromNode(node);
        //        }
        //    }
        //}

        //private void SendSignalFromNode(Node node)
        //{
        //    //get output signal connections
        //    foreach (var signal in node.GetOutputConnections().Where(k=>k.Type == KnobType.signal))
        //        signals.Add(signal);
        //}

        public void Stop()
        {
            foreach (var pr in runningProcesses)
            {
                try
                {
                    if (!pr.HasExited)
                        pr.Kill();
                }
                catch { }
            }

            lock (queueToStop)
                queueToStop.Clear();
            runningNodes?.Clear();
            runningProcesses?.Clear();
            Bus.RunnerState += RunnerState.Stop;
        }

        //bool IsGraphCompleted => nodes.Count == readyNodes.Count;

        private void CheckNodes()
        {
            //check empty input knobs
            foreach (var n in nodes)
                foreach (var knob in n.GetInputs())
                    if (knob.JoinedKnob == null)
                        throw new Exception("Some input sockets are not connected");
        }

        string runFolder;

        private void Prepare()
        {
            nodes = graph.NodesHolder.GetComponentsInChildren<Node>().ToList();
            readyNodes = new HashSet<Node>();
            runningNodes = new HashSet<Node>();
            lastRunException = null;
            runningProcesses = new List<Process>();
            queueToStop = new Queue<(Node, Process, Exception)>();

            foreach (var n in nodes)
            {
                n.SetState(NodeRunState.None);
                n.OutputDataVersion = 0;
            }

            foreach (var knob in Graph.Instance.NodesHolder.GetComponentsInChildren<InputKnob>())
                knob.LastProcessedDataVersion = 0;

            RemoveSignalFiles();
        }
    }

    enum RunnerState
    {
        Stop, Run, Pause
    }
}

namespace uGraph
{
    public partial class Node
    {
        public string SourceLibraryFolder;

        //internal string GetScriptFilePath()
        //{
        //    if (!Directory.Exists(Graph.Instance.ProjectDirectory))
        //        return null;
        //    var dir = Path.Combine(Graph.Instance.ProjectDirectory, Id.ToString());
        //    return Directory.GetFiles(dir, UserSettings.Instance.ScriptSearchPattern).FirstOrDefault();
        //}

        public string ProjectDirectory => Path.Combine(Graph.Instance.ProjectDirectory, Id.ToString());

        public int OutputDataVersion = 0;
    }
}