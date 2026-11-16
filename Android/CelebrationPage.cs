using TreeId;

namespace TreeMobile;

/// <summary>
/// Shown after saving a find the app is sure about. Safety warnings come first, before any celebrating;
/// then the card, the leaves earned, new badges and a new rank.
/// </summary>
public class CelebrationPage : ContentPage
{
	public CelebrationPage(SpeciesInfo sp, Find find, Award award)
	{
		BackgroundColor = Color.FromArgb("#1F4D29");
		var stack = new VerticalStackLayout { Padding = new Thickness(20, 32, 20, 24), Spacing = 14 };

		foreach (var warning in sp.Warnings)
			stack.Add(new Border
			{
				BackgroundColor = Color.FromArgb("#FEF3C7"),
				Padding = 12,
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
				Content = new Label { Text = "⚠ " + warning, FontSize = 16, TextColor = Color.FromArgb("#92400E") },
			});

		stack.Add(new Label
		{
			Text = award.NewSpecies ? "New tree found!" : "Found again!",
			FontSize = 30, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center,
		});

		var (light, _) = Game.RarityColor(award.Rarity);
		var card = new Border
		{
			BackgroundColor = Colors.White,
			Stroke = new SolidColorBrush(Color.FromArgb(light)),
			StrokeThickness = 4,
			Padding = 12,
			StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
			Scale = 0.3,
			Opacity = 0,
			Content = new VerticalStackLayout
			{
				Spacing = 4,
				Children =
				{
					new Image { Source = find.Photos.Count > 0 ? ImageSource.FromFile(find.Photos[0]) : "tree_hidden.png", HeightRequest = 220, Aspect = Aspect.AspectFill },
					new Label { Text = sp.Common, FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827") },
					new Label { Text = sp.Scientific, FontSize = 14, FontAttributes = FontAttributes.Italic, TextColor = Color.FromArgb("#6B7280") },
					new Label { Text = award.Rarity.ToString().ToUpperInvariant(), FontSize = 13, FontAttributes = FontAttributes.Bold, CharacterSpacing = 1, TextColor = Color.FromArgb(light) },
					new Label { Text = sp.Features.FirstOrDefault() ?? "", FontSize = 14, TextColor = Color.FromArgb("#374151") },
				},
			},
		};
		stack.Add(card);

		string leaves = $"+{award.Points} leaves 🍃";
		if (award.PhotoBonus > 0) leaves += $"\nincluding +{award.PhotoBonus} for photographing 3 different parts";
		stack.Add(new Label { Text = leaves, FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#BBF7D0"), HorizontalTextAlignment = TextAlignment.Center });

		foreach (var b in award.NewBadges)
			stack.Add(new Border
			{
				BackgroundColor = Color.FromArgb("#2E6B3A"),
				Padding = 12,
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
				Content = new Label { Text = $"{Game.BadgeIcon(b.Id)}  New badge: {b.Name}\n{b.Description}", FontSize = 16, TextColor = Colors.White },
			});

		if (award.NewRank != null)
			stack.Add(new Label { Text = $"You're now a {award.NewRank.Name}!", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#FDE68A"), HorizontalTextAlignment = TextAlignment.Center });

		var p = award.After;
		stack.Add(new Label { Text = $"{p.Collection.Count} of {p.SpeciesCount} trees in your tree book", FontSize = 15, TextColor = Color.FromArgb("#D1FAE5"), HorizontalTextAlignment = TextAlignment.Center });

		var done = new Button { Text = "Awesome!", FontSize = 18, FontAttributes = FontAttributes.Bold, BackgroundColor = Colors.White, TextColor = Color.FromArgb("#1F4D29"), HeightRequest = 54 };
		done.Clicked += async (s, e) => await Navigation.PopModalAsync();
		stack.Add(done);

		Content = new ScrollView { Content = stack };
		Loaded += async (s, e) =>
		{
			await Task.WhenAll(card.FadeToAsync(1, 250), card.ScaleToAsync(1.08, 350, Easing.CubicOut));
			await card.ScaleToAsync(1, 150, Easing.CubicIn);
		};
	}
}
