using System.Diagnostics;
using Swivel.Core;
using Swivel.Ui.Screens;

namespace Swivel.Ui;

/// <summary>Kind of a settings row.</summary>
public enum OptionKind
{
    Choice,
    Toggle,
    Slider,
}

/// <summary>One row on the settings screen.</summary>
public sealed class Option
{
    public required string Name { get; init; }
    public required OptionKind Kind { get; init; }
    public bool On { get; set; }
    public int Number { get; set; }
    public int Index { get; set; }
    public int Low { get; init; }
    public int High { get; init; }
    public int Step { get; init; } = 1;
    public string[] Choices { get; init; } = [];

    public string Display => Kind switch
    {
        OptionKind.Choice => Choices[Index],
        OptionKind.Toggle => On ? "ON" : "OFF",
        _ => Number.ToString(),
    };
}

/// <summary>Setup-screen input buffers.</summary>
public sealed class Form(string webhook, string target)
{
    public string[] Values { get; } = [webhook, target];
    public int Selected { get; set; }
    public bool Editing { get; set; }
}

/// <summary>Application state, input dispatch and the frame loop.</summary>
public sealed class App(Runtime runtime, string webhook, string target)
{
    public static readonly string[] TabNames = ["SETUP", "LIVE", "STATS", "FP", "SETTINGS", "ABOUT"];

    public Runtime Runtime { get; } = runtime;
    public Sim Sim { get; private set; } = new();
    public Form Form { get; } = new(webhook, target);
    public int Tab { get; set; }
    public int Selected { get; set; }

    public List<Option> Options { get; } =
    [
        new() { Name = "Theme", Kind = OptionKind.Choice, Choices = [.. Theme.Palettes.Keys], Index = 0 },
        new() { Name = "Animations", Kind = OptionKind.Toggle, On = true },
        new() { Name = "Sparklines", Kind = OptionKind.Toggle, On = true },
        new() { Name = "Compact", Kind = OptionKind.Toggle, On = true },
        new() { Name = "Brightness", Kind = OptionKind.Slider, Number = 100, Low = 30, High = 150, Step = 5 },
        new() { Name = "Refresh Hz", Kind = OptionKind.Slider, Number = 20, Low = 5, High = 60, Step = 5 },
    ];

    public App(Runtime runtime, string webhook, string target, Sim sim)
        : this(runtime, webhook, target)
    {
        Sim = sim;
    }

    public Option Get(string name) => Options.First(o => o.Name == name);

    public void ApplyTheme()
    {
        Theme.SetBrightness(Get("Brightness").Number);
        Theme.SetTheme(Get("Theme").Display);
    }

    /// <summary>Nudge the highlighted option left or right.</summary>
    public void Adjust(int direction)
    {
        var option = Options[Selected];
        switch (option.Kind)
        {
            case OptionKind.Toggle:
                option.On = !option.On;
                break;
            case OptionKind.Choice:
                option.Index = (option.Index + direction + option.Choices.Length)
                    % option.Choices.Length;
                break;
            default:
                option.Number = Math.Clamp(option.Number + direction * option.Step,
                    option.Low, option.High);
                break;
        }
        ApplyTheme();
    }

    /// <summary>Dispatch one keypress; returns false to quit.</summary>
    public bool Handle(string ch)
    {
        if (Form.Editing)
        {
            if (ch == Input.Escape || Input.IsEnter(ch))
                Form.Editing = false;
            else
                Form.Values[Form.Selected] = Input.Edit(ch, Form.Values[Form.Selected]);
            return true;
        }
        if (ch is "q" or "\u0003")
            return false;
        if (ch == Input.Escape)
            return true;
        if (ch is "\t" or "]")
            return Jump((Tab + 1) % TabNames.Length);
        if (ch == "[")
            return Jump((Tab - 1 + TabNames.Length) % TabNames.Length);
        if (ch.Length == 1 && ch[0] is >= '1' and <= '6')
            return Jump(ch[0] - '1');
        if (ch == "r")
        {
            Sim = new Sim();
            return true;
        }
        if (ch == "c")
        {
            Runtime.Hits.Clear();
            return true;
        }
        return ScreenKeys(ch);
    }

    bool Jump(int tab)
    {
        Tab = tab;
        Selected = 0;
        return true;
    }

    bool ScreenKeys(string ch)
    {
        switch (Tab)
        {
            case 0:
                if (ch == "w")
                    Form.Selected = (Form.Selected + 2) % 3;
                else if (ch == "s")
                    Form.Selected = (Form.Selected + 1) % 3;
                else if (Input.IsConfirm(ch))
                {
                    if (Form.Selected == 2)
                        Runtime.Start(Form.Values[0], Form.Values[1]);
                    else
                        Form.Editing = true;
                }
                return true;

            case 4:
                var count = Options.Count;
                if (ch == "w")
                    Selected = (Selected + count - 1) % count;
                else if (ch == "s")
                    Selected = (Selected + 1) % count;
                else if (ch == "a")
                    Adjust(-1);
                else if (ch == "d")
                    Adjust(1);
                else if (Input.IsConfirm(ch))
                    Adjust(1);
                return true;

            default:
                return true;
        }
    }

    /// <summary>Boot the listener, open the tunnel and run until the user quits.</summary>
    public async Task<int> RunAsync()
    {
        _ = Geo.LoadExitListAsync();
        ApplyTheme();
        Terminal.Write(Terminal.EnterAltScreen);
        var clock = Stopwatch.StartNew();
        try
        {
            await Splash.RunAsync().ConfigureAwait(false);
            while (true)
            {
                Sim.Step();
                var ctx = Context.Create(Runtime, clock.Elapsed.TotalSeconds);
                var lines = Views.All[Tab].Render(this, ctx);
                Terminal.Write(Terminal.Home
                    + string.Join('\n', lines.Select(l => l + Terminal.ClearLine))
                    + Terminal.ClearBelow);
                Terminal.Flush();

                var ch = Input.Poll();
                if (ch is not null && !Handle(ch))
                    break;
                await Task.Delay(TimeSpan.FromSeconds(1.0 / Math.Max(1, Get("Refresh Hz").Number)))
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        finally
        {
            Runtime.Stop();
            Terminal.Write(Terminal.LeaveAltScreen);
            Terminal.Flush();
        }
        foreach (var note in Runtime.Notes)
            Console.WriteLine(note);
        return 0;
    }
}