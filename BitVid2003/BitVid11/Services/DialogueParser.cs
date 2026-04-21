using System.Text.RegularExpressions;

namespace BitVid11.Services
{
    public class DialogueParser
    {
        // Regex:
        // - Character name (letters + spaces)
        // - Capture everything until next "Name:" or end
        private static readonly Regex DialogueRegex = new Regex(
            @"([A-Za-z ]+):\s*(.*?)(?=\s+[A-Za-z ]+:|$)",
            RegexOptions.Singleline | RegexOptions.Compiled
        );

        public static List<string> GetCharacterNames(string input)
        {
            return Parse(input)
                .Select(d => d.Character)
                .Distinct()
                .ToList();
        }

        public static List<DialogueLine> Parse(string input)
        {
            var results = new List<DialogueLine>();

            if (string.IsNullOrWhiteSpace(input))
                return results;

            var matches = DialogueRegex.Matches(input);

            foreach (Match match in matches)
            {
                var line = new DialogueLine
                {
                    Character = match.Groups[1].Value.Trim(),
                    Text = match.Groups[2].Value.Trim()
                };

                results.Add(line);
            }

            return results;
        }
    }

    public class DialogueLine
    {
        public string Character { get; set; }
        public string Text { get; set; }
    }
}
