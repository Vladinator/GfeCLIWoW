using System.Diagnostics;

namespace GfeCLIWoW
{
    class Program : IDisposable
    {
        private static readonly Env env = new();

        private static string DurationToText(double duration)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(duration);
            return string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        }

        private static void RunGfeCLI(double duration, double offset)
        {
            string timeText = DurationToText(duration);
            string offsetText = DurationToText(offset);
            ProcessStartInfo psi = new()
            {
                FileName = env.GfeCLI,
                Arguments = $"--process {env.Process} --highlight {timeText} --offset {offsetText}",
                WorkingDirectory = Path.GetDirectoryName(env.GfeCLI),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            Process process = new()
            {
                StartInfo = psi
            };
            try
            {
                process.Start();
                string error = process.StandardError.ReadToEnd();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                string clean = output.Trim();
                string last = clean.Split("\n").Last();
                Console.WriteLine($"[{process.ExitCode}] {last ?? clean}");
            }
            catch
            {
            }
        }

        private static readonly double MAX_HIGHLIGHT_DURATION = 1200000;
        private static bool IsRunningAction = false;
        private static readonly object lockObject = new();

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
            var action = () =>
            {
                var delta = DateTime.Now - e.Timestamp;
                var offset = delta.TotalMilliseconds;
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
                var totalDuration = duration + env.DurationPadding;
                var totalDurationAndOffset = totalDuration + offset;
                if (totalDurationAndOffset > MAX_HIGHLIGHT_DURATION)
                {
                    Console.WriteLine($"Unable to save highlight that happened {DurationToText(totalDurationAndOffset)} ago because recording limit is at {DurationToText(MAX_HIGHLIGHT_DURATION)}.");
                    return;
                }
                if (totalDuration > MAX_HIGHLIGHT_DURATION)
                {
                    Console.WriteLine($"Unable to save highlight of {DurationToText(totalDuration)} length because recording limit is at {DurationToText(MAX_HIGHLIGHT_DURATION)}.");
                    return;
                }
                Task.Run(() =>
                {
                    Console.WriteLine($"Encounter {encounterInfo.ID} \"{encounterInfo.Name}\" on {encounterInfo.Difficulty} ended at {e.Timestamp:HH:mm:ss} after {encounterInfo.FightTime / 1000:0} seconds - clipping {(encounterInfo.Success ? "victory" : "wipe")}{(padding.TotalMilliseconds > 0 ? $" in {padding.TotalSeconds}" : "")}...");
                    if (padding.TotalMilliseconds > 0)
                    {
                        Thread.Sleep(padding);
                    }
                    RunGfeCLI(totalDuration, offset);
                }).Wait();
            };
            var queuedAction = () =>
            {
                lock (lockObject)
                {
                    while (IsRunningAction)
                    {
                        Thread.Sleep(100);
                    }
                    IsRunningAction = true;
                }
                try
                {
                    action();
                }
                catch
                {
                }
                IsRunningAction = false;
            };
            _ = Task.Run(queuedAction);
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
