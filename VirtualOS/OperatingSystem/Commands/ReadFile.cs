using System;
using System.Collections.Generic;
using VirtualOS.OperatingSystem.Files;

namespace VirtualOS.OperatingSystem.Commands
{
    public class ReadFile : Command
    {
        private readonly FileSystem _fs;
        public ReadFile(ref FileSystem fs)
        {
            _aliases = new List<string>(new [] { "read", "cat" });
            _helpMessage = "read/cat <file> to write text to the file.";
            _fs = fs;
        }
        public override void Execute(List<string> args)
        {
            if (IsHelpRequested(args))
            {
                CommandLine.DefaultLog(_helpMessage);
                return;
            }
            // Second Param is a name of a file to read
            if (args.Count < 1)
            {
                CommandLine.Error("Specify file to read.");
                return;
            }

            var path = args[1];
            path = Path.ToAbsolutePath(path, CommandProcessor.CurrentLocation);

            if (!Path.IsFile(path, CommandProcessor.CurrentLocation))
            {
                CommandLine.Error("You can only read files.");
                return;
            }
            
            path = Path.ToZipFormat(path);
            CommandLine.DefaultLog(_fs.ReadFile(path));
            
        }
    }
}