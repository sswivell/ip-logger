using Swivel.Core;

namespace Swivel.Ui.Screens;

/// <summary>Theme, animation and rendering options.</summary>
public sealed class SettingsScreen : IScreen
{
    public List<string> Render(App app, Context ctx)
    {
        var lines = ScreenHelpers.Heading(ctx, " SETTINGS ");
        var body = new List<string> { "" };

        for (var i = 0; i < app.Options.Count; i++)
        {
            var option = app.Options[i];
            var focused = i == app.Selected;
            var row = Theme.Split(
                $" {Widgets.Marker(focused)} {Widgets.Label(option.Name, focused)}",
                Value(option),
                ctx.Width);
            body.Add(focused ? Widgets.Highlight(row, ctx.Width, Theme.Violet) : row);
        }

        body.Add("");
        body.Add(Theme.Fg(Theme.Grey) + "w/s move   a/d adjust   enter toggle" + Terminal.Reset);
        lines.AddRange(ctx.Indent(Widgets.Panel("SETTINGS", body, ctx.Width, Theme.Violet)));
        return lines;
    }

    static string Value(Option option) => option.Kind switch
    {
        OptionKind.Toggle => option.On
            ? Terminal.Bold + Theme.Fg(Theme.Green) + "[====O]" + Terminal.Reset + " "
              + Terminal.Bold + Theme.Fg(Theme.Green) + "ON " + Terminal.Reset
            : Theme.Fg(Theme.Grey) + "[O====]" + Terminal.Reset + " "
              + Theme.Fg(Theme.Grey) + "OFF" + Terminal.Reset,
        OptionKind.Choice => Theme.Fg(Theme.Violet) + "<" + Terminal.Reset + " "
            + Terminal.Bold + Theme.Fg(Theme.White)
            + option.Display.PadLeft(5 + (10 - option.Display.Length) / 2).PadRight(10)
            + Terminal.Reset + " " + Theme.Fg(Theme.Violet) + ">" + Terminal.Reset,
        _ => Widgets.Slider(option.Number, option.Low, option.High),
    };
}