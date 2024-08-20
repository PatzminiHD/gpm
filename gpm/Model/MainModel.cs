using PatzminiHD.CSLib.Input.Console;
using PatzminiHD.CSLib.Output.Console.Table;
using PatzminiHD.CSLib.Network.SpecificApps;
using System.ComponentModel.Design;

namespace gpm.Model
{
    public class MainModel
    {
        public static AppSettings appSettings = new();

        public string Title
        {
            get
            {
                return $"{appSettings.ApplicationName} {appSettings.ApplicationVersion}";
            }
        }
        private List<(List<Entry>, uint)>? tableValues;
        public List<(List<Entry>, uint)> TableValues
        {
            get
            {
                if (tableValues == null)
                {
                    tableValues = new();
                    RefreshUpdate();
                }

                return tableValues;
            }
        }

        public MainModel()
        {
            GetAppSettings();
        }

        private void GetAppSettings()
        {
            if (File.Exists(appSettings.ApplicationSettingsFilePath))
            {
                appSettings = appSettings.Deserialze<AppSettings>();
                if (appSettings == null)
                {
                    var userChoice = MessageBox.Show("Deserializing AppSettings failed.\n" +
                        "Do you want to delete the old/broken AppSettings file?", "Error getting AppSettings", MessageBox.ResponseOptions.YES_NO);

                    appSettings = new AppSettings();
                    if (userChoice == MessageBox.Response.YES)
                    {
                        File.Delete(appSettings.ApplicationSettingsFilePath);
                        appSettings.Serialize();
                    }
                }
            }
            else
            {
                appSettings.Serialize();
            }
        }

        public void RefreshUpdate()
        {
            if(tableValues == null)
                tableValues = new();
            tableValues.Clear();

            List<Entry> rowValues = new();
            if(appSettings.updateSettings.updateApplications == null)
            {
                MessageBox.Show("Update Applications are null in AppSettings");
                return;
            }

            if(String.IsNullOrEmpty(appSettings.updateSettings.selfRepoOwner) ||
                String.IsNullOrEmpty(appSettings.updateSettings.selfRepoName) ||
                String.IsNullOrEmpty(appSettings.updateSettings.selfLocalDirectoryPath))
            {
                MessageBox.Show("Settings for self updating was not set!\nUpdating can not continue");
                return;
            }
            rowValues.Add(new Entry(appSettings.ApplicationName));
            var isSelfUpdateable = GitHubInterface.CheckForUpdates(out GitHub.GitHubRelease? selfRelease,
                    appSettings.updateSettings.selfRepoOwner,
                    appSettings.updateSettings.selfRepoName,
                    appSettings.updateSettings.selfLocalDirectoryPath,
                    null);
            if (isSelfUpdateable.Is<string>())
            {
                MessageBox.Show($"Getting update for application {appSettings.ApplicationName} failed:\n\n{isSelfUpdateable.Get<string>()}");
                rowValues.Add(new Entry("Failed"));
            }
            else
            {
                rowValues.Add(new Entry(isSelfUpdateable.Get<bool>().ToString()));
            }

            tableValues.Add((rowValues.ToList(), 1));
            rowValues.Clear();

            foreach (var item in appSettings.updateSettings.updateApplications)
            {
                rowValues.Add(new Entry(item.name));
                var isUpdateable = GitHubInterface.CheckForUpdates(out GitHub.GitHubRelease? release,
                    item.githubRepoOwner,
                    item.githubRepo,
                    item.localDirectoryPath,
                    null);
                if(isUpdateable.Is<string>())
                {
                    MessageBox.Show($"Getting update for application {item.name} failed:\n\n{isUpdateable.Get<string>()}");
                    rowValues.Add(new Entry("Failed"));
                }
                else
                {
                    rowValues.Add(new Entry(isUpdateable.Get<bool>().ToString()));
                }

                tableValues.Add((rowValues.ToList(), 1));
                rowValues.Clear();
            }
        }

        public void UpdateAll()
        {
            if (String.IsNullOrEmpty(appSettings.updateSettings.selfRepoOwner) ||
                String.IsNullOrEmpty(appSettings.updateSettings.selfRepoName) ||
                String.IsNullOrEmpty(appSettings.updateSettings.selfLocalDirectoryPath))
            {
                MessageBox.Show("Settings for self updating was not set!\nUpdating can not continue");
                return;
            }
            foreach (var item in TableValues)
            {
                if ((string)item.Item1[1].Value != "True")
                    continue;

                if (appSettings.updateSettings.updateApplications == null)
                    throw new Exception("List of Update Applications was null");

                List<(string name, string githubRepoOwner, string githubRepo, string localDirectoryPath)> updateApp = new();

                if ((string)item.Item1[0].Value == appSettings.ApplicationName)
                    updateApp.Add((appSettings.ApplicationName,
                        appSettings.updateSettings.selfRepoOwner,
                        appSettings.updateSettings.selfRepoName,
                        appSettings.updateSettings.selfLocalDirectoryPath));
                else
                    updateApp = appSettings.updateSettings.updateApplications.FindAll(p => p.name == (string)item.Item1[0].Value);

                if (updateApp.Count == 0)
                    throw new Exception($"No application with name {(string)item.Item1[0].Value} was found");

                GitHubInterface.UpdateFromRelease(updateApp[0].githubRepoOwner,
                    updateApp[0].githubRepo,
                    updateApp[0].localDirectoryPath);
            }
        }
    }
}
