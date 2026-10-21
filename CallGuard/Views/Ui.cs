using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace CallGuard.Views;

/// <summary>Small helpers shared by the views.</summary>
static class Ui
{
    /// <summary>Where a pattern's numbers are from, e.g. "Arizona" for 602*; null when it can't tell.</summary>
    public static readonly IValueConverter PlaceOfPattern =
        new FuncValueConverter<string?, string?>(pattern => pattern == null ? null : PlaceNames.ForPattern(pattern));

    /// <summary>Shows a status message in green (ok) or red (problem); null text hides it.</summary>
    public static void Show(TextBlock block, string? text, bool ok)
    {
        block.IsVisible = text != null;
        block.Text = text;
        block.Classes.Set("ok", ok);
        block.Classes.Set("problem", !ok);
    }
}
