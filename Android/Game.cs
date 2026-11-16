using TreeId;

namespace TreeMobile;

/// <summary>Shared look for the tree book game: rarity colors, badge icons, rank bar.</summary>
public static class Game
{
	public static (string light, string dark) RarityColor(Rarity r) => r switch
	{
		Rarity.Rare => ("#7C3AED", "#C4B5FD"),
		Rarity.Uncommon => ("#1D4ED8", "#93C5FD"),
		_ => ("#2E6B3A", "#8FD19E"),
	};

	public static Label RarityChip(Rarity r)
	{
		var (light, dark) = RarityColor(r);
		return new Label { Text = r.ToString().ToUpperInvariant(), FontSize = 11, FontAttributes = FontAttributes.Bold, CharacterSpacing = 1 }
			.Themed(Label.TextColorProperty, light, dark);
	}

	public static string BadgeIcon(string id) => id switch
	{
		"first" => "🌱",
		"collector" => "📚",
		"oaks" => "🌳",
		"winter" => "❄️",
		"bark" => "🪵",
		"invader" => "🚨",
		"safety" => "🛡️",
		_ => "⭐",
	};

	/// <summary>Rank name, leaves total and a bar toward the next rank.</summary>
	public static View RankPanel(ProgressSummary p)
	{
		double progress = 1;
		string next = "Top rank reached!";
		if (p.NextRank != null)
		{
			progress = (double)(p.Points - p.Rank.MinPoints) / (p.NextRank.MinPoints - p.Rank.MinPoints);
			next = $"{p.NextRank.MinPoints - p.Points} more leaves to {p.NextRank.Name}";
		}
		return Ui.Card(new VerticalStackLayout
		{
			Spacing = 4,
			Children =
			{
				new Label { Text = p.Rank.Name, FontSize = 22, FontAttributes = FontAttributes.Bold }.AccentText(),
				Ui.Body($"🍃 {p.Points} leaves  ·  {p.Collection.Count} of {p.SpeciesCount} trees found"),
				new ProgressBar { Progress = progress, Margin = new Thickness(0, 4) }.Themed(ProgressBar.ProgressColorProperty, "#2E6B3A", "#8FD19E"),
				Ui.Note(next),
			},
		});
	}
}
