using System.Collections.Generic;

namespace PaintPower.ProjectSystem.SpriteEditor
{

    public class AssetNode
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public bool IsDirectory { get; set; }
        public List<AssetNode> Children { get; set; } = new();
    }

    public class AssetEntry
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public bool IsDirectory { get; set; }
        public int Depth { get; set; } = 0;

        public override string ToString()
        {
            string indent = new string(' ', Depth * 4);
            string icon = IsDirectory ? "📁 " : "🖼 ";
            return indent + icon + Name;
        }
    }

}