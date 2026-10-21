using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CallGuard.Data;

namespace CallGuard.Views;

/// <summary>Add, edit and remove block / always-allow patterns, and export or import them.</summary>
public partial class PatternsView : UserControl
{
    static readonly FilePickerFileType TextFile = new("Text file")
    {
        Patterns = ["*.txt"],
        MimeTypes = ["text/plain"],
    };

    /// <summary>A pattern that matches every number, waiting for a second tap on Add to confirm.</summary>
    string? pendingConfirm;

    /// <summary>The pattern whose note the form is editing, or null when the form adds patterns.</summary>
    NumberPattern? editing;

    public PatternsView() => InitializeComponent();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Store.Patterns.Changed += OnPatternsChanged;
        Refresh();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Store.Patterns.Changed -= OnPatternsChanged;
        base.OnDetachedFromVisualTree(e);
    }

    void OnPatternsChanged() => Dispatcher.UIThread.Post(Refresh);

    void Refresh()
    {
        var all = Store.Patterns.List();
        var blocked = all.Where(p => p.Action == PatternAction.Block).ToList();
        var allowed = all.Where(p => p.Action == PatternAction.Allow).ToList();
        BlockList.ItemsSource = blocked;
        AllowList.ItemsSource = allowed;
        NoBlocked.IsVisible = blocked.Count == 0;
        NoAllowed.IsVisible = allowed.Count == 0;
    }

    void OnAdd(object? sender, RoutedEventArgs e)
    {
        if (editing != null)
        {
            Store.Patterns.SetLabel(editing.Id, LabelBox.Text);
            EndEdit();
            return;
        }

        var action = AllowRadio.IsChecked == true ? PatternAction.Allow : PatternAction.Block;
        var cleaned = RuleEngine.CleanPattern(PatternBox.Text);
        bool confirmed = cleaned != null && cleaned == pendingConfirm;
        pendingConfirm = null;

        var result = Store.Patterns.Add(PatternBox.Text, action, LabelBox.Text, allowEveryNumber: confirmed);
        switch (result)
        {
            case AddPatternResult.Added:
                Ui.Show(AddMessage, null, ok: true);
                PatternBox.Text = "";
                LabelBox.Text = "";
                break;
            case AddPatternResult.Duplicate:
                Ui.Show(AddMessage, "That pattern is already in one of the lists.", ok: false);
                break;
            case AddPatternResult.Invalid:
                Ui.Show(AddMessage, "Enter digits, with * or ? as wildcards.", ok: false);
                break;
            case AddPatternResult.MatchesEveryNumber:
                pendingConfirm = cleaned;
                Ui.Show(AddMessage, action == PatternAction.Block
                    ? $"⚠ \"{cleaned}\" matches every number, so every caller who isn't a contact would be blocked. Tap Add again if you're sure."
                    : $"⚠ \"{cleaned}\" matches every number, so nothing would ever be blocked. Tap Add again if you're sure.", ok: false);
                break;
        }
    }

    void OnToggle(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox { Tag: int id } box) Store.Patterns.SetEnabled(id, box.IsChecked == true);
    }

    void OnRemove(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int id }) return;
        if (editing?.Id == id) EndEdit();
        Store.Patterns.Remove(id);
    }

    /// <summary>Turns the form at the top into "Edit note" for that pattern.</summary>
    void OnEdit(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: NumberPattern pattern }) return;
        editing = pattern;
        pendingConfirm = null;
        FormHeader.Text = "Edit note";
        FormHint.IsVisible = false;
        PatternBox.Text = pattern.Pattern;
        PatternBox.IsEnabled = false;
        LabelBox.Text = pattern.Label ?? "";
        ActionChoice.IsVisible = false;
        AddButton.Content = "Save note";
        CancelEditButton.IsVisible = true;
        Ui.Show(AddMessage, null, ok: true);
        Scroller.ScrollToHome();
        LabelBox.Focus();
    }

    void OnCancelEdit(object? sender, RoutedEventArgs e) => EndEdit();

    /// <summary>Puts the form back to adding patterns.</summary>
    void EndEdit()
    {
        editing = null;
        FormHeader.Text = "Add a pattern";
        FormHint.IsVisible = true;
        PatternBox.Text = "";
        PatternBox.IsEnabled = true;
        LabelBox.Text = "";
        ActionChoice.IsVisible = true;
        AddButton.Content = "Add";
        CancelEditButton.IsVisible = false;
    }

    async void OnExport(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage) return;
            var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export patterns",
                SuggestedFileName = $"callguard-patterns-{DateTime.Now:yyyy-MM-dd}", // the picker adds .txt
                DefaultExtension = "txt",
                FileTypeChoices = [TextFile],
            });
            if (file == null) return;

            var count = Store.Patterns.List().Count;
            await using (var stream = await file.OpenWriteAsync())
            await using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(Store.Patterns.Export());
            }
            Ui.Show(BackupMessage, $"Exported {count} pattern(s) to {file.Name}.", ok: true);
        }
        catch (Exception ex)
        {
            Ui.Show(BackupMessage, "Export failed: " + ex.Message, ok: false);
        }
    }

    async void OnImport(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage) return;
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import patterns",
                AllowMultiple = false,
                FileTypeFilter = [TextFile, FilePickerFileTypes.All],
            });
            if (files.Count == 0) return;

            string text;
            await using (var stream = await files[0].OpenReadAsync())
            using (var reader = new StreamReader(stream))
            {
                text = await reader.ReadToEndAsync();
            }

            var r = Store.Patterns.Import(text);
            var message = $"Imported {r.Added} pattern(s).";
            if (r.Duplicates > 0) message += $" {r.Duplicates} were already in your lists.";
            if (r.Invalid > 0) message += $" {r.Invalid} line(s) had no usable pattern.";
            Ui.Show(BackupMessage, message, ok: r.Invalid == 0);
        }
        catch (Exception ex)
        {
            Ui.Show(BackupMessage, "Import failed: " + ex.Message, ok: false);
        }
    }
}
