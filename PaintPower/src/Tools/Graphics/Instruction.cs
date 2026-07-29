using Avalonia.Media;

namespace PaintPower.Tools.Graphics
{
    public class GfxInstruction
    {
        public Color color { get; set; }
        
        public int xfrom { get; set; }
        public int yfrom { get; set; }

        public int xto { get; set; }
        public int yto { get; set; }

        public int thickness { get; set; }
    }
}