using System.Diagnostics;

namespace GfeCLIWoW
{
    class Program : IDisposable
    {
        private static readonly Env env = new();

        private static string DurationToText(TimeSpan timeSpan)
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        }

        private static void RunGfeCLI(TimeSpan duration, TimeSpan offset)
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
                Console.WriteLine($"[{process.ExitCode}] {clean}");
            }
            catch
            {
            }
        }

        private static readonly TimeSpan MAX_HIGHLIGHT_DURATION = TimeSpan.FromMinutes(20);
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
            reader.Skip(true);
            reader.ProcessChanges();
            reader.Skip(false);
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
#if DEBUG
                Console.WriteLine($"[Debug] Encounter info is empty after parsing: {e.Data}");
#endif
                return;
            }
            if (encounterInfo.FightTime.TotalMilliseconds < env.MinDuration)
            {
#if DEBUG
                Console.WriteLine($"[Debug] Encounter skipped because fight was too short: {encounterInfo.FightTime.TotalMilliseconds:0} < {env.MinDuration:0}");
#endif
                return;
            }
            var encounterText = $"Encounter {encounterInfo.ID} \"{encounterInfo.Name}\" on {encounterInfo.Difficulty} ended at {e.Timestamp:HH:mm:ss} after {encounterInfo.FightTime.TotalSeconds:0} seconds";
#if DEBUG
            Console.WriteLine($"[Debug] {encounterText} - {(encounterInfo.Success ? "victory" : "wipe")}");
#endif
            var action = () =>
            {
                var durationPadding = TimeSpan.FromMilliseconds(env.DurationPadding);
                var offset = DateTime.Now - e.Timestamp - durationPadding;
                if (offset > MAX_HIGHLIGHT_DURATION)
                {
                    Console.WriteLine($"Unable to save highlight that happened {DurationToText(offset)} ago because recording limit is at {DurationToText(MAX_HIGHLIGHT_DURATION)}.");
                    return;
                }
                var duration = encounterInfo.FightTime + durationPadding * 2;
                if (duration > MAX_HIGHLIGHT_DURATION)
                {
                    Console.WriteLine($"Unable to save highlight of {DurationToText(duration)} length because recording limit is at {DurationToText(MAX_HIGHLIGHT_DURATION)}.");
                    return;
                }
                Console.WriteLine($"{encounterText} - clipping {(encounterInfo.Success ? "victory" : "wipe")}...");
                RunGfeCLI(duration, offset);
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
