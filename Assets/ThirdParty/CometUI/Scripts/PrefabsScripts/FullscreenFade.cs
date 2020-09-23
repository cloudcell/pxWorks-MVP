using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using CometUI;

namespace CometUI
{
    public class FullscreenFade : BaseView
    {
        public bool OverrideSorting = false;

        private void Start()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas)
                canvas.overrideSorting = OverrideSorting;
        }

        protected override void OnDisable()
        {
            Destroy(gameObject);
            base.OnDisable();
        }
    }
}

