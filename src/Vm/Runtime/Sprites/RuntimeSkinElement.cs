// RuntimeSkinElement.cs

using PaintPower.Display.DisplayIntegration;
using PaintPower.Tools.Graphics;

namespace PaintPower.Vm.Runtime.Sprites
{
    public class RuntimeSkinElement : DIItem
    {
        public string InstanceName { get; set; } = "";
        public int ZIndex { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
    }
}
