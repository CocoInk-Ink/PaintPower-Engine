using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using PaintPower.Accessibility.Translation;

namespace PaintPower.Editors;

public partial class Editor : UserControl
{
    public virtual Task Save() => Task.CompletedTask;
    public virtual Task SaveAs() => Task.CompletedTask;
    public virtual Task Cut() => Task.CompletedTask;
    public virtual Task Copy() => Task.CompletedTask;
    public virtual Task Paste() => Task.CompletedTask;
    public virtual Task Undo() => Task.CompletedTask;
    public virtual Task Redo() => Task.CompletedTask;

    public virtual void SetUIMode(EditorUIMode mode) { }

    // Override this in each editor
    public virtual HeaderDefinition GetHeaderDefinition()
    {
        return HeaderDefinition.Empty;
    }

    // Shared menus
    protected List<HeaderItem> StandardEditMenu() => new()
    {
        new HeaderItem { Label = "Undo", Command = () => Undo() },
        new HeaderItem { Label = "Redo", Command = () => Redo() },
        new HeaderItem { IsSeparator = true },
        new HeaderItem { Label = "Cut", Command = () => Cut() },
        new HeaderItem { Label = "Copy", Command = () => Copy() },
        new HeaderItem { Label = "Paste", Command = () => Paste() }
    };

    protected List<HeaderItem> StandardHelpMenu() => new()
    {
        new HeaderItem { Label = "About" },
        new HeaderItem { Label = "Documentation" },
        new HeaderItem { Label = "Videos" },
        new HeaderItem { Label = "Tutorials" }
    };

    protected List<HeaderItem> LanguageMenu()
    {
        var list = new List<HeaderItem>();

        foreach (var kv in Translator.GetAvailableLanguages())
        {
            string fullName = kv.Key;
            string shortCode = kv.Value;

            list.Add(new HeaderItem
            {
                Label = fullName,
                Command = () =>
                {
                    Translator.changeLang(shortCode);
                    Translator.refreshNeeded = true;
                    Translator.refresh();
                }
            });
        }

        return list;
    }
}

public class HeaderDefinition
{
    public Dictionary<string, List<HeaderItem>> Menus { get; set; } = new();

    public static HeaderDefinition Empty => new HeaderDefinition();
}

public class HeaderItem
{
    public string Label { get; set; }
    public Action? Command { get; set; }
    public bool IsSeparator { get; set; }
}
