using System.Diagnostics;

namespace gfecliwow
{
    class Program : IDisposable
    {

        // TODO: implement settings handling
        private static readonly string GFECLI_NAME = "GfeCLI.exe";
        private static readonly string GFECLI_ROOT = "C:\\Users\\Vlad\\Source\\repos\\GfeCLI\\build\\x64\\Release";
        private static readonly string GFECLI_PATH = Path.Combine(GFECLI_ROOT, GFECLI_NAME);
        private static readonly string GFECLI_HIGHLIGHT_PROCESS = "wow.exe";
        private static readonly int GFECLI_HIGHLIGHT_MIN_DURATION = 10000;
        private static readonly int CFECLI_HIGHLIGHT_PADDING = 30000;

        private static void RunGfeCLI(int duration)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(duration);
            string timeText = string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            ProcessStartInfo psi = new()
            {
                FileName = GFECLI_PATH,
                Arguments = $"--process {GFECLI_HIGHLIGHT_PROCESS} --highlight {timeText}",
                WorkingDirectory = GFECLI_ROOT,
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

        public static void Main()
        {
            Console.WriteLine("Forwarding to the end of the current combatlog...");
            using var program = new Program("D:\\Games\\World of Warcraft\\_retail_\\Logs\\WoWCombatLog.txt");
            Console.WriteLine("Monitoring for encounters. Press Enter to exit.");
            Console.ReadLine();
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

        private class EncounterInfo
        {
            public int ID { get; }
            public string Name { get; }
            public int DifficultyID { get; }
            public int GroupSize { get; }
            public bool Success { get; }
            public int FightTime { get; }
            public EncounterInfo(IDictionary<string, object?> data)
            {
                ID = data.TryGetValue("encounterID", out var encounterID) && encounterID != null && int.TryParse(encounterID.ToString(), out var _encounterID) ? _encounterID : -1;
                Name = data.TryGetValue("encounterName", out var encounterName) && encounterName != null ? encounterName.ToString() ?? string.Empty : string.Empty;
                DifficultyID = data.TryGetValue("difficultyID", out var difficultyID) && difficultyID != null && int.TryParse(difficultyID.ToString(), out var _difficultyID) ? _difficultyID : -1;
                GroupSize = data.TryGetValue("groupSize", out var groupSize) && groupSize != null && int.TryParse(groupSize.ToString(), out var _groupSize) ? _groupSize : -1;
                Success = data.TryGetValue("success", out var success) && success != null && int.TryParse(success.ToString(), out var _success) && _success > 0;
                FightTime = data.TryGetValue("fightTime", out var fightTime) && fightTime != null && int.TryParse(fightTime.ToString(), out var _fightTime) ? _fightTime : -1;
            }
            public bool IsEmpty()
            {
                return ID == -1 || Name == string.Empty || DifficultyID == -1 || GroupSize == -1 || FightTime == -1;
            }
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
            if (encounterInfo.FightTime < GFECLI_HIGHLIGHT_MIN_DURATION)
            {
                return;
            }
            Task.Run(() =>
            {
                Console.WriteLine($"Encounter {encounterInfo.ID} \"{encounterInfo.Name}\" on {encounterInfo.DifficultyID} with {encounterInfo.GroupSize} players, ended after {encounterInfo.FightTime / 1000:0} seconds, clipping highlight in {CFECLI_HIGHLIGHT_PADDING / 1000:0} seconds...");
                Thread.Sleep(CFECLI_HIGHLIGHT_PADDING);
                RunGfeCLI(encounterInfo.FightTime + CFECLI_HIGHLIGHT_PADDING * 2);
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
