using PatzminiHD.CSLib;
using PatzminiHD.CSLib.Types;
using PatzminiHD.CSLib.Network;
using static PatzminiHD.CSLib.Network.SpecificApps.GitHub;

namespace gpm
{
    internal class GitHubInterface
    {
        public static Variant<bool, string> CheckForUpdates(out DataTypes.GpmUpdateEntry? returnRelease, string repoOwner, string repo, string localDirectory, string? accessToken = null)
        {
            if (String.IsNullOrWhiteSpace(accessToken))
                accessToken = null;
            returnRelease = null;
            string oldVersion, newVersion;
            var latestRelease = GetLatestRelease(repoOwner, repo, accessToken);

            if (latestRelease == null)
                return $"{nameof(GetLatestRelease)} returned null";

            if (latestRelease.Is<string>())
                return latestRelease.Get<string>();

            returnRelease = new()
            {
                GitHubRelease = latestRelease.Get<GitHubRelease>(),
                RemoteVersion = null,
                LocalVersion = null,
            };

            var assets = latestRelease.Get<GitHubRelease>().assets;

            if (assets == null || assets.Count == 0)
                return "The latest release does not contain assets";

            if (!Directory.Exists(localDirectory))
                return "The local Directory does not exist";

            if (string.IsNullOrEmpty(Program.appSettings.updateSettings.versionTrackerFileName))
                return $"{nameof(AppSettings.UpdateSettings.versionTrackerFileName)} was not set";

            string versionTrackerFile = Path.Combine(localDirectory, Program.appSettings.updateSettings.versionTrackerFileName);
            if (File.Exists(versionTrackerFile))
            {
                oldVersion = File.ReadAllText(versionTrackerFile);
            }
            else
            {
                //If no old version exists, treat it as lowest version
                oldVersion = "v0.0.0";
            }


            newVersion = GetVersionFromTagName(latestRelease.Get<GitHubRelease>().tag_name);

            returnRelease = new()
            {
                GitHubRelease = latestRelease.Get<GitHubRelease>(),
                RemoteVersion = newVersion,
                LocalVersion = oldVersion,
            };

            return PatzminiHD.CSLib.Settings.Base.IsGreaterVersion(newVersion, oldVersion);
            
        }

        public static Variant<bool, string> UpdateFromRelease(string repoOwner, string repo, string localDirectory, bool deleteOldVersion = false, string? accessToken = null)
        {
            var update = CheckForUpdates(out DataTypes.GpmUpdateEntry? release, repoOwner, repo, localDirectory, accessToken);

            if(update.Is<string>())
                return update;
            if (update.Is<bool>() && !update.Get<bool>())
                return "No update available";
            if (release == null)
                return "Release was null";
            if (string.IsNullOrEmpty(Program.appSettings.updateSettings.versionTrackerFileName))
                return $"{nameof(AppSettings.UpdateSettings.versionTrackerFileName)} was not set";

            foreach (var asset in release.Value.GitHubRelease.assets)
            {
                // Skip files for other operating systems
                if (PatzminiHD.CSLib.Environment.Get.OS == PatzminiHD.CSLib.Environment.Get.OperatingSystem.Linux)
                    if (asset.name.ToLower().Contains("windows"))
                        continue;
                if (PatzminiHD.CSLib.Environment.Get.OS == PatzminiHD.CSLib.Environment.Get.OperatingSystem.Windows)
                    if (asset.name.ToLower().Contains("linux"))
                        continue;

                string localFileName = Path.Combine(localDirectory, GetAssetNameWithoutVersion(asset.name));
                string localTmpFileName = localFileName + ".tmp";
                
                List<(string name, string value)>? customHeaders = new()
                {
                    ("Accept", $"application/octet-stream"),
                    ("X-GitHub-Api-Version", $"2022-11-28"),
                    ("Authorization", $"Bearer {accessToken}"),
                };
                FileDownloader fileDownloader = new FileDownloader(customHeaders);
                fileDownloader.DownloadFailed += FileDownloader_DownloadFailed;
                fileDownloader.DownloadProgess += FileDownloader_DownloadProgess;
                var downloadStatus = fileDownloader.Download(asset.url, localTmpFileName);
                downloadStatus.Wait();

                Console.WriteLine("Checking file hash...");
                var remoteFileHash = GetHashFromReleaseBody(release.Value.GitHubRelease.body, asset.name);
                if (remoteFileHash == null)
                    Console.WriteLine("Hash was not specified by remote");
                else
                {
                    if(!CheckFileHash(localTmpFileName, remoteFileHash))
                    {

                        return "File hash did not match!";
                    }
                    else
                        Console.WriteLine($"File hash matched!");
                }

                if (File.Exists(localFileName))
                {
                    string localVersion;
                    if (release.Value.LocalVersion == null)
                        localVersion = "v0.0.0";
                    else
                        localVersion = release.Value.LocalVersion;

                    if (deleteOldVersion)
                        File.Delete(localFileName);
                    else if (repoOwner == Program.appSettings.updateSettings.selfRepoOwner && repo == Program.appSettings.updateSettings.selfRepoName)
                    {
                        // ===========
                        // Self-update
                        // ===========
                        string backupVersion = localFileName + ".old_" + localVersion;
                        File.Move(localFileName, backupVersion);
                    }
                    else
                    {
                        string backupDirectory = Path.Combine(localDirectory, localVersion);
                        if (!Directory.Exists(backupDirectory))
                            Directory.CreateDirectory(backupDirectory);



                        File.Move(localFileName, Path.Combine(backupDirectory, Path.GetFileName(localFileName)));

                    }
                }

                File.Move(localTmpFileName, localFileName);
            }

            string versionTrackerFile = Path.Combine(localDirectory, Program.appSettings.updateSettings.versionTrackerFileName);
            if (File.Exists(versionTrackerFile))
                File.Delete(versionTrackerFile);

            File.WriteAllText(versionTrackerFile, GetVersionFromTagName(release.Value.GitHubRelease.tag_name));

            return true;
        }

