using System;
using System.IO;
using System.Threading.Tasks;

namespace Toolbox.Accessibility.Text2Speech;

public static class TextToSpeech
{

	public static async Task TalkSpeech(string[] args)
	{
		Console.WriteLine("Enter text to speak:");
		string text = "";

		if (string.IsNullOrWhiteSpace(text))
		{
			Console.WriteLine("No text entered. Exiting.");
			return;
		}

		try
		{
			await SpeakTextAsync(text);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error: {ex.Message}");
		}
	}

	static async Task SpeakTextAsync(string text)
	{
		//
	}
}
