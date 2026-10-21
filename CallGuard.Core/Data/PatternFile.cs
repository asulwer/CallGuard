namespace CallGuard.Data;

/// <param name="Pattern">The cleaned pattern, or null if the line had no usable pattern.</param>
/// <param name="Enabled">False when the line is marked "(off)".</param>
public sealed record PatternLine(int LineNumber, PatternAction Action, string? Pattern, string? Label, bool Enabled = true);

/// <summary>
/// Plain-text export format, one pattern per line, easy to read and edit:
/// <code>
/// block 602*          # Phoenix spam
/// block +44*          # every UK number
/// block 480* (off)    # kept, but switched off
/// allow 6025550123    # doctor's office
/// 480555*             # no keyword means block
/// </code>
/// Blank lines and lines starting with # are ignored.
/// </summary>
public static class PatternFile
{
    public static string Write(IEnumerable<NumberPattern> patterns)
    {
        var lines = new List<string>
        {
            "# Call Guard patterns",
            "# One per line: block <pattern> or allow <pattern>, optionally followed by # note",
            "# * = any digits, ? = one digit, +44* = a whole country (+ and its country code)",
            "# (off) after a pattern = kept but switched off",
            "",
        };
        foreach (var p in patterns)
        {
            var line = $"{(p.Action == PatternAction.Allow ? "allow" : "block")} {p.Pattern}{(p.IsEnabled ? "" : " (off)")}";
            lines.Add(p.Label == null ? line : $"{line}  # {p.Label}");
        }
        return string.Join('\n', lines) + "\n";
    }

    public static List<PatternLine> Parse(string text)
    {
        var result = new List<PatternLine>();
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            string? label = null;
            var hash = line.IndexOf('#');
            if (hash >= 0)
            {
                label = line[(hash + 1)..].Trim();
                if (label.Length == 0) label = null;
                line = line[..hash].Trim();
            }

            var enabled = true;
            var off = line.IndexOf("(off)", StringComparison.OrdinalIgnoreCase);
            if (off >= 0)
            {
                enabled = false;
                line = (line[..off] + line[(off + 5)..]).Trim();
            }

            var action = PatternAction.Block;
            foreach (var (word, wordAction) in new[] { ("allow", PatternAction.Allow), ("block", PatternAction.Block) })
            {
                if (line.StartsWith(word, StringComparison.OrdinalIgnoreCase))
                {
                    action = wordAction;
                    line = line[word.Length..].TrimStart(':', ' ', '\t');
                    break;
                }
            }

            result.Add(new PatternLine(i + 1, action, RuleEngine.CleanPattern(line), label, enabled));
        }
        return result;
    }
}
