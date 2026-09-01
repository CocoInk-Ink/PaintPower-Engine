using System;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Toolbox.Logging;

namespace Toolbox.Plumbing.Pipes;

public class AssetPipe : Pipe
{
	// Instance versions
	public const string AssetPath = "avares://Assets/Resources/";
	public const string IconPath = AssetPath + "Icons/";
	public const string FileIcon = IconPath + "PaintPower Filetypes/";
	public const string CursorIcon = IconPath + "Cursors/";

	// Class versions
	static public readonly string AssetsPath = "avares://Assets/Resources/";
	static public readonly string IconsPath = AssetsPath + "Icons/";
	static public readonly string FilesIcon = IconsPath + "PaintPower Filetypes/";
	static public readonly string CursorsIcon = IconsPath + "Cursors/";

	public AssetPipe(string name, string path) : base(name, path)
	{
		//
	}

	public string LoadAsset(string path)
	{
		return PipeOut(new Uri($"{AssetPath}{path}"));
	}

	public bool AssetExists(Uri uri)
	{
		bool b = AssetLoader.Exists(uri);
		Log.QuickLog($"Asset found?: {b}, Asset: {uri}");
		return b;
	}

	public bool AssetExists(string path)
	{
		return AssetExists(new Uri($"{AssetPath}{path}"));
	}

	public Bitmap GetIcon(string path)
	{
		Uri uri = new($"{IconPath}{path}");
		Uri fallbackUri = new($"{IconPath}Fallback2.png");

		Bitmap bitmap;

		if (AssetExists(uri))
		{
			bitmap = new(PipeOut(uri));
		}
		else if (AssetExists(fallbackUri))
		{
			bitmap = new(PipeOut(fallbackUri));
		}
		else
		{
			// For now.
			bitmap = new(PipeOut(fallbackUri));
		}

		return bitmap;
	}

	// Pipes a stream into folder, then returns path.
	public override string PipeOut(Uri uri)
	{
		using var stream = AssetLoader.Open(uri);

		AssetExists(uri);

		// First copy
		string outpath = Path.Combine(path, Guid.NewGuid().ToString());
		using (var fs = File.Create(outpath))
		{
			stream.CopyTo(fs);
		}

		// Rewind
		stream.Position = 0;

		// Second copy, append
		using (var bin = File.Open(Path.Combine(this.path, "pipe.bin"), FileMode.Append))
		{
			stream.CopyTo(bin);
		}

		return outpath;
	}

}