namespace GfeCLIWoW
{
    class LogWatcherEventArgs : EventArgs
    {
        public string FilePath { get; }

        public LogWatcherEventArgs(string filePath)
        {
            FilePath = filePath;
        }
    }

    class LogWatcher
    {
        private readonly string fileDirPath;
        private readonly string fileName;
        private FileSystemWatcher? watcher;

        public event EventHandler<LogWatcherEventArgs>? LogChanged;

        private void OnLogChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created)
            {
                LogChanged?.Invoke(this, new LogWatcherEventArgs(e.FullPath));
            }
        }

        public LogWatcher(string filePath)
        {
            fileDirPath = Path.GetDirectoryName(filePath) ?? "";
            fileName = Path.GetFileName(filePath);
        }

        private FileSystemWatcher GetWatcher()
        {
            if (watcher == null)
            {
                watcher = new FileSystemWatcher(fileDirPath, fileName)
                {
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };
            }
            return watcher;
        }

        public void Start()
        {
            var watcher = GetWatcher();
            watcher.Changed += OnLogChanged;
        }

        public void Stop()
        {
            var watcher = GetWatcher();
            watcher.Changed -= OnLogChanged;
        }

        public void Update()
        {
            OnLogChanged(this, new FileSystemEventArgs(WatcherChangeTypes.Changed, fileDirPath, fileName));
        }
    }
}
