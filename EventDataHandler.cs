using System.Dynamic;

namespace gfecliwow
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
}
