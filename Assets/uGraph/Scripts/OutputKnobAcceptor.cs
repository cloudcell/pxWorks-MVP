// Copyright (c) 2020 Cloudcell Limited

using UnityEngine;

namespace uGraph
{
    public class OutputKnobAcceptor : MonoBehaviour, IAcceptor
    {
        public DragMode DragMode => DragMode.MoveAndCancel;
        public RectTransform RectTransform => transform as RectTransform;

        public bool CanDropIn(IDraggable draggable)
        {
            var outDrag = draggable as DraggedOutputKnob;
            if (outDrag == null || !outDrag.IsInput)
                return false;
            //var outKnob = outDrag.GetComponentInParent<OutputKnob>();
            //var inKnob = GetComponentInParent<InputKnob>();
            //return outKnob.Type == inKnob.Type;                
            return true;
        }

        public void DropIn(IDraggable view)
        {
            var knob = (view as MonoBehaviour).GetComponentInParent<InputKnob>();
            using (var comm = new StateCommand("Add connection"))
                knob.SetInputConnection(GetComponentInParent<OutputKnob>());
        }
    }
}
