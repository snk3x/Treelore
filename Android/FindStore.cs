using System.Text.Json;
using System.Text.Json.Serialization;

namespace TreeMobile;

/// <summary>A tree the user identified and saved.</summary>
public sealed class Find
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	public DateTime When { get; set; } = DateTime.Now;
	public double? Lat { get; set; }
	public double? Lng { get; set; }
	public string Scientific { get; set; }
	public string Common { get; set; }
	public double Confidence { get; set; }
	public List<string> Photos { get; set; } = new();
}

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
}

[JsonSerializable(typeof(List<Find>))]
internal partial class FindJson : JsonSerializerContext { }
