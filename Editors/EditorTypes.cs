using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PaintPower.Tools.Media.Sound;

namespace PaintPower.Editors;

// Valid filenames for each type of editor.
public class EditorTypes
{
    public static string[] Paint = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };

    public static string[] Script = { ".paint", ".Paint", ".pxml", ".pxs", ".psf", ".pss", ".c", ".cs", ".c#", ".h", ".cpp", ".c++", ".hpp", ".lua", ".py",
        ".json", ".xml", ".yaml", ".yml", ".md", ".txt", ".spk", ".sxml", ".xss", ".xs", 
        ".Coco", ".coco", ".script", ".CocoScript", ".cocoscript", ".jav", ".java", ".html", ".htm", ".xml", ".xaml", ".axaml", 
        ".css", ".js", ".jsx", ".ts", ".m", ".json", ".jsonc", ".wxc", ".asm", ".s", ".coffee", ".coffeescript", ".sasm" };

    public static string[] Animation = { ".wxa" };
    public static string[] Video = { ".mp4", ".mov", ".flv", ".wxv" };

    public static string[] Sound = { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".wxs" };

    // These are files that can only be opened in a special viewer, like binary files, project files, etc. They can't be edited in the traditional sense, but they can be viewed in a special viewer.
    public static string[] SpecialViewerFiles = { ".wasm", ".bin", ".rar", ".dat", ".project", ".pproj", ".pprj", ".pprojx", ".pprjx", ".class", ".jar", ".swf", ".zip" };

    public static string[] All = Paint.Concat(Script).Concat(Animation).Concat(Video).Concat(Sound).Concat(SpecialViewerFiles).ToArray();

    // Find editor from list of supported file extensions.
    public static string FindEditorFromExt(string ext)
    {
        // Check each list for the extension.

        ext = ext.ToLower();

        var result = "?";

        // No need for a try/catch really, I just added it just in case.
        try
        {
            // Paint editor.
            if (Paint.Contains(ext))
            {
                result = "Paint";
            }
            // Script Editor
            else if (Script.Contains(ext))
            {
                result = "Script";
            }
            // Animation Editor (Animator)
            else if (Animation.Contains(ext))
            {
                result = "Animation";
            }
            // Video Editor
            else if (Video.Contains(ext))
            {
                result = "Video";
            }
            // Sound Player
            else if (Sound.Contains(ext))
            {
                result = "Sound";
            }
            // Something else was put.
            else
            {
                result = "?";
            }
        }
        catch {
            result = "?";
        }

        return result;
    }
}