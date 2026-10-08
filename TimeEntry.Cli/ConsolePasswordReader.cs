using System.Text;

namespace TimeEntry.Cli;

public static class ConsolePasswordReader
{
    /// <summary> Reads a password from the console with masked input. Never echoes or logs the value. </summary>
    public static string Read(string prompt)
    {
        Console.Write(prompt);
        var builder = new StringBuilder();

        ConsoleKeyInfo key;
        while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace)
            {
                if (builder.Length > 0)
                {
                    builder.Length--;
                    Console.Write("\b \b");
                }
                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                builder.Append(key.KeyChar);
                Console.Write('*');
            }
        }

        Console.WriteLine();
        return builder.ToString();
    }
}
