﻿// ********************************************************************************************************************
//
// FileManager.cs : file management abstraction layer
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

#define JAFDTC_LOG

using JAFDTC.Models;
using JAFDTC.Models.DCS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace JAFDTC.Utilities
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class FileManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly static string _appDirPath = AppContext.BaseDirectory;

        private static string _settingsDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JAFDTC");

        private static string _settingsPath = null;

        private static string _logPath = null;

        private static TextWriterTraceListener _logTraceListener = null;

        // ------------------------------------------------------------------------------------------------------------
        //
        // setup
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// pre-flight the file manager by checking paths and creating the settings folder if necessary. throws an
        /// exception on issues.
        /// </summary>
        public static void Preflight()
        {
            Debug.Assert(Directory.Exists(_appDirPath));

            try
            {
                Directory.CreateDirectory(_settingsDirPath);
                _settingsPath = Path.Combine(_settingsDirPath, "jafdtc-settings.json");
#if JAFDTC_LOG
                _logPath = Path.Combine(_settingsDirPath, "jafdtc-log.txt");
                FileStream traceLog = new(_logPath, FileMode.Create);
                _logTraceListener = new TextWriterTraceListener(traceLog);
#endif
            }
            catch (Exception ex)
            {
                _settingsDirPath = null;
                string msg = $"Unable to create settings folder: {_settingsDirPath}. Make sure the path is correct and that you have appropriate permissions.";
                throw new Exception(msg, ex);
            }
        }

        /// <summary>
        /// return the path to the internal dcs data directory in the application package.
        /// </summary>
        public static string AppDCSDataDirPath()
        {
            return Path.Combine(_appDirPath, "DCS");
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // logging
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// log to the log file in the documents directory. this is a nop unless JAFDTC_LOG is defined
        /// </summary>
        public static void Log(string msg)
        {
            _logTraceListener?.WriteLine(msg);
            _logTraceListener?.Flush();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // core file i/o
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the contents of the text file at the specified path. callers should protect this with a try/catch.
        /// </summary>
        public static string ReadFile(string path)
        {
            return File.ReadAllText(path);
        }

        /// <summary>
        /// return the contents of the text file at the specified path within a zip file at the specified path. callers
        /// should protect this with a try/catch.
        /// </summary>
        public static string ReadFileFromZip(string zipPath, string filePath)
        {
            using ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Read);
            ZipArchiveEntry entry = archive.GetEntry(filePath);
            return new StreamReader(entry?.Open()).ReadToEnd();
        }

        /// <summary>
        /// write the text content to a file at teh specified path. callers should protect this with a try/catch.
        /// </summary>
        public static void WriteFile(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // settings
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the settings object for the settings. if the settings file doesn't exist, create it. reads the json
        /// from the settings file, deserializes it, and returns the resulting object, null on error.
        /// </summary>
        public static SettingsData ReadSettings()
        {
            SettingsData settings = null;
            string json;
            try
            {
                json = ReadFile(_settingsPath);
            }
            catch (Exception ex)
            {
                json = null;
                FileManager.Log($"Settings:ReadSettings settings missing, exception {ex}");
            }
            try
            {
                if (json == null)
                {
                    settings = new SettingsData();
                    WriteSettings(settings);
                    json = ReadFile(_settingsPath);
                }
                if (json != null)
                {
                    settings = JsonSerializer.Deserialize<SettingsData>(json);
                }
            }
            catch (Exception ex)
            {
                settings = new SettingsData();
                FileManager.Log($"Settings:ReadSettings create empty settings, exception {ex}");
            }
            return settings;
        }

        /// <summary>
        /// serialize the settings object to json and write the json to the settings file.
        /// </summary>
        public static void WriteSettings(SettingsData settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, Globals.JSONOptions);
                WriteFile(_settingsPath, json);
            }
            catch (Exception ex)
            {
                FileManager.Log($"Settings:WriteSettings exception {ex}");
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // aircraft configurations
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns the path to the configurations directory for a given airframe in the settings directory
        /// </summary>
        private static string AirframeConfigDirPath(AirframeTypes airframe)
        {
            return airframe switch
            {
                AirframeTypes.A10C => Path.Combine(_settingsDirPath, "Configs", "A10C"),
                AirframeTypes.AH64D => Path.Combine(_settingsDirPath, "Configs", "AH64D"),
                AirframeTypes.AV8B => Path.Combine(_settingsDirPath, "Configs", "AV8B"),
                AirframeTypes.F14AB => Path.Combine(_settingsDirPath, "Configs", "F14AB"),
                AirframeTypes.F15E => Path.Combine(_settingsDirPath, "Configs", "F15E"),
                AirframeTypes.F16C => Path.Combine(_settingsDirPath, "Configs", "F16C"),
                AirframeTypes.FA18C => Path.Combine(_settingsDirPath, "Configs", "FA18C"),
                AirframeTypes.M2000C => Path.Combine(_settingsDirPath, "Configs", "M2000C"),
                _ => Path.Combine(_settingsDirPath, "Configs"),
            };
        }

        /// <summary>
        /// returns the configuration file name for the given airframe. configuration file name is built by removing
        /// invalid path chars from lowercase config name. a 4-digit hex int is appended to this to uniquify names
        /// like "A / B" and "A ? B".
        /// </summary>
        private static string ConfigFileame(AirframeTypes airframe, string name)
        {
            name = name.ToLower();

            string path = AirframeConfigDirPath(airframe);
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            string cleanFilenameBase = new(name.Where(m => !invalidChars.Contains(m)).ToArray<char>());
            string cleanFilename = cleanFilenameBase + ".json";

            int index = 1;
            while (File.Exists(Path.Combine(path, cleanFilename)))
            {
                cleanFilename = string.Format("{0} {1:X4}.json", cleanFilenameBase, index);
                index++;
            }
            return cleanFilename;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public static Dictionary<string, IConfiguration> LoadConfigurationFiles(AirframeTypes airframe)
        {
            string path = AirframeConfigDirPath(airframe);
            Dictionary<string, IConfiguration> dict = new();
            if (Directory.Exists(path))
            {
                var files = Directory.EnumerateFiles(path, "*.json");
                foreach (var file in files)
                {
                    var json = File.ReadAllText(file);
                    IConfiguration config = Configuration.FactoryJSON(airframe, json);
                    dict.Add(file, config);
                }
            }
            return dict;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public static void SaveConfigurationFile(IConfiguration config)
        {
            string path = AirframeConfigDirPath(config.Airframe);
            Directory.CreateDirectory(path);
            config.Filename ??= ConfigFileame(config.Airframe, config.Name);
            File.WriteAllText(Path.Combine(path, config.Filename), config.Serialize());
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public static void DeleteConfigurationFile(IConfiguration config)
        {
            string path = Path.Combine(AirframeConfigDirPath(config.Airframe), config.Filename);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public static void RenameConfigurationFile(IConfiguration config, string oldFilename)
        {
            string path = AirframeConfigDirPath(config.Airframe);
            if (Directory.Exists(path))
            {
                config.Filename = ConfigFileame(config.Airframe, config.Name);
                File.Move(Path.Combine(path, oldFilename), Path.Combine(path, config.Filename));
                SaveConfigurationFile(config);
            }
            else
            {
                config.Filename = null;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // databases
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// load the database at the given path. here, a "database" is a List<T> of objects of type T that is
        /// serialized to a .json file. return an empty list on error.
        /// </summary>
        private static List<T> LoadDbaseCore<T>(string path)
        {
            List<T> dbase = new();
            try
            {
                string json = ReadFile(path);
                dbase = (List<T>)JsonSerializer.Deserialize<List<T>>(json);
            }
            catch (Exception ex)
            {
                FileManager.Log($"FileManager:LoadDbaseCore exception {ex}");
            }
            return dbase;
        }

        /// <summary>
        /// load a system database (a .json serialized List<T>) from the "Data" directory in the app bundle.  system
        /// databases are immutable and cannot be updated. returns an empty list on error.
        /// </summary>
        public static List<T> LoadSystemDbase<T>(string name)
        {
            string path = Path.Combine(_appDirPath, "Data", name);
            return (File.Exists(path)) ? LoadDbaseCore<T>(path) : new List<T>();
        }

        /// <summary>
        /// load the user database (a .json serialized List<T>) with the given name from the database area in the
        /// jafdtc settings directory. user databases are mutable and may be updated. returns an empty list on error.
        /// </summary>
        public static List<T> LoadUserDbase<T>(string name)
        {
            string path = Path.Combine(_settingsDirPath, "Dbase");
            Directory.CreateDirectory(path);
            return LoadDbaseCore<T>(Path.Combine(path, name));
        }

        /// <summary>
        /// save the user database (a .json serialized List<T>) with the given name to the database area in the jafdtc
        /// settings directory (creating the file if necessary). returns true on success, false on failure
        /// </summary>
        public static bool SaveUserDbase<T>(string name, List<T> dbase)
        {
            string path = Path.Combine(_settingsDirPath, "Dbase");
            Directory.CreateDirectory(path);
            path = Path.Combine(path, name);
            try
            {
                string json = JsonSerializer.Serialize<List<T>>(dbase, Configuration.JsonOptions);
                WriteFile(path, json);
                return true;
            }
            catch (Exception ex)
            {
                FileManager.Log($"FileManager:SaveUserDbase exception {ex}");
            }
            return false;
        }

        /// <summary>
        /// delete the user database (a .json serialized List<T>) with the given name from the database area in the
        /// jafdtc settings directory.
        /// </summary>
        public static void DeleteUserDatabase(string name)
        {
            string path = Path.Combine(Path.Combine(_settingsDirPath, "Dbase"), name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // poi database
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return a sanitized filename for a campaign point of interest database.
        /// </summary>
        private static string CampaignPoIFilename(string campaign)
        {
            campaign = campaign.ToLower().Replace(' ', '-');
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            string cleanFilenameBase = new(campaign.Where(m => !invalidChars.Contains(m)).ToArray<char>());
            return $"jafdtc-pois-campaign-{cleanFilenameBase}.json";
        }

        /// <summary>
        /// return the point of interest database that provides coordinates on known points in the world. this database
        /// is the combination of a system dbase that carries fixed dcs points, a user dbase that holds editable
        /// user-specified points, and any number of read-only campaign databases.
        /// </summary>
        public static List<PointOfInterest> LoadPointsOfInterest()
        {
            List<PointOfInterest> dbase = LoadSystemDbase<PointOfInterest>("db-pois-airbases.json");
            foreach (PointOfInterest point in dbase)
            {
                point.SourceFile = "db-pois-airbases.json";
            }

            string path = Path.Combine(_settingsDirPath, "Dbase");
            foreach (string srcFile in Directory.GetFiles(path))
            {
                string fileName = Path.GetFileName(srcFile);
                if (fileName.ToLower().StartsWith("jafdtc-pois-") && fileName.ToLower().EndsWith(".json"))
                {
                    List<PointOfInterest> points = LoadUserDbase<PointOfInterest>(fileName);
                    foreach (PointOfInterest point in points)
                    {
                        point.SourceFile = fileName;
                    }
                    dbase.AddRange(points);
                }
            }
            return dbase;
        }

        /// <summary>
        /// saves points of interest to the user point of interest database. returns true on success, false otherwise.
        /// </summary>
        public static bool SaveUserPointsOfInterest(List<PointOfInterest> userPoIs)
        {
            return SaveUserDbase<PointOfInterest>("jafdtc-pois-user.json", userPoIs);
        }

        /// <summary>
        /// saves points of interest to the campaign point of interest database. returns true on success, false
        /// otherwise.
        /// </summary>
        public static bool SaveCampaignPointsOfInterest(string campaign, List<PointOfInterest> userPoIs)
        {
            return SaveUserDbase<PointOfInterest>(CampaignPoIFilename(campaign), userPoIs);
        }

        /// <summary>
        /// returns true if there is a point of interest database for the specified campaign, false otherwise.
        /// </summary>
        public static bool IsCampaignDatabase(string campaign)
        {
            return File.Exists(Path.Combine(Path.Combine(_settingsDirPath, "Dbase"), CampaignPoIFilename(campaign)));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // emitters database
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the emitter database that provides information on known emitters for harm alic/hts systems.
        /// </summary>
        public static List<Emitter> LoadEmitters()
        {
            return LoadSystemDbase<Emitter>("db-emitters.json");
        }
    }
}