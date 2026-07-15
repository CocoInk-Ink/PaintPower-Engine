using PaintPower.Tools.Graphics;

namespace PaintPower.Vm.Runtime.Sprites
{
    public abstract class RuntimeSkinElement
    {
        public string InstanceName { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Rotation { get; set; }
        public double ScaleX { get; set; } = 1;
        public double ScaleY { get; set; } = 1;
        public int ZIndex { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
    }
}
