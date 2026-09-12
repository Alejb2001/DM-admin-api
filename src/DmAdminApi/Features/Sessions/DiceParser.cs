using System.Text.RegularExpressions;

namespace DmAdminApi.Features.Sessions;

public record DiceParseResult(
    int NumDice,
    int DiceSides,
    int Modifier,
    string? Label,
    string? Advantage,
    bool IsSecret);

public static class DiceParser
{
    private static readonly int[] ValidSides = [4, 6, 8, 10, 12, 20, 100];

    public static DiceParseResult Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Entrada vacía.");

        var text = input.Trim();

        // Must start with /roll or /r
        if (!Regex.IsMatch(text, @"^/r(?:oll)?\b", RegexOptions.IgnoreCase))
            throw new ArgumentException("La fórmula debe comenzar con /roll o /r.");

        // Remove command prefix
        text = Regex.Replace(text, @"^/r(?:oll)?\s*", "", RegexOptions.IgnoreCase).Trim();

        // Detect "secreto"
        bool isSecret = false;
        if (Regex.IsMatch(text, @"^secreto\b", RegexOptions.IgnoreCase))
        {
            isSecret = true;
            text = Regex.Replace(text, @"^secreto\s*", "", RegexOptions.IgnoreCase).Trim();
        }

        // Extract label in double quotes
        string? label = null;
        var labelMatch = Regex.Match(text, "\"([^\"]+)\"");
        if (labelMatch.Success)
        {
            label = labelMatch.Groups[1].Value;
            text = text.Replace(labelMatch.Value, "").Trim();
        }

        // Detect ventaja / desventaja
        string? advantage = null;
        if (Regex.IsMatch(text, @"\bventaja\b", RegexOptions.IgnoreCase))
        {
            advantage = "ventaja";
            text = Regex.Replace(text, @"\bventaja\b", "", RegexOptions.IgnoreCase).Trim();
        }
        else if (Regex.IsMatch(text, @"\bdesventaja\b", RegexOptions.IgnoreCase))
        {
            advantage = "desventaja";
            text = Regex.Replace(text, @"\bdesventaja\b", "", RegexOptions.IgnoreCase).Trim();
        }

        // Parse dice formula: NdX or NdX+N or NdX-N
        var diceMatch = Regex.Match(text.Trim(), @"^(\d+)[dD](\d+)\s*([+-]\s*\d+)?$");
        if (!diceMatch.Success)
            throw new ArgumentException($"Fórmula de dados no válida: '{text.Trim()}'. Ejemplo válido: 2d6, 1d20+5.");

        int numDice = int.Parse(diceMatch.Groups[1].Value);
        int diceSides = int.Parse(diceMatch.Groups[2].Value);
        int modifier = 0;
        if (diceMatch.Groups[3].Success)
        {
            var modStr = diceMatch.Groups[3].Value.Replace(" ", "");
            modifier = int.Parse(modStr);
        }

        if (numDice < 1 || numDice > 20)
            throw new ArgumentException($"El número de dados debe estar entre 1 y 20 (recibido: {numDice}).");

        if (!ValidSides.Contains(diceSides))
            throw new ArgumentException($"Tipo de dado no válido: d{diceSides}. Válidos: d4, d6, d8, d10, d12, d20, d100.");

        return new DiceParseResult(numDice, diceSides, modifier, label, advantage, isSecret);
    }
}
