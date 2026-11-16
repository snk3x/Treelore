using TreeId;

namespace TreeMobile;

/// <summary>One species: how to recognize it, look-alikes and how to tell them apart, safety notes.</summary>
public class SpeciesPage : ContentPage
{
	public SpeciesPage(SpeciesInfo sp)
	{
		Title = sp.Common;
		var stack = new VerticalStackLayout
		{
			Padding = new Thickness(16, 8, 16, 24),
			Spacing = 8,
			Children =
			{
				Ui.Title(sp.Common),
				new Label { Text = sp.Scientific, FontSize = 16, FontAttributes = FontAttributes.Italic }.MutedText(),
				Ui.Note(sp.Family),
			},
		};

		foreach (var note in sp.Notes)
			stack.Add(SpeciesInfo.IsWarning(note) ? new Label { Text = "⚠ " + note, FontSize = 15 }.WarnText() : Ui.Body(note));
		// Reading a poisonous tree's page counts toward the Safety Smart badge
		FindStore.MarkViewed(sp.Scientific);

		stack.Add(Ui.Heading("How to recognize it"));
		foreach (var f in sp.Features) stack.Add(Ui.Bullet(f));

		if (sp.Lookalikes.Count > 0)
		{
			stack.Add(Ui.Heading("Look-alikes"));
			foreach (var pair in sp.Lookalikes)
			{
				var other = TreeEngine.Find(pair[0]);
				var card = Ui.Card(new VerticalStackLayout
				{
					Spacing = 2,
					Children =
					{
						new Label { Text = other?.Common ?? pair[0], FontSize = 16, FontAttributes = FontAttributes.Bold },
						Ui.Body(pair[1]),
					},
				});
				if (other != null) card.OnTap(() => Navigation.PushAsync(new SpeciesPage(other)));
				stack.Add(card);
			}
		}

		Content = new ScrollView { Content = stack };
	}
}
