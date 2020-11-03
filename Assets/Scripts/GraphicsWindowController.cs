// Copyright (c) 2020 Cloudcell Limited

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using uGraph;
using UnityEngine;

public class GraphicsWindowController : Singleton<GraphicsWindowController>
{
    Process graphicsProcess;

    private void Start()
    {
        Application.quitting += Application_quitting;
        InvokeRepeating("RemoveOldGraphicFiles", 3, 3);
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

    void RemoveOldGraphicFiles()
    {
        if (Graph.Instance == null || Graph.Instance.ProjectDirectory == null)
            return;

        var folder = Path.Combine(Graph.Instance.ProjectDirectory, UserSettings.Instance.OutputGraphicsFolder);

        if (!Directory.Exists(folder))
            return;

        //get list of files
        var files = new DirectoryInfo(folder)
            .GetFiles()
            .OrderBy(f => f.LastWriteTime)
            .ToArray();

        //remove old files
        for (int i = 0; i < files.Length - UserSettings.Instance.MaxOutputGraphicsFilesCount; i++)
            try
            {
                File.Delete(files[i].FullName);
            }
            catch { }
    }
}
