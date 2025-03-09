using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace GfeCLIWoW
{
    public class ClipCriteriaContext
    {
        public required int Encounter { get; set; }
        public required string EncounterName { get; set; }
        public required int Difficulty { get; set; }
        public required string DifficultyName { get; set; }
        public required int Size { get; set; }
        public required TimeSpan Duration { get; set; }
        public required bool Success { get; set; }
    }
    class ClipCriteria
    {
        private static readonly ScriptOptions Options = ScriptOptions.Default
            .WithEmitDebugInformation(false)
            .WithAllowUnsafe(false)
            .WithImports("System", "System.Text", "System.Collections.Generic");
        private static readonly ClipCriteriaContext ValidationContext = new()
        {
            Encounter = 1,
            EncounterName = "Example",
            Difficulty = 1,
            DifficultyName = Game.GetInstanceDifficultyName(1),
            Size = 20,
            Duration = TimeSpan.FromMinutes(1),
            Success = true,
        };
        public static bool IsValid(string criteria)
        {
            if (criteria.Trim().Length == 0)
            {
                return false;
            }
            return CanClip(criteria, ValidationContext, false) != null;
        }
        public static bool? CanClip(string criteria, ClipCriteriaContext context, bool silent = true)
        {
            string code = string.Join("\n", new string[]
            {
                $"int Encounter = {context.Encounter};",
                $"string EncounterName = \"{context.EncounterName.Replace("\"", "\\\"")}\";",
                $"int Difficulty = {context.Difficulty};",
                $"string DifficultyName = \"{context.DifficultyName.Replace("\"", "\\\"")}\";",
                $"int Size = {context.Size};",
                $"TimeSpan Duration = TimeSpan.FromMilliseconds({context.Duration.TotalMilliseconds});",
                $"bool Success = {(context.Success ? "true" : "false")};",
                $"return {criteria};",
            });
            var script = CSharpScript.Create<bool>(code, Options);
            script.Compile();
            try
            {
                var result = script.RunAsync().GetAwaiter().GetResult();
                return result.ReturnValue;
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    Console.WriteLine($"[ClipCriteria.IsValid] {DateTime.Now}: {ex.Message}");
                }
            }
            return null;
        }
    }
}
