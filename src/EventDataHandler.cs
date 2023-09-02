using System.Dynamic;

namespace GfeCLIWoW
{
    class EventDataHandler_ExpandoObjectFormatter
    {
        private static readonly string NewLine = Environment.NewLine;
        private static readonly string NewLineWithPadding = $"{NewLine}  ";

        public static string Format(ExpandoObject obj)
        {
            if (obj == null)
            {
                return "Null";
            }
            IDictionary<string, object?> expandoDict = obj;
            return $"{{{NewLineWithPadding}{string.Join($"{NewLineWithPadding}", expandoDict.Select(kv => $"{kv.Value?.GetType().Name ?? "Null"} {kv.Key} = {kv.Value}"))}{NewLine}}}";
        }
    }

    class EventDataHandler
    {
        private static object? ConvertType(string? value, Type? targetType)
        {
            if (value == null || targetType == null)
            {
                return value;
            }
            if (targetType == typeof(bool))
            {
                if (value == "1")
                {
                    return true;
                }
                if (bool.TryParse(value, out bool result))
                {
                    return result;
                }
                return false;
            }
            return Convert.ChangeType(value, targetType);
        }

        public static dynamic? Unpack(string[] data, Dictionary<string, Type> structure, int startPosition = 0)
        {
            if (startPosition < 0 || startPosition >= data.Length)
            {
                return null;
            }
            dynamic unpackedData = new ExpandoObject();
            IDictionary<string, object?> unpackedDict = unpackedData;
            foreach (var kvp in structure)
            {
                string propertyName = kvp.Key;
                Type propertyType = kvp.Value;
                if (startPosition < data.Length)
                {
                    object? unpackedValue = ConvertType(data[startPosition], propertyType);
                    unpackedDict[propertyName] = unpackedValue;
                    startPosition++;
                }
            }
            return unpackedData;
        }

        public static dynamic? UnpackKV(string[] data, Dictionary<string, Type> structure, int startPosition = 0)
        {
            if (startPosition < 0 || startPosition >= data.Length)
            {
                return null;
            }
            dynamic unpackedData = new ExpandoObject();
            IDictionary<string, object?> unpackedDict = unpackedData;
            for (int i = 0; i < data.Length; i += 2)
            {
                var dataKey = data[i];
                Type? dataType = null;
                foreach (var kvp in structure)
                {
                    if (kvp.Key == dataKey)
                    {
                        dataType = kvp.Value;
                        break;
                    }
                }
                object? dataValue = ConvertType(data[i + 1], dataType);
                unpackedDict[dataKey] = dataValue;
            }
            return unpackedData;
        }

        public static string Format(dynamic unpacked)
        {
            return EventDataHandler_ExpandoObjectFormatter.Format(unpacked);
        }

        public static void Print(dynamic? unpacked)
        {
            if (unpacked == null) return;
            Console.WriteLine(EventDataHandler.Format(unpacked));
        }
    }

    class EncounterInfo
    {
        public int ID { get; }
        public string Name { get; }
        public int DifficultyID { get; }
        public string Difficulty { get { return Game.GetInstanceDifficultyName(DifficultyID); } }
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
}
