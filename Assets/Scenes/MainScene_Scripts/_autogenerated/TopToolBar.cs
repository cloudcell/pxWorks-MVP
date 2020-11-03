/////////////////////////////////////////
//     THIS IS AUTOGENERATED CODE      //
//       do not change directly        //
/////////////////////////////////////////
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using CometUI;

namespace MainScene_UI
{
    partial class TopToolBar : BaseView //Autogenerated
    {
        /// <summary>Static instance of the view</summary>
        public static TopToolBar Instance { get; private set; }
        // Controls
        #pragma warning disable 0414
        //[Header("Controls (auto capture)")]
        [Header("Custom")]
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btNew = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btOpen = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btAddNode = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btCleanUp = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btCenter = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btUndo = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btRedo = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btSaveAs = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btPause = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btPlay = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Image imPlayIcon = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btDebug = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btOpenLogFolder = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btOpenGraphics = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btSettings = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btAbout = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btTest = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btWindowState = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btQuit = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btFullScreen = default;
        [AutoGenerated, SerializeField, HideInInspector] TMPro.TextMeshProUGUI lbStatus = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Text txProjectLink = default;
        [AutoGenerated, SerializeField, HideInInspector] AddNodeWindow AddNodeWindow = default;
        [AutoGenerated, SerializeField, HideInInspector] AboutWindow AboutWindow = default;
        [AutoGenerated, SerializeField, HideInInspector] SetingsWindow SetingsWindow = default;
        [AutoGenerated, SerializeField, HideInInspector] ConsolePanel ConsolePanel = default;
        #pragma warning restore 0414
        
        public override void AutoSubscribe()
        {
            SubscribeOnChanged(btNew);
            SubscribeOnChanged(btOpen);
            SubscribeOnChanged(btAddNode);
            SubscribeOnChanged(btCleanUp);
            SubscribeOnChanged(btCenter);
            SubscribeOnChanged(btUndo);
            SubscribeOnChanged(btRedo);
            SubscribeOnChanged(btSaveAs);
            SubscribeOnChanged(btPause);
            SubscribeOnChanged(btPlay);
            SubscribeOnChanged(imPlayIcon);
            SubscribeOnChanged(btDebug);
            SubscribeOnChanged(btOpenLogFolder);
            SubscribeOnChanged(btOpenGraphics);
            SubscribeOnChanged(btSettings);
            SubscribeOnChanged(btAbout);
            SubscribeOnChanged(btTest);
            SubscribeOnChanged(btWindowState);
            SubscribeOnChanged(btQuit);
            SubscribeOnChanged(btFullScreen);
            SubscribeOnChanged(lbStatus);
            SubscribeOnChanged(txProjectLink);
            SubscribeOnChanged(AddNodeWindow);
            SubscribeOnChanged(AboutWindow);
            SubscribeOnChanged(SetingsWindow);
            SubscribeOnChanged(ConsolePanel);
            Subscribe(btQuit, () => UIManager.CloseApp());
            Subscribe(btAbout, () => AboutWindow.BuildAndShow(OwnerForChild));
            Subscribe(btSettings, () => SetingsWindow.BuildAndShow(OwnerForChild));
            Subscribe(btDebug, () => ConsolePanel.ShowOrClose(OwnerForChild));
        }
        
        [VisibleInGraph(false)]
        public void Build()
        {
            OnBuildSafe(true);
        }
    }
}