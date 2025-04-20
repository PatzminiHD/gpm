using PatzminiHD.CSLib.Network.SpecificApps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gpm
{
    internal class DataTypes
    {
        public struct GpmUpdateEntry
        {
            public string Name;
            public GitHub.GitHubRelease GitHubRelease;
            public string? RemoteVersion;
            public string? LocalVersion;
        }
    }
}
