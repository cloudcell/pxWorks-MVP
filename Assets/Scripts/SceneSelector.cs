using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSelector : MonoBehaviour
{
    public string MainScene;
    public string GraphicsScene;

    void Awake()
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            var comm = args[1];
            if (comm == "/graphics")
                SceneManager.LoadScene(GraphicsScene);
        }else
            SceneManager.LoadScene(MainScene);
    }
}
