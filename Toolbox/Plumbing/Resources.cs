using System;
using System.IO;
using Avalonia.Controls;

namespace Toolbox.Plumbing;

public class Resources
{
	public Plumber Plumber { get; private set; }

	public Resources(Plumber plumber) {
		Plumber = plumber;
		OnReadyToLoadResources += Setup;
	}

	public void Reset()
	{
		OnReadyToLoadResources = null;
		Keys = DefaultKeys;
	}

	public Action? OnReadyToLoadResources { get; set; }

	public static class Images {
		public static Image Placeholder { get; private set; } = new Image();
		public static Image Fallback { get; private set; } = new Image();
		public static class Thumbnails
		{
			public static class UI
			{
				public static Image Logo { get; private set; } = new Image();
				public static Image xPaintLogo { get; private set; } = new Image();
			}
		}
		public static class FileIcons
		{
			
		}
	}

	public static class Icons {
		public static class Small {}
		public static class Large {}
		public static class Cursors {}
	}
	public static class Documents {}
	public static class Archives {}
	public static class Media
	{
		public static class Audio {}
		public static class Fonts {}
		public static class Videos {}
	}
	public class External
	{
		// Wrapper for external resources
		public Resources Plugins { get; private set; } = null!;
		public Resources Extensions { get; private set; } = null!;
	}

	public static class Other {
		public static class Grammars {}

		// For binary files, use paths instead.
		public static class Paths {}
	}

	private void Setup()
	{
		// For each key, check type, and load the resource accordingly.
		// Images are loaded based on their type and imagetype property.
		//
		// if (type == "image" && imagetype == "resource") Resources/ (base folder), if (imagetype == "icon") Resources/Icons/,
		// if (type == "image" && imagetype == "filetype") Resources/Icons/PaintPower Filetypes,
		// if (type == "image" && imagetype == "cursor") Resources/Icons/Cursors/,
		// if (type == "grammar") Resources/Grammars/,
		// if (type == "lang") Resources/lang/,
		// if (type == "sound") Resources/Sounds/,
		// if (type == "theme") Resources/Themes/
		// if (type == "b") 

		Keys ??= DefaultKeys;
		foreach (var property in Keys.GetType().GetProperties()) {}
	}

	// Paths to resources like images, icons, and other assets
	public dynamic Keys;

	private dynamic DefaultKeys = new
	{
							//	File Path,					// type,		// imagetype (Parent Folder)	// Is embedded into output .dll file?
		PaintPower_Logo = new { Path = "PaintPower Logo.png", type = "image", imagetype = "resource",		isEmbedded = true },
	};
}