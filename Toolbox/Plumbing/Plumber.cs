/*
	Toolbox/Plumbing/Plumber.cs
	Copyright (c) 2026 CocoInk Software.

	The xPaint Project, PaintPower Engine/Toolbox.

	Mostly used for the Asset Pipe, but has other pipes.
	This file is the main controller for pipes.

	// Asset Pipe: //

	Assets embedded inside of application are not accessible directly, Avalonia supports
	images and some other assets for built in use, but for most, especially for binary files
	such as the default project, and embedded images used in places other then direct Avalonia use.

	So we extract these assets to the filesystem, in a temp folder, then return the path of the file to
	be read via filesystem.

	// Plug-In Pipe: //
	No use.

	// Network Pipe: //
	No use.
*/

using System;
using System.Collections.Generic;
using System.IO;
using Toolbox.Plumbing.Pipes;

namespace Toolbox.Plumbing;

public class Plumber
{
	// Don't make nullable, should throw if not initialized. Don't init in constructor.
	public static Plumber MainPlumber = null!;

	// Pipes and stuff
	private readonly Dictionary<string, Pipe> pipes = new();

	public Pipe GetPipe(string pipeName) => this.pipes[pipeName];

	public AssetPipe AssetPipe { get; private set; }
	public PluginPipe PluginPipe { get; private set; }

	public string path;

	string PipesPath => Path.Combine(this.path, "Pipes");

	public Plumber()
	{
		path = Path.Combine(Path.GetTempPath(), "PaintPowerPlumber_" + Guid.NewGuid() + "/");

		if (!Directory.Exists(path)) Directory.CreateDirectory(path);

		Console.WriteLine(path);

		AssetPipe = makeAssetPipe();
		PluginPipe = makePluginPipe();
		pipes.Add("NetworkPipe", new Pipe("NetworkPipe", Path.Combine(PipesPath, "NetworkPipe")));
	}

	AssetPipe makeAssetPipe()
	{
		var p = new AssetPipe("AssetPipe", Path.Combine(PipesPath, "Assets"));
		pipes.Add("AssetPipe", p); return p;
	}

	PluginPipe makePluginPipe()
	{
		var p = new PluginPipe("PluginPipe", Path.Combine(PipesPath, "PluginPipe"));
		pipes.Add("PluginPipe", p);
		return p;
	}

	public void MakeMainPlumber()
	{
		MainPlumber = this;
	}
}