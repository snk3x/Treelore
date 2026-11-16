using System.Text.Json;
using System.Text.Json.Serialization;

namespace TreeId;

/// <summary>A region pack: the species it covers, the model settings, follow-up questions and range data.</summary>
public sealed class Pack
{
	public string Region { get; set; }
	public int Version { get; set; }
	public string Model { get; set; }
	public int InputSize { get; set; }
	public double ResizeRatio { get; set; }
	public float[] Mean { get; set; }
	public float[] Std { get; set; }
	public double PriorWeight { get; set; }
	public List<SpeciesInfo> Species { get; set; }
	public List<Question> Questions { get; set; }
	/// <summary>Training-photo counts per species, keyed by "floor(lat),floor(lng)". Used for the location prior.</summary>
	public Dictionary<string, int[]> Range { get; set; }
	/// <summary>Real research-grade observation counts per species and cell, used for rarity. May be null.</summary>
	public Dictionary<string, int[]> Abundance { get; set; }

	public static Pack Load(Stream json) => JsonSerializer.Deserialize(json, PackJson.Default.Pack);
	public static Pack Load(string json) => JsonSerializer.Deserialize(json, PackJson.Default.Pack);
}

public sealed class SpeciesInfo
{
	public string Scientific { get; set; }
	public string Common { get; set; }
	public string Family { get; set; }
	public Dictionary<string, string> Traits { get; set; } = new();
	public List<string> Features { get; set; } = new();
	/// <summary>Pairs of [scientific name, how to tell them apart].</summary>
	public List<List<string>> Lookalikes { get; set; } = new();
	public List<string> Notes { get; set; } = new();

	public string Trait(string key) => Traits.TryGetValue(key, out var v) ? v : null;

	/// <summary>Notes about poison, toxicity or invasiveness, shown before anything else.</summary>
	public IEnumerable<string> Warnings => Notes.Where(IsWarning);
	public bool IsPoisonous => Notes.Any(n => n.Contains("toxic", StringComparison.OrdinalIgnoreCase)
		|| n.Contains("poison", StringComparison.OrdinalIgnoreCase) || n.Contains("vomit", StringComparison.OrdinalIgnoreCase));
	public bool IsInvasive => Notes.Any(n => n.StartsWith("Invasive", StringComparison.OrdinalIgnoreCase));

	public static bool IsWarning(string note) =>
		note.Contains("toxic", StringComparison.OrdinalIgnoreCase) || note.Contains("poison", StringComparison.OrdinalIgnoreCase)
		|| note.Contains("vomit", StringComparison.OrdinalIgnoreCase) || note.StartsWith("Invasive", StringComparison.OrdinalIgnoreCase);
}

public sealed class Question
{
	public string Trait { get; set; }
	public string Text { get; set; }
	/// <summary>Pairs of [trait value, label shown to the user].</summary>
	public List<List<string>> Options { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Pack))]
internal partial class PackJson : JsonSerializerContext { }
