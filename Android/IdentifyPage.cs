using TreeId;

namespace TreeMobile;

/// <summary>
/// Take or pick up to several photos of one tree (leaf, bark, buds, fruit, whole tree). After each photo the
/// answer updates; when two species are close, a quick question about the tree settles it.
/// </summary>
public class IdentifyPage : ContentPage
{
	const int MaxPhotos = 5;

	readonly List<Shot> _shots = new();
	readonly Dictionary<string, string> _answers = new();
	Part _part = Part.Leaf;
	double? _lat, _lng;
	string _locationSource;
	bool _busy;

	readonly FlexLayout _partChips = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Start };
	readonly HorizontalStackLayout _thumbs = new() { Spacing = 8 };
	readonly VerticalStackLayout _results = new() { Spacing = 10 };
	readonly Label _hint = Ui.Note();
	readonly Label _location = Ui.Note();
	readonly ActivityIndicator _spinner = new() { IsRunning = false, IsVisible = false, HeightRequest = 28 };

	public IdentifyPage()
	{
		Title = "Identify";
		var take = Ui.Primary("Take photo", () => AddPhoto(camera: true));
		var pick = Ui.Secondary("From gallery", () => AddPhoto(camera: false));
		var startOver = Ui.Secondary("Start over", () => { Reset(); return Task.CompletedTask; });

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(16, 8, 16, 24),
				Spacing = 10,
				Children =
				{
					Ui.Title("Identify a tree"),
					Ui.Note("Photograph different parts of the same tree. Three photos (for example a leaf, the bark and the whole tree) give the best answer."),
					Ui.Heading("What are you photographing?"),
					_partChips,
					Ui.Row(take, pick),
					new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = _thumbs },
					_hint,
					_spinner,
					_results,
					_location,
					startOver,
				},
			},
		};
		Refresh();
	}

	async Task AddPhoto(bool camera)
	{
		if (_busy) return;
		if (_shots.Count >= MaxPhotos)
		{
			await DisplayAlertAsync("Enough photos", $"Up to {MaxPhotos} photos can be used for one tree. Start over to identify another tree.", "OK");
			return;
		}

		FileResult file;
		try
		{
			if (camera && !MediaPicker.Default.IsCaptureSupported)
			{
				await DisplayAlertAsync("No camera", "This phone has no camera the app can use. Pick a photo from the gallery instead.", "OK");
				return;
			}
			file = camera ? await MediaPicker.Default.CapturePhotoAsync() : (await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 }))?.FirstOrDefault();
		}
		catch (PermissionException)
		{
			await DisplayAlertAsync("Camera permission needed", "Allow camera access in the phone's settings to take photos.", "OK");
			return;
		}
		if (file == null) return;

		_busy = true;
		_spinner.IsVisible = _spinner.IsRunning = true;
		_hint.Text = "Looking at your photo...";
		try
		{
			var local = Path.Combine(FileSystem.CacheDirectory, $"shot_{Guid.NewGuid():N}.jpg");
			using (var source = await file.OpenReadAsync())
			using (var dest = File.Create(local))
				await source.CopyToAsync(dest);

			var logProbs = await TreeEngine.ClassifyAsync(local);
			_shots.Add(new Shot { Part = _part, LogProbs = logProbs, PhotoPath = local });
			if (_lat == null) await FindLocation(local);
			_part = SuggestNextPart();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Couldn't read that photo", ex.Message, "OK");
		}
		finally
		{
			_busy = false;
			_spinner.IsVisible = _spinner.IsRunning = false;
			Refresh();
		}
	}

	/// <summary>Uses the photo's own GPS position if it has one, otherwise asks the phone where it is.</summary>
	async Task FindLocation(string photo)
	{
		var gps = PhotoDecoder.GpsOf(photo);
		if (gps != null)
		{
			(_lat, _lng) = gps.Value;
			_locationSource = "the photo's location";
			return;
		}
		try
		{
			if (await Permissions.RequestAsync<Permissions.LocationWhenInUse>() != PermissionStatus.Granted) return;
			var loc = await Geolocation.Default.GetLastKnownLocationAsync()
				?? await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Low, TimeSpan.FromSeconds(6)));
			if (loc != null)
			{
				(_lat, _lng) = (loc.Latitude, loc.Longitude);
				_locationSource = "your current location";
			}
		}
		catch { /* location is optional; identify without it */ }
	}

	/// <summary>Suggests a part not photographed yet: leaf and bark first, buds in winter, fruit otherwise.</summary>
	Part SuggestNextPart()
	{
		bool winter = DateTime.Now.Month is 12 or 1 or 2;
		var order = winter
			? new[] { Part.BudsTwig, Part.Bark, Part.WholeTree, Part.Leaf, Part.FruitFlower }
			: new[] { Part.Leaf, Part.Bark, Part.WholeTree, Part.FruitFlower, Part.BudsTwig };
		return order.FirstOrDefault(p => _shots.All(s => s.Part != p), _part);
	}

	void Reset()
	{
		_shots.Clear();
		_answers.Clear();
		_lat = _lng = null;
		_locationSource = null;
		_part = DateTime.Now.Month is 12 or 1 or 2 ? Part.BudsTwig : Part.Leaf;
		Refresh();
	}

	void Refresh()
	{
		_partChips.Children.Clear();
		foreach (var part in Enum.GetValues<Part>())
		{
			var p = part;
			var chip = Ui.Chip(char.ToUpper(TreeEngine.PartName(p)[0]) + TreeEngine.PartName(p)[1..], p == _part,
				() => { _part = p; Refresh(); return Task.CompletedTask; });
			chip.Margin = new Thickness(0, 0, 6, 6);
			_partChips.Children.Add(chip);
		}

		_thumbs.Children.Clear();
		for (int i = 0; i < _shots.Count; i++)
		{
			var shot = _shots[i];
			_thumbs.Children.Add(new VerticalStackLayout
			{
				Spacing = 2,
				Children =
				{
					new Image { Source = ImageSource.FromFile(shot.PhotoPath), WidthRequest = 84, HeightRequest = 84, Aspect = Aspect.AspectFill },
					new Label { Text = TreeEngine.PartName(shot.Part), FontSize = 12, HorizontalTextAlignment = TextAlignment.Center }.MutedText(),
				},
			}.OnTap(async () =>
			{
				if (await DisplayAlertAsync("Remove photo?", $"Remove this {TreeEngine.PartName(shot.Part)} photo?", "Remove", "Keep"))
				{
					_shots.Remove(shot);
					Refresh();
				}
			}));
		}

		_results.Children.Clear();
		_location.Text = _lat == null ? "" : $"Using {_locationSource} to favor trees that grow nearby.";
		if (_shots.Count == 0)
		{
			_hint.Text = "Start with a clear photo of a leaf, filling most of the frame.";
			return;
		}
		_hint.Text = _shots.Count < 3 ? $"Next: add a {TreeEngine.PartName(_part)} photo to improve the answer. Tap a photo to remove it." : "Tap a photo to remove it.";

		var result = TreeEngine.Identifier.Identify(_shots, _lat, _lng, _answers);
		var top = result.Top[0];
		string verdict = top.Probability >= Identifier.ConfidentAt ? "Likely match"
			: top.Probability >= 0.5 ? "Possible match" : "Not sure yet";
		_results.Children.Add(Ui.Heading(verdict));

		foreach (var c in result.Top.Where(c => c.Probability >= 0.02))
			_results.Children.Add(CandidateCard(c));

		if (result.NextQuestion != null) _results.Children.Add(QuestionCard(result.NextQuestion));

		_results.Children.Add(Ui.Primary($"Save as {top.Species.Common}", () => Save(top)));
		_results.Children.Add(Ui.Note("Never eat or use a wild plant based on this app's answer."));
	}

	View CandidateCard(Candidate c)
	{
		var sp = c.Species;
		var lines = new VerticalStackLayout
		{
			Spacing = 2,
			Children =
			{
				new Label { Text = $"{sp.Common}  ·  {c.Probability:P0}", FontSize = 18, FontAttributes = FontAttributes.Bold },
				new Label { Text = sp.Scientific, FontSize = 14, FontAttributes = FontAttributes.Italic }.MutedText(),
				new ProgressBar { Progress = c.Probability, Margin = new Thickness(0, 4) }.Themed(ProgressBar.ProgressColorProperty, "#2E6B3A", "#8FD19E"),
			},
		};
		if (_shots.Count > 1 && c.BestShot >= 0)
			lines.Add(Ui.Note($"Strongest clue: your {TreeEngine.PartName(_shots[c.BestShot].Part)} photo"));
		foreach (var warning in sp.Notes.Where(IsWarning))
			lines.Add(new Label { Text = "⚠ " + warning, FontSize = 13 }.WarnText());
		lines.Add(Ui.Note("Tap for details and look-alikes"));
		return Ui.Card(lines).OnTap(() => Navigation.PushAsync(new SpeciesPage(sp)));
	}

	View QuestionCard(Question q)
	{
		var stack = new VerticalStackLayout { Spacing = 8, Children = { Ui.Heading("Quick question"), Ui.Body(q.Text) } };
		foreach (var option in q.Options)
		{
			string value = option[0];
			stack.Add(Ui.Secondary(option[1], () => { _answers[q.Trait] = value; Refresh(); return Task.CompletedTask; }));
		}
		stack.Add(Ui.Secondary("Not sure", () => { _answers[q.Trait] = ""; Refresh(); return Task.CompletedTask; }));
		return Ui.Card(stack);
	}

	async Task Save(Candidate c)
	{
		try
		{
			Directory.CreateDirectory(FindStore.PhotoFolder);
			var find = new Find { Scientific = c.Species.Scientific, Common = c.Species.Common, Confidence = c.Probability, Lat = _lat, Lng = _lng };
			foreach (var shot in _shots)
			{
				var dest = Path.Combine(FindStore.PhotoFolder, $"{find.Id}_{find.Photos.Count}.jpg");
				await Task.Run(() => PhotoDecoder.SaveCopy(shot.PhotoPath, dest));
				find.Photos.Add(dest);
			}
			FindStore.Add(find);
			await DisplayAlertAsync("Saved", $"{c.Species.Common} was added to My trees.", "OK");
			Reset();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Couldn't save", ex.Message, "OK");
		}
	}

	internal static bool IsWarning(string note) =>
		note.Contains("toxic", StringComparison.OrdinalIgnoreCase) || note.Contains("poison", StringComparison.OrdinalIgnoreCase)
		|| note.Contains("vomit", StringComparison.OrdinalIgnoreCase) || note.StartsWith("Invasive", StringComparison.OrdinalIgnoreCase);
}
