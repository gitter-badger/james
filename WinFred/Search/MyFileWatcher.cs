﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace James.Search
{
    internal class MyFileWatcher
    {
        private const int MaxMoveDelay = 50;
        private static readonly object SingeltonLock = new object();
        private static readonly object DeleteEventsLock = new object();

        private static MyFileWatcher _instance;
        private static readonly Queue<DeleteEvent> DeleteEvents = new Queue<DeleteEvent>();
        private readonly List<FileSystemWatcher> _fileSystemWatchers = new List<FileSystemWatcher>();
        private readonly List<Path> _paths = new List<Path>();

        private MyFileWatcher()
        {
            var paths = Config.Instance.Paths.ToList();
            paths.Where(path => path.IsEnabled).ToList().ForEach(AddPath);
        }

        public static MyFileWatcher Instance
        {
            get
            {
                lock (SingeltonLock)
                {
                    return _instance ?? (_instance = new MyFileWatcher());
                }
            }
        }

        private void File_Created(object sender, FileSystemEventArgs e)
        {
            var newName = e.Name.Split('\\').Last();
            var watcher = sender as FileSystemWatcher;
            var currentPath = _paths.First(path => path.Location == watcher?.Path);
            var oldPath = GetOldPathIfExists(newName);
            if (oldPath != null)
            {
                Console.WriteLine($"moved {e.FullPath} | old: {oldPath}");
                SearchEngine.Instance.RenameFile(oldPath, e.FullPath);
            }
            else
            {
                Console.WriteLine($"created {e.FullPath}");
                var priority = currentPath.GetPathPriority(e.FullPath);
                if (priority >= 0)
                {
                    SearchEngine.Instance.AddFile(new SearchResult {Path = e.FullPath, Priority = priority});
                }
            }
            if (Directory.Exists(e.FullPath))
            {
                SearchEngine.Instance.WriteFilesToIndex(
                    currentPath.GetItemsToBeIndexed(e.FullPath.Replace(currentPath.Location, "")).ToList());
            }
        }

        private static string GetOldPathIfExists(string newName)
        {
            string oldPath = null;
            lock (DeleteEventsLock)
            {
                if (DeleteEvents.Count > 0 && DeleteEvents.Peek().Name == newName)
                {
                    oldPath = DeleteEvents.Dequeue().Path;
                }
            }
            return oldPath;
        }

        private static void File_Deleted(object sender, FileSystemEventArgs e)
        {
            lock (DeleteEventsLock)
            {
                DeleteEvents.Enqueue(new DeleteEvent
                {
                    Date = DateTime.Now,
                    Path = e.FullPath,
                    Name = e.FullPath.Split('\\').Last()
                });
            }
            Task.Run(() =>
            {
                Thread.Sleep(MaxMoveDelay);
                lock (DeleteEventsLock)
                {
                    DeleteFileLazy();
                }
            });
            Console.WriteLine($"marking for deletion: {e.FullPath}");
            SearchEngine.Instance.DeletePath(e.FullPath);
        }

        private static void DeleteFileLazy()
        {
            var now = DateTime.Now;
            while (DeleteEvents.Count > 0 && (now - DeleteEvents.Peek().Date).TotalMilliseconds > MaxMoveDelay)
            {
                var path = DeleteEvents.Dequeue().Path;
                Console.WriteLine($"lazy deleted {path}");
                SearchEngine.Instance.DeletePathRecursive(path);
            }
        }

        private void File_Renamed(object sender, RenamedEventArgs e)
        {
            Console.WriteLine($"renamed {e.OldFullPath} to {e.FullPath}");
            var watcher = sender as FileSystemWatcher;
            var currentPath = _paths.First(path => path.Location == watcher?.Path);
            var priority = currentPath.GetPathPriority(e.FullPath);
            if (priority >= 0)
            {
                SearchEngine.Instance.RenameFile(e.OldFullPath, e.FullPath);
            }
        }

        /// <summary>
        ///     Destroys the filewatcher listening to that path as well removes it from the path list.
        /// </summary>
        /// <param name="path"></param>
        public void RemovePath(Path path)
        {
            _paths.Remove(path);
            var watcher = _fileSystemWatchers.First(item => item.Path == path.Location);
            watcher.Dispose();
            _fileSystemWatchers.Remove(watcher);
        }

        /// <summary>
        ///     Adds the given path to the list as well as created a new filewatcher for this path.
        /// </summary>
        /// <param name="path"></param>
        public void AddPath(Path path)
        {
            _paths.Add(path);
            _fileSystemWatchers.Add(CreateFileSystemWatcher(path));
        }

        /// <summary>
        ///     Creates a FileSystemWatcher for the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private FileSystemWatcher CreateFileSystemWatcher(Path path)
        {
            var watcher = new FileSystemWatcher(path.Location)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            watcher.Created += File_Created;
            watcher.Deleted += File_Deleted;
            watcher.Renamed += File_Renamed;
            return watcher;
        }
    }
}