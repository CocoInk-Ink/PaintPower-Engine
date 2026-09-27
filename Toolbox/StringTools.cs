using System;

namespace Toolbox;

public class StringTools
{
	public static string SplitLast(string input, string delimiter)
		{
			int lastIndex = input.LastIndexOf(delimiter);

			// If delimiter not found, return the whole string
			if (lastIndex == -1)
				return input;

			// Return substring after the last delimiter
			return input.Substring(lastIndex + 1);
		}

		public static string GetFilenameFromPath(string path)
	{
		return SplitLast(SplitLast(path, "/"), "\\");
	}
}