namespace TreeMobile;

/// <summary>Bottom tab bar: Identify, My trees, Species, About.</summary>
public class AppShell : Shell
{
	public AppShell()
	{
		FlyoutBehavior = FlyoutBehavior.Disabled;
		var tabs = new TabBar();
		tabs.Items.Add(Tab("Identify", "tab_identify.png", new IdentifyPage()));
		tabs.Items.Add(Tab("My trees", "tab_finds.png", new FindsPage()));
		tabs.Items.Add(Tab("Species", "tab_species.png", new SpeciesListPage()));
		tabs.Items.Add(Tab("About", "tab_about.png", new AboutPage()));
		Items.Add(tabs);
	}

	static ShellContent Tab(string title, string icon, Page page)
	{
		// Each page shows its own large title, so hide the extra bar at the top.
		Shell.SetNavBarIsVisible(page, false);
		return new() { Title = title, Icon = icon, Content = page };
	}
}
