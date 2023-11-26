using System.Linq;
using System.Text.RegularExpressions;

namespace GfeCLIWoW
{
    class LogReaderEventArgs : EventArgs
    {
        public DateTime Timestamp { get; }
        public string[] Data { get; }

        public LogReaderEventArgs(DateTime timestamp, string[] data)
        {
            Timestamp = timestamp;
            Data = data;
        }
    }

    class LogReader
    {
        private static readonly Regex lineRegex = new(@"^(\d+)/(\d+)\s(\d+):(\d+):(\d+)\.(\d+)\s\s(.+)$");
        private static long lastProcessedPosition = 0;

        private static string[]? ParseEventData(string eventData)
        {
            if (LogTokenizer.TryParse(eventData, out var result))
            {
                return result.ToArray();
            }
            return null;
        }

        private static bool TryParseDateTime(Match match, out DateTime timestamp)
        {
            if (int.TryParse(match.Groups[1].Value, out int month) &&
                int.TryParse(match.Groups[2].Value, out int day) &&
                int.TryParse(match.Groups[3].Value, out int hours) &&
                int.TryParse(match.Groups[4].Value, out int minutes) &&
                int.TryParse(match.Groups[5].Value, out int seconds) &&
                int.TryParse(match.Groups[6].Value, out int milliseconds))
            {
                timestamp = new DateTime(DateTime.Now.Year, month, day, hours, minutes, seconds, milliseconds, DateTimeKind.Local);
                return true;
            }
            timestamp = default;
            return false;
        }

        public bool SkipReading;
        public bool SkipEvents;

        public void Skip(bool skip)
        {
            SkipReading = skip;
            SkipEvents = skip;
        }

        public event EventHandler<LogReaderEventArgs>? LogChanged;

        public string FilePath { get; }

        public LogReader(string filePath)
        {
            FilePath = filePath;
        }

        private bool ProcessNewLine(Match match)
        {
            string eventData = match.Groups[7].Value;
            string[]? data = ParseEventData(eventData);
            if (data == null)
            {
                return false;
            }
            if (TryParseDateTime(match, out DateTime timestamp))
            {
                if (!SkipEvents)
                {
#if DEBUG
                    // Console.WriteLine($"[LogReader.ProcessNewLine] {timestamp} {string.Join(" ", data.Take(1))}{(data.Length > 1 ? $" ... ({data.Length})" : "")}");
#endif
                    LogChanged?.Invoke(null, new LogReaderEventArgs(timestamp, data));
                }
                return true;
            }
            return false;
        }

        public void ProcessChanges()
        {
            if (!File.Exists(FilePath))
            {
#if DEBUG
                Console.WriteLine($"[LogReader.ProcessChanges] Log file doesn't exist. Skipping.");
#endif
                return;
            }

            using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            if (lastProcessedPosition > stream.Length)
            {
#if DEBUG
                Console.WriteLine($"[LogReader.ProcessChanges] Log file shrunk in size. Resetting position.");
#endif
                lastProcessedPosition = 0;
            }

            reader.BaseStream.Seek(lastProcessedPosition, SeekOrigin.Begin);
            long lastValidPosition = lastProcessedPosition;

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!SkipReading)
                {
                    if (line[..18] == "COMBAT_LOG_VERSION")
                    {
                        string[]? data = ParseEventData(line);
                        if (data == null)
                        {
#if DEBUG
                            Console.WriteLine($"[LogReader.ProcessChanges] Log file contains unsupported combat log version: {line}");
#endif
                        }
                    }
                    else
                    {
                        var match = lineRegex.Match(line);
                        if (!match.Success)
                        {
#if DEBUG
                            Console.WriteLine($"[LogReader.ProcessChanges] Log file contains invalid line: {line}");
#endif
                        }
                        else if (!ProcessNewLine(match))
                        {
#if DEBUG
                            Console.WriteLine($"[LogReader.ProcessChanges] Log file could not process line: {line}");
#endif
                        }
                    }
                }
                lastValidPosition = reader.BaseStream.Position;
            }

#if DEBUG
            Console.WriteLine($"[LogReader.ProcessChanges] Log file processed. Position is {lastValidPosition}.");
#endif

            lastProcessedPosition = lastValidPosition;
        }
    }
}
