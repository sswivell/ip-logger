namespace Swivel.Ui.Screens;

/// <summary>One dashboard tab.</summary>
public interface IScreen
{
    List<string> Render(App app, Context ctx);
}

/// <summary>Tab registry, indexed the same way as <see cref="App.TabNames"/>.</summary>
public static class Views
{
    public static readonly IScreen[] All =
    [
        new SetupScreen(),
        new LiveScreen(),
        new StatsScreen(),
        new FingerprintScreen(),
        new SettingsScreen(),
        new AboutScreen(),
    ];
}

/// <summary>Shared helpers for the tab renderers.</summary>
public static class ScreenHelpers
{
    public const int RiskHigh = 60;
    public const int RiskMedium = 25;

    public static Rgb RiskColour(int risk) => risk >= RiskHigh ? Theme.Shades[4]
        : risk >= RiskMedium ? Theme.Shades[2]
        : Theme.Green;

    public static List<string> Heading(Context ctx, string text)
    {
        var phase = (ctx.Now * .15) % 1.0;
        return
        [
            ctx.Indent(Theme.Gradient(text, Theme.Brand, phase)),
            "",
        ];
    }
}