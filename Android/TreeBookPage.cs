using TreeId;

namespace TreeMobile;

/// <summary>
/// The collection: every tree in the pack as a card. Found trees show the player's own photo and rarity;
/// trees not found yet are a shadow with a hint. Rank and badges sit at the top.
/// </summary>
public class TreeBookPage : ContentPage
{
	readonly VerticalStackLayout _content = new() { Padding = new Thickness(16, 8, 16, 24), Spacing = 12 };

	public TreeBookPage()
	{
		Title = "Tree book";
		Content = new ScrollView { Content = _content };
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await TreeEngine.LoadAsync();
		Fill();
	}

	void Fill()
	{
		var p = FindStore.Progress();
		_content.Children.Clear();
		_content.Add(Ui.Title("Tree book"));
		_content.Add(Game.RankPanel(p));

		int daysLeft = (Challenges.WeekStart(DateTime.Now).AddDays(7) - DateTime.Now.Date).Days;
		_content.Add(Ui.Heading("This week's challenges"));
		_content.Add(Ui.Note($"{p.ThisWeek.Count(c => c.Done)} of {p.ThisWeek.Count} done · new challenges in {daysLeft} {(daysLeft == 1 ? "day" : "days")}"));
		foreach (var ch in p.ThisWeek)
		{
			var c = ch;
			_content.Add(Ui.Card(new Grid
			{
				ColumnDefinitions = { new(new GridLength(40)), new(GridLength.Star) },
				Children =
				{
					new Label { Text = c.Done ? "✅" : "🎯", FontSize = 26, VerticalOptions = LayoutOptions.Center },
					new VerticalStackLayout
					{
						Spacing = 2,
						Children =
						{
							new Label { Text = c.Title, FontSize = 15, FontAttributes = FontAttributes.Bold, TextDecorations = c.Done ? TextDecorations.Strikethrough : TextDecorations.None },
							Ui.Note(c.Done ? $"Done! +{c.Points} leaves" : $"+{c.Points} leaves · {c.Hint}"),
						},
					}.Column(1),
				},
			}));
		}

		_content.Add(Ui.Heading("Badges"));
		var badges = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8, RowSpacing = 8 };
		for (int i = 0; i < p.Badges.Count; i++)
		{
			var b = p.Badges[i];
			if (i % 2 == 0) badges.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			var card = Ui.Card(new VerticalStackLayout
			{
				Spacing = 2,
				Opacity = b.Earned ? 1 : 0.55,
				Children =
				{
					new Label { Text = Game.BadgeIcon(b.Id), FontSize = 28 },
					new Label { Text = b.Name, FontSize = 14, FontAttributes = FontAttributes.Bold },
					Ui.Note(b.Earned ? "Earned!" : b.Status),
				},
			}).OnTap(() => DisplayAlertAsync(b.Name, b.Description + (b.Earned ? "\n\nYou earned this one!" : $"\n\n{b.Status}"), "OK"));
			badges.Add(card, i % 2, i / 2);
		}
		_content.Add(badges);

		_content.Add(Ui.Heading("Trees"));
		_content.Add(Ui.Note("Tap a shadow for a hint about what to look for."));
		var grid = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 8, RowSpacing = 8 };
		var species = TreeEngine.Pack.Species
			.OrderByDescending(s => p.Collection.ContainsKey(s.Scientific))
			.ThenBy(s => p.Collection.TryGetValue(s.Scientific, out var c) ? c.First.When : DateTime.MaxValue)
			.ThenBy(s => s.Common)
			.ToList();
		for (int i = 0; i < species.Count; i++)
		{
			if (i % 2 == 0) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			grid.Add(TreeCard(species[i], p), i % 2, i / 2);
		}
		_content.Add(grid);
	}

	View TreeCard(SpeciesInfo sp, ProgressSummary p)
	{
		if (p.Collection.TryGetValue(sp.Scientific, out var c))
		{
			var photo = c.First.Photos.FirstOrDefault();
			var stack = new VerticalStackLayout
			{
				Spacing = 3,
				Children =
				{
					new Image { Source = photo != null ? ImageSource.FromFile(photo) : "tree_hidden.png", HeightRequest = 120, Aspect = Aspect.AspectFill },
					new Label { Text = sp.Common, FontSize = 15, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation },
					Game.RarityChip(c.BestRarity),
					Ui.Note(c.Times == 1 ? $"Found {c.First.When:MMM d}" : $"Found {c.Times} times"),
				},
			};
			var (light, dark) = Game.RarityColor(c.BestRarity);
			var card = Ui.Card(stack);
			card.SetAppTheme<Brush>(Border.StrokeProperty, new SolidColorBrush(Color.FromArgb(light)), new SolidColorBrush(Color.FromArgb(dark)));
			card.StrokeThickness = 2;
			return card.OnTap(() => Navigation.PushAsync(new SpeciesPage(sp)));
		}

		return Ui.Card(new VerticalStackLayout
		{
			Spacing = 3,
			Children =
			{
				new Image { Source = "tree_hidden.png", HeightRequest = 120, Aspect = Aspect.AspectFit },
				new Label { Text = "???", FontSize = 15, FontAttributes = FontAttributes.Bold }.MutedText(),
				Ui.Note("Not found yet"),
			},
		}).OnTap(() => DisplayAlertAsync("Mystery tree", "Hint: look for a tree with " + LowerFirst(sp.Features.FirstOrDefault() ?? "something special") + ".", "OK"));
	}

	static string LowerFirst(string s) => s.Length > 1 && char.IsUpper(s[0]) && !char.IsUpper(s[1]) ? char.ToLower(s[0]) + s[1..] : s;
}
