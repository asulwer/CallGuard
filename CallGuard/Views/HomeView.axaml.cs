using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace CallGuard.Views;

/// <summary>Setup status, blocked-call counts, and a "what would happen" tester.</summary>
public partial class HomeView : UserControl
{
    static Android.Content.Context Ctx => Android.App.Application.Context;

    public HomeView()
    {
        InitializeComponent();
        VersionText.Text = $"Call Guard {AppVersion()}";
    }

    /// <summary>The version Android reports for the app (its versionName), e.g. "1.1".</summary>
    static string AppVersion()
    {
        try
        {
            var pm = Ctx.PackageManager!;
            var info = OperatingSystem.IsAndroidVersionAtLeast(33)
                ? pm.GetPackageInfo(Ctx.PackageName!, Android.Content.PM.PackageManager.PackageInfoFlags.Of(0L))
                : pm.GetPackageInfo(Ctx.PackageName!, 0);
            return $"{info!.VersionName} (build {info.LongVersionCode})";
        }
        catch
        {
            return "";
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        MainActivity.StatusChanged += Refresh;
        Store.CallLog.Changed += OnLogChanged;
        Store.Patterns.Changed += OnPatternsChanged;
        Refresh();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        MainActivity.StatusChanged -= Refresh;
        Store.CallLog.Changed -= OnLogChanged;
        Store.Patterns.Changed -= OnPatternsChanged;
        base.OnDetachedFromVisualTree(e);
    }

    void OnLogChanged() => Dispatcher.UIThread.Post(RefreshCounts);
    void OnPatternsChanged() => Dispatcher.UIThread.Post(Refresh);

    void Refresh()
    {
        bool role = MainActivity.IsScreeningRoleHeld(Ctx);
        Ui.Show(RoleStatus, role
            ? "✔ Call Guard is the Caller ID & spam app"
            : "✖ Call Guard is NOT the Caller ID & spam app — nothing will be blocked", role);
        RoleButton.IsVisible = !role;

        bool contacts = ContactsChecker.HasAccess(Ctx);
        Ui.Show(ContactsStatus, contacts
            ? "✔ Contacts access granted"
            : "✖ No contacts access — calls that would be blocked are allowed so contacts aren't blocked", contacts);
        ContactsButton.IsVisible = !contacts;

        NoPatternsStatus.IsVisible = Store.Patterns.Load().Block.Count == 0;
        RefreshCounts();
    }

    void RefreshCounts()
    {
        var log = Store.CallLog;
        TodayCount.Text = log.BlockedSince(DateTime.Today.ToUniversalTime()).ToString();
        WeekCount.Text = log.BlockedSince(DateTime.UtcNow.AddDays(-7)).ToString();
        MonthCount.Text = log.BlockedSince(DateTime.UtcNow.AddDays(-30)).ToString();

        // Repaint the whole screen: on Android, changed text could leave parts of the old value
        // on screen, and updates made while the app was in the background could show stale values.
        TopLevel.GetTopLevel(this)?.InvalidateVisual();
    }

    void OnRequestRole(object? sender, RoutedEventArgs e) => MainActivity.Current?.RequestScreeningRole();

    void OnRequestContacts(object? sender, RoutedEventArgs e) => MainActivity.Current?.RequestContactsAccess();

    void OnTest(object? sender, RoutedEventArgs e)
    {
        var input = TestBox.Text?.Trim();
        if (string.IsNullOrEmpty(input) || RuleEngine.Normalize(input).Length == 0)
        {
            Ui.Show(TestResult, "Enter a phone number.", ok: false);
            return;
        }

        var decision = ScreeningDecider.Decide(
            new IncomingCall(input),
            Store.Patterns.Load,
            raw => ContactsChecker.IsContact(Ctx, raw));

        var shown = RuleEngine.Format(RuleEngine.Normalize(input));
        var (text, ok) = decision switch
        {
            { Action: ScreeningAction.Reject } => ($"{shown} would be BLOCKED: {decision.Reason}.", false),
            { Reason: "" } => ($"{shown} would ring: no pattern matches it.", true),
            _ => ($"{shown} would ring: {decision.Reason}.", true),
        };
        Ui.Show(TestResult, text, ok);
    }
}
