using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace CometUI
{
    /// <summary>Provides Drag&Drop functionality</summary>
    internal class Dragger
    {
        #region Public

        /// <summary>User is dragging now</summary>
        public bool IsDragging => dragInfo != null;

        public void OnDragging(Vector3 delta)
        {
            dragInfo.View.RectTransform.anchoredPosition = dragInfo.Canvas.ScreenToCanvasPosition(Input.mousePosition);
            dragInfo.CanvasGroup.alpha = GetAcceptor() == null ? 0.5f : 1f;
        }

        public void OnDragEnd(Vector2 pos)
        {
            if (dragInfo == null)
                return;

            //
            foreach (var comp in dragInfo.DisabledComponents)
                comp.enabled = true;
            if (dragInfo.CanvasGroup != null)
                GameObject.Destroy(dragInfo.CanvasGroup);

            //
            BaseView acceptor = GetAcceptor();

            if (acceptor != null)
            {
                //remove from prev owner
                if (dragInfo.DragMode == DragMode.Move)
                {
                    if (dragInfo.View.Owner != null)
                    {
                        dragInfo.View.Owner.DropOut(dragInfo.View, acceptor);
                        dragInfo.View.Owner.OpenedChildren.Remove(dragInfo.View);
                    }

                    dragInfo.View.Owner = null;
                }

                //move to new owner
                acceptor.DropIn(dragInfo.View);

                //call Dropped event
                dragInfo.View.OnDropped(acceptor);
            }
            else
            {
                CancelDragDrop();
            }

            dragInfo = null;
        }

        public void OnDragStart(Vector2 pos, Component lastTouchedUI)
        {
            //find touched BaseView
            var ui = lastTouchedUI;
            if (ui == null) return;

            var view = ui.GetComponentsInParent<BaseView>().FirstOrDefault(v => v.VisibleState != VisibleState.Closed);
            if (view == null) return;

            //is there Draggable views?
            while (view != null)
            {
                if (view.DragMode != DragMode.None)
                {
                    OnDragStart(pos, view);
                    break;
                }
                view = view.Owner;
            }
        }

        #endregion

        #region Private

        DragInfo dragInfo;

        class DragInfo
        {
            public BaseView View;
            public RectTransform Parent;
            public CanvasGroup CanvasGroup;
            public Canvas Canvas;
            public List<Behaviour> DisabledComponents = new List<Behaviour>();
            public DragMode DragMode;
            public int sourceSiblingIndex;
            public Vector2 anchoredPosition;
            public Vector2 sourceSizeDelta;
            public Vector2 sourceAnchorsMax;
            public Vector2 sourceAnchorsMin;
        }

        private BaseView GetAcceptor()
        {
            foreach (var v in UIManager.GetViewsUnderMouse())
                if (!dragInfo.View.GetMeAndMyOwners().Any(o => o == v))
                    if (v.CanDropIn(dragInfo.View))
                        return v;

            return null;
        }

        private void CancelDragDrop()
        {
            if (dragInfo.DragMode == DragMode.Copy)
            {
                GameObject.Destroy(dragInfo.View.gameObject);
            }
            else
            {
                dragInfo.View.transform.SetParent(dragInfo.Parent, false);
                dragInfo.View.transform.SetSiblingIndex(dragInfo.sourceSiblingIndex);
                dragInfo.View.RectTransform.anchorMax = dragInfo.sourceAnchorsMax;
                dragInfo.View.RectTransform.anchorMin = dragInfo.sourceAnchorsMin;
                dragInfo.View.RectTransform.anchoredPosition = dragInfo.anchoredPosition;
                dragInfo.View.RectTransform.sizeDelta = dragInfo.sourceSizeDelta;
            }
        }

        private void OnDragStart(Vector2 pos, BaseView source)
        {
            source.OnStartDrag();
            source.CloseAllChildren();

            //create draggable view
            var parent = source.transform.parent as RectTransform;
            dragInfo = new DragInfo();
            dragInfo.Parent = parent;
            dragInfo.DragMode = source.DragMode;
            dragInfo.sourceSiblingIndex = source.transform.GetSiblingIndex();
            dragInfo.sourceAnchorsMax = source.RectTransform.anchorMax;
            dragInfo.sourceAnchorsMin = source.RectTransform.anchorMin;
            dragInfo.anchoredPosition = source.RectTransform.anchoredPosition;
            dragInfo.sourceSizeDelta = source.RectTransform.sizeDelta;
            //get canvas
            dragInfo.Canvas = source.GetComponentsInParent<Canvas>().LastOrDefault();

            //clone source
            BaseView view = null;
            if (dragInfo.DragMode == DragMode.Copy)
            {
                //copy mode
                view = dragInfo.View = source.Clone();
                view.transform.SetParent(dragInfo.Canvas.transform, false);
                (view as IBaseViewInternal).SetVisibleState(VisibleState.Visible);
                view.Owner = null;
            }
            else
            {
                //move mode
                view = dragInfo.View = source;
                view.transform.SetParent(dragInfo.Canvas.transform, false);
            }

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
            if (!view.GetComponent<CanvasGroup>())
            {
                dragInfo.CanvasGroup = view.gameObject.AddComponent<CanvasGroup>();
                dragInfo.CanvasGroup.alpha = 0.5f;
            }

            //set pos
            view.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            view.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            view.RectTransform.anchoredPosition = dragInfo.Canvas.ScreenToCanvasPosition(Input.mousePosition);
            view.RectTransform.sizeDelta = size;
        }

        #endregion
    }
}