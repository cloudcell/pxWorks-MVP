// Copyright (c) 2020 Cloudcell Limited

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using uGraph;
using UnityEngine;

public class GraphicsWindowController : Singleton<GraphicsWindowController>
{
    Process graphicsProcess;

    private void Start()
    {
        Application.quitting += Application_quitting;
    }

    private void Application_quitting()
    {
        CloseGraphicsWindowIfOpened();
    }

    public void OpenGraphicsWindow()
    {
        var args = Environment.GetCommandLineArgs();

        if (graphicsProcess != null && !graphicsProcess.HasExited)
        {
            return;
        }

        var graph = Graph.Instance;
        var folder = Path.Combine(graph.ProjectDirectory, UserSettings.Instance.OutputGraphicsFolder);

        var si = new ProcessStartInfo();
        si.Arguments = "/graphics " + "\"" + folder + "\"";
        si.FileName = args[0];
        graphicsProcess = new Process();
        graphicsProcess.StartInfo = si;
        graphicsProcess.Start();
    }

    public void CloseGraphicsWindowIfOpened()
    {
        if (graphicsProcess != null && !graphicsProcess.HasExited)
        {
            graphicsProcess.Kill();
            graphicsProcess = null;
        }
    }
}
