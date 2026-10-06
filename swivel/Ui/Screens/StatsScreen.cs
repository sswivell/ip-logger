using Swivel.Core;

namespace Swivel.Ui.Screens;

/// <summary>Counters, top countries and top ISPs.</summary>
public sealed class StatsScreen : IScreen
{
    const int BarWidth = 12;
    const int IspWidth = 22;

    public List<string> Render(App app, Context ctx)
    {
        var lines = ScreenHelpers.Heading(ctx, " STATS ");
        var store = ctx.Runtime.Hits;

        lines.AddRange(ctx.Indent(Widgets.Panel("COUNTS",
        [
            Widgets.StatRow("total", Math.Max(1, store.Total), Theme.Cyan),
            Widgets.StatRow("clean", store.Clean, Theme.Green),
            Widgets.StatRow("adblock", store.AdBlock, Theme.Amber),
            Widgets.StatRow("vpn/dc", store.Networked, Theme.Violet),
            Widgets.StatRow("tor", store.Tor, Theme.Pink),
            Widgets.StatRow("scanner", store.Scanners, Theme.Alert),
        ], ctx.PanelWidth, Theme.Cyan)));
        lines.Add("");

        lines.AddRange(ctx.Indent(Widgets.Panel("TOP COUNTRIES",
            Countries(store.TopCountries), ctx.PanelWidth, Theme.Green)));
        lines.Add("");

        lines.AddRange(ctx.Indent(Widgets.Panel("TOP ISPs",
            Isps(store.TopIsps), ctx.PanelWidth, Theme.Amber)));
        return lines;
    }

    static List<string> Countries(Dictionary<string, int> ranked)
    {
        var top = ranked.OrderByDescending(p => p.Value).Take(8).ToList();
        if (top.Count == 0)
            return [Theme.Fg(Theme.Grey) + "no data" + Terminal.Reset];
        var peak = Math.Max(1, top[0].Value);
        return top.Select(entry =>
        {
            var label = Geo.FlagEmoji(entry.Key) + " " + entry.Key;
            return Theme.Pad(Theme.Fg(Theme.White) + label, label.Length + 4)
                + " " + Widgets.Bar(entry.Value / (double)peak * 100, BarWidth, Theme.Cyan)
                + " " + Terminal.Bold + Theme.Fg(Theme.White)
                + entry.Value.ToString().PadLeft(4) + Terminal.Reset;
        }).ToList();
    }

    static List<string> Isps(Dictionary<string, int> ranked)
    {
        var top = ranked.OrderByDescending(p => p.Value).Take(6).ToList();
        if (top.Count == 0)
            return [Theme.Fg(Theme.Grey) + "no data" + Terminal.Reset];
        return top.Select(entry =>
        {
            var name = entry.Key.Length > IspWidth ? entry.Key[..IspWidth] : entry.Key;
            var filler = name.Length + Theme.Fg(Theme.White).Length + Terminal.Reset.Length;
            return Theme.Pad(Theme.Fg(Theme.White) + name, filler + IspWidth)
                + " " + Terminal.Bold + Theme.Fg(Theme.Amber)
                + entry.Value.ToString().PadLeft(4) + Terminal.Reset;
        }).ToList();
    }
}