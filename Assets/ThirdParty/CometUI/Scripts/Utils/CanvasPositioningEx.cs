using UnityEngine;

/// <summary>
/// Small helper class to convert viewport, screen or world positions to canvas space.
/// Only works with screen space canvases.
/// Usage:
/// objectOnCanvasRectTransform.anchoredPosition = specificCanvas.WorldToCanvasPoint(worldspaceTransform.position);
/// </summary>
public static class CanvasPositioningEx
{
    public static Vector3 WorldToCanvasPosition(this Canvas canvas, Vector3 worldPosition, Camera camera = null)
    {
        if (camera == null)
        {
            camera = Camera.main;
        }
        var viewportPosition = camera.WorldToViewportPoint(worldPosition);
        return canvas.ViewportToCanvasPosition(viewportPosition);
    }

    public static Vector3 ScreenToCanvasPosition(this Canvas canvas, Vector3 screenPosition)
    {
        var viewportPosition = new Vector3(screenPosition.x / Screen.width,
                                            screenPosition.y / Screen.height,
                                            0);
        return canvas.ViewportToCanvasPosition(viewportPosition);
    }

    public static Vector3 ViewportToCanvasPosition(this Canvas canvas, Vector3 viewportPosition)
    {
        var centerBasedViewPortPosition = viewportPosition - new Vector3(0.5f, 0.5f, 0);
        var canvasRect = canvas.GetComponent<RectTransform>();
        var scale = canvasRect.sizeDelta;
        return Vector3.Scale(centerBasedViewPortPosition, scale);
    }

    public static Rect RectTransformToScreenSpace(this RectTransform transform)
    {
        Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
        return new Rect((Vector2)transform.position - (size * 0.5f), size);
    }

    public static Rect RectTransformToViewport(this RectTransform transform)
    {
        var res = transform.RectTransformToScreenSpace();
        var scale = new Vector2(1f / Screen.width, 1f / Screen.height);
        return new Rect(Vector2.Scale(res.position, scale), Vector2.Scale(res.size, scale));
    }

    private static Vector3[] WorldCorners = new Vector3[4];
    public static Bounds GetRectTransformBounds(this RectTransform transform)
    {
        transform.GetWorldCorners(WorldCorners);
        Bounds bounds = new Bounds(WorldCorners[0], Vector3.zero);
        for (int i = 1; i < 4; ++i)
        {
            bounds.Encapsulate(WorldCorners[i]);
        }
        return bounds;
    }
}