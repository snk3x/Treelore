namespace TreeMobile;

/// <summary>The user's life list: every tree they've saved, newest first.</summary>
public class FindsPage : ContentPage
{
	readonly VerticalStackLayout _list = new() { Spacing = 10 };
	readonly Label _summary = Ui.Note();

	public FindsPage()
	{
		Title = "My trees";
		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(16, 8, 16, 24),
				Spacing = 10,
				Children = { Ui.Title("My trees"), _summary, _list },
			},
		};
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await TreeEngine.LoadAsync();
		Fill();
	}

	void Fill()
	{
		var finds = FindStore.Load();
		_list.Children.Clear();
		int kinds = finds.Select(f => f.Scientific).Distinct().Count();
		_summary.Text = finds.Count == 0
			? "Trees you identify and save show up here."
			: $"{finds.Count} saved, {kinds} different {(kinds == 1 ? "species" : "species")}";

		foreach (var find in finds)
		{
			var f = find;
			var grid = new Grid { ColumnDefinitions = { new(new GridLength(72)), new(GridLength.Star) }, ColumnSpacing = 12 };
			if (f.Photos.Count > 0)
				grid.Add(new Image { Source = ImageSource.FromFile(f.Photos[0]), WidthRequest = 72, HeightRequest = 72, Aspect = Aspect.AspectFill }, 0);
			var text = new VerticalStackLayout
			{
				Spacing = 2,
				Children =
				{
					new Label { Text = f.Common, FontSize = 16, FontAttributes = FontAttributes.Bold },
					new Label { Text = f.Scientific, FontSize = 13, FontAttributes = FontAttributes.Italic }.MutedText(),
					Ui.Note($"{f.When:MMMM d, yyyy}  ·  {f.Confidence:P0} match"),
				},
			};
			var delete = new Button { Text = "Delete", FontSize = 12, HeightRequest = 32, Padding = new Thickness(10, 0), HorizontalOptions = LayoutOptions.Start }
				.Themed(VisualElement.BackgroundColorProperty, "#E5E7EB", "#3A3A3C").Themed(Button.TextColorProperty, "#B91C1C", "#F87171");
			delete.Clicked += async (s, e) =>
			{
				if (await DisplayAlertAsync("Delete this tree?", $"{f.Common} and its photos will be removed from this phone.", "Delete", "Keep"))
				{
					FindStore.Delete(f);
					Fill();
				}
			};
			text.Add(delete);
			grid.Add(text, 1);

			var card = Ui.Card(grid);
			var sp = TreeEngine.Find(f.Scientific);
			if (sp != null) card.OnTap(() => Navigation.PushAsync(new SpeciesPage(sp)));
			_list.Add(card);
		}
	}
}
