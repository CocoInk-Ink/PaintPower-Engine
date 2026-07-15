using System.Collections.Generic;

namespace PaintPower.Vm.Runtime.Sprites
{
    public class RuntimeSkin
    {
        public string Name { get; set; } = "";
        public string ScriptPath { get; set; } = "";

        public List<RuntimeSkinElement> Elements { get; set; } = new();
    }
}
