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

        private static void RunGfeCLI(double duration)
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

        private static Dictionary<int, string> Difficulties = new()
        {
            { 1, "Normal" },
            { 2, "Heroic" },
            { 3, "10 Player" },
            { 4, "25 Player" },
            { 5, "10 Player (Heroic)" },
            { 6, "25 Player (Heroic)" },
            { 7, "Looking For Raid" },
            { 8, "Mythic Keystone" },
            { 9, "40 Player" },
            { 11, "Heroic Scenario" },
            { 12, "Normal Scenario" },
            { 14, "Normal" },
            { 15, "Heroic" },
            { 16, "Mythic" },
            { 17, "Looking For Raid" },
            { 18, "Event" },
            { 19, "Event" },
            { 20, "Event Scenario" },
            { 23, "Mythic" },
            { 24, "Timewalking" },
            { 25, "World PvP Scenario" },
            { 29, "PvEvP Scenario" },
            { 30, "Event" },
            { 32, "World PvP Scenario" },
            { 33, "Timewalking" },
            { 34, "PvP" },
            { 38, "Normal" },
            { 39, "Heroic" },
            { 40, "Mythic" },
            { 45, "PvP" },
            { 147, "Normal" },
            { 149, "Heroic" },
            { 150, "Normal" },
            { 151, "Looking For Raid" },
            { 152, "Visions of N'Zoth" },
            { 153, "Teeming Island" },
            { 167, "Torghast" },
            { 168, "Path of Ascension: Courage" },
            { 169, "Path of Ascension: Loyalty" },
            { 170, "Path of Ascension: Wisdom" },
            { 171, "Path of Ascension: Humility" },
            { 172, "World Boss" },
            { 192, "Challenge Level 1" }
        };

        private static string GetDifficulty(int id, string? fallback = null)
        {
            if (Difficulties.TryGetValue(id, out var text))
            {
                return text;
            }
            return fallback ?? string.Empty;
        }

        private class EncounterInfo
        {
            public int ID { get; }
            public string Name { get; }
            public int DifficultyID { get; }
            public string Difficulty { get { return GetDifficulty(DifficultyID); } }
            public int GroupSize { get; }
            public bool Success { get; }
            public double FightTime { get; }
            public EncounterInfo(IDictionary<string, object?> data)
            {
                ID = data.TryGetValue("encounterID", out var encounterID) && encounterID != null && int.TryParse(encounterID.ToString(), out var _encounterID) ? _encounterID : -1;
                Name = data.TryGetValue("encounterName", out var encounterName) && encounterName != null ? encounterName.ToString() ?? string.Empty : string.Empty;
                DifficultyID = data.TryGetValue("difficultyID", out var difficultyID) && difficultyID != null && int.TryParse(difficultyID.ToString(), out var _difficultyID) ? _difficultyID : -1;
                GroupSize = data.TryGetValue("groupSize", out var groupSize) && groupSize != null && int.TryParse(groupSize.ToString(), out var _groupSize) ? _groupSize : -1;
                Success = data.TryGetValue("success", out var success) && success != null && int.TryParse(success.ToString(), out var _success) && _success > 0;
                FightTime = data.TryGetValue("fightTime", out var fightTime) && fightTime != null && double.TryParse(fightTime.ToString(), out var _fightTime) ? _fightTime : -1;
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
            var delta = DateTime.Now - e.Timestamp;
            var padding = TimeSpan.FromMilliseconds(CFECLI_HIGHLIGHT_PADDING) - delta;
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
                RunGfeCLI(duration + CFECLI_HIGHLIGHT_PADDING);
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
