using System.Text;

namespace ReadingList.App;

public static class CommandLineParser
{
    public static (string keyword, string[] args) Parse(string? line)
    {
        var tokens = SplitTokens(line);
        if (tokens.Length == 0)
            return ("", Array.Empty<string>());
        return (tokens[0], tokens[1..]);
    }

    public static string[] SplitTokens(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Array.Empty<string>();

        var tokens = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        char quote = '"';

        for (int i = 0; i < input.Length; i++)
        {
            var ch = input[i];

            if (inQuotes)
            {
                if (ch == '\\' && i + 1 < input.Length &&
                    (input[i + 1] == quote || input[i + 1] == '\\'))
                {
                    sb.Append(input[i + 1]);
                    i++; 
                }
                else if (ch == quote)
                {
                    inQuotes = false;
                }
                else
                {
                    sb.Append(ch);
                }
            }
            else
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (sb.Length > 0)
                    {
                        tokens.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                else if (ch == '"' || ch == '\'')
                {
                    inQuotes = true;
                    quote = ch;
                }
                else
                {
                    sb.Append(ch);
                }
            }
        }

        if (sb.Length > 0)
        {
            tokens.Add(sb.ToString());
        }

        return tokens.ToArray();
    }
}

