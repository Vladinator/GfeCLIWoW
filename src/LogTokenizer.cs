using Microsoft.VisualBasic.FileIO;

namespace GfeCLIWoW
{
    class LogTokenizer
    {
        public static bool TryParse(string input, out List<string> tokens)
        {
            tokens = new();

            using var reader = new StringReader(input);
            using var parser = new TextFieldParser(reader);

            parser.HasFieldsEnclosedInQuotes = true;
            parser.SetDelimiters(",");

            while (!parser.EndOfData)
            {
                string[]? fields = parser.ReadFields();
                if (fields != null)
                {
                    tokens.AddRange(fields);
                }
            }

            return true;
        }
    }
}
