namespace TreeMobile;

/// <summary>App settings kept on the phone.</summary>
public static class Settings
{
	/// <summary>
	/// Kid mode: simpler words, bigger buttons, the Tree book game and celebrations. On by default;
	/// turning it off needs a grown-up to answer a multiplication question.
	/// </summary>
	public static bool KidMode
	{
		get => Preferences.Default.Get("kidMode", true);
		set => Preferences.Default.Set("kidMode", value);
	}

	/// <summary>Picks the kid-mode or regular wording.</summary>
	public static string Say(string kid, string grownUp) => KidMode ? kid : grownUp;

	/// <summary>Rebuilds the tabs so every page picks up a changed setting.</summary>
	public static void Reload()
	{
		var window = Application.Current?.Windows.FirstOrDefault();
		if (window != null) window.Page = new AppShell();
	}
}
