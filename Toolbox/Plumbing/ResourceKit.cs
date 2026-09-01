using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace Toolbox.Plumbing;

public static class ResourceKit
{
	public static Plumber Plumber { get; private set; } = null!;

	public static Dictionary<string, object> LoadedResources { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
	public static Dictionary<string, string> ResourcePaths { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
	public static Dictionary<string, Image> LoadedImages { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
	public static Dictionary<string, string> LoadedTextFiles { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

	public static void Initialize(Plumber plumber) {
		Plumber = plumber;
		OnReadyToLoadResources += Setup;

		Reset();
	}

	public static void Reset()
	{
		Keys = DefaultKeys;
		LoadedResources.Clear();
		ResourcePaths.Clear();
		LoadedImages.Clear();
		LoadedTextFiles.Clear();
		OnReadyToLoadResources = Setup;
		OnReadyToLoadResources?.Invoke();
	}

	public static Action? OnReadyToLoadResources { get; set; }

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
		public static class Cursors {
			public static Image Pencil { get; private set; } = new Image();
		}
	}
	public static class Documents {}
	public static class Archives {}
	public static class Media
	{
		public static class Audio {}
		public static class Fonts {}
		public static class Videos {}
	}
	public static class External
	{
		// Wrapper for external resources
		public static class Plugins { }
		public static class Extensions { }
	}

	public static class Other {
		public static class Grammars {}

		// For binary files, use paths instead.
		public static class Paths {
			public static string? DefaultProject_1 = null;
		}
	}

	private static string? GetStringProperty(object instance, string name)
	{
		var property = instance.GetType().GetProperty(name);
		return property?.GetValue(instance)?.ToString();
	}

	private static bool GetBoolProperty(object instance, string name)
	{
		var property = instance.GetType().GetProperty(name);
		return property is not null && property.GetValue(instance) is bool value && value;
	}

	private static void SetItemValue(object instance, object value)
	{
		if (instance is null) return;

		var property = instance.GetType().GetProperty("Item");
		if (property is not null && property.CanWrite)
		{
			property.SetValue(instance, value);
			return;
		}

		var field = instance.GetType().GetField("Item", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
			?? instance.GetType().GetField("<Item>i__Field", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		field?.SetValue(instance, value);
	}

	private static string ResolveResourcePath(string path, string? type, string? imagetype, bool isEmbedded)
	{
		var cleanPath = path.Replace('\\', '/');

		if (!isEmbedded)
		{
			return Path.GetFullPath(cleanPath);
		}

		var resourceFolder = type?.ToLowerInvariant() switch
		{
			"image" => imagetype?.ToLowerInvariant() switch
			{
				"resource" => string.Empty,
				"icon" => "Icons/",
				"filetype" => "Icons/PaintPower Filetypes/",
				"cursor" => "Icons/Cursors/",
				_ => string.Empty,
			},
			"grammar" => "Grammars/",
			"lang" => "lang/",
			"sound" => "Sounds/",
			"theme" => "Themes/",
			"project" => "Default Projects/",
			"document" => "Documents/",
			_ => string.Empty,
		};

		if (!string.IsNullOrEmpty(resourceFolder) && !cleanPath.StartsWith(resourceFolder, StringComparison.OrdinalIgnoreCase))
		{
			cleanPath = resourceFolder + cleanPath;
		}

		return Plumber.AssetPipe.LoadAsset(cleanPath);
	}

	public static void Setup()
	{
		// For each key, check type, and load the resource accordingly.
		// Images are loaded based on their type and imagetype property.

		// Read the rules below to figure out how to load resources based on their properties.

		/**
		
		// Not code, turn this back into a comment, you silly.
		// You are suppose to read it, like a normal person.

		if (isEmbedded) {
			if (type == "image" && imagetype == "resource") Resources/ (base folder), if (imagetype == "icon") Resources/Icons/,
			if (type == "image" && imagetype == "filetype") Resources/Icons/PaintPower Filetypes,
			if (type == "image" && imagetype == "cursor") Resources/Icons/Cursors/,
			if (type == "grammar") Resources/Grammars/,
			if (type == "lang") Resources/lang/,
			if (type == "sound") Resources/Sounds/,
			if (type == "theme") Resources/Themes/
			if (type == "project") Resources/Default Projects/
		} else {
			Just use the path.
		}

		/**/

		Keys ??= DefaultKeys;
		foreach (var property in Keys.GetType().GetProperties())
		{
			var entry = property.GetValue(Keys);
			if (entry is null) continue;

			var path = GetStringProperty(entry, "Path");
			if (string.IsNullOrWhiteSpace(path)) continue;

			var type = GetStringProperty(entry, "type");
			var imageType = GetStringProperty(entry, "imagetype");
			var isEmbedded = GetBoolProperty(entry, "isEmbedded");
			var resolvedPath = ResolveResourcePath(path, type, imageType, isEmbedded);
			ResourcePaths[property.Name] = resolvedPath;

			if (string.Equals(type, "image", StringComparison.OrdinalIgnoreCase))
			{
				var bitmap = new Bitmap(resolvedPath);
				var image = new Image { Source = bitmap };
				LoadedImages[property.Name] = image;
				LoadedResources[property.Name] = image;
				SetItemValue(entry, image);
				continue;
			}

			if (string.Equals(type, "sound", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(type, "project", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(type, "binary", StringComparison.OrdinalIgnoreCase))
			{
				LoadedResources[property.Name] = resolvedPath;
				SetItemValue(entry, resolvedPath);
				continue;
			}

			if (File.Exists(resolvedPath))
			{
				LoadedTextFiles[property.Name] = File.ReadAllText(resolvedPath);
				LoadedResources[property.Name] = LoadedTextFiles[property.Name];
				SetItemValue(entry, LoadedTextFiles[property.Name]);
			}
			else
			{
				LoadedResources[property.Name] = resolvedPath;
				SetItemValue(entry, resolvedPath);
			}
		}
	}

	// Paths to resources like images, icons, and other assets
	public static dynamic Keys;

	private static dynamic DefaultKeys = new
	{
								//	File Path,								// type,			// imagetype (Parent Folder)	// Is embedded into output .dll file?	// Thing to be (usage) (associated with the resource)
		PaintPower_Logo = new { Path = "PaintPower Logo.png", 				type = "image", 	imagetype = "resource",			isEmbedded = true,                    	Item = Images.Thumbnails.UI.Logo },
		Default_Project = new { Path = "Untitled.xPaint", 					type = "project", 	imagetype = string.Empty,		isEmbedded = false,       				Item = Other.Paths.DefaultProject_1 },
		Pencil_Cursor = new {	Path = "Pencil.png", 						type = "image", 	imagetype = "cursor",			isEmbedded = true,                    	Item = Icons.Cursors.Pencil },
	};
}