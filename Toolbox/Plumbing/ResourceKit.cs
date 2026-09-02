/*
	Toolbox/Plumbing/ResourceKit.cs
	The xPaint Project, PaintPower Engine/Toolbox.
	Copyright (c) 2026 CocoInk Software.

	The xPaint Project, PaintPower Engine/Toolbox.

	This stores the references to various resources within the application.
	If you want to change the resources, check ResourceManifest.cs.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Toolbox.Logging;
using Toolbox.Plumbing.Pipes;

namespace Toolbox.Plumbing;

public static class ResourceKit
{
	public static Plumber Plumber { get; private set; } = null!;

	// Paths to various resources
	static public readonly string AssetsPath = "avares://Assets/Resources/";

	public static void Initialize(Plumber plumber)
	{
		Plumber = plumber;

		OnReadyToLoadResources += LoadImages;
		OnReadyToLoadResources += LoadTextFiles;
		OnReadyToLoadResources += LoadBinaryFiles;
	}

	public static Action? OnReadyToLoadResources { get; set; }

	// ---------------------------
	// IMAGE LOADING
	// ---------------------------
	private static void LoadImages()
	{
		foreach (var kv in ResourceManifest.Images)
		{
			string propertyPath = kv.Key;
			string assetPath = kv.Value;

			var uri = new Uri($"{AssetsPath}{assetPath}");
			var bitmap = new Bitmap(AssetLoader.Open(uri));
			var img = new Image { Source = bitmap };

			SetPropertyByPath(propertyPath, img);
		}
	}

	// ---------------------------
	// TEXT LOADING
	// ---------------------------
	private static void LoadTextFiles()
	{
		foreach (var kv in ResourceManifest.TextFiles)
		{
			string propertyPath = kv.Key;
			string assetPath = kv.Value;

			var uri = new Uri($"{AssetsPath}{assetPath}");
			using var stream = AssetLoader.Open(uri);
			using var reader = new StreamReader(stream);

			string text = reader.ReadToEnd();
			SetPropertyByPath(propertyPath, text);
		}
	}

	// ---------------------------
	// BINARY EXTRACTION
	// ---------------------------
	private static void LoadBinaryFiles()
	{
		foreach (var kv in ResourceManifest.BinaryFiles)
		{
			string propertyPath = kv.Key;
			string assetPath = kv.Value;

			var uri = new Uri($"{AssetsPath}{assetPath}");
			string extractedPath = Plumber.AssetPipe.PipeOut(uri);

			SetPropertyByPath(propertyPath, extractedPath);
		}
	}

	// ---------------------------
	// REFLECTION ASSIGNMENT
	// ---------------------------
	private static void SetPropertyByPath(string path, object value)
	{
		string[] parts = path.Split('.');

		Type type = typeof(ResourceKit);

		// Traverse nested classes
		for (int i = 0; i < parts.Length - 1; i++)
		{
			var nested = type.GetNestedType(parts[i], BindingFlags.Public | BindingFlags.Static);
			if (nested == null)
				throw new Exception($"ResourceKit path invalid: {path}");

			type = nested;
		}

		// Final property
		string propertyName = parts.Last();
		var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);

		if (prop == null)
			throw new Exception($"Property not found: {propertyName} in {type.Name}");

		prop.SetValue(null, value);
	}

	public static void Reset()
	{
	}

	public static class Images
	{
		public static Image Placeholder { get; private set; } = new Image();
		public static Image Fallback { get; private set; } = new Image();

		public static class Icons
		{
			public static class FileIcons { }
			public static class Cursors
			{
				public static Image Pencil { get; private set; } = new Image();
			}
		}
		public static class UI
		{
			public static Image Logo { get; private set; } = new Image();
			public static Image xPaintLogo { get; private set; } = new Image();
		}
	}

	public static class Documents { }
	public static class Archives { }
	public static class Media
	{
		public static class Audio { }
		public static class Fonts { }
		public static class Videos { }
	}
	public static class External
	{
		// Wrapper for external resources
		public static class Plugins { }
		public static class Extensions { }
	}

	public static class Other
	{
		public static class Grammars { }

		// For binary files, use paths instead.
		public static class Paths
		{
			public static string? DefaultProject_1 = null;
		}
	}
}