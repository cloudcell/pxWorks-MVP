using SFB;
using System;

class OpenFileController : Singleton<OpenFileController>
{
    public void OpenFile(string title, string filter, Action<string> feedback)
    {
        var parts = filter.Split('|');
        var ext = new ExtensionFilter[parts.Length / 2];
        for (int i=0;i<ext.Length;i++)
        {
            ext[i] = new ExtensionFilter(parts[i * 2], parts[i * 2 + 1].Replace("*.", "").Split(';'));
        }
        StandaloneFileBrowser.OpenFilePanelAsync(title, "", ext, false, f =>
        {
            if (f.Length > 0)
                feedback(f[0]);
        });
    }

    public void OpenFolder(string title, string dir, Action<string> feedback)
    {
        StandaloneFileBrowser.OpenFolderPanelAsync(title, dir, false, f =>
        {
            if (f.Length > 0)
                feedback(f[0]);
        });
    }

    public void SaveFile(string title, string defaultFileName, string filter, Action<string> feedback)
    {
        var parts = filter.Split('|');
        var ext = new ExtensionFilter[parts.Length / 2];
        for (int i = 0; i < ext.Length; i++)
        {
            ext[i] = new ExtensionFilter(parts[i * 2], parts[i * 2 + 1].Replace("*.", "").Split(';'));
        }
        StandaloneFileBrowser.SaveFilePanelAsync(title, "", defaultFileName, ext, f =>
        {
            if (f != null)
                feedback(f);
        });
    }
}