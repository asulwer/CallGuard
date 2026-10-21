using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CallGuard.Data;

namespace CallGuard.Views;

/// <summary>One row of the recent-calls list.</summary>
public sealed class CallRow(CallLogEntry entry)
{
    public CallLogEntry Entry { get; } = entry;

    public string Title => ScreeningDecision.Verb(Entry.Action) + "  " +
        (Entry.Number.Length > 0 ? RuleEngine.Format(Entry.Number) : "hidden number");

    public string Detail
    {
        get
        {
            var when = Entry.TimeUtc.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.CurrentCulture);
            var reason = Entry.Reason == "hidden number" ? "" : Entry.Reason;
            var place = Entry.Number.Length > 0 ? PlaceNames.ForNumber(Entry.Number) : null;
            return string.Join(" · ", new[] { when, place, reason }.Where(part => !string.IsNullOrEmpty(part)));
        }
    }

    public bool IsRejected => Entry.Action == ScreeningAction.Reject;
    public bool HasNumber => Entry.Number.Length > 0;

    /// <summary>Block a call that got through, or always allow one that was stopped.</summary>
    public string ActionText => Entry.Action == ScreeningAction.Allow ? "Block" : "Allow";
}

/// <summary>The recent-calls log.</summary>
public partial class CallsView : UserControl
{
    public CallsView() => InitializeComponent();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Store.CallLog.Changed += OnLogChanged;
        Ui.Show(Message, null, ok: true);
        Refresh();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Store.CallLog.Changed -= OnLogChanged;
        base.OnDetachedFromVisualTree(e);
    }

    void OnLogChanged() => Dispatcher.UIThread.Post(Refresh);

    void Refresh()
    {
        var rows = Store.CallLog.Recent().Select(entry => new CallRow(entry)).ToList();
        CallList.ItemsSource = rows;
        Empty.IsVisible = rows.Count == 0;
        ClearButton.IsVisible = rows.Count > 0;
    }

    void OnRowAction(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: CallRow row }) return;

        var number = row.Entry.Number;
        var shown = RuleEngine.Format(number);
        bool block = row.Entry.Action == ScreeningAction.Allow;
        var result = Store.Patterns.Add(number, block ? PatternAction.Block : PatternAction.Allow, "added from call log");

        if (result == AddPatternResult.Added)
        {
            var note = block && row.Entry.Reason.StartsWith("contact") ? " It's a saved contact, so it will still ring." : "";
            Ui.Show(Message, block ? $"Now blocking {shown}.{note}" : $"{shown} will always ring now.", ok: true);
        }
        else
        {
            Ui.Show(Message, $"{shown} is already in your patterns — see the Patterns tab.", ok: false);
        }
    }

    void OnClear(object? sender, RoutedEventArgs e)
    {
        Store.CallLog.Clear();
        Ui.Show(Message, null, ok: true);
    }
}
