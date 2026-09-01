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
	public Resources Resources { get; private set; }
	
	public Toolkit()
	{
		Plumber plumber = new();
		plumber.MakeMainPlumber();

		Resources = new Resources(plumber);
	}
}