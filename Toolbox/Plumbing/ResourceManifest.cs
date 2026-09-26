/*
	Toolbox/Plumbing/ResourceManifest.cs
	The xPaint Project, PaintPower Engine/Toolbox.
	Copyright (c) 2026 CocoInk Software.

	This file contains the manifest of resources available to the application.
	Modify this file to add or remove resources.
*/

namespace Toolbox.Plumbing;

public static class ResourceManifest
{
	// Add your images here.
    public static readonly Dictionary<string, string> Images = new()
    {
        // Placeholder and fallback images
        ["Images.Placeholder"] = "Images/Icons/Fallback2.png",
        ["Images.Fallback"] = "Images/Icons/Fallback.png",

        // Branding and logos
        ["Images.UI.Logo"] = "Images/UI/PaintPower Logo.png",
        ["Images.UI.xPaintLogo"] = "Images/UI/xPaint Logo.png",

        // No access
        ["Images.UI.No.NoAccess"] = "Images/UI/NoAccess.gif",

        // It's red because no blue
        ["Images.UI.No.Red"] = "Images/UI/NoRed.png",

        // It's blue because no red
        ["Images.UI.No.Blue"] = "Images/UI/NoBlue.png",

        // Cursors
        ["Images.Cursors.Pencil"] = "Images/Cursors/Pencil.png",

        // Icons
        ["Images.Icons.File"] = "Images/Icons/File.png",
        ["Images.Icons.Image"] = "Images/Icons/Image.png",
        ["Images.Icons.ImageFile"] = "Images/Icons/Image file.png",

        ["Images.Icons.Folder1"] = "Images/Icons/Folder.png",
        ["Images.Icons.Folder2"] = "Images/Icons/Folder2.png",
        ["Images.Icons.FolderOpen"] = "Images/Icons/Folder open.png",

        ["Images.Icons.Import"] = "Images/Icons/Import.png",
        ["Images.Icons.Export"] = "Images/Icons/Export.png",

        // Animation/Paint Editor images

        // Brush Sizes

        ["Images.UI.Paint_Animation_Editor.BrushSizes.BrushVerySmall"] = "Images/UI/Paint and Animation Editor/Brush Sizes/BrushVerySmall.png",
        ["Images.UI.Paint_Animation_Editor.BrushSizes.BrushSmall"] = "Images/UI/Paint and Animation Editor/Brush Sizes/BrushSmall.png",
        ["Images.UI.Paint_Animation_Editor.BrushSizes.BrushMedium"] = "Images/UI/Paint and Animation Editor/Brush Sizes/BrushMedium.png",
        ["Images.UI.Paint_Animation_Editor.BrushSizes.BrushNormal"] = "Images/UI/Paint and Animation Editor/Brush Sizes/BrushNormal.png",
        ["Images.UI.Paint_Animation_Editor.BrushSizes.BrushBig"] = "Images/UI/Paint and Animation Editor/Brush Sizes/BrushBig.png",
        ["Images.UI.Paint_Animation_Editor.BrushSizes.BrushVeryBig"] = "Images/UI/Paint and Animation Editor/Brush Sizes/BrushVeryBig.png",
        ["Images.UI.Paint_Animation_Editor.BrushSizes.BrushHuge"] = "Images/UI/Paint and Animation Editor/Brush Sizes/BrushHuge.png"
    };

	// Add your text files here.
	public static readonly Dictionary<string, string> TextFiles = new() {};

	// Not grammar like languages, grammar like for programming languages.
    public static readonly Dictionary<string, string> Grammars = new()
    {
        ["Other.Grammars.ActionScript"] 	=	"Grammars/AS3.tmLanguage.json",
		["Other.Grammars.MXML"]				=	"Grammars/MXML.tmLanguage.json",
		["Other.Grammars.PaintScript"]		=	"Grammars/PaintScript.tmLanguage.json"
    };

	// Add raw binary files here.
    public static readonly Dictionary<string, string> BinaryFiles = new()
    {
        ["Media.Audio.Click"] = "Binary/Sounds/Click.wav",


        /*==== Compilers ==== { */
            // Compiler 0.1.x {
                ["Other.Paths.Compilers.Compiler0_1.c0_1_0"] = "compiler0.1.0.wasm"
            // }
        // }
    };

	// Add default projects here.
	public static readonly Dictionary<string, string> DefaultProjects = new()
    {
        ["Other.Paths.DefaultProject_1"] = "Binary/Default Projects/Untitled.xPaint"
    };
}
