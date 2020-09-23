using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

namespace CometUI
{
    public class Tooltip : MonoBehaviour
    {
        [Tooltip("Assign custom TooltipView here (instead of default)")]
        public TooltipView TooltipViewPrefab;
        [Tooltip("Target RectTransform")]
        public RectTransform Target;
        [TextArea]
        public string TextLeft;
        [TextArea]
        public string TextRight;

        internal TooltipView TooltipInstance;

        void Start()
        {
            UIManager.Instance?.RegisterTooltip(this);
        }

        private void OnDestroy()
        {
            UIManager.Instance?.UnRegisterTooltip(this);
        }
    }
}