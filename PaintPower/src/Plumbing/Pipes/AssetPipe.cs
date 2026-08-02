using System;
using System.IO;
using Avalonia.Platform;

namespace PaintPower.Plumbing.Pipes;

public class AssetPipe : Pipe
{
	public AssetPipe(string name, string path) : base(name, path)
	{
		//
	}

	public const string AssetPath = "avares://PaintPower/src/Assets/";

	public string LoadAsset(string path)
	{
		return PipeOut(new Uri(Path.Combine(AssetPath, path)));
	}

	public bool AssetExists(Uri uri)
	{
		return AssetLoader.Exists(uri);
	}

	public bool AssetExists(string path)
	{
		return AssetExists(new Uri(Path.Combine(AssetPath, path)));
	}

	// Pipes a stream into folder, then returns path.
	public override string PipeOut(Uri uri)
	{
		this.stream = AssetLoader.Open(uri);
		string outpath = Path.Combine(path, Guid.NewGuid().ToString());

		using (var fs = File.Create(outpath))
            stream.CopyTo(fs);

		return outpath;
	}
}