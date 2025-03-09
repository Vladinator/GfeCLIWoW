using Microsoft.VisualBasic.FileIO;

namespace GfeCLIWoW
{
    class LogTokenizer
    {
        private static bool TryParseDefault(string input, out List<string> tokens)
        {
            tokens = new();

            using var reader = new StringReader(input);
            using var parser = new TextFieldParser(reader);

            parser.HasFieldsEnclosedInQuotes = true;
            parser.SetDelimiters(",");

            while (!parser.EndOfData)
            {
                string[]? fields;
                try
                {
                    fields = parser.ReadFields();
                }
                catch (MalformedLineException)
                {
#if DEBUG
                    // Console.WriteLine($"[LogTokenizer.TryParse] {ex.Message}");
#endif
                    return false;
                }
                if (fields != null)
                {
                    tokens.AddRange(fields);
                }
            }

            return true;
        }
        private static bool TryParseEscape(string input, out List<string> tokens)
        {
            input = input.Replace("\\\"", "\"\"");
            return TryParseDefault(input, out tokens);
        }
        public static bool TryParse(string input, out List<string> tokens)
        {
            if (TryParseDefault(input, out tokens))
            {
                return true;
            }
            tokens.Clear();
            return TryParseEscape(input, out tokens);
        }
    }
}
