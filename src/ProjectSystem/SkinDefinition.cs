using System.Collections.Generic;

namespace PaintPower.ProjectSystem
{
    public class SkinDefinition
    {
        public string Name { get; set; } = "";
        public string ScriptPath { get; set; } = ""; // path inside items/

        public List<SkinElement> Elements { get; set; } = new();

        public override string ToString() => Name;
    }
}
