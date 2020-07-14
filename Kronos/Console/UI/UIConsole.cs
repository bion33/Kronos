using System;
using System.Collections.Generic;
using System.Linq;
using Console.Commands;

namespace Console.UI
{
    public class UIConsole
    {
        private const string HelpText = "\nKronos Quick Help\n" +
                                        "\n" +
                                        "    Syntax: Kronos [-d] [-k] [-o] [-q] [-t]\n" +
                                        "\n" +
                                        "Options:\n" +
                                        "  -d, -detag:   an update sheet limited to detag-able regions.\n" +
                                        "  -k, -kronos:  the full update times sheet.\n" +
                                        "  -o, -ops:     likely military operations from the last update.\n" +
                                        "  -q, -quit:    exit Kronos.\n" +
                                        "  -t, -timer:   time to when a region updates. Implies [-k].\n" +
                                        "\n" +
                                        "See \"Purpose & Use\" in the README for more information.\n" +
                                        "The order of the parameters determines the order of execution.\n" +
                                        "Use -q as the last parameter if you want Kronos to automatically quit.\n" +
                                        "\n";

        public static List<ICommand> GetCommands(string[] initialArgs)
        {
            var args = initialArgs.ToList();
            var commands = new List<ICommand>();

            var correctArgs = false;
            while (args.Count == 0 || !correctArgs)
            {
                correctArgs = args.Count > 0;
                foreach (var arg in args)
                    switch (arg.ToLower())
                    {
                        case "-q":
                        case "-quit": 
                            commands.Add(new Quit());
                            break;
                        case "-d":
                        case "-detag":
                            commands.Add(new Detag());
                            break;
                        case "-k":
                        case "-kronos":
                            commands.Add(new Kronos());
                            break;
                        case "-o":
                        case "-ops":
                            commands.Add(new Ops());
                            break;
                        case "-t":
                        case "-timer":
                            commands.Add(new Timer());
                            break;
                        default:
                            correctArgs = false;
                            break;
                    }

                if (args.Count > 0 && correctArgs) continue;

                Show(HelpText);
                Show("What do you want to do?\n");
                Show("Kronos ");
                args = GetInput().Split(" ").ToList();
            }

            return commands;
        }

        public static string GetUserInfo()
        {
            Show("You need to provide your nation name or email address once. This is needed to comply " +
                 "with NS script rules. It will be saved in config.txt in this folder so that " +
                 "you do not have to provide it again.\n");
            Show("User Information: ");
            return GetInput();
        }

        public static void Show(string message)
        {
            System.Console.Write(message);
        }

        public static string GetInput()
        {
            return System.Console.ReadLine();
        }

        public static bool Interrupted()
        {
            return System.Console.KeyAvailable && System.Console.ReadKey(true).Key == ConsoleKey.Q;
        }
    }
}