using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using System.Collections.Generic;

namespace PaintPower.Editors.EditorTools.ScriptEditorTools;

public class CodeFoldingStrategy
{
    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        var newFoldings = CreateNewFoldings(document);
        manager.UpdateFoldings(newFoldings, -1);
    }

    private IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
    {
        var foldings = new List<NewFolding>();
        var stack = new Stack<int>();
        string text = document.Text;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '{')
            {
                stack.Push(i);
            }
            else if (c == '}' && stack.Count > 0)
            {
                int start = stack.Pop();
                int end = i;

                // Only fold if block spans multiple lines
                if (document.GetLineByOffset(start).LineNumber !=
                    document.GetLineByOffset(end).LineNumber)
                {
                    foldings.Add(new NewFolding(start, end + 1)
                    {
                        Name = "{...}"
                    });
                }
            }
        }

        foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        return foldings;
    }
}