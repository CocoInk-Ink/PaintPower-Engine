using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Toolbox.Accessibility.Translation;
using Toolbox.Logging;

namespace Toolbox;

public interface ITranslatable
{
	void TranslateGUI();
	void Refresh();
}

public class TranslatableControl : UserControl, ITranslatable
{
	public TranslatableControl() : base()
	{

		this.AttachedToVisualTree += (_, __) =>
		{
			Refresh();
			Translator.LanguageChanged += Refresh;
		};

		this.DetachedFromVisualTree += (_, __) =>
		{
			Translator.LanguageChanged -= Refresh;
		};
	}

	public virtual void TranslateGUI()
	{
		Translator.LanguageChanged += () =>
		{
			Log.Info("Language changed, updating translations.");
		};
	}

	public void Refresh()
	{
		TranslateGUI();
		InvalidateVisual();
	}
}

public interface IControlWithImages
{
	void PipeAndLoadImages();
}

public class TranslatableButton : Button
{
	public void AddTranslateHandler(Action? action)
	{
		Translator.LanguageChanged += action;
	}

	public void SetClasses(string[] classes)
	{
		ControlCreator.SetClasses(classes, this);
	}
}

public static class ControlCreator
{
	public static TranslatableButton CreateTranslatableButton(string text, bool? justTranslate = true, EventHandler<Avalonia.Interactivity.RoutedEventArgs>? onClick = null, string[]? classes = null)
	{
		var b = new TranslatableButton
		{
			Content = text
		};

		if (justTranslate == true)
		{
			Translator.LanguageChanged += () =>
			{
				b.Content = Translator.Translate(text);
			};
		}

		if (onClick != null) b.Click += onClick;

		if (classes != null)
		{
			b.Classes.Clear();

			foreach (var item in classes)
			{
				b.Classes.Add(item);
			}
		}

		return b;
	}

	public static T SetClasses<T>(string[] classes, T t)
	{
		if (t is Avalonia.StyledElement control)
		{
			control.Classes.Clear();
			foreach (var item in classes)
			{
				control.Classes.Add(item);
			}
		}
		return t;
	}
}