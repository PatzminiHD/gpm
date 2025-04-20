using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm
{
    internal class Upgrade
    {
        public static void Run(bool confirm)
        {
            var upgrades = List.ListUpdates();
            if (upgrades.Count == 0)
            {
                Console.WriteLine("\nNo updates available!");
                return;
            }
            if (confirm || PatzminiHD.CSLib.Input.Console.YesNo.Show("Do you want to upgrade all applications?", true))
            {
                if (Program.appSettings.updateSettings.updateApplications != null)
                {
                    foreach (var upgrade in upgrades)
                    {                        
                        Console.WriteLine($"Upgrading '{upgrade.Name}'...");
                        (string name, string githubRepoOwner, string githubRepo, string localDirectoryPath, string? accessToken) setting;
                        if (upgrade.Name == Program.appSettings.ApplicationName)
                        {
                            if (string.IsNullOrEmpty(Program.appSettings.updateSettings.selfRepoOwner) ||
                               string.IsNullOrEmpty(Program.appSettings.updateSettings.selfRepoName) ||
                               string.IsNullOrEmpty(Program.appSettings.updateSettings.selfLocalDirectoryPath) ||
                               string.IsNullOrEmpty(Program.appSettings.ApplicationName))
                            {
                                Console.WriteLine("Self update settings have not been set! Will not update self");
                                continue;
                            }
                            setting = (Program.appSettings.ApplicationName,
                                       Program.appSettings.updateSettings.selfRepoOwner,
                                       Program.appSettings.updateSettings.selfRepoName,
                                       Program.appSettings.updateSettings.selfLocalDirectoryPath,
                                       Program.appSettings.updateSettings.selfAccessToken);
                        }
                        else
                            setting = Program.appSettings.updateSettings.updateApplications.Where(p => p.name == upgrade.Name).First();

                        var upgradeResult = GitHubInterface.UpdateFromRelease(setting.githubRepoOwner, setting.githubRepo, setting.localDirectoryPath, false, setting.accessToken);
                        if(upgradeResult == null)
                        {
                            Console.WriteLine($"Error upgrading '{upgrade.Name}'. Reason: Upgrade result was null");
                            continue;
                        }
                        if(upgradeResult.Is<string>())
                        {
                            Console.WriteLine($"Error upgrading '{upgrade.Name}'. Reason: {upgradeResult.Get<string>()}");
                            continue;
                        }
                        if (!upgradeResult.Get<bool>())
                        {
                            Console.WriteLine($"Error upgrading '{upgrade.Name}'. Unkown Reason");
                        }
                        else
                        {
                            Console.WriteLine($"'{upgrade.Name}' Upgraded successfully!");
                        }
                    }
                }
            }
        }
    }
}
