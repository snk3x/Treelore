namespace TreeId;

/// <summary>A tree the user identified and saved.</summary>
public sealed class Find
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
	public DateTime When { get; set; } = DateTime.Now;
	public double? Lat { get; set; }
	public double? Lng { get; set; }
	public string Scientific { get; set; }
	public string Common { get; set; }
	/// <summary>Probability of the saved answer, after any follow-up answers.</summary>
	public double Confidence { get; set; }
	public List<string> Photos { get; set; } = new();
	/// <summary>Which part each photo shows, in the same order as <see cref="Photos"/>.</summary>
	public List<Part> Parts { get; set; } = new();
	/// <summary>How rare the tree was where it was found, fixed when it was saved.</summary>
	public Rarity Rarity { get; set; }

	/// <summary>Only finds the app is sure about count toward the tree book, so kids don't learn wrong names.</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	public bool Verified => Confidence >= Identifier.ConfidentAt;

	/// <summary>Set while computing progress: true when this was the first verified find of its species.</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	public bool IsFirstOfSpecies { get; set; }
}

public enum Rarity { Common, Uncommon, Rare }

public sealed class Rank
{
	public string Name { get; init; }
	public int MinPoints { get; init; }
}

public sealed class Badge
{
	public string Id { get; init; }
	public string Name { get; init; }
	public string Description { get; init; }
	public bool Earned { get; init; }
	/// <summary>Progress toward the badge, such as "3 of 5 oaks".</summary>
	public string Status { get; init; }
}

public sealed class Collected
{
	public SpeciesInfo Species { get; init; }
	public Find First { get; init; }
	public int Times { get; init; }
	public Rarity BestRarity { get; init; }
}

public sealed class ProgressSummary
{
	public int Points { get; init; }
	public Rank Rank { get; init; }
	public Rank NextRank { get; init; }
	public Dictionary<string, Collected> Collection { get; init; }
	public List<Badge> Badges { get; init; }
	public int SpeciesCount { get; init; }
	/// <summary>Leaves earned from completed weekly challenges (included in <see cref="Points"/>).</summary>
	public int ChallengePoints { get; init; }
	/// <summary>This week's challenges.</summary>
	public List<Challenge> ThisWeek { get; init; }
	/// <summary>Ids of every completed challenge, as "week start|challenge id".</summary>
	public HashSet<string> CompletedChallenges { get; init; }
}

/// <summary>What one newly saved find earned.</summary>
public sealed class Award
{
	public bool Counted { get; init; }
	public bool NewSpecies { get; init; }
	public int Points { get; init; }
	public int PhotoBonus { get; init; }
	public Rarity Rarity { get; init; }
	public List<Badge> NewBadges { get; init; } = new();
	public List<Challenge> NewChallenges { get; init; } = new();
	public int ChallengePoints { get; init; }
	public Rank NewRank { get; init; }
	public ProgressSummary After { get; init; }
}

/// <summary>
/// The tree book game: points ("leaves") for verified finds, local rarity, ranks and badges.
/// Everything is computed from the saved finds, so nothing can drift out of sync.
/// </summary>
public static class Progress
{
	public static readonly Rank[] Ranks =
	{
		new() { Name = "Seedling", MinPoints = 0 },
		new() { Name = "Sprout", MinPoints = 50 },
		new() { Name = "Sapling", MinPoints = 150 },
		new() { Name = "Young Tree", MinPoints = 350 },
		new() { Name = "Forest Ranger", MinPoints = 700 },
	};

	public const int RepeatPoints = 2;
	public const int PhotoBonusPoints = 5;

	public static int NewSpeciesPoints(Rarity r) => r switch { Rarity.Rare => 50, Rarity.Uncommon => 25, _ => 10 };

	public static string Name(Rarity r) => r.ToString();

