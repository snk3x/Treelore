using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using TreeId;

// Usage: Eval [--quick] <pack folder> <data folder> <test_obs.txt> [max photos]
// --quick runs only the logic checks, not the model on every test photo.
bool quick = args.Contains("--quick");
args = args.Where(a => a != "--quick").ToArray();
var packDir = args.Length > 0 ? args[0] : @"..\Android\Resources\Raw\pack";
var dataDir = args.Length > 1 ? args[1] : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Tree ID data");
var testObsFile = args.Length > 2 ? args[2] : @"..\training\out\test_obs.txt";
int max = args.Length > 3 ? int.Parse(args[3]) : int.MaxValue;

var pack = Pack.Load(File.ReadAllText(Path.Combine(packDir, "pack.json")));
using var model = new Classifier(File.ReadAllBytes(Path.Combine(packDir, pack.Model)), pack.InputSize, threads: 4);
var id = new Identifier(pack);
int failures = 0;

// --- Question logic checks (no images needed) ---
int Index(string name) => pack.Species.FindIndex(s => s.Scientific == name);
Shot FakeShot(params (string name, double p)[] probs)
{
	var lp = Enumerable.Repeat((float)Math.Log(1e-4), pack.Species.Count).ToArray();
	foreach (var (name, p) in probs) lp[Index(name)] = (float)Math.Log(p);
	return new Shot { Part = Part.Leaf, LogProbs = lp };
}
void Check(string what, bool ok) { Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {what}"); if (!ok) failures++; }

var walnutAsh = FakeShot(("Juglans nigra", 0.55), ("Fraxinus pennsylvanica", 0.40));
var r1 = id.Identify(new[] { walnutAsh }, null, null);
Check("walnut vs ash asks about leaf arrangement", r1.NextQuestion?.Trait == "arrangement");
var r2 = id.Identify(new[] { walnutAsh }, null, null, new Dictionary<string, string> { ["arrangement"] = "opposite" });
Check("answering 'opposite' makes green ash the top answer", r2.Top[0].Species.Scientific == "Fraxinus pennsylvanica");
Check("after answering, the result is confident", r2.Confident);

var junipers = FakeShot(("Juniperus ashei", 0.5), ("Juniperus virginiana", 0.45));
var r3 = id.Identify(new[] { junipers }, null, null);
Check("Ashe vs eastern red cedar asks about trunks", r3.NextQuestion?.Trait == "trunks");
var r4 = id.Identify(new[] { junipers }, null, null, new Dictionary<string, string> { ["trunks"] = "" });
Check("'not sure' is accepted and no question repeats", r4.NextQuestion?.Trait != "trunks");

var sure = FakeShot(("Platanus occidentalis", 0.97));
Check("a confident result asks nothing", id.Identify(new[] { sure }, null, null).NextQuestion == null);

// Hill Country (Kerrville area) should favor Ashe juniper over eastern red cedar
var prior = id.LocationPrior(30.05, -99.14);
Check("Hill Country prior favors Ashe juniper", prior[Index("Juniperus ashei")] > prior[Index("Juniperus virginiana")]);
Check("no location gives no prior", id.LocationPrior(null, null) == null);

// --- Tree book game checks ---
Console.WriteLine();
Find Make(string name, double conf, Rarity rarity, int day, params Part[] parts) => new()
{
	Scientific = name, Common = name, Confidence = conf, Rarity = rarity,
	When = new DateTime(2026, 6, 1).AddDays(day), Parts = parts.ToList(),
};
var none = new List<Find>();
var a1 = Progress.AwardFor(pack, none, Make("Quercus virginiana", 0.95, Rarity.Common, 0, Part.Leaf));
Check("first common find: new species, 10 leaves", a1.NewSpecies && a1.Points == 10);
Check("first find earns the First Find badge", a1.NewBadges.Any(b => b.Id == "first"));
var after1 = new List<Find> { Make("Quercus virginiana", 0.95, Rarity.Common, 0, Part.Leaf) };
var a2 = Progress.AwardFor(pack, after1, Make("Quercus virginiana", 0.95, Rarity.Common, 1, Part.Leaf));
Check("repeat find: not new, 2 leaves", !a2.NewSpecies && a2.Points == Progress.RepeatPoints);
var a3 = Progress.AwardFor(pack, after1, Make("Taxodium distichum", 0.95, Rarity.Rare, 1, Part.Leaf, Part.Bark, Part.WholeTree));
Check("rare find with 3 parts: 50 + 5 photo bonus", a3.Points == 55 && a3.PhotoBonus == 5);
Check("leaf + bark earns Bark Reader", a3.NewBadges.Any(b => b.Id == "bark"));
var a4 = Progress.AwardFor(pack, after1, Make("Quercus stellata", 0.60, Rarity.Common, 1, Part.Leaf));
Check("unsure find (60%) earns nothing", !a4.Counted && a4.Points == 0 && a4.NewBadges.Count == 0);
var a5 = Progress.AwardFor(pack, after1, Make("Triadica sebifera", 0.9, Rarity.Common, 1, Part.Leaf));
Check("invasive tree earns Invader Spotter", a5.NewBadges.Any(b => b.Id == "invader"));

var oakFinds = pack.Species.Where(s => s.Scientific.StartsWith("Quercus ")).Select((s, i) => Make(s.Scientific, 0.9, Rarity.Common, i, Part.Leaf)).ToList();
var oakSummary = Progress.Compute(pack, oakFinds);
Check("all 5 oaks earn Oak Squad", oakSummary.Badges.Single(b => b.Id == "oaks").Earned);
Check("5 new commons = 50 leaves = Sprout rank", oakSummary.Points == 50 && oakSummary.Rank.Name == "Sprout");
var winterFind = Make("Platanus occidentalis", 0.9, Rarity.Common, 0, Part.Bark);
winterFind.When = new DateTime(2027, 1, 10);
Check("bare tree in January earns Winter Detective", Progress.Compute(pack, new[] { winterFind }).Badges.Single(b => b.Id == "winter").Earned);
var evergreenWinter = Make("Quercus virginiana", 0.9, Rarity.Common, 0, Part.Leaf);
evergreenWinter.When = new DateTime(2027, 1, 10);
Check("evergreen live oak in January does not", !Progress.Compute(pack, new[] { evergreenWinter }).Badges.Single(b => b.Id == "winter").Earned);
var poison = pack.Species.Where(s => s.IsPoisonous).Select(s => s.Scientific).ToHashSet();
Check("reading every poisonous tree earns Safety Smart", Progress.Compute(pack, none, poison).Badges.Single(b => b.Id == "safety").Earned);

// Rarity should match what people actually see
var austin = (30.27, -97.74);
Check("live oak is common around Austin", Progress.RarityAt(pack, "Quercus virginiana", austin.Item1, austin.Item2) == Rarity.Common);
Check("Ashe juniper is common around Austin", Progress.RarityAt(pack, "Juniperus ashei", austin.Item1, austin.Item2) == Rarity.Common);
Check("planted crape myrtle is not rare around Austin", Progress.RarityAt(pack, "Lagerstroemia indica", austin.Item1, austin.Item2) != Rarity.Rare);
Check("loblolly pine is common around Houston", Progress.RarityAt(pack, "Pinus taeda", 29.76, -95.37) != Rarity.Rare);
Console.WriteLine("Rarity around Austin:  " + string.Join(", ", Enum.GetValues<Rarity>().Select(r =>
	$"{r}: {pack.Species.Count(s => Progress.RarityAt(pack, s.Scientific, austin.Item1, austin.Item2) == r)}")));
foreach (var name in new[] { "Quercus virginiana", "Juniperus ashei", "Taxodium distichum", "Quercus macrocarpa", "Pinus taeda", "Diospyros texana" })
	Console.WriteLine($"  {name,-26} Austin {Progress.RarityAt(pack, name, 30.27, -97.74),-9} Houston {Progress.RarityAt(pack, name, 29.76, -95.37),-9} Dallas {Progress.RarityAt(pack, name, 32.78, -96.80)}");

if (quick)
{
	Console.WriteLine(failures == 0 ? "\nAll checks passed." : $"\n{failures} check(s) failed.");
	return failures == 0 ? 0 : 1;
}

// --- Accuracy on held-out test photos ---
var testObs = File.ReadAllLines(testObsFile).ToHashSet();
var lines = File.ReadAllLines(Path.Combine(dataDir, "photos_tagged.csv"));
var header = lines[0].Split(',');
int col(string name) => Array.IndexOf(header, name);
var rows = lines.Skip(1).Select(l => l.Split(','))
	.Where(f => testObs.Contains(f[col("obs_id")])).Take(max).ToList();
Console.WriteLine($"\nRunning the model on {rows.Count} test photos...");

int singleHits = 0;
var byObs = new Dictionary<string, (string species, double? lat, double? lng, List<Shot> shots)>();
var sw = System.Diagnostics.Stopwatch.StartNew();
double modelMs = 0;
foreach (var f in rows)
{
	var (rgb, w, h) = Decode(Path.Combine(dataDir, f[col("path")]));
	var tensor = Preprocess.ToTensor(rgb, w, h, pack);
	var t0 = sw.Elapsed.TotalMilliseconds;
	var lp = model.LogProbs(tensor);
	modelMs += sw.Elapsed.TotalMilliseconds - t0;
	string species = f[col("species")];
	if (pack.Species[Array.IndexOf(lp, lp.Max())].Scientific == species) singleHits++;
	string obs = f[col("obs_id")];
	if (!byObs.TryGetValue(obs, out var o))
	{
		double? lat = double.TryParse(f[col("lat")], out var a) ? a : null, lng = double.TryParse(f[col("lng")], out var b) ? b : null;
		byObs[obs] = o = (species, lat, lng, new List<Shot>());
	}
	o.shots.Add(new Shot { LogProbs = lp });
}

int fused = 0, fusedPrior = 0, fusedTop3 = 0;
foreach (var o in byObs.Values)
{
	if (id.Identify(o.shots, null, null).Top[0].Species.Scientific == o.species) fused++;
	var withLoc = id.Identify(o.shots, o.lat, o.lng);
	if (withLoc.Top[0].Species.Scientific == o.species) fusedPrior++;
	if (withLoc.Top.Any(c => c.Species.Scientific == o.species)) fusedTop3++;
}
double Pct(int hits, int n) => 100.0 * hits / n;
Console.WriteLine($"Single photo top-1:            {Pct(singleHits, rows.Count):F1}%   (Python ONNX fp32: 82.1%)");
Console.WriteLine($"Sighting top-1, no location:   {Pct(fused, byObs.Count):F1}%   (Python ONNX fp32: 90.5%)");
Console.WriteLine($"Sighting top-1, with location: {Pct(fusedPrior, byObs.Count):F1}%");
Console.WriteLine($"Sighting top-3, with location: {Pct(fusedTop3, byObs.Count):F1}%");
Console.WriteLine($"Model time: {modelMs / rows.Count:F1} ms per photo (4 threads, desktop CPU)");
Console.WriteLine(failures == 0 ? "\nAll checks passed." : $"\n{failures} check(s) failed.");
return failures == 0 ? 0 : 1;

static (byte[] rgb, int w, int h) Decode(string path)
{
	using var bmp = new Bitmap(path);
	var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
	var rgb = new byte[bmp.Width * bmp.Height * 3];
	var row = new byte[data.Stride];
	for (int y = 0; y < bmp.Height; y++)
	{
		Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, data.Stride);
		for (int x = 0; x < bmp.Width; x++)
		{
			// GDI+ stores pixels as BGR
			rgb[(y * bmp.Width + x) * 3] = row[x * 3 + 2];
			rgb[(y * bmp.Width + x) * 3 + 1] = row[x * 3 + 1];
			rgb[(y * bmp.Width + x) * 3 + 2] = row[x * 3];
		}
	}
	bmp.UnlockBits(data);
	return (rgb, bmp.Width, bmp.Height);
}
