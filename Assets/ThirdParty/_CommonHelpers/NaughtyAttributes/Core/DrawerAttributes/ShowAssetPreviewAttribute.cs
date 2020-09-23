using System;

namespace NaughtyAttributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ShowAssetPreviewAttribute : DrawerAttribute
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool DrawPropertyField { get; private set; }
        

        public ShowAssetPreviewAttribute(int width = 64, int height = 64, bool drawPropertyField = true)
        {
            this.Width = width;
            this.Height = height;
            this.DrawPropertyField = drawPropertyField;
        }
    }
}