        private static void FileDownloader_DownloadProgess(object? sender, FileDownloader.DownloadProgressEventArgs e)
        {
            //TODO
            //throw new NotImplementedException();
        }

        private static void FileDownloader_DownloadFailed(object? sender, FileDownloader.DownloadFailedEventArgs e)
        {
            //TODO
            Console.WriteLine($"DOWNLOAD FAILED! Reason:\n{e.FailReason}");
        }

        public static string GetVersionFromTagName(string name)
        {
            if(name == null)
                throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrEmpty(Program.appSettings.updateSettings.tagVersionSeperator))
                throw new Exception($"{nameof(AppSettings.updateSettings.tagVersionSeperator)} was not set");

            if (!name.Contains(Program.appSettings.updateSettings.tagVersionSeperator))
                throw new Exception($"{nameof(name)} does not contain a {nameof(AppSettings.UpdateSettings.tagVersionSeperator)}");

            return name.Split(Program.appSettings.updateSettings.tagVersionSeperator).Last();
        }

        public static string GetVersionFromAssetName(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (string.IsNullOrEmpty(Program.appSettings.updateSettings.fileVersionSeperator))
                throw new Exception($"{nameof(AppSettings.updateSettings.fileVersionSeperator)} was not set");

            if (!name.Contains(Program.appSettings.updateSettings.fileVersionSeperator))
                throw new Exception($"{nameof(name)} does not contain a {nameof(AppSettings.UpdateSettings.fileVersionSeperator)}");

            return name.Split(Program.appSettings.updateSettings.fileVersionSeperator).Last().Split('.' + name.Split('.').Last()).First();
        }

        public static bool CheckFileHash(string filepath, string hash)
        {
            return PatzminiHD.CSLib.Math.Crypto.GetSHA512HashString(filepath) == hash;
        }

        public static string? GetHashFromReleaseBody(string releaseBody, string fileName)
        {
            foreach(string line in releaseBody.Split('\n'))
            {
                if(line.StartsWith(fileName) && line.Contains('='))
                    return line.Substring(line.IndexOf('=')+1).Trim();
            }
            return null;
        }

        public static string GetAssetNameWithoutVersion(string assetName)
        {
            if (assetName == null)
                throw new ArgumentNullException(nameof(assetName));

            if (string.IsNullOrEmpty(Program.appSettings.updateSettings.fileVersionSeperator))
                throw new Exception($"{nameof(AppSettings.updateSettings.fileVersionSeperator)} was not set");

            if (!assetName.Contains(Program.appSettings.updateSettings.fileVersionSeperator))
                throw new Exception($"{nameof(assetName)} does not contain a {nameof(AppSettings.UpdateSettings.fileVersionSeperator)}");

            string fileExtension;

            //Linux executables do not have a file extension
            if (!assetName.Contains('.') || assetName.Split(Path.DirectorySeparatorChar).Last().ToLower().Contains("linux"))
                fileExtension = "";
            else
            {
                fileExtension = assetName.Split('.').Last();

                //The '.' was somewhere in a folder name, and the file does not have an extension
                if (fileExtension.Contains(Path.DirectorySeparatorChar))
                    return assetName.Split(Program.appSettings.updateSettings.fileVersionSeperator).First();
            }
            if(assetName.ToLower().StartsWith("linux") || assetName.ToLower().StartsWith("windows"))
                return assetName.Split(Program.appSettings.updateSettings.fileVersionSeperator)[1] + '.' + fileExtension;
            else
                return assetName.Split(Program.appSettings.updateSettings.fileVersionSeperator).First() + '.' + fileExtension;
        }
    }
}
