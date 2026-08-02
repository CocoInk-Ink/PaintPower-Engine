using System;
using System.Collections.Generic;
using System.IO;
using PaintPower.Plumbing.Pipes;

namespace PaintPower.Plumbing;

public class Plumber
{
	private readonly Dictionary<string, Pipe> pipes = new();

	public Pipe GetPipe(string pipeName) => this.pipes[pipeName];

	public AssetPipe GetAssetPipe() => (AssetPipe)this.pipes["AssetPipe"];
	public PluginPipe GetPluginPipe() => (PluginPipe)this.pipes["PluginPipe"];

	public string path;

	string PipesPath => Path.Combine(this.path, "Pipes");

	public Plumber()
	{
		path = Path.Combine(Path.GetTempPath(), "PaintPowerPlumber_" + Guid.NewGuid());

		if (!Directory.Exists(path)) Directory.CreateDirectory(path);

		pipes.Add("AssetPipe", new AssetPipe("AssetPipe", Path.Combine(PipesPath, "Assets")));
		pipes.Add("PluginPipe", new Pipe("PluginPipe", Path.Combine(PipesPath, "PluginPipe")));
		pipes.Add("NetworkPipe", new Pipe("NetworkPipe", Path.Combine(PipesPath, "NetworkPipe")));
	}
}