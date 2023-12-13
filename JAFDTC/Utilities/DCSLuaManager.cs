// ********************************************************************************************************************
//
// DCSLuaManager.cs : manages install/remove/update of lua files in dcs install to support jafdtc
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023 ilominar/raven
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along with this program.  If not, see
// <https://www.gnu.org/licenses/>.
//
// ********************************************************************************************************************

#undef USE_DCS_TEST_FOLDER

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace JAFDTC.Utilities
{
    /// <summary>
    /// provides detailed information on the outcome of a lua installation check. the install and update paths
    /// list the paths to install or update jafdtc lua support, respectively. error information provides details
    /// on failures.
    /// </summary>
    public sealed class DCSLuaManagerCheckResult
    {
        public string ErrorTitle { get; set; }
        public string ErrorMessage {  get; set; }
        public List<string> InstallPaths { get; set; }
        public List<string> UpdatePaths { get; set; }

        public DCSLuaManagerCheckResult()
        {
            InstallPaths = new List<string>();
            UpdatePaths = new List<string>();
        }
    }

    /// <summary>
    /// TODO: document
    /// </summary>
    public static class DCSLuaManager
    {
        public enum DCSLuaVersion
        {
            NONE = -1,
            CURRENT = 1
        };

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
                                                  uint dwFlags,
                                                  IntPtr hToken = default);

        static readonly Guid _savedGamesFolderGuid = new("4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4");
#if USE_DCS_TEST_FOLDER
        static readonly List<string> _installFolders = new() { "TEST_DCS", "TEST_DCS.openbeta" };
#else
        static readonly List<string> _installFolders = new() { "DCS", "DCS.openbeta" };
#endif
        static readonly string DCSExportMagic = "local JAFDTClfs=require('lfs'); dofile(JAFDTClfs.writedir()..'Scripts/JAFDTC/JAFDTC.lua')";

        // returns true if (according to settings) there is at least one valid lua install, false otherwise.
        //
        public static bool IsLuaInstalled()
        {
                foreach (KeyValuePair<string, DCSLuaVersion> kvp in Settings.VersionDCSLua)
				{
					if ((kvp.Value != DCSLuaVersion.NONE) && IsJAFDTCInstalled(kvp.Key))
					{
						return true;
					}
                }
				return false;
        }

        // check the status of any lua installations in the _installFolders to flag common errors and determine which
        // folders need updates or fresh installs. return a DCSLuaManagerCheckResult with information on the situation
        // that can be used to determine how to proceed.
        //
        public static DCSLuaManagerCheckResult LuaCheck()
        {
            DCSLuaManagerCheckResult result = new();

            if (Settings.IsSkipDCSLuaInstall) return result;

            string savedGamesPath = SHGetKnownFolderPath(_savedGamesFolderGuid, 0);
            if (string.IsNullOrEmpty(savedGamesPath))
            {
                result.ErrorTitle = "Missing Saved Games Folder";
                result.ErrorMessage = "The Saved Games folder was not found. Lua support cannot be installed.\n\n" +
                                      "To install Lua support at a later time, use the JAFDTC Settings page.";
                foreach (string path in Settings.VersionDCSLua.Keys)
                {
                    Settings.SetVersionDCSLua(path, DCSLuaVersion.NONE);
                }
                return result;
            }

            int nInvalid = 0;
            foreach (string folder in _installFolders)
            {
                string dcsPath = Path.Combine(savedGamesPath, folder);
                bool isDCSPathValid = Directory.Exists(dcsPath);
                if (isDCSPathValid && !IsJAFDTCInstalled(dcsPath))
                {
                    result.InstallPaths.Add(dcsPath);
                }
                else if (isDCSPathValid)
                {
                    result.UpdatePaths.Add(dcsPath);
                }
                else
                {
                    Settings.SetVersionDCSLua(dcsPath, DCSLuaVersion.NONE);
                    nInvalid++;
                }
            }

            if (nInvalid == _installFolders.Count)
            {
                result.ErrorTitle = "Missing DCS Install Folder";
                result.ErrorMessage = "A DCS or DCS.openbeta install folder for DCS was not found in Saved Games. Please launch DCS and exit it to create this folder.\n\n" +
                                      "To install Lua support at a later time, use the JAFDTC Settings page.";
                result.InstallPaths.Clear();
                result.UpdatePaths.Clear();
            }

            return result;
        }

        // TODO: document
        //
        public static bool InstallOrUpdateLua(string path, out bool didUpdate)
        {
            didUpdate = false;
            try
            {
                string scriptsFolder = Path.Combine(path, "Scripts");
                if (!Directory.Exists(scriptsFolder))
                {
                    Directory.CreateDirectory(scriptsFolder);
                }

                if (CopyLuaFilesToDCS("JAFDTC", scriptsFolder) || CopyLuaFilesToDCS("Hooks", scriptsFolder))
                {
                    didUpdate = true;
                }

                string exportLuaPath = Path.Combine(scriptsFolder, "Export.lua");
                if (!File.Exists(exportLuaPath))
                {
                    File.WriteAllText(exportLuaPath, "");
                }
                string exportLuaContent = File.ReadAllText(exportLuaPath);
                if (!exportLuaContent.Contains(DCSExportMagic))
                {
                    exportLuaContent += $"\n\n{DCSExportMagic}\n\n";
                    File.WriteAllText(exportLuaPath, exportLuaContent);
                }

                Settings.SetVersionDCSLua(path, DCSLuaVersion.CURRENT);

                return true;
            }
            catch
            {
                Settings.SetVersionDCSLua(path, DCSLuaVersion.NONE);

                // TODO: should uninstall on error?
            }
            return false;
        }

        // TODO: implement
        public static bool UninstallLua(string path)
        {
            Settings.SetVersionDCSLua(path, DCSLuaVersion.NONE);

            return false;
        }

        // return true if jafdtc appears to be installed at the given path, false otherwise. the installed check verifies
        // that the scripts folder exists, scripts/JAFDTC folder exists, scripts/export.lua file exists, and
        // scripts/export.lua contains the jafdtc magic at the given path.
        //
        // NOTE: this does not indicate if the installation is valid or complete.
        //
        private static bool IsJAFDTCInstalled(string path)
        {
            bool isInstalled;
            string scriptsFolder = Path.Combine(path, "Scripts");
            string jafdtcFolder = Path.Combine(scriptsFolder, "JAFDTC");
            string exportLuaPath = Path.Combine(scriptsFolder, "Export.lua");
            if (!Directory.Exists(scriptsFolder) || !Directory.Exists(jafdtcFolder) || !File.Exists(exportLuaPath))
            {
                isInstalled = false;
            }
            else
            {
                string exportLuaContent = File.ReadAllText(exportLuaPath);
                isInstalled = exportLuaContent.Contains(DCSExportMagic);
            }
            if (!isInstalled && Settings.VersionDCSLua.ContainsKey(path))
            {
                Settings.SetVersionDCSLua(path, DCSLuaVersion.NONE);
            }
            return isInstalled;
        }

        // update or create a file. returns true if the destination file was updated (i.e., it existed but differed
        // from the source), false if the destination file was created.
        //
        private static bool UpdateOrCreateFile(string srcPath, string dstPath)
        {
            if (!File.Exists(dstPath))
            {
                File.Copy(srcPath, dstPath);
            }
            else if ((File.ReadAllText(srcPath) != File.ReadAllText(dstPath)))
            {
                File.Copy(srcPath, dstPath, true);
                return true;
            }
            return false;
        }

        // copy lua files from the DCS directory in the application package to the appropriate dcs installation in
        // saved games. returns true if existing files were updated, false if the files were new copies.
        //
        // NOTE: this installation will not remove files that may be no longer relevant.
        //
        private static bool CopyLuaFilesToDCS(string folderToCopy, string scriptsFolder)
        {
            bool isUpdate = false;
            string srcFolder = Path.Combine(FileManager.AppDCSDataDirPath(), folderToCopy);
            string dstFolder = Path.Combine(scriptsFolder, folderToCopy);

            if (!Directory.Exists(dstFolder))
            {
                Directory.CreateDirectory(dstFolder);
            }
            foreach (string srcFile in Directory.GetFiles(srcFolder))
            {
                string dstFile = Path.Combine(dstFolder, Path.GetFileName(srcFile));
                isUpdate = UpdateOrCreateFile(srcFile, dstFile) || isUpdate;
            }
            return isUpdate;
        }
    }
}