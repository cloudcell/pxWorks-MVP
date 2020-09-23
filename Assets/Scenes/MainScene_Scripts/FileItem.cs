using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using CometUI;
using System.IO;
using uGraph;

namespace MainScene_UI
{
    partial class FileItem : BaseView
    {
        private void Start()
        {
            //subscribe buttons or events here
            Subscribe(bt, AddNode);
        }

        private void AddNode()
        {
            if (!Graph.Instance.DirectoryIsDefined)
            {
                UIManager.ShowDialog(null, "Project directory does not exist or is not defined." + Environment.NewLine + "Create new project.", "Ok");
                return;
            }
            GraphHelper.AddNode(Graph.Instance, dir);
        }

        [SerializeField] Sprite FolderIcon;
        [SerializeField] Sprite ScriptIcon;

        protected override void OnBuild(bool isFirstBuild)
        {
            var isBlock = File.Exists(Path.Combine(dir, UserSettings.Instance.RunMetaFileName));

            //Data: string dir, int level
            //copy data to UI controls here
            Set(icon, isBlock ? ScriptIcon : FolderIcon);
            Set(text, Path.GetFileName(dir));
            bt.interactable = isBlock;

            var hlg = GetComponentInChildren<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(15 * level, 0, 0, 0);
        }
    }
}