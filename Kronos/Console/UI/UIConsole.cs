using System.Collections.Generic;
using System.Linq;
using Console.Commands;

namespace Console.UI
{
    public class UIConsole
    {
        const string HelpText = "\nKronos Quick Help\n" +
        "\n" +
        "    Syntax: Kronos [-d] [-k] [-o] [-t]\n" +
        "\n" +
        "Options:\n" +
        "  -d, -detag:   an update sheet limited to detag-able regions.\n" +
        "  -k, -kronos:  the full update times sheet.\n" +
        "  -o, -ops:     likely military operations from the last update.\n" +
        "  -t, -timer:   time to when a region updates. Implies [-k].\n" +
        "\n" +
        "See \"Purpose & Use\" in the README for more information.\n" +
        "Use -q or -quit to quit." +
        "\n";

        public static List<ICommand> GetCommands(string[] initialArgs)
        {
            List<string> args = initialArgs.ToList();
            var commands = new List<ICommand>();

            var correctArgs = false;
            while (args.Count == 0 || !correctArgs)
            {
                correctArgs = args.Count > 0;
                foreach (var arg in args)
                {
                    switch (arg.ToLower())
                    {
                        case "-q": 
                        case "-quit": System.Environment.Exit(0); break;
                        case "-d":
                        case "-detag": commands.Add(new Detag()); break;
                        case "-k":
                        case "-kronos": commands.Add(new Kronos()); break;
                        case "-o":
                        case "-ops": commands.Add(new Ops()); break;
                        case "-t":
                        case "-timer": commands.Add(new Timer()); break;
                        default: correctArgs = false; break;
                    }
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
    }
}