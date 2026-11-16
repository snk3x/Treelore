namespace TreeMobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		// Start loading the model in the background so the first photo is identified quickly.
		_ = TreeEngine.LoadAsync();
	}

	protected override Window CreateWindow(IActivationState activationState) => new(new AppShell()) { Title = "Tree ID" };
}
