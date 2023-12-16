// ********************************************************************************************************************
//
// FileManager.cs : file management abstraction layer
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
        /// <exception cref="Exception"></exception>
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
        /// <returns></returns>
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
            catch
            {
                json = null;
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
            catch
            {
                settings = new SettingsData();
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
                AirframeTypes.F15E => Path.Combine(_settingsDirPath, "Configs", "F15E"),
                AirframeTypes.F16C => Path.Combine(_settingsDirPath, "Configs", "F16C"),
                AirframeTypes.FA18C => Path.Combine(_settingsDirPath, "Configs", "FA18C"),
                AirframeTypes.M2000C => Path.Combine(_settingsDirPath, "Configs", "M2000C"),
                AirframeTypes.F14AB => Path.Combine(_settingsDirPath, "Configs", "F14AB"),
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
        // user database
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        public static List<T> LoadUserDbase<T>(string name)
        {
            string path = Path.Combine(_settingsDirPath, "Dbase");
            Directory.CreateDirectory(path);
            path = Path.Combine(path, name);
            List<T> dbase = new();
            try
            {
                string json = ReadFile(path);
                dbase = (List<T>)JsonSerializer.Deserialize<List<T>>(json);
            }
            catch (Exception ex)
            {
                FileManager.Log($"FileManager:LoadUserDbase exception {ex}");
            }
            return dbase;
        }

        /// <summary>
        /// TODO: document
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

        // ------------------------------------------------------------------------------------------------------------
        //
        // poi database
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        public static Dictionary<string, List<PointOfInterest>> LoadPointsOfInterest()
        {
            try
            {
                string path = Path.Combine(_appDirPath, "Data", "db-poi-airbases.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<Dictionary<string, List<PointOfInterest>>>(json);
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"FileManager:LoadPointsOfInterest exception {ex}");
            }
            return new Dictionary<string, List<PointOfInterest>>();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // emitters database
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// </summary>
        public static Emitter[] LoadEmitters()
        {
            try
            {
                string path = Path.Combine(_appDirPath, "Data", "db-emitters.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<Emitter[]>(json);
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"FileManager:LoadEmitters exception {ex}");
            }
            return Array.Empty<Emitter>();
        }
    }
}