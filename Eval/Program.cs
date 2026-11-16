using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using TreeId;

// Usage: Eval <pack folder> <data folder> <test_obs.txt> [max photos]
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
