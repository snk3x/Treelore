namespace TreeMobile;

/// <summary>What the app does, how accurate it is, where the data comes from, and safety notes.</summary>
public class AboutPage : ContentPage
{
	readonly Label _pack = Ui.Body("");

	public AboutPage()
	{
		Title = "About";
		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(16, 8, 16, 24),
				Spacing = 8,
				Children =
				{
					Ui.Title("About Tree ID"),
					_pack,
					Ui.Heading("How it works"),
					Ui.Body("Everything runs on your phone, with no internet needed. Each photo is checked by a model trained on tens of thousands of tree photos. Photos of different parts of the same tree are combined, and your location favors trees that grow nearby."),
					Ui.Body("In testing, the top answer was right about 9 times out of 10 for a single sighting, and the right tree was in the top three about 97% of the time. Leaves, fruit and flowers work best; bark alone is harder."),
					Ui.Heading("Privacy"),
					Ui.Body("Photos and locations stay on this phone. Nothing is uploaded."),
					Ui.Heading("Safety"),
					new Label { Text = "Never eat, or use as medicine, any plant based on this app. Some trees here have poisonous seeds, fruit or sap.", FontSize = 15 }.WarnText(),
					Ui.Heading("Credits"),
					Ui.Body("The model was trained on photos shared by iNaturalist observers under Creative Commons licenses. Range data comes from their research-grade observations."),
					Ui.Note($"Version {AppInfo.Current.VersionString}"),
				},
			},
		};
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await TreeEngine.LoadAsync();
		_pack.Text = $"Region pack: {TreeEngine.Pack.Region}, {TreeEngine.Pack.Species.Count} trees.";
	}
}
