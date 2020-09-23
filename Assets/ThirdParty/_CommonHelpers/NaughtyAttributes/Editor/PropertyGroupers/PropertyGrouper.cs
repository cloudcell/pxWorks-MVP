using System;

namespace NaughtyAttributes.Editor
{
    public abstract class PropertyGrouper
    {
        public abstract bool IsVisible { get; }
        public abstract void BeginGroup(string label);
        public abstract void EndGroup();
    }
}
