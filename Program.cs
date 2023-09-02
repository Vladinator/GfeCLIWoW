using System.Diagnostics;

namespace gfecliwow
{
    class Program : IDisposable
    {
        private static readonly string GFECLI_NAME = "GfeCLI.exe";
        private static readonly string GFECLI_ROOT = "C:\\Users\\Vlad\\Source\\repos\\GfeCLI\\build\\x64\\Release";
        private static readonly string GFECLI_PATH = Path.Combine(GFECLI_ROOT, GFECLI_NAME);
        private static readonly int GFECLI_HIGHLIGHT_MIN_DURATION = 10000;
        private static readonly int CFECLI_HIGHLIGHT_PADDING = 30000;

        private static void RunGfeCLI(int duration)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(duration);
            string timeText = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = GFECLI_PATH,
                Arguments = $"--process wow.exe --highlight {timeText}",
                WorkingDirectory = GFECLI_ROOT,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            Process process = new Process
            {
                StartInfo = psi
            };
            process.Start();
            string error = process.StandardError.ReadToEnd();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0 || output.Contains("unable to save highlight"))
            {
                Console.WriteLine($"Error! Unable to save highlight: {timeText}");
            }
            else
            {
                Console.WriteLine($"Done! Saved highlight: {timeText}");
            }
        }

        public static void Main()
        {
            using var program = new Program("D:\\Games\\World of Warcraft\\_retail_\\Logs\\WoWCombatLog.txt");
            Console.WriteLine("Monitoring for encounter end events. Press Enter to exit.");
            Console.ReadLine();
        }

        private readonly LogReader reader;
        private readonly LogWatcher watcher;

        public Program(string filePath)
        {
            EventHandler.OnEvent += EventHandlerOnEvent;
            reader = new LogReader(filePath);
#if !DEBUG
            reader.ProcessChanges();
#endif
            reader.LogChanged += ReaderLogChanged;
            watcher = new LogWatcher(filePath);
            watcher.LogChanged += WatcherLogChanged;
            watcher.Start();
#if DEBUG
            watcher.Update();
#endif
        }

        public void Dispose()
        {
            watcher.Stop();
            EventHandler.OnEvent -= EventHandlerOnEvent;
        }

        private void EventHandlerOnEvent(object? sender, EventHandlerArgs e)
        {
            if (e.Name != "ENCOUNTER_END")
            {
                return;
            }
            if (!e.Data.TryGetValue("fightTime", out var fightTime))
            {
                return;
            }
            if (fightTime == null || !int.TryParse(fightTime.ToString(), out var ms))
            {
                return;
            }
            if (ms < GFECLI_HIGHLIGHT_MIN_DURATION)
            {
                return;
            }
            Task.Run(() =>
            {
                Console.WriteLine($"Encounter ended after {ms / 1000:0} seconds, clipping highlight in {CFECLI_HIGHLIGHT_PADDING / 1000:0} seconds...");
                Thread.Sleep(CFECLI_HIGHLIGHT_PADDING);
                RunGfeCLI(ms + CFECLI_HIGHLIGHT_PADDING * 2);
            });
        }

        private void ReaderLogChanged(object? sender, LogReaderEventArgs e)
        {
            EventHandler.ProcessEvent(e);
        }

        private void WatcherLogChanged(object? sender, LogWatcherEventArgs e)
        {
            if (reader.FilePath == e.FilePath)
            {
                reader.ProcessChanges();
            }
        }
    }
}
