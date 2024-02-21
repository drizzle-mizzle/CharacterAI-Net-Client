using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharacterAiNetApiWrapper
{
    internal static class Helpers
    {
        internal static readonly string CD = Directory.GetCurrentDirectory();
        internal static readonly char SC = Path.DirectorySeparatorChar;

        internal static void LogRed(string? title = null, Exception? e = null)
        {
            if (title is not null)
                Log($"{title}\n", ConsoleColor.Red);

            if (e is not null)
                Log($"Exception details:\n{e}\n", ConsoleColor.Red);
        }

        internal static void Log(string text, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }
    }
}
