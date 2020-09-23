using System;

namespace NaughtyAttributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FoldoutAttribute : GroupAttribute
    {
        public FoldoutAttribute(string name)
            : base(name)
        {
        }
    }
}
