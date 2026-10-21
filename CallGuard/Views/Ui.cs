using Avalonia.Controls;

namespace CallGuard.Views;

/// <summary>Small helpers shared by the views.</summary>
static class Ui
{
    /// <summary>Shows a status message in green (ok) or red (problem); null text hides it.</summary>
    public static void Show(TextBlock block, string? text, bool ok)
    {
        block.IsVisible = text != null;
        block.Text = text;
        block.Classes.Set("ok", ok);
        block.Classes.Set("problem", !ok);
    }
}
