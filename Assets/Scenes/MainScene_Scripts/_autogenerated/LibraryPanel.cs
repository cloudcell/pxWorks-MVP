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
    partial class LibraryPanel : BaseView //Autogenerated
    {
        /// <summary>Static instance of the view</summary>
        public static LibraryPanel Instance { get; private set; }
        // Controls
        #pragma warning disable 0414
        //[Header("Controls (auto capture)")]
        [Header("Custom")]
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btRefresh = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.UI.Button btOpen = default;
        [AutoGenerated, SerializeField, HideInInspector] UnityEngine.RectTransform pnContent = default;
        [AutoGenerated, SerializeField, HideInInspector] FileItem FileItem = default;
        #pragma warning restore 0414
        
        public override void AutoSubscribe()
        {
            SubscribeOnChanged(btRefresh);
            SubscribeOnChanged(btOpen);
            SubscribeOnChanged(pnContent);
            SubscribeOnChanged(FileItem);
        }
        
        [VisibleInGraph(false)]
        public void Build()
        {
            OnBuildSafe(true);
        }
    }
}