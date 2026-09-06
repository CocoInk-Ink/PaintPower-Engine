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


    };

	// Add your text files here.
	public static readonly Dictionary<string, string> TextFiles = new() {};

	// Not grammar like languages, grammar like for programming languages.
    public static readonly Dictionary<string, string> Grammars = new()
    {
        ["Other.Grammars.ActionScript_3"] 	=	"AS3.tmLanguage.json",
		["Other.Grammars.MXML"]				=	"MXML.tmLanguage.json",
		["Other.Grammars.PaintScript"]		=	"PaintScript.tmLanguage.json"
    };

	// Add raw binary files here.
    public static readonly Dictionary<string, string> BinaryFiles = new() {};

	// Add default projects here.
	public static readonly Dictionary<string, string> DefaultProjects = new()
    {
        ["Other.Paths.DefaultProject_1"] = "Untitled.xPaint"
    };
}
