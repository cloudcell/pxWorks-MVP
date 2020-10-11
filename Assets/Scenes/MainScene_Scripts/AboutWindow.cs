// Copyright (c) 2020 Cloudcell Limited

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using CometUI;

namespace MainScene_UI
{
    partial class AboutWindow : BaseView
    {
        private void Start()
        {
            //subscribe buttons or events here
        }
        
        protected override void OnBuild(bool isFirstBuild)
        {
            //copy data to UI controls here
            //Set(txTitle, default);
            Set(txVersion, "Version: " + Application.version);
        }
    }
}