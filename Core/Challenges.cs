namespace TreeId;

/// <summary>One weekly challenge and whether it's done.</summary>
public sealed class Challenge
{
	public string Id { get; init; }
	public string Title { get; init; }
	public string Hint { get; init; }
	public int Points { get; init; } = Challenges.ChallengePoints;
	public bool Done { get; init; }
	public DateTime WeekStart { get; init; }
}

/// <summary>
/// Three challenges a week, built from the pack's traits and look-alike pairs. The set for a week is picked
/// with a fixed seed, so it's the same every time the app opens and needs no storage. In winter
/// (December to February), challenges that need leaves on the tree are left out.
/// A challenge is done when a verified find made that week satisfies it.
/// </summary>
public static class Challenges
{
	public const int ChallengePoints = 15;

	sealed record Def(string Id, string Title, string Hint, bool NeedsLeaves,
		Func<Pack, List<Find>, ISet<string>, bool> IsDone);

	static readonly (string trait, string value, string title, string hint, bool leaves)[] TraitTemplates =
	{
		("arrangement", "opposite", "Find a tree whose leaves grow in pairs",
			"Look where leaves or buds join the twig: two at the same spot, facing each other.", false),
		("thorns", "yes", "Find a tree with thorns",
			"Look along the twigs, but don't touch the sharp parts!", false),
		("evergreen", "yes", "Find a tree that stays green all winter",
			"Many trees go bare in winter. Find one that keeps its leaves.", false),
		("evergreen", "no", "Find a tree that loses its leaves in winter",
			"Most trees here drop their leaves in fall. Find one of them.", false),
		("lobed", "yes", "Find a tree with lobed leaves",
			"Look for leaves with deep cuts, like fingers or a star.", true),
		("leaf", "pinnate", "Find a tree with leaflets",
			"Look for a leaf made of many small leaflets along one stem.", true),
		("leaf", "bipinnate", "Find a tree with feathery leaves",
			"Look for tiny leaflets on branching stems, like a feather.", true),
		("arrangement", "needles", "Find a tree with needles",
			"Pines and bald cypress have needles instead of flat leaves.", false),
		("arrangement", "scale", "Find a tree with tiny scale leaves",
			"Junipers have leaves like tiny overlapping scales.", false),
	};

	public static DateTime WeekStart(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));

	/// <param name="verified">All verified finds, oldest first.</param>
	public static List<Challenge> ForWeek(Pack pack, DateTime anyDayInWeek, List<Find> verified)
	{
		var start = WeekStart(anyDayInWeek);
		var end = start.AddDays(7);
		var thisWeek = verified.Where(f => f.When >= start && f.When < end).ToList();
		var collectedByEnd = verified.Where(f => f.When < end).Select(f => f.Scientific).ToHashSet();
		return Pick(pack, start).Select(d => new Challenge
		{
			Id = d.Id, Title = d.Title, Hint = d.Hint, WeekStart = start,
			Done = d.IsDone(pack, thisWeek, collectedByEnd),
		}).ToList();
	}

	static List<Def> Pick(Pack pack, DateTime weekStart)
	{
		bool winter = weekStart.Month is 12 or 1 or 2;
		var rng = new Random(weekStart.Year * 1000 + weekStart.DayOfYear);

		var traits = AllTraitDefs(pack).Where(d => !winter || !d.NeedsLeaves).OrderBy(_ => rng.Next()).ToList();
		var skills = SkillDefs().OrderBy(_ => rng.Next()).ToList();
		var pairs = PairDefs(pack).OrderBy(_ => rng.Next()).ToList();

		// One about tree features, one photo or collecting skill, and a third from features or look-alikes
		var picks = new List<Def> { traits[0], skills[0] };
		// The second feature challenge must be about a different trait (not "leaflets" and "feathery leaves" together)
		string TraitKey(Def d) => d.Id.Split('=')[0];
		var otherTrait = traits.First(d => TraitKey(d) != TraitKey(traits[0]));
		var third = rng.Next(2) == 0 && pairs.Count > 0 ? pairs[0] : otherTrait;
		picks.Add(third);
		return picks;
	}

	static IEnumerable<Def> AllTraitDefs(Pack pack) =>
		TraitTemplates
			.Where(t => pack.Species.Any(s => s.Trait(t.trait) == t.value))
			.Select(t => new Def($"trait:{t.trait}={t.value}", t.title, t.hint, t.leaves,
				(p, week, _) => week.Any(f => p.Species.FirstOrDefault(s => s.Scientific == f.Scientific)?.Trait(t.trait) == t.value)));

	static IEnumerable<Def> SkillDefs() => new[]
	{
		new Def("skill:parts3", "Photograph 3 different parts of one tree",
			"Try a leaf, the bark and the whole tree. It also makes answers more accurate.", false,
			(_, week, _) => week.Any(f => f.Parts.Distinct().Count() >= 3)),
		new Def("skill:bark", "Use a bark photo to identify a tree",
			"Bark is tricky! Add a bark photo along with at least one other photo.", false,
			(_, week, _) => week.Any(f => f.Parts.Contains(Part.Bark) && f.Parts.Distinct().Count() >= 2)),
		new Def("skill:new", "Find a tree that's new to your tree book",
			"Look for a tree you haven't found before.", false,
			(_, week, _) => week.Any(f => f.IsFirstOfSpecies)),
	};

	/// <summary>Look-alike pairs where both trees are in the pack: find both (at least one this week).</summary>
	static IEnumerable<Def> PairDefs(Pack pack)
	{
		var seen = new HashSet<string>();
		foreach (var a in pack.Species)
			foreach (var pair in a.Lookalikes)
			{
				var b = pack.Species.FirstOrDefault(s => s.Scientific == pair[0]);
				if (b == null) continue;
				var key = string.CompareOrdinal(a.Scientific, b.Scientific) < 0 ? $"{a.Scientific}|{b.Scientific}" : $"{b.Scientific}|{a.Scientific}";
				if (!seen.Add(key)) continue;
				var (x, y) = (a, b);
				yield return new Def($"pair:{key}", $"Find both {x.Common} and {y.Common}",
					"They look alike! Check the species pages to learn how to tell them apart.", false,
					(_, week, collected) => collected.Contains(x.Scientific) && collected.Contains(y.Scientific)
						&& week.Any(f => f.Scientific == x.Scientific || f.Scientific == y.Scientific));
			}
	}
}
