using Swivel.Core;

namespace Swivel.Ui;

/// <summary>Animated boot sequence shown on the alternate screen.</summary>
public static class Splash
{
    const double DurationSeconds = 2.2;
    const int FramesPerSecond = 30;

    static readonly string[] Art =
    [
        "⠀⠀⠀⠀⠀⠀⠐⢶⣶⣶⣤⣄⣀⠀⠀⠀⠀⠀⠀⠀⠀⠀",
        "⠀⠀⠀⠀⠀⠀⠀⠈⢿⣿⣿⣿⣿⣿⣦⣄⠀⠀⠀⠀⠀⠀",
        "⠀⢀⣠⣶⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣄⠀⠀⠀⠀",
        "⠰⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣆⠀⠀⠀",
        "⠀⠀⠀⠈⠙⣿⣿⣿⣿⣿⣿⡿⠛⠛⢿⣿⣿⣿⣿⣄⣀⣀",
        "⠀⠀⠀⣠⣾⣿⣿⣿⣿⣿⣿⠁⢠⡄⠈⣿⣿⣿⣿⣿⣿⠟",
        "⠀⠀⣰⣿⣿⣿⣿⣿⣿⣿⣿⣷⡋⠀⠀⣿⣿⡿⢻⠟⠁⠀",
        "⠀⢠⡿⠟⠋⠉⠉⠀⠀⠉⠛⢿⣿⣷⣶⣿⣻⠁⠁⠀⠀⠀",
        "⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠉⠉⠉⠙⠃⠀⠀⠀⠀",
    ];

    static readonly string Title = "I P L O G G E R";
    static readonly string[] Spinner = ["|", "/", "-", "\\"];

    public static async Task RunAsync()
    {
        var artWidth = Art.Max(line => line.Length);
        var frameTime = TimeSpan.FromMilliseconds(1000.0 / FramesPerSecond);
        var start = DateTime.UtcNow;

        while (true)
        {
            var elapsed = (DateTime.UtcNow - start).TotalSeconds;
            if (elapsed >= DurationSeconds)
                break;
            var progress = Math.Min(1.0, elapsed / DurationSeconds);
            var (cols, _) = Terminal.Size();
            cols = Math.Max(40, cols);

            var lines = new List<string> { Terminal.Home };
            foreach (var row in Art)
            {
                var span = Math.Max(1, row.Length - 1);
                var painted = string.Concat(row.Select((ch, i) => ch == ' '
                    ? " "
                    : Theme.Fg(Rgb.Sample(Theme.Brand, ((i / (double)span) + elapsed * .6) % 1.0)) + ch));
                lines.Add(new string(' ', Math.Max(0, (cols - artWidth) / 2))
                    + painted + Terminal.ClearLine);
            }
            lines.Add("");
            lines.Add(new string(' ', Math.Max(0, (cols - Title.Length) / 2))
                + Theme.Gradient(Title, Theme.Brand, elapsed * .3));
            lines.Add("");
            lines.Add(ProgressLine(progress, elapsed, cols));

            Terminal.Write(string.Join('\n', lines) + Terminal.ClearBelow);
            Terminal.Flush();
            await Task.Delay(frameTime).ConfigureAwait(false);
        }
    }

    static string ProgressLine(double progress, double elapsed, int cols)
    {
        var barWidth = Math.Min(44, cols - 24);
        var filled = (int)(progress * barWidth);
        var bar = Theme.Fg(Theme.Amber) + new string('\u2588', filled)
            + Theme.Fg(Theme.Track) + new string('\u2591', Math.Max(0, barWidth - filled))
            + Terminal.Reset;
        var spin = Spinner[(int)(elapsed * 14) % Spinner.Length];
        var line = Theme.Fg(Rgb.Sample(Theme.Brand, (elapsed * .8) % 1.0)) + spin + Terminal.Reset
            + "  " + Theme.Fg(Theme.White) + "booting..." + Terminal.Reset
            + $"  [{bar}] {Terminal.Bold}{Theme.Fg(Theme.Amber)}"
            + $"{(int)(progress * 100),3}%{Terminal.Reset}";
        return new string(' ', Math.Max(0, (cols - Theme.VisibleLength(line)) / 2)) + line;
    }
}