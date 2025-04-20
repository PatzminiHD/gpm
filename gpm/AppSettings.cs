using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PatzminiHD.CSLib.Settings;

namespace gpm
{
    public class AppSettings : Base
    {
        public class UpdateSettings
        {
            public string? selfRepoOwner;
            public string? selfRepoName;
            public string? selfLocalDirectoryPath;
            public string? selfAccessToken;
            public List<(string name, string githubRepoOwner, string githubRepo, string localDirectoryPath, string? accessToken)>? updateApplications;
            public string? versionTrackerFileName;
            public string? tagVersionSeperator;
            public string? fileVersionSeperator;
        }

        public UpdateSettings updateSettings;

        public AppSettings()
        {
            ApplicationName = "gpm";
            ApplicationVersion = "v2.0.0";

            updateSettings = new UpdateSettings()
            {
                selfRepoOwner = "PatzminiHD",
                selfRepoName = "gpm",
                selfLocalDirectoryPath = "C:\\Program Files\\PatzminiHD\\gpm\\",
                selfAccessToken = "",
                updateApplications = new()
                {
                    ("Example (GitHub Repo name)", "Example (Github Repo Owner)", "Example (Name shown in gpm)", "Z:\\Example\\Path\\To\\Local\\Copy\\(Local path of the application)\\", "Example (Access token)")
                },
                versionTrackerFileName = "CurrentVersion.txt",
                tagVersionSeperator = "/",
                fileVersionSeperator = "_",
            };
        }
    }
}
