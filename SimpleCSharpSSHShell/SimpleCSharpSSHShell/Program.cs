using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Renci.SshNet;
using System.Threading;
using ManyConsole.CommandLineUtils;

namespace SimpleCSharpSSHShell
{
    class Program
    {
        static int Main(string[] args)
        {
            // locate any commands in the assembly (or use an IoC container, or whatever source)
            var commands = GetCommands();

            // then run them.
            return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
        }

        public static IEnumerable<ConsoleCommand> GetCommands()
        {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));
        }
    }
}