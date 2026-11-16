namespace TreeMobile;

/// <summary>Every species in the region pack, searchable by common or scientific name.</summary>
public class SpeciesListPage : ContentPage
{
	readonly VerticalStackLayout _list = new() { Spacing = 8 };
	readonly SearchBar _search = new() { Placeholder = "Search by name" };
	readonly Label _header = Ui.Note();

	public SpeciesListPage()
	{
		Title = "Species";
		_search.TextChanged += (s, e) => Fill();
		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(16, 8, 16, 24),
				Spacing = 10,
				Children = { Ui.Title("Species"), _header, _search, _list },
			},
		};
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await TreeEngine.LoadAsync();
		_header.Text = $"{TreeEngine.Pack.Species.Count} trees in the {TreeEngine.Pack.Region} pack";
		Fill();
	}

	void Fill()
	{
		if (TreeEngine.Pack == null) return;
		var q = _search.Text?.Trim() ?? "";
		_list.Children.Clear();
		foreach (var sp in TreeEngine.Pack.Species
			.Where(s => q.Length == 0 || s.Common.Contains(q, StringComparison.OrdinalIgnoreCase) || s.Scientific.Contains(q, StringComparison.OrdinalIgnoreCase))
			.OrderBy(s => s.Common))
		{
			_list.Add(Ui.Card(new VerticalStackLayout
			{
				Children =
				{
					new Label { Text = sp.Common, FontSize = 16, FontAttributes = FontAttributes.Bold },
					new Label { Text = sp.Scientific, FontSize = 13, FontAttributes = FontAttributes.Italic }.MutedText(),
				},
			}).OnTap(() => Navigation.PushAsync(new SpeciesPage(sp))));
		}
	}
}
