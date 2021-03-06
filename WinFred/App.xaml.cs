﻿using System;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using Exceptionless;
using James.HelperClasses;
using James.Search;
using James.Shortcut;
using James.Workflows;
using Microsoft.Win32.TaskScheduler;
using Squirrel;
using Task = System.Threading.Tasks.Task;

namespace James
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static bool _showTheWelcomeWizard;

        private static Mutex _mutex;

        private static void SetStyleAccents()
        {
            var accentColor = "pack://application:,,,/MahApps.Metro;component/Styles/Accents/" +
                              Config.Instance.WindowAccentColor + ".xaml";
            (((App) Current).Resources).MergedDictionaries[5].Source = new Uri(accentColor);
            var baseColor = Config.Instance.IsBaseLight ? "BaseLight" : "BaseDark";
            baseColor = "pack://application:,,,/MahApps.Metro;component/Styles/Accents/" + baseColor + ".xaml";
            (((App) Current).Resources).MergedDictionaries[4].Source = new Uri(baseColor);
        }
        
        /// <summary>
        /// Entry point of the application, here we check if it's the first running instance of James
        /// </summary>
        /// <param name="e"></param>
        [STAThread]
        protected override void OnStartup(StartupEventArgs e)
        {
            ExceptionlessClient.Default.Register();
            bool createdNew;
            _mutex = new Mutex(true, "James", out createdNew);
            if (createdNew)
            {
                StartProgram();
                base.OnStartup(e);
            }
            else if (e.Args.Length != 0)
            {
                Config.Instance.FirstInstance = false;
                AlternativeRun(e);
            }
        }

        /// <summary>
        /// If an instance of James is already running, its opened to import a workflow or triggers the Api
        /// </summary>
        /// <param name="e"></param>
        private static void AlternativeRun(StartupEventArgs e)
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream("james"))
            {
                client.Connect(100);
                using (StreamWriter writer = new StreamWriter(client))
                {
                    
                    if (e.Args.Length == 1 && File.Exists(e.Args[0]) && e.Args[0].EndsWith(".james"))
                    {
                        string workflowFolder = Config.WorkflowFolderLocation + "\\" + PathHelper.GetFilename(e.Args[0]).Replace(".james", "");
                        if (!Directory.Exists(workflowFolder))
                        {
                            ZipFile.ExtractToDirectory(e.Args[0], workflowFolder);
                            if (client.IsConnected)
                            {
                                writer.WriteLine("workflow/" + PathHelper.GetFilename(e.Args[0]).Replace(".james", ""));
                            }
                        }
                        else
                        {
                            MessageBox.Show($"An Workflow with the name '{PathHelper.GetFilename(e.Args[0]).Replace(".james", "")}' is already importet!", "Workflow already imported!", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else if (client.IsConnected)
                    {
                        writer.WriteLine(string.Join(" ", e.Args).Substring(6));
                    }
                    writer.Flush();
                }
            }
            Environment.Exit(0);
        }

        private static void StartProgram()
        {
            Config.Instance.WindowChangedAccentColor += App_WindowChangedAccentColor;
            SetStyleAccents();
            SquirrelAwareApp.HandleEvents(onFirstRun: OnFirstRun, onAppUninstall: OnAppUninstall, onAppUpdate: OnAppUpdate);
            InitializeSingeltons();
            if (_showTheWelcomeWizard)
            {
                new Windows.WelcomeWindow().Show();
            }
            else
            {
                James.Windows.MainWindow.GetInstance().Show();
            }
        }

        private static void OnAppUpdate(Version version)
        {
            MessageBox.Show($"James got an update to: {version}");
            RegistryHelper.AssociateFileExtension();
            RegistryHelper.RegisterCustomProtocol();
        }

        private static void InitializeSingeltons()
        {
            var searchEngine = SearchEngine.Instance;
            var workflowManager = WorkflowManager.Instance;
            var watcher = MyFileWatcher.Instance;
            var shortcutManager = ShortcutManager.Instance;
            var apiListener = ApiListener.Instance;
        }

        private static void OnAppUninstall(Version version)
        {
            using (var ts = new TaskService())
            {
                ts.RootFolder.DeleteTask("James");
            }
            using (var updateManager = new UpdateManager(Config.Instance.ReleaseUrl))
            {
                updateManager.RemoveShortcutsForExecutable("James.exe", ShortcutLocation.Desktop);
                updateManager.RemoveShortcutsForExecutable("James.exe", ShortcutLocation.StartMenu);
            }
        }

        private static void AddAutoUpdateTask()
        {
            using (var ts = new TaskService())
            {
                var td = ts.NewTask();
                td.RegistrationInfo.Description = "Calls the updater for James once a day";
                td.Triggers.Add(new DailyTrigger {DaysInterval = 1});
                td.Actions.Add(new ExecAction(Config.ConfigFolderLocation + "\\Update.exe",
                    "--update " + Config.Instance.ReleaseUrl, null));
                ts.RootFolder.RegisterTaskDefinition(@"James", td);
            }
        }

        private static void OnFirstRun()
        {
            _showTheWelcomeWizard = true;
            using (var updateManager = new UpdateManager(Config.Instance.ReleaseUrl))
            {
                updateManager.CreateShortcutsForExecutable("James.exe", ShortcutLocation.Desktop, true);
                updateManager.CreateShortcutsForExecutable("James.exe", ShortcutLocation.StartMenu, true);
            }
            AddAutoUpdateTask();
        }

        private static void App_WindowChangedAccentColor(object sender, EventArgs e) => SetStyleAccents();
    }
}