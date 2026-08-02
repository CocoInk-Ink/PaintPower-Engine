using System;
using System.IO;

namespace PaintPower.Plumbing;

public partial class Pipe
{
	public string name;
	public string path;
	public string GetPipeName => name;

	public Stream? stream;

	// Pipes a stream into folder, then returns path.
	public virtual string PipeOut(string path) => "";
	public virtual string PipeOut(Uri uri) => "";

	public Pipe(string name, string path)
	{
		this.name = name;
		this.path = path;

		Directory.CreateDirectory(path);

		// Create default pipe
		File.Create(Path.Combine(path, "pipe.bin"));
	}
}