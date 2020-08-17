using System;
using System.IO;
using System.Xml.Serialization;
using VirtualOS.Encryption;
using VirtualOS.OperatingSystem.Files;
using VirtualOS.OperatingSystem.StatusCodes;

namespace VirtualOS.OperatingSystem
{
    public class System
    {
        private FileSystem _fileSystem;
        private SystemInfo _info;
        private SystemUser _user;
        private CommandProcessor _commandProcessor;
        
        public System(string systemPath)
        {
            try
            {
                // On create, system will create file system to operate with data
                CommandLine.ClearScreen();
                _fileSystem = new FileSystem(systemPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                CommandLine.Error("Could not run the system.");
            }
        }

        // This method is used for clear all data about system in memory before shutting the system down
        private void ClearSystem()
        {
            _fileSystem.Close();
            _commandProcessor = null;
        }
        public SystemExitCode Start()
        {
            CommandLine.DefaultLog("Welcome to the system.");
            try
            {
                GetSystemInfo();
                LoginUser();
            }
            catch (Exception e)
            {
                CommandLine.Error("An error occured while starting the system;");
                return SystemExitCode.SystemBroken;
            }
            
            _commandProcessor = new CommandProcessor(_user, _info, ref _fileSystem);

            // When processing commands is done, system will receive exit code of Command Processor
            var exitCode = StartCommandProcessor();
            
            ClearSystem();
            // System will ask the boot manager to reboot if there was request from Command Processor
            if (exitCode == CommandProcessorCode.RebootRequest) return SystemExitCode.Reboot;
            return SystemExitCode.Shutdown;
        }

        // Read the user input and give the command to the Command Processor
        private CommandProcessorCode StartCommandProcessor()
        {
            while (true)
            {
                var processedCode = _commandProcessor.ProcessCommands();
                // If there's no default exit code, stop the processor with given exit code
                if (processedCode != CommandProcessorCode.Processed) return processedCode;
            }
        }

        // Load system info from sysinfo file
        private void GetSystemInfo()
        {
            try
            {
                var infoFile = _fileSystem.GetFile("sys/sysinfo.xml");
                using (Stream sr = infoFile.Open())
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(SystemInfo));
                    _info = (SystemInfo) serializer.Deserialize(sr);
                }
            }
            catch (NullReferenceException e)
            {
                CommandLine.Error("System's broken: No system information file file.");
                throw;
            }
        }
        
        // Ask the user for the login and password, if user is found, set user to the _user variable
        private void LoginUser()
        {
            try
            {
                while (true)
                {
                    var name = CommandLine.GetInput("System User");
                    if (!UserExists(name))
                    {
                        CommandLine.Error("No user found with name " + name);
                        continue;
                    }
                    var password = CommandLine.GetInput($"{name}'s password");
                    if (!ValidatePassword(name, password))
                    {
                        CommandLine.Error("Invalid password for user: " + name);
                        continue;
                    }

                    _user = new SystemUser(name);
                    break;
                }
            }
            catch
            {
                CommandLine.Error("Error while logging into the system");
                throw;
            }
        }
        
        private bool UserExists(string name)
        {
            try
            {
                var usersFile = _fileSystem.GetFile("sys/usr/users.info");
                using (StreamReader reader = new StreamReader(usersFile.Open()))
                {
                    var users = reader.ReadToEnd().Split("\n");
                    foreach (var user in users)
                    {
                        var userName = user.Split(":")[0];
                        if (userName == name)
                            return true;
                    }
                    return false;
                }
            }
            catch (NullReferenceException e)
            {
                CommandLine.Error("System's broken: User files not found in /sys/usr/");
                throw;
            }
        }

        // Try to find valid password for user in passwords file
        private bool ValidatePassword(string username, string userpass)
        {
            var passwordsFile = _fileSystem.GetFile("sys/usr/passwd.info");
            using (StreamReader reader = new StreamReader(passwordsFile.Open()))
            {
                var passwords = reader.ReadToEnd().Split("\n");
                foreach (var password in passwords)
                {
                    // First value represents username, the second one is password
                    var passwordLine = password.Split(":");
                    if (passwordLine[0] == username)
                        return Encryptor.CompareWithHash(userpass, passwordLine[1]);
                }
            }
            return false;
        }
    }
}