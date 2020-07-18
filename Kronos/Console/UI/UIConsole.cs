using System;
using System.Collections.Generic;
using System.Linq;
using Console.Commands;

namespace Console.UI
{
    /// <summary> Console user interface. This class is for interaction with the user trough the console. </summary>
    public class UIConsole
    {
        private const string HelpText = "\nKronos Quick Help\n" +
                                        "\n" +
                                        "    Syntax: Kronos [-d] [-k] [-o] [-q] [-t]\n" +
                                        "\n" +
                                        "Options:\n" +
                                        "  -d, -detag:   an update sheet limited to detag-able regions.\n" +
                                        "  -k, -kronos:  an update sheet for all regions.\n" +
                                        "  -o, -ops:     likely military operations from the last update.\n" +
                                        "  -q, -quit:    exit Kronos. Use as last option to automatically quit.\n" +
                                        "  -t, -timer:   (approximate) count down to the moment a region updates.\n" +
                                        "\n" +
                                        "See \"Purpose & Use\" in the README for more information.\n" +
                                        "The order of the parameters determines the order of execution.\n" +
                                        "\n";

        /// <summary> Parse initial options or ask user </summary>
        public static List<ICommand> UserCommandInput(string[] initialOptions)
        {
            var options = initialOptions.ToList();
            var commands = new List<ICommand>();

            // Initially, assume the arguments are incorrect
            var correctOptions = false;
            while (options.Count == 0 || !correctOptions)
            {
                // If there are any options, assume they are correct
                correctOptions = options.Count > 0;

                foreach (var option in options)
                    switch (option.ToLower())
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
                            // The options are considered incorrect if any option didn't match any of the above
                            correctOptions = false;
                            break;
                    }

                // Skip asking new options if there are given options and they are correct
                if (options.Count > 0 && correctOptions) continue;

                // Ask new options
                Show(HelpText);
                Show("What do you want to do?\n");
                Show("Kronos ");
                options = GetInput().Split(" ").ToList();
            }

            return commands;
        }

        /// <summary> Get the user-specific part of the User-Agent to be sent with requests to NationStates </summary>
        public static string GetUserInfo()
        {
            Show("You need to provide your nation name or email address once. This is needed to comply " +
                 "with NS script rules. It will be saved in config.txt in this folder so that " +
                 "you do not have to provide it again.\n");
            Show("User Information: ");
            return GetInput();
        }

        /// <summary> Wrapper for Console.Write </summary>
        public static void Show(string message)
        {
            System.Console.Write(message);
        }

        /// <summary> Wrapper for Console.ReadLine </summary>
        public static string GetInput()
        {
            return System.Console.ReadLine();
        }

        /// <summary> Check if "Q" was pressed without blocking program execution </summary>
        public static bool Interrupted()
        {
            return System.Console.KeyAvailable && System.Console.ReadKey(true).Key == ConsoleKey.Q;
        }
    }
}