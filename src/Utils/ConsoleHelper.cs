namespace AdoPipelinesLocalRunner.Utils;

/// <summary>
/// Helper class for colored console output with ANSI color support.
/// Provides centralized color management and message formatting.
/// </summary>
public static class ConsoleHelper
{
    /// <summary>
    /// Writes a formatted header with application name and version.
    /// </summary>
    /// <param name="appName">Application name to display.</param>
    /// <param name="version">Version string to display.</param>
    public static void WriteHeader(string appName, string version)
    {
        WriteLineColored("╔═══════════════════════════════════════════════════════════════╗", ConsoleColor.Cyan);
        WriteLineColored($"║  {appName,-57} ║", ConsoleColor.Cyan);
        WriteLineColored($"║  Version: {version,-50} ║", ConsoleColor.Cyan);
        WriteLineColored("╚═══════════════════════════════════════════════════════════════╝", ConsoleColor.Cyan);
        Console.WriteLine();
    }

    /// <summary>
    /// Writes a success message in green.
    /// </summary>
    /// <param name="message">Message to display.</param>
    public static void WriteSuccess(string message) =>
        WriteLineColored($"✓ {message}", ConsoleColor.Green);

    /// <summary>
    /// Writes an error message in red.
    /// </summary>
    /// <param name="message">Message to display.</param>
    public static void WriteError(string message) =>
        WriteLineColored(message, ConsoleColor.Red);

    /// <summary>
    /// Writes a warning message in yellow.
    /// </summary>
    /// <param name="message">Message to display.</param>
    public static void WriteWarning(string message) =>
        WriteLineColored(message, ConsoleColor.Yellow);

    /// <summary>
    /// Writes an informational message in cyan.
    /// </summary>
    /// <param name="message">Message to display.</param>
    public static void WriteInfo(string message) =>
        WriteLineColored(message, ConsoleColor.Cyan);

    /// <summary>
    /// Writes a colored message without line break.
    /// </summary>
    /// <param name="message">Message to display.</param>
    /// <param name="color">Console color to use.</param>
    public static void WriteColored(string message, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.Write(message);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }

    /// <summary>
    /// Writes a colored message with line break.
    /// </summary>
    /// <param name="message">Message to display.</param>
    /// <param name="color">Console color to use.</param>
    public static void WriteLineColored(string message, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }
    }
}
