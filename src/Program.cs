using System.Diagnostics;

namespace GfeCLIWoW
{
    class Program : IDisposable
    {
        private static readonly Env env = new();

        private static void RunGfeCLI(double duration)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(duration);
            string timeText = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            ProcessStartInfo psi = new()
            {
                FileName = env.GfeCLI,
                Arguments = $"--process {env.Process} --highlight {timeText}",
                WorkingDirectory = Path.GetDirectoryName(env.GfeCLI),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            Process process = new()
            {
                StartInfo = psi
            };
            process.Start();
            string error = process.StandardError.ReadToEnd();
            string output = process.StandardOutput.ReadToEnd();
            string buffer = $"{error}\n{output}";
            process.WaitForExit();
            if (process.ExitCode != 0 || buffer.Contains("unable to save highlight"))
            {
                Console.WriteLine($"Error! Unable to save highlight: {timeText}");
            }
            else
            {
                Console.WriteLine($"Done! Saved highlight: {timeText}");
            }
        }

        private static readonly ManualResetEventSlim exitEvent = new(false);

        public static void Main()
        {
            if (!env.IsValid())
            {
                Console.WriteLine("You need to create a .env file and it needs to contain the required fields.");
                return;
            }
            Console.WriteLine("Fast forwarding to the end of the current combatlog...");
            using var program = new Program(env.LogFile);
            Console.WriteLine("Monitoring for encounters. Press Ctrl+C to quit.");
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; exitEvent.Set(); };
            exitEvent.Wait();
        }

        private readonly LogReader reader;
        private readonly LogWatcher watcher;

        public Program(string filePath)
        {
            EventHandler.OnEvent += EventHandlerOnEvent;
            reader = new LogReader(filePath);
            reader.LogChanged += ReaderLogChanged;
#if !DEBUG
            reader.SkipEvents = true;
            reader.ProcessChanges();
            reader.SkipEvents = false;
#endif
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
            var encounterInfo = new EncounterInfo(e.Data);
            if (encounterInfo.IsEmpty())
            {
                return;
            }
            if (encounterInfo.FightTime < env.MinDuration)
            {
                return;
            }
            var delta = DateTime.Now - e.Timestamp;
            var padding = TimeSpan.FromMilliseconds(env.DurationPadding) - delta;
            var duration = encounterInfo.FightTime;
            if (padding.TotalMilliseconds > 0)
            {
                duration += padding.TotalMilliseconds;
            }
            else
            {
                duration -= padding.TotalMilliseconds;
                padding = TimeSpan.Zero;
            }
            _ = Task.Run(() =>
            {
                Console.WriteLine($"Encounter {encounterInfo.ID} \"{encounterInfo.Name}\" on {encounterInfo.Difficulty} ended at {e.Timestamp:HH:mm:ss} after {encounterInfo.FightTime / 1000:0} seconds, clipping {(encounterInfo.Success ? "victory" : "wipe")}{(padding.TotalMilliseconds > 0 ? $" in {padding.TotalSeconds}" : "")}...");
                if (padding.TotalMilliseconds > 0)
                {
                    Thread.Sleep(padding);
                }
                RunGfeCLI(duration + env.DurationPadding);
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
