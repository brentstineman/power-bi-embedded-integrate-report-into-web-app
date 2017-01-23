﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProvisionSample
{
    /// <summary>
    /// Container for async methods, collectng user input and executing PaaS functinality
    /// </summary>
    public class Commands
    {
        private readonly List<Tuple<string, Func<Task>>> m_commands = new List<Tuple<string, Func<Task>>>();

        public void RegisterCommand(string description, Func<Task> operation)
        {
            m_commands.Add(Tuple.Create(description, operation));
        }

        public Func<Task> GetCommand(int commandNumber)
        {
            if (commandNumber >= m_commands.Count)
            {
                return null;
            }
            return m_commands[commandNumber].Item2;
        }

        public string GetCommandDescription(int commandNumber)
        {
            if (commandNumber >= m_commands.Count)
            {
                throw new Exception("Unknown command " + commandNumber);
            }
            return m_commands[commandNumber].Item1;
        }

        public int Count { get { return m_commands.Count; } }
    }

    /// <summary>
    /// List of avalable administrative commands. Sholud all differ in prefix
    /// </summary>
    public enum AdminCommands
    {
        /// <summary>
        /// Exit application
        /// </summary>
        ExitTool,

        /// <summary>
        /// Clear cached parameters
        /// </summary>
        ClearSettings,

        /// <summary>
        /// iterate on all cached parameter to clear/re-assign/leave as is
        /// </summary>
        ManageSettings,

        /// <summary>
        /// Display all cached parameters
        /// </summary>
        DisplaySettings,
    };

    /// <summary>
    /// Utilities for getting user insertion. To be overritten for processing scripts
    /// </summary>
    public class UserInput
    {
        public virtual int? EnsureIntParam(int? param, string desc, bool onlyFillIfEmpty = false, bool forceReEnter = false)
        {
            bool available = param.HasValue;
            if (onlyFillIfEmpty && available)
            {
                return param;
            }

            if (available)
            {
                ConsoleHelper.WriteColoredValue(desc, param.Value.ToString(), ConsoleColor.Magenta, forceReEnter ? ". Re-Enter same, or new int value:" : ". Press enter to use, or give new int value:");
            }
            else
            {
                Console.Write(desc + " is required. Enter int value:");
            }

            var entered = Console.ReadLine();
            int val;
            if (!string.IsNullOrWhiteSpace(entered))
            {
                if (!Int32.TryParse(entered, out val))
                {
                    Console.WriteLine("illegal int value:[" + entered + "]");
                    return null;
                }
                param = val;
            }
            return null;
        }

        public virtual string EnsureParam(string param, string desc, bool onlyFillIfEmpty = false, bool forceReEnter = false, bool isPassword = false)
        {
            bool available = !string.IsNullOrWhiteSpace(param);
            if (onlyFillIfEmpty && available)
            {
                return param;
            }

            if (available)
            {
                ConsoleHelper.WriteColoredValue(desc, param, ConsoleColor.Magenta, forceReEnter ? ". Re-Enter same, or new value:" : ". Press enter to use, or give new value:");
            }
            else
            {
                Console.Write(desc + " is required. Enter value:");
            }

            var entered = isPassword ? ConsoleHelper.ReadPassword() : Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(entered))
            {
                param = entered;
            }

            return param;
        }

        public virtual string EnterOptionalParam(string desc, string skipResultDescription)
        {
            ConsoleHelper.WriteColoredValue(desc + " (optional). Enter value (or press Enter to ", skipResultDescription, ConsoleColor.Magenta, "):");

            var entered = Console.ReadLine();
            Console.WriteLine();
            if (!string.IsNullOrWhiteSpace(entered))
            {
                return entered;
            }

            return null;
        }
        public virtual string ManageCachedParam(string param, string desc, bool forceReset = false)
        {
            if (forceReset)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(param))
            {
                ConsoleHelper.WriteColoredValue(desc, param, ConsoleColor.Magenta, ". Enter 'Y': to Reset, 'A': to assign, Q: to Quit, Any another key to skip:");
            }
            else
            {
                ConsoleHelper.WriteColoredValue(desc, param, ConsoleColor.Magenta, ". Enter 'A': to assign, Q: to Quit, Any another key to skip:");
            }
            var ch = Char.ToUpper(Console.ReadKey().KeyChar);
            Console.WriteLine();
            switch (ch)
            {
                case 'Y':
                    return null;
                case 'A':
                    param = EnsureParam(null, desc);
                    break;
                case 'Q':
                    throw new Exception(string.Format("Quit managing cache when on '{0}', value ={1}", desc, param));
                default:
                    break;
            }
            return param;
        }

        public virtual void GetUserCommandSelection(out AdminCommands? adminCommand, out int? numericCommand)
        {
            numericCommand = null;
            adminCommand = null;
            while (true)
            {
                var command = Console.ReadLine();
                if (string.IsNullOrEmpty(command))
                {
                    Console.WriteLine("No input. Try again");
                    continue;
                }

                int val;
                if (int.TryParse(command, out val))
                {
                    numericCommand = val;
                    return;
                }
                if (command.Length >= 1)
                {
                    foreach (AdminCommands ac in Enum.GetValues(typeof(AdminCommands)))
                    {
                        if (ac.ToString().StartsWith(command, StringComparison.CurrentCultureIgnoreCase))
                        {
                            adminCommand = ac;
                            return;
                        }
                    }
                }
                Console.WriteLine("Illegal input. Try again");
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class ConsoleHelper
    {
        public static void PrintCommands(Commands commands)
        {
            var color = ConsoleColor.Green;
            Console.WriteLine();
            Console.Write("What do you want to do (select ");
            Console.ForegroundColor = color;
            Console.Write("prefix/numeric");
            Console.ResetColor();
            Console.WriteLine(" value)?");
            Console.WriteLine("=================================================================");
            foreach (AdminCommands ac in Enum.GetValues(typeof(AdminCommands)))
            {
                WriteColoredStringLine(ac.ToString(), color, 1);
            }
            Console.WriteLine();
            for (int i = 0; i < commands.Count; i++)
            {
                var numericSize = i < 9 ? 1 : ((i < 99) ? 2 : 3);
                var align = i < 9 ? " " : "";
                WriteColoredStringLine(string.Format("{0} {1} {2}", i + 1, align, commands.GetCommandDescription(i)), color, numericSize);
            }
            Console.WriteLine();
        }

        public static void WriteColoredStringLine(string text, ConsoleColor color, int coloredChars)
        {
            Console.ForegroundColor = color;
            Console.Write(text.Substring(0, coloredChars));
            Console.ResetColor();
            Console.WriteLine(text.Substring(coloredChars));
        }

        public static void WriteColoredValue(string desc, string param, ConsoleColor color, string restOfLine = null)
        {
            Console.Write(desc + " = ");
            Console.ForegroundColor = color;
            Console.Write(param);
            Console.ResetColor();
            if (restOfLine != null)
                Console.Write(restOfLine);
        }

        public static string ReadPassword()
        {
            ConsoleKeyInfo key;
            var password = string.Empty;

            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                    }
                }
            }
            // Stops Receving Keys Once Enter is Pressed
            while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }
    }
}
