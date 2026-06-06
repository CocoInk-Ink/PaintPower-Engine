using System.Collections.Generic;

namespace PaintPower.Sprites
{
    public class RuntimeSkin
    {
        public string Name { get; set; } = "";
        public string ScriptPath { get; set; } = "";

        public List<RuntimeSkinElement> Elements { get; set; } = new();
    }
}
