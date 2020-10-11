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
    partial class AddNodeWindow : BaseView
    {
        [SerializeField] Button buttonPrefab;

        private void Start()
        {
            //subscribe buttons or events here
        }
        
        protected override void OnBuild(bool isFirstBuild)
        {
            //copy data to UI controls here

            if(isFirstBuild)
            {
                Debug.Log(Directory.GetCurrentDirectory());
                Helper.DestroyAllChildrenImmediate(pnSockets.gameObject);

                //get available scripts
                if (Directory.Exists(UserSettings.Instance.LibraryPath))
                foreach(var folder in Directory.GetDirectories(UserSettings.Instance.LibraryPath))
                {
                    var bt = Instantiate(buttonPrefab, pnSockets.transform);
                    bt.onClick.AddListener(() => AddNode(folder));
                    bt.GetComponentInChildren<Text>().text = Path.GetFileName(folder);
                }
            }
        }

        private void AddNode(string folder)
        {
            GraphHelper.AddNode(Graph.Instance, folder);

            Close();
        }
    }
}