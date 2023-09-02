namespace GfeCLIWoW
{
    class Env
    {
        public string GfeCLI { get; } = string.Empty;
        public string Process { get; } = string.Empty;
        public double MinDuration { get; } = -1;
        public double DurationPadding { get; } = -1;
        public Env()
        {
            var envFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            if (!File.Exists(envFile))
            {
                throw new Exception("You need to create a .env file in the application folder.");
            }
            var lines = File.ReadAllLines(envFile);
            if (lines == null)
            {
                throw new Exception("You need to put content in the .env file.");
            }
            foreach (var line in lines)
            {
                var offset = line.IndexOf("=");
                if (offset < 0)
                {
                    continue;
                }
                var k = line[..offset].Trim();
                var v = line[(offset + 1)..].Trim();
                switch (k)
                {
                    case "GFECLI":
                        if (v.Length > 0)
                        {
                            GfeCLI = v;
                        }
                        break;
                    case "PROCESS":
                        if (v.Length > 0)
                        {
                            Process = v;
                        }
                        break;
                    case "MIN_DURATION":
                        if (double.TryParse(v, out var minDuration) && minDuration >= 0)
                        {
                            MinDuration = minDuration;
                        }
                        break;
                    case "DURATION_PADDING":
                        if (double.TryParse(v, out var durationPadding) && durationPadding >= 0)
                        {
                            DurationPadding = durationPadding;
                        }
                        break;
                }
            }
        }
        public bool IsValid()
        {
            return GfeCLI.Length > 0 && File.Exists(GfeCLI) && Process.Length > 0 && MinDuration >= 0 && DurationPadding >= 0;
        }
    }
}
