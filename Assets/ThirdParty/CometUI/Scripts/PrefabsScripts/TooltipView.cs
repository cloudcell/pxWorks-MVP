using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using CometUI;
using TMPro;

namespace CometUI
{
    public class TooltipView : BaseView
    {
        [SerializeField] TextMeshProUGUI txLeft;
        [SerializeField] TextMeshProUGUI txRight;
        [SerializeField] GameObject pnMainFrame;
        [SerializeField] GameObject pnAltFrame;
        [Header("Tooltip")]
        [SerializeField] PlaceAppear Appearing = default;
        [SerializeField] bool KeepInScreen = false;
        public float TooltipDelay = 0.4f;
        public float MaxDuration = 100;
        [SerializeField] Vector2 MouseOffset = new Vector2(20, 20);

        ///<summary>Data</summary>
        public Tooltip tooltip { get; private set; }

        bool started;

        public override void GrabComponents()
        {
            //do nothing because we need assign fields in inspector
        }

        public override void AutoSubscribe()
        {
        }
       
        [VisibleInGraph(false)]
        public void Build(Tooltip tooltip)
        {
            this.tooltip = tooltip;
            OnBuildSafe(true);
        }
        
        public override BaseView Clone()
        {
            var clone = (TooltipView)base.Clone();
            clone.tooltip = tooltip;
            return clone;
        }

        private void Start()
        {
            started = true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (started && VisibleState != VisibleState.Hidden)
            {
                Destroy(gameObject);
                tooltip.TooltipInstance = null;
            }
        }

        protected override void OnBuild(bool isFirstBuild)
        {
            if (!isFirstBuild)
                return;

            //get BaseView
            var view = tooltip.GetComponentInParent<BaseView>();
            //try get tooltip in touched view and it's owners
            var info = new TooltipInfo(tooltip);
            while (view != null)
            {
                //call method of view
                view.OnTooltipRequired(info);
                if (info.IsHandled)
                    break;//tooltip is handled
                //go to owner
                view = view.Owner;
            }

            //if tooltips are not defined => get from tooltip script
            if (info.TextLeft == null)
                info.TextLeft = tooltip.TextLeft;
            if (info.TextRight == null)
                info.TextRight = tooltip.TextRight;

            //assign text
            txLeft.text = info.TextLeft;
            txRight.text = info.TextRight;

            //resize
            ResizeToMatchText();

            //get target rect
            var rt = tooltip.Target;
            if (rt == null)
                rt = tooltip.transform as RectTransform;

            var rect = GetRectTransformRect(rt);

            //place
            var flipped = false;

            if (Appearing == PlaceAppear.NearMouse)
            {
                var pos = Input.mousePosition;
                var size = GetSizeOfRectTransform(RectTransform);
                pos += new Vector3(size.x + MouseOffset.x * 2, -size.y - MouseOffset.y * 2, 0) / 2;
                Place(RectTransform, pos, KeepInScreen);
            }
            else
            {
                PlaceAround(RectTransform, rect, Appearing, KeepInScreen, false, ref flipped);
            }

            if (pnMainFrame)
                pnMainFrame.SetActive(!flipped);
            if (pnAltFrame)
                pnAltFrame.SetActive(flipped);
        }

        private void ResizeToMatchText()
        {
            // Find the biggest height between both text layers
            float biggestY = txLeft.preferredHeight;
            float rightY = txRight.preferredHeight;
            if (rightY > biggestY)
                biggestY = rightY;

            // Dont forget to add the margins
            //var margins = TextLeft.margin.y * 2;
            var margins = 0;

            // Also reduce width if text is small
            float widthLeft = Mathf.Clamp(txRight.GetPreferredValues().x + txLeft.GetPreferredValues().x + margins, 0, RectTransform.sizeDelta.x);
            float widthRight = Mathf.Clamp(txRight.GetPreferredValues().x + margins, 0, RectTransform.sizeDelta.x);
            float width = Mathf.Max(widthLeft, widthRight);
            //float width = RectTransform.sizeDelta.x;

            // Update the height of the tooltip panel
            RectTransform.sizeDelta = new Vector2(width, biggestY + margins);
        }
    }

    public class TooltipInfo
    {
        /// <summary>Touched UI</summary>
        public GameObject TouchedUI => Tooltip.gameObject;
        /// <summary>Tooltip</summary>
        public Tooltip Tooltip { get; }
        /// <summary>Set to True to prevent owner to process tooltip</summary>
        public bool IsHandled { get; set; }
        /// <summary>Assigned left text</summary>
        public string TextLeft { set; get; }
        /// <summary>Assigned right text</summary>
        public string TextRight { set; get; }

        public TooltipInfo(Tooltip tooltip)
        {
            Tooltip = tooltip;
        }
    }
}