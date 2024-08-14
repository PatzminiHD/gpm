using PatzminiHD.CSLib.Network.SpecificApps;
using PatzminiHD.CSLib.Types;
using PatzminiHD.CSLib.Network;

namespace gpm.Model
{
    public class GitHubInterface
    {
        public static Variant<bool, string> CheckForUpdates(out GitHub.GitHubRelease? returnRelease, string repoOwner, string repo, string localDirectory, string? accessToken = null)
        {
            returnRelease = null;
            string oldVersion, newVersion;
            var latestRelease = GitHub.GetLatestRelease(repoOwner, repo, accessToken);

            if (latestRelease == null)
                return $"{nameof(GitHub.GetLatestRelease)} returned null";

            if (latestRelease.Is<string>())
                return latestRelease.Get<string>();

            returnRelease = latestRelease.Get<GitHub.GitHubRelease>();

            var assets = latestRelease.Get<GitHub.GitHubRelease>().assets;

            if (assets == null || assets.Count == 0)
                return "The latest release does not contain assets";

            if (!Directory.Exists(localDirectory))
                return "The local Directory does not exist";

            if (String.IsNullOrEmpty(MainModel.appSettings.updateSettings.versionTrackerFileName))
                return $"{nameof(AppSettings.UpdateSettings.versionTrackerFileName)} was not set";

            string versionTrackerFile = Path.Combine(localDirectory, MainModel.appSettings.updateSettings.versionTrackerFileName);
            if (File.Exists(versionTrackerFile))
            {
                oldVersion = File.ReadAllText(versionTrackerFile);
            }
            else
            {
                //If no old version exists, treat it as lowest version
                oldVersion = "v0.0.0";
            }


            newVersion = GetVersionFromTagName(latestRelease.Get<GitHub.GitHubRelease>().tag_name);

            return PatzminiHD.CSLib.Settings.Base.IsGreaterVersion(newVersion, oldVersion);
            
        }

        public static Variant<bool, string> UpdateFromRelease(string repoOwner, string repo, string localDirectory, bool deleteOldVersion = false, string? accessToken = null)
        {
            var update = CheckForUpdates(out GitHub.GitHubRelease? release, repoOwner, repo, localDirectory, accessToken);

            if(update.Is<string>())
                return update;
            if (update.Is<bool>() && !update.Get<bool>())
                return "No update available";
            if (release == null)
                return "Release was null";
            if (string.IsNullOrEmpty(MainModel.appSettings.updateSettings.versionTrackerFileName))
                return ($"{nameof(AppSettings.UpdateSettings.versionTrackerFileName)} was not set");

            foreach (var asset in release.Value.assets)
            {
                string localFileName = Path.Combine(localDirectory, GetAssetNameWithoutVersion(asset.name));
                if (File.Exists(localFileName))
                {
                    if (deleteOldVersion)
                        File.Delete(localFileName);
                    else
                    {
                        string backupDirectory = Path.Combine(localDirectory, GetVersionFromAssetName(asset.name));
                        if (!Directory.Exists(backupDirectory))
                            Directory.CreateDirectory(backupDirectory);


                        foreach(var file in Directory.GetFiles(localDirectory))
                            File.Move(file, Path.Combine(backupDirectory, file));

                        
                    }
                }

                FileDownloader fileDownloader = new FileDownloader();
                fileDownloader.DownloadFailed += FileDownloader_DownloadFailed;
                var downloadStatus = fileDownloader.Download(asset.browser_download_url, localFileName);
                downloadStatus.Wait();
            }

            string versionTrackerFile = Path.Combine(localDirectory, MainModel.appSettings.updateSettings.versionTrackerFileName);
            if (File.Exists(versionTrackerFile))
                File.Delete(versionTrackerFile);

            File.WriteAllText(versionTrackerFile, GetVersionFromTagName(release.Value.tag_name));

            return true;
        }

        private static void FileDownloader_DownloadFailed(object? sender, FileDownloader.DownloadFailedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static string GetVersionFromTagName(string name)
        {
            if(name == null)
                throw new ArgumentNullException(nameof(name));

            if (String.IsNullOrEmpty(MainModel.appSettings.updateSettings.tagVersionSeperator))
                throw new Exception($"{nameof(AppSettings.updateSettings.tagVersionSeperator)} was not set");

            if (!name.Contains(MainModel.appSettings.updateSettings.tagVersionSeperator))
                throw new Exception($"{nameof(name)} does not contain a {nameof(AppSettings.UpdateSettings.tagVersionSeperator)}");

            return name.Split(MainModel.appSettings.updateSettings.tagVersionSeperator).Last();
        }

        public static string GetVersionFromAssetName(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (String.IsNullOrEmpty(MainModel.appSettings.updateSettings.fileVersionSeperator))
                throw new Exception($"{nameof(AppSettings.updateSettings.fileVersionSeperator)} was not set");

            if (!name.Contains(MainModel.appSettings.updateSettings.fileVersionSeperator))
                throw new Exception($"{nameof(name)} does not contain a {nameof(AppSettings.UpdateSettings.fileVersionSeperator)}");

            return name.Split(MainModel.appSettings.updateSettings.fileVersionSeperator).Last();
        }

        public static string GetAssetNameWithoutVersion(string assetName)
        {
            if (assetName == null)
                throw new ArgumentNullException(nameof(assetName));

            if (String.IsNullOrEmpty(MainModel.appSettings.updateSettings.fileVersionSeperator))
                throw new Exception($"{nameof(AppSettings.updateSettings.fileVersionSeperator)} was not set");

            if (!assetName.Contains(MainModel.appSettings.updateSettings.fileVersionSeperator))
                throw new Exception($"{nameof(assetName)} does not contain a {nameof(AppSettings.UpdateSettings.fileVersionSeperator)}");

            string fileExtension;

            if (!assetName.Contains('.'))
                fileExtension = "";
            else
            {
                fileExtension = assetName.Split('.').Last();

                //The '.' was somewhere in a folder name, and the file does not have an extension
                if (fileExtension.Contains(Path.DirectorySeparatorChar))
                    return assetName.Split(MainModel.appSettings.updateSettings.fileVersionSeperator).First();
            }

            return assetName.Split(MainModel.appSettings.updateSettings.fileVersionSeperator).First() + '.' + fileExtension;
        }
    }
}
