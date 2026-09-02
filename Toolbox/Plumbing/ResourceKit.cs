using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Toolbox.Logging;

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
		OnReadyToLoadResources = Setup;

		Reset();
	}

	public static void Reset()
	{
		Keys = LoadResourceManifest();
		LoadedResources.Clear();
		ResourcePaths.Clear();
		LoadedImages.Clear();
		LoadedTextFiles.Clear();
		Log.QuickLog("ResourceKit reset completed");
		OnReadyToLoadResources = Setup;
		Log.QuickLog("OnReadyToLoadResources reassigned to Setup");
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
			public static string DefaultProject_1 = string.Empty;
		}
	}

	private sealed class ResourceManifestEntry
	{
		public string Name { get; set; } = string.Empty;
		public string Path { get; set; } = string.Empty;
		public string type { get; set; } = string.Empty;
		public string imagetype { get; set; } = string.Empty;
		public bool isEmbedded { get; set; } = true;
		public object? Item { get; set; }
	}

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
	};

	private static Dictionary<string, object> LoadResourceManifest()
	{
		var candidates = new[]
		{
			Path.Combine(AppContext.BaseDirectory, "resource_manifest.json"),
			Path.Combine(AppContext.BaseDirectory, "Assets", "Resources", "resource_manifest.json"),
			Path.Combine(Directory.GetCurrentDirectory(), "resource_manifest.json"),
			Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "resource_manifest.json"),
		};

		foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			if (string.IsNullOrWhiteSpace(candidate) || !File.Exists(candidate))
				continue;

			var manifest = LoadManifestFromFile(candidate);
			if (manifest.Count > 0)
				return manifest;
		}

		if (Plumber is not null && Plumber.AssetPipe.AssetExists("resource_manifest.json"))
		{
			var assetPath = Plumber.AssetPipe.LoadAsset("resource_manifest.json");
			if (File.Exists(assetPath))
			{
				var manifest = LoadManifestFromFile(assetPath);
				if (manifest.Count > 0)
					return manifest;
			}
		}

		return DefaultKeys;
	}

	private static Dictionary<string, object> LoadManifestFromFile(string path)
	{
		try
		{
			var json = File.ReadAllText(path);
			var manifestDictionary = JsonSerializer.Deserialize<Dictionary<string, ResourceManifestEntry>>(json, JsonOptions);
			if (manifestDictionary is null || manifestDictionary.Count == 0)
				return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

			return manifestDictionary
				.ToDictionary(
					kvp => kvp.Key,
					kvp => (object)new ResourceManifestEntry
					{
						Name = kvp.Value.Name,
						Path = kvp.Value.Path,
						type = kvp.Value.type,
						imagetype = kvp.Value.imagetype,
						isEmbedded = kvp.Value.isEmbedded,
						Item = kvp.Value.Item,
					},
					StringComparer.OrdinalIgnoreCase);
		}
		catch (Exception ex)
		{
			Log.QuickLog($"Failed to load resource manifest from '{path}': {ex.Message}");
			return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private static string? GetStringProperty(object instance, string name)
	{
		if (instance is null) return null;
		if (instance is IDictionary<string, object> dict && dict.TryGetValue(name, out var dictValue))
			return dictValue?.ToString();

		var property = instance.GetType().GetProperty(name);
		return property?.GetValue(instance)?.ToString();
	}

	private static bool GetBoolProperty(object instance, string name)
	{
		if (instance is IDictionary<string, object> dict && dict.TryGetValue(name, out var dictValue))
			return dictValue is bool value && value;

		var property = instance.GetType().GetProperty(name);
		return property is not null && property.GetValue(instance) is bool value2 && value2;
	}

	private static void SetItemValue(object instance, object value)
	{
		if (instance is null) { Log.QuickLog("Instance is null"); return; }
		Log.QuickLog($"Instance: {instance}");

		if (instance is ResourceManifestEntry entry)
		{
			entry.Item = value;
			return;
		}

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
		Log.QuickLog("ResourceKit setup called");
		Log.QuickLog($"Keys is {(Keys is null ? "null" : "not null")}");
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

		if (Keys is null || Keys.Count == 0)
		{
			Keys = DefaultKeys;
			Log.QuickLog("Keys was null or empty, using DefaultKeys");
		}

		foreach (var kvp in Keys)
		{
			var keyName = kvp.Key;
			var entry = kvp.Value;
			if (entry is null) continue;

			Log.QuickLog($"\nEntry: {keyName}\n");
			Log.QuickLog(entry);

			var path = GetStringProperty(entry, "Path");
			if (string.IsNullOrWhiteSpace(path)) continue;

			var type = GetStringProperty(entry, "type");
			var imageType = GetStringProperty(entry, "imagetype");
			var isEmbedded = GetBoolProperty(entry, "isEmbedded");
			var resolvedPath = ResolveResourcePath(path, type, imageType, isEmbedded);
			ResourcePaths[keyName] = resolvedPath;

			Log.QuickLog($"Path of asset: {resolvedPath}");

			if (string.Equals(type, "image", StringComparison.OrdinalIgnoreCase))
			{
				var bitmap = new Bitmap(resolvedPath);
				var image = new Image { Source = bitmap };
				LoadedImages[keyName] = image;
				LoadedResources[keyName] = image;
				SetItemValue(entry, image);
				continue;
			}

			if (string.Equals(type, "sound", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(type, "project", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(type, "binary", StringComparison.OrdinalIgnoreCase))
			{
				LoadedResources[keyName] = resolvedPath;
				SetItemValue(entry, resolvedPath);
				continue;
			}

			if (File.Exists(resolvedPath))
			{
				LoadedTextFiles[keyName] = File.ReadAllText(resolvedPath);
				LoadedResources[keyName] = LoadedTextFiles[keyName];
				SetItemValue(entry, LoadedTextFiles[keyName]);
			}
			else
			{
				LoadedResources[keyName] = resolvedPath;
				SetItemValue(entry, resolvedPath);
			}
		}

		Log.QuickLog("\n");
		Log.QuickLog("ResourceKit setup completed");
		Log.QuickLog("\n");
	}

	// Paths to resources like images, icons, and other assets
	public static Dictionary<string, object> Keys { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

	private static Dictionary<string, object> DefaultKeys => new(StringComparer.OrdinalIgnoreCase)
	{
		["PaintPower_Logo"] = new ResourceManifestEntry
		{
			Name = "PaintPower Logo",
			Path = "PaintPower Logo.png",
			type = "image",
			imagetype = "resource",
			isEmbedded = true,
			Item = Images.Thumbnails.UI.Logo,
		},
		["Default_Project"] = new ResourceManifestEntry
		{
			Name = "Default Project 1",
			Path = "Untitled.xPaint",
			type = "project",
			imagetype = string.Empty,
			isEmbedded = true,
			Item = Other.Paths.DefaultProject_1,
		},
		["Pencil_Cursor"] = new ResourceManifestEntry
		{
			Name = "Pencil Cursor",
			Path = "Pencil.png",
			type = "image",
			imagetype = "cursor",
			isEmbedded = true,
			Item = Icons.Cursors.Pencil,
		},
	};
}