// Copyright (c) 2020 Cloudcell Limited

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using CometUI;
using System.Linq;
using System.IO;
using System.Collections;
using System.Diagnostics;

namespace MainScene_UI
{
    partial class LibraryPanel : BaseView
    {
        VerticalLayoutGroup vlg;
        bool needUpdateVlg;

        public HashSet<string> openedFolders = new HashSet<string>();

        private void Start()
        {
            if (!Bus.EULA.Value)
                Close();

            //subscribe buttons or events here
            Subscribe(btRefresh, Rebuild);
            Subscribe(btOpen, OpenInBrowser);
            vlg = GetComponentInChildren<VerticalLayoutGroup>();

            Build();

            Bus.LibraryChanged.Subscribe(this, Rebuild);
            Bus.EULA.Subscribe(this, (a) => { if (a) { Show(null, noAnimation: true); Rebuild(); } }).CallWhenInactive();
        }

        private void OpenInBrowser()
        {
            var path = UserSettings.Instance.LibraryPath;
            if (!Path.IsPathRooted(path))
                path = Path.Combine(Directory.GetCurrentDirectory(), path);
            if (Directory.Exists(path))
                Process.Start(path);
        }

        protected override void OnBuild(bool isFirstBuild)
        {
            DestroyDynamicallyCreatedChildren();

            //get available scripts
            if (Directory.Exists(UserSettings.Instance.LibraryPath))
                foreach (var sub in Directory.GetDirectories(UserSettings.Instance.LibraryPath))
                    Build(null, sub, 0);

            //Adjust visibility
            AdjustVisibility();
        }

        public void AdjustVisibility()
        {
            var items = pnContent.GetComponentsInChildren<FileItem>(true);
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.dir) || item.Parent == null)
                    continue;

                var parent = item.Parent;
                var visible = true;

                while (parent != null)
                {
                    if (!openedFolders.Contains(parent.dir))
                    {
                        visible = false;
                        break;
                    }
                    parent = parent.Parent;
                }

                item.gameObject.SetActive(visible);
            }
        }

        private void Build(FileItem parent, string dir, int padding)
        {
            //is block?
            if (File.Exists(Path.Combine(dir, UserSettings.Instance.RunMetaFileName)))
            {
                //build block
                var fi = Instantiate(FileItem);
                fi.Build(dir, padding);
                fi.Parent = parent;
                fi.Show(this, noAnimation: true);
            }
            else
            {
                //build folder item
                var fi = Instantiate(FileItem);
                fi.Build(dir, padding);
                fi.Parent = parent;
                fi.Show(this, noAnimation: true);
                //build subfolders
                foreach (var sub in Directory.GetDirectories(dir))
                    Build(fi, sub, padding + 1);
            };
        }

        private void DestroyDynamicallyCreatedChildren()
        {
            GetComponentsInChildren<FileItem>().Where(f => f.IsDynamicallyCreated).ToList().ForEach(f => DestroyImmediate(f.gameObject));
        }
    }
}