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
        ["Images.Placeholder"] = "UI/Placeholder.png",
        ["Images.Fallback"] = "UI/Fallback.png",

        ["Images.Thumbnails.UI.Logo"] = "Icons/UI/Logo.png",
        ["Images.Thumbnails.UI.xPaintLogo"] = "Icons/UI/xPaintLogo.png",

        ["Icons.Cursors.Pencil"] = "Cursors/Pencil.png",
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
