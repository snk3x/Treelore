using System.Text.Json;
using System.Text.Json.Serialization;
using TreeId;

namespace TreeMobile;

/// <summary>Saved finds, kept in a JSON file with their photos in the app's private storage.</summary>
public static class FindStore
{
	static string Folder => FileSystem.AppDataDirectory;
	static string FilePath => Path.Combine(Folder, "finds.json");
	public static string PhotoFolder => Path.Combine(Folder, "photos");

	public static List<Find> Load()
	{
		try
		{
			return File.Exists(FilePath)
				? JsonSerializer.Deserialize(File.ReadAllText(FilePath), FindJson.Default.ListFind) ?? new()
				: new();
		}
		catch { return new(); }
	}

	public static void Add(Find find)
	{
		var all = Load();
		all.Insert(0, find);
		Save(all);
	}

	public static void Delete(Find find)
	{
		var all = Load();
		all.RemoveAll(f => f.Id == find.Id);
		Save(all);
		foreach (var p in find.Photos)
			try { File.Delete(p); } catch { }
	}

	static void Save(List<Find> all)
	{
		var tmp = FilePath + ".tmp";
		File.WriteAllText(tmp, JsonSerializer.Serialize(all, FindJson.Default.ListFind));
		File.Move(tmp, FilePath, overwrite: true);
	}

	/// <summary>Species whose page the user has opened, for the Safety Smart badge.</summary>
	public static HashSet<string> Viewed()
	{
		var raw = Preferences.Default.Get("viewedSpecies", "");
		return raw.Split('|', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
	}

	public static void MarkViewed(string scientific)
	{
		var all = Viewed();
		if (all.Add(scientific)) Preferences.Default.Set("viewedSpecies", string.Join("|", all));
	}

	public static ProgressSummary Progress() =>
		TreeId.Progress.Compute(TreeEngine.Pack, Load(), Viewed());
}

[JsonSerializable(typeof(List<Find>))]
internal partial class FindJson : JsonSerializerContext { }
