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
					Ui.Title("About Treelore"),
					_pack,
					Ui.Heading("Kid mode"),
					KidModeSwitch(),
					Ui.Note("Simpler words, bigger buttons, the Tree book game with leaves, badges and weekly challenges. A grown-up answers a quick question to turn it off."),
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

	View KidModeSwitch()
	{
		var sw = new Switch { IsToggled = Settings.KidMode };
		bool reverting = false;
		sw.Toggled += async (s, e) =>
		{
			if (reverting) return;
			if (!e.Value && !await GrownUpCheck())
			{
				reverting = true;
				sw.IsToggled = true;
				reverting = false;
				return;
			}
			Settings.KidMode = e.Value;
			Settings.Reload();
		};
		return new HorizontalStackLayout
		{
			Spacing = 8,
			Children = { sw, new Label { Text = "Kid mode", FontSize = 16, VerticalOptions = LayoutOptions.Center } },
		};
	}

	/// <summary>A multiplication question most young kids can't answer yet: a light lock, not real security.</summary>
	async Task<bool> GrownUpCheck()
	{
		var rng = new Random();
		int a = rng.Next(6, 10), b = rng.Next(6, 10);
		var answer = await DisplayPromptAsync("Grown-ups only", $"To turn off kid mode, what is {a} × {b}?",
			"OK", "Cancel", keyboard: Keyboard.Numeric, maxLength: 3);
		if (answer == null) return false;
		if (int.TryParse(answer.Trim(), out var n) && n == a * b) return true;
		await DisplayAlertAsync("Not quite", "Kid mode is staying on.", "OK");
		return false;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await TreeEngine.LoadAsync();
		_pack.Text = $"Region pack: {TreeEngine.Pack.Region}, {TreeEngine.Pack.Species.Count} trees.";
	}
}
