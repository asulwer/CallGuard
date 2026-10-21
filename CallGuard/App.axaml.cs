using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CallGuard.Views;

namespace CallGuard;

public partial class App : Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IActivityApplicationLifetime activity)
            activity.MainViewFactory = () => new MainView();

        base.OnFrameworkInitializationCompleted();
    }
}
