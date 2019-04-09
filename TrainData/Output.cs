namespace Arcanist
{
    using System;

    internal class Output
    {
        public static void BlankLine()
        {
            Console.WriteLine();
        }

        public static Output Indent(int level)
        {
            Console.Write(string.Empty.PadLeft(level * 4));
            return new Output();
        }

        public static Output Write(string message, ConsoleColor colour = ConsoleColor.Gray)
        {
            Console.ForegroundColor = colour;
            Console.Write(message);
            return new Output();
        }

        public void End()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
        }

        public Output _(string message, ConsoleColor colour = ConsoleColor.Gray)
        {
            Console.ForegroundColor = colour;
            Console.Write(message);
            return this;
        }
    }
}