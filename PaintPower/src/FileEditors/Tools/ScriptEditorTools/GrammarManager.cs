using System.IO;
using TextMateSharp.Grammars;

namespace PaintPower.FileEditors.Tools.ScriptEditorTools;

public class GrammarManager
{
    public string LoadGrammar(RegistryOptions options, string grammarName, string fileName)
    {

        // Path to JSON grammar file
        var fileInfo = new FileInfo($"avares://Assets/Resources/Grammars/{fileName}");

        // Load grammar into TextMateSharp registry
        options.LoadFromLocalFile(grammarName, fileInfo, overwrite: true);

        return grammarName;
    }

    public string LoadActionScriptGrammar (RegistryOptions options) => LoadGrammar(options, "source.actionscript.3", "AS3.tmLanguage.json");
}