	/// <summary>
	/// How rare a species is around a spot (3×3 one-degree cells), ranked against the other species in the pack:
	/// the most observed 40% are common, the next 35% uncommon, the rest (and anything never seen there) rare.
	/// Without a location, the whole region's counts are used.
	/// </summary>
	public static Rarity RarityAt(Pack pack, string scientific, double? lat, double? lng)
	{
		var source = pack.Abundance ?? pack.Range;
		int n = pack.Species.Count, index = pack.Species.FindIndex(s => s.Scientific == scientific);
		if (index < 0 || source == null) return Rarity.Common;
		var counts = new double[n];
		if (lat != null && lng != null)
		{
			int cy = (int)Math.Floor(lat.Value), cx = (int)Math.Floor(lng.Value);
			for (int dy = -1; dy <= 1; dy++)
				for (int dx = -1; dx <= 1; dx++)
					if (source.TryGetValue($"{cy + dy},{cx + dx}", out var c))
						for (int i = 0; i < n; i++) counts[i] += c[i];
		}
		if (counts.Sum() == 0)
			foreach (var c in source.Values)
				for (int i = 0; i < n; i++) counts[i] += c[i];

		if (counts[index] == 0) return Rarity.Rare;
		int above = counts.Count(c => c > counts[index]);
		double position = (double)above / n;
		return position < 0.40 ? Rarity.Common : position < 0.75 ? Rarity.Uncommon : Rarity.Rare;
	}

	/// <param name="today">The day whose week's challenges to report; defaults to now.</param>
	public static ProgressSummary Compute(Pack pack, IEnumerable<Find> finds, ISet<string> viewedSpecies = null, DateTime? today = null)
	{
		var ordered = finds.Where(f => f.Verified && pack.Species.Any(s => s.Scientific == f.Scientific)).OrderBy(f => f.When).ToList();
		int points = 0;
		var collection = new Dictionary<string, Collected>();
		foreach (var f in ordered)
		{
			var sp = pack.Species.First(s => s.Scientific == f.Scientific);
			f.IsFirstOfSpecies = !collection.ContainsKey(f.Scientific);
			points += PointsFor(f, f.IsFirstOfSpecies);
			collection[f.Scientific] = collection.TryGetValue(f.Scientific, out var c)
				? new Collected { Species = sp, First = c.First, Times = c.Times + 1, BestRarity = (Rarity)Math.Max((int)c.BestRarity, (int)f.Rarity) }
				: new Collected { Species = sp, First = f, Times = 1, BestRarity = f.Rarity };
		}

		// Weekly challenges: every week with a verified find can complete up to three
		int challengePoints = 0;
		var completed = new HashSet<string>();
		foreach (var week in ordered.Select(f => Challenges.WeekStart(f.When)).Distinct())
			foreach (var ch in Challenges.ForWeek(pack, week, ordered).Where(c => c.Done))
			{
				challengePoints += ch.Points;
				completed.Add($"{week:yyyy-MM-dd}|{ch.Id}");
			}
		points += challengePoints;

		var rank = Ranks.Last(r => points >= r.MinPoints);
		var next = Ranks.FirstOrDefault(r => r.MinPoints > points);
		return new ProgressSummary
		{
			Points = points,
			Rank = rank,
			NextRank = next,
			Collection = collection,
			Badges = Badges(pack, ordered, collection, viewedSpecies ?? new HashSet<string>()),
			SpeciesCount = pack.Species.Count,
			ChallengePoints = challengePoints,
			ThisWeek = Challenges.ForWeek(pack, today ?? DateTime.Now, ordered),
			CompletedChallenges = completed,
		};
	}

