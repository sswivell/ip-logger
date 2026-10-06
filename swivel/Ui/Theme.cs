using System.Text.RegularExpressions;
using Swivel.Core;

namespace Swivel.Ui;

/// <summary>Colour palettes, brightness and ANSI text measurement.</summary>
public static class Theme
{
    public static readonly Rgb White = new(255, 248, 235);
    public static readonly Rgb Grey = new(110, 130, 170);
    public static readonly Rgb Track = new(28, 36, 64);
    public static readonly Rgb Alert = new(255, 90, 90);

    public static readonly IReadOnlyDictionary<string, Rgb[]> Palettes = new Dictionary<string, Rgb[]>
    {
        ["BLUE"] = [new(30, 90, 200), new(60, 140, 240), new(100, 180, 255), new(150, 210, 255), new(200, 230, 255)],
        ["SWIVEL"] = [new(100, 180, 255), new(140, 90, 230), new(230, 40, 60), new(0, 220, 140), new(255, 200, 40)],
        ["EMERALD"] = [new(0, 220, 140), new(0, 180, 220), new(100, 180, 255), new(180, 255, 200), new(255, 255, 220)],
        ["NIGHTS"] = [new(80, 40, 180), new(180, 90, 220), new(255, 200, 40), new(100, 180, 255), new(230, 40, 60)],
        ["AMBER"] = [new(255, 200, 100), new(230, 150, 60), new(255, 140, 50), new(255, 210, 120), new(255, 180, 60)],
    };

    static readonly Rgb[] NoShades = new Rgb[5];

    public static Rgb Cyan { get; private set; }
    public static Rgb Violet { get; private set; }
    public static Rgb Pink { get; private set; }
    public static Rgb Green { get; private set; }
    public static Rgb Amber { get; private set; }

    public static Rgb[] Shades { get; private set; } = NoShades;
    public static Rgb[] Brand { get; private set; } = [];

    public static double Brightness { get; private set; } = 1.0;
    public static bool TrueColor { get; } =
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TERMUX_VERSION"))
        || (Environment.GetEnvironmentVariable("PREFIX") ?? "").Contains("com.termux", StringComparison.Ordinal)
        || (Environment.GetEnvironmentVariable("COLORTERM") ?? "").ToLowerInvariant()
            is "truecolor" or "24bit";

    public static void SetBrightness(int percent) => Brightness = Math.Clamp(percent, 30, 150) / 100.0;

    /// <summary>Activate a palette and derive its shades and brand ramp.</summary>
    public static void SetTheme(string name)
    {
        var palette = Palettes[name];
        Cyan = palette[0];
        Violet = palette[1];
        Pink = palette[2];
        Green = palette[3];
        Amber = palette[4];
        Shades = [Rgb.Mix(Cyan, Black, .55), Cyan, Amber, Pink, Alert];
        Brand = [Cyan, Violet, Amber, White];
    }

    public static Rgb Black => new(0, 0, 0);

    public static string Fg(Rgb colour) => colour.Scale(Brightness).Escape(background: false);

    public static string Bg(Rgb colour) => colour.Scale(Brightness).Escape(background: true);

    static readonly Regex EscapeSequence = new("\u001b\\[[0-9;]*m", RegexOptions.Compiled);

    public static int VisibleLength(string text) => EscapeSequence.Replace(text, "").Length;

    public static string Pad(string text, int width) =>
        text + new string(' ', Math.Max(0, width - VisibleLength(text)));

    /// <summary>Left/right justified row with a computed gutter.</summary>
    public static string Split(string left, string right, int width)
    {
        var gap = Math.Max(1, width - VisibleLength(left) - VisibleLength(right));
        return left + new string(' ', gap) + right;
    }

    /// <summary>Paint each non-space character along a colour ramp.</summary>
    public static string Gradient(string text, IReadOnlyList<Rgb> stops, double phase = 0.0)
    {
        var span = Math.Max(1, text.Length - 1);
        var builder = new System.Text.StringBuilder(text.Length + 16);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ')
            {
                builder.Append(' ');
                continue;
            }
            var t = ((i / (double)span) + phase) % 1.0;
            builder.Append(Fg(Rgb.Sample(stops, t))).Append(text[i]);
        }
        return builder.Append(Terminal.Reset).ToString();
    }
}

/// <summary>An RGB colour with blending and escape-sequence helpers.</summary>
public readonly record struct Rgb(int R, int G, int B)
{
    public static Rgb Mix(Rgb from, Rgb to, double t)
    {
        int Channel(int a, int b) => (int)Math.Round(a + (b - a) * t, MidpointRounding.AwayFromZero);
        return new Rgb(Channel(from.R, to.R), Channel(from.G, to.G), Channel(from.B, to.B));
    }

    public Rgb Scale(double factor)
    {
        int Channel(int value) => Math.Min(255, (int)(value * factor));
        return new Rgb(Channel(R), Channel(G), Channel(B));
    }

    public string Escape(bool background)
    {
        if (Theme.TrueColor)
            return string.Concat(Terminal.Esc, background ? "48;2;" : "38;2;", R, ";", G, ";", B, "m");
        var index = 16 + 36 * (int)Math.Round(R / 51.0)
                  + 6 * (int)Math.Round(G / 51.0)
                  + (int)Math.Round(B / 51.0);
        return string.Concat(Terminal.Esc, background ? "48;5;" : "38;5;", index, "m");
    }

    /// <summary>Sample a ramp at t in [0, 1].</summary>
    public static Rgb Sample(IReadOnlyList<Rgb> stops, double t)
    {
        if (stops.Count == 0)
            return Theme.Black;
        if (stops.Count == 1)
            return stops[0];
        t = Math.Clamp(t, 0.0, 1.0);
        var x = t * (stops.Count - 1);
        var i = Math.Min((int)x, stops.Count - 2);
        return Mix(stops[i], stops[i + 1], x - i);
    }
}