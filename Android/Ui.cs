namespace TreeMobile;

/// <summary>Small building blocks shared by the pages.</summary>
public static class Ui
{
	/// <summary>Sets a color that switches automatically when the phone changes between light and dark mode.</summary>
	public static T Themed<T>(this T view, BindableProperty property, string light, string dark) where T : BindableObject
	{
		view.SetAppThemeColor(property, Color.FromArgb(light), Color.FromArgb(dark));
		return view;
	}

	public static Label AccentText(this Label l) => l.Themed(Label.TextColorProperty, "#2E6B3A", "#8FD19E");
	public static Label MutedText(this Label l) => l.Themed(Label.TextColorProperty, "#6B7280", "#A1A1AA");
	public static Label WarnText(this Label l) => l.Themed(Label.TextColorProperty, "#B45309", "#FBBF24");

	public static Label Title(string text) => new Label { Text = text, FontSize = 24, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 8, 0, 0) }.AccentText();
	public static Label Heading(string text) => new() { Text = text, FontSize = 17, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 8, 0, 0) };
	public static Label Note(string text = "") => new Label { Text = text, FontSize = 13 }.MutedText();
	public static Label Body(string text) => new() { Text = text, FontSize = 15 };
	public static Label Bullet(string text) => new() { Text = "•  " + text, FontSize = 15 };

	public static Button Primary(string text, Func<Task> onClick)
	{
		var b = new Button { Text = text, TextColor = Colors.White, FontAttributes = FontAttributes.Bold }
			.Themed(VisualElement.BackgroundColorProperty, "#2E6B3A", "#3F8A4E");
		b.Clicked += async (s, e) => await onClick();
		return b;
	}

	public static Button Secondary(string text, Func<Task> onClick)
	{
		var b = new Button { Text = text }
			.Themed(VisualElement.BackgroundColorProperty, "#E5E7EB", "#3A3A3C")
			.Themed(Button.TextColorProperty, "#111827", "#F3F4F6");
		b.Clicked += async (s, e) => await onClick();
		return b;
	}

	/// <summary>A small rounded button that shows whether it's selected.</summary>
	public static Button Chip(string text, bool selected, Func<Task> onClick)
	{
		var b = new Button { Text = text, FontSize = 13, Padding = new Thickness(12, 4), CornerRadius = 16, HeightRequest = 36 };
		if (selected)
			b.Themed(VisualElement.BackgroundColorProperty, "#2E6B3A", "#3F8A4E").TextColor = Colors.White;
		else
			b.Themed(VisualElement.BackgroundColorProperty, "#E5E7EB", "#3A3A3C").Themed(Button.TextColorProperty, "#111827", "#F3F4F6");
		b.Clicked += async (s, e) => await onClick();
		return b;
	}

	/// <summary>Two views side by side.</summary>
	public static Grid Row(View left, View right)
	{
		var g = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8 };
		g.Add(left, 0);
		g.Add(right, 1);
		return g;
	}

	public static Border Card(View content)
	{
		var border = new Border
		{
			Content = content,
			Padding = 12,
			StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
		}.Themed(VisualElement.BackgroundColorProperty, "#FFFFFF", "#2A2A2C");
		border.SetAppTheme<Brush>(Border.StrokeProperty, new SolidColorBrush(Color.FromArgb("#D1D5DB")), new SolidColorBrush(Color.FromArgb("#48484A")));
		return border;
	}

	/// <summary>Makes any view open something when tapped.</summary>
	public static T OnTap<T>(this T view, Func<Task> action) where T : View
	{
		var tap = new TapGestureRecognizer();
		tap.Tapped += async (s, e) => await action();
		view.GestureRecognizers.Add(tap);
		return view;
	}

	public static ContentPage Page(string title, params View[] children)
	{
		var stack = new VerticalStackLayout { Padding = new Thickness(16, 8, 16, 24), Spacing = 10 };
		stack.Add(Title(title));
		foreach (var c in children) stack.Add(c);
		return new ContentPage { Title = title, Content = new ScrollView { Content = stack } };
	}
}