	/// <summary>What saving <paramref name="newFind"/> on top of <paramref name="before"/> earns.</summary>
	public static Award AwardFor(Pack pack, IReadOnlyList<Find> before, Find newFind, ISet<string> viewedSpecies = null)
	{
		var was = Compute(pack, before, viewedSpecies, newFind.When);
		var now = Compute(pack, before.Append(newFind), viewedSpecies, newFind.When);
		if (!newFind.Verified)
			return new Award { Counted = false, Rarity = newFind.Rarity, After = now };
		bool isNew = !was.Collection.ContainsKey(newFind.Scientific);
		var earnedBefore = was.Badges.Where(b => b.Earned).Select(b => b.Id).ToHashSet();
		var week = $"{Challenges.WeekStart(newFind.When):yyyy-MM-dd}|";
		var newChallenges = now.ThisWeek.Where(c => c.Done && !was.CompletedChallenges.Contains(week + c.Id)).ToList();
		return new Award
		{
			Counted = true,
			NewSpecies = isNew,
			Points = now.Points - was.Points,
			PhotoBonus = HasPhotoBonus(newFind) ? PhotoBonusPoints : 0,
			Rarity = newFind.Rarity,
			NewBadges = now.Badges.Where(b => b.Earned && !earnedBefore.Contains(b.Id)).ToList(),
			NewChallenges = newChallenges,
			ChallengePoints = now.ChallengePoints - was.ChallengePoints,
			NewRank = now.Rank.Name != was.Rank.Name ? now.Rank : null,
			After = now,
		};
	}

	static int PointsFor(Find f, bool isNew) =>
		(isNew ? NewSpeciesPoints(f.Rarity) : RepeatPoints) + (HasPhotoBonus(f) ? PhotoBonusPoints : 0);

	/// <summary>Three different parts photographed: the habit that makes answers most accurate.</summary>
	static bool HasPhotoBonus(Find f) => f.Parts.Distinct().Count() >= 3;

	static List<Badge> Badges(Pack pack, List<Find> verified, Dictionary<string, Collected> collection, ISet<string> viewed)
	{
		var oaks = pack.Species.Where(s => s.Scientific.StartsWith("Quercus ")).Select(s => s.Scientific).ToList();
		int oaksFound = oaks.Count(collection.ContainsKey);
		var poisonous = pack.Species.Where(s => s.IsPoisonous).Select(s => s.Scientific).ToList();
		int poisonRead = poisonous.Count(viewed.Contains);

		bool winter = verified.Any(f => f.When.Month is 12 or 1 or 2
			&& pack.Species.FirstOrDefault(s => s.Scientific == f.Scientific)?.Trait("evergreen") == "no");
		bool bark = verified.Any(f => f.Parts.Contains(Part.Bark) && f.Parts.Distinct().Count() >= 2);
		bool invader = verified.Any(f => pack.Species.FirstOrDefault(s => s.Scientific == f.Scientific)?.IsInvasive == true);

		return new List<Badge>
		{
			new() { Id = "first", Name = "First Find", Description = "Identify your first tree.",
				Earned = collection.Count >= 1, Status = collection.Count >= 1 ? "Done" : "Find any tree" },
			new() { Id = "collector", Name = "Tree Collector", Description = "Find 10 different kinds of trees.",
				Earned = collection.Count >= 10, Status = $"{Math.Min(collection.Count, 10)} of 10 trees" },
			new() { Id = "oaks", Name = "Oak Squad", Description = $"Find all {oaks.Count} kinds of oak.",
				Earned = oaks.Count > 0 && oaksFound == oaks.Count, Status = $"{oaksFound} of {oaks.Count} oaks" },
			new() { Id = "winter", Name = "Winter Detective", Description = "Identify a tree that loses its leaves, in winter (December to February).",
				Earned = winter, Status = winter ? "Done" : "Try it in winter" },
			new() { Id = "bark", Name = "Bark Reader", Description = "Identify a tree using a bark photo and at least one other photo.",
				Earned = bark, Status = bark ? "Done" : "Add a bark photo" },
			new() { Id = "invader", Name = "Invader Spotter", Description = "Find an invasive tree that crowds out native plants.",
				Earned = invader, Status = invader ? "Done" : "Look for Chinese tallow or chinaberry" },
			new() { Id = "safety", Name = "Safety Smart", Description = "Read about every poisonous tree in the Species list.",
				Earned = poisonous.Count > 0 && poisonRead == poisonous.Count, Status = $"{poisonRead} of {poisonous.Count} read" },
		};
	}
}
