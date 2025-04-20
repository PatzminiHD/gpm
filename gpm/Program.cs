using PatzminiHD.CSLib.Input.Console;

namespace gpm
{
    internal class Program
    {
        private static List<(List<string> names, CmdArgsParser.ArgType type)> validArgs = new()
        {
            (new(){"h", "help"}, CmdArgsParser.ArgType.SET),
            (new(){"l", "list"}, CmdArgsParser.ArgType.SET),
            (new(){"u", "upgrade"}, CmdArgsParser.ArgType.SET),
            (new(){"y", "confirm"}, CmdArgsParser.ArgType.SET),
        };

        public static AppSettings appSettings = new();
        static int Main(string[] args)
        {
            CmdArgsParser parser = new(args, validArgs);
            try
            {
                GetAppSettings();
                var parsedArgs = parser.Parse();

                if (parsedArgs.ContainsKey(validArgs[0].names))
                {
                    ShowHelpAndExit();
                }

                //User decided to upgrade (or did not specify any arguments besides -y)
                if(parsedArgs.ContainsKey(validArgs[2].names) || parsedArgs.ContainsKey(validArgs[3].names) || parsedArgs.Count == 0)
                {
                    if (parsedArgs.TryGetValue(validArgs[3].names, out _))
                    {
                        gpm.Upgrade.Run(true); //Run upgrade with auto-confirm
                    }
                    else
                    {
                        gpm.Upgrade.Run(false); //Run upgrade without auto-confirm
                    }
                }

                //User decided to list (and not to upgrade -> upgrade lists updates automatically)
                if (parsedArgs.ContainsKey(validArgs[1].names) && !parsedArgs.ContainsKey(validArgs[2].names))
                {
                    gpm.List.Run();
                }
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 2;
            }
            return 0;
            /*try
            {
                MainView mainView = new MainView();
                mainView.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Critical Exception occured:\n\n" + ex.ToString());
            }*/
        }

        private static void ShowHelpAndExit()
        {
            Console.WriteLine($"{appSettings.ApplicationName} {appSettings.ApplicationVersion}");
            Console.WriteLine($"using {PatzminiHD.CSLib.Info.Name} {PatzminiHD.CSLib.Info.Version}");
            Console.WriteLine($"");
            Console.WriteLine($"Usage: gpm [-hluy]");
            Console.WriteLine($"Command line arguments:");
            Console.WriteLine($"-h --help         Show this help");
            Console.WriteLine($"-l --list         Search if updates are available and display this information");
            Console.WriteLine($"-u --upgrade      Upgrade all applications");
            Console.WriteLine($"-y --confirm      Don't ask for confirmation before updating");
            Console.WriteLine($"");
            Console.WriteLine($"Running gpm without arguments is the same as running with the '-u' option");
            Environment.Exit(0);
        }
        private static void GetAppSettings()
        {
            if (File.Exists(appSettings.ApplicationSettingsFilePath))
            {
                appSettings = appSettings.Deserialze<AppSettings>();
                if (appSettings == null)
                {
                    var userChoice = PatzminiHD.CSLib.Input.Console.YesNo.Show("Deserializing AppSettings failed.\nDo you want to delete the old/broken AppSettings file?", false);
                    appSettings = new AppSettings();
                    if (userChoice)
                    {
                        File.Delete(appSettings.ApplicationSettingsFilePath);
                        appSettings.Serialize();
                    }
                }
                // Don't let the AppSettings.xml file overwrite the application name and version
                appSettings.ApplicationVersion = new AppSettings().ApplicationVersion;
                appSettings.ApplicationName = new AppSettings().ApplicationName;
            }
            else
            {
                Console.WriteLine("AppSettings.xml does not exist, creating...");
                Console.WriteLine("If this is your first time using this program, take a look at the documentation at:");
                Console.WriteLine($"https://github.com/{appSettings.updateSettings.selfRepoOwner}/{appSettings.updateSettings.selfRepoName}");
                appSettings.Serialize();
            }
        }
    }
}
