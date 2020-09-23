using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace uGraph
{
    public class LineController : MonoBehaviour
    {
        [SerializeField] Color SelectedColor = new Color(0, 1, 0);
        Graph graph;
        UILineRenderer lineRenderer;
        Color prevColor;
        private bool lineIsSelected;

        public bool LineIsSelected
        {
            get => lineIsSelected;
            set
            {
                if (lineIsSelected == value)
                    return;
                if (value)
                {
                    prevColor = lineRenderer.color;
                    lineRenderer.color = SelectedColor;
                }else
                {
                    lineRenderer.color = prevColor;
                }

                lineIsSelected = value;
            }
        }

        private void Start()
        {
            graph = GetComponentInParent<Graph>();
            lineRenderer = GetComponent<UILineRenderer>();
        }

        public void OnClick()
        {
            LineIsSelected = true;
        }
    }
}