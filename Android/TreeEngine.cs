using TreeId;

namespace TreeMobile;

/// <summary>Loads the region pack once and runs the model on photos, off the main thread.</summary>
public static class TreeEngine
{
	static Task _loading;
	static Classifier _model;
	static readonly object _modelLock = new();

	public static Pack Pack { get; private set; }
	public static Identifier Identifier { get; private set; }

	public static Task LoadAsync() => _loading ??= Task.Run(async () =>
	{
		using (var json = await FileSystem.OpenAppPackageFileAsync("pack/pack.json"))
			Pack = TreeId.Pack.Load(json);
		using var model = await FileSystem.OpenAppPackageFileAsync("pack/" + Pack.Model);
		using var buffer = new MemoryStream();
		await model.CopyToAsync(buffer);
		_model = new Classifier(buffer.ToArray(), Pack.InputSize);
		Identifier = new Identifier(Pack);
	});

	/// <summary>The model's log-probabilities for one photo.</summary>
	public static async Task<float[]> ClassifyAsync(string photoPath)
	{
		await LoadAsync();
		return await Task.Run(() =>
		{
			// Decode at about twice the resize target so downscaling stays smooth.
			int minShorter = (int)(Pack.InputSize * Pack.ResizeRatio) * 2;
			var (rgb, w, h) = PhotoDecoder.DecodeForModel(photoPath, minShorter);
			var tensor = Preprocess.ToTensor(rgb, w, h, Pack);
			lock (_modelLock) return _model.LogProbs(tensor);
		});
	}

	public static SpeciesInfo Find(string scientific) => Pack?.Species.FirstOrDefault(s => s.Scientific == scientific);

	public static string PartName(Part part) => part switch
	{
		Part.Leaf => "leaf",
		Part.Bark => "bark",
		Part.BudsTwig => "buds and twig",
		Part.FruitFlower => "fruit or flower",
		_ => "whole tree",
	};
}
