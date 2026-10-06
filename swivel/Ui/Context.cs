using Swivel.Core;

namespace Swivel.Ui;

/// <summary>Per-frame layout metrics derived from the terminal size.</summary>
public sealed class Context
{
    public const int MaxColumns = 100;
    public const int GapWidth = 2;
    public const int MinColumns = 40;

    Context(Runtime runtime, double now)
    {
        Runtime = runtime;
        Now = now;
        var (cols, rows) = Terminal.Size();
        Cols = Math.Max(MinColumns, cols);
        Rows = rows;
        Width = Math.Min(Cols - 2, MaxColumns);
        Margin = new string(' ', Math.Max(0, (Cols - Width) / 2));
        PanelWidth = (Width - GapWidth) / 2;
        Inner = PanelWidth - 4;
    }

    public Runtime Runtime { get; }
    public double Now { get; }
    public int Cols { get; }
    public int Rows { get; }
    public int Width { get; }
    public int Gap { get { return GapWidth; } }
    public int PanelWidth { get; }
    public int Inner { get; }
    public string Margin { get; }

    public string Indent(string line) => Margin + line;

    public List<string> Indent(IEnumerable<string> lines) => lines.Select(Indent).ToList();

    public static Context Create(Runtime runtime, double now) => new(runtime, now);
}