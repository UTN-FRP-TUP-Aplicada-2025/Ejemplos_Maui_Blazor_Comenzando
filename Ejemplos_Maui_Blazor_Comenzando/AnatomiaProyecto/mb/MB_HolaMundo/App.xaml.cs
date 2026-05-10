using MB_HolaMundo.Pages;

namespace MB_HolaMundo;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "MB_HolaMundo" };
    }
}
