using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System.Collections.Generic;

namespace PaintPower.EditorTools.ScriptEditorTools;

public class RainbowBracketColorizer : DocumentColorizingTransformer
{
    private static readonly Color[] BracketColors =
    {
        Color.Parse("#FFD700"), // Yellow
        Color.Parse("#C586C0"), // Purple
        Color.Parse("#569CD6"), // Blue
    };

    private readonly List<(int offset, int depth)> _brackets = new();

    public RainbowBracketColorizer(TextDocument document)
    {
        ParseDocument(document);
    }

    public void Update(TextDocument document)
    {
        _brackets.Clear();
        ParseDocument(document);
    }

    private void ParseDocument(TextDocument doc)
    {
        var stack = new Stack<(char type, int offset)>();
        string text = doc.Text;

        bool inString = false;
        bool inChar = false;
        bool inLineComment = false;
        bool inBlockComment = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            char next = i + 1 < text.Length ? text[i + 1] : '\0';
            char prev = i > 0 ? text[i - 1] : '\0';

            // --- COMMENT & STRING STATE MACHINE ---

            // End of line comment
            if (inLineComment && c == '\n')
                inLineComment = false;

            // End of block comment
            if (inBlockComment && c == '*' && next == '/')
            {
                inBlockComment = false;
                i++; // skip '/'
                continue;
            }

            // Inside comment → ignore everything
            if (inLineComment || inBlockComment)
                continue;

            // Start of line comment
            if (!inString && !inChar && c == '/' && next == '/')
            {
                inLineComment = true;
                i++; // skip second '/'
                continue;
            }

            // Start of block comment
            if (!inString && !inChar && c == '/' && next == '*')
            {
                inBlockComment = true;
                i++; // skip '*'
                continue;
            }

            // Toggle string literal
            if (!inChar && c == '"' && prev != '\\')
            {
                inString = !inString;
                continue;
            }

            // Toggle char literal
            if (!inString && c == '\'' && prev != '\\')
            {
                inChar = !inChar;
                continue;
            }

            // Inside string or char → ignore brackets
            if (inString || inChar)
                continue;

            // --- REAL BRACKET PARSING ---

            if (c is '{' or '(' or '[')
            {
                stack.Push((c, i));
                int depth = stack.Count - 1;
                _brackets.Add((i, depth));
            }
            else if (c is '}' or ')' or ']')
            {
                if (stack.Count > 0)
                {
                    var (openType, openOffset) = stack.Peek();

                    bool matches =
                        openType == '{' && c == '}' ||
                        openType == '(' && c == ')' ||
                        openType == '[' && c == ']';

                    if (matches)
                    {
                        stack.Pop();
                        int depth = stack.Count;
                        _brackets.Add((i, depth));
                    }
                    else
                    {
                        // mismatched type
                        _brackets.Add((i, -1));
                    }
                }
                else
                {
                    // no opening bracket
                    _brackets.Add((i, -1));
                }
            }
        }

        // leftover unmatched opening brackets
        while (stack.Count > 0)
        {
            var (_, offset) = stack.Pop();
            _brackets.Add((offset, -1));
        }
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        foreach (var (offset, depth) in _brackets)
        {
            if (offset >= line.Offset && offset < line.EndOffset)
            {
                int index = offset - line.Offset;

                Color color = depth >= 0
                    ? BracketColors[depth % BracketColors.Length]
                    : Colors.Red;

                ApplyColor(line, index, color);
            }
        }
    }

    private void ApplyColor(DocumentLine line, int index, Color color)
    {
        ChangeLinePart(
            line.Offset + index,
            line.Offset + index + 1,
            (element) =>
            {
                element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(color));
            }
        );
    }
}