using UnityEngine;

namespace uGraph
{
    public interface IDraggable
    {
        DragMode DragMode { get; }
        RectTransform RectTransform { get; }
        void OnStopDrag(IAcceptor acceptor);
        void OnStartDrag();
        void OnDragging();
    }

    public enum DragMode
    {
        Move, MoveAndCancel
    }
}
