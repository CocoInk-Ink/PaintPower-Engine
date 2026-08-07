// RuntimeSkinElement.cs

using Toolbox.Display.DisplayIntegration;
using Toolbox.Graphics;

namespace Toolbox.Display.Sprites
{
    public class RuntimeSkinElement : DIItem
    {
        public string InstanceName { get; set; } = "";
        public int ZIndex { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
    }
}
