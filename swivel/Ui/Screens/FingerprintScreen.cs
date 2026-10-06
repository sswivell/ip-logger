using Swivel.Core;

namespace Swivel.Ui.Screens;

/// <summary>Browser fingerprint detail for the three most recent hits.</summary>
public sealed class FingerprintScreen : IScreen
{
    const int MaxHits = 6;
    const int MaxShown = 3;
    const int MaxFields = 14;
    const int ValueWidth = 40;

    public List<string> Render(App app, Context ctx)
    {
        var lines = ScreenHelpers.Heading(ctx, " FINGERPRINTS ");
        var hits = ctx.Runtime.Hits.Recent(MaxHits).AsEnumerable().Reverse().ToList();
        if (!hits.Any(h => h.Fingerprint.ValueKind == System.Text.Json.JsonValueKind.Object))
        {
            lines.Add(ctx.Indent(Theme.Fg(Theme.Grey) + "no fingerprints yet" + Terminal.Reset));
            lines.Add(ctx.Indent(Theme.Fg(Theme.Grey) + "hit the link first" + Terminal.Reset));
            return lines;
        }

        var shown = 0;
        foreach (var hit in hits)
        {
            if (hit.Fingerprint.ValueKind != System.Text.Json.JsonValueKind.Object)
                continue;
            var header = Theme.Fg(Theme.Violet) + hit.Timestamp + Terminal.Reset + "  "
                + Terminal.Bold + Theme.Fg(Theme.White) + hit.Ip + Terminal.Reset + "  "
                + Theme.Fg(Theme.Grey) + Clip(hit.Location, 30) + Terminal.Reset;
            lines.Add(ctx.Indent(header));
            lines.Add(ctx.Indent(Widgets.Rule(Math.Min(ctx.Width, 60))));
            foreach (var (label, value) in Payload.Rows(hit.Fingerprint).Take(MaxFields))
            {
                var key = label.Length > 14 ? label[..14] : label;
                lines.Add(ctx.Indent("  " + Theme.Fg(Theme.Violet)
                    + Theme.Pad(key, 14) + Terminal.Reset
                    + Theme.Fg(Theme.White) + Clip(value, ValueWidth) + Terminal.Reset));
            }
            lines.Add("");
            if (++shown >= MaxShown)
                break;
        }
        return lines;
    }

    static string Clip(string text, int width) => text.Length > width ? text[..width] : text;
}