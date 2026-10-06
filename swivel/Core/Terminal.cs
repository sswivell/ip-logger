using System.Runtime.InteropServices;
using System.Text;

namespace Swivel.Core;

/// <summary>Console plumbing: UTF-8, VT sequences, alternate screen, sizing.</summary>
public static class Terminal
{
    public const string Esc = "\u001b[";
    public const string Reset = Esc + "0m";
    public const string Bold = Esc + "1m";

    public const string EnterAltScreen = Esc + "?1049h" + Esc + "?25l";
    public const string LeaveAltScreen = Esc + "?25h" + Esc + "?1049l" + Reset;
    public const string Home = Esc + "H";
    public const string ClearBelow = Esc + "J";
    public const string ClearLine = Esc + "K";
    public const string HideCursor = Esc + "?25l";
    public const string ShowCursor = Esc + "?25h";

    const int StdOutputHandle = -11;
    const uint EnableVirtualTerminalProcessing = 0x0004;

    static TextWriter? _out;

    /// <summary>Shared stdout writer, pinned to UTF-8 so block glyphs survive.</summary>
    public static TextWriter Out
    {
        get
        {
            if (_out is null)
            {
                try { Console.OutputEncoding = new UTF8Encoding(false); }
                catch (IOException) { /* redirected or unsupported codepage */ }
                _out = Console.Out;
            }
            return _out;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GetStdHandle(int handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetConsoleMode(IntPtr handle, out uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool SetConsoleMode(IntPtr handle, uint mode);

    public static bool EnableAnsi()
    {
        _ = Out;
        if (!OperatingSystem.IsWindows())
            return true;
        var handle = GetStdHandle(StdOutputHandle);
        return handle != IntPtr.Zero
            && GetConsoleMode(handle, out var mode)
            && SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
    }

    public static bool IsInteractive() => !Console.IsInputRedirected && !Console.IsOutputRedirected;

    /// <summary>Terminal size in cells, falling back to 80x40 when unavailable.</summary>
    public static (int Cols, int Rows) Size()
    {
        try
        {
            return (Console.WindowWidth, Console.WindowHeight);
        }
        catch (IOException)
        {
            return (80, 40);
        }
    }

    public static void Write(string text) => Out.Write(text);

    public static void Flush() => Out.Flush();

    public static void ClearScreen() => Write(Home + ClearBelow);
}