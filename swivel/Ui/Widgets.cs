using Swivel.Core;

namespace Swivel.Ui;

/// <summary>Reusable terminal widgets: bars, sparklines, rows and panels.</summary>
public static class Widgets
{
    public const char FullBlock = '\u2588';
    public const char LightShade = '\u2591';
    public const string Blocks = "\u2581\u2582\u2583\u2584\u2585\u2586\u2587\u2588";
    public const char Horizontal = '\u2500';
    public const char Vertical = '\u2502';
    public const char TopLeft = '\u256d';
    public const char TopRight = '\u256e';
    public const char BottomLeft = '\u2570';
    public const char BottomRight = '\u256f';

    /// <summary>Filled progress bar over a shaded track.</summary>
    public static string Bar(double percent, int width, Rgb colour)
    {
        percent = Math.Clamp(percent, 0, 100);
        var filled = (int)Math.Round(percent / 100 * width, MidpointRounding.AwayFromZero);
        return Theme.Fg(colour) + new string(FullBlock, filled)
            + Theme.Fg(Theme.Track) + new string(LightShade, Math.Max(0, width - filled))
            + Terminal.Reset;
    }

    /// <summary>Labelled slider used by the settings screen.</summary>
    public static string Slider(int value, int low, int high, int width = 12)
    {
        var t = (value - low) / (double)Math.Max(1, high - low);
        var filled = (int)(t * width);
        var body = Theme.Fg(Rgb.Sample(Theme.Shades, t)) + new string(FullBlock, filled)
            + Theme.Fg(Theme.Track) + new string(LightShade, Math.Max(0, width - filled))
            + Terminal.Reset;
        return $"< {body} {Theme.Fg(Theme.White)}{Terminal.Bold}{value,4}{Terminal.Reset} >";
    }

    /// <summary>Block sparkline over the most recent samples.</summary>
    public static string Sparkline(IReadOnlyList<double> values, int width, double high = 100)
    {
        var builder = new System.Text.StringBuilder(width + 16);
        foreach (var value in values.Skip(Math.Max(0, values.Count - width)))
        {
            var t = Math.Clamp(value / high, 0.0, 1.0);
            builder.Append(Theme.Fg(Rgb.Sample(Theme.Shades, t))).Append(Blocks[(int)(t * 7)]);
        }
        return builder.Append(Terminal.Reset).ToString();
    }

    public static string Marker(bool active) => active
        ? Theme.Fg(Theme.Shades[1]) + Terminal.Bold + ">" + Terminal.Reset
        : Theme.Fg(Rgb.Mix(Theme.Grey, Theme.Black, .4)) + "." + Terminal.Reset;

    public static string Label(string text, bool active) => active
        ? Theme.Fg(Theme.White) + Terminal.Bold + text + Terminal.Reset
        : Theme.Fg(Theme.Grey) + text + Terminal.Reset;

    /// <summary>Dim the row background for the focused entry.</summary>
    public static string Highlight(string line, int width, Rgb accent)
    {
        var back = Theme.Bg(Rgb.Mix(accent, Theme.Black, .72));
        return back + line.Replace(Terminal.Reset, Terminal.Reset + back)
            + Theme.Pad("", width - Theme.VisibleLength(line)) + Terminal.Reset;
    }

    /// <summary>Rounded box containing a title and pre-padded body lines.</summary>
    public static List<string> Panel(string title, IEnumerable<string> body, int width, Rgb accent)
    {
        var dim = Theme.Fg(Rgb.Mix(accent, Theme.Black, .35));
        var lines = new List<string>
        {
            dim + TopLeft + Horizontal + Terminal.Reset + " "
            + Terminal.Bold + Theme.Fg(accent) + title + Terminal.Reset + " "
            + dim + new string(Horizontal, Math.Max(1, width - title.Length - 5))
            + TopRight + Terminal.Reset,
        };
        lines.AddRange(body.Select(line =>
            dim + Vertical + Terminal.Reset + " " + Theme.Pad(line, width - 4) + " "
            + dim + Vertical + Terminal.Reset));
        lines.Add(dim + BottomLeft + new string(Horizontal, Math.Max(0, width - 2))
            + BottomRight + Terminal.Reset);
        return lines;
    }

    public static string Rule(int width) =>
        Theme.Fg(Rgb.Mix(Theme.Grey, Theme.Black, .55)) + new string(Horizontal, width) + Terminal.Reset;

    /// <summary>Tab strip plus a gradient underline.</summary>
    public static List<string> Tabs(IReadOnlyList<string> names, int active, int width)
    {
        var chips = new List<string>();
        for (var i = 0; i < names.Count; i++)
        {
            var chip = $"  {names[i]}  ";
            chips.Add(i == active
                ? Theme.Bg(Theme.Shades[i % Theme.Shades.Length])
                  + Theme.Fg(new Rgb(15, 20, 40)) + Terminal.Bold + chip + Terminal.Reset
                : Theme.Fg(Theme.Grey) + chip + Terminal.Reset);
        }
        var strip = new System.Text.StringBuilder();
        for (var i = 0; i < width; i++)
            strip.Append(Theme.Fg(Rgb.Sample(Theme.Brand, i / (double)Math.Max(1, width - 1))))
                .Append(Horizontal);
        return [string.Concat(chips), strip.Append(Terminal.Reset).ToString()];
    }

    public static string StatRow(string name, object value, Rgb colour) =>
        Theme.Fg(colour) + name.PadRight(9) + Terminal.Bold + Theme.Fg(Theme.White)
        + Convert.ToString(value)?.PadLeft(6) + Terminal.Reset;

    public static string Field(string name, string value) =>
        Theme.Fg(Theme.Grey) + Terminal.Bold + name + Terminal.Reset + "  "
        + Theme.Fg(Theme.White) + value + Terminal.Reset;
}