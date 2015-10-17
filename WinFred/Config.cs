﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using James.Annotations;
using James.HelperClasses;
using James.Search;
using Microsoft.Win32;
using Path = James.Search.Path;

namespace James
{
    public delegate void ChangedWindowAccentColorEventHandler(object sender, EventArgs e);

    public class Config
    {
        private Config()
        {
            Paths = new ObservableCollection<Path>();
            DefaultFileExtensions = new List<FileExtension>();
            Workflows = new ObservableCollection<Workflow>();
            ExcludedFolders = new ObservableCollection<string>();
        }

        public static void LoadDefaultFileExtensions()
        {
            var tmp = new List<FileExtension>
            {
                new FileExtension("exe", 100),
                new FileExtension("png", 10),
                new FileExtension("jpg", 10),
                new FileExtension("pdf", 40),
                new FileExtension("doc", 10),
                new FileExtension("c", 11),
                new FileExtension("cpp", 11),
                new FileExtension("html", 15),
                new FileExtension("js", 10),
                new FileExtension("html", 10),
                new FileExtension("msi", 80),
                new FileExtension("zip", 50),
                new FileExtension("csv", 10),
                new FileExtension("cs", 10),
                new FileExtension("cshtml", 10),
                new FileExtension("jar", 20),
                new FileExtension("java", 30),
                new FileExtension("txt", 20),
                new FileExtension("docx", 10),
                new FileExtension("xmcd", 39),
                new FileExtension("mcdx", 40)
            };
            _config.DefaultFileExtensions.AddRange(tmp);
            _config.DefaultFileExtensions.Sort();
        }

        #region singleton

        private static Config _config; //todo test if private is possible
        private static readonly object SingeltonLock = new object();

        public static Config Instance
        {
            get
            { 
                lock (SingeltonLock)
                {
                    if (_config == null)
                    {
                        try
                        {
                            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\James";
                            Directory.CreateDirectory(path);
                            _config = GeneralHelper.Deserialize<Config>(path + "\\config.xml");
                        }
                        catch (Exception)
                        {
                            InitConfig();
                        }
                    }
                    return _config;
                }
            }
        }

        private static void InitConfig()
        {
            _config = new Config();
            _config.Paths.Add(new Path {Location = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)});
            LoadDefaultFileExtensions();
            _config.Persist();
        }

        public void Persist()
        {
            lock (_config)
            {
                DefaultFileExtensions.Sort();
                foreach (var item in Paths)
                {
                    item.FileExtensions.Sort();
                }
                File.WriteAllText(_config.ConfigFolderLocation + "\\config.xml", _config.Serialize());
            }
        }

        public void ResetConfig()
        {
            lock (SingeltonLock)
            {
                InitConfig();
                StartProgramOnStartup = false;
                WindowChangedAccentColor?.Invoke(this, null);
            }
            Persist();
        }

        #endregion

        #region fields

        public ObservableCollection<Path> Paths { get; set; }
        public List<FileExtension> DefaultFileExtensions { get; set; }
        public ObservableCollection<Workflow> Workflows { get; set; }
        public ObservableCollection<string> ExcludedFolders { get; set; }

        public string ConfigFolderLocation { get; set; } =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\James";

        public string ReleaseUrl { get; set; } = @"http://www.moserm.tk/Releases";

        public int DefaultFolderPriority { get; set; } = 80;
        public int MaxSearchResults { get; set; } = 8;

        public int StartSearchMinTextLength { get; set; } = 1;

        public double LargeTypeOpacity { get; set; } = 0.75;

        public event ChangedWindowAccentColorEventHandler WindowChangedAccentColor;

        private string _windowAccentColor = "Lime";
        private bool _isBaseLight = true;
        private bool _startProgramOnStartup;
        public bool DisplayFileIcons { get; set; } = true;

        public bool StartProgramOnStartup
        {
            get { return _startProgramOnStartup; }
            set
            {
                var registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (value)
                {
                    registryKey?.SetValue("James",
                        "\"" + ConfigFolderLocation + "\\Update.exe\" --processStart James.exe");
                }
                else
                {
                    registryKey?.DeleteValue("James", false);
                }
                _startProgramOnStartup = value;
            }
        }

        public string WindowAccentColor
        {
            get { return _windowAccentColor; }
            set
            {
                _windowAccentColor = value;
                WindowChangedAccentColor?.Invoke(this, new EventArgs());
            }
        }

        public bool IsBaseLight
        {
            get { return _isBaseLight; }
            set
            {
                _isBaseLight = value;
                WindowChangedAccentColor?.Invoke(this, new EventArgs());
            }
        }

        #endregion
    }
}