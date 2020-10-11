// Copyright (c) 2020 Cloudcell Limited

using CometUI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace uGraph
{
    /// <summary>Provides Drag&Drop functionality</summary>
    internal class Dragger : MonoBehaviour
    {
        [SerializeField] float TransparencyOnDragging = 0.5f;
        private void Start()
        {
            SimpleGestures.OnPan += (v) => { OnDragging(v); };
            SimpleGestures.OnDragStart += (v) => OnDragStart(v);
            SimpleGestures.OnDragEnd += OnDragEnd;
        }

        #region Public

        /// <summary>User is dragging now</summary>
        public bool IsDragging => dragInfo != null;

        public void OnDragging(Vector3 delta)
        {
            if (!IsDragging)
                return;
            var mousePoint = Input.mousePosition; 
            dragInfo.View.RectTransform.position = mousePoint - dragInfo.delta;
            dragInfo.View.OnDragging();
            dragInfo.CanvasGroup.alpha = GetAcceptor() == null ? TransparencyOnDragging : 1f;
        }

        public void OnDragEnd(Vector2 pos)
        {
            if (dragInfo == null)
                return;

            //
            foreach (var comp in dragInfo.DisabledComponents)
                comp.enabled = true;
            if (dragInfo.CanvasGroup != null)
                GameObject.DestroyImmediate(dragInfo.CanvasGroup);

            //
            var acceptor = GetAcceptor();

            //call Dropped event
            dragInfo.View.OnStopDrag(acceptor);

            if (acceptor != null && acceptor.CanDropIn(dragInfo.View))
            {
                //drop in acceptor
                acceptor.DropIn(dragInfo.View);
            }

            //
            if (dragInfo.DragMode == DragMode.MoveAndCancel)
                CancelDragDrop();

            dragInfo = null;
        }

        public void OnDragStart(Vector2 pos)
        {
            if (UIManager.FullScreenFadeStack.Count > 0)
                return;

            var obj = SimpleGestures.GetUIObjectsUnderPosition(SimpleGestures.Instance.LastTouchedMousePos).FirstOrDefault(o=>o.gameObject.GetComponent<IDraggable>() != null);
            if (obj.gameObject == null)
                return;
            var view = obj.gameObject.GetComponent<IDraggable>();
            if (view == null) return;

            //start drag
            OnDragStartRaw(pos, view);
        }

        #endregion

        #region Private

        DragInfo dragInfo;

        class DragInfo
        {
            public IDraggable View;
            public CanvasGroup CanvasGroup;
            public List<Behaviour> DisabledComponents = new List<Behaviour>();
            public DragMode DragMode;
            public Vector2 anchoredPosition;
            public Vector3 delta;
        }

        private IAcceptor GetAcceptor()
        {
            var uis = SimpleGestures.GetUIObjectsUnderPosition(Input.mousePosition).Select(r => r.gameObject.GetComponent<IAcceptor>()).Where(c => c != null);
            foreach (var v in uis)
            if (v.CanDropIn(dragInfo.View))
                return v;

            return null;
        }

        private void CancelDragDrop()
        {
            dragInfo.View.RectTransform.anchoredPosition = dragInfo.anchoredPosition;
        }

        private void OnDragStartRaw(Vector2 pos, IDraggable source)
        {
            //create draggable view
            var parent = source.RectTransform.parent as RectTransform;
            dragInfo = new DragInfo();
            dragInfo.DragMode = source.DragMode;
            dragInfo.anchoredPosition = source.RectTransform.anchoredPosition;
            dragInfo.delta = Input.mousePosition - source.RectTransform.position;
            if (dragInfo.DragMode == DragMode.MoveAndCancel)
                dragInfo.delta = Vector3.zero;


            //move mode
            IDraggable view = null;
            view = dragInfo.View = source;

            //disable layouts (vert/horiz)
            var layout = parent.GetComponent<LayoutGroup>();
            if (layout)
            {
                dragInfo.DisabledComponents.Add(layout);
                layout.enabled = false;
            }

            //disable fitter
            var fitter = parent.GetComponent<ContentSizeFitter>();
            if (fitter)
            {
                dragInfo.DisabledComponents.Add(fitter);
                fitter.enabled = false;
            }

            //disable scrolls
            var scroll = parent.GetComponentInParent<ScrollRect>();
            if (scroll != null)
            {
                dragInfo.DisabledComponents.Add(scroll);
                scroll.enabled = false;
            }

            //adjust size
            var size = view.RectTransform.rect.size;
            var grid = parent.GetComponent<GridLayoutGroup>();
            if (grid)
            {
                size = grid.cellSize;
                dragInfo.DisabledComponents.Add(grid);
                grid.enabled = false;
            }
            else
            if (size.x == 0)
                size.x = parent.rect.width;
            else
            if (size.y == 0)
                size.y = parent.rect.height;

            //add transparency
            if (!(view as MonoBehaviour).GetComponent<CanvasGroup>())
            {
                dragInfo.CanvasGroup = (view as MonoBehaviour).gameObject.AddComponent<CanvasGroup>();
                dragInfo.CanvasGroup.alpha = TransparencyOnDragging;
            }

            //callback
            dragInfo.View.OnStartDrag();
        }

        #endregion
    }
}
