using System;

namespace PaintPower.ProjectSystem
{
    public abstract class SkinElement
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string InstanceName { get; set; } = "";
        public SkinTransform Transform { get; set; } = new();
        public int ZIndex { get; set; } = 0;
        public virtual string AssetPath { get; set; } = ""; // relative to items/
    }
}