// Copyright (c) 2020 Cloudcell Limited

using CometUI;
using UnityEngine;
using UnityEngine.EventSystems;

public class SplitterController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] RectTransform[] IncreaseTargets;
    [SerializeField] RectTransform[] DecreaseTargets;
    [SerializeField] Texture2D cursor;
    [SerializeField] Vector2 hotSpotMouse = new Vector2(32, 32);
    [SerializeField] Vector2 MinSize = new Vector2(20, 20);
    [SerializeField] Vector2 MaxSize = new Vector2(1000, 1000);
    [SerializeField] bool IsHorizontal;

    Vector3[] startSizes;
    bool isDragging;
    Vector3 startCursorPos;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (UIManager.FullScreenFadeStack.Count > 0)
            return;

        isDragging = true;
        startCursorPos = Input.mousePosition;

        startSizes = new Vector3[IncreaseTargets.Length + DecreaseTargets.Length];
        for (int i=0;i<IncreaseTargets.Length;i++)
            startSizes[i] = IncreaseTargets[i].sizeDelta;
        for (int i = 0; i < DecreaseTargets.Length; i++)
            startSizes[i + IncreaseTargets.Length] = DecreaseTargets[i].sizeDelta;

        Cursor.SetCursor(cursor, hotSpotMouse, CursorMode.Auto);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            var delta = Input.mousePosition - startCursorPos;
            if (IsHorizontal)
                delta.x = 0;
            else
                delta.y = 0;
            //if (Target)
            //{
            //    var scaler = GetComponentInParent<CanvasScaler>();
            //    if (scaler != null)
            //        delta.x *= Screen.width / scaler.referenceResolution.x;
            //}

            for (int i = 0; i < IncreaseTargets.Length; i++)
            {
                var newSize = startSizes[i] + delta;

                if (IsHorizontal)
                {
                    if (newSize.y < MinSize.y) delta.y -= newSize.y - MinSize.y;
                    if (newSize.y > MaxSize.y) delta.y -= newSize.y - MaxSize.y;
                }
                else
                {
                    if (newSize.x < MinSize.x) delta.x -= newSize.x - MinSize.x;
                    if (newSize.x > MaxSize.x) delta.x -= newSize.x - MaxSize.x;
                }

                IncreaseTargets[i].sizeDelta = startSizes[i] + delta;
            }

            for (int i = 0; i < DecreaseTargets.Length; i++)
            {
                var newSize = startSizes[i + IncreaseTargets.Length] - delta;
                DecreaseTargets[i].sizeDelta = newSize;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        Cursor.SetCursor(null, hotSpotMouse, CursorMode.Auto);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging)
            Cursor.SetCursor(cursor, hotSpotMouse, CursorMode.Auto);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging)
            Cursor.SetCursor(null, hotSpotMouse, CursorMode.Auto);
    }
}
