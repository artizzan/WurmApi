﻿using System;
using System.IO;
using Microsoft.Win32;

namespace AldursLab.WurmApi.Modules.Wurm.InstallDirectory
{
    class WurmInstallDirectory : IWurmInstallDirectory
    {
        /// <param name="fullPath">Null to allow WurmApi to figure this on its own.</param>
        /// <exception cref="WurmGameClientInstallDirectoryValidationException">
        /// Specified path is not a valid Wurm client installation directory
        /// </exception>
        public WurmInstallDirectory(string fullPath = null)
        {
            if (fullPath == null)
            {
                fullPath = GetFromRegistry();
                if (fullPath == null)
                {
                    throw new WurmGameClientInstallDirectoryValidationException(
                        "No wurm game client install directory specified and failed to find it in registry.");
                }
            }

            var pathExists = Directory.Exists(fullPath);
            if (!pathExists)
            {
                throw new WurmGameClientInstallDirectoryValidationException(
                    "Directory does not exist: " + fullPath);
            }
            var expectedSubdirs = new[] { "configs", "packs", "players" };
            foreach (var expectedSubdir in expectedSubdirs)
            {
                if (!Directory.Exists(Path.Combine(fullPath, expectedSubdir)))
                {
                    throw new WurmGameClientInstallDirectoryValidationException(
                        "Directory does not have expected subdirectory: " + expectedSubdir);
                }
            }

            FullPath = fullPath;
        }

        public string FullPath { get; private set; }

        private string GetFromRegistry()
        {
            object regObj = Registry.GetValue(@"HKEY_CURRENT_USER\Software\JavaSoft\Prefs\com\wurmonline\client", "wurm_dir", null);
            if (regObj == null)
            {
                return null;
            }

            var wurmdir = Convert.ToString(regObj);
            wurmdir = wurmdir.Replace(@"//", @"\");
            wurmdir = wurmdir.Replace(@"/", @"");
            wurmdir = wurmdir.Trim();
            if (!wurmdir.EndsWith(@"\", StringComparison.Ordinal))
                wurmdir += @"\";
            return wurmdir;
        }
    }
}