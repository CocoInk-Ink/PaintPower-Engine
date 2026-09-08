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

#pragma warning disable CA2211 // Non-constant fields should not be visible
public static class ResourceKit
{
	public static Plumber Plumber { get; private set; } = null!;

	// Paths to various resources
	static public readonly string AssetsPath = "avares://Assets/Resources/";

	public static void Initialize(Plumber plumber)
	{
		Plumber = plumber;

		OnReadyToLoadResources += Unpack;
	}

	public static Action? OnReadyToLoadResources { get; set; }

	public static void Unpack()
	{
		foreach (var kv in ResourceManifest.BinaryFiles)
		{
			string propertyPath = kv.Key;
			string assetPath = kv.Value;

			var uri = new Uri($"{AssetsPath}{assetPath}");
			string extractedPath = Plumber.AssetPipe.ExtractIfNeeded(uri);

			SetPropertyByPath(propertyPath, extractedPath);
		}

		foreach (var kv in ResourceManifest.DefaultProjects)
		{
			string propertyPath = kv.Key;
			string assetPath = kv.Value;

			var uri = new Uri($"{AssetsPath}{assetPath}");
			string extractedPath = Plumber.AssetPipe.ExtractIfNeeded(uri);

			SetPropertyByPath(propertyPath, extractedPath);
		}

		foreach (var kv in ResourceManifest.Grammars)
		{
			string propertyPath = kv.Key;
			string assetPath = kv.Value;

			var uri = new Uri($"{AssetsPath}{assetPath}");
			string extractedPath = Plumber.AssetPipe.ExtractIfNeeded(uri);

			SetPropertyByPath(propertyPath, extractedPath);
		}

		foreach (var kv in ResourceManifest.Images)
		{
			string propertyPath = kv.Key;
			string assetPath = kv.Value;

			var uri = new Uri($"{AssetsPath}{assetPath}");
			string extractedPath = Plumber.AssetPipe.ExtractIfNeeded(uri);

			SetPropertyByPath(propertyPath, extractedPath);
		}

		foreach (var kv in ResourceManifest.TextFiles)
		{
			string propertyPath = kv.Key;
			string assetPath = kv.Value;

			var uri = new Uri($"{AssetsPath}{assetPath}");
			string extractedPath = Plumber.AssetPipe.ExtractIfNeeded(uri);

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
			if (nested == null) throw new Exception($"ResourceKit path invalid: {path}");

			type = nested;
		}

		// Final property
		string propertyName = parts.Last();

		// Try property first
		var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
		if (prop != null)
		{
			prop.SetValue(null, value);
			return;
		}

		// Try field next
		var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Static);
		if (field != null)
		{
			field.SetValue(null, value);
			return;
		}

		throw new Exception($"Property or field not found: {propertyName} in {type.Name}");


		prop.SetValue(null, value);
	}

	public static Bitmap AsBitmap(string path)
	{
		return new Bitmap(path);
	}

	public static string AsString(string path)
	{
		return File.ReadAllText(path);
	}

	public static class Images
	{
		public static string Placeholder = string.Empty;
		public static string Fallback = string.Empty;

		public static class Cursors
		{
			public static string Pencil = string.Empty;
		}

		public static class Icons
		{
			public static class FileIcons { }

			public static string File = string.Empty;
			public static string Image = string.Empty;
			public static string ImageFile = string.Empty;
			public static string Folder1 = string.Empty;
			public static string Folder2 = string.Empty;
			public static string FolderOpen = string.Empty;
			public static string Import = string.Empty;
			public static string Export = string.Empty;
		}
		public static class UI
		{
			public static string Logo = string.Empty;
			public static string xPaintLogo = string.Empty;

			public static class No
			{
				public static string NoAccess = string.Empty;
				public static string Red = string.Empty;
				public static string Blue = string.Empty;
			}
		}
	}

	public static class Documents { }
	public static class Archives { }
	public static class Media
	{
		public static class Audio
		{
			public static string Click = string.Empty;
		}

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
		public static class Grammars {
			public static string ActionScript = string.Empty;
			public static string MXML = string.Empty;
			public static string PaintScript = string.Empty;
		 }

		// For binary files, use paths instead.
		public static class Paths
		{
			public static string DefaultProject_1 = string.Empty;
		}
	}
}

#pragma warning restore CA2211 // Non-constant fields should not be visible