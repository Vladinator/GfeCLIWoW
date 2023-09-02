using System.Text.RegularExpressions;

namespace gfecliwow
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
                LogChanged?.Invoke(null, new LogReaderEventArgs(timestamp, data));
                return true;
            }
            return false;
        }

        public void ProcessChanges()
        {
            using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            reader.BaseStream.Seek(lastProcessedPosition, SeekOrigin.Begin);
            long lastValidPosition = lastProcessedPosition;

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var match = lineRegex.Match(line);
                if (!match.Success)
                {
                    break;
                }
                if (!ProcessNewLine(match))
                {
                    break;
                }
                lastValidPosition = reader.BaseStream.Position;
            }

            lastProcessedPosition = lastValidPosition;
        }
    }
}
