using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using CometUI;
using System.Linq;
using System.IO;
using System.Collections;

namespace MainScene_UI
{
    partial class LibraryPanel : BaseView
    {
        VerticalLayoutGroup vlg;
        bool needUpdateVlg;

        private void Start()
        {
            //subscribe buttons or events here
            Subscribe(btRefresh, Rebuild);
            vlg = GetComponentInChildren<VerticalLayoutGroup>();

            Build();

            Bus.LibraryChanged.Subscribe(this, Rebuild);
        }

        protected override void OnBuild(bool isFirstBuild)
        {
            StartCoroutine(OnBuildAsync(isFirstBuild));
        }

        private IEnumerator OnBuildAsync(bool isFirstBuild)
        {
            DestroyDynamicallyCreatedChildren();

            vlg.enabled = false;

            //get available scripts
            if (Directory.Exists(UserSettings.Instance.LibraryPath))
                foreach (var sub in Directory.GetDirectories(UserSettings.Instance.LibraryPath))
                    Build(sub, 0);

            yield return new WaitForSeconds(0.5f);

            vlg.enabled = true;
        }

        private void Build(string dir, int padding)
        {
            //is block?
            if (File.Exists(Path.Combine(dir, UserSettings.Instance.RunMetaFileName)))
            {
                //build block
                var fi = Instantiate(FileItem);
                fi.Build(dir, padding);
                fi.Show(this);
            }
            else
            {
                //build folder item
                var fi = Instantiate(FileItem);
                fi.Build(dir, padding);
                fi.Show(this);
                //build subfolders
                foreach (var sub in Directory.GetDirectories(dir))
                    Build(sub, padding + 1);
            };
        }

        private void DestroyDynamicallyCreatedChildren()
        {
            GetComponentsInChildren<FileItem>().Where(f => f.IsDynamicallyCreated).ToList().ForEach(f => DestroyImmediate(f.gameObject));
        }
    }
}