using Toolbox.Plumbing;

namespace Toolbox;

public class Program
{
	public static void Main(string[] args)
	{
		_ = new Toolkit();
	}
}

public class Toolkit
{
	public Toolkit()
	{
		Plumber plumber = new();
		plumber.MakeMainPlumber();

		ResourceKit.Initialize(plumber);
	}
}