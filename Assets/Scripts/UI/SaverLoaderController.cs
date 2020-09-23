using CometUI;
using System;
using System.IO;
using System.Threading.Tasks;
using uGraph;
using UI;
using UnityEngine;

class SaverLoaderController : Singleton<SaverLoaderController>
{
    public async void Save(Graph graph, bool forcedAskFileName)
    {
        async Task Save(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            Bus.SetStatusLabel += "Saving " + path;
            await Await.NextUpdate();

            try
            {
                switch (Path.GetExtension(path).ToLower())
                {
                    case ".pxw":
                        GraphHelper.SaveGraph(path);
                        break;
                    case "": path += ".pxw"; goto case ".pxw";
                }
            }
            catch (Exception ex)
            {
                UIManager.ShowDialog(null, ex.Message, "Ok");
            }

            Bus.SetStatusLabel += "Completed.";
        }

        if (forcedAskFileName || string.IsNullOrWhiteSpace(Graph.Instance.SceneFilePath))
            OpenFileController.Instance.SaveFile("Save Graph", Graph.Instance.GetProposedNameOfScene(), "px Works Graph|*.pxw", async (path) => await Save(path));
        else
            await Save(Graph.Instance.SceneFilePath);
    }

    public void Load_old(Graph graph)
    {
        OpenFileController.Instance.OpenFile("Open Graph", "px Works Graph|*.pxw", (path) =>
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            try
            {
                switch (Path.GetExtension(path).ToLower())
                {
                    case ".pxw":
                        GraphHelper.LoadGraph(path);
                        break;
                }
            }
            catch (Exception ex)
            {
                UIManager.ShowDialog(null, ex.Message, "Ok");
                Debug.LogException(ex);
            }
        });
    }

    public void Load(Graph graph)
    {
        OpenFileController.Instance.OpenFolder("Select Graph Folder", "", (path) =>
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            var files = Directory.GetFiles(path, UserSettings.Instance.ProjectFile);
            try
            {
                if (files.Length == 0)
                    throw new Exception("File " + UserSettings.Instance.ProjectFile + " is not found");

                GraphHelper.LoadGraph(files[0]);

                //close graphics window
                GraphicsWindowController.Instance.CloseGraphicsWindowIfOpened();
            }
            catch (Exception ex)
            {
                UIManager.ShowDialog(null, ex.Message, "Ok");
                Debug.LogException(ex);
            }
        });
    }

    public void New(Graph graph)
    {
        void Open(string path)
        {
            try
            {
                GraphHelper.NewGraph();
                var pxwFile = Path.Combine(path, UserSettings.Instance.ProjectFile);
                GraphHelper.SaveGraph(pxwFile);

                SaverLoader.ClearSpecFolders(Path.GetDirectoryName(pxwFile));
                GraphicsWindowController.Instance.CloseGraphicsWindowIfOpened();
            }
            catch (Exception ex)
            {
                UIManager.ShowDialog(null, ex.Message, "Ok");
                Debug.LogException(ex);
            }
        }

        OpenFileController.Instance.OpenFolder("Select Graph Folder", "", (path) =>
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            var files = Directory.GetFiles(path, "*.*");
            var folders = Directory.GetDirectories(path, "*.*");
            if (files.Length != 0 || folders.Length != 0)
            {
                UIManager.ShowDialog(null, "Folder is not empty." + Environment.NewLine + "All files and subdirectories will be removed." + Environment.NewLine + "Are you sure to create new project here?", "Ok", "Cancel", onClosed: (res) =>
                {
                    Debug.Log(res);
                    if (res == DialogResult.Ok)
                        Open(path);
                }
                );
            }
            else
                Open(path);
        });
    }
}
