namespace Swivel.Core;

/// <summary>Non-blocking keystroke polling and inline text buffer editing.</summary>
public static class Input
{
    public const string Escape = "\u001b";
    public const string Enter = "\r";

    const int SequenceWindowMs = 80;
    const int SequenceExtendMs = 40;

    /// <summary>One keypress as a string, or null when the queue is empty.</summary>
    public static string? Poll()
    {
        if (!Console.KeyAvailable)
            return null;

        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Escape)
        {
            DrainSequence();
            return Escape;
        }
        if (key.KeyChar == '\0')
        {
            // Arrow/navigation key: swallow the rest of its escape sequence.
            DrainSequence();
            return null;
        }
        return key.KeyChar.ToString();
    }

    static void DrainSequence()
    {
        var deadline = Environment.TickCount64 + SequenceWindowMs;
        while (Environment.TickCount64 < deadline)
        {
            if (Console.KeyAvailable)
            {
                Console.ReadKey(intercept: true);
                deadline = Environment.TickCount64 + SequenceExtendMs;
            }
            else
            {
                Thread.Sleep(5);
            }
        }
    }

    /// <summary>Apply one character to the buffer being edited.</summary>
    public static string Edit(string ch, string buffer) => ch switch
    {
        "\u007f" or "\b" => buffer.Length > 0 ? buffer[..^1] : buffer,
        _ when ch.Length == 1 && ch[0] >= ' ' => buffer + ch,
        _ => buffer,
    };

    public static bool IsEnter(string ch) => ch is "\r" or "\n";

    public static bool IsConfirm(string ch) => ch is "\r" or "\n" or " ";
}