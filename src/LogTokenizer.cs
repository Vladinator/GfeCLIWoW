using Microsoft.VisualBasic.FileIO;

namespace GfeCLIWoW
{
    class LogTokenizer
    {
        public static bool TryParse(string input, out List<string> tokens)
        {
            input = input.Replace("\\\"", "\"\"");
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
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine($"[LogTokenizer.TryParse] {ex.Message}");
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
    }
}
