using System;
using System.IO;
using System.Threading;
using VirtualOS.Install;
using VirtualOS.OperatingSystem;
using VirtualOS.OperatingSystem.StatusCodes;

namespace VirtualOS.Boot
{
    enum BootMode {BootExisting, InstallNew, ExitManager}
    public class BootManager
    {
        private OperatingSystem.System _system;                

        public void Boot()
        {
            CommandLine.ColorLog("Welcome to the VirtualOS Boot Manager.", ConsoleColor.Cyan);
            while (true)
            {
                // Prompt User how to start the system
                var selected = SelectBootMode();
                if (selected == BootMode.InstallNew)
                {
                    SystemInstaller systemInstaller = new SystemInstaller();
                    try
                    {
                        // After system's installed, restart the boot manager
                        systemInstaller.Install();
                        CommandLine.GetInput("Press enter to reboot");
                        CommandLine.ClearScreen();
                    }
                    catch
                    {
                        CommandLine.Error("System not installed.\nRestarting Boot Manager.");
                    }
                }
                else if (selected == BootMode.BootExisting)
                {
                    // Start existing system with system .vos file and exit after system is shut down
                    var systemPath = SelectSystem();
                    StartSystem(systemPath);
                    break;
                }
                else if (selected == BootMode.ExitManager)
                {
                    CommandLine.ColorLog("Exiting Boot Manager.", ConsoleColor.DarkGreen);
                    break;
                }
            }
        }

        private void StartSystem(string systemInfo)
        {
            
            while (true)
            {
                _system = new OperatingSystem.System(systemInfo);
                
                // System return status code when it shuts down
                var exitCode = _system.Start();

                if (exitCode == SystemExitCode.Shutdown)
                {
                    CommandLine.ColorLog("System is shutting down...", ConsoleColor.DarkGreen);
                    break;
                }

                if (exitCode == SystemExitCode.Reboot)
                {
                    CommandLine.ColorLog("System's rebooting...", ConsoleColor.DarkGreen);
                    Thread.Sleep(1000);
                }
                
                if (exitCode == SystemExitCode.SystemBroken)
                {
                    CommandLine.Error("System could not run, shutting down...");
                    break;
                }
               
               
            }
           
        }

        private BootMode SelectBootMode()
        {
            CommandLine.DefaultLog(
                "System boot mode:\n1. Select existing VirtualOS.\n2. Install new VirtualOS\n3.Exit Boot Manager");
            while (true)
            {
                var input = CommandLine.GetInput("Mode");
                switch (input)
                {
                    case "1":
                        return BootMode.BootExisting;
                    case "2":
                        return BootMode.InstallNew;
                    case "3":
                        return BootMode.ExitManager;
                    default:
                        CommandLine.Error("Invalid Option. Select number of option.");
                        break;
                }
            }
        }

        // Find system file and return path to this file
        private string SelectSystem()
        {
            CommandLine.ColorLog("Select path to\nVirtualOS System folder\nor to\nSystems Config", ConsoleColor.Green);
            while (true)
            {
                var systemPath = CommandLine.GetInput("Path to .vos file");
                // Return path to the system if file found
                if (File.Exists(systemPath) && systemPath.EndsWith(".vos"))
                    return systemPath;
                
                CommandLine.Error("No System File found.");
                
            }
        }
    }
}