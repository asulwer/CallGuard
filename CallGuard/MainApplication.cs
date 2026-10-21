using Android.App;
using Android.Runtime;
using Avalonia.Android;

namespace CallGuard;

/// <summary>
/// The app process. Android also starts it just to screen a call, which needs no UI, so the UI
/// framework (Avalonia) isn't started here but by <see cref="StartUi"/> when a screen is opened.
/// That keeps call-triggered starts fast enough to answer Android in time.
/// </summary>
[Application]
public class MainApplication : AvaloniaAndroidApplication<App>
{
    bool uiStarted;

    protected MainApplication(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public override void OnCreate()
    {
        // Deliberately not calling base.OnCreate(): that starts Avalonia. See StartUi.
    }

    /// <summary>Starts Avalonia the first time a screen needs it. Call on the main thread.</summary>
    public void StartUi()
    {
        if (uiStarted) return;
        uiStarted = true;
        // Place names ("Arizona") load a few MB of data; get that done before a screen needs it.
        Task.Run(PlaceNames.WarmUp);
        base.OnCreate();
    }
}
