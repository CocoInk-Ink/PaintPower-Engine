using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Toolbox.Accessibility.Translation;
using Toolbox.Logging;

namespace Toolbox;

public class TranslatableControl : UserControl
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

	public virtual void Translate()
	{
		Translator.LanguageChanged += () =>
		{
			Log.Info("Language changed, updating translations.");
		};
	}

	public void Refresh()
	{
		Translate();
		InvalidateVisual();
	}
}