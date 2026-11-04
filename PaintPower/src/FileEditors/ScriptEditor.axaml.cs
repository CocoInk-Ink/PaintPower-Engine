using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.TextMate;
using Toolbox.Accessibility.Translation;
using PaintPower.FileEditors.Tools.ScriptEditorTools;
using PaintPower.ProjectSystem;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TextMateSharp.Grammars;
using Toolbox.Plumbing;

namespace PaintPower.FileEditors;

public partial class ScriptEditor : FileEditor
{
    private readonly TempWorkspace _workspace;
    private TextMate.Installation _textMateInstallation;
    private RegistryOptions _registryOptions;

    private RainbowBracketColorizer _bracketColorizer;
    private FoldingManager _foldingManager;
    private CodeFoldingStrategy _foldingStrategy;

	private TextEditor _editor;

    // For custom languages that need to be added directly.
    private string[] types = { ".as", ".mxml", ".jsfl", ".pxml", ".pss", ".psf", ".pxs" };

    public ScriptEditor(string relativePath, TempWorkspace workspace)
    {

		InitializeComponent();

        _workspace = workspace;

		var _ = this.FindControl<TextEditor>("Editor");

        if (_ == null)
		{
			throw new Exception("Editor is null!");
		}

		_editor = _;

		_ = null; GC.Collect();

		// Add bracket colorizer
        _bracketColorizer = new RainbowBracketColorizer(_editor.Document);
        _editor.TextArea.TextView.LineTransformers.Add(_bracketColorizer);

		// Add code folding support
		_foldingManager = FoldingManager.Install(_editor.TextArea);
        _foldingStrategy = new CodeFoldingStrategy();
        _foldingStrategy.UpdateFoldings(_foldingManager, _editor.Document);
        
        _editor.Text = _workspace.LoadText(FullPath);
        _editor.Focus();

        // 1. Create registry options with a theme
        _registryOptions = new RegistryOptions(ThemeName.LightPlus);
        var manager = new GrammarManager();

        // 2. Install TextMate
        _textMateInstallation = _editor.InstallTextMate(_registryOptions);

        // 3. Set theme
        _textMateInstallation.SetTheme(_registryOptions.LoadTheme(ThemeName.DarkPlus));

		SetGrammarFromExtension(Path.GetExtension(relativePath), _editor);

        // Autosave
        _editor.TextChanged += (_, __) =>
        {
            MarkDirty();

            _bracketColorizer.Update(_editor.Document);
            _editor.TextArea.TextView.InvalidateVisual();

            _foldingStrategy.UpdateFoldings(_foldingManager, _editor.Document);
        };
    }

	public void SetGrammarFromExtension(string ext, TextEditor editor)
	{
		// 4. Set grammar based on file extension
        var scope = _registryOptions.GetScopeByExtension(ext);

        // Add custom language support like .pxml, .pxs, .pss, etc.
        // For now, because we don't have TextMate grammars for those, we'll just use html, css, js, java, and C# grammar as a placeholder.

        // .pxml -> xml
        // .pss, psf -> css
        // .pxs -> PaintScript (new)
        if (ext == ".Coco" || ext == ".coco")
            scope = _registryOptions.GetScopeByExtension(".cs");
        else if (ext == ".CocoScript" || ext == ".cocoscript")
            scope = _registryOptions.GetScopeByExtension(".js");
        else if (ext == ".pxml")
            scope = _registryOptions.GetScopeByExtension(".xml");
        if (ext == ".psf")
            scope = _registryOptions.GetScopeByExtension(".css");
        else if (ext == ".pss")
            scope = _registryOptions.GetScopeByExtension(".css");
        else if (ext == ".jsfl")
            scope = _registryOptions.GetScopeByExtension(".js");
        else if (ext == ".asc")
            scope = _registryOptions.GetScopeByExtension(".js");

        if (scope != null)
        {
            _textMateInstallation.SetGrammar(scope);
        }
        else if (types.Contains(ext))
        {
            var assetPipe = Plumber.MainPlumber.AssetPipe;
            var grammarPath = assetPipe.LoadAsset($"Grammars/{GetGrammarFile(ext)}");

            _textMateInstallation.SetGrammarFile(grammarPath);
        }
        else
        {
            Debug.WriteLine($"No TextMate grammar found for extension: {ext}");
        }

        // 5. Remove AvaloniaEdit built-in highlighting (must NOT be used)
        editor.SyntaxHighlighting = null;
	}

    private string GetGrammarFile(string ext)
    {
        return ext switch
        {
            ".as" => "AS3",
            ".as2" => "AS3",
            ".as3" => "AS3",
            ".mxml" => "MXML",
            ".pxs" => "PaintScript",
            _ => throw new Exception("Should not happen! Error loading grammar in script editor!")
        } + ".tmLanguage.json";
    }

    override public void Save()
    {
        _workspace.SaveFile(FullPath, _editor.Text);
    }

    public override void Activate()
    {
        _editor.Text = _workspace.LoadText(FullPath);
    }

    private void SaveButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		Save();
	}
}