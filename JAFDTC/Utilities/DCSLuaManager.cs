// ********************************************************************************************************************
//
// DCSLuaManager.cs : manages install/remove/update of lua files in dcs install to support jafdtc
//
// Copyright(C) 2021-2023 the-paid-actor & dcs-dtc contributors
// Copyright(C) 2023-2024 ilominar/raven
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
        // ------------------------------------------------------------------------------------------------------------
        //
        // windoze interfaces & data structs
        //
        // ------------------------------------------------------------------------------------------------------------

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
                                                  uint dwFlags,
                                                  IntPtr hToken = default);

        // ------------------------------------------------------------------------------------------------------------
        //
        // types & statics
        //
        // ------------------------------------------------------------------------------------------------------------

        public enum DCSLuaVersion
        {
            NONE = -1,
            CURRENT = 1
        };

        static readonly Guid _savedGamesFolderGuid = new("4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4");
#if USE_DCS_TEST_FOLDER
        static readonly List<string> _installFolders = new() { "TEST_DCS", "TEST_DCS.openbeta" };
#else
        static readonly List<string> _installFolders = new() { "DCS", "DCS.openbeta" };
#endif
        static readonly string _dcsExportMagic = "local JAFDTClfs=require('lfs'); dofile(JAFDTClfs.writedir()..'Scripts/JAFDTC/JAFDTC.lua')";

        static readonly List<string> _deprecatedFiles = new()
        {
            "Hooks\\JAFDTCCfgNameHook.lua",
            "Hooks\\JAFDTCHook.lua",
            "JAFDTC\\ConfigName.dlg",
            "JAFDTC\\WaypointCapture.dlg"
        };

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns true if (according to settings) there is at least one valid lua install, false otherwise.
        /// </summary>
        public static bool IsLuaInstalled()
        {
            foreach (KeyValuePair<string, DCSLuaVersion> kvp in Settings.VersionDCSLua)
				if ((kvp.Value != DCSLuaVersion.NONE) && IsJAFDTCInstalled(kvp.Key))
					return true;
			return false;
        }

        /// <summary>
        /// returns dictionary keyed by install path with boolean indicating if lua is installed according to
        /// IsJAFDTCInstalled
        /// </summary>
        public static Dictionary<string, bool> LuaInstallStatus()
        {
            Dictionary<string, bool> result = new Dictionary<string, bool>();
            foreach (KeyValuePair<string, DCSLuaVersion> kvp in Settings.VersionDCSLua)
                result[kvp.Key] = ((kvp.Value != DCSLuaVersion.NONE) && IsJAFDTCInstalled(kvp.Key));
            return result;
        }

        /// <summary>
        /// check the status of any lua installations in the _installFolders to flag common errors and determine which
        /// folders need updates or fresh installs. return a DCSLuaManagerCheckResult with information on the situation
        /// that can be used to determine how to proceed.
        /// </summary>
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

        /// <summary>
        /// attempt to install or update lua at the given dcs path. this will copy/update files in both the scripts
        /// and hooks locations, as well as update Export.lua. returns true on success, false on failure. didUpdate
        /// is set to true if the lua install was updated (versus newly installed).
        /// </summary>
        public static bool InstallOrUpdateLua(string path, out bool didUpdate)
        {
            didUpdate = false;
            try
            {
                string scriptsFolder = System.IO.Path.Combine(path, "Scripts");
                if (!Directory.Exists(scriptsFolder))
                {
                    FileManager.Log($"DCSLuaManager: Creates directory: {scriptsFolder}");
                    Directory.CreateDirectory(scriptsFolder);
                }

                RemoveDeprecatedFiles(scriptsFolder, _deprecatedFiles);

                if (CopyLuaFilesToDCS("JAFDTC", scriptsFolder))
                {
                    didUpdate = true;
                }
                if (CopyLuaFilesToDCS("Hooks", scriptsFolder))
                {
                    didUpdate = true;
                }

                string exportLuaPath = Path.Combine(scriptsFolder, "Export.lua");
                if (!File.Exists(exportLuaPath))
                {
                    FileManager.Log($"DCSLuaManager: Creates Export.lua");
                    File.WriteAllText(exportLuaPath, "");
                }
                string exportLuaContent = File.ReadAllText(exportLuaPath);
                if (!exportLuaContent.Contains(_dcsExportMagic))
                {
                    FileManager.Log($"DCSLuaManager: Updates Export.lua");
                    exportLuaContent += $"\n\n{_dcsExportMagic}\n\n";
                    File.WriteAllText(exportLuaPath, exportLuaContent);
                }

                Settings.SetVersionDCSLua(path, DCSLuaVersion.CURRENT);

                return true;
            }
            catch (Exception ex)
            {
                Settings.SetVersionDCSLua(path, DCSLuaVersion.NONE);

                // TODO: should uninstall on error?

                FileManager.Log($"DCSLuaManager:InstallOrUpdateLua fails, {ex}");
            }
            return false;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public static bool UninstallLua(string path)
        {
            Settings.SetVersionDCSLua(path, DCSLuaVersion.NONE);
            // TODO: implement
            return false;
        }

        /// <summary>
        /// return true if jafdtc appears to be installed at the given path, false otherwise. the installed check verifies
        /// that the scripts folder exists, scripts/JAFDTC folder exists, scripts/export.lua file exists, and
        /// scripts/export.lua contains the jafdtc magic at the given path.
        ///
        /// NOTE: this does not indicate if the installation is valid or complete.
        /// </summary>
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
                isInstalled = exportLuaContent.Contains(_dcsExportMagic);
            }
            if (!isInstalled && Settings.VersionDCSLua.ContainsKey(path))
            {
                Settings.SetVersionDCSLua(path, DCSLuaVersion.NONE);
            }
            return isInstalled;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private static void RemoveDeprecatedFiles(string scriptsFolder, List<string> files)
        {
            foreach (string file in files)
            {
                string path = Path.Combine(scriptsFolder, file);
                if (File.Exists(path))
                {
                    FileManager.Log($"DCSLuaManager: Removes deprecated file: {path}");
                    File.Delete(path);
                }
            }
        }

        /// <summary>
        /// update or create a file. returns true if the destination file was updated (i.e., it existed but differed
        /// from the source), false if the destination file was created.
        /// </summary>
        private static bool UpdateOrCreateFile(string srcPath, string dstPath)
        {
            if (!File.Exists(dstPath))
            {
                FileManager.Log($"DCSLuaManager: Creates file: {dstPath}");
                File.Copy(srcPath, dstPath);
            }
            else if ((File.ReadAllText(srcPath) != File.ReadAllText(dstPath)))
            {
                FileManager.Log($"DCSLuaManager: Updates file: {dstPath}");
                File.Copy(srcPath, dstPath, true);
                return true;
            }
            return false;
        }

        /// <summary>
        /// copy lua files from the DCS directory in the application package to the appropriate dcs installation in
        /// saved games. returns true if existing files were updated, false if the files were new copies.
        ///
        /// NOTE: this installation will not remove files that may be no longer relevant.
        /// </summary>
        private static bool CopyLuaFilesToDCS(string folderToCopy, string scriptsFolder)
        {
            bool isUpdate = false;
            string srcFolder = Path.Combine(FileManager.AppDCSDataDirPath(), folderToCopy);
            string dstFolder = Path.Combine(scriptsFolder, folderToCopy);

            if (!Directory.Exists(dstFolder))
            {
                FileManager.Log($"DCSLuaManager: Creates directory: {dstFolder}");
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