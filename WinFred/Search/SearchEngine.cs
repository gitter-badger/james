﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using James.Properties;

namespace James.Search
{
    public class SearchEngine
    {
        private static SearchEngine _searchEngine;
        private static readonly object SingeltonLock = new object();
        private readonly SearchEngineWrapper.SearchEngineWrapper _searchEngineWrapper;
        private readonly Timer _timer;

        private SearchEngine()
        {
            _searchEngineWrapper =
                new SearchEngineWrapper.SearchEngineWrapper(Config.Instance.ConfigFolderLocation + "\\files.txt");
            _timer = new Timer(1000*60*5)
            {
                AutoReset = true,
                Enabled = true
            };
            _timer.Elapsed += Timer_Elapsed;
        }

        public static SearchEngine Instance
        {
            get
            {
                lock (SingeltonLock)
                {
                    return _searchEngine ?? (_searchEngine = new SearchEngine());
                }
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
#if DEBUG
            Console.WriteLine(Resources.SearchEngine_SearchEngineBackup_Notification);
#endif
            _searchEngineWrapper.Save();
        }

        public delegate void ChangedBuildingIndexProgressEventHandler(object sender, ProgressChangedEventArgs e);
        public event ChangedBuildingIndexProgressEventHandler ChangedBuildingIndexProgress;

        public void BuildIndex()
        {
            var data = GetFilesToBeIndexed();
            WriteFilesToIndex(data);
        }

        private static List<SearchResult> GetFilesToBeIndexed()
        {
            var data = new List<SearchResult>();
            Parallel.ForEach(Config.Instance.Paths.Where(path => path.IsEnabled),
                currentPath => { data.AddRange(currentPath.GetItemsToBeIndexed()); });
            return data;
        }

        public void WriteFilesToIndex(IReadOnlyList<SearchResult> data)
        {
            var lastProgress = -1;
            for (var i = 0; i < data.Count; i++)
            {
                ChangeProgress(i, data.Count, ref lastProgress);
                AddFile(data[i]);
            }
            ChangedBuildingIndexProgress?.Invoke(this, new ProgressChangedEventArgs(100, null));
            _searchEngineWrapper.Save();
        }

        private void ChangeProgress(int position, int total, ref int lastProgress)
        {
            if (lastProgress != CalcProgress(position, total))
            {
                lastProgress = CalcProgress(position, total);
                ChangedBuildingIndexProgress?.Invoke(this, new ProgressChangedEventArgs(lastProgress, null));
            }
        }

        private static int CalcProgress(int position, int total) => (int) (((double) position)/total*100);

        public void AddFile(SearchResult file) => _searchEngineWrapper.Insert(file.Path, file.Priority);

        public void RenameFile(string oldPath, string newPath) => _searchEngineWrapper.Rename(oldPath, newPath);

        public void DeletePath(string path) => _searchEngineWrapper.Remove(path);

        public void DeletePathRecursive(string path) => _searchEngineWrapper.RemoveRecursive(path);

        public void IncrementPriority(SearchResult result) => IncrementPriority(result.Path);

        public void IncrementPriority(string path, int priority = 5) => _searchEngineWrapper.AddPriority(path, priority);

        public List<SearchResult> Query(string search)
        {
            if (search.Length < Config.Instance.StartSearchMinTextLength)
            {
                return new List<SearchResult>();
            }
#if DEBUG
            var tmp = DateTime.Now;
            _searchEngineWrapper.Find(search);
            Console.WriteLine((DateTime.Now - tmp).TotalMilliseconds);
#else
            _searchEngineWrapper.Find(search);
#endif
            return
                _searchEngineWrapper.searchResults.Select(item => new SearchResult {Path = item.path, Priority = item.priority})
                    .ToList();
        }
    }
}