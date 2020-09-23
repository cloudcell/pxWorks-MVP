using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnityExtededShortKeys : ScriptableObject
{
    [MenuItem("My/Hotkeys/Run _F5")]
//    [MenuItem("My/Hotkeys/Run2 _%H")]
    static void PlayGame()
    {
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "", false);
        EditorApplication.ExecuteMenuItem("Edit/Play");
    }
}