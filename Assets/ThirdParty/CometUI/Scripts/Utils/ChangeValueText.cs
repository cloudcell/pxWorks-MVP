using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CometUI
{
    public class ChangeValueText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IPointerDownHandler, IEndDragHandler
    {
        [SerializeField] InputField InputField;
        [SerializeField] bool integerOnly;
        [SerializeField] float min = 0;
        [SerializeField] float max = 10;
        [SerializeField] float step = 1;
        [SerializeField] string format = "0.0";
        int sensitivity = 10;
        [SerializeField] Texture2D cursor;
        [SerializeField] Vector2 hotSpotMouse = Vector2.zero;

        int counter = 0;
        float startMousePos;

        public void OnDrag(PointerEventData eventData)
        {
            var d = (startMousePos - eventData.position.x);
            if (Mathf.Abs(d) < sensitivity)
                return;

            startMousePos = eventData.position.x;
            var text = InputField.text.Replace(",", ".");
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
            {
                val = min;
            }
            else
            {
                val += step * Mathf.Sign(eventData.delta.x);
            }

            if (integerOnly)
                val = Mathf.Round(val);

            if (val < min) val = min;
            if (val > max) val = max;

            var res = val.ToString(format);
            InputField.text = res;
            InputField.onEndEdit.Invoke(res);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isHover)
                Cursor.SetCursor(null, hotSpotMouse, CursorMode.Auto);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            startMousePos = eventData.position.x;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHover = true;
            Cursor.SetCursor(cursor, hotSpotMouse, CursorMode.Auto);
        }

        bool isHover;

        public void OnPointerExit(PointerEventData eventData)
        {
            isHover = false;
            if (eventData.dragging)
                return;

            Cursor.SetCursor(null, hotSpotMouse, CursorMode.Auto);
        }
    }
}