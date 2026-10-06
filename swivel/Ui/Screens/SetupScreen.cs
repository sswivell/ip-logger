using Swivel.Core;

namespace Swivel.Ui.Screens;

/// <summary>Webhook and target entry plus listener status.</summary>
public sealed class SetupScreen : IScreen
{
    static readonly string[] Fields = ["Webhook", "Target", "START"];

    public List<string> Render(App app, Context ctx)
    {
        var lines = ScreenHelpers.Heading(ctx, " SETUP ");
        var rt = ctx.Runtime;
        var body = new List<string> { "" };

        for (var i = 0; i < Fields.Length; i++)
        {
            var focused = i == app.Form.Selected;
            var marker = Widgets.Marker(focused);
            var name = Widgets.Label(Fields[i], focused);
            var value = Fields[i] == "START"
                ? rt.Started
                    ? Terminal.Bold + Theme.Fg(Theme.Green) + "[ RUNNING ]" + Terminal.Reset
                    : Terminal.Bold + Theme.Fg(Theme.Amber) + "[ ENTER ]" + Terminal.Reset
                : Entry(app, i, focused);
            var row = Theme.Split($" {marker} {name}", value, ctx.Inner);
            body.Add(focused ? Widgets.Highlight(row, ctx.Inner, Theme.Violet) : row);
            if (i < 2)
                body.Add("");
        }

        body.Add("");
        body.Add(Theme.Fg(Theme.Grey) + "w/s move   enter edit   esc cancel" + Terminal.Reset);
        lines.AddRange(ctx.Indent(Widgets.Panel("INPUTS", body, ctx.PanelWidth, Theme.Violet)));
        lines.Add("");
        lines.AddRange(ctx.Indent(Widgets.Panel("STATUS", Status(ctx, rt), ctx.PanelWidth, Theme.Green)));
        return lines;
    }

    static string Entry(App app, int index, bool focused)
    {
        var text = app.Form.Values[index];
        var shown = text.Length == 0
            ? Theme.Fg(Rgb.Mix(Theme.Grey, Theme.Black, .6)) + "(empty)"
            : text;
        if (focused && app.Form.Editing)
            shown += "_";
        return Theme.Fg(Theme.White) + shown + Terminal.Reset;
    }

    static IEnumerable<string> Status(Context ctx, Runtime rt)
    {
        var rows = new List<string>();
        if (rt.PublicUrl is { Length: > 0 } url)
            rows.Add(Theme.Fg(Theme.Green) + "public " + Terminal.Bold
                + Theme.Fg(Theme.White) + Clip(url, 50) + Terminal.Reset);
        else if (rt.Started)
            rows.Add(Theme.Fg(Theme.Amber) + "waiting for URL..." + Terminal.Reset);
        else
            rows.Add(Theme.Fg(Theme.Grey) + "not started" + Terminal.Reset);

        rows.Add(Widgets.Field("port", rt.Port.ToString())
            + "  " + Widgets.Field("hits", rt.Hits.Total.ToString()));
        rows.Add(Widgets.Field("webhook", Clip(rt.Webhook ?? "none", 40)));
        return rows;
    }

    static string Clip(string text, int width) => text.Length > width ? text[..width] : text;
}