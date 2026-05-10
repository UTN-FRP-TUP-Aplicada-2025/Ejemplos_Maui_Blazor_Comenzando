using BS_HolaMundo.Pages;

namespace BS_HolaMundo;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "BS_HolaMundo" };
    }
}
