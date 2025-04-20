using PatzminiHD.CSLib.Network.SpecificApps;
using PatzminiHD.CSLib.Output.Console.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm
{
    internal class List
    {
        public static void Run()
        {
            var updates = ListUpdates();
            if (updates.Count == 0)
            {
                Console.WriteLine("\nNo updates available!");
            }
        }

        public static List<DataTypes.GpmUpdateEntry> ListUpdates()
        {
            List<DataTypes.GpmUpdateEntry> updates = new();
            if (Program.appSettings.updateSettings.updateApplications != null)
            {
                foreach (var entry in Program.appSettings.updateSettings.updateApplications)
                {
                    var checkResult = GitHubInterface.CheckForUpdates(out DataTypes.GpmUpdateEntry? updateEntry,
                                                                        entry.githubRepoOwner,
                                                                        entry.githubRepo,
                                                                        entry.localDirectoryPath,
                                                                        entry.accessToken);
                    if(checkResult.Is<string>())
                    {
                        Console.WriteLine($"Checking failed for '{entry.name}', reason: {checkResult.Get<string>()}");
                        continue;
                    }
                    if(updateEntry == null)
                    {
                        Console.WriteLine($"Checking failed for '{entry.name}', reason: Update entry is null!");
                        continue;
                    }
                    updateEntry = new()
                    {
                        Name = entry.name,
                        GitHubRelease = updateEntry.Value.GitHubRelease,
                        RemoteVersion = updateEntry.Value.RemoteVersion,
                        LocalVersion = updateEntry.Value.LocalVersion,
                    };
                    if (checkResult.Get<bool>())
                    {
                        Console.WriteLine($"{entry.name}: Remote version: {updateEntry.Value.RemoteVersion} LocalVersion: {updateEntry.Value.LocalVersion}; Update available!");
                        updates.Add(updateEntry.Value);
                    }
                    else
                    {
                        Console.WriteLine($"{entry.name}: Remote version: {updateEntry.Value.RemoteVersion} LocalVersion: {updateEntry.Value.LocalVersion}; Newest version already installed!");
                    }
                }
            }
            //Check self update
            if (string.IsNullOrEmpty(Program.appSettings.updateSettings.selfRepoOwner) ||
               string.IsNullOrEmpty(Program.appSettings.updateSettings.selfRepoName) ||
               string.IsNullOrEmpty(Program.appSettings.updateSettings.selfLocalDirectoryPath) ||
               string.IsNullOrEmpty(Program.appSettings.ApplicationName))
            {
                Console.WriteLine("Self update settings have not been set! Will not update self");
                return updates;
            }

            var selfCheckResult = GitHubInterface.CheckForUpdates(out DataTypes.GpmUpdateEntry? selfUpdateEntry,
                                                                    Program.appSettings.updateSettings.selfRepoOwner,
                                                                    Program.appSettings.updateSettings.selfRepoName,
                                                                    Program.appSettings.updateSettings.selfLocalDirectoryPath,
                                                                    Program.appSettings.updateSettings.selfAccessToken);
            if (selfCheckResult.Is<string>())
            {
                Console.WriteLine($"Checking failed for '{Program.appSettings.ApplicationName}', reason: {selfCheckResult.Get<string>()}");
            }
            if (selfUpdateEntry == null)
            {
                Console.WriteLine($"Checking failed for '{Program.appSettings.ApplicationName}', reason: Update entry is null!");
                return updates;
            }
            selfUpdateEntry = new()
            {
                Name = Program.appSettings.ApplicationName,
                GitHubRelease = selfUpdateEntry.Value.GitHubRelease,
                RemoteVersion = selfUpdateEntry.Value.RemoteVersion,
                LocalVersion = selfUpdateEntry.Value.LocalVersion,
            };
            if (selfCheckResult.Get<bool>())
            {
                Console.WriteLine($"{Program.appSettings.ApplicationName}: Remote version: {selfUpdateEntry.Value.RemoteVersion} LocalVersion: {selfUpdateEntry.Value.LocalVersion}; Update available!");
                updates.Add(selfUpdateEntry.Value);
            }
            else
            {
                Console.WriteLine($"{Program.appSettings.ApplicationName}: Remote version: {selfUpdateEntry.Value.RemoteVersion} LocalVersion: {selfUpdateEntry.Value.LocalVersion}; Newest version already installed!");
            }

            return updates;
        }
    }
}
