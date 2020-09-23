using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace uGraph
{
    public class DraggedOutputKnob : MonoBehaviour, IDraggable
    {
        public DragMode DragMode => DragMode.MoveAndCancel;

        public RectTransform RectTransform => transform as RectTransform;

        [SerializeField] UILineRenderer lineRendererPrefab;

        Graph graph;
        UILineRenderer lineRenderer;
        Vector2[] points = new Vector2[4];
        public bool IsInput;

        private void Start()
        {
            graph = GetComponentInParent<Graph>();
        }

        public void OnDragging()
        {
            const int d = 50;
            if (IsInput)
            {
                points[3] = new Vector2(0, 0);
                points[2] = new Vector2(-d, 0);
                points[1] = GetPos(transform.position) + new Vector2(d, 0);
                points[0] = GetPos(transform.position);
            }
            else
            {
                points[0] = new Vector2(0, 0);
                points[1] = new Vector2(d, 0);
                points[2] = GetPos(transform.position) - new Vector2(d, 0);
                points[3] = GetPos(transform.position);
            }

            lineRenderer.Points = points;
            lineRenderer.RelativeSize = true;
            lineRenderer.drivenExternally = true;
            //lineRenderer.color = InputKnob.GetLineColorForType(GetComponentInParent<OutputKnob>().Type);
        }

        Vector2 GetPos(Vector3 pos)
        {
            var p = (pos - lineRenderer.transform.position);
            p *= 1 / lineRenderer.transform.lossyScale.x;
            return p;
        }

        public void OnStartDrag()
        {
            lineRenderer = Instantiate(lineRendererPrefab);
            lineRenderer.transform.SetParent(graph.LinesHolder.transform);
            lineRenderer.transform.position = transform.position;
            lineRenderer.transform.localScale = new Vector3(1, 1, 1);
            points[3] = points[2] = points[1] = points[0] = new Vector2(0, 0);
            lineRenderer.Points = points;
        }

        public void OnStopDrag(IAcceptor acceptor)
        {
            if (lineRenderer)
                DestroyImmediate(lineRenderer.gameObject);
        }      
    }
}