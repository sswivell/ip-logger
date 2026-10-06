using Swivel.Core;

namespace Swivel.Ui.Screens;

/// <summary>Recent hits plus the traffic chart.</summary>
public sealed class LiveScreen : IScreen
{
    public List<string> Render(App app, Context ctx)
    {
        var lines = ScreenHelpers.Heading(ctx, " LIVE HITS ");
        var hits = ctx.Runtime.Hits.Recent(8).AsEnumerable().Reverse().ToList();
        if (hits.Count == 0)
            lines.Add(ctx.Indent(Theme.Fg(Theme.Grey) + "waiting..." + Terminal.Reset));

        foreach (var hit in hits)
        {
            var colour = ScreenHelpers.RiskColour(hit.Risk);
            var line = Theme.Fg(Rgb.Mix(colour, Theme.Black, .5)) + hit.Timestamp + Terminal.Reset + "  "
                + Terminal.Bold + Theme.Fg(Theme.White) + Theme.Pad(hit.Ip, 15) + Terminal.Reset + " "
                + Theme.Fg(colour) + Theme.Pad(hit.Tags, 10) + Terminal.Reset + " "
                + Theme.Fg(Theme.Grey) + Clip(hit.Location, 30) + Terminal.Reset;
            lines.Add(ctx.Indent(line));
        }
        lines.Add("");
        lines.AddRange(ctx.Indent(Widgets.Panel("LOAD", Chart(app, ctx), ctx.PanelWidth, Theme.Cyan)));
        return lines;
    }

    static List<string> Chart(App app, Context ctx)
    {
        var store = ctx.Runtime.Hits;
        var rows = new List<string>
        {
            Terminal.Bold + Theme.Fg(Theme.Cyan) + "hits" + Terminal.Reset + "  "
            + Theme.Fg(Theme.White) + store.Total + Terminal.Reset,
            app.Get("Sparklines").On
                ? Widgets.Sparkline(app.Sim.Cpu, ctx.Inner)
                : Theme.Fg(Theme.Track) + new string('\u2500', ctx.Inner) + Terminal.Reset,
            Theme.Split(
                Theme.Fg(Theme.Grey) + "lat " + Theme.Fg(Theme.White)
                    + $"{(int)app.Sim.Latency}ms",
                Theme.Fg(Theme.Grey) + "up " + Theme.Fg(Theme.White)
                    + $"{(int)app.Sim.ElapsedSeconds}s",
                ctx.Inner),
        };
        return rows;
    }

    static string Clip(string text, int width) => text.Length > width ? text[..width] : text;
}