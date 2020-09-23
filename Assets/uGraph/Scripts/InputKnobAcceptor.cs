using UnityEngine;

namespace uGraph
{
    public class InputKnobAcceptor : MonoBehaviour, IAcceptor
    {
        public DragMode DragMode => DragMode.MoveAndCancel;
        public RectTransform RectTransform => transform as RectTransform;

        public bool CanDropIn(IDraggable draggable)
        {
            var outDrag = draggable as DraggedOutputKnob;
            if (outDrag == null || outDrag.IsInput)
                return false;
            //var outKnob = outDrag.GetComponentInParent<OutputKnob>();
            //var inKnob = GetComponentInParent<InputKnob>();
            //return outKnob.Type == inKnob.Type;                
            return true;
        }

        public void DropIn(IDraggable view)
        {
            var knob = (view as MonoBehaviour).GetComponentInParent<OutputKnob>();
            using (var comm = new StateCommand("Add connection"))
                GetComponentInParent<InputKnob>().SetInputConnection(knob);
        }
    }

    public enum KnobType
    {
        data, signal
    }
}
