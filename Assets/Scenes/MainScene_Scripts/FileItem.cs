// Copyright (c) 2020 Cloudcell Limited

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
            Subscribe(bt, () => { if (isBlock) AddNode(); else OpenFolder(); });
        }

        private void OpenFolder()
        {
            var pn = GetComponentInParent<LibraryPanel>();;

            if (pn.openedFolders.Contains(dir))
                pn.openedFolders.Remove(dir);
            else
                pn.openedFolders.Add(dir);

            pn.AdjustVisibility();
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

        public bool isBlock;
        public string FullPath => dir;
        public FileItem Parent;

        protected override void OnBuild(bool isFirstBuild)
        {
            isBlock = File.Exists(Path.Combine(dir, UserSettings.Instance.RunMetaFileName));

            //Data: string dir, int level
            //copy data to UI controls here
            Set(icon, isBlock ? ScriptIcon : FolderIcon);
            Set(icon, isBlock ? new Color32(173, 173, 173, 255) : new Color32(200, 200, 0, 255));
            Set(text, Path.GetFileName(dir));
            //bt.interactable = isBlock;

            var hlg = GetComponentInChildren<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(15 * level, 0, 0, 0);
        }
    }
}