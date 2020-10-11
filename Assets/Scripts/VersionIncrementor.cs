using UnityEngine;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR
[InitializeOnLoad]
public class VersionIncrementor
{
    static VersionIncrementor()
    {
        EditorApplication.playModeStateChanged += LogPlayModeState;
    }

    private static void LogPlayModeState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.playModeStateChanged -= LogPlayModeState;
            ReadVersionAndIncrement();
        }
    }

    static void ReadVersionAndIncrement()
    {
        string versionText = UnityEditor.PlayerSettings.bundleVersion;

        if (versionText != null)
        {
            versionText = versionText.Trim(); //clean up whitespace if necessary
            string[] lines = versionText.Split('.');

            int MajorVersion = int.Parse(lines[0]);
            int MinorVersion = lines.Length > 0 ? int.Parse(lines[1]) : 0;
            int SubMinorVersion = lines.Length > 1 ? int.Parse(lines[2]) + 1 : 1; //increment here

            if (SubMinorVersion > 999)
            {
                MinorVersion++;
                SubMinorVersion = 1;
            }

            versionText = MajorVersion.ToString("0") + "." +
                          MinorVersion.ToString("0") + "." +
                          SubMinorVersion.ToString("000");

            Debug.Log("Version Incremented " + versionText);

            UnityEditor.PlayerSettings.bundleVersion = versionText;

            //tell unity the file changed (important if the versionTextFileNameAndPath is in the Assets folder)
            AssetDatabase.Refresh();
        }
    }
}
#endif