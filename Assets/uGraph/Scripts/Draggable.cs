using System;
using UnityEngine;

namespace uGraph
{
    public class Draggable : MonoBehaviour, IDraggable
    {
        [SerializeField] DragMode dragMode;

        public DragMode DragMode => dragMode;
        public RectTransform RectTransform => transform as RectTransform;

        public void OnDropped(IAcceptor acceptor)
        {
        }

        public void OnStartDrag()
        {
        }

        public void OnDragging()
        {
        }

        public void OnStopDrag(IAcceptor acceptor)
        {
        }
    }
}
