// Copyright (c) 2020 Cloudcell Limited

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using uGraph;
using UnityEngine;

public class ProjectLinkController : MonoBehaviour
{
    public void OnProjectLinkClick()
    {
        if (Graph.Instance != null)
        if (!string.IsNullOrEmpty(Graph.Instance.SceneFilePath))
            Process.Start(Path.GetDirectoryName(Graph.Instance.SceneFilePath));
    }
}
