using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaintPower.Editors;

// Base class for editors like the Paint editor or the Script editor
public partial class EditorBase : UserControl
{

    public virtual void Save() { }

    public virtual void Load() { }

    public virtual void Undo() { }

    public virtual void Redo() { }

    public virtual void New() { }

    public virtual void Open() { }

    public virtual void Close() { }

    public virtual void Copy() { }
    public virtual void Cut() { }
    public virtual void Paste() { }

    public virtual void SelectAll() { }
    public virtual void DeselectAll() { }

    public virtual void Delete() { }

    public virtual void Import() { }
    public virtual void Export() { }

    public virtual void Refresh() { }

    public string RelativePath { get; private set; } = "";

    public virtual void SetRelativePath(string path)
    {
        RelativePath = path;
    }

    public EditorBase addText(TextBlock t) {
        Content = t;
        return this;
    }

    public string type = "";

    public virtual void TranslateGUI() { }
}
