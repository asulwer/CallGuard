using Android;
using Android.App;
using Android.App.Roles;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Widget;
using Avalonia.Android;
using Avalonia.Styling;

namespace CallGuard;

/// <summary>
/// Hosts the Avalonia UI (<see cref="Views.MainView"/>) and handles the Android-only prompts:
/// the "Caller ID &amp; spam" role and contacts permission.
/// </summary>
[Activity(Label = "Call Guard", Theme = "@style/CallGuardTheme", Icon = "@mipmap/appicon", MainLauncher = true, Exported = true,
          ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    const int RoleRequestCode = 1;
    const int ContactsRequestCode = 2;

    /// <summary>The running activity, so the UI can show Android prompts. Null when the app is closed.</summary>
    public static MainActivity? Current { get; private set; }

    /// <summary>Raised when the role or contacts permission may have changed.</summary>
    public static event Action? StatusChanged;

    public static bool IsScreeningRoleHeld(Context c) =>
        c.GetSystemService(RoleService) is RoleManager rm && rm.IsRoleHeld(RoleManager.RoleCallScreening);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Current = this;
        ((MainApplication)Application!).StartUi(); // must run before Avalonia's OnCreate
        base.OnCreate(savedInstanceState);
        ApplyTheme(Resources?.Configuration);
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        ApplyTheme(newConfig);
    }

    /// <summary>Follows the phone's dark-theme setting, including when it changes while the app is open.</summary>
    static void ApplyTheme(Configuration? config)
    {
        if (config == null || Avalonia.Application.Current is not { } app) return;
        bool dark = (config.UiMode & UiMode.NightMask) == UiMode.NightYes;
        app.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    protected override void OnResume()
    {
        base.OnResume();
        StatusChanged?.Invoke();
        RepaintEverything();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus) RepaintEverything();
    }

    /// <summary>
    /// On Android, Avalonia can leave parts of the screen undrawn or stale after the app was in the
    /// background, or when it was started while the phone was asleep. Repaint the whole screen whenever
    /// the app comes back into view.
    /// </summary>
    void RepaintEverything()
    {
        if (Content is Avalonia.Visual root)
            Avalonia.Threading.Dispatcher.UIThread.Post(() => Avalonia.Controls.TopLevel.GetTopLevel(root)?.InvalidateVisual());
    }

    protected override void OnDestroy()
    {
        if (Current == this) Current = null;
        base.OnDestroy();
    }

    public void RequestScreeningRole()
    {
        var rm = GetSystemService(RoleService) as RoleManager;
        if (rm == null || !rm.IsRoleAvailable(RoleManager.RoleCallScreening))
        {
            Toast.MakeText(this, "Call screening isn't available on this device", ToastLength.Long)!.Show();
            return;
        }
        if (rm.IsRoleHeld(RoleManager.RoleCallScreening))
        {
            Toast.MakeText(this, "Already set", ToastLength.Short)!.Show();
            return;
        }
        StartActivityForResult(rm.CreateRequestRoleIntent(RoleManager.RoleCallScreening), RoleRequestCode);
    }

    public void RequestContactsAccess()
    {
        RequestPermissions(new[] { Manifest.Permission.ReadContacts }, ContactsRequestCode);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        StatusChanged?.Invoke();
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        // After two refusals Android stops showing the prompt, so send the user to the app's settings instead.
        if (requestCode == ContactsRequestCode
            && grantResults.Length > 0 && grantResults[0] == Permission.Denied
            && !ShouldShowRequestPermissionRationale(Manifest.Permission.ReadContacts))
        {
            Toast.MakeText(this, "Turn on Contacts under Permissions", ToastLength.Long)!.Show();
            StartActivity(new Intent(Android.Provider.Settings.ActionApplicationDetailsSettings,
                                     Android.Net.Uri.FromParts("package", PackageName, null)));
        }
        StatusChanged?.Invoke();
    }
}
