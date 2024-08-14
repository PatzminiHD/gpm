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
            public List<(string name, string githubRepoOwner, string githubRepo, string localDirectoryPath)>? updateApplications;
            public string? versionTrackerFileName;
            public string? tagVersionSeperator;
            public string? fileVersionSeperator;
        }

        public UpdateSettings updateSettings;

        public AppSettings()
        {
            ApplicationName = "gpm";
            ApplicationVersion = "v0.1.0";

            updateSettings = new UpdateSettings()
            {
                updateApplications = new()
                {
                    ("PatzminiHD.CSLib", "PatzminiHD", "PatzminiHD.CSLib", "E:\\Projects\\Programming\\CSharp\\gpm\\gpm\\lib\\")
                },
                versionTrackerFileName = "CurrentVersion.txt",
                tagVersionSeperator = "/",
                fileVersionSeperator= "_",
            };
        }
    }
}
