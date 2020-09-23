using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using uGraph;
using UnityEngine;

public class WindowStateController : Singleton<WindowStateController>
{
    FullScreenMode initState;
    Resolution initScreenRes;
    Vector2Int initWinSize;

    void Start()
    {
        Bus.SceneFilePathChanged.Subscribe(this, OnSceneFilePathChanged);

        initState = Screen.fullScreenMode;
        initScreenRes = Screen.currentResolution;
        if (Screen.fullScreenMode == FullScreenMode.Windowed)
            initWinSize = new Vector2Int(Screen.width, Screen.height);
    }

    //Import the following.
    [DllImport("user32.dll", EntryPoint = "SetWindowText")]
    public static extern bool SetWindowText(System.IntPtr hwnd, System.String lpString);
    [DllImport("user32.dll")]
    static extern System.IntPtr GetActiveWindow();

    private void OnSceneFilePathChanged()
    {
#if UNITY_EDITOR || !UNITY_STANDALONE_WIN
        return;
#endif
        //Get the window handle.
        var windowPtr = GetActiveWindow();
        //Set the title text using the window handle.
        var title = Application.productName;
        if (!string.IsNullOrEmpty(Graph.Instance.SceneFilePath))
            title += " [" + Path.GetDirectoryName(Graph.Instance.SceneFilePath) + "]";
        SetWindowText(windowPtr, title);
    }

    public void ChangeWinState()
    {
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.Windowed:
                initWinSize = new Vector2Int(Screen.width, Screen.height);
                Screen.SetResolution(initScreenRes.width, initScreenRes.height, FullScreenMode.FullScreenWindow);
                break;
            default:
                if (initWinSize == Vector2Int.zero)
                    initWinSize = new Vector2Int(initScreenRes.width * 2 / 3, initScreenRes.height * 2 / 3);
                Screen.SetResolution(initWinSize.x, initWinSize.y, FullScreenMode.Windowed);
                break;
        }
    }
}
