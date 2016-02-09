// -----------------------------------------------------------
// This program is private software, based on C# source code.
// To sell or change credits of this software is forbidden,
// except if someone approves it from the LeafyCoding INC. team.
// -----------------------------------------------------------
// Copyrights (c) 2016 LeafyYT-Downloader INC. All rights reserved.
// -----------------------------------------------------------

#region

using System;
using System.Windows;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Nini.Config;

#endregion

namespace LeafyYT_Downloader.Classes
{
    internal static class Config
    {
        private static IniConfigSource _configFile;

        public static void Init()
        {
            ((MainWindow) Application.Current.MainWindow).youtubeService =
                new YouTubeService(new BaseClientService.Initializer
                {
                    ApiKey = ConfigSetting("GoogleAPI", "ApiKey"),
                    ApplicationName = ConfigSetting("GoogleAPI", "ApplicationName")
                });
        }

        private static string ConfigSetting(string key, string setting)
        {
            _configFile = new IniConfigSource("config.ini");

            return _configFile.Configs[key].Get(setting);
        }
    }
}